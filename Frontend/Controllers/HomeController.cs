using Microsoft.AspNetCore.Mvc;
using Backend.Data; // Thêm dòng này

namespace Frontend.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        // Constructor để nhận kết nối DB
        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Kiểm tra kết nối tới DB
            bool isConnected = _context.Database.CanConnect();
            ViewBag.Status = isConnected ? "Kết nối Database thành công!" : "Kết nối Database thất bại!";

            return View();
        }
    }
}