using Godot;

public partial class GameManager : Node {
    [Export] public Interactable currentInteractable = null;
    [Export] public PauseMenu pauseMenu = null;
    public bool IsMenuOpen { get; set; } = false;

    public override void _Ready() {
        this.ProcessMode = Node.ProcessModeEnum.Always;
        if (this.pauseMenu == null) {
            return;
        }
        this.pauseMenu.Visible = false;
        this.pauseMenu.ProcessMode = Node.ProcessModeEnum.Always;
        this.pauseMenu.gameManager = this;
    }

    public void SetCurrentInteractable(Interactable interactable) {
        this.currentInteractable = interactable;
    } 

    public bool IsBusy => this.currentInteractable != null || IsMenuOpen;

    public override void _Process(double delta) {
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event.IsActionPressed("pause")) {
            if (!this.IsBusy) {
                this.TogglePause();
                GetViewport().SetInputAsHandled();
            } else if (this.currentInteractable != null) {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                this.currentInteractable.ExitInteraction();
                GetViewport().SetInputAsHandled();
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
        Input.MouseMode = newState ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }
}