using Godot;

public partial class QuickExperimentSetup : Node3D {
    [Export] public bool AutoSetupOnReady { get; set; } = false;
    [Export] public ExperimentType SelectedExperiment { get; set; } = ExperimentType.AcidBase;

    public enum ExperimentType {
        AcidBase,
        MetalAcid,
        SodiumWater,
        Combustion,
        Magnetism,
        Custom
    }

    public override void _Ready() {
        if (AutoSetupOnReady) {
            SetupExperiment();
        }
    }

    public void SetupExperiment() {
        GD.Print($"[QuickExperimentSetup] 开始设置实验类型：{SelectedExperiment}");
        switch (SelectedExperiment) {
            case ExperimentType.AcidBase:
                SetupAcidBaseExperiment();
                break;
            case ExperimentType.MetalAcid:
                SetupMetalAcidExperiment();
                break;
            case ExperimentType.SodiumWater:
                SetupSodiumWaterExperiment();
                break;
            case ExperimentType.Combustion:
                SetupCombustionExperiment();
                break;
            case ExperimentType.Magnetism:
                SetupMagnetismExperiment();
                break;
            default:
                GD.PushWarning("请选择实验类型或使用 Custom 自定义");
                break;
        }
    }

    private void SetupAcidBaseExperiment() {
        ExperimentBuilder.Create(this, "酸碱中和实验")
            .AddAcid("盐酸", new Vector3(-0.3f, 0, 0))
            .AddBase("氢氧化钠", new Vector3(0.3f, 0, 0))
            .WithAcidBaseReaction()
            .Build();
    }

    private void SetupMetalAcidExperiment() {
        ExperimentBuilder.Create(this, "金属与酸反应")
            .AddMetal("锌片", new Vector3(-0.3f, 0, 0))
            .AddAcid("稀硫酸", new Vector3(0.3f, 0, 0))
            .WithMetalAcidReaction()
            .Build();
    }

    private void SetupSodiumWaterExperiment() {
        ExperimentBuilder.Create(this, "钠与水反应")
            .AddSodium("钠块", new Vector3(-0.3f, 0, 0))
            .AddWater("水", new Vector3(0.3f, 0, 0))
            .WithSodiumWaterReaction()
            .Build();
    }

    private void SetupCombustionExperiment() {
        ExperimentBuilder.Create(this, "燃烧实验")
            .AddItem("镁带", ItemTypePresets.COMBUSTIBLE, new Vector3(-0.3f, 0, 0))
            .AddFire("酒精灯", new Vector3(0.3f, 0, 0))
            .WithCombustion()
            .Build();
    }

    private void SetupMagnetismExperiment() {
        ExperimentBuilder.Create(this, "磁性实验")
            .AddMagnet("磁铁", new Vector3(-0.3f, 0, 0))
            .AddItem("铁钉", ItemTypePresets.IRON, new Vector3(0.3f, 0, 0))
            .WithMagnetization()
            .Build();
    }
}