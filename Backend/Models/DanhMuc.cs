using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class DanhMuc
    {
        [Key]
        [Display(Name = "Mã danh mục")]
        public int MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string TenDanhMuc { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn loại danh mục")]
        [StringLength(50)]
        [Display(Name = "Loại danh mục")]
        public string LoaiDanhMuc { get; set; }

        [Display(Name = "Biểu tượng")]
        [StringLength(50, ErrorMessage = "Biểu tượng không được vượt quá 50 ký tự")]
        public string BieuTuong { get; set; }

        public int? MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }

        public ICollection<GiaoDich> GiaoDich { get; set; }
    }
}