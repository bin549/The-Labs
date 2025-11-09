using Godot;

public partial class PauseMenu : Control {
    [Export] private Control _panel;
    [Export] private Button ResumeBtn;
    [Export] private Button QuitBtn;
    [Export] public GameManager gameManager;

    public override void _Ready() {
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
        if (ResumeBtn != null)
            ResumeBtn.Pressed += OnResumePressed;
        if (QuitBtn != null)
            QuitBtn.Pressed += OnQuitPressed;
    }

    private void OnResumePressed() {
        if (this.gameManager != null) {
            this.gameManager.TogglePause(false);
        } else {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            GetTree().Paused = false;
            Visible = false;
        }
    }

    private void OnQuitPressed() {
        GetTree().Quit();
    }
}
