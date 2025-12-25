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
    [Export] private Area3D switchArea;
    [Export] private AnimationPlayer switchAnimationPlayer;
    private ConnectionManager connectionManager;
    private bool isSwitchClosed = false;

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.ResolveConnectionManager();
        this.SetupSwitchArea();
    }

    private void SetupSwitchArea() {
        if (this.switchArea != null) {
            this.switchArea.InputRayPickable = true;
            this.switchArea.Monitorable = true;
            this.switchArea.Monitoring = true;
            this.switchArea.InputEvent += OnSwitchInputEvent;
        }
        if (this.switchAnimationPlayer != null) {
            this.switchAnimationPlayer.AnimationFinished += OnSwitchAnimationFinished;
        }
    }

    private void OnSwitchInputEvent(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx) {
        if (!base.isInteracting) {
            return;
        }
        if (this.currentStep != OhmsLawExperimentStep.Step02) {
            return;
        }
        if (this.isSwitchClosed) {
            return;
        }
        if (@event is InputEventMouseButton mouseButton) {
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed && !mouseButton.IsEcho()) {
                this.OnSwitchClicked();
            }
        }
    }

    private void OnSwitchClicked() {
        if (this.switchAnimationPlayer != null) {
            this.switchAnimationPlayer.Play("close");
        }
    }

    private void OnSwitchAnimationFinished(StringName animName) {
        if (animName == "close") {
            this.isSwitchClosed = true;
            this.SetConnectionsColorToGreen();
            this.CompleteCurrentStep();
        }
    }

    private void SetConnectionsColorToGreen() {
        this.ResolveConnectionManager();
        if (this.connectionManager != null) {
            this.connectionManager.SetConnectionsColorInExperiment(this, Colors.Green);
        }
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        this.UpdateConnectionManagerState();
    }

    protected override void OnStepChanged(OhmsLawExperimentStep previousStep, OhmsLawExperimentStep newStep) {
        base.OnStepChanged(previousStep, newStep);
        this.UpdateConnectionManagerState();
        if (newStep == OhmsLawExperimentStep.Step02) {
            this.isSwitchClosed = false;
        }
    }

    private void UpdateConnectionManagerState() {
        bool shouldEnable = this.currentStep == OhmsLawExperimentStep.Step01;
        this.EnableConnectionManager(shouldEnable);
    }

    public override void ExitInteraction() {
        base.isInteracting = false;
        this.EnableConnectionManager(false);
        base.ExitInteraction();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (!base.isInteracting) {
            return;
        }
        if (this.currentStep == OhmsLawExperimentStep.Step01) {
            this.CheckConnectionCount();
        }
    }

    private void CheckConnectionCount() {
        this.ResolveConnectionManager();
        if (this.connectionManager != null) {
            int connectionCount = this.connectionManager.GetConnectionCountInExperiment(this);
            if (connectionCount >= 7) {
                this.CompleteCurrentStep();
            }
        }
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
