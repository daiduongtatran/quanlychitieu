-- ============================================================================
-- RESET DATABASE SCRIPT - Xóa và tạo lại database QuanLyChiTieuDB
-- ============================================================================
-- Chú ý: Chạy script này sẽ XÓA tất cả dữ liệu cũ
-- ============================================================================

USE [master]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 1: Xóa database cũ nếu tồn tại
-- ═══════════════════════════════════════════════════════════════════════════
IF EXISTS (SELECT * FROM sys.databases WHERE name = N'QuanLyChiTieuDB')
BEGIN
    ALTER DATABASE [QuanLyChiTieuDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [QuanLyChiTieuDB];
    PRINT 'Database cũ đã được xóa';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 2: Tạo database mới
-- ═══════════════════════════════════════════════════════════════════════════
CREATE DATABASE [QuanLyChiTieuDB]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'QuanLyChiTieuDB', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA\QuanLyChiTieuDB.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'QuanLyChiTieuDB_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA\QuanLyChiTieuDB_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO

PRINT 'Database mới đã được tạo';

ALTER DATABASE [QuanLyChiTieuDB] SET COMPATIBILITY_LEVEL = 170
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [QuanLyChiTieuDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [QuanLyChiTieuDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET AUTO_CLOSE ON 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET  ENABLE_BROKER 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET  MULTI_USER 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [QuanLyChiTieuDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET OPTIMIZED_LOCKING = OFF 
GO
ALTER DATABASE [QuanLyChiTieuDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [QuanLyChiTieuDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [QuanLyChiTieuDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO

USE [QuanLyChiTieuDB]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 3: Tạo bảng NguoiDung (User)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE [dbo].[NguoiDung](
	[MaNguoiDung] [int] IDENTITY(1,1) NOT NULL,
	[TenDangNhap] [nvarchar](50) NOT NULL,
	[MatKhauHash] [nvarchar](max) NOT NULL,
	[Email] [nvarchar](100) NULL,
	[HoTen] [nvarchar](100) NULL,
	[NgayTao] [datetime] NULL,
	[SoDuTaiKhoan] [decimal](18, 2) NULL,
	[TotpSecret] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaNguoiDung] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[TenDangNhap] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[NguoiDung] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO
ALTER TABLE [dbo].[NguoiDung] ADD  DEFAULT ((0)) FOR [SoDuTaiKhoan]
GO

PRINT 'Bảng NguoiDung đã tạo';

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 4: Tạo bảng DanhMuc (Category)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE [dbo].[DanhMuc](
	[MaDanhMuc] [int] IDENTITY(1,1) NOT NULL,
	[TenDanhMuc] [nvarchar](100) NOT NULL,
	[LoaiDanhMuc] [nvarchar](20) NOT NULL,
	[BieuTuong] [nvarchar](50) NULL,
	[MaNguoiDung] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[MaDanhMuc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DanhMuc]  WITH CHECK ADD  CONSTRAINT [FK_DanhMuc_NguoiDung] FOREIGN KEY([MaNguoiDung])
REFERENCES [dbo].[NguoiDung] ([MaNguoiDung])
GO
ALTER TABLE [dbo].[DanhMuc] CHECK CONSTRAINT [FK_DanhMuc_NguoiDung]
GO

PRINT 'Bảng DanhMuc đã tạo';

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 5: Tạo bảng GiaoDich (Transaction)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE [dbo].[GiaoDich](
	[MaGiaoDich] [int] IDENTITY(1,1) NOT NULL,
	[SoTien] [decimal](18, 2) NOT NULL,
	[GhiChu] [nvarchar](max) NULL,
	[NgayGiaoDich] [datetime] NULL,
	[MaDanhMuc] [int] NOT NULL,
	[MaNguoiDung] [int] NOT NULL,
	[IsDinhKy] [bit] NOT NULL,
	[TanSuat] [nvarchar](50) NULL,
	[NgayKetThuc] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaGiaoDich] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[GiaoDich] ADD  DEFAULT (getdate()) FOR [NgayGiaoDich]
GO
ALTER TABLE [dbo].[GiaoDich] ADD  DEFAULT ((0)) FOR [IsDinhKy]
GO

ALTER TABLE [dbo].[GiaoDich]  WITH CHECK ADD  CONSTRAINT [FK_GiaoDich_DanhMuc] FOREIGN KEY([MaDanhMuc])
REFERENCES [dbo].[DanhMuc] ([MaDanhMuc])
GO
ALTER TABLE [dbo].[GiaoDich] CHECK CONSTRAINT [FK_GiaoDich_DanhMuc]
GO
ALTER TABLE [dbo].[GiaoDich]  WITH CHECK ADD  CONSTRAINT [FK_GiaoDich_NguoiDung] FOREIGN KEY([MaNguoiDung])
REFERENCES [dbo].[NguoiDung] ([MaNguoiDung])
GO
ALTER TABLE [dbo].[GiaoDich] CHECK CONSTRAINT [FK_GiaoDich_NguoiDung]
GO

PRINT 'Bảng GiaoDich đã tạo';

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 6: Tạo bảng NganSach (Budget)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE [dbo].[NganSach](
	[MaNganSach] [int] IDENTITY(1,1) NOT NULL,
	[SoTienHanMuc] [decimal](18, 2) NOT NULL,
	[NgayBatDau] [datetime] NOT NULL,
	[NgayKetThuc] [datetime] NOT NULL,
	[MaDanhMuc] [int] NULL,
	[MaNguoiDung] [int] NOT NULL,
	[LoaiNganSach] [nvarchar](50) NOT NULL,
	[SoTienNganSachThang] [decimal](18, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[MaNganSach] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[NganSach] ADD  DEFAULT ('Thang') FOR [LoaiNganSach]
GO

ALTER TABLE [dbo].[NganSach]  WITH CHECK ADD  CONSTRAINT [FK_NganSach_DanhMuc] FOREIGN KEY([MaDanhMuc])
REFERENCES [dbo].[DanhMuc] ([MaDanhMuc])
GO
ALTER TABLE [dbo].[NganSach] CHECK CONSTRAINT [FK_NganSach_DanhMuc]
GO
ALTER TABLE [dbo].[NganSach]  WITH CHECK ADD  CONSTRAINT [FK_NganSach_NguoiDung] FOREIGN KEY([MaNguoiDung])
REFERENCES [dbo].[NguoiDung] ([MaNguoiDung])
GO
ALTER TABLE [dbo].[NganSach] CHECK CONSTRAINT [FK_NganSach_NguoiDung]
GO
ALTER TABLE [dbo].[NganSach]  WITH CHECK ADD  CONSTRAINT [CK_LoaiNganSach] CHECK  (([LoaiNganSach]='DanhMuc' OR [LoaiNganSach]='Thang'))
GO
ALTER TABLE [dbo].[NganSach] CHECK CONSTRAINT [CK_LoaiNganSach]
GO

PRINT 'Bảng NganSach đã tạo';

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 7: Tạo bảng ThongBao (Notification)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE [dbo].[ThongBao](
	[MaThongBao] [int] IDENTITY(1,1) NOT NULL,
	[TieuDe] [nvarchar](200) NOT NULL,
	[NoiDung] [nvarchar](max) NOT NULL,
	[LoaiThongBao] [nvarchar](50) NOT NULL,
	[BieuTuong] [nvarchar](10) NULL,
	[DaDoc] [bit] NOT NULL,
	[NgayTao] [datetime] NOT NULL,
	[MaNguoiDung] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaThongBao] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[ThongBao] ADD  DEFAULT ((0)) FOR [DaDoc]
GO
ALTER TABLE [dbo].[ThongBao] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO

ALTER TABLE [dbo].[ThongBao]  WITH CHECK ADD  CONSTRAINT [FK_ThongBao_NguoiDung] FOREIGN KEY([MaNguoiDung])
REFERENCES [dbo].[NguoiDung] ([MaNguoiDung])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ThongBao] CHECK CONSTRAINT [FK_ThongBao_NguoiDung]
GO

PRINT 'Bảng ThongBao đã tạo';

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 8: Tạo bảng NhacNho (Reminder - Optional)
-- ═══════════════════════════════════════════════════════════════════════════
CREATE TABLE [dbo].[NhacNho](
	[MaNhacNho] [int] IDENTITY(1,1) NOT NULL,
	[MaNguoiDung] [int] NOT NULL,
	[TieuDe] [nvarchar](200) NOT NULL,
	[NoiDung] [nvarchar](max) NOT NULL,
	[LoaiThongBao] [nvarchar](50) NOT NULL,
	[SoTienBienDong] [decimal](18, 2) NULL,
	[SoDuSauGiaoDich] [decimal](18, 2) NULL,
	[TenDanhMuc] [nvarchar](100) NULL,
	[NgayTao] [datetime] NOT NULL,
	[DaDoc] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[MaNhacNho] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[NhacNho] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO
ALTER TABLE [dbo].[NhacNho] ADD  DEFAULT ((0)) FOR [DaDoc]
GO

ALTER TABLE [dbo].[NhacNho]  WITH CHECK ADD  CONSTRAINT [FK_NhacNho_NguoiDung] FOREIGN KEY([MaNguoiDung])
REFERENCES [dbo].[NguoiDung] ([MaNguoiDung])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[NhacNho] CHECK CONSTRAINT [FK_NhacNho_NguoiDung]
GO

PRINT 'Bảng NhacNho đã tạo';

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 9: Tạo các INDEX
-- ═══════════════════════════════════════════════════════════════════════════
CREATE NONCLUSTERED INDEX [IX_ThongBao_ChuaDoc] ON [dbo].[ThongBao]
(
	[MaNguoiDung] ASC,
	[DaDoc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ThongBao_NguoiDung_NgayTao] ON [dbo].[ThongBao]
(
	[MaNguoiDung] ASC,
	[NgayTao] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_NhacNho_User_DaDoc] ON [dbo].[NhacNho]
(
	[MaNguoiDung] ASC,
	[DaDoc] ASC,
	[NgayTao] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

PRINT 'Các INDEX đã tạo';

-- ═══════════════════════════════════════════════════════════════════════════
-- STEP 10: Seed dữ liệu mẫu (tùy chọn)
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '=== THÊM DỮ LIỆU MẪU ===';

-- Thêm user test
INSERT INTO [dbo].[NguoiDung] (TenDangNhap, MatKhauHash, Email, HoTen, SoDuTaiKhoan)
VALUES ('testuser', 'hashed_password_here', 'test@example.com', 'Người Dùng Test', 20000000);

PRINT 'User test đã thêm';

-- Thêm danh mục
INSERT INTO [dbo].[DanhMuc] (TenDanhMuc, LoaiDanhMuc, BieuTuong, MaNguoiDung)
VALUES
  ('Lương', 'Thu', '💰', 1),
  ('Thưởng', 'Thu', '🎁', 1),
  ('Ăn uống', 'Chi', '🍔', 1),
  ('Xăng', 'Chi', '⛽', 1),
  ('Điện nước', 'Chi', '💡', 1),
  ('Giáo dục', 'Chi', '📚', 1),
  ('Mua sắm', 'Chi', '🛍️', 1),
  ('Giải trí', 'Chi', '🎮', 1);

PRINT 'Danh mục đã thêm';

-- Thêm ngân sách tháng hiện tại (tháng 6/2026)
INSERT INTO [dbo].[NganSach] (SoTienHanMuc, NgayBatDau, NgayKetThuc, MaNguoiDung, LoaiNganSach, SoTienNganSachThang)
VALUES (20000000, '2026-06-01', '2026-06-30', 1, 'Thang', 20000000);

PRINT 'Ngân sách tháng đã thêm';

-- Thêm ngân sách danh mục
INSERT INTO [dbo].[NganSach] (SoTienHanMuc, NgayBatDau, NgayKetThuc, MaDanhMuc, MaNguoiDung, LoaiNganSach)
VALUES
  (1000000, '2026-06-01', '2026-06-30', 3, 1, 'DanhMuc'),  -- Ăn uống
  (2000000, '2026-06-01', '2026-06-30', 4, 1, 'DanhMuc'),  -- Xăng
  (1500000, '2026-06-01', '2026-06-30', 5, 1, 'DanhMuc'),  -- Điện nước
  (500000, '2026-06-01', '2026-06-30', 6, 1, 'DanhMuc');   -- Giáo dục

PRINT 'Ngân sách danh mục đã thêm';

-- ═══════════════════════════════════════════════════════════════════════════
-- COMPLETE
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '✅ DATABASE RESET HOÀN TẤT!';
PRINT '   - Database: QuanLyChiTieuDB';
PRINT '   - Tất cả bảng đã tạo';
PRINT '   - Dữ liệu mẫu đã seed';
PRINT '';

USE [master]
GO
ALTER DATABASE [QuanLyChiTieuDB] SET  READ_WRITE 
GO
