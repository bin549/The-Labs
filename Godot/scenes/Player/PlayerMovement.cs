using Godot;

public partial class PlayerMovement : CharacterBody3D {
    [Export] private float WalkSpeed = 3.0f;
    [Export] private float RunSpeed = 8.0f;
    private float JUMP_VELOCITY = 5.5f;
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private Node3D cameraPivot;
    private float pitch = 0.0f;
    private AnimationPlayer animationPlayer;
    private Node3D visuals;
    private bool isJumping = false;
    [Export] public NodePath VisualsPath { get; set; } = "Player/visuals";
    [Export] private NodePath gameManagerPath;
    private GameManager gameManager;
    private bool IsInteracting => gameManager != null && gameManager.IsBusy;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        this.animationPlayer = GetNode<AnimationPlayer>("visuals/mixamo_base/AnimationPlayer");
        this.visuals = GetNode<Node3D>("visuals");
        this.ResolveGameManager();
    }

    public override void _PhysicsProcess(double delta) {
        if (gameManager == null || !GodotObject.IsInstanceValid(gameManager)) {
            this.ResolveGameManager();
        }
        this.ApplyMovement(delta);
        var simpleGrass = GetNodeOrNull<Node>("/root/SimpleGrass");
        simpleGrass?.Call("set_player_position", GlobalPosition);
    }

    private void ApplyMovement(double delta) {
        Vector3 velocity = Velocity;
        bool onFloor = IsOnFloor();
        if (!onFloor)
            velocity.Y -= this.gravity * (float)delta;
        bool interacting = IsInteracting;
        if (onFloor) {
            if (Input.IsActionJustPressed("jump")) {
                velocity.Y = this.JUMP_VELOCITY;
                this.isJumping = true;
                this.PlayAnimation("jump");
            } else if (this.isJumping) {
                this.isJumping = false;
            }
        } else if (this.isJumping && velocity.Y <= 0.0f) {
            this.isJumping = false;
        }
        Vector2 inputDir = interacting
            ? Vector2.Zero
            : Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        bool isRunning = !interacting && Input.IsActionPressed("run");
        float speed = isRunning ? this.RunSpeed : this.WalkSpeed;
        Node3D cameraPivot = GetNode<Node3D>("CameraPivot");
        Basis camBasis = cameraPivot.GlobalTransform.Basis;
        Vector3 forward = camBasis.Z;
        forward.Y = 0;
        forward = forward.Normalized();
        Vector3 right = camBasis.X;
        right.Y = 0;
        right = right.Normalized();
        Vector3 direction = (right * inputDir.X + forward * inputDir.Y).Normalized();
        if (!onFloor) {
            if (this.isJumping && velocity.Y > 0.0f) {
                this.PlayAnimation("jump");
            } else {
                this.PlayAnimation("fall");
            }
        } else if (this.isJumping) {
            this.PlayAnimation("jump");
        } else if (direction != Vector3.Zero) {
            Vector3 lookTarget = this.visuals.GlobalPosition + new Vector3(direction.X, 0, direction.Z);
            this.visuals.LookAt(lookTarget, Vector3.Up);
            velocity.X = direction.X * speed;
            velocity.Z = direction.Z * speed;
            this.PlayAnimation(isRunning ? "run" : "walk");
        } else {
            this.PlayAnimation("idle");
            float stopSpeed = interacting ? Mathf.Max(this.WalkSpeed, this.RunSpeed) : speed;
            velocity.X = Mathf.MoveToward(velocity.X, 0, stopSpeed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, stopSpeed);
        }
        Velocity = velocity;
        MoveAndSlide();
    }

    private void PlayAnimation(string animationName) {
        if (this.animationPlayer.CurrentAnimation != animationName) {
            this.animationPlayer.Play(animationName);
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
    }
}