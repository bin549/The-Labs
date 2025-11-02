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
	[Export] private RayCast3D interactionRay;
	private Node currentInteractable;

	public override void _Ready() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
		this.InitPhantomCamera();
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
		if (interactionRay.IsColliding()) {
			var collider = interactionRay.GetCollider() as Node;
			if (this.currentInteractable != null && collider == this.currentInteractable) {
				return;
			}
			bool hasInteractMethod = collider != null && (collider.HasMethod("Interact"));
			if (hasInteractMethod) {
	 			if (this.currentInteractable != collider) {
					this.currentInteractable = collider;
					if (collider.HasMethod("OnFocusEnter"))
   						collider.Call("OnFocusEnter");
				}
 			}
		} else {
			if (this.currentInteractable != null) {
				if (currentInteractable.HasMethod("OnFocusExit"))
					currentInteractable.Call("OnFocusExit");
				this.currentInteractable = null;
			}
		}
	}
}
