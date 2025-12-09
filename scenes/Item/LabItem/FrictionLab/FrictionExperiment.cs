using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
    [Export]  public Godot.Collections.Array<NodePath> PlacableItemPaths { get; set; } = new();
    private List<PlacableItem> placableItems = new();

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
        foreach (var placableItem in this.placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.IsDraggable = true;
            }
        }
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void ExitInteraction() {
        foreach (var placableItem in this.placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.IsDraggable = false;
            }
        }
        base.ExitInteraction();
    }
}
