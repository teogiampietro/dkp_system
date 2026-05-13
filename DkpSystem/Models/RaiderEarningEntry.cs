namespace DkpSystem.Models;

/// <summary>
/// Raider earning entry for raider view.
/// </summary>
public class RaiderEarningEntry
{
    /// <summary>
    /// Gets or sets the reason for the earning.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DKP amount earned.
    /// </summary>
    public int DkpAmount { get; set; }

    /// <summary>
    /// Gets or sets the date earned.
    /// </summary>
    public DateTime EarnedAt { get; set; }
}
