using Godot;

public partial class PauseMenu : Control {
    [Export] private Control _panel;
    [Export] private Button Resume;
    [Export] private Button Quit;

    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        if (Resume != null)
            Resume.Pressed += OnResumePressed;
        if (Quit != null)
            Quit.Pressed += OnQuitPressed;
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("pause")) {
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
