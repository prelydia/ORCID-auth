using diploma.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace diploma.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        //private ApplicationContext db;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        //[Authorize(Roles = "admin, user")]
        [Authorize]
        public IActionResult Index(int id)
        {
            string userLogin = null; // инициализация логина пользователя

            // при авторизации через ORCID
            if (User.HasClaim(c => c.Type == "urn:orcid:orcid"))
            {
                userLogin = User.FindFirst(x => x.Type == "urn:orcid:orcid").Value;
            }
            // при дефолтной авторизации
            else {
                userLogin = User.FindFirst(x => x.Type == ClaimsIdentity.DefaultNameClaimType).Value;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Info()
        {
            string userLogin = null; // инициализация логина пользователя

            // при авторизации через ORCID
            if (User.HasClaim(c => c.Type == "urn:orcid:orcid"))
            {
                userLogin = User.FindFirst(x => x.Type == "urn:orcid:orcid").Value;
            }
            // при дефолтной авторизации
            else
            {
                userLogin = User.FindFirst(x => x.Type == ClaimsIdentity.DefaultNameClaimType).Value;
            }

            return View();
        }

        public string GetCulture(string code = "")
        {
            if (!String.IsNullOrEmpty(code))
            {
                CultureInfo.CurrentCulture = new CultureInfo(code);
                CultureInfo.CurrentUICulture = new CultureInfo(code);
            }

            return CultureInfo.CurrentCulture.Name;
        }

        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        public IActionResult TestButton()
        {
            var specialFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            return Content(specialFolder);
        }

    }
}
