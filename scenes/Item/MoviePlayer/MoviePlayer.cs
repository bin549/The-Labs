using Godot;

public partial class MoviePlayer : Interactable {
	[Export] public NodePath VideoPlayerPath { get; set; } = new NodePath("../SubViewport/SubViewportContainer/VideoStreamPlayer");
	[Export] public bool ResetOnStop { get; set; } = true;
	[Export] public string PlayActionText { get; set; } = "播放影片";
	[Export] public string StopActionText { get; set; } = "关闭影片";
	[Export] public Godot.Collections.Array<VideoStream> VideoStreams { get; set; } = new();
	[Export] public int DefaultStreamIndex { get; set; } = 0;

	private VideoStreamPlayer videoPlayer;
	private bool isPlaying;
	private int currentStreamIndex = -1;
	private VideoStream originalStream;

	public override void _Ready() {
		base._Ready();
		ResolveVideoPlayer();
		InitializePlaylist();
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

	public override void _Process(double delta) {
		base._Process(delta);
		if (!isInteracting || !isPlaying || videoPlayer == null) return;
		if (Input.IsActionJustPressed("ui_left")) {
			SwitchVideo(-1);
		} else if (Input.IsActionJustPressed("ui_right")) {
			SwitchVideo(1);
		}
	}

	public override void OnFocusEnter() {
		UpdateActionLabel();
		base.OnFocusEnter();
	}

	private void ResolveVideoPlayer() {
		var previousPlayer = videoPlayer;
		if (VideoPlayerPath.IsEmpty) {
			videoPlayer = null;
		} else {
			videoPlayer = GetNodeOrNull<VideoStreamPlayer>(VideoPlayerPath);
			if (videoPlayer == null) {
				videoPlayer = GetTree().Root.GetNodeOrNull<VideoStreamPlayer>(VideoPlayerPath);
			}
		}
		if (previousPlayer != null && previousPlayer != videoPlayer) {
			DisconnectVideoSignals(previousPlayer);
		}
		if (videoPlayer == null) {
			GD.PushWarning($"{Name}: 未找到 VideoStreamPlayer 节点 {VideoPlayerPath}。");
			return;
		}
		ConnectVideoSignals();
		videoPlayer.Autoplay = false;
		videoPlayer.Stop();
		if (ResetOnStop) {
			videoPlayer.StreamPosition = 0;
		}
		originalStream = videoPlayer.Stream;
	}

	private void TogglePlayback() {
		if (videoPlayer == null) return;
		if (videoPlayer.IsPlaying()) {
			videoPlayer.Stop();
			if (ResetOnStop) {
				videoPlayer.StreamPosition = 0;
			}
			isPlaying = false;
			isInteracting = false;
		} else {
			PrepareStreamForPlayback();
			videoPlayer.Play();
			isPlaying = true;
			isInteracting = true;
		}
		UpdateActionLabel();
	}

	private void OnVideoFinished() {
		isPlaying = false;
		isInteracting = false;
		if (ResetOnStop && videoPlayer != null) {
			videoPlayer.StreamPosition = 0;
		}
		UpdateActionLabel();
	}

	private void RefreshPlaybackState() {
		isPlaying = videoPlayer != null && videoPlayer.IsPlaying();
		isInteracting = isPlaying;
	}

	private void UpdateActionLabel() {
		ActionName = isPlaying ? StopActionText : PlayActionText;
		if (isFocus && nameLabel != null) {
			nameLabel.Text = $"[E] {ActionName}";
		}
	}

	private bool videoSignalsConnected;

	private void ConnectVideoSignals() {
		if (videoPlayer == null || videoSignalsConnected) return;
		videoPlayer.Finished += OnVideoFinished;
		videoSignalsConnected = true;
	}

	private void DisconnectVideoSignals(VideoStreamPlayer target = null) {
		var player = target ?? videoPlayer;
		if (player == null || !videoSignalsConnected) return;
		player.Finished -= OnVideoFinished;
		videoSignalsConnected = false;
	}

	private void InitializePlaylist() {
		if (videoPlayer == null) return;
		if (VideoStreams != null && VideoStreams.Count > 0) {
			var targetIndex = WrapIndex(DefaultStreamIndex, VideoStreams.Count);
			if (AssignStream(targetIndex)) {
				videoPlayer.Stop();
			}
			isPlaying = false;
			isInteracting = false;
		} else {
			currentStreamIndex = -1;
		}
	}

	private void PrepareStreamForPlayback() {
		if (videoPlayer == null) return;
		if (VideoStreams != null && VideoStreams.Count > 0) {
			var targetIndex = currentStreamIndex >= 0 ? currentStreamIndex : WrapIndex(DefaultStreamIndex, VideoStreams.Count);
			AssignStream(targetIndex, true);
		} else if (videoPlayer.Stream == null && originalStream != null) {
			videoPlayer.Stream = originalStream;
			if (ResetOnStop) {
				videoPlayer.StreamPosition = 0;
			}
		} else if (ResetOnStop) {
			videoPlayer.StreamPosition = 0;
		}
	}

	private void SwitchVideo(int direction) {
		if (videoPlayer == null || VideoStreams == null || VideoStreams.Count <= 1) return;
		var baseIndex = currentStreamIndex >= 0 ? currentStreamIndex : WrapIndex(DefaultStreamIndex, VideoStreams.Count);
		var nextIndex = WrapIndex(baseIndex + direction, VideoStreams.Count);
		bool wasPlaying = videoPlayer.IsPlaying();
		AssignStream(nextIndex);
		if (wasPlaying || isPlaying) {
			videoPlayer.Play();
			isPlaying = true;
			isInteracting = true;
		} else {
			isPlaying = false;
			isInteracting = false;
		}
		UpdateActionLabel();
	}

	private bool AssignStream(int index, bool resetPosition = true) {
		if (videoPlayer == null || VideoStreams == null || VideoStreams.Count == 0) return false;
		int wrappedIndex = WrapIndex(index, VideoStreams.Count);
		var stream = VideoStreams[wrappedIndex];
		if (stream == null) {
			GD.PushWarning($"{Name}: 播放列表中的 VideoStream（索引 {wrappedIndex}）为空。");
			return false;
		}
		bool wasPlaying = videoPlayer.IsPlaying();
		bool needUpdate = currentStreamIndex != wrappedIndex || videoPlayer.Stream != stream;
		if (needUpdate && wasPlaying) {
			videoPlayer.Stop();
		}
		if (needUpdate) {
			videoPlayer.Stream = stream;
		}
		if (resetPosition || ResetOnStop) {
			videoPlayer.StreamPosition = 0;
		}
		currentStreamIndex = wrappedIndex;
		return wasPlaying;
	}

	private static int WrapIndex(int index, int count) {
		if (count <= 0) return 0;
		int result = index % count;
		if (result < 0) {
			result += count;
		}
		return result;
	}
}
