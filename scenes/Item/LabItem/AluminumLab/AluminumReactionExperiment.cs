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

public partial class AluminumReactionExperiment : LabItem {
    [Export] public Godot.Collections.Array<NodePath> PlacableItemPaths { get; set; } = new();
    
    [Export] public AluminumReactionExperimentStep CurrentStep { get; set; } = AluminumReactionExperimentStep.Setup;
    
    [Export] public Label3D HintLabel { get; set; }
    
    [Export] public Button NextStepButton { get; set; }
    
    [Export] public Button PlayVoiceButton { get; set; }
    
    [Export] public AudioStreamPlayer VoicePlayer { get; set; }
    
    [Export] public Godot.Collections.Array<AudioStream> StepVoiceResources { get; set; } = new();
    
    private Dictionary<AluminumReactionExperimentItem, Node3D> experimentItems = new Dictionary<AluminumReactionExperimentItem, Node3D>();
    
    private Dictionary<AluminumReactionExperimentStep, bool> stepCompletionStatus = new Dictionary<AluminumReactionExperimentStep, bool>();
    
    private Dictionary<AluminumReactionExperimentStep, string> stepHints = new Dictionary<AluminumReactionExperimentStep, string>();
    
    private Dictionary<AluminumReactionExperimentStep, AudioStream> stepVoices = new Dictionary<AluminumReactionExperimentStep, AudioStream>();

    public override void _Ready() {
        base._Ready();
        InitializeStepHints();
        InitializeExperimentItems();
        InitializeStepStatus();
        InitializeButton();
        InitializeVoiceButton();
        InitializeVoiceResources();
        UpdateHintLabel();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) {
            return;
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
        base.ExitInteraction();
    }

    private void InitializeExperimentItems() {
        foreach (var path in PlacableItemPaths) {
            if (path != null && !path.IsEmpty) {
                var item = GetNodeOrNull<Node3D>(path);
                if (item != null) {
                }
            }
        }
    }

    private void InitializeStepHints() {
        stepHints[AluminumReactionExperimentStep.Setup] = 
            "[b]步骤 1：准备阶段[/b]\n\n" +
            "• 将实验台放置在平稳的表面上\n" +
            "• 从实验物品中选择烧杯（Beaker）\n" +
            "• 将烧杯放置在实验台上\n" +
            "• 确保实验环境安全，准备好防护用品\n\n" +
            "[color=yellow]提示：[/color] 可以使用鼠标拖拽来移动和放置物品";
        
        stepHints[AluminumReactionExperimentStep.PrepareReagents] = 
            "[b]步骤 2：准备试剂[/b]\n\n" +
            "• 准备铝片（AluminumStrip），确保表面清洁\n" +
            "• 准备氢氧化钠溶液（SodiumHydroxideSolution）\n" +
            "• 检查试剂的浓度和纯度\n" +
            "• 将氢氧化钠溶液倒入烧杯中（适量即可）\n\n" +
            "[color=yellow]提示：[/color] 氢氧化钠具有腐蚀性，操作时需小心";
        
        stepHints[AluminumReactionExperimentStep.AddReagents] = 
            "[b]步骤 3：添加试剂[/b]\n\n" +
            "• 将准备好的铝片轻轻放入装有氢氧化钠溶液的烧杯中\n" +
            "• 确保铝片完全浸入溶液中\n" +
            "• 观察铝片与溶液接触后的变化\n" +
            "• 注意观察反应是否立即开始\n\n" +
            "[color=yellow]提示：[/color] 铝片与氢氧化钠反应会产生氢气";
        
        stepHints[AluminumReactionExperimentStep.ObserveReaction] = 
            "[b]步骤 4：观察反应[/b]\n\n" +
            "• 仔细观察反应现象：铝片表面产生气泡\n" +
            "• 观察气泡产生的速度和数量\n" +
            "• 注意溶液温度的变化（可使用温度计）\n" +
            "• 观察铝片是否逐渐溶解\n\n" +
            "[color=yellow]提示：[/color] 反应方程式：2Al + 2NaOH + 6H₂O → 2NaAlO₂ + 3H₂↑";
        
        stepHints[AluminumReactionExperimentStep.CollectGas] = 
            "[b]步骤 5：收集气体[/b]\n\n" +
            "• 使用导管（GasTube）连接烧杯和试管（TestTube）\n" +
            "• 将试管倒置在水槽（WaterTank）中收集氢气\n" +
            "• 观察试管中气体的收集情况\n" +
            "• 确认收集到的是氢气（可以后续进行验证实验）\n\n" +
            "[color=yellow]提示：[/color] 氢气比空气轻，适合用排水法收集";
        
        stepHints[AluminumReactionExperimentStep.RecordData] = 
            "[b]步骤 6：记录数据[/b]\n\n" +
            "• 记录反应开始的时间\n" +
            "• 记录反应过程中的温度变化\n" +
            "• 记录产生的气体体积\n" +
            "• 将测量数据记录到数据记录板（DataBoard）上\n" +
            "• 记录反应现象和观察结果\n\n" +
            "[color=yellow]提示：[/color] 详细记录有助于后续分析反应规律";
        
        stepHints[AluminumReactionExperimentStep.AnalyzeResult] = 
            "[b]步骤 7：分析结果[/b]\n\n" +
            "• 查看数据记录板上的所有实验数据\n" +
            "• 分析反应速率与温度的关系\n" +
            "• 理解铝与氢氧化钠反应的化学原理\n" +
            "• 思考：为什么铝能与强碱反应？\n" +
            "• 总结实验现象和结论\n\n" +
            "[color=yellow]提示：[/color] 铝是两性金属，既能与酸反应，也能与强碱反应";
        
        stepHints[AluminumReactionExperimentStep.Completed] = 
            "[b]实验完成！[/b]\n\n" +
            "恭喜你完成了铝和氢氧化钠反应实验！\n\n" +
            "[color=lightgreen]实验总结：[/color]\n" +
            "• 你已经成功完成了所有实验步骤\n" +
            "• 观察了铝与氢氧化钠的化学反应现象\n" +
            "• 理解了铝的两性金属特性\n" +
            "• 掌握了收集气体的方法\n\n" +
            "可以重新开始实验，尝试不同的条件，探索更多有趣的化学现象！";
    }

