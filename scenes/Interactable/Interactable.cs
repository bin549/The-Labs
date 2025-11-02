using Godot;
using PhantomCamera;

public partial class Interactable : Node3D {
    [Export] protected string DisplayName { get; set; } = "物体";
    [Export] protected string ActionName { get; set; } = "交互";
    [Export] protected ShaderMaterial outlineMat;
    [Export] protected Label3D nameLabel;
    [Export] protected Node3D lineNode;

    public override void _Ready() {
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
            this.nameLabel.Visible = true;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = true;
        }
    }

    public virtual void OnFocusEnter() {
        ApplyOutline(true);
        if (this.nameLabel != null) {
            this.nameLabel.Text = $"[E] {ActionName}";
        }
    }

    public virtual void OnFocusExit() {
        ApplyOutline(false);
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
        }
    }

    protected virtual void Interact(Node3D interactor) {
    }

    protected virtual void ExitInteraction() {
        if (this.nameLabel != null) this.nameLabel.Text = DisplayName;
        ApplyOutline(false);
    }

    private void ApplyOutline(bool enable) {
        if (outlineMat == null) return;
    }
}
