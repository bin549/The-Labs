using Godot;
using System;

public partial class Player : CharacterBody3D {
    [Export] private float WalkSpeed = 3.0f;
    [Export] private float RunSpeed = 8.0f;
    private float JUMP_VELOCITY = 4.5f;
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    [Export] private float MOUSE_SENSITIVITY_HORIZONTAL = 0.5f;
    [Export] private float MOUSE_SENSITIVITY_VERTICLE = 0.5f;
    private Node3D cameraPivot;
    private float pitch = 0.0f;
    private AnimationPlayer animationPlayer;
    private Node3D visuals;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        animationPlayer = GetNode<AnimationPlayer>("visuals/mixamo_base/AnimationPlayer");
        visuals = GetNode<Node3D>("visuals");
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseEvent) {
            RotateY(-Mathf.DegToRad(mouseEvent.Relative.X * MOUSE_SENSITIVITY_HORIZONTAL));
            // visuals.Rotation = new Vector3(visuals.Rotation.X, Rotation.Y, visuals.Rotation.Z);
            cameraPivot = GetNode<Node3D>("CameraPivot");
            pitch -= mouseEvent.Relative.Y * MOUSE_SENSITIVITY_VERTICLE;
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            cameraPivot.Rotation = new Vector3(Mathf.DegToRad(pitch), 0, 0);
        }
        if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
            if (keyEvent.Keycode == Key.Escape) {
                GetTree().Quit();
            }
        }
    }

    public override void _PhysicsProcess(double delta) {
        Vector3 velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JUMP_VELOCITY;
        Vector2 input_dir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        bool isRunning = Input.IsActionPressed("run");
        float speed = isRunning ? RunSpeed : WalkSpeed;
        Vector3 direction = (Transform.Basis * new Vector3(input_dir.X, 0, input_dir.Y)).Normalized();
        if (direction != Vector3.Zero) {
            Vector3 lookTarget = visuals.GlobalPosition + new Vector3(direction.X, 0, direction.Z);
            visuals.LookAt(lookTarget, Vector3.Up);
            velocity.X = direction.X * speed;
            velocity.Z = direction.Z * speed;
            PlayAnimation(isRunning ? "running" : "walking");
        } else {
            PlayAnimation("idle");
            velocity.X = Mathf.MoveToward(velocity.X, 0, speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, speed);
        }
        Velocity = velocity;
        MoveAndSlide();
    }


    private void PlayAnimation(string animationName) {
        if (animationPlayer.CurrentAnimation != animationName) {
            animationPlayer.Play(animationName);
        }
    }
}
