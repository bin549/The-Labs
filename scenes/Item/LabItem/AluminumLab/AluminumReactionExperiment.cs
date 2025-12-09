using Godot;
using System.Collections.Generic;

public partial class AluminumReactionExperiment : LabItem {
    public override void _Ready() {
        base._Ready();
    }

    public override void _Process(double delta) {
    }
    
    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) {
            return;
        }
    }
    
    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void ExitInteraction() {
        base.ExitInteraction();
    }
}
