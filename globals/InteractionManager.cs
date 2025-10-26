using Godot;

public partial class InteractionManager : Node {
    [Export]
    public NodePath CameraPath { get; set; } = "MainCamera3D";
    [Export]
    public float MaxDistance { get; set; } = 4.0f;
    [Export]
    public Label3D HintLabel3D { get; set; }
    [Export]
    public NodePath HintLabel3DPath { get; set; } = new NodePath();
    private Camera3D _camera;
    private Interactable _focused;
    private Camera3D _previousCamera;
    private Node3D _activeInteractionCam;
    private float _yaw;
    private float _pitch;
    private Vector2 _lastMousePos;
    [Export] public float InteractionYawSpeed = 0.15f;
    [Export] public float InteractionPitchSpeed = 0.15f;
    [Export] public Vector2 InteractionPitchClamp = new Vector2(-20, 35);
    public bool IsInteracting { get; private set; }
    public bool PlayerControlEnabled { get; private set; } = true;

    public override void _Ready() {
        EnsureInputAction();
        _camera = GetNodeOrNull<Camera3D>(CameraPath);
        if (_camera == null) {
            GD.PushWarning("InteractionManager: Camera not found.");
        }
        if (HintLabel3D == null && !HintLabel3DPath.IsEmpty) {
            HintLabel3D = GetNodeOrNull<Label3D>(HintLabel3DPath);
        }
        if (HintLabel3D != null) {
            HintLabel3D.Visible = false;
        }
    }

    public override void _Process(double delta) {
        UpdateFocus();
        if (Input.IsActionJustPressed("interact") && _focused != null && !IsInteracting) {
            _previousCamera = _camera;
            _focused.Interact(_camera);
            _activeInteractionCam = _focused.GetActiveCameraNode();
            ResetInteractionAngles();
            IsInteracting = true;
            PlayerControlEnabled = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    private void UpdateFocus() {
        if (_camera == null) return;
        var from = _camera.ProjectRayOrigin(GetViewport().GetMousePosition());
        var dir = _camera.ProjectRayNormal(GetViewport().GetMousePosition());
        var to = from + dir * MaxDistance;
        var space = _camera.GetWorld3D()?.DirectSpaceState;
        if (space == null) return;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        var hit = space.IntersectRay(query);
        Interactable hitInteractable = null;
        if (hit.Count > 0) {
            if (hit.TryGetValue("collider", out var colliderObj)) {
                var collider = (GodotObject)colliderObj;
                if (collider is Node n) {
                    hitInteractable = FindInteractable(n);
                }
            }
        }
        if (hitInteractable != _focused) {
            _focused?.OnFocusExit();
            _focused = hitInteractable;
            _focused?.OnFocusEnter();
        }
        bool useGlobalHint = false;
        if (HintLabel3D != null) {
            if (_focused == null) {
                useGlobalHint = true;
            }
            else {
                var namePath = (NodePath)_focused.Get("NameLabelPath");
                useGlobalHint = namePath.IsEmpty;
            }
        }
        if (HintLabel3D != null && useGlobalHint) {
            if (_focused != null && !IsInteracting) {
                HintLabel3D.Visible = true;
                HintLabel3D.Text = $"[E] {_focused.ActionName}: {_focused.DisplayName}";
                Vector3 hintPos = _camera.GlobalPosition + _camera.GlobalTransform.Basis.Z * -1.5f;
                if (hit.Count > 0 && hit.TryGetValue("position", out var posObj)) {
                    hintPos = (Vector3)posObj;
                }
                HintLabel3D.GlobalPosition = hintPos;
            }
            else {
                HintLabel3D.Visible = false;
            }
        }
        if (IsInteracting && _activeInteractionCam != null) {
            var currentPos = GetViewport().GetMousePosition();
            var delta = currentPos - _lastMousePos;
            _yaw -= delta.X * InteractionYawSpeed;
            _pitch = Mathf.Clamp(_pitch - delta.Y * InteractionPitchSpeed, InteractionPitchClamp.X, InteractionPitchClamp.Y);
            _activeInteractionCam.RotationDegrees = new Vector3(_pitch, _yaw, 0);
            _lastMousePos = currentPos;
        }
    }

    public void ExitInteraction() {
        if (!IsInteracting) return;
        if (_previousCamera != null) {
            _previousCamera.Current = true;
        }
        _focused?.ExitInteraction();
        IsInteracting = false;
        PlayerControlEnabled = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void ResetInteractionAngles() {
        if (_activeInteractionCam != null) {
            _yaw = _activeInteractionCam.RotationDegrees.Y;
            _pitch = _activeInteractionCam.RotationDegrees.X;
        }
        _lastMousePos = GetViewport().GetMousePosition();
    }

    private Interactable FindInteractable(Node node) {
        Node current = node;
        while (current != null) {
            if (current is Interactable it) return it;
            current = current.GetParent();
        }
        return null;
    }

    private void EnsureInputAction() {
        const string action = "interact";
        if (!InputMap.HasAction(action)) {
            InputMap.AddAction(action);
            var ev = new InputEventKey { Keycode = Key.E };
            InputMap.ActionAddEvent(action, ev);
        }
    }
}
