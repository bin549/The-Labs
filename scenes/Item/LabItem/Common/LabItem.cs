using Godot;
using PhantomCamera;

public partial class LabItem : Interactable {
    private PhantomCamera3D phantomCam;
    private ExperimentManager experimentManager;
    [ExportGroup("Teleport Settings")]
    [Export] public bool RegisterToMenu { get; set; } = true;
    [Export] public string ExperimentName { get; set; } = "实验";
    [Export] public string ExperimentDescription { get; set; } = "";
    [Export] public ExperimentCategory ExperimentCategory { get; set; } = ExperimentCategory.Mechanics;
    [Export] public Node3D TeleportPosition { get; set; }

    public override void _Ready() {
        base._Ready();
        this.InitPhantomCamera();
        this.RegisterToExperimentManager();
    }

    public override void _ExitTree() {
        base._ExitTree();
        this.UnregisterFromExperimentManager();
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

    private void RegisterToExperimentManager() {
        if (!RegisterToMenu) return;
        this.experimentManager = GetTree().Root.FindChild("ExperimentManager", true, false) as ExperimentManager;
        if (this.experimentManager == null) {
            GD.PushWarning($"LabItem [{ExperimentName}]: 未找到 ExperimentManager，无法注册到快速跳转菜单");
            return;
        }
        Vector3 teleportPos;
        if (TeleportPosition != null && GodotObject.IsInstanceValid(TeleportPosition)) {
            teleportPos = TeleportPosition.GlobalPosition;
        } else {
            teleportPos = GlobalPosition;
        }
        var expInfo = new ExperimentInfo {
            ExperimentName = string.IsNullOrEmpty(ExperimentName) ? DisplayName : ExperimentName,
            Description = ExperimentDescription,
            Category = ExperimentCategory,
            Position = teleportPos,
            ExperimentNodePath = GetPath()
        };
        this.experimentManager.RegisterExperiment(expInfo);
    }

    private void UnregisterFromExperimentManager() {
        if (!RegisterToMenu || this.experimentManager == null) return;
        string expName = string.IsNullOrEmpty(ExperimentName) ? DisplayName : ExperimentName;
        this.experimentManager.UnregisterExperiment(expName);
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
        if (base.gameManager != null) {
            base.gameManager.SetCurrentInteractable(null);
        }
    }
}