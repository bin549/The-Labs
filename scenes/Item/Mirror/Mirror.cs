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

    protected override void OnDialogueFinished() {
        base.OnDialogueFinished();
        if (this.nameLabel != null) {
            this.nameLabel.Visible = true;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = true;
        }
        GD.Print($"{DisplayName}: 对话结束，名称标签和指示线已恢复显示");
    }
}
