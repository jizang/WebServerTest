using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebServer.Models;

namespace WebServer.Controllers
{
    // 1. [Authorize] 放在 Class 層級
    //    這會保護此 Controller 中的「所有」Action (Index, Privacy, Error)
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // 2. 首頁
        //    因為 Class 已經有 [Authorize]，此 Action 會自動被保護
        //    (不需要額外再加 [Authorize])
        public IActionResult Index()
        {
            return View();
        }

        // 3. Privacy 頁面
        //    因為 Class 已經有 [Authorize]，此 Action 會自動被保護
        //    (不需要額外再加 [Authorize])
        public IActionResult Privacy()
        {
            return View();
        }

        // 4. Error 頁面也應該保持公開
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}