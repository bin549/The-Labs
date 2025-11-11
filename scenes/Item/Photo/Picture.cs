using Godot;

public partial class Picture : Interactable {
	[Export] public NodePath TextureRectPath { get; set; } = default;
	[Export] public Texture2D PhotoTexture { get; set; }
	private TextureRect textureRect;
	private bool canToggleOff = false;
	private Texture2D originalTexture;

	public override void _Ready() {
		base._Ready();
		ResolveTextureRect();
		if (textureRect != null) {
			originalTexture = textureRect.Texture;
			textureRect.Visible = false;
		}
	}

	public override void _Process(double delta) {
		base._Process(delta);
		if (!isInteracting) return;
		if (!canToggleOff && Input.IsActionJustReleased("interact")) {
			canToggleOff = true;
		} else if (canToggleOff && Input.IsActionJustPressed("interact")) {
			ExitInteraction();
		}
	}

	public override void EnterInteraction() {
		if (isInteracting) return;
		ResolveGameManager();
		base.EnterInteraction();
		isInteracting = true;
		canToggleOff = false;
		ShowTexture(true);
	}

	public override void ExitInteraction() {
		if (!isInteracting) return;
		ShowTexture(false);
		isInteracting = false;
		canToggleOff = false;
		base.ExitInteraction();
		if (gameManager != null) {
			gameManager.SetCurrentInteractable(null);
		}
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private void ShowTexture(bool visible) {
		if (textureRect == null) return;
		if (visible) {
			if (PhotoTexture != null) {
				textureRect.Texture = PhotoTexture;
			}
		} else {
			textureRect.Texture = originalTexture;
		}
		textureRect.Visible = visible;
		textureRect.ProcessMode = visible ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
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
		textureRect = candidate;
	}
}
