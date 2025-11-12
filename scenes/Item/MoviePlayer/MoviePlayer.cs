using System.Collections.Generic;
using System.IO;
using Godot;

public partial class MoviePlayer : Interactable {
	[Export] public NodePath VideoPlayerPath { get; set; } = new NodePath("../SubViewport/SubViewportContainer/VideoStreamPlayer");
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
	private int currentStreamIndex = -1;
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
			ClearVideoFrame();
		}
		UpdateScreenVisibility();
	}

	public override void _ExitTree() {
		base._ExitTree();
		if (videoPlayer != null) {
			videoPlayer.Finished -= OnVideoFinished;
		}
	}

	public override void EnterInteraction() {
		this.TogglePlayback();
	}

	public override void _Process(double delta) {
		base._Process(delta);
		if (!isInteracting || videoPlayer == null) return;
		if (Input.IsKeyPressed(Key.Escape)) {
			HidePlaylistUI();
		}
		bool togglePressed = Input.IsKeyPressed(Key.U);
		if (togglePressed && !this.isToggleKeyHeld) {
			if (this.isPlaying || this.isPaused) {
				TogglePlaylistUI(!this.isPlaylistVisible);
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
				ResumePlayback();
			} else if (Input.IsActionJustPressed("ui_left")) {
				this.SwitchVideo(-1);
			} else if (Input.IsActionJustPressed("ui_right")) {
				this.SwitchVideo(1);
			}
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
		if (this.isPaused) {
			ResumePlayback();
			return;
		}
		if (videoPlayer.IsPlaying()) {
			StopPlayback();
			return;
		}
		PrepareStreamForPlayback();
		videoPlayer.Play();
		this.isPlaying = true;
		this.isPaused = false;
		isInteracting = true;
		UpdateActionLabel();
		UpdateScreenVisibility();
	}

	private void StopPlayback() {
		if (videoPlayer == null) return;
		bool wasPlaying = videoPlayer.IsPlaying() || this.isPlaying;
		if (!wasPlaying && !this.isPaused) return;
		videoPlayer.Stop();
		videoPlayer.Paused = false;
		this.isPlaying = false;
		this.isPaused = false;
		isInteracting = false;
		ClearVideoFrame();
		HidePlaylistUI();
		UpdateActionLabel();
		UpdateScreenVisibility();
	}

	private void PausePlayback() {
		if (videoPlayer == null || this.isPaused) return;
		if (!(videoPlayer.IsPlaying() || this.isPlaying)) return;
		videoPlayer.Paused = true;
		this.isPlaying = false;
		this.isPaused = true;
		isInteracting = true;
		UpdateActionLabel();
		UpdateScreenVisibility();
	}

	private void ResumePlayback() {
		if (videoPlayer == null || !this.isPaused) return;
		videoPlayer.Paused = false;
		if (!videoPlayer.IsPlaying()) {
			videoPlayer.Play();
		}
		this.isPlaying = true;
		this.isPaused = false;
		isInteracting = true;
		UpdateActionLabel();
		UpdateScreenVisibility();
	}

	private void OnVideoFinished() {
		StopPlayback();
	}

	private void RefreshPlaybackState() {
		if (videoPlayer == null) {
			this.isPlaying = false;
			this.isPaused = false;
			isInteracting = false;
		} else {
			bool playing = videoPlayer.IsPlaying();
			bool paused = playing && videoPlayer.Paused;
			this.isPlaying = playing && !paused;
			this.isPaused = paused;
			isInteracting = this.isPlaying || this.isPaused;
		}
		UpdateScreenVisibility();
	}

	private void UpdateActionLabel() {
		ActionName = this.isPlaying ? StopActionText : PlayActionText;
		if (isFocus && nameLabel != null) {
			nameLabel.Text = $"[E] {ActionName}";
		}
	}

	private bool videoSignalsConnected;

	private void ConnectVideoSignals() {
		if (videoPlayer == null || this.videoSignalsConnected) return;
		videoPlayer.Finished += OnVideoFinished;
		this.videoSignalsConnected = true;
	}

	private void DisconnectVideoSignals(VideoStreamPlayer target = null) {
		var player = target ?? videoPlayer;
		if (player == null || !this.videoSignalsConnected) return;
		player.Finished -= OnVideoFinished;
		this.videoSignalsConnected = false;
	}

	private void InitializePlaylist() {
		if (videoPlayer == null) return;
		if (VideoStreams != null && VideoStreams.Count > 0) {
			var targetIndex = WrapIndex(DefaultStreamIndex, VideoStreams.Count);
			if (AssignStream(targetIndex)) {
				videoPlayer.Stop();
			}
			this.isPlaying = false;
			this.isPaused = false;
			isInteracting = false;
		} else {
			currentStreamIndex = -1;
		}
		UpdatePlaylistSelection();
		HidePlaylistUI();
	}

	private void PrepareStreamForPlayback() {
		if (videoPlayer == null) return;
		if (HideScreenOnStop) {
			videoPlayer.Visible = true;
		}
		videoPlayer.Paused = false;
		this.isPaused = false;
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
		bool wasPaused = this.isPaused || (wasPlaying && videoPlayer.Paused);
		AssignStream(nextIndex);
		if (wasPaused) {
			videoPlayer.Play();
			videoPlayer.Paused = true;
			this.isPlaying = false;
			this.isPaused = true;
			isInteracting = true;
		} else if (wasPlaying || this.isPlaying) {
			videoPlayer.Play();
			videoPlayer.Paused = false;
			this.isPlaying = true;
			this.isPaused = false;
			isInteracting = true;
		} else {
			this.isPlaying = false;
			this.isPaused = false;
			isInteracting = false;
		}
		UpdatePlaylistSelection();
		UpdateActionLabel();
		UpdateScreenVisibility();
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
		UpdatePlaylistSelection();
		return wasPlaying;
	}

	private void ClearVideoFrame() {
		if (videoPlayer == null) return;
		if (ResetOnStop) {
			videoPlayer.StreamPosition = 0;
		}
		videoPlayer.Paused = false;
		this.isPaused = false;
		if (!HideScreenOnStop) return;
		if ((VideoStreams != null && VideoStreams.Count > 0) || originalStream != null) {
			videoPlayer.Stream = null;
		}
		videoPlayer.Visible = false;
		HidePlaylistUI();
	}

	private void UpdateScreenVisibility() {
		if (videoPlayer == null) return;
		if (HideScreenOnStop) {
			videoPlayer.Visible = this.isPlaying || this.isPaused;
		} else {
			videoPlayer.Visible = true;
		}
	}

	private void ResolvePlaylistUI() {
		playlistPanel = PlaylistPanelPath.IsEmpty ? null : GetNodeOrNull<Control>(PlaylistPanelPath);
		if (playlistPanel == null) {
			GD.PushWarning($"{Name}: 未找到影片選單面板 {PlaylistPanelPath}，將於運行時建立預設面板。");
			CreateRuntimePlaylistUI();
		}
		if (playlistPanel == null) return;
		playlistButtonContainer = PlaylistButtonContainerPath.IsEmpty ? null : GetNodeOrNull<Container>(PlaylistButtonContainerPath);
		if (playlistButtonContainer == null && playlistPanel != null) {
			playlistButtonContainer = playlistPanel as Container;
			if (playlistButtonContainer == null) {
				playlistButtonContainer = playlistPanel.GetNodeOrNull<Container>("VBoxContainer");
			}
		}
		if (playlistButtonContainer == null) {
			CreateRuntimePlaylistContainer();
		}
		BuildPlaylistButtons();
		HidePlaylistUI();
	}

	private void BuildPlaylistButtons() {
		foreach (var button in playlistButtons) {
			if (button != null && IsInstanceValid(button)) {
				button.QueueFree();
			}
		}
		playlistButtons.Clear();
		if (playlistButtonContainer == null) return;
		foreach (Node child in playlistButtonContainer.GetChildren()) {
			child.QueueFree();
		}
		if (VideoStreams == null || VideoStreams.Count == 0) {
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
		for (int i = 0; i < VideoStreams.Count; i++) {
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
			playlistButtons.Add(button);
		}
		UpdatePlaylistSelection();
	}

	private string GetStreamDisplayName(int index) {
		var stream = VideoStreams[index];
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
		PlayStreamAt(index);
	}

	private void PlayStreamAt(int index) {
		if (videoPlayer == null || VideoStreams == null || VideoStreams.Count == 0) return;
		if (index < 0 || index >= VideoStreams.Count) return;
		if (HideScreenOnStop) {
			videoPlayer.Visible = true;
		}
		videoPlayer.Paused = false;
		this.isPaused = false;
		AssignStream(index, true);
		videoPlayer.Play();
		this.isPlaying = true;
		isInteracting = true;
		UpdateActionLabel();
		UpdateScreenVisibility();
		UpdatePlaylistSelection();
		HidePlaylistUI();
	}

	private void UpdateHoverDescription(int index) {
		if (VideoStreams == null || index < 0 || index >= VideoStreams.Count) return;
		var stream = VideoStreams[index];
		if (stream == null) return;
		var tooltip = GetStreamDisplayName(index);
		if (!string.IsNullOrEmpty(stream.ResourcePath)) {
			tooltip += $"\n{stream.ResourcePath}";
		}
		if (playlistPanel != null) {
			playlistPanel.TooltipText = tooltip;
		}
	}

	private void SetGameInteractionLock(bool enabled) {
		if (gameManager == null || !GodotObject.IsInstanceValid(gameManager)) return;
		if (enabled) {
			if (gameManager.currentInteractable != this) {
				gameManager.SetCurrentInteractable(this);
			}
		} else if (gameManager.currentInteractable == this) {
			gameManager.SetCurrentInteractable(null);
		}
	}

	private void TogglePlaylistUI(bool visible) {
		if (playlistPanel == null) return;
		if (isPlaylistVisible == visible) return;
		if (visible) {
			ShowPlaylistUI();
		} else {
			HidePlaylistUI();
		}
	}

	private void ShowPlaylistUI() {
		if (playlistPanel == null) return;
		BuildPlaylistButtons();
		playlistPanel.Visible = true;
		isPlaylistVisible = true;
		SetGameInteractionLock(true);
		UpdatePlaylistSelection();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetViewport().SetInputAsHandled();
	}

	private void HidePlaylistUI() {
		if (playlistPanel == null) return;
		playlistPanel.Visible = false;
		isPlaylistVisible = false;
		SetGameInteractionLock(false);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private void UpdatePlaylistSelection() {
		if (playlistButtons.Count == 0) return;
		for (int i = 0; i < playlistButtons.Count; i++) {
			var button = playlistButtons[i];
			if (button == null || !IsInstanceValid(button)) continue;
			button.ButtonPressed = (i == currentStreamIndex && currentStreamIndex >= 0);
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
		playlistPanel = panel;
		CreateRuntimePlaylistContainer();
		PlaylistPanelPath = playlistPanel.GetPath();
		if (playlistButtonContainer != null) {
			PlaylistButtonContainerPath = playlistButtonContainer.GetPath();
		}
	}

	private void CreateRuntimePlaylistContainer() {
		if (playlistPanel == null) return;
		playlistButtonContainer = playlistPanel.GetNodeOrNull<Container>("VBoxContainer");
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
		playlistPanel.AddChild(vbox);
		playlistButtonContainer = vbox;
	}

	private void BeginInteractionSession() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private void EndInteractionSession() {
		if (gameManager != null) {
			gameManager.SetCurrentInteractable(null);
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
