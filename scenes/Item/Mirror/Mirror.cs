using Godot;
using System;

public partial class Mirror : Interactable {
	public override void EnterInteraction() {
		base.EnterInteraction();
		GD.Print($"{DisplayName} 已被激活");
	}

	public override void ExitInteraction() {
		base.ExitInteraction();
	}
	
	// 对话结束时确保名称标签和指示线显示
	protected override void OnDialogueFinished() {
		base.OnDialogueFinished();
		// 对话结束后，重新显示标签和指示线（因为 EnterInteraction 会隐藏它们）
		if (this.nameLabel != null) {
			this.nameLabel.Visible = true;
		}
		if (this.lineNode != null) {
			this.lineNode.Visible = true;
		}
		GD.Print($"{DisplayName}: 对话结束，名称标签和指示线已恢复显示");
	}
}
