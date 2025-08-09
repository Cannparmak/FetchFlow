namespace FetchFlow.Database.Service.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string MkkMemberOid { get; set; } = string.Empty;
        public string KapMemberTitle { get; set; } = string.Empty;
        public string? RelatedMemberTitle { get; set; }
        public string? StockCode { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string? RelatedMemberOid { get; set; }
        public string KapMemberType { get; set; } = string.Empty;
    }
} 