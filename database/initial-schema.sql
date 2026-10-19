IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [Benh] (
        [MaBenh] int NOT NULL IDENTITY,
        [TenBenh] nvarchar(150) NOT NULL,
        [MoTa] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Benh] PRIMARY KEY ([MaBenh])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [GoiVacXin] (
        [MaGoi] int NOT NULL IDENTITY,
        [TenGoi] nvarchar(150) NOT NULL,
        [GiaGoi] decimal(18,2) NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_GoiVacXin] PRIMARY KEY ([MaGoi]),
        CONSTRAINT [CK_GoiVacXin_GiaGoi] CHECK ([GiaGoi] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [KhachHang] (
        [MaKhachHang] int NOT NULL IDENTITY,
        [HoTen] nvarchar(150) NOT NULL,
        [CCCD] varchar(12) NOT NULL,
        [SoDienThoai] varchar(15) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        CONSTRAINT [PK_KhachHang] PRIMARY KEY ([MaKhachHang])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [KhuyenMai] (
        [MaKhuyenMai] int NOT NULL IDENTITY,
        [TenKhuyenMai] nvarchar(150) NOT NULL,
        [NoiDung] nvarchar(500) NOT NULL,
        [ThoiGianBatDau] datetime2 NOT NULL,
        [ThoiGianKetThuc] datetime2 NOT NULL,
        [MucUuDai] decimal(8,2) NOT NULL,
        CONSTRAINT [PK_KhuyenMai] PRIMARY KEY ([MaKhuyenMai]),
        CONSTRAINT [CK_KhuyenMai_MucUuDai] CHECK ([MucUuDai] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [LoaiTiemChung] (
        [MaLoaiTiemChung] int NOT NULL IDENTITY,
        [TenLoaiTiemChung] nvarchar(120) NOT NULL,
        [MoTa] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_LoaiTiemChung] PRIMARY KEY ([MaLoaiTiemChung])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [LoaiVacXin] (
        [MaLoaiVacXin] int NOT NULL IDENTITY,
        [TenLoaiVacXin] nvarchar(120) NOT NULL,
        [MoTa] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_LoaiVacXin] PRIMARY KEY ([MaLoaiVacXin])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [NhanVien] (
        [MaNhanVien] int NOT NULL IDENTITY,
        [HoTen] nvarchar(150) NOT NULL,
        [CCCD] varchar(12) NOT NULL,
        [SoDienThoai] varchar(15) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [BoPhan] nvarchar(100) NOT NULL,
        [ChucDanh] nvarchar(100) NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_NhanVien] PRIMARY KEY ([MaNhanVien])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [QuyenHan] (
        [MaQuyen] int NOT NULL IDENTITY,
        [TenQuyen] nvarchar(100) NOT NULL,
        [MoTa] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_QuyenHan] PRIMARY KEY ([MaQuyen])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [NguoiDuocTiem] (
        [MaNguoiDuocTiem] int NOT NULL IDENTITY,
        [MaKhachHang] int NOT NULL,
        [HoTen] nvarchar(150) NOT NULL,
        [CCCD] varchar(12) NULL,
        [NgaySinh] date NOT NULL,
        [GioiTinh] nvarchar(20) NOT NULL,
        [QuanHeVoiKhachHang] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_NguoiDuocTiem] PRIMARY KEY ([MaNguoiDuocTiem]),
        CONSTRAINT [FK_NguoiDuocTiem_KhachHang_MaKhachHang] FOREIGN KEY ([MaKhachHang]) REFERENCES [KhachHang] ([MaKhachHang]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [VacXin] (
        [MaVacXin] int NOT NULL IDENTITY,
        [MaLoaiVacXin] int NOT NULL,
        [MaLoaiTiemChung] int NOT NULL,
        [TenVacXin] nvarchar(150) NOT NULL,
        [NhaSanXuat] nvarchar(150) NOT NULL,
        [XuatXu] nvarchar(100) NOT NULL,
        [LieuLuong] nvarchar(100) NOT NULL,
        [TheTich] decimal(10,2) NOT NULL,
        [PhanUngSauTiem] nvarchar(1000) NOT NULL,
        [Gia] decimal(18,2) NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_VacXin] PRIMARY KEY ([MaVacXin]),
        CONSTRAINT [CK_VacXin_Gia] CHECK ([Gia] >= 0),
        CONSTRAINT [FK_VacXin_LoaiTiemChung_MaLoaiTiemChung] FOREIGN KEY ([MaLoaiTiemChung]) REFERENCES [LoaiTiemChung] ([MaLoaiTiemChung]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VacXin_LoaiVacXin_MaLoaiVacXin] FOREIGN KEY ([MaLoaiVacXin]) REFERENCES [LoaiVacXin] ([MaLoaiVacXin]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [TaiKhoan] (
        [MaTaiKhoan] int NOT NULL IDENTITY,
        [MaNhanVien] int NOT NULL,
        [TenDangNhap] nvarchar(100) NOT NULL,
        [MatKhauHash] nvarchar(255) NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_TaiKhoan] PRIMARY KEY ([MaTaiKhoan]),
        CONSTRAINT [FK_TaiKhoan_NhanVien_MaNhanVien] FOREIGN KEY ([MaNhanVien]) REFERENCES [NhanVien] ([MaNhanVien]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [DangKyDichVu] (
        [MaDangKy] int NOT NULL IDENTITY,
        [MaKhachHang] int NOT NULL,
        [MaNguoiDuocTiem] int NOT NULL,
        [MaNhanVien] int NOT NULL,
        [MaKhuyenMai] int NULL,
        [NgayDangKy] datetime2 NOT NULL,
        [HinhThucTiem] nvarchar(50) NOT NULL,
        [ThoiGianDuKien] datetime2 NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DangKyDichVu] PRIMARY KEY ([MaDangKy]),
        CONSTRAINT [FK_DangKyDichVu_KhachHang_MaKhachHang] FOREIGN KEY ([MaKhachHang]) REFERENCES [KhachHang] ([MaKhachHang]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DangKyDichVu_KhuyenMai_MaKhuyenMai] FOREIGN KEY ([MaKhuyenMai]) REFERENCES [KhuyenMai] ([MaKhuyenMai]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DangKyDichVu_NguoiDuocTiem_MaNguoiDuocTiem] FOREIGN KEY ([MaNguoiDuocTiem]) REFERENCES [NguoiDuocTiem] ([MaNguoiDuocTiem]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DangKyDichVu_NhanVien_MaNhanVien] FOREIGN KEY ([MaNhanVien]) REFERENCES [NhanVien] ([MaNhanVien]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [LichHenTiem] (
        [MaLichHen] int NOT NULL IDENTITY,
        [MaNguoiDuocTiem] int NOT NULL,
        [ThoiGianHen] datetime2 NOT NULL,
        [DiemTiem] nvarchar(200) NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_LichHenTiem] PRIMARY KEY ([MaLichHen]),
        CONSTRAINT [FK_LichHenTiem_NguoiDuocTiem_MaNguoiDuocTiem] FOREIGN KEY ([MaNguoiDuocTiem]) REFERENCES [NguoiDuocTiem] ([MaNguoiDuocTiem]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [GoiVacXin_VacXin] (
        [MaGoi] int NOT NULL,
        [MaVacXin] int NOT NULL,
        [SoLuong] int NOT NULL,
        CONSTRAINT [PK_GoiVacXin_VacXin] PRIMARY KEY ([MaGoi], [MaVacXin]),
        CONSTRAINT [CK_GoiVacXin_VacXin_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [FK_GoiVacXin_VacXin_GoiVacXin_MaGoi] FOREIGN KEY ([MaGoi]) REFERENCES [GoiVacXin] ([MaGoi]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoiVacXin_VacXin_VacXin_MaVacXin] FOREIGN KEY ([MaVacXin]) REFERENCES [VacXin] ([MaVacXin]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [LoVacXin] (
        [SoLo] nvarchar(80) NOT NULL,
        [MaVacXin] int NOT NULL,
        [NgaySanXuat] date NOT NULL,
        [HanSuDung] date NOT NULL,
        [SoLuongBanDau] int NOT NULL,
        [SoLuongTon] int NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_LoVacXin] PRIMARY KEY ([SoLo]),
        CONSTRAINT [CK_LoVacXin_HanSuDung] CHECK ([HanSuDung] >= [NgaySanXuat]),
        CONSTRAINT [CK_LoVacXin_SoLuongBanDau] CHECK ([SoLuongBanDau] >= 0),
        CONSTRAINT [CK_LoVacXin_SoLuongTon] CHECK ([SoLuongTon] >= 0),
        CONSTRAINT [FK_LoVacXin_VacXin_MaVacXin] FOREIGN KEY ([MaVacXin]) REFERENCES [VacXin] ([MaVacXin]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [VacXin_Benh] (
        [MaVacXin] int NOT NULL,
        [MaBenh] int NOT NULL,
        CONSTRAINT [PK_VacXin_Benh] PRIMARY KEY ([MaVacXin], [MaBenh]),
        CONSTRAINT [FK_VacXin_Benh_Benh_MaBenh] FOREIGN KEY ([MaBenh]) REFERENCES [Benh] ([MaBenh]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VacXin_Benh_VacXin_MaVacXin] FOREIGN KEY ([MaVacXin]) REFERENCES [VacXin] ([MaVacXin]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [TaiKhoan_QuyenHan] (
        [MaTaiKhoan] int NOT NULL,
        [MaQuyen] int NOT NULL,
        CONSTRAINT [PK_TaiKhoan_QuyenHan] PRIMARY KEY ([MaTaiKhoan], [MaQuyen]),
        CONSTRAINT [FK_TaiKhoan_QuyenHan_QuyenHan_MaQuyen] FOREIGN KEY ([MaQuyen]) REFERENCES [QuyenHan] ([MaQuyen]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TaiKhoan_QuyenHan_TaiKhoan_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [ChiTietGoiVacXin] (
        [MaChiTietGoi] int NOT NULL IDENTITY,
        [MaDangKy] int NOT NULL,
        [MaGoi] int NOT NULL,
        [SoLuongGoi] int NOT NULL,
        [GiaTaiThoiDiemDangKy] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ChiTietGoiVacXin] PRIMARY KEY ([MaChiTietGoi]),
        CONSTRAINT [CK_ChiTietGoiVacXin_GiaTaiThoiDiemDangKy] CHECK ([GiaTaiThoiDiemDangKy] >= 0),
        CONSTRAINT [CK_ChiTietGoiVacXin_SoLuongGoi] CHECK ([SoLuongGoi] > 0),
        CONSTRAINT [FK_ChiTietGoiVacXin_DangKyDichVu_MaDangKy] FOREIGN KEY ([MaDangKy]) REFERENCES [DangKyDichVu] ([MaDangKy]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietGoiVacXin_GoiVacXin_MaGoi] FOREIGN KEY ([MaGoi]) REFERENCES [GoiVacXin] ([MaGoi]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [ChiTietVacXinLe] (
        [MaChiTietVacXin] int NOT NULL IDENTITY,
        [MaDangKy] int NOT NULL,
        [MaVacXin] int NOT NULL,
        [SoLuong] int NOT NULL,
        [GiaTaiThoiDiemDangKy] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ChiTietVacXinLe] PRIMARY KEY ([MaChiTietVacXin]),
        CONSTRAINT [CK_ChiTietVacXinLe_GiaTaiThoiDiemDangKy] CHECK ([GiaTaiThoiDiemDangKy] >= 0),
        CONSTRAINT [CK_ChiTietVacXinLe_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [FK_ChiTietVacXinLe_DangKyDichVu_MaDangKy] FOREIGN KEY ([MaDangKy]) REFERENCES [DangKyDichVu] ([MaDangKy]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietVacXinLe_VacXin_MaVacXin] FOREIGN KEY ([MaVacXin]) REFERENCES [VacXin] ([MaVacXin]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [HangDoiVacXin] (
        [MaHangDoi] int NOT NULL IDENTITY,
        [MaDangKy] int NOT NULL,
        [MaVacXin] int NOT NULL,
        [ThoiDiemVaoHang] datetime2 NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_HangDoiVacXin] PRIMARY KEY ([MaHangDoi]),
        CONSTRAINT [FK_HangDoiVacXin_DangKyDichVu_MaDangKy] FOREIGN KEY ([MaDangKy]) REFERENCES [DangKyDichVu] ([MaDangKy]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HangDoiVacXin_VacXin_MaVacXin] FOREIGN KEY ([MaVacXin]) REFERENCES [VacXin] ([MaVacXin]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [PhieuKhamSangLoc] (
        [MaPhieuKham] int NOT NULL IDENTITY,
        [MaDangKy] int NOT NULL,
        [MaNguoiDuocTiem] int NOT NULL,
        [MaNhanVien] int NOT NULL,
        [ChiSoSinhTon] nvarchar(500) NOT NULL,
        [TienSuBenhLy] nvarchar(1000) NOT NULL,
        [KetLuan] nvarchar(500) NOT NULL,
        [LyDoHoan] nvarchar(500) NULL,
        CONSTRAINT [PK_PhieuKhamSangLoc] PRIMARY KEY ([MaPhieuKham]),
        CONSTRAINT [FK_PhieuKhamSangLoc_DangKyDichVu_MaDangKy] FOREIGN KEY ([MaDangKy]) REFERENCES [DangKyDichVu] ([MaDangKy]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuKhamSangLoc_NguoiDuocTiem_MaNguoiDuocTiem] FOREIGN KEY ([MaNguoiDuocTiem]) REFERENCES [NguoiDuocTiem] ([MaNguoiDuocTiem]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuKhamSangLoc_NhanVien_MaNhanVien] FOREIGN KEY ([MaNhanVien]) REFERENCES [NhanVien] ([MaNhanVien]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [ThanhToan] (
        [MaThanhToan] int NOT NULL IDENTITY,
        [MaDangKy] int NOT NULL,
        [MaNhanVien] int NOT NULL,
        [SoTien] decimal(18,2) NOT NULL,
        [PhuongThuc] nvarchar(50) NOT NULL,
        [ThoiDiem] datetime2 NOT NULL,
        [LoaiThanhToan] nvarchar(30) NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_ThanhToan] PRIMARY KEY ([MaThanhToan]),
        CONSTRAINT [CK_ThanhToan_SoTien] CHECK ([SoTien] > 0),
        CONSTRAINT [FK_ThanhToan_DangKyDichVu_MaDangKy] FOREIGN KEY ([MaDangKy]) REFERENCES [DangKyDichVu] ([MaDangKy]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ThanhToan_NhanVien_MaNhanVien] FOREIGN KEY ([MaNhanVien]) REFERENCES [NhanVien] ([MaNhanVien]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [NhacLich] (
        [MaNhacLich] int NOT NULL IDENTITY,
        [MaLichHen] int NOT NULL,
        [KenhGui] nvarchar(30) NOT NULL,
        [ThoiDiemGui] datetime2 NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_NhacLich] PRIMARY KEY ([MaNhacLich]),
        CONSTRAINT [FK_NhacLich_LichHenTiem_MaLichHen] FOREIGN KEY ([MaLichHen]) REFERENCES [LichHenTiem] ([MaLichHen]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [BienDongKho] (
        [MaBienDong] int NOT NULL IDENTITY,
        [SoLo] nvarchar(80) NOT NULL,
        [MaNhanVien] int NOT NULL,
        [LoaiBienDong] nvarchar(30) NOT NULL,
        [SoLuong] int NOT NULL,
        [ThoiDiem] datetime2 NOT NULL,
        CONSTRAINT [PK_BienDongKho] PRIMARY KEY ([MaBienDong]),
        CONSTRAINT [CK_BienDongKho_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [FK_BienDongKho_LoVacXin_SoLo] FOREIGN KEY ([SoLo]) REFERENCES [LoVacXin] ([SoLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BienDongKho_NhanVien_MaNhanVien] FOREIGN KEY ([MaNhanVien]) REFERENCES [NhanVien] ([MaNhanVien]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [MuiTiem] (
        [MaMuiTiem] int NOT NULL IDENTITY,
        [MaNguoiDuocTiem] int NOT NULL,
        [SoLo] nvarchar(80) NOT NULL,
        [MaNhanVien] int NOT NULL,
        [ThoiDiemTiem] datetime2 NOT NULL,
        [ViTriTiem] nvarchar(100) NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_MuiTiem] PRIMARY KEY ([MaMuiTiem]),
        CONSTRAINT [FK_MuiTiem_LoVacXin_SoLo] FOREIGN KEY ([SoLo]) REFERENCES [LoVacXin] ([SoLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MuiTiem_NguoiDuocTiem_MaNguoiDuocTiem] FOREIGN KEY ([MaNguoiDuocTiem]) REFERENCES [NguoiDuocTiem] ([MaNguoiDuocTiem]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MuiTiem_NhanVien_MaNhanVien] FOREIGN KEY ([MaNhanVien]) REFERENCES [NhanVien] ([MaNhanVien]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [ChiDinhTiem] (
        [MaChiDinh] int NOT NULL IDENTITY,
        [MaPhieuKham] int NOT NULL,
        [MaVacXin] int NOT NULL,
        [KetQuaChiDinh] nvarchar(50) NOT NULL,
        [ThoiGian] datetime2 NOT NULL,
        [GhiChu] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_ChiDinhTiem] PRIMARY KEY ([MaChiDinh]),
        CONSTRAINT [FK_ChiDinhTiem_PhieuKhamSangLoc_MaPhieuKham] FOREIGN KEY ([MaPhieuKham]) REFERENCES [PhieuKhamSangLoc] ([MaPhieuKham]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiDinhTiem_VacXin_MaVacXin] FOREIGN KEY ([MaVacXin]) REFERENCES [VacXin] ([MaVacXin]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE TABLE [TheoDoiSauTiem] (
        [MaTheoDoi] int NOT NULL IDENTITY,
        [MaMuiTiem] int NOT NULL,
        [TinhTrang] nvarchar(100) NOT NULL,
        [MucDoPhanUng] nvarchar(100) NOT NULL,
        [HuongXuTri] nvarchar(500) NOT NULL,
        [ThoiDiem] datetime2 NOT NULL,
        CONSTRAINT [PK_TheoDoiSauTiem] PRIMARY KEY ([MaTheoDoi]),
        CONSTRAINT [FK_TheoDoiSauTiem_MuiTiem_MaMuiTiem] FOREIGN KEY ([MaMuiTiem]) REFERENCES [MuiTiem] ([MaMuiTiem]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BienDongKho_MaNhanVien] ON [BienDongKho] ([MaNhanVien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BienDongKho_SoLo] ON [BienDongKho] ([SoLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChiDinhTiem_MaPhieuKham] ON [ChiDinhTiem] ([MaPhieuKham]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChiDinhTiem_MaVacXin] ON [ChiDinhTiem] ([MaVacXin]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiVacXin_MaDangKy] ON [ChiTietGoiVacXin] ([MaDangKy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiVacXin_MaGoi] ON [ChiTietGoiVacXin] ([MaGoi]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChiTietVacXinLe_MaDangKy] ON [ChiTietVacXinLe] ([MaDangKy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ChiTietVacXinLe_MaVacXin] ON [ChiTietVacXinLe] ([MaVacXin]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DangKyDichVu_MaKhachHang] ON [DangKyDichVu] ([MaKhachHang]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DangKyDichVu_MaKhuyenMai] ON [DangKyDichVu] ([MaKhuyenMai]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DangKyDichVu_MaNguoiDuocTiem] ON [DangKyDichVu] ([MaNguoiDuocTiem]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DangKyDichVu_MaNhanVien] ON [DangKyDichVu] ([MaNhanVien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GoiVacXin_VacXin_MaVacXin] ON [GoiVacXin_VacXin] ([MaVacXin]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HangDoiVacXin_MaDangKy] ON [HangDoiVacXin] ([MaDangKy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HangDoiVacXin_MaVacXin] ON [HangDoiVacXin] ([MaVacXin]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LichHenTiem_MaNguoiDuocTiem] ON [LichHenTiem] ([MaNguoiDuocTiem]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LoVacXin_MaVacXin] ON [LoVacXin] ([MaVacXin]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MuiTiem_MaNguoiDuocTiem] ON [MuiTiem] ([MaNguoiDuocTiem]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MuiTiem_MaNhanVien] ON [MuiTiem] ([MaNhanVien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MuiTiem_SoLo] ON [MuiTiem] ([SoLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_NguoiDuocTiem_MaKhachHang] ON [NguoiDuocTiem] ([MaKhachHang]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_NhacLich_MaLichHen] ON [NhacLich] ([MaLichHen]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PhieuKhamSangLoc_MaDangKy] ON [PhieuKhamSangLoc] ([MaDangKy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PhieuKhamSangLoc_MaNguoiDuocTiem] ON [PhieuKhamSangLoc] ([MaNguoiDuocTiem]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PhieuKhamSangLoc_MaNhanVien] ON [PhieuKhamSangLoc] ([MaNhanVien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaiKhoan_MaNhanVien] ON [TaiKhoan] ([MaNhanVien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaiKhoan_TenDangNhap] ON [TaiKhoan] ([TenDangNhap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TaiKhoan_QuyenHan_MaQuyen] ON [TaiKhoan_QuyenHan] ([MaQuyen]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ThanhToan_MaDangKy] ON [ThanhToan] ([MaDangKy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ThanhToan_MaNhanVien] ON [ThanhToan] ([MaNhanVien]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TheoDoiSauTiem_MaMuiTiem] ON [TheoDoiSauTiem] ([MaMuiTiem]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VacXin_MaLoaiTiemChung] ON [VacXin] ([MaLoaiTiemChung]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VacXin_MaLoaiVacXin] ON [VacXin] ([MaLoaiVacXin]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VacXin_Benh_MaBenh] ON [VacXin_Benh] ([MaBenh]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019094242_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019094242_InitialCreate', N'10.0.13');
END;

COMMIT;
GO