    private void InitializeStepStatus() {
        foreach (AluminumReactionExperimentStep step in System.Enum.GetValues(typeof(AluminumReactionExperimentStep))) {
            stepCompletionStatus[step] = false;
        }
    }

    private void InitializeButton() {
        if (NextStepButton != null) {
            NextStepButton.Pressed += OnNextStepButtonPressed;
            UpdateButtonState();
        }
    }

    private void InitializeVoiceButton() {
        if (PlayVoiceButton != null) {
            PlayVoiceButton.Pressed += OnPlayVoiceButtonPressed;
        }
    }

    private void InitializeVoiceResources() {
        var steps = System.Enum.GetValues(typeof(AluminumReactionExperimentStep));
        for (int i = 0; i < steps.Length && i < StepVoiceResources.Count; i++) {
            var step = (AluminumReactionExperimentStep)steps.GetValue(i);
            if (StepVoiceResources[i] != null) {
                stepVoices[step] = StepVoiceResources[i];
            }
        }
    }

    private void OnPlayVoiceButtonPressed() {
        PlayCurrentStepVoice();
    }

    private void PlayCurrentStepVoice() {
        if (VoicePlayer == null) {
            GD.PrintErr("VoicePlayer 未设置，无法播放语音");
            return;
        }

        if (stepVoices.ContainsKey(CurrentStep) && stepVoices[CurrentStep] != null) {
            VoicePlayer.Stream = stepVoices[CurrentStep];
            VoicePlayer.Play();
            GD.Print($"播放步骤语音: {GetStepName(CurrentStep)}");
        } else {
            GD.Print($"步骤 {GetStepName(CurrentStep)} 没有对应的语音资源");
        }
    }

    private void OnNextStepButtonPressed() {
        if (CanGoToNextStep()) {
            GoToNextStep();
            UpdateButtonState();
        }
    }

    private void UpdateButtonState() {
        if (NextStepButton != null) {
            NextStepButton.Disabled = !CanGoToNextStep();
            if (CurrentStep >= AluminumReactionExperimentStep.Completed) {
                NextStepButton.Text = "实验完成";
            } else {
                NextStepButton.Text = "下一步";
            }
        }
    }

    public void SetCurrentStep(AluminumReactionExperimentStep step) {
        CurrentStep = step;
    }

    public void CompleteCurrentStep() {
        if (stepCompletionStatus.ContainsKey(CurrentStep)) {
            stepCompletionStatus[CurrentStep] = true;
        }
        GoToNextStep();
    }

    public bool GoToNextStep() {
        if (CurrentStep >= AluminumReactionExperimentStep.Completed) {
            return false;
        }
        var previousStep = CurrentStep;
        CurrentStep++;
        OnStepChanged(previousStep, CurrentStep);
        return true;
    }

    public bool GoToPreviousStep() {
        if (CurrentStep <= AluminumReactionExperimentStep.Setup) {
            return false;
        }

        var previousStep = CurrentStep;
        CurrentStep--;
        
        OnStepChanged(previousStep, CurrentStep);
        
        return true;
    }

    public bool CanGoToNextStep() {
        return CurrentStep < AluminumReactionExperimentStep.Completed;
    }

    public bool CanGoToPreviousStep() {
        return CurrentStep > AluminumReactionExperimentStep.Setup;
    }

    private void OnStepChanged(AluminumReactionExperimentStep previousStep, AluminumReactionExperimentStep newStep) {
        GD.Print($"步骤变更: {GetStepName(previousStep)} -> {GetStepName(newStep)}");
        StopCurrentVoice();
        UpdateHintLabel();
        UpdateButtonState();
    }

    private void StopCurrentVoice() {
        if (VoicePlayer != null && VoicePlayer.Playing) {
            VoicePlayer.Stop();
        }
    }

    private void UpdateHintLabel() {
        if (HintLabel != null) {
            HintLabel.Text = GetCurrentStepHint();
        }
    }

    public bool IsStepCompleted(AluminumReactionExperimentStep step) {
        return stepCompletionStatus.ContainsKey(step) && stepCompletionStatus[step];
    }

    public Node3D GetExperimentItem(AluminumReactionExperimentItem itemType) {
        return experimentItems.ContainsKey(itemType) ? experimentItems[itemType] : null;
    }

    public void RegisterExperimentItem(AluminumReactionExperimentItem itemType, Node3D itemNode) {
        experimentItems[itemType] = itemNode;
    }

    public string GetCurrentStepHint() {
        return GetStepHint(CurrentStep);
    }

    public string GetStepHint(AluminumReactionExperimentStep step) {
        return stepHints.ContainsKey(step) ? stepHints[step] : "";
    }

    public Dictionary<AluminumReactionExperimentStep, string> GetAllStepHints() {
        return new Dictionary<AluminumReactionExperimentStep, string>(stepHints);
    }

    public string GetStepName(AluminumReactionExperimentStep step) {
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

    public string GetCurrentStepName() {
        return GetStepName(CurrentStep);
    }
}
