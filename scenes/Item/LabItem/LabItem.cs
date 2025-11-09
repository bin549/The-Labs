using Godot;
using PhantomCamera;

public partial class LabItem : Interactable {
    private PhantomCamera3D phantomCam;

    public override void _Ready() {
        base._Ready();
        this.InitPhantomCamera();
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
    }

    public override void ExitInteraction() {
        this.SetOutlineActive(false);
        base.ExitInteraction();
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 1;
        }
        base.gameManager.SetCurrentInteractable(null);
    }
}
