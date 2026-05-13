using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using DkpSystem.Data.Repositories;
using DkpSystem.Models.ViewModels;
using DkpSystem.Services;

namespace DkpSystem.Components.Pages.Admin.Auctions;

public partial class AuctionForm : ComponentBase, IDisposable
{
    [Inject] private IAuctionService AuctionService { get; set; } = default!;
    [Inject] private IImageStorageService ImageStorageService { get; set; } = default!;
    [Inject] private UserRepository UserRepository { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [SupplyParameterFromQuery]
    public Guid? FromAuctionId { get; set; }

    private AuctionFormModel _model = new();
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private string _submitStatus = string.Empty;
    private bool _isSubmitting = false;
    private int _preloadedItemCount = 0;
    private DotNetObjectReference<AuctionForm>? _dotNetRef;
    private int _browserUtcOffsetMinutes = 0;

    protected override async Task OnInitializedAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);

        if (FromAuctionId.HasValue)
        {
            var skippedItems = (await AuctionService.GetUnresolvedItemsAsync(FromAuctionId.Value)).ToList();
            foreach (var item in skippedItems)
            {
                _model.Items.Add(new AuctionItemModel { Name = item.Name, MinimumBid = item.MinimumBid, ImageDataUrl = item.ImageUrl });
            }
            _preloadedItemCount = skippedItems.Count;
        }

        if (_model.Items.Count == 0)
        {
            _model.Items.Add(new AuctionItemModel { Name = "Item 1" });
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await InitPasteZonesAsync();

        if (firstRender)
        {
            _browserUtcOffsetMinutes = await JSRuntime.InvokeAsync<int>("getBrowserUtcOffsetMinutes");
            _model.ClosesAt = DateTime.UtcNow.AddHours(24).AddMinutes(-_browserUtcOffsetMinutes);
            StateHasChanged();
        }
    }

    private async Task InitPasteZonesAsync()
    {
        for (int i = 0; i < _model.Items.Count; i++)
        {
            if (_model.Items[i].ImageDataUrl == null)
            {
                await JSRuntime.InvokeVoidAsync("initAuctionPasteZone", $"paste-zone-{i}", _dotNetRef, i);
            }
        }
    }

    [JSInvokable]
    public void OnImagePasted(int itemIndex, string dataUrl, string mimeType, string suggestedName)
    {
        if (itemIndex < 0 || itemIndex >= _model.Items.Count) return;
        var item = _model.Items[itemIndex];
        item.ImageDataUrl = dataUrl;
        item.ImageMimeType = mimeType;
        item.IsOcrPending = true;
        item.IsOcrDetected = false;
        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnOcrCompleted(int itemIndex, string suggestedName)
    {
        if (itemIndex < 0 || itemIndex >= _model.Items.Count) return;
        var item = _model.Items[itemIndex];
        item.IsOcrPending = false;

        if (!string.IsNullOrWhiteSpace(suggestedName) &&
            System.Text.RegularExpressions.Regex.IsMatch(item.Name, @"^Item \d+$"))
        {
            item.Name = suggestedName;
            item.IsOcrDetected = true;
        }

        InvokeAsync(StateHasChanged);
    }

    private void AddItem()
    {
        var nextNumber = _model.Items.Count + 1;
        _model.Items.Add(new AuctionItemModel { Name = $"Item {nextNumber}" });
    }

    private void RemoveItem(int index)
    {
        if (_model.Items.Count > 1)
        {
            _model.Items.RemoveAt(index);
        }
        else
        {
            _errorMessage = "At least one item is required.";
        }
    }

    private void ClearImage(int index)
    {
        _model.Items[index].ImageDataUrl = null;
        _model.Items[index].ImageMimeType = null;
        _model.Items[index].IsOcrDetected = false;
        _model.Items[index].IsOcrPending = false;
    }

    private async Task HandleSubmit()
    {
        _errorMessage = string.Empty;
        _successMessage = string.Empty;
        _isSubmitting = true;

        try
        {
            if (string.IsNullOrWhiteSpace(_model.Name))
            {
                _errorMessage = "Auction name is required.";
                return;
            }

            if (_model.ClosesAt.AddMinutes(_browserUtcOffsetMinutes) <= DateTime.UtcNow)
            {
                _errorMessage = "Closing date must be in the future.";
                return;
            }

            if (_model.Items.Count == 0)
            {
                _errorMessage = "At least one item is required.";
                return;
            }

            foreach (var item in _model.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    _errorMessage = "All items must have a name.";
                    return;
                }

                if (item.MinimumBid <= 0)
                {
                    _errorMessage = "All items must have a minimum bid greater than zero.";
                    return;
                }
            }

            var authState = await AuthenticationStateTask!;
            var userIdClaim = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _errorMessage = "Unable to identify current user.";
                return;
            }

            var user = await UserRepository.FindByIdAsync(userId);
            if (user == null || user.GuildId == null)
            {
                _errorMessage = "You must be assigned to a guild to create auctions.";
                return;
            }

            // Upload images that were pasted
            var itemsWithUrls = new List<(string Name, int MinimumBid, string? ImageUrl)>();
            for (int i = 0; i < _model.Items.Count; i++)
            {
                var item = _model.Items[i];
                string? imageUrl = null;

                if (item.ImageDataUrl != null)
                {
                    // Check if it's already a regular URL (from preloaded items) or a data URL (from paste/upload)
                    if (item.ImageDataUrl.StartsWith("http://") || item.ImageDataUrl.StartsWith("https://"))
                    {
                        // Already a valid URL from a previous auction, use it directly
                        imageUrl = item.ImageDataUrl;
                    }
                    else if (item.ImageDataUrl.StartsWith("data:") && item.ImageMimeType != null)
                    {
                        // Data URL from paste/upload, need to upload to storage
                        _submitStatus = $"Uploading image {i + 1}/{_model.Items.Count}...";
                        StateHasChanged();

                        var base64 = item.ImageDataUrl[(item.ImageDataUrl.IndexOf(',') + 1)..];
                        var imageBytes = Convert.FromBase64String(base64);
                        imageUrl = await ImageStorageService.UploadImageAsync(imageBytes, item.ImageMimeType);
                    }
                }

                itemsWithUrls.Add((item.Name, item.MinimumBid, imageUrl));
            }

            _submitStatus = "Creating auction...";
            StateHasChanged();

            var closesAtUtc = DateTime.SpecifyKind(_model.ClosesAt.AddMinutes(_browserUtcOffsetMinutes), DateTimeKind.Utc);
            var result = await AuctionService.CreateAuctionAsync(
                user.GuildId.Value,
                _model.Name,
                closesAtUtc,
                userId,
                itemsWithUrls,
                _model.Description
            );

            if (result.Success && result.Auction != null)
            {
                _successMessage = "Auction created successfully! Redirecting...";
                await Task.Delay(1000);
                Navigation.NavigateTo($"/auctions/{result.Auction.Id}");
            }
            else
            {
                _errorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
            _submitStatus = string.Empty;
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/auctions");
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
}
