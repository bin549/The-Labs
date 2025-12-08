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
        this.ResolveAudioPlayer();
        this.UpdateLoopSetting();
        this.RefreshState();
        this.UpdateActionLabel();
    }

    public override void _ExitTree() {
        base._ExitTree();
        this.DisconnectAudioSignals();
    }

    public override void EnterInteraction() {
        if (this.audioPlayer == null) return;
        if (IsAudioPlaying()) {
            this.audioPlayer.Stop();
        } else {
            this.audioPlayer.Play();
        }
        this.RefreshState();
        this.UpdateActionLabel();
    }

    public override void OnFocusEnter() {
        this.UpdateActionLabel();
        base.OnFocusEnter();
    }

    private void ResolveAudioPlayer() {
        var previousPlayer = this.audioPlayer;
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
            this.DisconnectAudioSignals(previousPlayer);
        }
        this.audioPlayer = resolvedPlayer;
        if (this.audioPlayer == null) {
            return;
        }
        this.ConnectAudioSignals();
    }

    private void ConnectAudioSignals() {
        if (this.audioPlayer == null || this.audioSignalsConnected) return;
        this.audioPlayer.Finished += OnAudioFinished;
        this.audioSignalsConnected = true;
    }

    private void DisconnectAudioSignals(AudioStreamPlayer3D targetPlayer = null) {
        var player = targetPlayer ?? this.audioPlayer;
        if (player == null || !this.audioSignalsConnected) return;
        player.Finished -= OnAudioFinished;
        this.audioSignalsConnected = false;
    }

    private void UpdateLoopSetting() {
        if (this.audioPlayer == null || this.audioPlayer.Stream == null) return;
        var duplicatedStream = this.audioPlayer.Stream.Duplicate() as AudioStream;
        if (duplicatedStream == null) {
            return;
        }
        if (duplicatedStream is AudioStreamWav wavStream) {
            wavStream.LoopMode = this.Loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
        } else if (duplicatedStream is AudioStreamOggVorbis oggStream) {
            oggStream.Loop = this.Loop;
        } else if (duplicatedStream is AudioStreamMP3 mp3Stream) {
            mp3Stream.Loop = this.Loop;
        }
        this.audioPlayer.Stream = duplicatedStream;
    }

    private void OnAudioFinished() {
        if (!this.Loop) {
            this.isPlaying = false;
            this.UpdateActionLabel();
        }
    }

    private bool IsAudioPlaying() {
        return this.audioPlayer != null && this.audioPlayer.Playing;
    }

    private void RefreshState() {
        this.isPlaying = this.IsAudioPlaying();
    }

    private void UpdateActionLabel() {
        ActionName = this.isPlaying ? StopActionText : PlayActionText;
        if (isFocus && base.nameLabel != null) {
            base.nameLabel.Text = $"[E] {ActionName}";
        }
    }
}