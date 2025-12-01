using Godot;
using System;

public partial class ExperimentSetupHelper : RefCounted {
    private Node3D rootNode;
    private ExperimentPhenomenonManager phenomenonManager;
    private Godot.Collections.Array<PlacableItem> items = new();
    private Godot.Collections.Array<ExperimentPhenomenon> phenomena = new();

    public ExperimentSetupHelper(Node3D root) {
        rootNode = root;
    }

    public void CreateBasicExperiment(string experimentName = "实验") {
        GD.Print($"[ExperimentSetupHelper] 开始创建实验：{experimentName}");
        phenomenonManager = new ExperimentPhenomenonManager();
        phenomenonManager.Name = "PhenomenonManager";
        phenomenonManager.EffectsParent = rootNode;
        rootNode.AddChild(phenomenonManager);
        phenomenonManager.Owner = rootNode.GetTree().EditedSceneRoot;
        GD.Print("[ExperimentSetupHelper] 现象管理器已创建");
    }

    public PlacableItem AddPlacableItem(
        string itemName,
        string itemType,
        Vector3 position,
        Color? color = null,
        Vector3? scale = null
    ) {
        var item = new PlacableItem();
        item.Name = itemName;
        item.ItemName = itemName;
        item.ItemType = itemType;
        item.ItemColor = color ?? ItemTypePresets.GetRecommendedColor(itemType);
        item.GlobalPosition = position;
        rootNode.AddChild(item);
        item.Owner = rootNode.GetTree().EditedSceneRoot;
        CreateDefaultMesh(item, scale ?? Vector3.One * 0.15f);
        if (phenomenonManager != null) {
            phenomenonManager.RegisterItem(item);
        }
        items.Add(item);
        GD.Print($"[ExperimentSetupHelper] 添加物品：{itemName} (类型: {itemType})");
        return item;
    }

    public void AddPhenomenon(ExperimentPhenomenon phenomenon) {
        if (phenomenonManager != null) {
            phenomena.Add(phenomenon);
            phenomenonManager.Phenomena = phenomena;
            GD.Print($"[ExperimentSetupHelper] 添加现象：{phenomenon.PhenomenonName}");
        }
    }

    public void AddChemistryPresets() {
        var presets = PhenomenonPresets.GetChemistryPresets();
        foreach (var preset in presets) {
            AddPhenomenon(preset);
        }
        GD.Print($"[ExperimentSetupHelper] 添加了 {presets.Count} 个化学现象预设");
    }

    public void AddPhysicsPresets() {
        var presets = PhenomenonPresets.GetPhysicsPresets();
        foreach (var preset in presets) {
            AddPhenomenon(preset);
        }
        GD.Print($"[ExperimentSetupHelper] 添加了 {presets.Count} 个物理现象预设");
    }

    public void Finalize() {
        GD.Print($"[ExperimentSetupHelper] 实验设置完成！");
        GD.Print($"  - 物品数量：{items.Count}");
        GD.Print($"  - 现象数量：{phenomena.Count}");
    }

    private void CreateDefaultMesh(PlacableItem item, Vector3 size) {
        var mesh = new MeshInstance3D();
        mesh.Name = "Mesh";
        var boxMesh = new BoxMesh();
        boxMesh.Size = size;
        mesh.Mesh = boxMesh;
        var material = new StandardMaterial3D();
        material.AlbedoColor = item.ItemColor;
        mesh.MaterialOverride = material;
        item.AddChild(mesh);
        mesh.Owner = rootNode.GetTree().EditedSceneRoot;
    }
}

public class ExperimentBuilder {
    private ExperimentSetupHelper helper;

    private ExperimentBuilder(Node3D root, string experimentName) {
        helper = new ExperimentSetupHelper(root);
        helper.CreateBasicExperiment(experimentName);
    }

    public static ExperimentBuilder Create(Node3D root, string experimentName = "实验") {
        return new ExperimentBuilder(root, experimentName);
    }

    #region 添加物品的便捷方法

    public ExperimentBuilder AddItem(string name, string type, Vector3 position, Color? color = null) {
        helper.AddPlacableItem(name, type, position, color);
        return this;
    }

    public ExperimentBuilder AddAcid(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.ACID, position);
        return this;
    }

    public ExperimentBuilder AddBase(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.BASE, position);
        return this;
    }

    public ExperimentBuilder AddMetal(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.METAL, position);
        return this;
    }

    public ExperimentBuilder AddWater(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.WATER, position);
        return this;
    }

    public ExperimentBuilder AddSodium(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.SODIUM, position);
        return this;
    }

    public ExperimentBuilder AddFire(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.FIRE, position);
        return this;
    }

    public ExperimentBuilder AddMagnet(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.MAGNET, position);
        return this;
    }

    #endregion

    public ExperimentBuilder WithPhenomenon(ExperimentPhenomenon phenomenon) {
        helper.AddPhenomenon(phenomenon);
        return this;
    }

    public ExperimentBuilder WithAcidBaseReaction() {
        helper.AddPhenomenon(PhenomenonPresets.CreateAcidBaseReaction());
        return this;
    }

    public ExperimentBuilder WithMetalAcidReaction() {
        helper.AddPhenomenon(PhenomenonPresets.CreateMetalAcidReaction());
        return this;
    }

    public ExperimentBuilder WithSodiumWaterReaction() {
        helper.AddPhenomenon(PhenomenonPresets.CreateSodiumWaterReaction());
        return this;
    }

    public ExperimentBuilder WithCombustion() {
        helper.AddPhenomenon(PhenomenonPresets.CreateCombustionReaction());
        return this;
    }

    public ExperimentBuilder WithMagnetization() {
        helper.AddPhenomenon(PhenomenonPresets.CreateMagnetizationPhenomenon());
        return this;
    }

    public void Build() {
        helper.Finalize();
    }
}

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