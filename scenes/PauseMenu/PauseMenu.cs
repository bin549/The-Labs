using Godot;

public partial class PauseMenu : Control {
    private Control _panel;
    private Button _btnResume;
    private Button _btnQuit;

    public override void _Ready() {
        Visible = false;
        _panel = GetNodeOrNull<Control>("Panel");
        _btnResume = GetNodeOrNull<Button>("Panel/VBox/Resume");
        _btnQuit = GetNodeOrNull<Button>("Panel/VBox/Quit");
        if (_btnResume != null) _btnResume.Pressed += _OnResumePressed;
        if (_btnQuit != null) _btnQuit.Pressed += _OnQuitPressed;
        CenterPanel();
    }

    public void ShowMenu() {
        Visible = true;
        GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        CenterPanel();
    }

    public void HideMenu() {
        Visible = false;
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void _OnResumePressed() {
        HideMenu();
    }

    private void _OnQuitPressed() {
        GetTree().Quit();
    }

    private void CenterPanel() {
        if (_panel == null) return;
        var viewportSize = GetViewportRect().Size;
        var panelSize = _panel.Size;
        _panel.Position = (viewportSize - panelSize) / 2f;
    }
}
