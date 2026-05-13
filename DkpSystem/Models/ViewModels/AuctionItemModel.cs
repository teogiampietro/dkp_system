namespace DkpSystem.Models.ViewModels;

public class AuctionItemModel
{
    public string Name { get; set; } = string.Empty;
    public int MinimumBid { get; set; } = 10;
    public string? ImageDataUrl { get; set; }
    public string? ImageMimeType { get; set; }
    public bool IsOcrPending { get; set; } = false;
    public bool IsOcrDetected { get; set; } = false;
}
