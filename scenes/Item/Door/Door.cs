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
	private bool isOpen;

	public override void _Ready() {
		base._Ready();
		ResolveDoorPivot();
		isOpen = StartsOpen;
		ApplyDoorRotation(isOpen ? OpenRotationDegrees : ClosedRotationDegrees);
	}

	public override void EnterInteraction() {
		if (isInteracting || rotationTween?.IsRunning() == true) {
			return;
		}
		ResolveGameManager();
		isInteracting = true;
		gameManager?.SetCurrentInteractable(this);
		ToggleDoor();
	}

	public override void ExitInteraction() {
		if (!isInteracting) return;
		KillTween();
		FinishInteraction();
	}

	private void ToggleDoor() {
		if (doorPivot == null) {
			GD.PushWarning($"{Name}: 未找到门旋转节点，无法开关门。");
			FinishInteraction();
			return;
		}
		bool targetState = !isOpen;
		PlaySound(targetState);
		AnimateDoor(targetState);
	}

	private void AnimateDoor(bool open) {
		isOpen = open;
		KillTween();
		var targetRotation = open ? OpenRotationDegrees : ClosedRotationDegrees;
		if (TransitionDuration <= Mathf.Epsilon) {
			ApplyDoorRotation(targetRotation);
			FinishInteraction();
			return;
		}
		var tween = CreateTween();
		tween.SetParallel(false);
		tween.SetEase(EaseType);
		tween.SetTrans(TransitionType);
		tween.TweenProperty(doorPivot, "rotation_degrees", targetRotation, TransitionDuration);
		tween.Finished += OnTweenFinished;
		rotationTween = tween;
	}

	private void OnTweenFinished() {
		KillTween();
		FinishInteraction();
	}

	private void ApplyDoorRotation(Vector3 rotationDegrees) {
		if (doorPivot == null) return;
		doorPivot.RotationDegrees = rotationDegrees;
	}

	private void ResolveDoorPivot() {
		if (DoorPivotPath.GetNameCount() == 0) {
			doorPivot = GetNodeOrNull<Node3D>("walldoor_0012/Node3D");
		} else {
			doorPivot = GetNodeOrNull<Node3D>(DoorPivotPath);
		}
		if (doorPivot == null) {
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
		if (rotationTween == null) return;
		rotationTween.Finished -= OnTweenFinished;
		rotationTween.Kill();
		rotationTween = null;
	}

	private void FinishInteraction() {
		isInteracting = false;
		gameManager?.SetCurrentInteractable(null);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
}
