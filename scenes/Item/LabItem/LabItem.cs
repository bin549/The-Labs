using Godot;
using PhantomCamera;

public partial class LabItem : Interactable {
    private PhantomCamera3D phantomCam;
    private ExperimentManager experimentManager;

    [ExportGroup("实验快速跳转配置")]
    [Export] public bool RegisterToMenu { get; set; } = true; // 是否注册到P键菜单
    [Export] public string ExperimentName { get; set; } = "实验"; // 实验名称
    [Export] public string ExperimentDescription { get; set; } = ""; // 实验描述
    [Export] public ExperimentCategory ExperimentCategory { get; set; } = ExperimentCategory.Mechanics; // 实验分类
    [Export] public Node3D TeleportPosition { get; set; } // 跳转位置节点（拖拽Marker3D节点到这里，留空则使用自身位置）

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

        // 查找 ExperimentManager
        experimentManager = GetTree().Root.FindChild("ExperimentManager", true, false) as ExperimentManager;
        if (experimentManager == null) {
            GD.PushWarning($"LabItem [{ExperimentName}]: 未找到 ExperimentManager，无法注册到快速跳转菜单");
            return;
        }

        // 确定跳转位置
        Vector3 teleportPos;
        if (TeleportPosition != null && GodotObject.IsInstanceValid(TeleportPosition)) {
            teleportPos = TeleportPosition.GlobalPosition;
            GD.Print($"  → 使用自定义传送点: {TeleportPosition.Name} at {teleportPos}");
        } else {
            teleportPos = GlobalPosition;
            GD.Print($"  → 使用LabItem自身位置: {teleportPos}");
        }

        // 创建实验信息
        var expInfo = new ExperimentInfo {
            ExperimentName = string.IsNullOrEmpty(ExperimentName) ? DisplayName : ExperimentName,
            Description = ExperimentDescription,
            Category = ExperimentCategory,
            Position = teleportPos,
            ExperimentNodePath = GetPath()
        };

        // 注册到管理器
        experimentManager.RegisterExperiment(expInfo);
        GD.Print($"✓ LabItem [{expInfo.ExperimentName}] 已注册到实验菜单 (分类: {ExperimentCategory})");
    }

    private void UnregisterFromExperimentManager() {
        if (!RegisterToMenu || experimentManager == null) return;

        string expName = string.IsNullOrEmpty(ExperimentName) ? DisplayName : ExperimentName;
        experimentManager.UnregisterExperiment(expName);
        GD.Print($"✗ LabItem [{expName}] 已从实验菜单取消注册");
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
