namespace Backend.Models
{
    public class NganSach
    {
        public int MaNganSach { get; set; }
        public decimal SoTienHanMuc { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }

        public int MaDanhMuc { get; set; }
        public DanhMuc DanhMuc { get; set; }

        public int MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }
    }
}