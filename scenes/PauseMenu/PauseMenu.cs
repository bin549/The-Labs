using Godot;

public partial class PauseMenu : Control {
    [Export] private Control _panel;
    [Export] private Button ResumeBtn;
    [Export] private Button QuitBtn;
    [Export] protected GameManager gameManager;

    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        if (ResumeBtn != null)
            ResumeBtn.Pressed += OnResumePressed;
        if (QuitBtn != null)
        QuitBtn.Pressed += OnQuitPressed;
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("pause")) {
            GD.Print(this.gameManager.IsBusy);
            if (this.gameManager.IsBusy) return;
            GetTree().Paused = !GetTree().Paused;
            Visible = GetTree().Paused;
            if (GetTree().Paused)
                Input.MouseMode = Input.MouseModeEnum.Visible;
            else
                Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void OnResumePressed() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().Paused = false;
        Visible = false;
    }

    private void OnQuitPressed() {
        GetTree().Quit();
    }
}
