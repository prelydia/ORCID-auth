# Авторизация пользователя через ORCID

В интернете почти нет информации об этом, поэтому делюсь своими наработками. Может быть, кому-то пригодится

Для начала нужно зарегистрироваться в песочнице ORCID (url: sandbox.orcid.org), чтобы безопасно тестировать и отлаживать код. После регистрации следует перейти в инстументы разработчика и, заполнив информацию о системе, получить токены (Client Id и Client Secret). 
Для большей безопасности хранить эти токены следует в appsettings.json. Стоит отметить, что редиректы в appsettings.json и в инструментах разработчика в песочнице должны совпадать между собой. У меня не получилось на локальном хосте тестить, потому что запрещен редирект на localhost.
Возможно, у вас получится с помощью какого-нибудь ngrok'a

```C#
  "OrcidSettings": {
    "ClientId": "YOUR-CLIENT-ID",
    "ClientSecret": "YOUR-CLIENT-SECRET",
    "RedirectUri": "http://localhost:5001/Access",
    "AccessScope": "/read-limited"
  },
```
Требуется установка библиотек Owin и OAuth. Создаем класс OrcidSettings в папке Models.

```C#
public class OrcidSettings
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RedirectUri { get; set; }
    public string AccessScope { get; set; }
}
```

В файле Startup.cs добавляем авторизацию через OAuth

```C#
  ...
  ).AddOAuth("ORCID", options => {
      options.ClientId = clientId;
      options.ClientSecret = clientSecret;
      options.CallbackPath = new Microsoft.AspNetCore.Http.PathString("/" + redirectUri);
      options.AuthorizationEndpoint = "https://sandbox.orcid.org/oauth/authorize";
      options.TokenEndpoint = "https://sandbox.orcid.org/oauth/token";
      options.Scope.Add("openid");
      options.SaveTokens = true;
```

Добавляем обработку событий

```C#
 options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
  {
      OnRedirectToAuthorizationEndpoint = context =>
      {
          // параметр promt=login обязывает пользователя авторизироваться по запросу, игнорируя единый вход
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

            // Извлечение данных
            var orcid = userInfo.GetProperty("sub").GetString(); // ORCID ID авторизированного пользователя
            var givenName = userInfo.GetProperty("given_name").GetString(); // имя пользователя
            var familyName = userInfo.GetProperty("family_name").GetString(); // фамилия пользователя

            // Если поле name отсутствует, создаём его из имени и фамилии
            var fullName = userInfo.TryGetProperty("name", out var nameElement) && nameElement.ValueKind != System.Text.Json.JsonValueKind.Null
                ? nameElement.GetString()
                : $"{givenName} {familyName}";

            // Добавление утверждений
            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:orcid:orcid", orcid));
            context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, fullName));
            context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.GivenName, givenName));
            context.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Surname, familyName));
            ...
```
