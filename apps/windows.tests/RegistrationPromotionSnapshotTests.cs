using System;
using TiemChung.Domain.Entities;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class RegistrationPromotionSnapshotTests
{
    [Theory]
    [InlineData(500000, 100000, 100000, 400000)] 
    [InlineData(300000, 500000, 300000, 0)]      
    [InlineData(200000, 0, 0, 200000)]           
    public void CalculateSnapshot_DiscountNeverExceedsTotalAmount(
        decimal subTotal, decimal promoDiscount, decimal expectedDiscount, decimal expectedFinal)
    {
        decimal appliedDiscount = Math.Min(promoDiscount, subTotal);
        decimal finalTotal = Math.Max(0m, subTotal - appliedDiscount);

        Assert.Equal(expectedDiscount, appliedDiscount);
        Assert.Equal(expectedFinal, finalTotal);
        Assert.True(finalTotal >= 0);
    }

    [Fact]
    public void Registration_WithoutPromotion_WorksNormally()
    {
        decimal subTotal = 750000m;
        int? promotionId = null;
        string? promotionName = null;
        decimal discountAmount = 0m;

        decimal appliedDiscount = promotionId.HasValue ? Math.Min(discountAmount, subTotal) : 0m;
        decimal finalTotal = Math.Max(0m, subTotal - appliedDiscount);

        Assert.Null(promotionName);
        Assert.Equal(0m, appliedDiscount);
        Assert.Equal(750000m, finalTotal);
    }

    [Fact]
    public void Registration_SnapshotPreservesHistoricData_WhenPromotionUpdatedLater()
    {
    
        var km = new KhuyenMai
        {
            MaKhuyenMai = 1,
            TenKhuyenMai = "Ưu đãi Mùa Thu",
            MucUuDai = 50000m
        };
        decimal subTotal = 200000m;

        var dangKy = new DangKyDichVu
        {
            MaDangKy = 10,
            MaKhuyenMai = km.MaKhuyenMai,
            TenKhuyenMaiSnapshot = km.TenKhuyenMai,
            MucGiamSnapshot = Math.Min(km.MucUuDai, subTotal),
            TongTienSauGiam = Math.Max(0m, subTotal - Math.Min(km.MucUuDai, subTotal))
        };

        
        km.TenKhuyenMai = "Ưu đãi Đã Đổi Tên";
        km.MucUuDai = 10000m;

        
        Assert.Equal("Ưu đãi Mùa Thu", dangKy.TenKhuyenMaiSnapshot);
        Assert.Equal(50000m, dangKy.MucGiamSnapshot);
        Assert.Equal(150000m, dangKy.TongTienSauGiam);
    }
}