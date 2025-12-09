using System.Collections.Generic;
using System.IO;
using Godot;

public partial class MoviePlayer : Interactable {
    [Export]
    public NodePath VideoPlayerPath { get; set; } =
        new NodePath("../SubViewport/SubViewportContainer/VideoStreamPlayer");
    [Export] public bool ResetOnStop { get; set; } = true;
    [Export] public bool HideScreenOnStop { get; set; } = true;
    [Export] public string PlayActionText { get; set; } = "播放影片";
    [Export] public string StopActionText { get; set; } = "关闭影片";
    [Export] public Godot.Collections.Array<VideoStream> VideoStreams { get; set; } = new();
    [Export] public int DefaultStreamIndex { get; set; } = 0;
    [Export] public NodePath PlaylistPanelPath { get; set; } = new NodePath("PlaylistUI/Panel");
    [Export] public NodePath PlaylistButtonContainerPath { get; set; } = new NodePath("PlaylistUI/Panel/VBoxContainer");
    private VideoStreamPlayer videoPlayer;
    private bool isPlaying;
    private bool isPaused;
    private int currentStreamIndexh = -1;
    private VideoStream originalStream;
    private Control playlistPanel;
    private Container playlistButtonContainer;
    private readonly List<Button> playlistButtons = new();
    private bool isPlaylistVisible;
    private bool isToggleKeyHeld;

    public override void _Ready() {
        base._Ready();
        this.ResolveVideoPlayer();
        this.InitializePlaylist();
        this.ResolvePlaylistUI();
        this.RefreshPlaybackState();
        this.UpdateActionLabel();
        if (!this.isPlaying) {
            this.ClearVideoFrame();
        }
        this.UpdateScreenVisibility();
    }

    public override void _ExitTree() {
        base._ExitTree();
        if (this.videoPlayer != null) {
            this.videoPlayer.Finished -= OnVideoFinished;
        }
    }

