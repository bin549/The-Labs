using Godot;
using PhantomCamera;

public partial class Interactable : Node3D {
    [Export] protected string DisplayName { get; set; } = "物体";
    [Export] protected string ActionName { get; set; } = "交互";
    [Export] protected ShaderMaterial outlineMat;
    [Export] protected Label3D nameLabel;
    [Export] protected Node3D lineNode;
    protected bool isFocus = false;
    protected bool isInteracting = false;
    [Export] protected GameManager gameManager;

    public override void _Ready() {
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
            this.nameLabel.Visible = true;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = true;
        }
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("interact") && this.isFocus && !this.gameManager.IsBusy) {
            this.EnterInteraction();
        }
    }

    public virtual void EnterInteraction() {
        this.gameManager.SetCurrentInteractable(this);
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public virtual void ExitInteraction() {
    }

    public virtual void OnFocusEnter() {
        ApplyOutline(true);
        if (this.nameLabel != null) {
            this.nameLabel.Text = $"[E] {ActionName}";
        }
        this.isFocus = true;
    }

    public virtual void OnFocusExit() {
        ApplyOutline(false);
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
        }
        this.isFocus = false;
    }

    protected virtual void Interact(Node3D interactor) {
    }

    private void ApplyOutline(bool enable) {
        if (outlineMat == null) return;
    }
}
