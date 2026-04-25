namespace Backend.Models
{
    public class DanhMuc
    {
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; }
        public string LoaiDanhMuc { get; set; } // "Thu" hoặc "Chi"
        public string BieuTuong { get; set; } // Ví dụ: "🍔", "🚗"
        
        // Link tới người dùng (nếu là danh mục riêng)
        public int? MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }
    }
}