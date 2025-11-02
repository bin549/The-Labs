using Godot;
using System;
using PhantomCamera;

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
	[Export]
	public NodePath VisualsPath { get; set; } = "Player/visuals";
    private bool isFirstPerson = false;
    private PhantomCamera3D phantomFirst;
    private PhantomCamera3D phantomThird;
    private InteractionManager interactionManager;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        animationPlayer = GetNode<AnimationPlayer>("visuals/mixamo_base/AnimationPlayer");
        visuals = GetNode<Node3D>("visuals");
        this.InitPhantomCamera();
        interactionManager = GetNodeOrNull<InteractionManager>("InteractionManager");
    }

    private void InitPhantomCamera() {
        var phantomFirstNode = GetNodeOrNull<Node3D>("CameraPivot/PhantomCamFirst");
        var phantomThirdNode = GetNodeOrNull<Node3D>("CameraPivot/PhantomCamThird");
        if (phantomFirstNode != null) phantomFirst = phantomFirstNode.AsPhantomCamera3D();
        if (phantomThirdNode != null) phantomThird = phantomThirdNode.AsPhantomCamera3D();
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseMotion mouseEvent) {
            cameraPivot = GetNode<Node3D>("CameraPivot");
            cameraPivot.RotateY(-Mathf.DegToRad(mouseEvent.Relative.X * MOUSE_SENSITIVITY_HORIZONTAL));
            pitch -= mouseEvent.Relative.Y * MOUSE_SENSITIVITY_VERTICLE;
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            var currentRotation = cameraPivot.Rotation;
            cameraPivot.Rotation = new Vector3(Mathf.DegToRad(pitch), currentRotation.Y, currentRotation.Z);

        }
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("quit")) {
            if (interactionManager != null && interactionManager.IsInteracting) {
                interactionManager.ExitInteraction();
                return;
            }
        }
        this.ToggleView();
    }

    private void ToggleView() {
        if (Input.IsActionJustPressed("toggle_view")) {
            this.isFirstPerson = !isFirstPerson;
            phantomFirst.Priority = this.isFirstPerson ? 15 : 5;
        }
    }

    public override void _PhysicsProcess(double delta) {
        Vector3 velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JUMP_VELOCITY;
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        bool isRunning = Input.IsActionPressed("run");
        float speed = isRunning ? RunSpeed : WalkSpeed;
        Node3D cameraPivot = GetNode<Node3D>("CameraPivot");
        Basis camBasis = cameraPivot.GlobalTransform.Basis;
        Vector3 forward = camBasis.Z;
        forward.Y = 0;
        forward = forward.Normalized();
        Vector3 right = camBasis.X;
        right.Y = 0;
        right = right.Normalized();
        Vector3 direction = (right * inputDir.X + forward * inputDir.Y).Normalized();
        if (direction != Vector3.Zero) {
            Vector3 lookTarget = visuals.GlobalPosition + new Vector3(direction.X, 0, direction.Z);
            visuals.LookAt(lookTarget, Vector3.Up);
            velocity.X = direction.X * speed;
            velocity.Z = direction.Z * speed;
            this.PlayAnimation(isRunning ? "running" : "walking");
        } else {
            this.PlayAnimation("idle");
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
