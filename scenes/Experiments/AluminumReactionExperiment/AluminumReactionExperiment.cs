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
    // 步骤三和步骤四：镊子和铝片
    [Export] private PlacableItem tweezers;
    [Export] private Node3D aluminumStrip1;
    [Export] private Area3D triggerArea3;
    [Export] private Label3D collisionLabel3;
    private bool isTweezersInArea3 = false;
    private bool isItemPlaced3 = false;
    private bool isStepThreeCollisionInitialized = false;
    [Export] private Node3D aluminumStrip2;
    [Export] private Area3D triggerArea4;
    [Export] private Label3D collisionLabel4;
    private bool isTweezersInArea4 = false;
    private bool isItemPlaced4 = false;
    private bool isStepFourCollisionInitialized = false;
    private Transform3D tweezersInitialTransform;
    private bool wasTweezersDragging = false;
    [Export] private float tweezersDragRotationAngle = 45.0f; // 拖拽时的旋转角度（度）

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.SetupStepOneCollision();
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolutionInitialTransform = this.sodiumHydroxideSolution.GlobalTransform;
        }
        if (this.tweezers != null) {
            this.tweezersInitialTransform = this.tweezers.GlobalTransform;
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
        } else if (this.currentStep == AluminumReactionExperimentStep.AddReagents) {
            if (!this.isStepThreeCollisionInitialized) {
                this.SetupStepThreeCollision();
                this.isStepThreeCollisionInitialized = true;
            }
            this.UpdateAluminumCollisionLabel();
        } else if (this.currentStep == AluminumReactionExperimentStep.ObserveReaction) {
            if (!this.isStepFourCollisionInitialized) {
                this.SetupStepFourCollision();
                this.isStepFourCollisionInitialized = true;
            }
            this.UpdateAluminumCollisionLabel();
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

    private void UpdateAluminumCollisionLabel() {
        if (this.tweezers == null) {
            return;
        }
        bool isInArea = false;
        bool isPlaced = false;
        if (this.currentStep == AluminumReactionExperimentStep.AddReagents) {
            isInArea = this.isTweezersInArea3;
            isPlaced = this.isItemPlaced3;
        } else if (this.currentStep == AluminumReactionExperimentStep.ObserveReaction) {
            isInArea = this.isTweezersInArea4;
            isPlaced = this.isItemPlaced4;
        }
        if (isPlaced) {
            return;
        }
        
        // 处理拖拽时的旋转
        bool isDragging = this.tweezers.IsDragging;
        if (isDragging && !this.wasTweezersDragging) {
            // 开始拖拽：应用旋转角度
            this.ApplyTweezersDragRotation();
        } else if (!isDragging && this.wasTweezersDragging) {
            // 结束拖拽：恢复原始旋转
            this.RestoreTweezersRotation();
        }
        this.wasTweezersDragging = isDragging;
        
        if (isDragging) {
            this.ShowAluminumCollisionLabel(isInArea);
        } else {
            if (isInArea && !isPlaced) {
                this.OnAluminumPlaced();
            }
        }
    }

    private void ApplyTweezersDragRotation() {
        if (this.tweezers == null) {
            return;
        }
        // 基于初始旋转，绕Z轴旋转一定角度（可以根据需要改为X轴或Y轴）
        Vector3 initialRotation = this.tweezersInitialTransform.Basis.GetEuler();
        Vector3 initialRotationDegrees = new Vector3(
            Mathf.RadToDeg(initialRotation.X),
            Mathf.RadToDeg(initialRotation.Y),
            Mathf.RadToDeg(initialRotation.Z)
        );
        this.tweezers.RotationDegrees = new Vector3(
            initialRotationDegrees.X,
            initialRotationDegrees.Y,
            initialRotationDegrees.Z + this.tweezersDragRotationAngle
        );
    }

    private void RestoreTweezersRotation() {
        if (this.tweezers == null) {
            return;
        }
        // 恢复初始旋转（从保存的Transform中获取）
        Vector3 initialRotation = this.tweezersInitialTransform.Basis.GetEuler();
        this.tweezers.Rotation = initialRotation;
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

    private void OnAluminumPlaced() {
        if (this.currentStep == AluminumReactionExperimentStep.AddReagents) {
            if (this.isItemPlaced3) {
                return;
            }
            this.ShowAluminumCollisionLabel(false);
            this.isItemPlaced3 = true;
            this.tweezers.Visible = false;
            if (this.aluminumStrip1 != null) {
                this.aluminumStrip1.Visible = false;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.ObserveReaction) {
            if (this.isItemPlaced4) {
                return;
            }
            this.ShowAluminumCollisionLabel(false);
            this.isItemPlaced4 = true;
            this.tweezers.Visible = false;
            if (this.aluminumStrip2 != null) {
                this.aluminumStrip2.Visible = false;
            }
        }
        this.fillObjects.Visible = true;
        this.animationPlayer.Play("fill");
    }

    private void OnItemPlacedDone() {
        if (this.currentStep == AluminumReactionExperimentStep.Setup || 
            this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
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
        } else if (this.currentStep == AluminumReactionExperimentStep.AddReagents) {
            if (this.tweezers != null) {
                this.tweezers.GlobalTransform = this.tweezersInitialTransform;
            }
            this.tweezers.Visible = true;
            if (this.aluminumStrip1 != null) {
                this.aluminumStrip1.Visible = true;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.ObserveReaction) {
            if (this.tweezers != null) {
                this.tweezers.GlobalTransform = this.tweezersInitialTransform;
            }
            this.tweezers.Visible = true;
            if (this.aluminumStrip2 != null) {
                this.aluminumStrip2.Visible = true;
            }
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
        // 步骤一和步骤二：检测氢氧化钠溶液
        if (this.currentStep == AluminumReactionExperimentStep.Setup || 
            this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
                this.isSodiumHydroxideInArea = true;
                if (this.sodiumHydroxideSolution != null && this.sodiumHydroxideSolution.IsDragging) {
                    this.ShowCollisionLabel(true);
                }
            }
        }
        // 步骤三：检测镊子与铝片1
        else if (this.currentStep == AluminumReactionExperimentStep.AddReagents) {
            if (this.IsNodePartOfItem(node, this.tweezers)) {
                this.isTweezersInArea3 = true;
                if (this.tweezers != null && this.tweezers.IsDragging) {
                    this.ShowAluminumCollisionLabel(true);
                }
            }
        }
        // 步骤四：检测镊子与铝片2
        else if (this.currentStep == AluminumReactionExperimentStep.ObserveReaction) {
            if (this.IsNodePartOfItem(node, this.tweezers)) {
                this.isTweezersInArea4 = true;
                if (this.tweezers != null && this.tweezers.IsDragging) {
                    this.ShowAluminumCollisionLabel(true);
                }
            }
        }
    }

    private void HandleTriggerAreaExit(Node node) {
        // 步骤一和步骤二：检测氢氧化钠溶液
        if (this.currentStep == AluminumReactionExperimentStep.Setup || 
            this.currentStep == AluminumReactionExperimentStep.PrepareReagents) {
            if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
                this.isSodiumHydroxideInArea = false;
                this.ShowCollisionLabel(false);
            }
        }
        // 步骤三：检测镊子与铝片1
        else if (this.currentStep == AluminumReactionExperimentStep.AddReagents) {
            if (this.IsNodePartOfItem(node, this.tweezers)) {
                this.isTweezersInArea3 = false;
                this.ShowAluminumCollisionLabel(false);
            }
        }
        // 步骤四：检测镊子与铝片2
        else if (this.currentStep == AluminumReactionExperimentStep.ObserveReaction) {
            if (this.IsNodePartOfItem(node, this.tweezers)) {
                this.isTweezersInArea4 = false;
                this.ShowAluminumCollisionLabel(false);
            }
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

    private void ShowAluminumCollisionLabel(bool isShow) {
        if (this.currentStep == AluminumReactionExperimentStep.AddReagents) {
            if (this.collisionLabel3 != null) {
                this.collisionLabel3.Visible = isShow;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.ObserveReaction) {
            if (this.collisionLabel4 != null) {
                this.collisionLabel4.Visible = isShow;
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

    private void SetupStepThreeCollision() {
        this.isItemPlaced3 = false;
        this.isTweezersInArea3 = false;
        this.wasTweezersDragging = false;
        if (this.collisionLabel3 != null) {
            this.collisionLabel3.Visible = false;
        }
        if (this.tweezers != null) {
            this.tweezers.StopDragging();
            // 确保旋转已恢复
            this.RestoreTweezersRotation();
        }
        if (this.aluminumStrip1 is PlacableItem aluminumStripItem) {
            aluminumStripItem.StopDragging();
        }
        if (this.triggerArea3 != null) {
            this.triggerArea3.BodyEntered += OnTriggerAreaBodyEntered;
            this.triggerArea3.BodyExited += OnTriggerAreaBodyExited;
            this.triggerArea3.AreaEntered += OnTriggerAreaEntered;
            this.triggerArea3.AreaExited += OnTriggerAreaExited;
        }
    }

    private void SetupStepFourCollision() {
        this.isItemPlaced4 = false;
        this.isTweezersInArea4 = false;
        this.wasTweezersDragging = false;
        if (this.collisionLabel4 != null) {
            this.collisionLabel4.Visible = false;
        }
        if (this.tweezers != null) {
            this.tweezers.StopDragging();
            // 确保旋转已恢复
            this.RestoreTweezersRotation();
        }
        if (this.aluminumStrip2 is PlacableItem aluminumStripItem) {
            aluminumStripItem.StopDragging();
        }
        if (this.triggerArea4 != null) {
            this.triggerArea4.BodyEntered += OnTriggerAreaBodyEntered;
            this.triggerArea4.BodyExited += OnTriggerAreaBodyExited;
            this.triggerArea4.AreaEntered += OnTriggerAreaEntered;
            this.triggerArea4.AreaExited += OnTriggerAreaExited;
        }
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