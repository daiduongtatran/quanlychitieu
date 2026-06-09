CREATE DATABASE QuanLyChiTieuDB;
GO
USE QuanLyChiTieuDB;
GO

-- 1. Bảng Người Dùng
CREATE TABLE NguoiDung (
    MaNguoiDung INT PRIMARY KEY IDENTITY(1,1),
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhauHash NVARCHAR(MAX) NOT NULL,
    Email NVARCHAR(100),
    HoTen NVARCHAR(100),
    NgayTao DATETIME DEFAULT GETDATE()
);

-- 2. Bảng Danh Mục (Ăn uống, Lương, Mua sắm...)
CREATE TABLE DanhMuc (
    MaDanhMuc INT PRIMARY KEY IDENTITY(1,1),
    TenDanhMuc NVARCHAR(100) NOT NULL,
    LoaiDanhMuc NVARCHAR(20) NOT NULL, -- 'Thu' hoặc 'Chi'
    BieuTuong NVARCHAR(50),
    MaNguoiDung INT NULL, -- NULL nếu là danh mục chung, có ID nếu là danh mục cá nhân
    CONSTRAINT FK_DanhMuc_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);

-- 3. Bảng Giao Dịch
CREATE TABLE GiaoDich (
    MaGiaoDich INT PRIMARY KEY IDENTITY(1,1),
    SoTien DECIMAL(18, 2) NOT NULL,
    GhiChu NVARCHAR(MAX),
    NgayGiaoDich DATETIME DEFAULT GETDATE(),
    MaDanhMuc INT NOT NULL,
    MaNguoiDung INT NOT NULL,
    CONSTRAINT FK_GiaoDich_DanhMuc FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc),
    CONSTRAINT FK_GiaoDich_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);

-- 4. Bảng Ngân Sách
CREATE TABLE NganSach (
    MaNganSach INT PRIMARY KEY IDENTITY(1,1),
    SoTienHanMuc DECIMAL(18, 2) NOT NULL,
    NgayBatDau DATETIME NOT NULL,
    NgayKetThuc DATETIME NOT NULL,
    MaDanhMuc INT NOT NULL,
    MaNguoiDung INT NOT NULL,
    CONSTRAINT FK_NganSach_DanhMuc FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc),
    CONSTRAINT FK_NganSach_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung)
);

-- 5. Bảng Thông Báo (biến động số dư, cảnh báo sắp hết tiền, chi tiêu lớn)
CREATE TABLE ThongBao (
    MaThongBao   INT PRIMARY KEY IDENTITY(1,1),
    TieuDe       NVARCHAR(200)  NOT NULL,
    NoiDung      NVARCHAR(MAX)  NOT NULL,
    LoaiThongBao NVARCHAR(50)   NOT NULL,   -- 'GiaoDich' | 'SapHetTien' | 'ChiTieuLon' | 'NganSach'
    BieuTuong    NVARCHAR(10)   NULL,
    DaDoc        BIT            NOT NULL DEFAULT 0,
    NgayTao      DATETIME       NOT NULL DEFAULT GETDATE(),
    MaNguoiDung  INT            NOT NULL,
    CONSTRAINT FK_ThongBao_NguoiDung FOREIGN KEY (MaNguoiDung)
        REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE
);

-- Index tăng tốc truy vấn thông báo theo user + thời gian
CREATE INDEX IX_ThongBao_NguoiDung_NgayTao
    ON ThongBao (MaNguoiDung, NgayTao DESC);

CREATE INDEX IX_ThongBao_ChuaDoc
    ON ThongBao (MaNguoiDung, DaDoc);

GO

-- ─── Seed dữ liệu danh mục mặc định ────────────────────────────────────────
INSERT INTO DanhMuc (TenDanhMuc, LoaiDanhMuc, BieuTuong) VALUES 
(N'Ăn uống', N'Chi', N'🍔'),
(N'Đi lại', N'Chi', N'🚗'),
(N'Tiền lương', N'Thu', N'💰'),
(N'Mua sắm', N'Chi', N'🛍️'),
(N'Tiền thưởng', N'Thu', N'🧧'),
(N'Tiền điện/nước', N'Chi', N'💡');

-- ─── Alter tables (migrations) ───────────────────────────────────────────────
ALTER TABLE NguoiDung
ADD SoDuTaiKhoan DECIMAL(18, 2) DEFAULT 0;

ALTER TABLE [GiaoDich] ADD [IsDinhKy] BIT NOT NULL DEFAULT 0;
ALTER TABLE [GiaoDich] ADD [TanSuat] NVARCHAR(50) NULL;
ALTER TABLE [GiaoDich] ADD [NgayKetThuc] DATETIME2 NULL;

-- Ngân sách: thêm cột tổng ngân sách tháng và cho phép MaDanhMuc NULL
ALTER TABLE NganSach ALTER COLUMN MaDanhMuc INT NULL;
ALTER TABLE NganSach ADD SoTienNganSachThang DECIMAL(18,2) NULL;
