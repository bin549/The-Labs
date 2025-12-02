using Godot;

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
