using Godot;

public partial class Door : Node3D {
    [Export] public NodePath DoorPivotPath { get; set; } = new("walldoor_0012/Node3D");
    [Export] public NodePath TriggerAreaPath { get; set; } = new("Area3D");
    [Export] public Vector3 ClosedRotationDegrees { get; set; } = Vector3.Zero;
    [Export] public Vector3 OpenRotationDegrees { get; set; } = new Vector3(0f, -90f, 0f);
    [Export] public float TransitionDuration { get; set; } = 0.6f;
    [Export] public Tween.TransitionType TransitionType { get; set; } = Tween.TransitionType.Sine;
    [Export] public Tween.EaseType EaseType { get; set; } = Tween.EaseType.InOut;
    [Export] public bool StartsOpen { get; set; } = false;
    [Export] public AudioStreamPlayer3D OpenSound { get; set; }
    [Export] public AudioStreamPlayer3D CloseSound { get; set; }
    private Node3D doorPivot;
    private Area3D triggerArea;
    private Tween rotationTween;
    private bool disOpen;
    private int playerCount = 0;

    public override void _Ready() {
        this.ResolveDoorPivot();
        this.ResolveTriggerArea();
        this.disOpen = this.StartsOpen;
        this.ApplyDoorRotation(this.disOpen ? OpenRotationDegrees : ClosedRotationDegrees);
    }

    private void ResolveTriggerArea() {
        if (TriggerAreaPath.GetNameCount() == 0) {
            this.triggerArea = GetNodeOrNull<Area3D>("Area3D");
        } else {
            this.triggerArea = GetNodeOrNull<Area3D>(TriggerAreaPath);
        }
        if (this.triggerArea != null) {
            this.triggerArea.BodyEntered += OnBodyEntered;
            this.triggerArea.BodyExited += OnBodyExited;
        }
    }

    private void OnBodyEntered(Node3D body) {
        if (body is CharacterBody3D) {
            this.playerCount++;
            if (this.playerCount == 1 && !this.disOpen) {
                this.OpenDoor();
            }
        }
    }

    private void OnBodyExited(Node3D body) {
        if (body is CharacterBody3D) {
            this.playerCount--;
            if (this.playerCount <= 0) {
                this.playerCount = 0;
                if (this.disOpen) {
                    this.CloseDoor();
                }
            }
        }
    }

    private void OpenDoor() {
        if (this.doorPivot == null || this.rotationTween?.IsRunning() == true) {
            return;
        }
        this.PlaySound(true);
        this.AnimateDoor(true);
    }

    private void CloseDoor() {
        if (this.doorPivot == null || this.rotationTween?.IsRunning() == true) {
            return;
        }
        this.PlaySound(false);
        this.AnimateDoor(false);
    }

    private void AnimateDoor(bool open) {
        this.disOpen = open;
        this.KillTween();
        var targetRotation = open ? OpenRotationDegrees : ClosedRotationDegrees;
        if (TransitionDuration <= Mathf.Epsilon) {
            this.ApplyDoorRotation(targetRotation);
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
}
