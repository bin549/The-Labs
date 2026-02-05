using Godot;
using PhantomCamera;

public partial class LabItem : Interactable {
    private PhantomCamera3D phantomCam;
    private TeleportManager teteportManager;
    [ExportGroup("Teleport Settings")]
    [Export] public bool RegisterToMenu { get; set; } = true;
    [Export] public string TeleportName { get; set; } = "实验";
    [Export] public string TeleportDescription { get; set; } = "";
    [Export(PropertyHint.Enum, "Mechanics,Electricity,Chemistry")] public int TeleportCategory { get; set; } = 0;
    [Export] public Node3D TeleportPosition { get; set; }
    public bool IsInteracting => base.isInteracting;

    public override void _Ready() {
        base._Ready();
        this.InitPhantomCamera();
        this.RegisterToTeleportManager();
    }

    public override void _ExitTree() {
        base._ExitTree();
        this.UnregisterFromTeleportManager();
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

    private void RegisterToTeleportManager() {
        if (!this.RegisterToMenu) return;
        this.teteportManager = GetTree().Root.FindChild("TeleportManager", true, false) as TeleportManager;
        if (this.teteportManager == null) {
            return;
        }
        Vector3 teleportPos;
        if (this.TeleportPosition != null && GodotObject.IsInstanceValid(this.TeleportPosition)) {
            teleportPos = this.TeleportPosition.GlobalPosition;
        } else {
            teleportPos = GlobalPosition;
        }
        var expInfo = new ExperimentInfo {
            ExperimentName = string.IsNullOrEmpty(TeleportName) ? DisplayName : TeleportName,
            Description = TeleportDescription,
            Category = (ExperimentCategory)TeleportCategory,
            Position = teleportPos,
            ExperimentNodePath = GetPath()
        };
        this.teteportManager.RegisterExperiment(expInfo);
    }

    private void UnregisterFromTeleportManager() {
        if (!this.RegisterToMenu || this.teteportManager == null) return;
        string expName = string.IsNullOrEmpty(TeleportName) ? DisplayName : TeleportName;
        this.teteportManager.UnregisterExperiment(expName);
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 999;
        }
    }

    public override void ExitInteraction() {
        base.ExitInteraction();
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 1;
        }
        if (base.gameManager != null) {
            base.gameManager.SetCurrentInteractable(null);
        }
    }
}