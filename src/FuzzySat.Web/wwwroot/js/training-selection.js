// Training Selection Tool — Canvas overlay for drawing rectangles on band preview
// Uses DotNetObjectReference to callback into Blazor when a rectangle is completed.
// Supports zoom (scroll wheel) and pan (Ctrl+drag / middle-click drag).

window.trainingSelection = {
    _canvas: null,
    _ctx: null,
    _img: null,
    _container: null,
    _dotNetRef: null,
    _isDrawing: false,
    _isPanning: false,
    _startX: 0,
    _startY: 0,
    _currentX: 0,
    _currentY: 0,
    _regions: [],       // { x1, y1, x2, y2, color, label }
    _activeColor: '#3B82F6',
    _activeLabel: '',
    _scaleX: 1,
    _scaleY: 1,

    // Zoom / pan state
    _zoom: 1,
    _panX: 0,
    _panY: 0,
    _panStartX: 0,
    _panStartY: 0,
    _panOriginX: 0,
    _panOriginY: 0,
    _baseWidth: 0,
    _baseHeight: 0,

    // Bound handler references (for removeEventListener)
    _boundMouseDown: null,
    _boundMouseMove: null,
    _boundMouseUp: null,
    _boundWheel: null,
    _zoomNotifyTimer: null,

    // Initialize the canvas overlay on top of the band preview image
    init: function (canvasId, imgId, dotNetRef, rasterCols, rasterRows) {
        // Clean up previous listeners if re-initializing
        this._removeListeners();

        this._canvas = document.getElementById(canvasId);
        this._img = document.getElementById(imgId);
        this._dotNetRef = dotNetRef;

        if (!this._canvas || !this._img) return;

        this._ctx = this._canvas.getContext('2d');
        this._container = this._canvas.parentElement;

        // Match canvas size to image display size
        var rect = this._img.getBoundingClientRect();
        this._canvas.width = rect.width;
        this._canvas.height = rect.height;
        this._baseWidth = rect.width;
        this._baseHeight = rect.height;

        // Compute scale: display pixel → raster pixel
        this._scaleX = rasterCols / rect.width;
        this._scaleY = rasterRows / rect.height;

        // Store bound handlers so they can be removed later
        this._boundMouseDown = this._onMouseDown.bind(this);
        this._boundMouseMove = this._onMouseMove.bind(this);
        this._boundMouseUp = this._onMouseUp.bind(this);
        this._boundWheel = this._onWheel.bind(this);

        // Bind mouse events
        this._canvas.addEventListener('mousedown', this._boundMouseDown);
        this._canvas.addEventListener('mousemove', this._boundMouseMove);
        this._canvas.addEventListener('mouseup', this._boundMouseUp);

        // Zoom via scroll wheel on the container
        if (this._container) {
            this._container.addEventListener('wheel', this._boundWheel, { passive: false });
        }

        // Reset zoom/pan on new init
        this._zoom = 1;
        this._panX = 0;
        this._panY = 0;
        this._applyTransform();

        this._redraw();
    },

    // Update the active class for the next rectangle
    setActiveClass: function (className, color) {
        this._activeLabel = className;
        this._activeColor = color;
    },

    // Add a completed region from Blazor (raster pixel coordinates → display coordinates)
    addRegion: function (rasterCol1, rasterRow1, rasterCol2, rasterRow2, color, label) {
        var scaleX = this._scaleX || 1;
        var scaleY = this._scaleY || 1;
        this._regions.push({
            x1: rasterCol1 / scaleX,
            y1: rasterRow1 / scaleY,
            x2: rasterCol2 / scaleX,
            y2: rasterRow2 / scaleY,
            color: color,
            label: label
        });
        this._redraw();
    },

    // Remove a region by index
    removeRegion: function (index) {
        if (index >= 0 && index < this._regions.length) {
            this._regions.splice(index, 1);
            this._redraw();
        }
    },

    // Clear all regions
    clearAll: function () {
        this._regions = [];
        this._redraw();
    },

    // Resize canvas when image size changes
    resize: function (rasterCols, rasterRows) {
        if (!this._canvas || !this._img) return;
        var rect = this._img.getBoundingClientRect();
        this._canvas.width = rect.width;
        this._canvas.height = rect.height;
        this._baseWidth = rect.width;
        this._baseHeight = rect.height;
        this._scaleX = rasterCols / rect.width;
        this._scaleY = rasterRows / rect.height;
        this._redraw();
    },

    dispose: function () {
        // Remove all event listeners
        this._removeListeners();

        // Clear zoom notify timer
        if (this._zoomNotifyTimer) {
            clearTimeout(this._zoomNotifyTimer);
            this._zoomNotifyTimer = null;
        }

        // Reset CSS transforms
        if (this._img && this._img.style) {
            this._img.style.transform = '';
            this._img.style.transformOrigin = '';
        }
        if (this._canvas && this._canvas.style) {
            this._canvas.style.transform = '';
            this._canvas.style.transformOrigin = '';
            this._canvas.style.cursor = '';
        }

        // Clear state and DOM references
        this._regions = [];
        this._dotNetRef = null;
        this._zoom = 1;
        this._panX = 0;
        this._panY = 0;
        this._ctx = null;
        this._canvas = null;
        this._img = null;
        this._container = null;
    },

    _removeListeners: function () {
        if (this._canvas) {
            if (this._boundMouseDown) this._canvas.removeEventListener('mousedown', this._boundMouseDown);
            if (this._boundMouseMove) this._canvas.removeEventListener('mousemove', this._boundMouseMove);
            if (this._boundMouseUp) this._canvas.removeEventListener('mouseup', this._boundMouseUp);
        }
        if (this._container && this._boundWheel) {
            this._container.removeEventListener('wheel', this._boundWheel);
        }
        this._boundMouseDown = null;
        this._boundMouseMove = null;
        this._boundMouseUp = null;
        this._boundWheel = null;
    },

    // --- Zoom and Pan ---

    _onWheel: function (e) {
        e.preventDefault();

        var zoomFactor = e.deltaY < 0 ? 1.15 : 1 / 1.15;
        var newZoom = Math.max(0.5, Math.min(10, this._zoom * zoomFactor));

        // Zoom toward cursor position
        var rect = this._container.getBoundingClientRect();
        var mouseX = e.clientX - rect.left;
        var mouseY = e.clientY - rect.top;

        // Adjust pan to keep cursor position stable
        this._panX = mouseX - (mouseX - this._panX) * (newZoom / this._zoom);
        this._panY = mouseY - (mouseY - this._panY) * (newZoom / this._zoom);

        this._zoom = newZoom;
        this._clampPan();
        this._applyTransform();
        this._redraw();

        // Debounced Blazor notification (visual zoom is instant, badge update throttled)
        var self = this;
        if (this._zoomNotifyTimer) clearTimeout(this._zoomNotifyTimer);
        this._zoomNotifyTimer = setTimeout(function () {
            if (self._dotNetRef) {
                self._dotNetRef.invokeMethodAsync('OnZoomChanged', Math.round(self._zoom * 100));
            }
            self._zoomNotifyTimer = null;
        }, 100);
    },

    _applyTransform: function () {
        if (!this._canvas || !this._img) return;
        var transform = 'scale(' + this._zoom + ') translate(' +
            (this._panX / this._zoom) + 'px, ' + (this._panY / this._zoom) + 'px)';
        this._canvas.style.transform = transform;
        this._canvas.style.transformOrigin = '0 0';
        this._img.style.transform = transform;
        this._img.style.transformOrigin = '0 0';
    },

    _clampPan: function () {
        // Allow panning so at least 20% of image stays visible
        var maxPanX = this._baseWidth * this._zoom * 0.8;
        var maxPanY = this._baseHeight * this._zoom * 0.8;
        this._panX = Math.max(-maxPanX, Math.min(maxPanX, this._panX));
        this._panY = Math.max(-maxPanY, Math.min(maxPanY, this._panY));
    },

    resetZoom: function () {
        this._zoom = 1;
        this._panX = 0;
        this._panY = 0;
        this._applyTransform();
        this._redraw();
    },

    // --- Private event handlers ---

    _onMouseDown: function (e) {
        // Middle-click or Ctrl+left-click → pan
        if (e.button === 1 || (e.button === 0 && e.ctrlKey)) {
            e.preventDefault();
            this._isPanning = true;
            this._panStartX = e.clientX;
            this._panStartY = e.clientY;
            this._panOriginX = this._panX;
            this._panOriginY = this._panY;
            this._canvas.style.cursor = 'grabbing';
            return;
        }

        if (!this._activeLabel) return;
        this._isDrawing = true;
        var pos = this._getCanvasPos(e);
        this._startX = pos.x;
        this._startY = pos.y;
        this._currentX = pos.x;
        this._currentY = pos.y;
    },

    _onMouseMove: function (e) {
        if (this._isPanning) {
            var dx = e.clientX - this._panStartX;
            var dy = e.clientY - this._panStartY;
            this._panX = this._panOriginX + dx;
            this._panY = this._panOriginY + dy;
            this._clampPan();
            this._applyTransform();
            this._redraw();
            return;
        }
        if (!this._isDrawing) return;
        var pos = this._getCanvasPos(e);
        this._currentX = pos.x;
        this._currentY = pos.y;
        this._redraw();
        this._drawPreviewRect();
    },

    _onMouseUp: function (e) {
        if (this._isPanning) {
            this._isPanning = false;
            this._canvas.style.cursor = 'crosshair';
            return;
        }
        if (!this._isDrawing) return;
        this._isDrawing = false;

        var pos = this._getCanvasPos(e);
        var x1 = Math.min(this._startX, pos.x);
        var y1 = Math.min(this._startY, pos.y);
        var x2 = Math.max(this._startX, pos.x);
        var y2 = Math.max(this._startY, pos.y);

        // Minimum 3px to avoid accidental clicks
        if (Math.abs(x2 - x1) < 3 || Math.abs(y2 - y1) < 3) {
            this._redraw();
            return;
        }

        // Convert display coordinates to raster pixel coordinates
        var startCol = Math.floor(x1 * this._scaleX);
        var startRow = Math.floor(y1 * this._scaleY);
        var endCol = Math.floor(x2 * this._scaleX);
        var endRow = Math.floor(y2 * this._scaleY);

        // Store for visual display
        this._regions.push({
            x1: x1, y1: y1, x2: x2, y2: y2,
            color: this._activeColor,
            label: this._activeLabel
        });

        this._redraw();

        // Callback to Blazor with raster coordinates
        if (this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('OnRegionSelected',
                this._activeLabel, startRow, startCol, endRow, endCol);
        }
    },

    _getCanvasPos: function (e) {
        var rect = this._canvas.getBoundingClientRect();
        // getBoundingClientRect accounts for CSS transform (scale),
        // so rect.width = baseWidth * zoom. We need unscaled coordinates.
        var x = (e.clientX - rect.left) / this._zoom;
        var y = (e.clientY - rect.top) / this._zoom;
        return {
            x: Math.max(0, Math.min(x, this._baseWidth)),
            y: Math.max(0, Math.min(y, this._baseHeight))
        };
    },

    _drawPreviewRect: function () {
        var ctx = this._ctx;
        var x = Math.min(this._startX, this._currentX);
        var y = Math.min(this._startY, this._currentY);
        var w = Math.abs(this._currentX - this._startX);
        var h = Math.abs(this._currentY - this._startY);

        ctx.strokeStyle = this._activeColor;
        ctx.lineWidth = 2;
        ctx.setLineDash([6, 3]);
        ctx.strokeRect(x, y, w, h);
        ctx.setLineDash([]);

        // Semi-transparent fill
        ctx.fillStyle = this._activeColor + '30';
        ctx.fillRect(x, y, w, h);
    },

    _redraw: function () {
        if (!this._ctx || !this._canvas) return;
        var ctx = this._ctx;
        ctx.clearRect(0, 0, this._canvas.width, this._canvas.height);

        // Draw all saved regions
        for (var i = 0; i < this._regions.length; i++) {
            var r = this._regions[i];
            var x = r.x1, y = r.y1;
            var w = r.x2 - r.x1, h = r.y2 - r.y1;

            // Fill
            ctx.fillStyle = r.color + '25';
            ctx.fillRect(x, y, w, h);

            // Border
            ctx.strokeStyle = r.color;
            ctx.lineWidth = 2;
            ctx.setLineDash([]);
            ctx.strokeRect(x, y, w, h);

            // Label
            ctx.font = '11px system-ui, sans-serif';
            ctx.fillStyle = '#fff';
            var textW = ctx.measureText(r.label).width;
            ctx.fillStyle = r.color + 'CC';
            ctx.fillRect(x, y - 16, textW + 8, 16);
            ctx.fillStyle = '#fff';
            ctx.fillText(r.label, x + 4, y - 4);
        }
    }
};
