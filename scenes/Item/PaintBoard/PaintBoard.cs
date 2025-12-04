using Godot;

public partial class PaintBoard : Interactable {
    [Export] public NodePath paintContrlPath = new NodePath("/root/World/Player/CanvasLayer/Control/PaintContrl");
    [Export] public NodePath painterImagePath =
        new NodePath("/root/World/Player/CanvasLayer/Control/PaintContrl/PainterImage");
    private Control paintContrl;
    private PainterImage painterImage;

    public override void _Ready() {
        base._Ready();
        this.paintContrl = GetNodeOrNull<Control>(this.paintContrlPath);
        if (this.paintContrl == null) {
            GD.PushWarning($"{Name}: 未找到 PaintContrl 节点，路径: {this.paintContrlPath}");
        } else {
            this.paintContrl.Visible = false;
        }
        CallDeferred(MethodName.SetupCanvasMesh);
    }

    private void SetupCanvasMesh() {
        this.painterImage = GetNodeOrNull<PainterImage>(this.painterImagePath);
        var canvasMesh = GetNodeOrNull<MeshInstance3D>("CanvasMesh");
        if (canvasMesh != null) {
            this.painterImage.SetCanvasMesh(canvasMesh);
            GD.Print($"{Name}: 已将 CanvasMesh 设置到 PainterImage");
        } else {
            GD.PushWarning($"{Name}: 未找到 CanvasMesh 子节点");
        }
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        if (this.paintContrl != null) {
            this.paintContrl.Visible = true;
        }
    }

    public override void ExitInteraction() {
        base.ExitInteraction();
        if (this.paintContrl != null) {
            this.paintContrl.Visible = false;
        }
    }
}