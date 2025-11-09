using Godot;
public partial class Gramophone : Interactable {
	[Export] public NodePath AudioPlayerPath { get; set; } = new("AudioStreamPlayer3D");
	[Export] public string PlayActionText { get; set; } = "播放音乐";
	[Export] public string StopActionText { get; set; } = "暂停音乐";
	[Export] public bool Loop { get; set; } = true;

	private AudioStreamPlayer3D audioPlayer;
	private bool audioSignalsConnected;
	private bool isPlaying;

	public override void _Ready() {
		base._Ready();
		ResolveAudioPlayer();
		UpdateLoopSetting();
		RefreshState();
		UpdateActionLabel();
	}

	public override void _ExitTree() {
		base._ExitTree();
		DisconnectAudioSignals();
	}

	public override void EnterInteraction() {
		if (audioPlayer == null) return;

		if (IsAudioPlaying()) {
			audioPlayer.Stop();
		} else {
			audioPlayer.Play();
		}

		RefreshState();
		UpdateActionLabel();
	}

	public override void OnFocusEnter() {
		UpdateActionLabel();
		base.OnFocusEnter();
	}

	private void ResolveAudioPlayer() {
		var previousPlayer = audioPlayer;

		AudioStreamPlayer3D resolvedPlayer = null;
		if (AudioPlayerPath.GetNameCount() == 0) {
			resolvedPlayer = GetNodeOrNull<AudioStreamPlayer3D>("AudioStreamPlayer3D");
		} else {
			resolvedPlayer = GetNodeOrNull<AudioStreamPlayer3D>(AudioPlayerPath);
			if (resolvedPlayer == null) {
				resolvedPlayer = GetTree().Root.GetNodeOrNull<AudioStreamPlayer3D>(AudioPlayerPath);
			}
		}

		if (previousPlayer != null && previousPlayer != resolvedPlayer) {
			DisconnectAudioSignals(previousPlayer);
		}

		audioPlayer = resolvedPlayer;

		if (audioPlayer == null) {
			GD.PushWarning($"{Name}: 未找到 AudioStreamPlayer3D 节点 {AudioPlayerPath}。");
			return;
		}

		ConnectAudioSignals();
	}

	private void ConnectAudioSignals() {
		if (audioPlayer == null || audioSignalsConnected) return;

		audioPlayer.Finished += OnAudioFinished;
		audioSignalsConnected = true;
	}

	private void DisconnectAudioSignals(AudioStreamPlayer3D targetPlayer = null) {
		var player = targetPlayer ?? audioPlayer;
		if (player == null || !audioSignalsConnected) return;

		player.Finished -= OnAudioFinished;
		audioSignalsConnected = false;
	}

	private void UpdateLoopSetting() {
		if (audioPlayer == null || audioPlayer.Stream == null) return;

		var duplicatedStream = audioPlayer.Stream.Duplicate() as AudioStream;
		if (duplicatedStream == null) {
			GD.PushWarning($"{Name}: 无法复制音频流资源，循环设置可能无效。");
			return;
		}

		bool loopApplied = false;

		if (duplicatedStream is AudioStreamWav wavStream) {
			wavStream.LoopMode = Loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
			loopApplied = true;
		} else if (duplicatedStream is AudioStreamOggVorbis oggStream) {
			oggStream.Loop = Loop;
			loopApplied = true;
		} else if (duplicatedStream is AudioStreamMP3 mp3Stream) {
			mp3Stream.Loop = Loop;
			loopApplied = true;
		}

		if (!loopApplied) {
			GD.PushWarning($"{Name}: 当前音频流类型 {duplicatedStream.GetClass()} 不支持自动循环设置。");
		}

		audioPlayer.Stream = duplicatedStream;
	}

	private void OnAudioFinished() {
		if (!Loop) {
			isPlaying = false;
			UpdateActionLabel();
		}
	}

	private bool IsAudioPlaying() {
		return audioPlayer != null && audioPlayer.Playing;
	}

	private void RefreshState() {
		isPlaying = IsAudioPlaying();
	}

	private void UpdateActionLabel() {
		ActionName = isPlaying ? StopActionText : PlayActionText;
		if (isFocus && nameLabel != null) {
			nameLabel.Text = $"[E] {ActionName}";
		}
	}
}

