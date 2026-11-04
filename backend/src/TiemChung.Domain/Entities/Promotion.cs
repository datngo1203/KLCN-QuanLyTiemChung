namespace TiemChung.Domain.Entities
{
    public class Promotion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;       
        public string Name { get; set; } = string.Empty;       
        public decimal? DiscountPercent { get; set; }          
        public decimal? DiscountAmount { get; set; }           
        public decimal MinOrderValue { get; set; }            
        public DateTime StartDate { get; set; }               
        public DateTime EndDate { get; set; }                  
        public bool IsActive { get; set; } = true;             
    }
}