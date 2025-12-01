using Godot;

public partial class PainterImage : Sprite2D {
    [Export] public Color paint_color = Colors.Red;
    [Export] public Vector2I img_size = new Vector2I(128, 170);
    [Export] public int brush_size = 3;
    [Export] public NodePath currentColorRectPath;
    [Export] public NodePath canvasMeshPath;
    [Export] public int pixelGridSize = 8;
    private Image img;
    private ColorRect currentColorRect;
    private MeshInstance3D canvasMesh;
    private StandardMaterial3D canvasMaterial;
    private bool canvasMeshInitialized = false;
    private bool pixelMode = false;
    private Vector2I lastPixelGrid = new Vector2I(-1, -1);
    private Color _paintColor = Colors.Red;

    [Export]
    public Color PaintColor {
        get => _paintColor;
        set {
            _paintColor = value;
            if (currentColorRect != null)
                currentColorRect.Color = value;
        }
    }

    public override void _Ready() {
        currentColorRect = null;
        if (currentColorRectPath != null && !currentColorRectPath.IsEmpty)
            currentColorRect = GetNodeOrNull<ColorRect>(currentColorRectPath);
        if (currentColorRect == null)
            currentColorRect = GetNodeOrNull<ColorRect>("../Panel/CurrentColorRect");
        if (currentColorRect != null)
            currentColorRect.Color = PaintColor;
        img = Image.CreateEmpty(img_size.X, img_size.Y, false, Image.Format.Rgba8);
        img.Fill(Colors.White);
        Texture = ImageTexture.CreateFromImage(img);
        var slider = GetNodeOrNull<HSlider>("../Panel/HSlider");
        if (slider != null) {
            slider.ValueChanged += _OnHSliderValueChanged;
        }
        var pixelModeToggle = GetNodeOrNull<CheckButton>("../Panel/PixelModeToggle");
        if (pixelModeToggle != null) {
            pixelModeToggle.Toggled += _OnPixelModeToggled;
        } else {
            GD.PushWarning("PainterImage: 未找到 PixelModeToggle 按钮");
        }
        if (canvasMeshPath != null && !canvasMeshPath.IsEmpty) {
            canvasMesh = GetNodeOrNull<MeshInstance3D>(canvasMeshPath);
            if (canvasMesh != null) {
                canvasMaterial = new StandardMaterial3D();
                canvasMaterial.AlbedoTexture = ImageTexture.CreateFromImage(img);
                canvasMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
                canvasMesh.MaterialOverride = canvasMaterial;
            }
        }
    }

    public void SetCanvasMesh(MeshInstance3D mesh) {
        canvasMesh = mesh;
        if (canvasMesh != null && img != null) {
            InitializeCanvasMesh();
        }
    }

    private void InitializeCanvasMesh() {
        if (canvasMesh == null || img == null || canvasMeshInitialized) return;
        canvasMaterial = new StandardMaterial3D();
        canvasMaterial.AlbedoTexture = ImageTexture.CreateFromImage(img);
        canvasMaterial.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        canvasMesh.MaterialOverride = canvasMaterial;
        canvasMeshInitialized = true;
        GD.Print($"PainterImage: CanvasMesh 初始化成功");
    }

    private void UpdateCanvasMeshTexture() {
        if (!canvasMeshInitialized && canvasMesh != null && img != null) {
            InitializeCanvasMesh();
        }
        if (canvasMesh != null && canvasMaterial != null && canvasMaterial.AlbedoTexture is ImageTexture imgTex) {
            imgTex.Update(img);
        }
    }

    private void _PaintTex(Vector2I pos, bool forcePixelMode = false) {
        if (pixelMode || forcePixelMode) {
            Vector2I gridIndex = new Vector2I(pos.X / pixelGridSize, pos.Y / pixelGridSize);
            Vector2I gridPos = new Vector2I(gridIndex.X * pixelGridSize, gridIndex.Y * pixelGridSize);
            img.FillRect(new Rect2I(gridPos, new Vector2I(pixelGridSize, pixelGridSize)), paint_color);
        } else {
            img.FillRect(new Rect2I(pos, new Vector2I(1, 1)).Grow(brush_size), paint_color);
        }
    }

    private Vector2I GetPixelGridIndex(Vector2I pos) {
        return new Vector2I(pos.X / pixelGridSize, pos.Y / pixelGridSize);
    }

