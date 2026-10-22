namespace TiemChung.Desktop.Services;
// TODO: Promotion endpoints belong to the API owner; no write contract is available to this UI.
public interface IPromotionService { bool UseSampleData { get; set; } IReadOnlyList<PromotionRecord> GetPromotions(); bool IsAssigned(int id); }
public sealed record PromotionRecord(int Id,string Name,string Description,DateTime Starts,DateTime Ends,decimal Discount) { public string Status => Starts.Date<=DateTime.Today&&Ends.Date>=DateTime.Today ? "Đang hoạt động" : "Hết hiệu lực"; }
public sealed class SamplePromotionService:IPromotionService { public bool UseSampleData{get;set;}=true; public IReadOnlyList<PromotionRecord> GetPromotions()=>UseSampleData?[new(1,"Chương trình mẫu","Nội dung minh họa",DateTime.Today.AddDays(-3),DateTime.Today.AddDays(10),50000m),new(2,"Ưu đãi mẫu đã kết thúc","Nội dung minh họa",DateTime.Today.AddDays(-30),DateTime.Today.AddDays(-2),0m)]:[]; public bool IsAssigned(int id)=>id==1; }
