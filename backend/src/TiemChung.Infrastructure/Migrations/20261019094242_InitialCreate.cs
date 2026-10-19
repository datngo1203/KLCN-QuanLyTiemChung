using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiemChung.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Benh",
                columns: table => new
                {
                    MaBenh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenBenh = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benh", x => x.MaBenh);
                });

            migrationBuilder.CreateTable(
                name: "GoiVacXin",
                columns: table => new
                {
                    MaGoi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenGoi = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    GiaGoi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiVacXin", x => x.MaGoi);
                    table.CheckConstraint("CK_GoiVacXin_GiaGoi", "[GiaGoi] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    MaKhachHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    CCCD = table.Column<string>(type: "varchar(12)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "varchar(15)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHang", x => x.MaKhachHang);
                });

            migrationBuilder.CreateTable(
                name: "KhuyenMai",
                columns: table => new
                {
                    MaKhuyenMai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhuyenMai = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MucUuDai = table.Column<decimal>(type: "decimal(8,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMai", x => x.MaKhuyenMai);
                    table.CheckConstraint("CK_KhuyenMai_MucUuDai", "[MucUuDai] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "LoaiTiemChung",
                columns: table => new
                {
                    MaLoaiTiemChung = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiTiemChung = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiTiemChung", x => x.MaLoaiTiemChung);
                });

            migrationBuilder.CreateTable(
                name: "LoaiVacXin",
                columns: table => new
                {
                    MaLoaiVacXin = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiVacXin = table.Column<string>(type: "nvarchar(120)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiVacXin", x => x.MaLoaiVacXin);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    MaNhanVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    CCCD = table.Column<string>(type: "varchar(12)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "varchar(15)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", nullable: false),
                    BoPhan = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ChucDanh = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVien", x => x.MaNhanVien);
                });

            migrationBuilder.CreateTable(
                name: "QuyenHan",
                columns: table => new
                {
                    MaQuyen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenQuyen = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyenHan", x => x.MaQuyen);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDuocTiem",
                columns: table => new
                {
                    MaNguoiDuocTiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhachHang = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    CCCD = table.Column<string>(type: "varchar(12)", nullable: true),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    QuanHeVoiKhachHang = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDuocTiem", x => x.MaNguoiDuocTiem);
                    table.ForeignKey(
                        name: "FK_NguoiDuocTiem_KhachHang_MaKhachHang",
                        column: x => x.MaKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "MaKhachHang",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VacXin",
                columns: table => new
                {
                    MaVacXin = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLoaiVacXin = table.Column<int>(type: "int", nullable: false),
                    MaLoaiTiemChung = table.Column<int>(type: "int", nullable: false),
                    TenVacXin = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    NhaSanXuat = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    XuatXu = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    LieuLuong = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TheTich = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PhanUngSauTiem = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacXin", x => x.MaVacXin);
                    table.CheckConstraint("CK_VacXin_Gia", "[Gia] >= 0");
                    table.ForeignKey(
                        name: "FK_VacXin_LoaiTiemChung_MaLoaiTiemChung",
                        column: x => x.MaLoaiTiemChung,
                        principalTable: "LoaiTiemChung",
                        principalColumn: "MaLoaiTiemChung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacXin_LoaiVacXin_MaLoaiVacXin",
                        column: x => x.MaLoaiVacXin,
                        principalTable: "LoaiVacXin",
                        principalColumn: "MaLoaiVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    TenDangNhap = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DangKyDichVu",
                columns: table => new
                {
                    MaDangKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhachHang = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDuocTiem = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    MaKhuyenMai = table.Column<int>(type: "int", nullable: true),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HinhThucTiem = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ThoiGianDuKien = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyDichVu", x => x.MaDangKy);
                    table.ForeignKey(
                        name: "FK_DangKyDichVu_KhachHang_MaKhachHang",
                        column: x => x.MaKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "MaKhachHang",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DangKyDichVu_KhuyenMai_MaKhuyenMai",
                        column: x => x.MaKhuyenMai,
                        principalTable: "KhuyenMai",
                        principalColumn: "MaKhuyenMai",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DangKyDichVu_NguoiDuocTiem_MaNguoiDuocTiem",
                        column: x => x.MaNguoiDuocTiem,
                        principalTable: "NguoiDuocTiem",
                        principalColumn: "MaNguoiDuocTiem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DangKyDichVu_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichHenTiem",
                columns: table => new
                {
                    MaLichHen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiDuocTiem = table.Column<int>(type: "int", nullable: false),
                    ThoiGianHen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiemTiem = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichHenTiem", x => x.MaLichHen);
                    table.ForeignKey(
                        name: "FK_LichHenTiem_NguoiDuocTiem_MaNguoiDuocTiem",
                        column: x => x.MaNguoiDuocTiem,
                        principalTable: "NguoiDuocTiem",
                        principalColumn: "MaNguoiDuocTiem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoiVacXin_VacXin",
                columns: table => new
                {
                    MaGoi = table.Column<int>(type: "int", nullable: false),
                    MaVacXin = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiVacXin_VacXin", x => new { x.MaGoi, x.MaVacXin });
                    table.CheckConstraint("CK_GoiVacXin_VacXin_SoLuong", "[SoLuong] > 0");
                    table.ForeignKey(
                        name: "FK_GoiVacXin_VacXin_GoiVacXin_MaGoi",
                        column: x => x.MaGoi,
                        principalTable: "GoiVacXin",
                        principalColumn: "MaGoi",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoiVacXin_VacXin_VacXin_MaVacXin",
                        column: x => x.MaVacXin,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoVacXin",
                columns: table => new
                {
                    SoLo = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    MaVacXin = table.Column<int>(type: "int", nullable: false),
                    NgaySanXuat = table.Column<DateOnly>(type: "date", nullable: false),
                    HanSuDung = table.Column<DateOnly>(type: "date", nullable: false),
                    SoLuongBanDau = table.Column<int>(type: "int", nullable: false),
                    SoLuongTon = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoVacXin", x => x.SoLo);
                    table.CheckConstraint("CK_LoVacXin_HanSuDung", "[HanSuDung] >= [NgaySanXuat]");
                    table.CheckConstraint("CK_LoVacXin_SoLuongBanDau", "[SoLuongBanDau] >= 0");
                    table.CheckConstraint("CK_LoVacXin_SoLuongTon", "[SoLuongTon] >= 0");
                    table.ForeignKey(
                        name: "FK_LoVacXin_VacXin_MaVacXin",
                        column: x => x.MaVacXin,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VacXin_Benh",
                columns: table => new
                {
                    MaVacXin = table.Column<int>(type: "int", nullable: false),
                    MaBenh = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacXin_Benh", x => new { x.MaVacXin, x.MaBenh });
                    table.ForeignKey(
                        name: "FK_VacXin_Benh_Benh_MaBenh",
                        column: x => x.MaBenh,
                        principalTable: "Benh",
                        principalColumn: "MaBenh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacXin_Benh_VacXin_MaVacXin",
                        column: x => x.MaVacXin,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan_QuyenHan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    MaQuyen = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan_QuyenHan", x => new { x.MaTaiKhoan, x.MaQuyen });
                    table.ForeignKey(
                        name: "FK_TaiKhoan_QuyenHan_QuyenHan_MaQuyen",
                        column: x => x.MaQuyen,
                        principalTable: "QuyenHan",
                        principalColumn: "MaQuyen",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_QuyenHan_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietGoiVacXin",
                columns: table => new
                {
                    MaChiTietGoi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaGoi = table.Column<int>(type: "int", nullable: false),
                    SoLuongGoi = table.Column<int>(type: "int", nullable: false),
                    GiaTaiThoiDiemDangKy = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietGoiVacXin", x => x.MaChiTietGoi);
                    table.CheckConstraint("CK_ChiTietGoiVacXin_GiaTaiThoiDiemDangKy", "[GiaTaiThoiDiemDangKy] >= 0");
                    table.CheckConstraint("CK_ChiTietGoiVacXin_SoLuongGoi", "[SoLuongGoi] > 0");
                    table.ForeignKey(
                        name: "FK_ChiTietGoiVacXin_DangKyDichVu_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DangKyDichVu",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiVacXin_GoiVacXin_MaGoi",
                        column: x => x.MaGoi,
                        principalTable: "GoiVacXin",
                        principalColumn: "MaGoi",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietVacXinLe",
                columns: table => new
                {
                    MaChiTietVacXin = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaVacXin = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    GiaTaiThoiDiemDangKy = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietVacXinLe", x => x.MaChiTietVacXin);
                    table.CheckConstraint("CK_ChiTietVacXinLe_GiaTaiThoiDiemDangKy", "[GiaTaiThoiDiemDangKy] >= 0");
                    table.CheckConstraint("CK_ChiTietVacXinLe_SoLuong", "[SoLuong] > 0");
                    table.ForeignKey(
                        name: "FK_ChiTietVacXinLe_DangKyDichVu_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DangKyDichVu",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietVacXinLe_VacXin_MaVacXin",
                        column: x => x.MaVacXin,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HangDoiVacXin",
                columns: table => new
                {
                    MaHangDoi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaVacXin = table.Column<int>(type: "int", nullable: false),
                    ThoiDiemVaoHang = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangDoiVacXin", x => x.MaHangDoi);
                    table.ForeignKey(
                        name: "FK_HangDoiVacXin_DangKyDichVu_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DangKyDichVu",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HangDoiVacXin_VacXin_MaVacXin",
                        column: x => x.MaVacXin,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuKhamSangLoc",
                columns: table => new
                {
                    MaPhieuKham = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDuocTiem = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    ChiSoSinhTon = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    TienSuBenhLy = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    KetLuan = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    LyDoHoan = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuKhamSangLoc", x => x.MaPhieuKham);
                    table.ForeignKey(
                        name: "FK_PhieuKhamSangLoc_DangKyDichVu_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DangKyDichVu",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKhamSangLoc_NguoiDuocTiem_MaNguoiDuocTiem",
                        column: x => x.MaNguoiDuocTiem,
                        principalTable: "NguoiDuocTiem",
                        principalColumn: "MaNguoiDuocTiem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKhamSangLoc_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThanhToan",
                columns: table => new
                {
                    MaThanhToan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoaiThanhToan = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhToan", x => x.MaThanhToan);
                    table.CheckConstraint("CK_ThanhToan_SoTien", "[SoTien] > 0");
                    table.ForeignKey(
                        name: "FK_ThanhToan_DangKyDichVu_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DangKyDichVu",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThanhToan_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhacLich",
                columns: table => new
                {
                    MaNhacLich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLichHen = table.Column<int>(type: "int", nullable: false),
                    KenhGui = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    ThoiDiemGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhacLich", x => x.MaNhacLich);
                    table.ForeignKey(
                        name: "FK_NhacLich_LichHenTiem_MaLichHen",
                        column: x => x.MaLichHen,
                        principalTable: "LichHenTiem",
                        principalColumn: "MaLichHen",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BienDongKho",
                columns: table => new
                {
                    MaBienDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoLo = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    LoaiBienDong = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BienDongKho", x => x.MaBienDong);
                    table.CheckConstraint("CK_BienDongKho_SoLuong", "[SoLuong] > 0");
                    table.ForeignKey(
                        name: "FK_BienDongKho_LoVacXin_SoLo",
                        column: x => x.SoLo,
                        principalTable: "LoVacXin",
                        principalColumn: "SoLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BienDongKho_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MuiTiem",
                columns: table => new
                {
                    MaMuiTiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiDuocTiem = table.Column<int>(type: "int", nullable: false),
                    SoLo = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    ThoiDiemTiem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViTriTiem = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuiTiem", x => x.MaMuiTiem);
                    table.ForeignKey(
                        name: "FK_MuiTiem_LoVacXin_SoLo",
                        column: x => x.SoLo,
                        principalTable: "LoVacXin",
                        principalColumn: "SoLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MuiTiem_NguoiDuocTiem_MaNguoiDuocTiem",
                        column: x => x.MaNguoiDuocTiem,
                        principalTable: "NguoiDuocTiem",
                        principalColumn: "MaNguoiDuocTiem",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MuiTiem_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiDinhTiem",
                columns: table => new
                {
                    MaChiDinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieuKham = table.Column<int>(type: "int", nullable: false),
                    MaVacXin = table.Column<int>(type: "int", nullable: false),
                    KetQuaChiDinh = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiDinhTiem", x => x.MaChiDinh);
                    table.ForeignKey(
                        name: "FK_ChiDinhTiem_PhieuKhamSangLoc_MaPhieuKham",
                        column: x => x.MaPhieuKham,
                        principalTable: "PhieuKhamSangLoc",
                        principalColumn: "MaPhieuKham",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiDinhTiem_VacXin_MaVacXin",
                        column: x => x.MaVacXin,
                        principalTable: "VacXin",
                        principalColumn: "MaVacXin",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TheoDoiSauTiem",
                columns: table => new
                {
                    MaTheoDoi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaMuiTiem = table.Column<int>(type: "int", nullable: false),
                    TinhTrang = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MucDoPhanUng = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    HuongXuTri = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheoDoiSauTiem", x => x.MaTheoDoi);
                    table.ForeignKey(
                        name: "FK_TheoDoiSauTiem_MuiTiem_MaMuiTiem",
                        column: x => x.MaMuiTiem,
                        principalTable: "MuiTiem",
                        principalColumn: "MaMuiTiem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BienDongKho_MaNhanVien",
                table: "BienDongKho",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_BienDongKho_SoLo",
                table: "BienDongKho",
                column: "SoLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiDinhTiem_MaPhieuKham",
                table: "ChiDinhTiem",
                column: "MaPhieuKham");

            migrationBuilder.CreateIndex(
                name: "IX_ChiDinhTiem_MaVacXin",
                table: "ChiDinhTiem",
                column: "MaVacXin");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiVacXin_MaDangKy",
                table: "ChiTietGoiVacXin",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiVacXin_MaGoi",
                table: "ChiTietGoiVacXin",
                column: "MaGoi");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietVacXinLe_MaDangKy",
                table: "ChiTietVacXinLe",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietVacXinLe_MaVacXin",
                table: "ChiTietVacXinLe",
                column: "MaVacXin");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyDichVu_MaKhachHang",
                table: "DangKyDichVu",
                column: "MaKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyDichVu_MaKhuyenMai",
                table: "DangKyDichVu",
                column: "MaKhuyenMai");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyDichVu_MaNguoiDuocTiem",
                table: "DangKyDichVu",
                column: "MaNguoiDuocTiem");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyDichVu_MaNhanVien",
                table: "DangKyDichVu",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_GoiVacXin_VacXin_MaVacXin",
                table: "GoiVacXin_VacXin",
                column: "MaVacXin");

            migrationBuilder.CreateIndex(
                name: "IX_HangDoiVacXin_MaDangKy",
                table: "HangDoiVacXin",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_HangDoiVacXin_MaVacXin",
                table: "HangDoiVacXin",
                column: "MaVacXin");

            migrationBuilder.CreateIndex(
                name: "IX_LichHenTiem_MaNguoiDuocTiem",
                table: "LichHenTiem",
                column: "MaNguoiDuocTiem");

            migrationBuilder.CreateIndex(
                name: "IX_LoVacXin_MaVacXin",
                table: "LoVacXin",
                column: "MaVacXin");

            migrationBuilder.CreateIndex(
                name: "IX_MuiTiem_MaNguoiDuocTiem",
                table: "MuiTiem",
                column: "MaNguoiDuocTiem");

            migrationBuilder.CreateIndex(
                name: "IX_MuiTiem_MaNhanVien",
                table: "MuiTiem",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_MuiTiem_SoLo",
                table: "MuiTiem",
                column: "SoLo");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDuocTiem_MaKhachHang",
                table: "NguoiDuocTiem",
                column: "MaKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_NhacLich_MaLichHen",
                table: "NhacLich",
                column: "MaLichHen");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhamSangLoc_MaDangKy",
                table: "PhieuKhamSangLoc",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhamSangLoc_MaNguoiDuocTiem",
                table: "PhieuKhamSangLoc",
                column: "MaNguoiDuocTiem");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhamSangLoc_MaNhanVien",
                table: "PhieuKhamSangLoc",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_MaNhanVien",
                table: "TaiKhoan",
                column: "MaNhanVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_QuyenHan_MaQuyen",
                table: "TaiKhoan_QuyenHan",
                column: "MaQuyen");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaDangKy",
                table: "ThanhToan",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_MaNhanVien",
                table: "ThanhToan",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_TheoDoiSauTiem_MaMuiTiem",
                table: "TheoDoiSauTiem",
                column: "MaMuiTiem",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacXin_MaLoaiTiemChung",
                table: "VacXin",
                column: "MaLoaiTiemChung");

            migrationBuilder.CreateIndex(
                name: "IX_VacXin_MaLoaiVacXin",
                table: "VacXin",
                column: "MaLoaiVacXin");

            migrationBuilder.CreateIndex(
                name: "IX_VacXin_Benh_MaBenh",
                table: "VacXin_Benh",
                column: "MaBenh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BienDongKho");

            migrationBuilder.DropTable(
                name: "ChiDinhTiem");

            migrationBuilder.DropTable(
                name: "ChiTietGoiVacXin");

            migrationBuilder.DropTable(
                name: "ChiTietVacXinLe");

            migrationBuilder.DropTable(
                name: "GoiVacXin_VacXin");

            migrationBuilder.DropTable(
                name: "HangDoiVacXin");

            migrationBuilder.DropTable(
                name: "NhacLich");

            migrationBuilder.DropTable(
                name: "TaiKhoan_QuyenHan");

            migrationBuilder.DropTable(
                name: "ThanhToan");

            migrationBuilder.DropTable(
                name: "TheoDoiSauTiem");

            migrationBuilder.DropTable(
                name: "VacXin_Benh");

            migrationBuilder.DropTable(
                name: "PhieuKhamSangLoc");

            migrationBuilder.DropTable(
                name: "GoiVacXin");

            migrationBuilder.DropTable(
                name: "LichHenTiem");

            migrationBuilder.DropTable(
                name: "QuyenHan");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "MuiTiem");

            migrationBuilder.DropTable(
                name: "Benh");

            migrationBuilder.DropTable(
                name: "DangKyDichVu");

            migrationBuilder.DropTable(
                name: "LoVacXin");

            migrationBuilder.DropTable(
                name: "KhuyenMai");

            migrationBuilder.DropTable(
                name: "NguoiDuocTiem");

            migrationBuilder.DropTable(
                name: "NhanVien");

            migrationBuilder.DropTable(
                name: "VacXin");

            migrationBuilder.DropTable(
                name: "KhachHang");

            migrationBuilder.DropTable(
                name: "LoaiTiemChung");

            migrationBuilder.DropTable(
                name: "LoaiVacXin");
        }
    }
}
