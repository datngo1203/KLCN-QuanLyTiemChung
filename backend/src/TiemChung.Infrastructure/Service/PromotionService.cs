using Microsoft.EntityFrameworkCore;
using TiemChung.Contracts;
using TiemChung.Infrastructure.Data;

namespace TiemChung.Infrastructure.Services
{
    public interface IPromotionService
    {
        Task<PromotionListResponse> GetAllAsync(CancellationToken cancellationToken);
        Task<PromotionListResponse> GetValidAsync(CancellationToken cancellationToken);
        Task<PromotionDto> CreateAsync(UpsertPromotionRequest request, CancellationToken cancellationToken);
    }

    public class PromotionService(TiemChungDbContext dbContext) : IPromotionService
    {
        public async Task<PromotionListResponse> GetAllAsync(CancellationToken cancellationToken)
        {
            var items = await dbContext.Promotions
                .OrderByDescending(p => p.ThoiGianBatDau)
                .Select(p => new PromotionDto(
                    p.MaKhuyenMai,
                    p.TenKhuyenMai,
                    p.NoiDung,
                    p.ThoiGianBatDau,
                    p.ThoiGianKetThuc,
                    p.MucUuDai,
                    true, // Trạng thái active giả định
                    false,
                    "VND"
                ))
                .ToListAsync(cancellationToken);

            return new PromotionListResponse(items);
        }

        public async Task<PromotionListResponse> GetValidAsync(CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var items = await dbContext.Promotions
                .Where(p => p.ThoiGianBatDau <= now && p.ThoiGianKetThuc >= now)
                .Select(p => new PromotionDto(
                    p.MaKhuyenMai,
                    p.TenKhuyenMai,
                    p.NoiDung,
                    p.ThoiGianBatDau,
                    p.ThoiGianKetThuc,
                    p.MucUuDai,
                    true,
                    false,
                    "VND"
                ))
                .ToListAsync(cancellationToken);

            return new PromotionListResponse(items);
        }

        public async Task<PromotionDto> CreateAsync(UpsertPromotionRequest request, CancellationToken cancellationToken)
        {
            // Validation ngày hiệu lực
            if (request.EndsAt <= request.StartsAt)
            {
                throw new ArgumentException("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            }

            var entity = new Domain.Entities.KhuyenMai
            {
                TenKhuyenMai = request.Name,
                NoiDung = request.Description,
                ThoiGianBatDau = request.StartsAt.UtcDateTime,
                ThoiGianKetThuc = request.EndsAt.UtcDateTime,
                MucUuDai = request.DiscountAmount
            };

            dbContext.Promotions.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new PromotionDto(
                entity.MaKhuyenMai,
                entity.TenKhuyenMai,
                entity.NoiDung,
                entity.ThoiGianBatDau,
                entity.ThoiGianKetThuc,
                entity.MucUuDai,
                true,
                false,
                "VND"
            );
        }
    }
}