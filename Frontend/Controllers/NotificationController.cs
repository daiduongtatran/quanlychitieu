using Microsoft.AspNetCore.Mvc;
using Frontend.Services;

namespace Frontend.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { count = 0 });

            var count = await _notificationService.LaySoThongBaoChuaDocAsync(userId.Value);
            return Json(new { count });
        }
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, items = new List<object>() });

            var list = await _notificationService.LayDanhSachThongBaoAsync(userId.Value, 50);

            var result = list.Select(t => new
            {
                id         = t.MaThongBao,
                tieuDe     = t.TieuDe,
                noiDung    = t.NoiDung,
                loai       = t.LoaiThongBao,
                bieuTuong  = t.BieuTuong,
                daDoc      = t.DaDoc,
                ngayTao    = t.NgayTao.ToString("HH:mm dd/MM/yyyy"),
                ngayTaoTs  = ((DateTimeOffset)t.NgayTao).ToUnixTimeSeconds()
            });

            return Json(new { success = true, items = result });
        }
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            await _notificationService.DanhDauTatCaDaDocAsync(userId.Value);
            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false });

            await _notificationService.DanhDauDaDocAsync(userId.Value, id);
            return Json(new { success = true });
        }
    }
}
