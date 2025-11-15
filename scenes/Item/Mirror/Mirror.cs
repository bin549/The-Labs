using Godot;
using System;

public partial class Mirror : Interactable {
	public override void _Ready() {
		base._Ready();
	}

	public override void EnterInteraction() {
		base.EnterInteraction();
		GD.Print($"{DisplayName} 已被激活");
	}

	public override void ExitInteraction() {
		base.ExitInteraction();
	}
}
