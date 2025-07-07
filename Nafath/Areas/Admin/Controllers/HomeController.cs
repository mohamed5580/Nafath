using Microsoft.AspNetCore.Mvc;
namespace Nafath.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        // either remove this attribute entirely…
        // public IActionResult Index() { … }

        // …or explicitly mark it as GET
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
