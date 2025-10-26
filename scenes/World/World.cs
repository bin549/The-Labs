using Godot;
using System;
using PhantomCamera;

public partial class World : Node3D {
    [Export]
    public NodePath FirstPersonCameraPath { get; set; } = "Player/CameraPivot/Camera3D2";
    [Export]
    public NodePath ThirdPersonCameraPath { get; set; } = "Player/CameraPivot/Camera3D";
	[Export]
	public NodePath VisualsPath { get; set; } = "Player/visuals";
    private Camera3D firstPersonCamera;
    private Camera3D thirdPersonCamera;
    private bool isFirstPerson = false;
    private Node3D playerVisuals;
    private PhantomCamera3D phantomFirst;
    private PhantomCamera3D phantomThird;
    private InteractionManager interactionManager;
    private PauseMenu pauseMenu;
    private bool isPauseShown = false;

    public override void _Ready() {
        EnsureInputAction();
        firstPersonCamera = GetNodeOrNull<Camera3D>(FirstPersonCameraPath);
        thirdPersonCamera = GetNodeOrNull<Camera3D>(ThirdPersonCameraPath);
        playerVisuals = GetNodeOrNull<Node3D>(VisualsPath);
        var phantomFirstNode = GetNodeOrNull<Node3D>("Player/CameraPivot/PhantomCamFirst");
        var phantomThirdNode = GetNodeOrNull<Node3D>("Player/CameraPivot/PhantomCamThird");
        if (phantomFirstNode != null) phantomFirst = phantomFirstNode.AsPhantomCamera3D();
        if (phantomThirdNode != null) phantomThird = phantomThirdNode.AsPhantomCamera3D();
        if (firstPersonCamera == null || thirdPersonCamera == null) {
            GD.PushWarning("View toggle cameras not found. Check node paths.");
        }
        SetThirdPerson();
        interactionManager = GetNodeOrNull<InteractionManager>("InteractionManager");
        pauseMenu = GetNodeOrNull<PauseMenu>("PauseMenu");
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("toggle_view")) {
            ToggleView();
        }
        if (Input.IsActionJustPressed("ui_cancel")) {
            if (interactionManager != null && interactionManager.IsInteracting) {
                interactionManager.ExitInteraction();
                return;
            }
            if (pauseMenu != null) {
                if (!isPauseShown) {
                    pauseMenu.ShowMenu();
                    isPauseShown = true;
                } else {
                    pauseMenu.HideMenu();
                    isPauseShown = false;
                }
            }
        }
    }

    private void ToggleView() {
        if (phantomFirst != null && phantomThird != null) {
            isFirstPerson = !isFirstPerson;
            if (isFirstPerson) {
                phantomFirst.Priority = Math.Max(phantomThird.Priority + 1, phantomFirst.Priority + 1);
                if (playerVisuals != null) playerVisuals.Visible = false;
            } else {
                phantomThird.Priority = Math.Max(phantomFirst.Priority + 1, phantomThird.Priority + 1);
                if (playerVisuals != null) playerVisuals.Visible = true;
            }
            return;
        }
        if (firstPersonCamera == null || thirdPersonCamera == null) return;
        isFirstPerson = !isFirstPerson;
        if (isFirstPerson) {
            firstPersonCamera.Current = true;
            if (playerVisuals != null) playerVisuals.Visible = false;
        } else {
            thirdPersonCamera.Current = true;
            if (playerVisuals != null) playerVisuals.Visible = true;
        }
    }

    private void SetThirdPerson() {
        isFirstPerson = false;
        if (phantomThird != null && phantomFirst != null) {
            phantomThird.Priority = Math.Max(phantomFirst.Priority + 1, phantomThird.Priority + 1);
        } else {
            if (thirdPersonCamera != null) thirdPersonCamera.Current = true;
            if (firstPersonCamera != null && firstPersonCamera.Current) firstPersonCamera.Current = false;
        }
        if (playerVisuals != null) playerVisuals.Visible = true;
    }

    private void EnsureInputAction() {
        const string action = "toggle_view";
        if (!InputMap.HasAction(action)) {
            InputMap.AddAction(action);
            var ev = new InputEventKey { Keycode = Key.T };
            InputMap.ActionAddEvent(action, ev);
        }
    }
}
