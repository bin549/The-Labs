using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
    public DragPlaneType DragPlane { get; set; } = DragPlaneType.Horizontal;
    [Export] private List<PlacableItem> placableItems = new();

    public override void _Ready() {
        base._Ready();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) {
            return;
        }
        if (@event is InputEventKey keyEvent) {
            if (keyEvent.Keycode == Key.Shift && keyEvent.Pressed && !keyEvent.IsEcho()) {
                this.DragPlane = DragPlaneType.VerticalX;
                this.UpdateAllPlacableItemsDragPlane();
            }
            if (keyEvent.Keycode == Key.Shift && !keyEvent.Pressed) {
                this.DragPlane = DragPlaneType.Horizontal;
                this.UpdateAllPlacableItemsDragPlane();
            }
        }
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        foreach (var placableItem in this.placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.IsDraggable = true;
                placableItem.DragPlane = this.DragPlane;
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

    private void UpdateAllPlacableItemsDragPlane() {
        foreach (var placableItem in this.placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.DragPlane = this.DragPlane;
            }
        }
    }
}
