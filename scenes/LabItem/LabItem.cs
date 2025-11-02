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

    protected override void EnterInteraction() {
        base.gameManager.IsBusy = true;
        base.EnterInteraction();
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 999;
        }
    }

    protected override void ExitInteraction() {
        base.ExitInteraction();
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 1;
        }
        base.gameManager.IsBusy = false;
    }
}
