window.initAuctionPasteZone = function (zoneId, dotNetHelper, itemIndex) {
    const zone = document.getElementById(zoneId);
    if (!zone) return;

    if (zone._pasteHandler) {
        zone.removeEventListener('paste', zone._pasteHandler);
    }

    zone._pasteHandler = async function (e) {
        const items = e.clipboardData && e.clipboardData.items;
        if (!items) return;

        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            if (item.type.startsWith('image/')) {
                e.preventDefault();
                const blob = item.getAsFile();
                const reader = new FileReader();

                reader.onload = async function (evt) {
                    const dataUrl = evt.target.result;
                    const mimeType = item.type;

                    // Notify Blazor immediately so the image preview appears without waiting for OCR
                    await dotNetHelper.invokeMethodAsync('OnImagePasted', itemIndex, dataUrl, mimeType, '');

                    // OCR runs asynchronously — always calls OnOcrCompleted to clear the spinner
                    try {
                        const suggestedName = await extractItemNameFromDataUrl(dataUrl);
                        await dotNetHelper.invokeMethodAsync('OnOcrCompleted', itemIndex, suggestedName || '');
                    } catch (err) {
                        console.warn('[auction.js] OCR failed:', err);
                        await dotNetHelper.invokeMethodAsync('OnOcrCompleted', itemIndex, '');
                    }
                };

                reader.readAsDataURL(blob);
                break;
            }
        }
    };

    zone.addEventListener('paste', zone._pasteHandler);
};

async function extractItemNameFromDataUrl(dataUrl) {
    if (typeof Tesseract === 'undefined') return '';

    const img = await loadImage(dataUrl);

    // Crop top 25% starting from y=0. Item names appear anywhere in this range
    // depending on whether the screenshot includes an icon above the name or not.
    const cropHeight = Math.max(Math.floor(img.height * 0.25), 80);

    // Scale up 2x so Tesseract gets larger text — significantly improves accuracy
    const scale = 2;
    const canvas = document.createElement('canvas');
    canvas.width = img.width * scale;
    canvas.height = cropHeight * scale;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(img, 0, 0, img.width, cropHeight, 0, 0, canvas.width, canvas.height);

    preprocessCanvasForOcr(ctx, canvas.width, canvas.height);

    const { data } = await Tesseract.recognize(canvas, 'eng', {
        tessedit_pageseg_mode: '6',
        logger: () => {}
    });

    // Item names never contain a colon (unlike all stat lines: "Base Score: 128").
    // Find the first line that has letters and no colon — that's the item name.
    const lines = (data.text || '').split('\n').map(l => l.trim()).filter(l => l.length > 2);
    const nameLine = lines.find(l => /[a-zA-Z]/.test(l) && !l.includes(':')) || '';

    if (nameLine.length < 3) return '';

    return nameLine.replace(/^[^a-zA-Z]+|[^a-zA-Z0-9\s]+$/g, '').trim();
}

// Converts to grayscale and binarizes: colored text on dark background becomes
// black-on-white (what Tesseract reads best). Dark bg (~gray 20) → white;
// colored text (~gray 100-180) → black.
function preprocessCanvasForOcr(ctx, width, height) {
    const imageData = ctx.getImageData(0, 0, width, height);
    const d = imageData.data;
    const threshold = 80;

    for (let i = 0; i < d.length; i += 4) {
        const gray = 0.299 * d[i] + 0.587 * d[i + 1] + 0.114 * d[i + 2];
        const val = gray > threshold ? 0 : 255;
        d[i] = d[i + 1] = d[i + 2] = val;
    }
    ctx.putImageData(imageData, 0, 0);
}

function loadImage(dataUrl) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = reject;
        img.src = dataUrl;
    });
}
