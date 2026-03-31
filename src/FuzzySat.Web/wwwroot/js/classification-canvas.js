// Classification Canvas — Side-by-side viewer for original vs classified images.
// Supports synced zoom/pan, overlay toggle, hover tooltip with class + confidence.

window.classificationCanvas = {
    _canvasLeft: null,
    _canvasRight: null,
    _ctxLeft: null,
    _ctxRight: null,
    _imgOriginal: null,
    _imgClassified: null,
    _container: null,
    _dotNetRef: null,
    _tooltip: null,

    // View state (shared between both canvases)
    _zoom: 1,
    _panX: 0,
    _panY: 0,
    _isPanning: false,
    _panStartX: 0,
    _panStartY: 0,
    _panOriginX: 0,
    _panOriginY: 0,
    _baseWidth: 0,
    _baseHeight: 0,

    // Image dimensions (raster coords)
    _rasterCols: 0,
    _rasterRows: 0,

    // View mode: "side-by-side", "original", "classified", "overlay"
    _viewMode: 'side-by-side',

    // Bound event handlers
    _boundMouseDown: null,
    _boundMouseMove: null,
    _boundMouseUp: null,
    _boundWheel: null,

    init: function (containerId, dotNetRef, rasterCols, rasterRows) {
        this._removeListeners();

        this._container = document.getElementById(containerId);
        if (!this._container) return;
        this._dotNetRef = dotNetRef;
        this._rasterCols = rasterCols;
        this._rasterRows = rasterRows;

        this._canvasLeft = this._container.querySelector('.cv-canvas-left');
        this._canvasRight = this._container.querySelector('.cv-canvas-right');
        this._tooltip = this._container.querySelector('.cv-tooltip');

        if (!this._canvasLeft || !this._canvasRight) return;

        this._ctxLeft = this._canvasLeft.getContext('2d');
        this._ctxRight = this._canvasRight.getContext('2d');

        this._zoom = 1;
        this._panX = 0;
        this._panY = 0;

        this._boundMouseDown = this._onMouseDown.bind(this);
        this._boundMouseMove = this._onMouseMove.bind(this);
        this._boundMouseUp = this._onMouseUp.bind(this);
        this._boundWheel = this._onWheel.bind(this);

        this._container.addEventListener('mousedown', this._boundMouseDown);
        this._container.addEventListener('mousemove', this._boundMouseMove);
        this._container.addEventListener('mouseup', this._boundMouseUp);
        this._container.addEventListener('mouseleave', this._boundMouseUp);
        this._container.addEventListener('wheel', this._boundWheel, { passive: false });
    },

    setOriginalImage: function (base64Png) {
        this._imgOriginal = new Image();
        this._imgOriginal.onload = () => {
            this._baseWidth = this._imgOriginal.width;
            this._baseHeight = this._imgOriginal.height;
            this._resizeCanvases();
            this._redraw();
        };
        this._imgOriginal.src = 'data:image/png;base64,' + base64Png;
    },

    setClassifiedImage: function (base64Png) {
        this._imgClassified = new Image();
        this._imgClassified.onload = () => {
            this._resizeCanvases();
            this._redraw();
        };
        this._imgClassified.src = 'data:image/png;base64,' + base64Png;
    },

    setViewMode: function (mode) {
        this._viewMode = mode;
        this._updateLayout();
        this._redraw();
    },

    resetView: function () {
        this._zoom = 1;
        this._panX = 0;
        this._panY = 0;
        this._redraw();
    },

    zoomIn: function () {
        this._zoom = Math.min(this._zoom * 1.3, 20);
        this._redraw();
    },

    zoomOut: function () {
        this._zoom = Math.max(this._zoom / 1.3, 0.1);
        this._redraw();
    },

    _resizeCanvases: function () {
        if (!this._canvasLeft) return;
        var containerRect = this._container.getBoundingClientRect();
        var w, h;

        if (this._viewMode === 'side-by-side') {
            w = Math.floor((containerRect.width - 8) / 2); // 8px gap
        } else {
            w = Math.floor(containerRect.width);
        }

        // Maintain aspect ratio
        var aspect = this._baseWidth > 0 ? this._baseHeight / this._baseWidth : 0.75;
        h = Math.min(Math.floor(w * aspect), 500);

        this._canvasLeft.width = w;
        this._canvasLeft.height = h;
        this._canvasRight.width = w;
        this._canvasRight.height = h;
    },

    _updateLayout: function () {
        if (!this._canvasLeft || !this._canvasRight) return;
        var leftWrapper = this._canvasLeft.parentElement;
        var rightWrapper = this._canvasRight.parentElement;

        if (this._viewMode === 'side-by-side') {
            leftWrapper.style.display = 'block';
            rightWrapper.style.display = 'block';
        } else if (this._viewMode === 'original') {
            leftWrapper.style.display = 'block';
            rightWrapper.style.display = 'none';
        } else if (this._viewMode === 'classified') {
            leftWrapper.style.display = 'none';
            rightWrapper.style.display = 'block';
        } else if (this._viewMode === 'overlay') {
            leftWrapper.style.display = 'block';
            rightWrapper.style.display = 'none';
        }
        this._resizeCanvases();
    },

    _redraw: function () {
        this._drawCanvas(this._ctxLeft, this._canvasLeft,
            this._viewMode === 'overlay' ? null : this._imgOriginal,
            this._viewMode === 'overlay' ? true : false);
        this._drawCanvas(this._ctxRight, this._canvasRight, this._imgClassified, false);
    },

    _drawCanvas: function (ctx, canvas, img, isOverlay) {
        if (!ctx || !canvas) return;
        var w = canvas.width;
        var h = canvas.height;

        ctx.clearRect(0, 0, w, h);
        ctx.fillStyle = '#0D1117';
        ctx.fillRect(0, 0, w, h);

        if (isOverlay && this._imgOriginal && this._imgClassified) {
            // Draw original
            ctx.save();
            ctx.translate(this._panX, this._panY);
            ctx.scale(this._zoom, this._zoom);
            ctx.drawImage(this._imgOriginal, 0, 0, w / this._zoom, h / this._zoom);
            ctx.restore();

            // Draw classified with 50% alpha on top
            ctx.save();
            ctx.globalAlpha = 0.5;
            ctx.translate(this._panX, this._panY);
            ctx.scale(this._zoom, this._zoom);
            ctx.drawImage(this._imgClassified, 0, 0, w / this._zoom, h / this._zoom);
            ctx.restore();
            return;
        }

        if (!img) return;

        ctx.save();
        ctx.translate(this._panX, this._panY);
        ctx.scale(this._zoom, this._zoom);
        ctx.drawImage(img, 0, 0, w / this._zoom, h / this._zoom);
        ctx.restore();
    },

    _onMouseDown: function (e) {
        if (e.ctrlKey || e.button === 1) {
            this._isPanning = true;
            this._panStartX = e.clientX;
            this._panStartY = e.clientY;
            this._panOriginX = this._panX;
            this._panOriginY = this._panY;
            e.preventDefault();
        }
    },

    _onMouseMove: function (e) {
        if (this._isPanning) {
            this._panX = this._panOriginX + (e.clientX - this._panStartX);
            this._panY = this._panOriginY + (e.clientY - this._panStartY);
            this._redraw();
            return;
        }

        // Tooltip: show raster coords + class info on hover
        if (this._tooltip && this._dotNetRef && this._rasterCols > 0) {
            var rect = (e.target === this._canvasRight ? this._canvasRight : this._canvasLeft)
                .getBoundingClientRect();
            var canvasX = e.clientX - rect.left;
            var canvasY = e.clientY - rect.top;
            var imgX = (canvasX - this._panX) / this._zoom;
            var imgY = (canvasY - this._panY) / this._zoom;
            var scaleX = this._rasterCols / (rect.width / this._zoom);
            var scaleY = this._rasterRows / (rect.height / this._zoom);
            var rasterCol = Math.floor(imgX * scaleX);
            var rasterRow = Math.floor(imgY * scaleY);

            if (rasterCol >= 0 && rasterCol < this._rasterCols &&
                rasterRow >= 0 && rasterRow < this._rasterRows) {
                this._tooltip.style.display = 'block';
                this._tooltip.style.left = (e.clientX - this._container.getBoundingClientRect().left + 12) + 'px';
                this._tooltip.style.top = (e.clientY - this._container.getBoundingClientRect().top - 8) + 'px';
                // Ask Blazor for class info at this pixel
                this._dotNetRef.invokeMethodAsync('GetPixelInfo', rasterRow, rasterCol)
                    .then(info => {
                        if (info) this._tooltip.innerHTML = info;
                    });
            } else {
                this._tooltip.style.display = 'none';
            }
        }
    },

    _onMouseUp: function () {
        this._isPanning = false;
        if (this._tooltip) this._tooltip.style.display = 'none';
    },

    _onWheel: function (e) {
        e.preventDefault();
        var factor = e.deltaY < 0 ? 1.15 : 1 / 1.15;
        var newZoom = Math.max(0.1, Math.min(this._zoom * factor, 20));

        // Zoom toward cursor position
        var rect = this._container.getBoundingClientRect();
        var mx = e.clientX - rect.left;
        var my = e.clientY - rect.top;

        this._panX = mx - (mx - this._panX) * (newZoom / this._zoom);
        this._panY = my - (my - this._panY) * (newZoom / this._zoom);
        this._zoom = newZoom;

        this._redraw();
    },

    _removeListeners: function () {
        if (this._container && this._boundMouseDown) {
            this._container.removeEventListener('mousedown', this._boundMouseDown);
            this._container.removeEventListener('mousemove', this._boundMouseMove);
            this._container.removeEventListener('mouseup', this._boundMouseUp);
            this._container.removeEventListener('mouseleave', this._boundMouseUp);
            this._container.removeEventListener('wheel', this._boundWheel);
        }
        this._boundMouseDown = null;
        this._boundMouseMove = null;
        this._boundMouseUp = null;
        this._boundWheel = null;
    },

    dispose: function () {
        this._removeListeners();
        this._canvasLeft = null;
        this._canvasRight = null;
        this._ctxLeft = null;
        this._ctxRight = null;
        this._imgOriginal = null;
        this._imgClassified = null;
        this._dotNetRef = null;
        this._tooltip = null;
    }
};
