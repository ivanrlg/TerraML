// Training Selection Tool — Canvas overlay for drawing rectangles on band preview
// Uses DotNetObjectReference to callback into Blazor when a rectangle is completed.

window.trainingSelection = {
    _canvas: null,
    _ctx: null,
    _img: null,
    _dotNetRef: null,
    _isDrawing: false,
    _startX: 0,
    _startY: 0,
    _currentX: 0,
    _currentY: 0,
    _regions: [],       // { x1, y1, x2, y2, color, label }
    _activeColor: '#3B82F6',
    _activeLabel: '',
    _scaleX: 1,
    _scaleY: 1,

    // Initialize the canvas overlay on top of the band preview image
    init: function (canvasId, imgId, dotNetRef, rasterCols, rasterRows) {
        this._canvas = document.getElementById(canvasId);
        this._img = document.getElementById(imgId);
        this._dotNetRef = dotNetRef;

        if (!this._canvas || !this._img) return;

        this._ctx = this._canvas.getContext('2d');

        // Match canvas size to image display size
        var rect = this._img.getBoundingClientRect();
        this._canvas.width = rect.width;
        this._canvas.height = rect.height;

        // Compute scale: display pixel → raster pixel
        this._scaleX = rasterCols / rect.width;
        this._scaleY = rasterRows / rect.height;

        // Bind mouse events
        this._canvas.addEventListener('mousedown', this._onMouseDown.bind(this));
        this._canvas.addEventListener('mousemove', this._onMouseMove.bind(this));
        this._canvas.addEventListener('mouseup', this._onMouseUp.bind(this));

        this._redraw();
    },

    // Update the active class for the next rectangle
    setActiveClass: function (className, color) {
        this._activeLabel = className;
        this._activeColor = color;
    },

    // Add a completed region (called from Blazor to restore state)
    addRegion: function (x1, y1, x2, y2, color, label) {
        this._regions.push({ x1, y1, x2, y2, color, label });
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
        this._scaleX = rasterCols / rect.width;
        this._scaleY = rasterRows / rect.height;
        this._redraw();
    },

    dispose: function () {
        this._regions = [];
        this._dotNetRef = null;
    },

    // --- Private event handlers ---

    _onMouseDown: function (e) {
        if (!this._activeLabel) return;
        this._isDrawing = true;
        var pos = this._getCanvasPos(e);
        this._startX = pos.x;
        this._startY = pos.y;
        this._currentX = pos.x;
        this._currentY = pos.y;
    },

    _onMouseMove: function (e) {
        if (!this._isDrawing) return;
        var pos = this._getCanvasPos(e);
        this._currentX = pos.x;
        this._currentY = pos.y;
        this._redraw();
        this._drawPreviewRect();
    },

    _onMouseUp: function (e) {
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
        return {
            x: Math.max(0, Math.min(e.clientX - rect.left, rect.width)),
            y: Math.max(0, Math.min(e.clientY - rect.top, rect.height))
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
