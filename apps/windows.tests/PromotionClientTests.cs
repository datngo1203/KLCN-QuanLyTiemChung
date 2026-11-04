using System;
using TiemChung.Desktop.Services;
using TiemChung.Desktop.ViewModels;
using Xunit;

namespace TiemChung.Windows.Tests;

public sealed class PromotionClientTests
{
    [Fact]
    public void InitialState_LoadsSampleData_WhenNotUsingApi()
    {
        var vm = new PromotionViewModel(useApi: false);

        Assert.NotEmpty(vm.Promotions);
        Assert.False(vm.HasNoData);
        Assert.Equal("Dữ liệu mẫu", vm.DataSourceLabel);
    }

    [Fact]
    public void Validation_Fails_WhenEndDateIsBeforeStartDate()
    {
        var vm = new PromotionViewModel(useApi: false)
        {
            Name = "Khuyen Mai Mua Thu",
            Description = "Giam gia tiem chung",
            Start = DateTime.Today.AddDays(5),
            End = DateTime.Today,
            Amount = "50000"
        };

        Assert.False(vm.CanSave);
        Assert.Equal("Thời điểm kết thúc phải sau thời điểm bắt đầu.", vm.Validation);
    }

    [Fact]
    public void Validation_Fails_WhenDiscountAmountIsNegative()
    {
        var vm = new PromotionViewModel(useApi: false)
        {
            Name = "Khuyen Mai Mua Thu",
            Description = "Giam gia tiem chung",
            Start = DateTime.Today,
            End = DateTime.Today.AddDays(5),
            Amount = "-50000"
        };

        Assert.False(vm.CanSave);
        Assert.Equal("Mức ưu đãi không được âm.", vm.Validation);
    }

    [Fact]
    public void Validation_Fails_WhenDiscountHasDecimals()
    {
        var vm = new PromotionViewModel(useApi: false)
        {
            Name = "Khuyen Mai Mua Thu",
            Description = "Giam gia tiem chung",
            Start = DateTime.Today,
            End = DateTime.Today.AddDays(5),
            Amount = "50000.5"
        };

        Assert.False(vm.CanSave);
    }

    [Fact]
    public void CanDelete_ReturnsFalse_WhenPromotionIsAssigned()
    {
        var vm = new PromotionViewModel(useApi: false);
        vm.Selected = vm.Promotions[0];

        Assert.True(vm.IsAssigned);
        Assert.False(vm.CanDelete);
    }

    [Fact]
    public void CanDelete_ReturnsTrue_WhenPromotionIsNotAssigned()
    {
        var vm = new PromotionViewModel(useApi: false);
        if (vm.Promotions.Count > 1)
        {
            vm.Selected = vm.Promotions[1];
            Assert.False(vm.IsAssigned);
            Assert.True(vm.CanDelete);
        }
    }
}