using Godot;

public partial class OhmsLawExperiment : LabItem {
	public override void _Ready() {
		base._Ready();
		GD.Print("欧姆定律实验已加载 - 当前仅用于测试连线功能");
	}
}
