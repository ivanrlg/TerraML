// File download helper for Blazor Server
// Triggers browser file download from text content or base64-encoded binary.
// When isBase64 is true, content is decoded from base64 to binary before download.
window.downloadFile = function (fileName, contentType, content, isBase64) {
    var blob;
    if (isBase64) {
        var binaryString = atob(content);
        var bytes = new Uint8Array(binaryString.length);
        for (var i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        blob = new Blob([bytes], { type: contentType });
    } else {
        blob = new Blob([content], { type: contentType });
    }
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
