namespace hoangstore.Models
{
    public interface IAuditable
    {
        string CreatedBy { get; set; }
        DateTime? CreatedDate { get; set; }
        string ModifiedBy { get; set; }
        DateTime? ModifiedDate { get; set; }
        string DeletedBy { get; set; }
        DateTime? DeletedDate { get; set; }
        bool IsDeleted { get; set; }
    }
}
