using diploma.Models;
using diploma.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


namespace diploma.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationContext _context;
        private string path;
        IWebHostEnvironment _appEnvironment;
        public static string oldPassword;
        public static int salt; 
        public AccountController(IWebHostEnvironment appEnvironment, IConfiguration configuration)
        {
            _appEnvironment = appEnvironment;
            Configuration = configuration;
            path = Path.Combine(this._appEnvironment.WebRootPath, "files");
            salt = 12;
        }

        public IConfiguration Configuration { get; }


        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.RecaptchaSiteKey = Configuration["Google:RecaptchaV3SiteKey"];
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string login, string pass)
        {
            User user = await _context.Users.Include(n => n.Role).FirstOrDefaultAsync(u => u.Login == login);

            LoginModel model = new LoginModel();

            if (!user.isOauth && user != null && pass == user.Password)
            {
                model.Login = user.Login;
                model.Password = user.Password;

                if (ModelState.IsValid) {
                    await Authenticate(user); // аутентификация

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Неправильный логин или пароль");
                }
            }
            else
            {
                ModelState.AddModelError("", "Неправильный логин или пароль");
            }

            return View(model);
        }


        public async Task<IActionResult> ORCIDLogin()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "ORCID");
        }

        public async Task<IActionResult> ORCIDLogOut()
        {
            return SignOut(new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme);
        }
        public async Task Authenticate(User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name)
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

    }
}
