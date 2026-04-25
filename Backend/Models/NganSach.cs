using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
    {
        public class NganSach
        {
            [Key] // Dòng này là bắt buộc
            public int MaNganSach { get; set; }

            [Required]
            [Column(TypeName = "decimal(18,2)")]
            public decimal SoTienHanMuc { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }

        public int MaDanhMuc { get; set; }
        public DanhMuc DanhMuc { get; set; }

        public int MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }
        }
    }