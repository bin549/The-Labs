using Godot;

public partial class Door : Interactable {
    [Export] public NodePath DoorPivotPath { get; set; } = new("walldoor_0012/Node3D");
    [Export] public Vector3 ClosedRotationDegrees { get; set; } = Vector3.Zero;
    [Export] public Vector3 OpenRotationDegrees { get; set; } = new Vector3(0f, -90f, 0f);
    [Export] public float TransitionDuration { get; set; } = 0.6f;
    [Export] public Tween.TransitionType TransitionType { get; set; } = Tween.TransitionType.Sine;
    [Export] public Tween.EaseType EaseType { get; set; } = Tween.EaseType.InOut;
    [Export] public bool StartsOpen { get; set; } = false;
    [Export] public AudioStreamPlayer3D OpenSound { get; set; }
    [Export] public AudioStreamPlayer3D CloseSound { get; set; }
    private Node3D doorPivot;
    private Tween rotationTween;
    private bool disOpen;

    public override void _Ready() {
        base._Ready();
        this.ResolveDoorPivot();
        this.disOpen = this.StartsOpen;
        this.ApplyDoorRotation(this.disOpen ? OpenRotationDegrees : ClosedRotationDegrees);
    }

    public override void EnterInteraction() {
        if (base.isInteracting || this.rotationTween?.IsRunning() == true) {
            return;
        }
        base.ResolveGameManager();
        base.isInteracting = true;
        base.gameManager?.SetCurrentInteractable(this);
        this.ToggleDoor();
    }

    public override void ExitInteraction() {
        if (!base.isInteracting) return;
        this.KillTween();
        this.FinishInteraction();
    }

    private void ToggleDoor() {
        if (this.doorPivot == null) {
            GD.PushWarning($"{Name}: 未找到门旋转节点，无法开关门。");
            this.FinishInteraction();
            return;
        }
        bool targetState = !this.disOpen;
        this.PlaySound(targetState);
        this.AnimateDoor(targetState);
    }

    private void AnimateDoor(bool open) {
        this.disOpen = open;
        this.KillTween();
        var targetRotation = open ? OpenRotationDegrees : ClosedRotationDegrees;
        if (TransitionDuration <= Mathf.Epsilon) {
            this.ApplyDoorRotation(targetRotation);
            this.FinishInteraction();
            return;
        }
        var tween = CreateTween();
        tween.SetParallel(false);
        tween.SetEase(EaseType);
        tween.SetTrans(TransitionType);
        tween.TweenProperty(this.doorPivot, "rotation_degrees", targetRotation, TransitionDuration);
        tween.Finished += OnTweenFinished;
        this.rotationTween = tween;
    }

    private void OnTweenFinished() {
        this.KillTween();
        this.FinishInteraction();
    }

    private void ApplyDoorRotation(Vector3 rotationDegrees) {
        if (this.doorPivot == null) return;
        this.doorPivot.RotationDegrees = rotationDegrees;
    }

    private void ResolveDoorPivot() {
        if (DoorPivotPath.GetNameCount() == 0) {
            this.doorPivot = GetNodeOrNull<Node3D>("walldoor_0012/Node3D");
        } else {
            this.doorPivot = GetNodeOrNull<Node3D>(DoorPivotPath);
        }
        if (this.doorPivot == null) {
            GD.PushWarning($"{Name}: 未找到 DoorPivotPath 指定的节点 {DoorPivotPath}。");
        }
    }

    private void PlaySound(bool opening) {
        var player = opening ? OpenSound : CloseSound;
        if (player == null) return;
        player.Stop();
        player.Play();
    }

    private void KillTween() {
        if (this.rotationTween == null) return;
        this.rotationTween.Finished -= OnTweenFinished;
        this.rotationTween.Kill();
        this.rotationTween = null;
    }

    private void FinishInteraction() {
        base.isInteracting = false;
        base.gameManager?.SetCurrentInteractable(null);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
}
