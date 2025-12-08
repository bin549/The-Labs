using Godot;
using System;

public partial class Mirror : Interactable {
    public override void EnterInteraction() {
        base.EnterInteraction();
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
    }
}
