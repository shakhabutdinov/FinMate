namespace aspnet.Models;

public class AssetSnapshot
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public DateTime Date { get; set; }
    public decimal Balance { get; set; }

    public Asset Asset { get; set; } = null!;
}
