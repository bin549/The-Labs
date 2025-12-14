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
        if (this.paintContrl != null) {
            this.paintContrl.Visible = false;
        }
        CallDeferred(MethodName.SetupCanvasMesh);
        CallDeferred(MethodName.SetupClearButton);
    }

    private void SetupCanvasMesh() {
        this.painterImage = GetNodeOrNull<PainterImage>(this.painterImagePath);
        var canvasMesh = GetNodeOrNull<MeshInstance3D>("CanvasMesh");
        if (canvasMesh != null) {
            this.painterImage.SetCanvasMesh(canvasMesh);
        } 
    }

    private void SetupClearButton() {
        if (this.painterImage == null) {
            this.painterImage = GetNodeOrNull<PainterImage>(this.painterImagePath);
        }
        if (this.painterImage == null) return;
        
        var clearButton = this.painterImage.GetNodeOrNull<Button>("../Panel/ClearButton");
        if (clearButton != null) {
            clearButton.Pressed += OnClearButtonPressed;
        }
    }

    private void OnClearButtonPressed() {
        if (this.painterImage != null) {
            this.painterImage.ClearCanvas();
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