using Godot;
using PhantomCamera;

public partial class BookPickup : Interactable {
	[Export] public NodePath PlayerPath { get; set; } = default;
	[Export] public NodePath PlayerBookPath { get; set; } = default;
	[Export] public NodePath PhantomCameraPath { get; set; } = default;
	[Export] public NodePath PickupVisualRootPath { get; set; } = default;
	[Export] public int InspectCameraPriority { get; set; } = 1200;
	[Export] public int HiddenCameraPriority { get; set; } = 0;

	private Node3D playerNode;
	private Node3D playerBook;
	private PhantomCamera3D phantomCamera;
	private Node3D pickupVisualRoot;
	private int? originalCameraPriority;
	private bool isInteractingWithBook;
	private bool canExitInteraction;

	public override void _Ready() {
		base._Ready();
		this.ResolvePlayerAndCamera();
		this.ResolvePickupVisual();
	}

	public override void _Process(double delta) {
		base._Process(delta);
		if (!this.isInteractingWithBook) return;
		if (Input.IsActionJustPressed("pause") || Input.IsActionJustPressed("ui_cancel")) {
			this.ExitInteraction();
			return;
		}
		if (!this.canExitInteraction && Input.IsActionJustReleased("interact")) {
			this.canExitInteraction = true;
		} else if (this.canExitInteraction && Input.IsActionJustPressed("interact")) {
			this.ExitInteraction();
		}
	}

	public override void EnterInteraction() {
		if (this.isInteractingWithBook) return;
		this.ResolvePlayerAndCamera();
		this.ResolvePickupVisual();
		base.EnterInteraction();
		this.isInteractingWithBook = true;
		this.canExitInteraction = false;
		this.ShowPickupVisual(false);
		this.ShowPlayerBook(true);
		BoostCameraPriority(true);
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void ExitInteraction() {
		if (!this.isInteractingWithBook) return;
		this.ShowPlayerBook(false);
		BoostCameraPriority(false);
		this.isInteractingWithBook = false;
		this.canExitInteraction = false;
		Input.ActionRelease("pause");
		Input.ActionRelease("ui_cancel");
		base.this.ExitInteraction();
		if (gameManager != null) {
			gameManager.SetCurrentInteractable(null);
		}
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private void ResolvePlayerAndCamera() {
		if (playerNode == null || !GodotObject.IsInstanceValid(playerNode)) {
			playerNode = ResolveNodePath(NodePathIsValid(PlayerPath) ? PlayerPath : default, this) ??
				GetTree().Root.GetNodeOrNull<Node3D>("World/Player") ??
				GetTree().Root.FindChild("Player", true, false) as Node3D;
			if (playerNode == null) {
				GD.PushWarning($"{Name}: 未找到 Player 节点，拾取书籍功能将不可用。");
			}
		}
		if (playerNode != null && (playerBook == null || !GodotObject.IsInstanceValid(playerBook))) {
			if (NodePathIsValid(PlayerBookPath)) {
				playerBook = ResolveNodePath(PlayerBookPath, playerNode);
			}
			playerBook ??= playerNode.FindChild("Book", true, false) as Node3D;
			if (playerBook == null) {
				GD.PushWarning($"{Name}: 未找到玩家持有的书籍节点。");
			}
		}
		if (playerBook != null && (phantomCamera == null || !GodotObject.IsInstanceValid(phantomCamera.Node3D))) {
			Node3D phantomNode = null;
			if (NodePathIsValid(PhantomCameraPath)) {
				phantomNode = ResolveNodePath(PhantomCameraPath, playerBook) ?? ResolveNodePath(PhantomCameraPath, this);
			}
			phantomNode ??= playerBook.FindChild("PhantomCamera3D", true, false) as Node3D;
			if (phantomNode != null) {
				phantomCamera = phantomNode.AsPhantomCamera3D();
			} else {
				phantomCamera = null;
				GD.PushWarning($"{Name}: 未找到 PhantomCamera3D，拾取后不会切换到书籍摄像机。");
			}
		}
	}

	private void ResolvePickupVisual() {
		if (pickupVisualRoot != null && GodotObject.IsInstanceValid(pickupVisualRoot)) return;
		if (NodePathIsValid(PickupVisualRootPath)) {
			pickupVisualRoot = ResolveNodePath(PickupVisualRootPath, this);
		}
		pickupVisualRoot ??= this;
	}

	private void ShowPlayerBook(bool visible) {
		if (playerBook == null || !GodotObject.IsInstanceValid(playerBook)) return;
		playerBook.Visible = visible;
		playerBook.ProcessMode = visible ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
	}

	private void ShowPickupVisual(bool visible) {
		if (pickupVisualRoot == null || !GodotObject.IsInstanceValid(pickupVisualRoot)) return;
		pickupVisualRoot.Visible = visible;
		pickupVisualRoot.ProcessMode = visible ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;
	}

	private void BoostCameraPriority(bool enable) {
		if (phantomCamera == null) return;
		if (!originalCameraPriority.HasValue) {
			originalCameraPriority = phantomCamera.Priority;
		}
		if (enable) {
			phantomCamera.Priority = InspectCameraPriority;
		} else {
			phantomCamera.Priority = HiddenCameraPriority;
		}
	}

	private static bool NodePathIsValid(NodePath path) {
		return path != null && path.ToString() != string.Empty;
	}

	private static Node3D ResolveNodePath(NodePath path, Node context) {
		if (!NodePathIsValid(path) || context == null) return null;
		var node = context.GetNodeOrNull<Node>(path);
		return node as Node3D;
	}
}
