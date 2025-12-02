using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class ExperimentPhenomenon : Resource {
    [ExportGroup("现象基本信息")] [Export] public string PhenomenonName { get; set; } = "实验现象";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public bool IsEnabled { get; set; } = true;
    [ExportGroup("触发条件")] [Export] public string TriggerItemType { get; set; } = "";
    [Export] public Godot.Collections.Array<string> RequiredItemTypes { get; set; } = new();
    [Export] public bool RequireAllItems { get; set; } = true;
    [Export] public float TriggerDelay { get; set; } = 0.0f;
    [ExportGroup("视觉效果")] [Export] public PackedScene ParticleEffect { get; set; }
    [Export] public Color EffectColor { get; set; } = Colors.White;
    [Export] public float EffectDuration { get; set; } = 3.0f;
    [Export] public bool PlaySound { get; set; } = false;
    [Export] public AudioStream SoundEffect { get; set; }
    [ExportGroup("现象结果")] [Export] public bool ShowMessage { get; set; } = true;
    [Export(PropertyHint.MultilineText)] public string ResultMessage { get; set; } = "";
    [Export] public bool ProduceNewItem { get; set; } = false;
    [Export] public PackedScene ProducedItemScene { get; set; }
    [Export] public bool ConsumeItems { get; set; } = false;

    public bool CheckTriggerCondition(PlacableItem mainItem, Godot.Collections.Array<PlacableItem> overlappingItems) {
        if (!IsEnabled) return false;
        if (mainItem.ItemType != TriggerItemType) return false;
        if (RequiredItemTypes.Count == 0) return true;
        var foundTypes = new HashSet<string>();
        foreach (var item in overlappingItems) {
            if (RequiredItemTypes.Contains(item.ItemType)) {
                foundTypes.Add(item.ItemType);
            }
        }
        if (RequireAllItems) {
            return foundTypes.Count == RequiredItemTypes.Count;
        } else {
            return foundTypes.Count > 0;
        }
    }
}
