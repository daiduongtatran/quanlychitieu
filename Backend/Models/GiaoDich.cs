namespace Backend.Models
{
    public class GiaoDich
    {
        public int MaGiaoDich { get; set; }
        public decimal SoTien { get; set; }
        public string GhiChu { get; set; }
        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        // Khóa ngoại link tới Danh Mục
        public int MaDanhMuc { get; set; }
        public DanhMuc DanhMuc { get; set; } // Thêm dòng này để lấy tên danh mục dễ hơn

        // Khóa ngoại link tới Người Dùng
        public int MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }
    }
}