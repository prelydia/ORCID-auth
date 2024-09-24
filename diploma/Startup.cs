using diploma.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using Microsoft.AspNetCore.Rewrite;
//using Microsoft.Owin;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System;
using Microsoft.AspNetCore.Diagnostics;
using System.Linq;
using diploma.Controllers;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace diploma
{
    public class Startup
    {
        private string path;
        IWebHostEnvironment _appEnvironment;

        public Startup(IConfiguration configuration, IWebHostEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            Configuration = configuration;
            path = Path.Combine(this._appEnvironment.WebRootPath, "files");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var orcidSettings = Configuration.GetSection("OrcidSettings").Get<OrcidSettings>();

            var clientId = orcidSettings.ClientId;
            var clientSecret = orcidSettings.ClientSecret;
            var redirectUri = orcidSettings.RedirectUri;
            var accessScope = orcidSettings.AccessScope;

            // получаем строку подключения из файла конфигурации
            string connection = Configuration.GetConnectionString("DefaultConnection");
            // добавляем контекст ApplicationContext в качестве сервиса в приложение
            //services.AddDbContext<ApplicationContext>(options =>
                //options.UseSqlServer(connection));

            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys")).SetApplicationName("diploma");

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/Account/Login");
                    options.AccessDeniedPath = new Microsoft.AspNetCore.Http.PathString("/Account/Login");
                    options.Cookie.SameSite = SameSiteMode.None; // Обязательно
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Обязательно при использовании SameSite=Nones
                    options.Cookie.HttpOnly = true; // Обязательно при использовании SameSite=Nones

                }).AddOAuth("ORCID", options => {
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.CallbackPath = new Microsoft.AspNetCore.Http.PathString("/" + redirectUri);
                    options.AuthorizationEndpoint = "https://sandbox.orcid.org/oauth/authorize";
                    options.TokenEndpoint = "https://sandbox.orcid.org/oauth/token";
                    options.Scope.Add("openid");
                    options.SaveTokens = true;

                    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
                    {
                        OnRedirectToAuthorizationEndpoint = context =>
                        {
                            // Добавление параметра prompt=login к URL авторизации
                            context.Response.Redirect(context.RedirectUri + "&prompt=login");
                            return Task.CompletedTask;
                        },

                        OnCreatingTicket = async context =>
                        {
                            // Запрос на получение профиля пользователя
                            var request = new HttpRequestMessage(HttpMethod.Get, "https://sandbox.orcid.org/oauth/userinfo");
                            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                            var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var user = await response.Content.ReadAsStringAsync();
                            var userInfo = System.Text.Json.JsonDocument.Parse(user).RootElement;

                            //Console.WriteLine($"User Info JSON: {userInfo}"); // Логирование JSON-ответа

                            // Извлечение данных
                            var orcid = userInfo.GetProperty("sub").GetString();
                            var givenName = userInfo.GetProperty("given_name").GetString();
                            var familyName = userInfo.GetProperty("family_name").GetString();

                            // Если поле name отсутствует, создаём его из имени и фамилии
                            var fullName = userInfo.TryGetProperty("name", out var nameElement) && nameElement.ValueKind != System.Text.Json.JsonValueKind.Null
                                ? nameElement.GetString()
                                : $"{givenName} {familyName}";

                            // Добавление утверждений
                            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:orcid:orcid", orcid));
                            context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, fullName));
                            context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.GivenName, givenName));
                            context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Surname, familyName));

                            // Проверка в БД
                            var httpContext = context.HttpContext;
                            var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationContext>();

                            var userCheck = await dbContext.Users.FirstOrDefaultAsync(u => u.Login == orcid);
                            Role userRole = dbContext.Roles.FirstOrDefault(n => n.Id == 2); // роль пользователя
                            User ORCIDUser = new User { Login = orcid, Name = givenName, Surname = familyName, Role = userRole, isOauth = true };

                            if (userCheck == null)
                            {
                                dbContext.Users.Add(ORCIDUser);
                                await dbContext.SaveChangesAsync();

                                Directory.CreateDirectory(Path.Combine(path, ORCIDUser.Login));
                            }

                            context.Response.Redirect("/Account/OAuthCheck?success=true");
                        }
                    };
                });
            services.AddControllersWithViews().AddDataAnnotationsLocalization().AddViewLocalization();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();

            app.UseDeveloperExceptionPage();

            app.UseRequestLocalization(locOptions.Value);

            app.UseExceptionHandler("/Home/Error");

            app.UseHsts();

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/plain";

                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    if (exceptionHandlerPathFeature?.Error != null)
                    {
                        // Логирование ошибки
                        Console.WriteLine($"Unhandled exception: {exceptionHandlerPathFeature.Error}");
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later.");
                    }
                });
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
