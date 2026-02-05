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
    private bool IsInteracting => this.gameManager != null && this.gameManager.IsBusy;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        this.InitPhantomCamera();
        this.ResolveInteractionRay();
        this.ResolveGameManager();
    }

    private void InitPhantomCamera() {
        var phantomFirstNode = GetNodeOrNull<Node3D>("PhantomCamFirst");
        var phantomThirdNode = GetNodeOrNull<Node3D>("PhantomCamThird");
        if (phantomFirstNode != null) this.phantomFirst = phantomFirstNode.AsPhantomCamera3D();
        if (phantomThirdNode != null) this.phantomThird = phantomThirdNode.AsPhantomCamera3D();
    }

    public override void _PhysicsProcess(double delta) {
        if (this.interactionRay == null || !GodotObject.IsInstanceValid(this.interactionRay)) {
            this.ResolveInteractionRay();
        }
        if (this.gameManager == null || !GodotObject.IsInstanceValid(this.gameManager)) {
            this.ResolveGameManager();
        }
        if (this.IsInteracting) {
            return;
        }
        this.CheckInteraction();
    }

    public override void _Input(InputEvent @event) {
        if (this.IsInteracting) return;
        if (@event is InputEventMouseMotion mouseEvent) {
            RotateY(-Mathf.DegToRad(mouseEvent.Relative.X * MOUSE_SENSITIVITY_HORIZONTAL));
            this.pitch -= mouseEvent.Relative.Y * MOUSE_SENSITIVITY_VERTICLE;
            this.pitch = Mathf.Clamp(this.pitch, -90f, 90f);
            var currentRotation = Rotation;
            Rotation = new Vector3(Mathf.DegToRad(this.pitch), currentRotation.Y, currentRotation.Z);
        }
    }

    public override void _Process(double delta) {
        if (this.IsInteracting) return;
        this.ToggleView();
    }

    private void ToggleView() {
        if (Input.IsActionJustPressed("toggle_view")) {
            this.isFirstPerson = !this.isFirstPerson;
            this.phantomFirst.Priority = this.isFirstPerson ? 15 : 5;
        }
    }

    private void CheckInteraction() {
        if (this.interactionRay == null) return;
        this.interactionRay.ForceRaycastUpdate();
        if (this.interactionRay.IsColliding()) {
            var colliderNode = this.interactionRay.GetCollider() as Node;
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
        if (this.interactionRayPath != null && this.interactionRayPath.ToString() != string.Empty) {
            this.interactionRay = GetNodeOrNull<RayCast3D>(this.interactionRayPath);
        }
        if (this.interactionRay == null) {
            this.interactionRay = GetNodeOrNull<RayCast3D>("PhantomCamFirst/InteractionRay") ??
                                  GetNodeOrNull<RayCast3D>("PhantomCamThird/InteractionRay");
        }
        if (this.interactionRay != null) {
            this.interactionRay.Enabled = true;
        }
    }

    private void ResolveGameManager() {
        if (this.gameManagerPath != null && this.gameManagerPath.ToString() != string.Empty) {
            this.gameManager = GetNodeOrNull<GameManager>(this.gameManagerPath);
        }
        if (this.gameManager == null) {
            this.gameManager = GetTree().Root.GetNodeOrNull<GameManager>("GameManager") ??
                               GetTree().Root.FindChild("GameManager", true, false) as GameManager;
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