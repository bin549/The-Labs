using Godot;
using System.Collections.Generic;

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
        // 如果 sodiumHydroxideSolution 为 null，尝试从场景中查找
        if (this.sodiumHydroxideSolution == null) {
            var node = GetNodeOrNull<Node3D>("LabObjects/sodiumHydroxide");
            if (node != null) {
                this.sodiumHydroxideSolution = node as PlacableItem;
            }
        }
        // 如果 emptyReagent 为 null，尝试从场景中查找
        if (this.emptyReagent == null) {
            this.emptyReagent = GetNodeOrNull<Node3D>("LabObjects/emptyReagent");
        }
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
        
        // 调试：监听鼠标释放事件
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
        
        // 参考 InclinedPlaneExperiment 的逻辑
        if (this.sodiumHydroxideSolution.IsDragging) {
            // 正在拖拽时，根据是否在区域内显示 Label
            if (this.isSodiumHydroxideInArea) {
                this.ShowCollisionLabel();
            } else {
                this.HideCollisionLabel();
            }
        } else {
            // 不在拖拽时，隐藏 Label
            this.HideCollisionLabel();
            // 如果不在拖拽且在触发区域内且未放置，则触发放置
            if (this.isSodiumHydroxideInArea && !this.isItemPlaced) {
                GD.Print($"[AluminumReaction] 触发放置: isSodiumHydroxideInArea={this.isSodiumHydroxideInArea}, isItemPlaced={this.isItemPlaced}, IsDragging={this.sodiumHydroxideSolution.IsDragging}");
                this.OnItemPlaced();
            }
        }
    }

    private void OnItemPlaced() {
        if (this.isItemPlaced) {
            GD.Print("[AluminumReaction] 已经放置过，跳过");
            return;
        }
        GD.Print("[AluminumReaction] 执行放置逻辑");
        this.isItemPlaced = true;
        // 放置完成：隐藏两个物体并进入下一步
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
            GD.Print($"[AluminumReaction] sodiumHydroxideSolution 进入触发区域: {node.Name}");
            this.isSodiumHydroxideInArea = true;
            // 参考 InclinedPlaneExperiment：只在拖拽时显示 Label
            if (this.sodiumHydroxideSolution != null && this.sodiumHydroxideSolution.IsDragging) {
                this.ShowCollisionLabel();
            }
        }
    }

    private void HandleTriggerAreaExit(Node node) {
        if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
            GD.Print($"[AluminumReaction] sodiumHydroxideSolution 离开触发区域: {node.Name}");
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
            "[b]步骤 1：准备阶段[/b]\n\n" +
            "• 从实验物品中选择烧杯（Beaker）\n" +
            "• 将烧杯放置在实验台上\n" +
            "• 确保实验环境安全，准备好防护用品\n\n" +
            "[color=yellow]提示：[/color] 可以使用鼠标拖拽来移动和放置物品";
        base.stepHints[AluminumReactionExperimentStep.PrepareReagents] = 
            "[b]步骤 2：准备试剂[/b]\n\n" +
            "• 准备铝片（AluminumStrip），确保表面清洁\n" +
            "• 准备氢氧化钠溶液（SodiumHydroxideSolution）\n" +
            "• 检查试剂的浓度和纯度\n" +
            "• 将氢氧化钠溶液倒入烧杯中（适量即可）\n\n" +
            "[color=yellow]提示：[/color] 氢氧化钠具有腐蚀性，操作时需小心";
        base.stepHints[AluminumReactionExperimentStep.AddReagents] = 
            "[b]步骤 3：添加试剂[/b]\n\n" +
            "• 将准备好的铝片轻轻放入装有氢氧化钠溶液的烧杯中\n" +
            "• 确保铝片完全浸入溶液中\n" +
            "• 观察铝片与溶液接触后的变化\n" +
            "• 注意观察反应是否立即开始\n\n" +
            "[color=yellow]提示：[/color] 铝片与氢氧化钠反应会产生氢气";
        base.stepHints[AluminumReactionExperimentStep.ObserveReaction] = 
            "[b]步骤 4：观察反应[/b]\n\n" +
            "• 仔细观察反应现象：铝片表面产生气泡\n" +
            "• 观察气泡产生的速度和数量\n" +
            "• 注意溶液温度的变化（可使用温度计）\n" +
            "• 观察铝片是否逐渐溶解\n\n" +
            "[color=yellow]提示：[/color] 反应方程式：2Al + 2NaOH + 6H₂O → 2NaAlO₂ + 3H₂↑";
        base.stepHints[AluminumReactionExperimentStep.CollectGas] = 
            "[b]步骤 5：收集气体[/b]\n\n" +
            "• 使用导管（GasTube）连接烧杯和试管（TestTube）\n" +
            "• 将试管倒置在水槽（WaterTank）中收集氢气\n" +
            "• 观察试管中气体的收集情况\n" +
            "• 确认收集到的是氢气（可以后续进行验证实验）\n\n" +
            "[color=yellow]提示：[/color] 氢气比空气轻，适合用排水法收集";
        base.stepHints[AluminumReactionExperimentStep.RecordData] = 
            "[b]步骤 6：记录数据[/b]\n\n" +
            "• 记录反应开始的时间\n" +
            "• 记录反应过程中的温度变化\n" +
            "• 记录产生的气体体积\n" +
            "• 将测量数据记录到数据记录板（DataBoard）上\n" +
            "• 记录反应现象和观察结果\n\n" +
            "[color=yellow]提示：[/color] 详细记录有助于后续分析反应规律";
        base.stepHints[AluminumReactionExperimentStep.AnalyzeResult] = 
            "[b]步骤 7：分析结果[/b]\n\n" +
            "• 查看数据记录板上的所有实验数据\n" +
            "• 分析反应速率与温度的关系\n" +
            "• 理解铝与氢氧化钠反应的化学原理\n" +
            "• 思考：为什么铝能与强碱反应？\n" +
            "• 总结实验现象和结论\n\n" +
            "[color=yellow]提示：[/color] 铝是两性金属，既能与酸反应，也能与强碱反应";
        base.stepHints[AluminumReactionExperimentStep.Completed] = 
            "[b]实验完成！[/b]\n\n" +
            "恭喜你完成了铝和氢氧化钠反应实验！\n\n" +
            "[color=lightgreen]实验总结：[/color]\n" +
            "• 你已经成功完成了所有实验步骤\n" +
            "• 观察了铝与氢氧化钠的化学反应现象\n" +
            "• 理解了铝的两性金属特性\n" +
            "• 掌握了收集气体的方法\n\n" +
            "可以重新开始实验，尝试不同的条件，探索更多有趣的化学现象！";
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