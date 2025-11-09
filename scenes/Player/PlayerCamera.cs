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
	[Export] private NodePath interactionRayPath;
	private Interactable currentInteractable;

	public override void _Ready() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
		this.InitPhantomCamera();
		if (interactionRay == null && interactionRayPath != null && interactionRayPath.ToString() != string.Empty) {
			interactionRay = GetNodeOrNull<RayCast3D>(interactionRayPath);
		}
		if (interactionRay != null) {
			interactionRay.Enabled = true;
		}
	}

	private void InitPhantomCamera() {
		var phantomFirstNode = GetNodeOrNull<Node3D>("PhantomCamFirst");
		var phantomThirdNode = GetNodeOrNull<Node3D>("PhantomCamThird");
		if (phantomFirstNode != null) phantomFirst = phantomFirstNode.AsPhantomCamera3D();
		if (phantomThirdNode != null) phantomThird = phantomThirdNode.AsPhantomCamera3D();
	}

	public override void _PhysicsProcess(double delta) {
		this.CheckInteraction();
	}

	public override void _Input(InputEvent @event) {
		if (@event is InputEventMouseMotion mouseEvent) {
			RotateY(-Mathf.DegToRad(mouseEvent.Relative.X * MOUSE_SENSITIVITY_HORIZONTAL));
			pitch -= mouseEvent.Relative.Y * MOUSE_SENSITIVITY_VERTICLE;
			pitch = Mathf.Clamp(pitch, -90f, 90f);
			var currentRotation = Rotation;
			Rotation = new Vector3(Mathf.DegToRad(pitch), currentRotation.Y, currentRotation.Z);
		}
	}

	public override void _Process(double delta) {
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

		if (interactionRay.IsColliding()) {
			var colliderNode = interactionRay.GetCollider() as Node;
			var interactable = FindInteractable(colliderNode);

			if (interactable != null) {
				if (this.currentInteractable != interactable) {
					// 先对上一个发出退出
					if (this.currentInteractable != null) {
						this.currentInteractable.OnFocusExit();
					}
					this.currentInteractable = interactable;
					this.currentInteractable.OnFocusEnter();
				}
				return;
			}
		}

		// 未命中或未找到交互对象，清理焦点
		if (this.currentInteractable != null) {
			this.currentInteractable.OnFocusExit();
			this.currentInteractable = null;
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
