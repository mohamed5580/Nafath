using Microsoft.AspNetCore.Mvc;

namespace Nafath.Areas.Admin.Controllers
{
    public class HomeController : Controller
    {
        [Area("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index()
        {
            return View();
        }
    }
}
