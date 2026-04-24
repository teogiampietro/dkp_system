window.initAuctionPasteZone = function (zoneId, dotNetHelper, itemIndex) {
    const zone = document.getElementById(zoneId);
    if (!zone) return;

    // Remove any previously attached handler to avoid duplicates
    if (zone._pasteHandler) {
        zone.removeEventListener('paste', zone._pasteHandler);
    }

    zone._pasteHandler = function (e) {
        const items = e.clipboardData && e.clipboardData.items;
        if (!items) return;

        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            if (item.type.startsWith('image/')) {
                e.preventDefault();
                const blob = item.getAsFile();
                const reader = new FileReader();
                reader.onload = function (evt) {
                    dotNetHelper.invokeMethodAsync('OnImagePasted', itemIndex, evt.target.result, item.type);
                };
                reader.readAsDataURL(blob);
                break;
            }
        }
    };

    zone.addEventListener('paste', zone._pasteHandler);
};
