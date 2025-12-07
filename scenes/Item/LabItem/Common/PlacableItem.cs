using Godot;

public partial class PlacableItem : Node3D {
	[Export] public bool IsDraggable { get; set; } = true;
}
