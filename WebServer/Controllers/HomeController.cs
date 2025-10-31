using System.Diagnostics; // �ޤJ�E�_�u��A�Ω������e���ʪ� ID
using Microsoft.AspNetCore.Mvc; // �ޤJ ASP.NET Core MVC �֤ߥ\��
using WebServer.Models; // �ޤJ���ε{�Ǫ� Models �R�W�Ŷ� (�Ω� ErrorViewModel)

namespace WebServer.Controllers // �w�q WebServer ������R�W�Ŷ�
{
    // HomeController �~�Ӧ� Controller ����
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; // �w�q��x�O����

        // ������غc��ơA�q�L DI �`�J ILogger
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger; // �N�ǤJ�� logger ��ҽ��
        }

        // Index �ʧ@��k (Action)�A���� /Home/Index
        public IActionResult Index()
        {
            // return View() �|�^�ǹ����� Razor ���� (Views/Home/Index.cshtml)
            return View();
        }

        // Privacy �ʧ@��k�A���� /Home/Privacy
        public IActionResult Privacy()
        {
            return View(); // �^�� Views/Home/Privacy.cshtml
        }

        // Error �ʧ@��k�A���� /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)] // �T�Φ��������֨�
        public IActionResult Error()
        {
            // �Ыؤ@�� ErrorViewModel ��ҡA�ñN�ШD ID �ǻ�����
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}