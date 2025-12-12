using Godot;

public enum FrictionExperimentStep {
    Setup,
    PrepareEquipment,
    PlaceObject,
    AddWeight,
    MeasureForce,
    RecordData,
    AnalyzeResult,
    Completed
}

public enum FrictionExperimentItem {
    FrictionBlock,
    FrictionTable,
    WeightSet,
    SpringScale,
    DataBoard,
    Ruler
}

public partial class FrictionExperiment : StepExperimentLabItem<FrictionExperimentStep, FrictionExperimentItem> {
    [Export] protected override FrictionExperimentStep currentStep { get; set; } = FrictionExperimentStep.Setup;

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
        base.isInteracting = false;
        base.ExitInteraction();
    }

    private void InitializeStepHints() {
        base.stepHints[FrictionExperimentStep.Setup] = 
            "[b]步骤 1：准备阶段[/b]\n\n" +
            "• 将实验台放置在平稳的表面上\n" +
            "• 准备摩擦力实验所需的材料\n" +
            "• 检查所有实验器材是否齐全\n" +
            "• 确保实验环境安全\n\n" +
            "[color=yellow]提示：[/color] 摩擦力是阻碍物体相对运动的力";
        base.stepHints[FrictionExperimentStep.PrepareEquipment] = 
            "[b]步骤 2：准备器材[/b]\n\n" +
            "• 将摩擦台（FrictionTable）放置在实验台上\n" +
            "• 准备摩擦块（FrictionBlock）\n" +
            "• 准备弹簧测力计（SpringScale）\n" +
            "• 准备不同质量的砝码（WeightSet）\n\n" +
            "[color=yellow]提示：[/color] 确保接触面清洁，以便获得准确的实验结果";
        base.stepHints[FrictionExperimentStep.PlaceObject] = 
            "[b]步骤 3：放置物体[/b]\n\n" +
            "• 将摩擦块放置在摩擦台表面\n" +
            "• 确保摩擦块平稳放置\n" +
            "• 将弹簧测力计连接到摩擦块上\n" +
            "• 检查连接是否牢固\n\n" +
            "[color=yellow]提示：[/color] 保持弹簧测力计水平，使测量更准确";
        base.stepHints[FrictionExperimentStep.AddWeight] = 
            "[b]步骤 4：添加砝码[/b]\n\n" +
            "• 在摩擦块上放置不同质量的砝码\n" +
            "• 记录每次添加的砝码质量\n" +
            "• 观察压力增加对摩擦力的影响\n" +
            "• 可以进行多次实验，改变砝码数量\n\n" +
            "[color=yellow]提示：[/color] 压力越大，摩擦力越大";
        base.stepHints[FrictionExperimentStep.MeasureForce] = 
            "[b]步骤 5：测量摩擦力[/b]\n\n" +
            "• 使用弹簧测力计水平拉动摩擦块\n" +
            "• 当物体开始移动时，读取测力计的读数\n" +
            "• 这个读数就是最大静摩擦力\n" +
            "• 物体匀速运动时的读数是滑动摩擦力\n\n" +
            "[color=yellow]提示：[/color] 最大静摩擦力通常大于滑动摩擦力";
        base.stepHints[FrictionExperimentStep.RecordData] = 
            "[b]步骤 6：记录数据[/b]\n\n" +
            "• 将测量的摩擦力数据记录到数据板（DataBoard）上\n" +
            "• 记录不同压力下的摩擦力大小\n" +
            "• 计算摩擦系数 μ = f / N\n" +
            "• 整理实验数据，准备分析\n\n" +
            "[color=yellow]提示：[/color] 多次测量取平均值可以提高实验精度";
        base.stepHints[FrictionExperimentStep.AnalyzeResult] = 
            "[b]步骤 7：分析结果[/b]\n\n" +
            "• 查看数据记录板上的所有实验数据\n" +
            "• 分析摩擦力与压力的关系\n" +
            "• 计算摩擦系数的平均值\n" +
            "• 思考：为什么摩擦力与压力成正比？\n" +
            "• 总结影响摩擦力的因素\n\n" +
            "[color=yellow]提示：[/color] 摩擦力 f = μN，其中 μ 是摩擦系数，N 是压力";
        base.stepHints[FrictionExperimentStep.Completed] = 
            "[b]实验完成！[/b]\n\n" +
            "恭喜你完成了摩擦力实验！\n\n" +
            "[color=lightgreen]实验总结：[/color]\n" +
            "• 你已经成功完成了所有实验步骤\n" +
            "• 测量了不同条件下的摩擦力\n" +
            "• 理解了摩擦力与压力的关系\n" +
            "• 掌握了摩擦系数的计算方法\n\n" +
            "可以重新开始实验，尝试不同的材料和条件，探索更多有趣的物理现象！";
    }

    protected override string GetStepName(FrictionExperimentStep step) {
        switch (step) {
            case FrictionExperimentStep.Setup:
                return "准备阶段";
            case FrictionExperimentStep.PrepareEquipment:
                return "准备器材";
            case FrictionExperimentStep.PlaceObject:
                return "放置物体";
            case FrictionExperimentStep.AddWeight:
                return "添加砝码";
            case FrictionExperimentStep.MeasureForce:
                return "测量摩擦力";
            case FrictionExperimentStep.RecordData:
                return "记录数据";
            case FrictionExperimentStep.AnalyzeResult:
                return "分析结果";
            case FrictionExperimentStep.Completed:
                return "实验完成";
            default:
                return "未知步骤";
        }
    }

    protected override FrictionExperimentStep SetupStep => FrictionExperimentStep.Setup;
    
    protected override FrictionExperimentStep CompletedStep => FrictionExperimentStep.Completed;
}
