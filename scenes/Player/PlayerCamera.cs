using Godot;
using System;
using PhantomCamera;

public partial class PlayerCamera : Node3D {
	[Export] private float MOUSE_SENSITIVITY_HORIZONTAL = 0.5f;
	[Export] private float MOUSE_SENSITIVITY_VERTICLE = 0.5f;
	private float pitch = 0.0f;
	private bool isFirstPerson = false;
	private PhantomCamera3D phantomFirst;
	private PhantomCamera3D phantomThird;
	private RayCast3D interactionRay;
	[Export] private NodePath interactionRayPath = "PhantomCamFirst/InteractionRay";
	[Export] private NodePath gameManagerPath;
	private Interactable currentInteractable;
	private GameManager gameManager;
	private bool IsInteracting => gameManager != null && gameManager.IsBusy;

	public override void _Ready() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
		this.InitPhantomCamera();
		this.ResolveInteractionRay();
		this.ResolveGameManager();
	}

	private void InitPhantomCamera() {
		var phantomFirstNode = GetNodeOrNull<Node3D>("PhantomCamFirst");
		var phantomThirdNode = GetNodeOrNull<Node3D>("PhantomCamThird");
		if (phantomFirstNode != null) phantomFirst = phantomFirstNode.AsPhantomCamera3D();
		if (phantomThirdNode != null) phantomThird = phantomThirdNode.AsPhantomCamera3D();
	}

	public override void _PhysicsProcess(double delta) {
		if (interactionRay == null || !GodotObject.IsInstanceValid(interactionRay)) {
			this.ResolveInteractionRay();
		}
		if (gameManager == null || !GodotObject.IsInstanceValid(gameManager)) {
			this.ResolveGameManager();
		}
		if (IsInteracting) {
			return;
		}
		this.CheckInteraction();
	}

	public override void _Input(InputEvent @event) {
		if (IsInteracting) return;
		if (@event is InputEventMouseMotion mouseEvent) {
			RotateY(-Mathf.DegToRad(mouseEvent.Relative.X * MOUSE_SENSITIVITY_HORIZONTAL));
			pitch -= mouseEvent.Relative.Y * MOUSE_SENSITIVITY_VERTICLE;
			pitch = Mathf.Clamp(pitch, -90f, 90f);
			var currentRotation = Rotation;
			Rotation = new Vector3(Mathf.DegToRad(pitch), currentRotation.Y, currentRotation.Z);
		}
	}

	public override void _Process(double delta) {
		if (IsInteracting) return;
		this.ToggleView();
	}

	private void ToggleView() {
		if (Input.IsActionJustPressed("toggle_view")) {
			this.isFirstPerson = !isFirstPerson;
			phantomFirst.Priority = this.isFirstPerson ? 15 : 5;
		}
	}

	private void CheckInteraction() {
		if (interactionRay == null) return;
		interactionRay.ForceRaycastUpdate();
		if (interactionRay.IsColliding()) {
			var colliderNode = interactionRay.GetCollider() as Node;
			var interactable = FindInteractable(colliderNode);
			if (interactable != null) {
				if (this.currentInteractable != interactable) {
					if (this.currentInteractable != null) {
						this.currentInteractable.OnFocusExit();
					}
					this.currentInteractable = interactable;
					this.currentInteractable.OnFocusEnter();
				}
				return;
			}
		}
		if (this.currentInteractable != null) {
			this.currentInteractable.OnFocusExit();
			this.currentInteractable = null;
		}
	}

	private void ResolveInteractionRay() {
		if (interactionRayPath != null && interactionRayPath.ToString() != string.Empty) {
			interactionRay = GetNodeOrNull<RayCast3D>(interactionRayPath);
		}
		if (interactionRay == null) {
			interactionRay = GetNodeOrNull<RayCast3D>("PhantomCamFirst/InteractionRay") ??
				GetNodeOrNull<RayCast3D>("PhantomCamThird/InteractionRay");
		}
		if (interactionRay != null) {
			interactionRay.Enabled = true;
		}
	}

	private void ResolveGameManager() {
		if (gameManagerPath != null && gameManagerPath.ToString() != string.Empty) {
			gameManager = GetNodeOrNull<GameManager>(gameManagerPath);
		}
		if (gameManager == null) {
			gameManager = GetTree().Root.GetNodeOrNull<GameManager>("GameManager") ??
				GetTree().Root.FindChild("GameManager", true, false) as GameManager;
		}
		if (gameManager == null) {
			GD.PushWarning($"{Name}: 未找到 GameManager 节点，无法检测交互状态。");
		}
	}

	private static Interactable FindInteractable(Node node) {
		var cursor = node;
		while (cursor != null) {
			if (cursor is Interactable interactable) return interactable;
			cursor = cursor.GetParent();
		}
		return null;
	}
}
