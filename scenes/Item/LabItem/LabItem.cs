using Godot;
using PhantomCamera;

public partial class LabItem : Interactable {
    private PhantomCamera3D phantomCam;

    public override void _Ready() {
        base._Ready();
        this.InitPhantomCamera();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) return;
        if (@event.IsActionPressed("pause") || @event.IsActionPressed("ui_cancel")) {
            GetViewport().SetInputAsHandled();
            this.ExitInteraction();
        }
    }

    private void InitPhantomCamera() {
        var phantomCamNode = GetNodeOrNull<Node3D>("PhantomCamera3D");
        if (phantomCamNode != null) this.phantomCam = phantomCamNode.AsPhantomCamera3D();
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        this.SetOutlineActive(true);
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 999;
        }
        GD.Print($"进入{DisplayName}交互");
        GD.Print("提示：如需使用摩擦力实验，请将脚本改为FrictionExperiment.cs");
    }

    public override void ExitInteraction() {
        this.SetOutlineActive(false);
        base.ExitInteraction();
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 1;
        }
        if (base.gameManager != null) {
            base.gameManager.SetCurrentInteractable(null);
        }
    }
}
