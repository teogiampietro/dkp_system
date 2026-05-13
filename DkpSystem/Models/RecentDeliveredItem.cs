namespace DkpSystem.Models;

/// <summary>
/// Represents a recently delivered auction item with winner and auction info, used for dashboard display.
/// </summary>
public class RecentDeliveredItem
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid AuctionId { get; set; }
    public string AuctionName { get; set; } = string.Empty;
    public string WinnerName { get; set; } = string.Empty;
    public int DkpPaid { get; set; }
    public DateTime DeliveredAt { get; set; }
}
