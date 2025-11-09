using Godot;

public partial class MoviePlayer : Interactable {
	[Export] public NodePath VideoPlayerPath { get; set; } = new NodePath("../SubViewport/SubViewportContainer/VideoStreamPlayer");
	[Export] public bool ResetOnStop { get; set; } = true;
	[Export] public string PlayActionText { get; set; } = "播放影片";
	[Export] public string StopActionText { get; set; } = "关闭影片";

	private VideoStreamPlayer videoPlayer;
	private bool isPlaying;

	public override void _Ready() {
		base._Ready();
		ResolveVideoPlayer();
		RefreshPlaybackState();
		UpdateActionLabel();
	}

	public override void _ExitTree() {
		base._ExitTree();
		if (videoPlayer != null) {
			videoPlayer.Finished -= OnVideoFinished;
		}
	}

	public override void EnterInteraction() {
		if (isInteracting) return;
		TogglePlayback();
	}

	public override void OnFocusEnter() {
		UpdateActionLabel();
		base.OnFocusEnter();
	}

	private void ResolveVideoPlayer() {
		if (VideoPlayerPath.IsEmpty) {
			videoPlayer = null;
		} else {
			videoPlayer = GetNodeOrNull<VideoStreamPlayer>(VideoPlayerPath);
			if (videoPlayer == null) {
				videoPlayer = GetTree().Root.GetNodeOrNull<VideoStreamPlayer>(VideoPlayerPath);
			}
		}

		if (videoPlayer == null) {
			GD.PushWarning($"{Name}: 未找到 VideoStreamPlayer 节点 {VideoPlayerPath}。");
			return;
		}

		videoPlayer.Autoplay = false;
		videoPlayer.Stop();
		if (ResetOnStop) {
			videoPlayer.StreamPosition = 0;
		}
		videoPlayer.Finished += OnVideoFinished;
	}

	private void TogglePlayback() {
		if (videoPlayer == null) return;

		if (videoPlayer.IsPlaying()) {
			videoPlayer.Stop();
			if (ResetOnStop) {
				videoPlayer.StreamPosition = 0;
			}
			isPlaying = false;
		} else {
			videoPlayer.Play();
			isPlaying = true;
		}

		UpdateActionLabel();
	}

	private void OnVideoFinished() {
		isPlaying = false;
		if (ResetOnStop && videoPlayer != null) {
			videoPlayer.StreamPosition = 0;
		}
		UpdateActionLabel();
	}

	private void RefreshPlaybackState() {
		isPlaying = videoPlayer != null && videoPlayer.IsPlaying();
	}

	private void UpdateActionLabel() {
		ActionName = isPlaying ? StopActionText : PlayActionText;
		if (isFocus && nameLabel != null) {
			nameLabel.Text = $"[E] {ActionName}";
		}
	}
}
