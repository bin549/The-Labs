using Godot;

public partial class SliderObject : PlacableItem {
    [Export] public float Mass { get; set; } = 1.0f;
    [Export] public bool IsOnInclinedPlane { get; private set; } = false;
    private RigidBody3D rigidBody;
    private bool isReleased = false;

    public override void _Ready() {
        base._Ready();
        InitializeRigidBody();
    }

    private void InitializeRigidBody() {
        rigidBody = GetNodeOrNull<RigidBody3D>("RigidBody3D");
        if (rigidBody != null) {
            rigidBody.Mass = Mass;
            rigidBody.GravityScale = 1.0f;
            rigidBody.Freeze = true;
        }
    }

    public void Release() {
        if (isReleased) return;
        isReleased = true;
        IsDraggable = false;
        if (rigidBody != null) {
            rigidBody.Freeze = false;
        }
    }

    public void Reset() {
        isReleased = false;
        IsDraggable = true;
        IsOnInclinedPlane = false;
        if (rigidBody != null) {
            rigidBody.Freeze = true;
            rigidBody.LinearVelocity = Vector3.Zero;
            rigidBody.AngularVelocity = Vector3.Zero;
        }
    }

    public void SetOnInclinedPlane(bool onPlane) {
        IsOnInclinedPlane = onPlane;
    }
}

