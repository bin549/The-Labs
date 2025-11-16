using Godot;
using System.Collections.Generic;

public partial class AluminumReactionExperiment : LabItem {
	[Export] public NodePath ExperimentUIPanelPath { get; set; }
	[Export] public NodePath AluminumPiecePath { get; set; }
	[Export] public NodePath NaOHSolutionPath { get; set; }
	[Export] public NodePath TestTubePath { get; set; }
	[Export] public NodePath BubbleEffectPath { get; set; }

	private Control experimentUIPanel;
	private Node3D aluminumPiece;
	private Node3D naohSolution;
	private Node3D testTube;
	private GpuParticles3D bubbleEffect;

	private enum ExperimentStep {
		Introduction,
		PrepareMaterials,
		AddAluminum,
		AddNaOH,
		ObserveReaction,
		CollectGas,
		TestGas,
		Conclusion
	}

	private ExperimentStep currentStep = ExperimentStep.Introduction;
	private bool isReacting = false;
	private float reactionProgress = 0.0f;
	private List<string> observations = new();
	private Label stepLabel;
	private Label instructionLabel;
	private RichTextLabel observationText;
	private Button nextStepButton;
	private Button previousStepButton;
	private Button startReactionButton;
	private Button testGasButton;
	private ProgressBar reactionProgressBar;
	private TextureRect equationImage;

	public override void _Ready() {
		base._Ready();
		ResolveComponents();
		InitializeUI();
		UpdateStepUI();
	}

	public override void _Process(double delta) {
		base._Process(delta);
		if (isReacting) {
			reactionProgress += (float)delta * 0.2f;
			if (reactionProgress >= 1.0f) {
				reactionProgress = 1.0f;
				isReacting = false;
				OnReactionComplete();
			}
			this.UpdateReactionVisuals();
		}
	}

	public override void _Input(InputEvent @event) {
		if (!base.isInteracting) return;
		if (@event.IsActionPressed("pause") || @event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			this.ExitInteraction();
		}
	}

