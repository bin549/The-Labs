using Godot;

// 实验信息
[GlobalClass]
public partial class ExperimentInfo : Resource {
	[Export] public string ExperimentName { get; set; } = "实验";
	[Export] public string Description { get; set; } = "";
	[Export] public ExperimentCategory Category { get; set; } = ExperimentCategory.Mechanics;
	[Export] public Vector3 Position { get; set; } = Vector3.Zero;
	[Export] public NodePath ExperimentNodePath { get; set; }
}

// 实验分类
public enum ExperimentCategory {
	Mechanics,    // 力学
	Electricity,  // 电学
	Chemistry     // 化学
}

