using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
    [Export]  public Godot.Collections.Array<NodePath> PlacableItemPaths { get; set; } = new();
    private List<PlacableItem> placableItems = new();
    
    /**
     * 斜坡实验：
     * 物体： 斜坡，小方块，测量仪
     * UI: 调整斜坡
     * 实验结论：
     * 
     */
    
    public override void _Ready() {
        base._Ready();
        foreach (var path in PlacableItemPaths) {
            var node = GetNode<PlacableItem>(path);
            this.placableItems.Add(node);
        }
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) {
            return;
        }
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void ExitInteraction() {
        base.ExitInteraction();
    }
}