    public override void EnterInteraction() {
        this.TogglePlayback();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (!base.isInteracting || this.videoPlayer == null) return;
        if (Input.IsKeyPressed(Key.Escape)) {
            this.HidePlaylistUI();
        }
        bool togglePressed = Input.IsKeyPressed(Key.U);
        if (togglePressed && !this.isToggleKeyHeld) {
            if (this.isPlaying || this.isPaused) {
                this.TogglePlaylistUI(!this.isPlaylistVisible);
            }
        } else if (!togglePressed && this.isToggleKeyHeld) {
            this.isToggleKeyHeld = false;
        }
        if (togglePressed) {
            this.isToggleKeyHeld = true;
        }
        if (this.isPlaylistVisible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        if (this.isPlaying) {
            if (Input.IsActionJustPressed("ui_down")) {
                PausePlayback();
            } else if (Input.IsActionJustPressed("ui_left")) {
                this.SwitchVideo(-1);
            } else if (Input.IsActionJustPressed("ui_right")) {
                this.SwitchVideo(1);
            }
        } else if (this.isPaused) {
            if (Input.IsActionJustPressed("ui_up")) {
                this.ResumePlayback();
            } else if (Input.IsActionJustPressed("ui_left")) {
                this.SwitchVideo(-1);
            } else if (Input.IsActionJustPressed("ui_right")) {
                this.SwitchVideo(1);
            }
        }
    }

    public override void OnFocusEnter() {
        this.UpdateActionLabel();
        base.OnFocusEnter();
    }

    private void ResolveVideoPlayer() {
        var previousPlayer = this.videoPlayer;
        if (VideoPlayerPath.IsEmpty) {
            this.videoPlayer = null;
        } else {
            this.videoPlayer = GetNodeOrNull<VideoStreamPlayer>(VideoPlayerPath);
            if (this.videoPlayer == null) {
                this.videoPlayer = GetTree().Root.GetNodeOrNull<VideoStreamPlayer>(VideoPlayerPath);
            }
        }
        if (previousPlayer != null && previousPlayer != this.videoPlayer) {
            this.DisconnectVideoSignals(previousPlayer);
        }
        if (this.videoPlayer == null) {
            return;
        }
        this.ConnectVideoSignals();
        this.videoPlayer.Autoplay = false;
        this.videoPlayer.Stop();
        if (ResetOnStop) {
            this.videoPlayer.StreamPosition = 0;
        }
        this.originalStream = this.videoPlayer.Stream;
    }

    private void TogglePlayback() {
        if (this.videoPlayer == null) return;
        if (this.isPaused) {
            this.ResumePlayback();
            return;
        }
        if (this.videoPlayer.IsPlaying()) {
            this.StopPlayback();
            return;
        }
        this.PrepareStreamForPlayback();
        this.videoPlayer.Play();
        this.isPlaying = true;
        this.isPaused = false;
        base.isInteracting = true;
        this.UpdateActionLabel();
        this.UpdateScreenVisibility();
    }

    private void StopPlayback() {
        if (this.videoPlayer == null) return;
        bool wasPlaying = this.videoPlayer.IsPlaying() || this.isPlaying;
        if (!wasPlaying && !this.isPaused) return;
        this.videoPlayer.Stop();
        this.videoPlayer.Paused = false;
        this.isPlaying = false;
        this.isPaused = false;
        base.isInteracting = false;
        this.ClearVideoFrame();
        this.HidePlaylistUI();
        this.UpdateActionLabel();
        this.UpdateScreenVisibility();
    }

    private void PausePlayback() {
        if (this.videoPlayer == null || this.isPaused) return;
        if (!(this.videoPlayer.IsPlaying() || this.isPlaying)) return;
        this.videoPlayer.Paused = true;
        this.isPlaying = false;
        this.isPaused = true;
        base.isInteracting = true;
        this.UpdateActionLabel();
        this.UpdateScreenVisibility();
    }

    private void ResumePlayback() {
        if (this.videoPlayer == null || !this.isPaused) return;
        this.videoPlayer.Paused = false;
        if (!this.videoPlayer.IsPlaying()) {
            this.videoPlayer.Play();
        }
        this.isPlaying = true;
        this.isPaused = false;
        base.isInteracting = true;
        this.UpdateActionLabel();
        this.UpdateScreenVisibility();
    }

    private void OnVideoFinished() {
        this.StopPlayback();
    }

    private void RefreshPlaybackState() {
        if (this.videoPlayer == null) {
            this.isPlaying = false;
            this.isPaused = false;
            base.isInteracting = false;
        } else {
            bool playing = this.videoPlayer.IsPlaying();
            bool paused = playing && this.videoPlayer.Paused;
            this.isPlaying = playing && !paused;
            this.isPaused = paused;
            base.isInteracting = this.isPlaying || this.isPaused;
        }
        this.UpdateScreenVisibility();
    }

    private void UpdateActionLabel() {
        base.ActionName = this.isPlaying ? StopActionText : PlayActionText;
        if (isFocus && nameLabel != null) {
            nameLabel.Text = $"[E] {base.ActionName}";
        }
    }

    private bool videoSignalsConnected;

    private void ConnectVideoSignals() {
        if (this.videoPlayer == null || this.videoSignalsConnected) return;
        this.videoPlayer.Finished += OnVideoFinished;
        this.videoSignalsConnected = true;
    }

    private void DisconnectVideoSignals(VideoStreamPlayer target = null) {
        var player = target ?? this.videoPlayer;
        if (player == null || !this.videoSignalsConnected) return;
        player.Finished -= OnVideoFinished;
        this.videoSignalsConnected = false;
    }

    private void InitializePlaylist() {
        if (this.videoPlayer == null) return;
        if (this.VideoStreams != null && this.VideoStreams.Count > 0) {
            var targetIndex = WrapIndex(DefaultStreamIndex, this.VideoStreams.Count);
            if (this.AssignStream(targetIndex)) {
                this.videoPlayer.Stop();
            }
            this.isPlaying = false;
            this.isPaused = false;
            base.isInteracting = false;
        } else {
            this.currentStreamIndexh = -1;
        }
        this.UpdatePlaylistSelection();
        this.HidePlaylistUI();
    }

    private void PrepareStreamForPlayback() {
        if (this.videoPlayer == null) return;
        if (this.HideScreenOnStop) {
            this.videoPlayer.Visible = true;
        }
        this.videoPlayer.Paused = false;
        this.isPaused = false;
        if (this.VideoStreams != null && this.VideoStreams.Count > 0) {
            var targetIndex = this.currentStreamIndexh >= 0
                ? this.currentStreamIndexh
                : WrapIndex(DefaultStreamIndex, this.VideoStreams.Count);
            this.AssignStream(targetIndex, true);
        } else if (this.videoPlayer.Stream == null && this.originalStream != null) {
            this.videoPlayer.Stream = this.originalStream;
            if (ResetOnStop) {
                this.videoPlayer.StreamPosition = 0;
            }
        } else if (ResetOnStop) {
            this.videoPlayer.StreamPosition = 0;
        }
    }

    private void SwitchVideo(int direction) {
        if (this.videoPlayer == null || this.VideoStreams == null || this.VideoStreams.Count <= 1) return;
        var baseIndex = this.currentStreamIndexh >= 0
            ? this.currentStreamIndexh
            : WrapIndex(DefaultStreamIndex, this.VideoStreams.Count);
        var nextIndex = WrapIndex(baseIndex + direction, this.VideoStreams.Count);
        bool wasPlaying = this.videoPlayer.IsPlaying();
        bool wasPaused = this.isPaused || (wasPlaying && this.videoPlayer.Paused);
        this.AssignStream(nextIndex);
        if (wasPaused) {
            this.videoPlayer.Play();
            this.videoPlayer.Paused = true;
            this.isPlaying = false;
            this.isPaused = true;
            base.isInteracting = true;
        } else if (wasPlaying || this.isPlaying) {
            this.videoPlayer.Play();
            this.videoPlayer.Paused = false;
            this.isPlaying = true;
            this.isPaused = false;
            base.isInteracting = true;
        } else {
            this.isPlaying = false;
            this.isPaused = false;
            base.isInteracting = false;
        }
        this.UpdatePlaylistSelection();
        this.UpdateActionLabel();
        this.UpdateScreenVisibility();
    }

    private bool AssignStream(int index, bool resetPosition = true) {
        if (this.videoPlayer == null || this.VideoStreams == null || this.VideoStreams.Count == 0) return false;
        int wrappedIndex = WrapIndex(index, this.VideoStreams.Count);
        var stream = this.VideoStreams[wrappedIndex];
        if (stream == null) {
            return false;
        }
        bool wasPlaying = this.videoPlayer.IsPlaying();
        bool needUpdate = this.currentStreamIndexh != wrappedIndex || this.videoPlayer.Stream != stream;
        if (needUpdate && wasPlaying) {
            this.videoPlayer.Stop();
        }
        if (needUpdate) {
            this.videoPlayer.Stream = stream;
        }
        if (resetPosition || ResetOnStop) {
            this.videoPlayer.StreamPosition = 0;
        }
        this.currentStreamIndexh = wrappedIndex;
        this.UpdatePlaylistSelection();
        return wasPlaying;
    }

    private void ClearVideoFrame() {
        if (this.videoPlayer == null) return;
        if (ResetOnStop) {
            this.videoPlayer.StreamPosition = 0;
        }
        this.videoPlayer.Paused = false;
        this.isPaused = false;
        if (!this.HideScreenOnStop) return;
        if ((this.VideoStreams != null && this.VideoStreams.Count > 0) || this.originalStream != null) {
            this.videoPlayer.Stream = null;
        }
        this.videoPlayer.Visible = false;
        this.HidePlaylistUI();
    }

    private void UpdateScreenVisibility() {
        if (this.videoPlayer == null) return;
        if (this.HideScreenOnStop) {
            this.videoPlayer.Visible = this.isPlaying || this.isPaused;
        } else {
            this.videoPlayer.Visible = true;
        }
    }

    private void ResolvePlaylistUI() {
        this.playlistPanel = this.PlaylistPanelPath.IsEmpty ? null : GetNodeOrNull<Control>(this.PlaylistPanelPath);
        if (this.playlistPanel == null) {
            CreateRuntimePlaylistUI();
        }
        if (this.playlistPanel == null) return;
        playlistButtonContainer = PlaylistButtonContainerPath.IsEmpty
            ? null
            : GetNodeOrNull<Container>(PlaylistButtonContainerPath);
        if (playlistButtonContainer == null && this.playlistPanel != null) {
            playlistButtonContainer = this.playlistPanel as Container;
            if (playlistButtonContainer == null) {
                playlistButtonContainer = this.playlistPanel.GetNodeOrNull<Container>("VBoxContainer");
            }
        }
        if (playlistButtonContainer == null) {
            this.CreateRuntimePlaylistContainer();
        }
        this.BuildPlaylistButtons();
        this.HidePlaylistUI();
    }

    private void BuildPlaylistButtons() {
        foreach (var button in this.playlistButtons) {
            if (button != null && IsInstanceValid(button)) {
                button.QueueFree();
            }
        }
        this.playlistButtons.Clear();
        if (playlistButtonContainer == null) return;
        foreach (Node child in playlistButtonContainer.GetChildren()) {
            child.QueueFree();
        }
        if (this.VideoStreams == null || this.VideoStreams.Count == 0) {
            var emptyLabel = new Label {
                Text = "暂无可播放影片",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = Control.SizeFlags.Fill
            };
            playlistButtonContainer.AddChild(emptyLabel);
            return;
        }
        var title = new Label {
            Text = "选择影片",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.Fill
        };
        playlistButtonContainer.AddChild(title);
        playlistButtonContainer.AddChild(new HSeparator());
        for (int i = 0; i < this.VideoStreams.Count; i++) {
            var button = new Button {
                Text = GetStreamDisplayName(i),
                SizeFlagsHorizontal = Control.SizeFlags.Fill,
                ToggleMode = true
            };
            int index = i;
            button.Pressed += () => OnPlaylistButtonPressed(index);
            button.MouseEntered += () => UpdateHoverDescription(index);
            button.FocusMode = Control.FocusModeEnum.All;
            playlistButtonContainer.AddChild(button);
            this.playlistButtons.Add(button);
        }
        this.UpdatePlaylistSelection();
    }

    private string GetStreamDisplayName(int index) {
        var stream = this.VideoStreams[index];
        if (stream == null) return $"影片 {index + 1}";
        if (!string.IsNullOrEmpty(stream.ResourceName)) {
            return stream.ResourceName;
        }
        if (!string.IsNullOrEmpty(stream.ResourcePath)) {
            return Path.GetFileNameWithoutExtension(stream.ResourcePath);
        }
        return $"{stream.GetType().Name} {index + 1}";
    }

    private void OnPlaylistButtonPressed(int index) {
        this.PlayStreamAt(index);
    }

    private void PlayStreamAt(int index) {
        if (this.videoPlayer == null || this.VideoStreams == null || this.VideoStreams.Count == 0) return;
        if (index < 0 || index >= this.VideoStreams.Count) return;
        if (this.HideScreenOnStop) {
            this.videoPlayer.Visible = true;
        }
        this.videoPlayer.Paused = false;
        this.isPaused = false;
        this.AssignStream(index, true);
        this.videoPlayer.Play();
        this.isPlaying = true;
        base.isInteracting = true;
        this.UpdateActionLabel();
        this.UpdateScreenVisibility();
        this.UpdatePlaylistSelection();
        this.HidePlaylistUI();
    }

    private void UpdateHoverDescription(int index) {
        if (this.VideoStreams == null || index < 0 || index >= this.VideoStreams.Count) return;
        var stream = this.VideoStreams[index];
        if (stream == null) return;
        var tooltip = GetStreamDisplayName(index);
        if (!string.IsNullOrEmpty(stream.ResourcePath)) {
            tooltip += $"\n{stream.ResourcePath}";
        }
        if (this.playlistPanel != null) {
            this.playlistPanel.TooltipText = tooltip;
        }
    }

    private void SetGameInteractionLock(bool enabled) {
        if (base.gameManager == null || !GodotObject.IsInstanceValid(base.gameManager)) return;
        if (enabled) {
            if (base.gameManager.currentInteractable != this) {
                base.gameManager.SetCurrentInteractable(this);
            }
        } else if (base.gameManager.currentInteractable == this) {
            base.gameManager.SetCurrentInteractable(null);
        }
    }

    private void TogglePlaylistUI(bool visible) {
        if (this.playlistPanel == null) return;
        if (this.isPlaylistVisible == visible) return;
        if (visible) {
            this.ShowPlaylistUI();
        } else {
            this.HidePlaylistUI();
        }
    }

    private void ShowPlaylistUI() {
        if (this.playlistPanel == null) return;
        this.BuildPlaylistButtons();
        this.playlistPanel.Visible = true;
        this.isPlaylistVisible = true;
        this.SetGameInteractionLock(true);
        this.UpdatePlaylistSelection();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetViewport().SetInputAsHandled();
    }

    private void HidePlaylistUI() {
        if (this.playlistPanel == null) return;
        this.playlistPanel.Visible = false;
        this.isPlaylistVisible = false;
        this.SetGameInteractionLock(false);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void UpdatePlaylistSelection() {
        if (this.playlistButtons.Count == 0) return;
        for (int i = 0; i < this.playlistButtons.Count; i++) {
            var button = this.playlistButtons[i];
            if (button == null || !IsInstanceValid(button)) continue;
            button.ButtonPressed = (i == this.currentStreamIndexh && this.currentStreamIndexh >= 0);
        }
    }

    private void CreateRuntimePlaylistUI() {
        var canvasLayer = GetNodeOrNull<CanvasLayer>("PlaylistUI");
        if (canvasLayer == null) {
            canvasLayer = new CanvasLayer {
                Name = "PlaylistUI",
                Layer = 1
            };
            AddChild(canvasLayer);
        }
        var panel = canvasLayer.GetNodeOrNull<Panel>("Panel");
        if (panel == null) {
            panel = new Panel {
                Name = "Panel",
                Visible = false
            };
            panel.SetAnchorsPreset(Control.LayoutPreset.Center);
            panel.AnchorLeft = 0.5f;
            panel.AnchorTop = 0.5f;
            panel.AnchorRight = 0.5f;
            panel.AnchorBottom = 0.5f;
            panel.OffsetLeft = -240;
            panel.OffsetTop = -160;
            panel.OffsetRight = 240;
            panel.OffsetBottom = 160;
            panel.CustomMinimumSize = new Vector2(480, 320);
            canvasLayer.AddChild(panel);
        }
        this.playlistPanel = panel;
        this.CreateRuntimePlaylistContainer();
        this.PlaylistPanelPath = this.playlistPanel.GetPath();
        if (playlistButtonContainer != null) {
            PlaylistButtonContainerPath = playlistButtonContainer.GetPath();
        }
    }

    private void CreateRuntimePlaylistContainer() {
        if (this.playlistPanel == null) return;
        playlistButtonContainer = this.playlistPanel.GetNodeOrNull<Container>("VBoxContainer");
        if (playlistButtonContainer != null) return;
        var vbox = new VBoxContainer {
            Name = "VBoxContainer"
        };
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.OffsetLeft = 16;
        vbox.OffsetTop = 16;
        vbox.OffsetRight = -16;
        vbox.OffsetBottom = -16;
        vbox.AddThemeConstantOverride("separation", 12);
        this.playlistPanel.AddChild(vbox);
        playlistButtonContainer = vbox;
    }

    private void BeginInteractionSession() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void EndInteractionSession() {
        if (base.gameManager != null) {
            base.gameManager.SetCurrentInteractable(null);
        }
        Input.MouseMode = Input.MouseModeEnum.Captured;
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