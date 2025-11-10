using Godot;

public partial class GameManager : Node {
    [Export] public Interactable currentInteractable = null;
    [Export] public PauseMenu pauseMenu = null;

    public override void _Ready() {
        this.ProcessMode = Node.ProcessModeEnum.Always;
        if (this.pauseMenu != null) {
            this.pauseMenu.Visible = false;
            this.pauseMenu.ProcessMode = Node.ProcessModeEnum.Always;
            this.pauseMenu.gameManager = this;
        } else {
            GD.PushWarning($"{nameof(GameManager)}: pauseMenu 未绑定，暂停菜单将无法显示。");
        }
    }

    public void SetCurrentInteractable(Interactable interactable) {
        this.currentInteractable = interactable;
    }
    public bool IsBusy => this.currentInteractable != null;

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("pause")) {
            if (!this.IsBusy) {
                this.TogglePause();
            } else {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                this.currentInteractable.ExitInteraction();
            }
        }
    }

    public void TogglePause(bool? targetState = null) {
        bool newState = targetState ?? !GetTree().Paused;
        if (GetTree().Paused == newState && targetState.HasValue) {
            return;
        }
        GetTree().Paused = newState;
        if (this.pauseMenu != null) {
            this.pauseMenu.Visible = newState;
        }
        if (newState)
            Input.MouseMode = Input.MouseModeEnum.Visible;
        else
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }
}
