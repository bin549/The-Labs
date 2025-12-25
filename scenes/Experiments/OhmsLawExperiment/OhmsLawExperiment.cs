using Godot;

public enum OhmsLawExperimentStep {
    Step01,
    Step02,
    Step03
}

public enum OhmsLawExperimentItem {
    Breadboard,
    PowerSupply,
    Voltmeter,
    Ammeter,
    Resistor_10Ohm,
    Resistor_20Ohm,
    Resistor_50Ohm,
    Wire,
    DataBoard
}

public partial class OhmsLawExperiment : StepExperimentLabItem<OhmsLawExperimentStep, OhmsLawExperimentItem> {
    [Export] protected override OhmsLawExperimentStep currentStep { get; set; } = OhmsLawExperimentStep.Step01;
    [Export] public NodePath ConnectionManagerPath { get; set; } = new NodePath("/root/World/ConnectionManager");
    private ConnectionManager connectionManager;

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.ResolveConnectionManager();
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        this.EnableConnectionManager(true);
    }

    public override void ExitInteraction() {
        base.isInteracting = false;
        this.EnableConnectionManager(false);
        base.ExitInteraction();
    }

    private void ResolveConnectionManager() {
        if (this.connectionManager != null && GodotObject.IsInstanceValid(this.connectionManager)) return;
        if (ConnectionManagerPath != null && !ConnectionManagerPath.IsEmpty) {
            this.connectionManager = GetNodeOrNull<ConnectionManager>(ConnectionManagerPath);
        }
        if (this.connectionManager == null) {
            this.connectionManager = GetTree().Root.FindChild("ConnectionManager", true, false) as ConnectionManager;
        }
    }

    private void EnableConnectionManager(bool enabled) {
        this.ResolveConnectionManager();
        if (this.connectionManager != null) {
            this.connectionManager.IsEnabled = enabled;
        }
    }

    private void InitializeStepHints() {
        base.stepHints[OhmsLawExperimentStep.Step01] = 
            "将导线按实验要求正确连接。";
        base.stepHints[OhmsLawExperimentStep.Step02] = 
            "合上电路开关，通电实验。";
        base.stepHints[OhmsLawExperimentStep.Step03] = 
            "移动滑动变阻器，观察电流变化。";
    }

    protected override OhmsLawExperimentStep SetupStep => OhmsLawExperimentStep.Step01;
    
    protected override OhmsLawExperimentStep CompletedStep => OhmsLawExperimentStep.Step03;
}
