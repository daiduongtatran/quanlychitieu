using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Khai báo các bảng (DbSet) tương ứng với Model
        public DbSet<NguoiDung> NguoiDung { get; set; }
        public DbSet<DanhMuc> DanhMuc { get; set; }
        public DbSet<GiaoDich> GiaoDich { get; set; }
        public DbSet<NganSach> NganSach { get; set; }
    }
}