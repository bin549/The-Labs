using Godot;

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
    [Export] private Node3D emptyReagent;
    [Export] private Area3D triggerArea;
    [Export] private Label3D collisionLabel;
    private bool isSodiumHydroxideInArea = false;
    private bool isItemPlaced = false;

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.SetupStepOneCollision();
    }

    private void SetupStepOneCollision() {
        this.isItemPlaced = false;
        if (this.collisionLabel != null) {
            this.collisionLabel.Visible = false;
        }
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolution.StopDragging();
        }
        if (this.emptyReagent is PlacableItem emptyReagentItem) {
            emptyReagentItem.StopDragging();
        }
        if (this.triggerArea != null) {
            this.triggerArea.BodyEntered += OnTriggerAreaBodyEntered;
            this.triggerArea.BodyExited += OnTriggerAreaBodyExited;
            this.triggerArea.AreaEntered += OnTriggerAreaEntered;
            this.triggerArea.AreaExited += OnTriggerAreaExited;
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
        this.HideStepOneObjects();
        base.ExitInteraction();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) return;
        if (@event is InputEventMouseButton mouseButton && this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            if (mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed) {
                GD.Print($"[AluminumReaction] 鼠标释放: isSodiumHydroxideInArea={this.isSodiumHydroxideInArea}, isItemPlaced={this.isItemPlaced}");
                if (this.sodiumHydroxideSolution != null) {
                    GD.Print($"[AluminumReaction] sodiumHydroxideSolution.IsDragging={this.sodiumHydroxideSolution.IsDragging}");
                }
            }
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (base.isInteracting && this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            this.UpdateCollisionLabel();
        }
    }

    private void UpdateCollisionLabel() {
        if (this.sodiumHydroxideSolution == null || this.isItemPlaced) {
            return;
        }
        if (this.sodiumHydroxideSolution.IsDragging) {
            if (this.isSodiumHydroxideInArea) {
                this.ShowCollisionLabel();
            } else {
                this.HideCollisionLabel();
            }
        } else {
            this.HideCollisionLabel();
            if (this.isSodiumHydroxideInArea && !this.isItemPlaced) {
                GD.Print($"[AluminumReaction] 触发放置: isSodiumHydroxideInArea={this.isSodiumHydroxideInArea}, isItemPlaced={this.isItemPlaced}, IsDragging={this.sodiumHydroxideSolution.IsDragging}");
                this.OnItemPlaced();
            }
        }
    }

    private void OnItemPlaced() {
        if (this.isItemPlaced) {
            return;
        }
        this.isItemPlaced = true;
        this.HideStepOneObjects();
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
                this.ShowCollisionLabel();
            }
        }
    }

    private void HandleTriggerAreaExit(Node node) {
        if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
            this.isSodiumHydroxideInArea = false;
            this.HideCollisionLabel();
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

    private void ShowCollisionLabel() {
        if (this.collisionLabel != null) {
            this.collisionLabel.Visible = true;
        }
    }

    private void HideCollisionLabel() {
        if (this.collisionLabel != null) {
            this.collisionLabel.Visible = false;
        }
    }

    private void HideStepOneObjects() {
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolution.Visible = false;
        }
        if (this.emptyReagent != null) {
            this.emptyReagent.Visible = false;
        }
        this.HideCollisionLabel();
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