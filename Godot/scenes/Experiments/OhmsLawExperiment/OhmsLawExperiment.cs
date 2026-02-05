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
    [Export] private ConnectableNode potentiometer;
    private ConnectionManager connectionManager;
    private bool isSwitchClosed = false;
    private bool isDraggingPotentiometer = false;
    private bool isPotentiometerHovered = false;
    private Vector3 potentiometerInitialPosition;
    private Vector2 dragStartMousePosition;

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.ResolveConnectionManager();
        this.SetupSwitchArea();
        this.InitializePotentiometer();
    }

    private void InitializePotentiometer() {
        if (this.potentiometer != null) {
            this.potentiometerInitialPosition = this.potentiometer.GlobalPosition;
        }
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
            this.connectionManager.SetConnectionsColorInExperiment(this, Colors.DarkGreen);
        }
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        this.UpdateConnectionManagerState();
        this.UpdatePotentiometerState();
    }

    protected override void OnStepChanged(OhmsLawExperimentStep previousStep, OhmsLawExperimentStep newStep) {
        base.OnStepChanged(previousStep, newStep);
        this.UpdateConnectionManagerState();
        this.UpdatePotentiometerState();
        if (newStep == OhmsLawExperimentStep.Step02) {
            this.isSwitchClosed = false;
        }
    }

    private void UpdateConnectionManagerState() {
        bool shouldEnable = this.currentStep == OhmsLawExperimentStep.Step01;
        this.EnableConnectionManager(shouldEnable);
    }

    private void UpdatePotentiometerState() {
    }

    public override void ExitInteraction() {
        base.isInteracting = false;
        this.EnableConnectionManager(false);
        if (this.isPotentiometerHovered && this.potentiometer != null) {
            this.potentiometer.OnHoverExit();
            this.isPotentiometerHovered = false;
        }
        this.isDraggingPotentiometer = false;
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
        if (this.isDraggingPotentiometer && this.currentStep == OhmsLawExperimentStep.Step03) {
            this.UpdatePotentiometerDrag();
        }
    }

    public override void _Input(InputEvent @event) {
        base._Input(@event);
        if (!base.isInteracting) {
            return;
        }
        if (this.currentStep != OhmsLawExperimentStep.Step03) {
            return;
        }
        if (this.potentiometer == null) {
            return;
        }
        if (@event is InputEventMouseMotion motionEvent) {
            this.HandlePotentiometerMouseMotion(motionEvent.Position);
        }
        if (@event is InputEventMouseButton mouseButton) {
            if (mouseButton.ButtonIndex == MouseButton.Left) {
                if (mouseButton.Pressed && !mouseButton.IsEcho()) {
                    this.HandlePotentiometerMouseDown(mouseButton.Position);
                } else if (!mouseButton.Pressed && this.isDraggingPotentiometer) {
                    this.HandlePotentiometerMouseUp();
                }
            }
        }
    }

    private void HandlePotentiometerMouseMotion(Vector2 mousePosition) {
        bool isOver = this.IsMouseOverPotentiometer(mousePosition);
        if (isOver != this.isPotentiometerHovered) {
            this.isPotentiometerHovered = isOver;
            if (this.isPotentiometerHovered) {
                this.potentiometer.OnHoverEnter();
            } else {
                this.potentiometer.OnHoverExit();
            }
        }
    }

    private void HandlePotentiometerMouseDown(Vector2 mousePosition) {
        if (this.IsMouseOverPotentiometer(mousePosition)) {
            this.isDraggingPotentiometer = true;
            this.dragStartMousePosition = mousePosition;
            this.potentiometerInitialPosition = this.potentiometer.GlobalPosition;
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandlePotentiometerMouseUp() {
        this.isDraggingPotentiometer = false;
    }

    private bool IsMouseOverPotentiometer(Vector2 mousePosition) {
        var camera = GetViewport().GetCamera3D();
        if (camera == null) {
            return false;
        }
        var from = camera.ProjectRayOrigin(mousePosition);
        var to = from + camera.ProjectRayNormal(mousePosition) * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;
        query.CollideWithAreas = true;
        query.CollisionMask = 1 << 19;
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        
        if (result == null || !result.ContainsKey("collider")) {
            return false;
        }
        var collider = result["collider"].As<Node>();
        if (collider == null) {
            return false;
        }
        Node current = collider;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == this.potentiometer) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private void UpdatePotentiometerDrag() {
        var currentMousePos = GetViewport().GetMousePosition();
        var mouseDelta = currentMousePos - this.dragStartMousePosition;
        Node3D parent = this.potentiometer.GetParent() as Node3D;
        Vector3 localZAxis;
        if (parent != null) {
            localZAxis = parent.GlobalTransform.Basis.Z;
        } else {
            localZAxis = Vector3.Forward;
        }
        var movementDistance = -mouseDelta.X * 0.01f;
        var newPosition = this.potentiometerInitialPosition + localZAxis * movementDistance;
        this.potentiometer.GlobalPosition = newPosition;
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
