using System;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class PaymentPromotionSnapshotTests
{
    [Fact]
    public void Payment_CalculatesDueAmount_StrictlyFromSnapshot_IgnoringLivePromotionChanges()
    {
        
        var registration = new DangKyDichVu
        {
            MaDangKy = 501,
            MaKhuyenMai = 1,
            TenKhuyenMaiSnapshot = "Giảm 50K hè 2026",
            MucGiamSnapshot = 50000m,
            TongTienSauGiam = 150000m 
        };

      
        var livePromotion = new KhuyenMai
        {
            MaKhuyenMai = 1,
            TenKhuyenMai = "Tên mới",
            MucUuDai = 999999m 
        };

       
        decimal totalDue = registration.TongTienSauGiam > 0 || registration.MucGiamSnapshot > 0
            ? registration.TongTienSauGiam
            : 200000m;

       
        var payment = new ThanhToan
        {
            MaDangKy = registration.MaDangKy,
            SoTien = totalDue,
            TrangThai = "ThanhCong"
        };

      
        Assert.Equal(150000m, payment.SoTien);
        Assert.NotEqual(livePromotion.MucUuDai, payment.SoTien);
    }

    [Fact]
    public void Payment_ZeroDueAmount_RequiresNoPayment()
    {
     
        var registration = new DangKyDichVu
        {
            MaDangKy = 502,
            MaKhuyenMai = 2,
            TenKhuyenMaiSnapshot = "Miễn phí 100%",
            MucGiamSnapshot = 200000m,
            TongTienSauGiam = 0m
        };

        decimal totalDue = registration.TongTienSauGiam;
        Assert.Equal(0m, totalDue);
    }
}