using Godot;
using PhantomCamera;

public partial class LabItem : Interactable {
    private PhantomCamera3D phantomCam;
    private bool isActivate;

    public override void _Ready() {
        this.InitPhantomCamera();
    }

    private void InitPhantomCamera() {
        var phantomCamNode = GetNodeOrNull<Node3D>("PhantomCamera3D");
        if (phantomCamNode != null) this.phantomCam = phantomCamNode.AsPhantomCamera3D();
    }

    protected override void Interact(Node3D interactor) {
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 999;
            this.isActivate = true;
            return;
        }
    }

    protected override void ExitInteraction() {
        if (this.isActivate && this.phantomCam != null) {
            this.phantomCam.Priority = 1;
        }
        base.ExitInteraction();
    }
}
