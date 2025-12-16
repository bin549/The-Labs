using Godot;
using Godot.Collections;

public enum AluminumReactionExperimentStep {
    Setup,
    PrepareReagents,
    AddReagents,
    ObserveReaction,
    CollectGas,
    RecordData,
    AnalyzeResult,
    Completed
}

public enum AluminumReactionExperimentItem {
    Beaker,
    AluminumStrip,
    SodiumHydroxideSolution,
    TestTube,
    GasTube,
    WaterTank,
    DataBoard,
    Thermometer
}

public partial class AluminumReactionExperiment : StepExperimentLabItem<AluminumReactionExperimentStep, AluminumReactionExperimentItem> {
    [Export] protected override AluminumReactionExperimentStep currentStep { get; set; } = AluminumReactionExperimentStep.Setup;
    [Export] private PlacableItem sodiumHydroxideSolution;
    [Export] private Node3D emptyReagent1;
    [Export] private Area3D triggerArea1;
    [Export] private Label3D collisionLabel1;
    private bool isSodiumHydroxideInArea = false;
    private bool isItemPlaced = false;
    [Export] private Node3D fillObjects;
    [Export] private AnimationPlayer animationPlayer;
    private Transform3D sodiumHydroxideSolutionInitialTransform;
    [Export] private Node3D emptyReagent2;
    [Export] private Area3D triggerArea2;
    [Export] private Label3D collisionLabel2;
    private bool isStepTwoCollisionInitialized = false;
    [Export] private Node3D matchBox;
    [Export] private PlacableItem matchItem;

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.SetupStepOneCollision();
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolutionInitialTransform = this.sodiumHydroxideSolution.GlobalTransform;
        }
    }

    private void SetupStepOneCollision() {
        this.isItemPlaced = false;
        if (this.collisionLabel1 != null) {
            this.collisionLabel1.Visible = false;
        }
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolution.StopDragging();
        }
        if (this.emptyReagent1 is PlacableItem emptyReagentItem) {
            emptyReagentItem.StopDragging();
        }
        if (this.triggerArea1 != null) {
            this.triggerArea1.BodyEntered += OnTriggerAreaBodyEntered;
            this.triggerArea1.BodyExited += OnTriggerAreaBodyExited;
            this.triggerArea1.AreaEntered += OnTriggerAreaEntered;
            this.triggerArea1.AreaExited += OnTriggerAreaExited;
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
        base.isInteracting = false;
        base.ExitInteraction();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) return;
        if (this.currentStep == AluminumReactionExperimentStep.CollectGas) {
            this.HandleMatchBoxInput(@event);
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (!base.isInteracting) {
            return;
        }

        if (this.currentStep == AluminumReactionExperimentStep.Setup) {
            this.UpdateCollisionLabel();
        } else if (this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            if (!this.isStepTwoCollisionInitialized) {
                this.SetupStepTwoCollision();
                this.isStepTwoCollisionInitialized = true;
            }
            this.UpdateCollisionLabel();
        }
    }

    private void UpdateCollisionLabel() {
        if (this.sodiumHydroxideSolution == null || this.isItemPlaced) {
            return;
        }
        if (this.sodiumHydroxideSolution.IsDragging) {
            this.ShowCollisionLabel(this.isSodiumHydroxideInArea);
        } else {
            if (this.isSodiumHydroxideInArea && !this.isItemPlaced) {
                this.OnItemPlaced();
            }
        }
    }

    private void OnItemPlaced() {
        if (this.isItemPlaced) {
            return;
        }
        this.ShowCollisionLabel(false);
        this.isItemPlaced = true;
        this.sodiumHydroxideSolution.Visible = false;
        if (this.currentStep == AluminumReactionExperimentStep.Setup) {
            if (this.emptyReagent1 != null) {
                this.emptyReagent1.Visible = false;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            if (this.emptyReagent2 != null) {
                this.emptyReagent2.Visible = false;
            }
        }
        this.fillObjects.Visible = true;
        this.animationPlayer.Play("fill");
    }

    private void OnItemPlacedDone() {
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolution.GlobalTransform = this.sodiumHydroxideSolutionInitialTransform;
        }
        this.sodiumHydroxideSolution.Visible = true;
        if (this.currentStep == AluminumReactionExperimentStep.Setup) {
            this.emptyReagent1.Visible = true;
        } else if (this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            this.emptyReagent2.Visible = true;
            this.sodiumHydroxideSolution.IsDraggable = false;
        }
        this.fillObjects.Visible = false;
        this.CompleteCurrentStep();
    }

    private void OnTriggerAreaBodyEntered(Node3D body) {
        this.HandleTriggerAreaCollision(body);
    }

    private void OnTriggerAreaBodyExited(Node3D body) {
        this.HandleTriggerAreaExit(body);
    }

    private void OnTriggerAreaEntered(Area3D area) {
        this.HandleTriggerAreaCollision(area);
    }

    private void OnTriggerAreaExited(Area3D area) {
        this.HandleTriggerAreaExit(area);
    }

    private void HandleTriggerAreaCollision(Node node) {
        if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
            this.isSodiumHydroxideInArea = true;
            if (this.sodiumHydroxideSolution != null && this.sodiumHydroxideSolution.IsDragging) {
                this.ShowCollisionLabel(true);
            }
        }
    }

    private void HandleTriggerAreaExit(Node node) {
        if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
            this.isSodiumHydroxideInArea = false;
            this.ShowCollisionLabel(false);
        }
    }

    private bool IsNodePartOfItem(Node node, PlacableItem item) {
        if (node == null || item == null) {
            return false;
        }
        if (node == item) {
            return true;
        }
        Node current = node;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == item) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private void ShowCollisionLabel(bool isShow) {
        if (this.currentStep == AluminumReactionExperimentStep.Setup) {
            if (this.collisionLabel1 != null) {
                this.collisionLabel1.Visible = isShow;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            if (this.collisionLabel2 != null) {
                this.collisionLabel2.Visible = isShow;
            }
        }
    }

    private void SetupStepTwoCollision() {
        this.isItemPlaced = false;
        this.isSodiumHydroxideInArea = false;
        if (this.collisionLabel2 != null) {
            this.collisionLabel2.Visible = false;
        }
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolution.StopDragging();
        }
        if (this.emptyReagent2 is PlacableItem emptyReagentItem) {
            emptyReagentItem.StopDragging();
        }
        if (this.triggerArea2 != null) {
            this.triggerArea2.BodyEntered += OnTriggerAreaBodyEntered;
            this.triggerArea2.BodyExited += OnTriggerAreaBodyExited;
            this.triggerArea2.AreaEntered += OnTriggerAreaEntered;
            this.triggerArea2.AreaExited += OnTriggerAreaExited;
        }
    }

    private void HandleMatchBoxInput(InputEvent @event) {
        if (this.matchBox == null || this.matchItem == null) {
            return;
        }
        if (@event is InputEventMouseButton mouseButton) {
            bool leftPressed = mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed;
            if (!leftPressed) {
                return;
            }
            var intersect = this.GetMouseIntersect(mouseButton.Position);
            if (intersect == null || !intersect.ContainsKey("collider")) {
                return;
            }
            var colliderVariant = intersect["collider"];
            var collider = colliderVariant.As<Node3D>();
            if (collider == null) {
                return;
            }
            if (this.IsNodePartOfNode(collider, this.matchBox)) {
                // 显示火柴并让其在鼠标位置进入拖拽状态
                this.matchItem.Visible = true;
                this.matchItem.IsDraggable = true;
                this.matchItem.StartDraggingAtMouse();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private bool IsNodePartOfNode(Node node, Node target) {
        if (node == null || target == null) {
            return false;
        }
        if (node == target) {
            return true;
        }
        Node current = node;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == target) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private Dictionary GetMouseIntersect(Vector2 mousePos) {
        var currentCamera = GetViewport().GetCamera3D();
        if (currentCamera == null) {
            return null;
        }
        var from = currentCamera.ProjectRayOrigin(mousePos);
        var to = from + currentCamera.ProjectRayNormal(mousePos) * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;
        query.CollideWithAreas = true;
        query.CollisionMask = 0xFFFFFFFF;
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        return result;
    }

    private void InitializeStepHints() {
        base.stepHints[AluminumReactionExperimentStep.Setup] = 
            "[b]步骤 1：将氢氧化钠倒入试剂1";
        base.stepHints[AluminumReactionExperimentStep.PrepareReagents] = 
            "[b]步骤 2：将氢氧化钠倒入试剂2";
        base.stepHints[AluminumReactionExperimentStep.AddReagents] = 
            "[b]步骤 3：将铝片放到试剂1";
        base.stepHints[AluminumReactionExperimentStep.ObserveReaction] = 
            "[b]步骤 4：将铝片放到试剂2";
        base.stepHints[AluminumReactionExperimentStep.CollectGas] = 
            "[b]步骤 5：拾取火柴";
        base.stepHints[AluminumReactionExperimentStep.RecordData] = 
            "[b]步骤 6：点燃木棍";
        base.stepHints[AluminumReactionExperimentStep.AnalyzeResult] = 
            "[b]步骤 7：检测生成气体";
        base.stepHints[AluminumReactionExperimentStep.Completed] = 
            "[b]实验完成！[/b]实验结束";
    }

    protected override string GetStepName(AluminumReactionExperimentStep step) {
        switch (step) {
            case AluminumReactionExperimentStep.Setup:
                return "准备阶段";
            case AluminumReactionExperimentStep.PrepareReagents:
                return "准备试剂";
            case AluminumReactionExperimentStep.AddReagents:
                return "添加试剂";
            case AluminumReactionExperimentStep.ObserveReaction:
                return "观察反应";
            case AluminumReactionExperimentStep.CollectGas:
                return "收集气体";
            case AluminumReactionExperimentStep.RecordData:
                return "记录数据";
            case AluminumReactionExperimentStep.AnalyzeResult:
                return "分析结果";
            case AluminumReactionExperimentStep.Completed:
                return "实验完成";
            default:
                return "未知步骤";
        }
    }

    protected override AluminumReactionExperimentStep SetupStep => AluminumReactionExperimentStep.Setup;
    
    protected override AluminumReactionExperimentStep CompletedStep => AluminumReactionExperimentStep.Completed;
}