using Godot;

public enum OhmsLawExperimentStep {
    Setup,
    ConnectCircuit,
    AdjustVoltage,
    MeasureCurrent,
    RecordData,
    ChangeResistor,
    AnalyzeResult,
    Completed
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
    [Export] protected override OhmsLawExperimentStep currentStep { get; set; } = OhmsLawExperimentStep.Setup;
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
        base.stepHints[OhmsLawExperimentStep.Setup] = 
            "[b]步骤 1：准备阶段[/b]\n\n" +
            "• 将实验台放置在平稳的表面上\n" +
            "• 准备欧姆定律实验所需的材料\n" +
            "• 检查所有电子器材是否正常工作\n" +
            "• 确保电源关闭，电路断开\n\n" +
            "[color=yellow]提示：[/color] 欧姆定律：U = IR，电压等于电流乘以电阻";
        
        base.stepHints[OhmsLawExperimentStep.ConnectCircuit] = 
            "[b]步骤 2：连接电路[/b]\n\n" +
            "• 将电阻器（Resistor）插入面包板（Breadboard）\n" +
            "• 使用导线（Wire）连接电源（PowerSupply）\n" +
            "• 串联连接电流表（Ammeter）测量电流\n" +
            "• 并联连接电压表（Voltmeter）测量电压\n\n" +
            "[color=yellow]提示：[/color] 电流表串联，电压表并联";
        
        base.stepHints[OhmsLawExperimentStep.AdjustVoltage] = 
            "[b]步骤 3：调整电压[/b]\n\n" +
            "• 确认电路连接正确后，打开电源\n" +
            "• 调整电源电压，从低电压开始\n" +
            "• 逐渐增加电压值\n" +
            "• 观察电流表和电压表的读数变化\n\n" +
            "[color=yellow]提示：[/color] 建议从 1V 开始，逐步增加到 5V";
        
        base.stepHints[OhmsLawExperimentStep.MeasureCurrent] = 
            "[b]步骤 4：测量电流[/b]\n\n" +
            "• 读取电压表显示的电压值 U\n" +
            "• 读取电流表显示的电流值 I\n" +
            "• 记录每组电压和电流的对应关系\n" +
            "• 调整电压，进行多次测量\n\n" +
            "[color=yellow]提示：[/color] 在同一电阻下，电压越大，电流越大";
        
        base.stepHints[OhmsLawExperimentStep.RecordData] = 
            "[b]步骤 5：记录数据[/b]\n\n" +
            "• 将测量的电压和电流数据记录到数据板上\n" +
            "• 计算电阻值 R = U / I\n" +
            "• 记录至少 3-5 组数据\n" +
            "• 检查数据的一致性\n\n" +
            "[color=yellow]提示：[/color] 多次测量可以验证欧姆定律的准确性";
        
        base.stepHints[OhmsLawExperimentStep.ChangeResistor] = 
            "[b]步骤 6：更换电阻[/b]\n\n" +
            "• 关闭电源，断开电路\n" +
            "• 更换不同阻值的电阻器\n" +
            "• 重新连接电路，重复实验\n" +
            "• 观察不同电阻对电流的影响\n\n" +
            "[color=yellow]提示：[/color] 电阻越大，相同电压下的电流越小";
        
        base.stepHints[OhmsLawExperimentStep.AnalyzeResult] = 
            "[b]步骤 7：分析结果[/b]\n\n" +
            "• 查看数据记录板上的所有实验数据\n" +
            "• 验证 U = IR 是否成立\n" +
            "• 绘制 U-I 图像，观察线性关系\n" +
            "• 思考：为什么电阻不变时，电压与电流成正比？\n" +
            "• 总结欧姆定律的应用\n\n" +
            "[color=yellow]提示：[/color] U-I 图像的斜率就是电阻值 R";
        
        base.stepHints[OhmsLawExperimentStep.Completed] = 
            "[b]实验完成！[/b]\n\n" +
            "恭喜你完成了欧姆定律实验！\n\n" +
            "[color=lightgreen]实验总结：[/color]\n" +
            "• 你已经成功完成了所有实验步骤\n" +
            "• 验证了欧姆定律 U = IR\n" +
            "• 理解了电压、电流和电阻的关系\n" +
            "• 掌握了电路的基本连接方法\n\n" +
            "可以重新开始实验，尝试不同的电阻和电压，探索更多有趣的电学现象！";
    }

    protected override string GetStepName(OhmsLawExperimentStep step) {
        switch (step) {
            case OhmsLawExperimentStep.Setup:
                return "准备阶段";
            case OhmsLawExperimentStep.ConnectCircuit:
                return "连接电路";
            case OhmsLawExperimentStep.AdjustVoltage:
                return "调整电压";
            case OhmsLawExperimentStep.MeasureCurrent:
                return "测量电流";
            case OhmsLawExperimentStep.RecordData:
                return "记录数据";
            case OhmsLawExperimentStep.ChangeResistor:
                return "更换电阻";
            case OhmsLawExperimentStep.AnalyzeResult:
                return "分析结果";
            case OhmsLawExperimentStep.Completed:
                return "实验完成";
            default:
                return "未知步骤";
        }
    }

    protected override OhmsLawExperimentStep SetupStep => OhmsLawExperimentStep.Setup;
    
    protected override OhmsLawExperimentStep CompletedStep => OhmsLawExperimentStep.Completed;
}
