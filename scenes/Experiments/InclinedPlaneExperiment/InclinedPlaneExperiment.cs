using Godot;

public enum InclinedPlaneExperimentStep {
    Setup,
    PlaceObject,
    AdjustAngle,
    MeasureAngle,
    ReleaseObject,
    RecordData,
    AnalyzeResult,
    Completed
}

public enum InclinedPlaneExperimentItem {
    InclinedPlane,
    SliderObject,
    AngleMeter,
    Ruler,
    Timer,
    DataBoard,
    SupportStand
}

public partial class InclinedPlaneExperiment : StepExperimentLabItem<InclinedPlaneExperimentStep, InclinedPlaneExperimentItem> {
    [Export] protected override InclinedPlaneExperimentStep currentStep { get; set; } = InclinedPlaneExperimentStep.Setup;

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
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

    private void InitializeStepHints() {
        base.stepHints[InclinedPlaneExperimentStep.Setup] = 
            "[b]步骤 1：准备阶段[/b]\n\n" +
            "• 将实验台放置在平稳的表面上\n" +
            "• 从实验物品中选择斜坡板（InclinedPlane）\n" +
            "• 将斜坡板放置在实验台上\n" +
            "• 确保斜坡板稳固，不会滑动\n\n" +
            "[color=yellow]提示：[/color] 可以使用鼠标拖拽来移动和放置物品";
        base.stepHints[InclinedPlaneExperimentStep.PlaceObject] = 
            "[b]步骤 2：放置物体[/b]\n\n" +
            "• 选择实验物体（滑块/SliderObject）\n" +
            "• 将物体轻轻放置在斜坡的顶部位置\n" +
            "• 确保物体放置在斜坡表面，而不是悬空\n" +
            "• 物体应该能够沿着斜坡滑动\n\n" +
            "[color=yellow]提示：[/color] 物体应该放在斜坡顶部，准备进行实验";
        base.stepHints[InclinedPlaneExperimentStep.AdjustAngle] = 
            "[b]步骤 3：调整角度[/b]\n\n" +
            "• 使用支撑架（SupportStand）来调整斜坡的角度\n" +
            "• 可以逐渐增加或减少斜坡的倾斜角度\n" +
            "• 观察物体在不同角度下的状态\n" +
            "• 选择合适的角度进行实验（建议从较小角度开始）\n\n" +
            "[color=yellow]提示：[/color] 角度越大，物体下滑的速度越快";
        base.stepHints[InclinedPlaneExperimentStep.MeasureAngle] = 
            "[b]步骤 4：测量角度[/b]\n\n" +
            "• 使用角度测量器（AngleMeter）来测量斜坡的倾斜角度\n" +
            "• 将角度测量器放置在斜坡表面\n" +
            "• 读取并记录角度数值\n" +
            "• 可以尝试测量多个不同的角度\n\n" +
            "[color=yellow]提示：[/color] 准确记录角度值，这对后续数据分析很重要";
        base.stepHints[InclinedPlaneExperimentStep.ReleaseObject] = 
            "[b]步骤 5：释放物体[/b]\n\n" +
            "• 确认物体已放置在斜坡顶部\n" +
            "• 准备好计时器（Timer）开始计时\n" +
            "• 释放物体，让它沿着斜坡自由滑动\n" +
            "• 观察物体的运动情况\n\n" +
            "[color=yellow]提示：[/color] 释放时要确保物体从静止状态开始运动";
        base.stepHints[InclinedPlaneExperimentStep.RecordData] = 
            "[b]步骤 6：记录数据[/b]\n\n" +
            "• 使用计时器记录物体从顶部滑到底部的时间\n" +
            "• 使用尺子（Ruler）测量斜坡的长度\n" +
            "• 将测量数据记录到数据记录板（DataBoard）上\n" +
            "• 可以尝试不同角度，记录多组数据\n" +
            "• 记录内容包括：角度、时间、距离等\n\n" +
            "[color=yellow]提示：[/color] 多次测量取平均值可以提高实验精度";
        base.stepHints[InclinedPlaneExperimentStep.AnalyzeResult] = 
            "[b]步骤 7：分析结果[/b]\n\n" +
            "• 查看数据记录板上的所有实验数据\n" +
            "• 分析角度与运动时间的关系\n" +
            "• 观察是否存在规律性\n" +
            "• 思考：为什么角度越大，物体下滑越快？\n" +
            "• 可以绘制角度-时间的图表来直观分析\n\n" +
            "[color=yellow]提示：[/color] 这与重力分力和摩擦力有关，角度越大，重力沿斜坡的分力越大";
        base.stepHints[InclinedPlaneExperimentStep.Completed] = 
            "[b]实验完成！[/b]\n\n" +
            "恭喜你完成了斜坡实验！\n\n" +
            "[color=lightgreen]实验总结：[/color]\n" +
            "• 你已经成功完成了所有实验步骤\n" +
            "• 记录了实验数据并进行了分析\n" +
            "• 理解了斜坡角度对物体运动的影响\n\n" +
            "可以重新开始实验，尝试不同的角度和物体，探索更多有趣的物理现象！";
    }

    protected override string GetStepName(InclinedPlaneExperimentStep step) {
        switch (step) {
            case InclinedPlaneExperimentStep.Setup:
                return "准备阶段";
            case InclinedPlaneExperimentStep.PlaceObject:
                return "放置物体";
            case InclinedPlaneExperimentStep.AdjustAngle:
                return "调整角度";
            case InclinedPlaneExperimentStep.MeasureAngle:
                return "测量角度";
            case InclinedPlaneExperimentStep.ReleaseObject:
                return "释放物体";
            case InclinedPlaneExperimentStep.RecordData:
                return "记录数据";
            case InclinedPlaneExperimentStep.AnalyzeResult:
                return "分析结果";
            case InclinedPlaneExperimentStep.Completed:
                return "实验完成";
            default:
                return "未知步骤";
        }
    }

    protected override InclinedPlaneExperimentStep SetupStep => InclinedPlaneExperimentStep.Setup;
    
    protected override InclinedPlaneExperimentStep CompletedStep => InclinedPlaneExperimentStep.Completed;

    public void OnItemsCollided(
        InclinedPlaneExperimentItem aType,
        InclinedPlaneExperimentItem bType,
        InclinedPlanePlacableItem a,
        InclinedPlanePlacableItem b
    ) {
        if (!IsStepCompleted(InclinedPlaneExperimentStep.ReleaseObject)) {
            bool hasSlider = aType == InclinedPlaneExperimentItem.SliderObject || bType == InclinedPlaneExperimentItem.SliderObject;
            bool hasSupport = aType == InclinedPlaneExperimentItem.SupportStand || bType == InclinedPlaneExperimentItem.SupportStand;
            if (hasSlider && hasSupport) {
                base.SetCurrentStep(InclinedPlaneExperimentStep.RecordData);
            }
        }
    }
}