    private void _OnPixelModeToggled(bool toggledOn) {
        pixelMode = toggledOn;
        lastPixelGrid = new Vector2I(-1, -1);
        GD.Print($"像素模式: {(pixelMode ? "开启" : "关闭")} (网格大小: {pixelGridSize})");
    }

    public void _OnHSliderValueChanged(double value) {
        brush_size = (int)value;
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseButton mb) {
            if (mb.Pressed && !mb.IsEcho()) {
                if (mb.ButtonIndex == MouseButton.Left) {
                    Vector2 localPos = ToLocal(mb.Position);
                    Vector2 imposF = localPos - Offset + GetRect().Size / 2.0f;
                    Vector2I impos = (Vector2I)imposF;
                    _PaintTex(impos);
                    if (pixelMode) {
                        lastPixelGrid = GetPixelGridIndex(impos);
                    }
                    ((ImageTexture)Texture).Update(img);
                    UpdateCanvasMeshTexture();
                }
                if (mb.ButtonIndex == MouseButton.Right) {
                    Vector2 localPos = ToLocal(mb.Position);
                    Vector2 imposF = localPos - Offset + GetRect().Size / 2.0f;
                    Vector2I impos = (Vector2I)imposF;
                    Color oldColor = paint_color;
                    paint_color = Colors.White;
                    _PaintTex(impos);
                    paint_color = oldColor;
                    if (pixelMode) {
                        lastPixelGrid = GetPixelGridIndex(impos);
                    }
                    ((ImageTexture)Texture).Update(img);
                    UpdateCanvasMeshTexture();
                }
            } else if (!mb.Pressed) {
                lastPixelGrid = new Vector2I(-1, -1);
            }
        }
        if (@event is InputEventMouseMotion mm) {
            if ((mm.ButtonMask & MouseButtonMask.Left) != 0) {
                Vector2 localPos = ToLocal(mm.Position);
                Vector2 imposF = localPos - Offset + GetRect().Size / 2.0f;
                Vector2I impos = (Vector2I)imposF;
                if (pixelMode) {
                    Vector2I currentGrid = GetPixelGridIndex(impos);
                    if (currentGrid != lastPixelGrid) {
                        _PaintTex(impos);
                        lastPixelGrid = currentGrid;
                    }
                } else {
                    _PaintTex(impos);
                    if (mm.Relative.LengthSquared() > 0) {
                        int num = Mathf.CeilToInt(mm.Relative.Length());
                        Vector2I target_pos = (Vector2I)(imposF - mm.Relative);
                        Vector2 current = impos;
                        for (int i = 0; i < num; i++) {
                            Vector2 toTarget = (Vector2)target_pos - current;
                            if (toTarget.LengthSquared() <= 1e-6f)
                                break;
                            current += toTarget.Normalized();
                            _PaintTex((Vector2I)current);
                        }
                        impos = (Vector2I)current;
                    }
                }
                ((ImageTexture)Texture).Update(img);
                UpdateCanvasMeshTexture();
            }
            if ((mm.ButtonMask & MouseButtonMask.Right) != 0) {
                Vector2 localPos = ToLocal(mm.Position);
                Vector2 imposF = localPos - Offset + GetRect().Size / 2.0f;
                Vector2I impos = (Vector2I)imposF;
                Color oldColor = paint_color;
                paint_color = Colors.White;
                if (pixelMode) {
                    Vector2I currentGrid = GetPixelGridIndex(impos);
                    if (currentGrid != lastPixelGrid) {
                        _PaintTex(impos);
                        lastPixelGrid = currentGrid;
                    }
                } else {
                    _PaintTex(impos);
                    if (mm.Relative.LengthSquared() > 0) {
                        int num = Mathf.CeilToInt(mm.Relative.Length());
                        Vector2I target_pos = (Vector2I)(imposF - mm.Relative);
                        Vector2 current = impos;
                        for (int i = 0; i < num; i++) {
                            Vector2 toTarget = (Vector2)target_pos - current;
                            if (toTarget.LengthSquared() <= 1e-6f)
                                break;
                            current += toTarget.Normalized();
                            _PaintTex((Vector2I)current);
                        }
                        impos = (Vector2I)current;
                    }
                }
                paint_color = oldColor;
                ((ImageTexture)Texture).Update(img);
                UpdateCanvasMeshTexture();
            }
        }
    }
}