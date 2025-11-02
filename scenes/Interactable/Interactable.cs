using Godot;
using PhantomCamera;

public partial class Interactable : Node3D {
    [Export]
    public string DisplayName { get; set; } = "物体";
    [Export]
    public string ActionName { get; set; } = "交互";
    [Export]
    public NodePath NameLabelPath { get; set; } = new NodePath();
    [Export]
    public NodePath LinePath { get; set; } = new NodePath();
    private ShaderMaterial _outlineMat;
    private Label3D nameLabel;
    private Node3D lineNode;
    private PhantomCamera3D phantomCam;
    private bool isActivate;

    public override void _Ready() {
        this.nameLabel = GetNodeOrNull<Label3D>(NameLabelPath);
        this.lineNode = GetNodeOrNull<Node3D>(LinePath);
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
            this.nameLabel.Visible = true;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = true;
        }
    }

    private void InitPhantomCamera() {
        var phantomCamNode = GetNodeOrNull<Node3D>("PhantomCamera3D");
        if (phantomCamNode != null) this.phantomCam = phantomCamNode.AsPhantomCamera3D();
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

    public virtual void Interact(Node3D interactor) {
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 999;
            this.isActivate = true;
            return;
        }
    }

    public virtual void ExitInteraction() {
        if (this.isActivate && this.phantomCam != null) {
            this.phantomCam.Priority = 1;
        }
        this.isActivate = false;
        if (this.nameLabel != null) this.nameLabel.Text = DisplayName;
        ApplyOutline(false);
    }

    private void ApplyOutline(bool enable) {
        if (_outlineMat == null) return;
    }
}
