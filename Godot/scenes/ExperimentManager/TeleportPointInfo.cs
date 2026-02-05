using Godot;

[GlobalClass]
public partial class TeleportPointInfo : Resource {
    [Export] public string PointName { get; set; } = "传送点";
    [Export] public string Description { get; set; } = "";
    [Export] public Vector3 Position { get; set; } = Vector3.Zero;
    [Export] public NodePath PointNodePath { get; set; }
}