	public override void EnterInteraction() {
		base.EnterInteraction();
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = true;
		}
		this.UpdateStepUI();
	}

	public override void ExitInteraction() {
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = false;
		}
		ResetExperiment();
		base.ExitInteraction();
	}

	private void ResolveComponents() {
		if (!string.IsNullOrEmpty(AluminumPiecePath?.ToString())) {
			aluminumPiece = GetNodeOrNull<Node3D>(AluminumPiecePath);
		}
		if (!string.IsNullOrEmpty(NaOHSolutionPath?.ToString())) {
			naohSolution = GetNodeOrNull<Node3D>(NaOHSolutionPath);
		}
		if (!string.IsNullOrEmpty(TestTubePath?.ToString())) {
			testTube = GetNodeOrNull<Node3D>(TestTubePath);
		}
		if (!string.IsNullOrEmpty(BubbleEffectPath?.ToString())) {
			bubbleEffect = GetNodeOrNull<GpuParticles3D>(BubbleEffectPath);
		}
	}

	private void InitializeUI() {
		if (experimentUIPanel == null) {
			CreateRuntimeUI();
		}
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = false;
		}
	}

	private void CreateRuntimeUI() {
		experimentUIPanel = new PanelContainer();
		experimentUIPanel.Name = "AluminumReactionUI";
		experimentUIPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		var mainVBox = new VBoxContainer();
		experimentUIPanel.AddChild(mainVBox);
		var titleLabel = new Label();
		titleLabel.Text = "铝与氢氧化钠溶液反应实验";
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.AddThemeColorOverride("font_color", Colors.White);
		titleLabel.AddThemeFontSizeOverride("font_size", 28);
		mainVBox.AddChild(titleLabel);
		mainVBox.AddChild(new HSeparator());
		stepLabel = new Label();
		stepLabel.HorizontalAlignment = HorizontalAlignment.Center;
		stepLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		stepLabel.AddThemeFontSizeOverride("font_size", 20);
		mainVBox.AddChild(stepLabel);
		instructionLabel = new Label();
		instructionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		instructionLabel.AddThemeColorOverride("font_color", Colors.White);
		mainVBox.AddChild(instructionLabel);
		mainVBox.AddChild(new HSeparator());
		var equationPanel = new PanelContainer();
		equationPanel.CustomMinimumSize = new Vector2(0, 80);
		var equationLabel = new RichTextLabel();
		equationLabel.BbcodeEnabled = true;
		equationLabel.Text = "[center][b]2Al + 2NaOH + 2H₂O → 2NaAlO₂ + 3H₂↑[/b][/center]";
		equationLabel.AddThemeFontSizeOverride("normal_font_size", 20);
		equationPanel.AddChild(equationLabel);
		mainVBox.AddChild(equationPanel);
		reactionProgressBar = new ProgressBar();
		reactionProgressBar.CustomMinimumSize = new Vector2(0, 30);
		reactionProgressBar.ShowPercentage = true;
		mainVBox.AddChild(reactionProgressBar);
		var buttonHBox = new HBoxContainer();
		buttonHBox.Alignment = BoxContainer.AlignmentMode.Center;
		startReactionButton = new Button();
		startReactionButton.Text = "开始反应";
		startReactionButton.Pressed += OnStartReaction;
		buttonHBox.AddChild(startReactionButton);
		testGasButton = new Button();
		testGasButton.Text = "检验气体";
		testGasButton.Pressed += OnTestGas;
		testGasButton.Disabled = true;
		buttonHBox.AddChild(testGasButton);
		mainVBox.AddChild(buttonHBox);
		var obsLabel = new Label();
		obsLabel.Text = "实验现象：";
		obsLabel.AddThemeColorOverride("font_color", Colors.Cyan);
		obsLabel.AddThemeFontSizeOverride("font_size", 18);
		mainVBox.AddChild(obsLabel);
		observationText = new RichTextLabel();
		observationText.CustomMinimumSize = new Vector2(0, 150);
		observationText.BbcodeEnabled = true;
		mainVBox.AddChild(observationText);
		var navHBox = new HBoxContainer();
		navHBox.Alignment = BoxContainer.AlignmentMode.Center;
		previousStepButton = new Button();
		previousStepButton.Text = "< 上一步";
		previousStepButton.Pressed += OnPreviousStep;
		navHBox.AddChild(previousStepButton);
		nextStepButton = new Button();
		nextStepButton.Text = "下一步 >";
		nextStepButton.Pressed += OnNextStep;
		navHBox.AddChild(nextStepButton);
		mainVBox.AddChild(navHBox);
		var player = GetTree().Root.FindChild("Player", true, false);
		if (player != null) {
			var canvasLayer = player.FindChild("CanvasLayer", false, false);
			if (canvasLayer != null) {
				canvasLayer.AddChild(experimentUIPanel);
			}
		}
	}

	private void UpdateStepUI() {
		if (stepLabel == null || instructionLabel == null) return;
		switch (currentStep) {
			case ExperimentStep.Introduction:
				stepLabel.Text = "步骤 1: 实验介绍";
				instructionLabel.Text = @"本实验将演示铝与氢氧化钠溶液的反应。

实验目的：
1. 观察金属铝与碱溶液的反应
2. 了解铝的两性特征
3. 学习氢气的产生和检验

注意事项：
⚠ 氢氧化钠溶液具有强腐蚀性
⚠ 反应产生的氢气易燃易爆";
				break;
			case ExperimentStep.PrepareMaterials:
				stepLabel.Text = "步骤 2: 准备材料";
				instructionLabel.Text = @"实验器材：
- 试管
- 氢氧化钠溶液（NaOH）
- 铝片（Al）
- 火柴（用于检验气体）";
				break;
			case ExperimentStep.AddAluminum:
				stepLabel.Text = "步骤 3: 加入铝片";
				instructionLabel.Text = "将打磨光亮的铝片放入试管中。";
				break;
			case ExperimentStep.AddNaOH:
				stepLabel.Text = "步骤 4: 加入氢氧化钠溶液";
				instructionLabel.Text = "向试管中加入氢氧化钠溶液，点击\"开始反应\"按钮。";
				break;
			case ExperimentStep.ObserveReaction:
				stepLabel.Text = "步骤 5: 观察反应现象";
				instructionLabel.Text = "仔细观察反应过程中的变化。";
				break;
			case ExperimentStep.CollectGas:
				stepLabel.Text = "步骤 6: 收集气体";
				instructionLabel.Text = "反应产生的气体可以收集起来。";
				break;
			case ExperimentStep.TestGas:
				stepLabel.Text = "步骤 7: 检验气体";
				instructionLabel.Text = "点击\"检验气体\"按钮，用燃着的火柴检验产生的气体。";
				break;
			case ExperimentStep.Conclusion:
				stepLabel.Text = "步骤 8: 实验结论";
				instructionLabel.Text = @"实验结论：

1. 铝具有两性，既能与酸反应，也能与强碱反应
2. 铝与氢氧化钠溶液反应生成偏铝酸钠和氢气
3. 反应方程式：2Al + 2NaOH + 2H₂O → 2NaAlO₂ + 3H₂↑
4. 生成的气体是氢气，可以燃烧，发出淡蓝色火焰";
				break;
		}

		UpdateButtonStates();
	}

	private void UpdateButtonStates() {
		if (previousStepButton != null) {
			previousStepButton.Disabled = currentStep == ExperimentStep.Introduction;
		}
		if (startReactionButton != null) {
			startReactionButton.Visible = currentStep == ExperimentStep.AddNaOH || currentStep == ExperimentStep.ObserveReaction;
			startReactionButton.Disabled = isReacting;
		}
		if (testGasButton != null) {
			testGasButton.Visible = currentStep == ExperimentStep.TestGas;
		}
	}

	private void OnStartReaction() {
		if (isReacting) return;
		isReacting = true;
		reactionProgress = 0.0f;
		observations.Clear();
		AddObservation("加入氢氧化钠溶液...");
		AddObservation("溶液开始变热（放热反应）");
		AddObservation("铝片表面产生气泡");
		AddObservation("气泡不断上升");
		if (bubbleEffect != null) {
			bubbleEffect.Emitting = true;
		}
		currentStep = ExperimentStep.ObserveReaction;
		UpdateStepUI();
	}

	private void UpdateReactionVisuals() {
		if (reactionProgressBar != null) {
			reactionProgressBar.Value = reactionProgress * 100;
		}
		if (aluminumPiece != null && aluminumPiece is MeshInstance3D mesh) {
			var material = mesh.GetActiveMaterial(0);
			if (material is StandardMaterial3D stdMat) {
				stdMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				stdMat.AlbedoColor = new Color(1, 1, 1, 1.0f - reactionProgress * 0.5f);
			}
		}
	}

	private void OnReactionComplete() {
		AddObservation("反应完成");
		AddObservation("铝片大部分溶解");
		AddObservation("继续产生气体");
		if (testGasButton != null) {
			testGasButton.Disabled = false;
		}
		currentStep = ExperimentStep.CollectGas;
		UpdateStepUI();
	}

	private void OnTestGas() {
		AddObservation("\n[color=yellow]== 检验气体 ==[/color]");
		AddObservation("将燃着的火柴靠近试管口...");
		AddObservation("[color=orange]气体被点燃，发出淡蓝色火焰[/color]");
		AddObservation("[color=cyan]\"噗\"的一声，产生爆鸣[/color]");
		AddObservation("\n[b]结论：生成的气体是氢气（H₂）[/b]");
		currentStep = ExperimentStep.Conclusion;
		UpdateStepUI();
	}

	private void AddObservation(string text) {
		observations.Add(text);
		UpdateObservationDisplay();
	}

	private void UpdateObservationDisplay() {
		if (observationText == null) return;
		string display = "";
		foreach (var obs in observations) {
			display += $"• {obs}\n";
		}
		observationText.Text = display;
	}

	private void OnNextStep() {
		if (currentStep < ExperimentStep.Conclusion) {
			currentStep++;
			UpdateStepUI();
		}
	}

	private void OnPreviousStep() {
		if (currentStep > ExperimentStep.Introduction) {
			currentStep--;
			UpdateStepUI();
		}
	}

	private void ResetExperiment() {
		currentStep = ExperimentStep.Introduction;
		isReacting = false;
		reactionProgress = 0.0f;
		observations.Clear();
		if (bubbleEffect != null) {
			bubbleEffect.Emitting = false;
		}
		if (observationText != null) {
			observationText.Text = "";
		}
		if (reactionProgressBar != null) {
			reactionProgressBar.Value = 0;
		}
	}
}
