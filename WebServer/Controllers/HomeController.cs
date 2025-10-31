using System.Diagnostics; // 引入診斷工具，用於獲取當前活動的 ID
using Microsoft.AspNetCore.Mvc; // 引入 ASP.NET Core MVC 核心功能
using WebServer.Models; // 引入應用程序的 Models 命名空間 (用於 ErrorViewModel)

namespace WebServer.Controllers // 定義 WebServer 控制器的命名空間
{
    // HomeController 繼承自 Controller 基類
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; // 定義日誌記錄器

        // 控制器的建構函數，通過 DI 注入 ILogger
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger; // 將傳入的 logger 實例賦值
        }

        // Index 動作方法 (Action)，對應 /Home/Index
        public IActionResult Index()
        {
            // return View() 會回傳對應的 Razor 視圖 (Views/Home/Index.cshtml)
            return View();
        }

        // Privacy 動作方法，對應 /Home/Privacy
        public IActionResult Privacy()
        {
            return View(); // 回傳 Views/Home/Privacy.cshtml
        }

        // Error 動作方法，對應 /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] // 禁用此頁面的快取
        public IActionResult Error()
        {
            // 創建一個 ErrorViewModel 實例，並將請求 ID 傳遞給它
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}