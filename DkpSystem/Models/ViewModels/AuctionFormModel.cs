namespace DkpSystem.Models.ViewModels;

public class AuctionFormModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ClosesAt { get; set; } = DateTime.UtcNow.AddHours(24);
    public List<AuctionItemModel> Items { get; set; } = new();
}
