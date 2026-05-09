using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Services;

namespace Frontend.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;

        public HomeController(AppDbContext context, ILogger<HomeController> logger, IUserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        public IActionResult Index()
        {
            // Kiểm tra kết nối tới DB
            bool isConnected = _context.Database.CanConnect();
            ViewBag.Status = isConnected ? "Kết nối Database thành công!" : "Kết nối Database thất bại!";

            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.UserName = user.HoTen;
                ViewBag.UserEmail = user.Email;
                
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return RedirectToAction("Login", "Account");
            }
        }
    }
}