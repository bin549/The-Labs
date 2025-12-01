using Godot;

public partial class Picture : Interactable {
    [Export] public NodePath TextureRectPath { get; set; } = default;
    [Export] public Texture2D PhotoTexture { get; set; }
    private TextureRect textureRect;
    private bool canToggleOff = false;
    private Texture2D originalTexture;

    public override void _Ready() {
        base._Ready();
        this.ResolveTextureRect();
        if (this.textureRect != null) {
            this.originalTexture = this.textureRect.Texture;
            this.textureRect.Visible = false;
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (!base.isInteracting) return;
        if (!this.canToggleOff && Input.IsActionJustReleased("interact")) {
            this.canToggleOff = true;
        } else if (this.canToggleOff && Input.IsActionJustPressed("interact")) {
            this.ExitInteraction();
        }
    }

    public override void EnterInteraction() {
        if (base.isInteracting) return;
        base.ResolveGameManager();
        base.EnterInteraction();
        base.isInteracting = true;
        this.canToggleOff = false;
        this.ShowTexture(true);
    }

    public override void ExitInteraction() {
        if (!base.isInteracting) return;
        this.ShowTexture(false);
        base.isInteracting = false;
        this.canToggleOff = false;
        base.ExitInteraction();
        if (gameManager != null) {
            gameManager.SetCurrentInteractable(null);
        }
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void ShowTexture(bool visible) {
        if (this.textureRect == null) return;
        if (visible) {
            if (PhotoTexture != null) {
                this.textureRect.Texture = PhotoTexture;
            }
        } else {
            this.textureRect.Texture = this.originalTexture;
        }
        this.textureRect.Visible = visible;
        this.textureRect.ProcessMode = visible ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
    }

    private void ResolveTextureRect() {
        if (TextureRectPath == default || TextureRectPath.ToString() == string.Empty) return;
        var candidate = GetNodeOrNull<TextureRect>(TextureRectPath);
        if (candidate == null) {
            candidate = GetTree().Root.GetNodeOrNull<TextureRect>(TextureRectPath);
        }
        if (candidate == null) {
            GD.PushWarning($"{Name}: 未找到 TextureRect 节点 {TextureRectPath}，无法显示照片。");
        }
        this.textureRect = candidate;
    }
}