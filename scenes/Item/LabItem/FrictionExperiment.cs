using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
	[Export] public NodePath ExperimentUIPanelPath { get; set; }
	[Export] public NodePath ForceMeterPath { get; set; }
	[Export] public NodePath SurfacePlatformPath { get; set; }
	[Export] public Godot.Collections.Array<NodePath> BlockPaths { get; set; } = new();
	private Control experimentUIPanel;
	private ForceMeter forceMeter;
	private Node3D surfacePlatform;
	private List<ExperimentBlock> blocks = new();
	private ExperimentBlock currentBlock;
	private SurfaceType currentSurface = SurfaceType.Wood;

	private enum ExperimentStep {
		Introduction,
		SelectBlock,
		PlaceBlock,
		SelectSurface,
		PullBlock,
		RecordData,
		ChangeCondition,
		Analysis
	}

	private ExperimentStep currentStep = ExperimentStep.Introduction;
	private List<ExperimentData> experimentDataList = new();
	private Label stepLabel;
	private Label instructionLabel;
	private Button nextStepButton;
	private Button previousStepButton;
	private Control dataPanel;
	private VBoxContainer dataContainer;
	private Label forceValueLabel;
	private OptionButton surfaceSelector;
	private Button startPullButton;
	private Button recordDataButton;
	private Button analysisButton;
	private RichTextLabel analysisText;

	public override void _Ready() {
		base._Ready();
		this.ResolveComponents();
		this.InitializeUI();
		this.InitializeBlocks();
		this.UpdateStepUI();
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
		this.currentStep = ExperimentStep.Introduction;
		this.UpdateStepUI();
	}

	public override void ExitInteraction() {
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = false;
		}
		this.ResetExperiment();
		base.ExitInteraction();
	}

	private void ResolveComponents() {
		if (!string.IsNullOrEmpty(ExperimentUIPanelPath?.ToString())) {
			experimentUIPanel = GetNodeOrNull<Control>(ExperimentUIPanelPath);
			if (experimentUIPanel == null) {
				experimentUIPanel = GetTree().Root.FindChild("ExperimentUIPanel", true, false) as Control;
			}
		}
		if (!string.IsNullOrEmpty(ForceMeterPath?.ToString())) {
			var node = GetNodeOrNull<Node3D>(ForceMeterPath);
			if (node != null) {
				forceMeter = node as ForceMeter;
			}
		}
		if (!string.IsNullOrEmpty(SurfacePlatformPath?.ToString())) {
			surfacePlatform = GetNodeOrNull<Node3D>(SurfacePlatformPath);
		}
		foreach (var path in BlockPaths) {
			var blockNode = GetNodeOrNull<Node3D>(path);
			if (blockNode != null && blockNode is ExperimentBlock block) {
				blocks.Add(block);
				block.OnBlockSelected += OnBlockSelected;
			}
		}
	}

	private void InitializeBlocks() {
		if (blocks.Count == 0) {
			GD.PushWarning($"{Name}: 未找到实验物块，请在编辑器中添加ExperimentBlock节点。");
		}
	}

	private void InitializeUI() {
		if (experimentUIPanel == null) {
			CreateRuntimeUI();
		} else {
			ResolveUIElements();
		}

		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = false;
		}
	}

	private void CreateRuntimeUI() {
		experimentUIPanel = new PanelContainer();
		experimentUIPanel.Name = "ExperimentUIPanel";
		experimentUIPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		experimentUIPanel.MouseFilter = Control.MouseFilterEnum.Stop;
		var mainVBox = new VBoxContainer();
		mainVBox.Name = "MainVBox";
		experimentUIPanel.AddChild(mainVBox);
		var titleLabel = new Label();
		titleLabel.Text = "摩擦力实验 - 探究滑动摩擦力的影响因素";
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
		instructionLabel.AddThemeFontSizeOverride("font_size", 16);
		mainVBox.AddChild(instructionLabel);
		mainVBox.AddChild(new HSeparator());
		var surfaceHBox = new HBoxContainer();
		var surfaceLabel = new Label();
		surfaceLabel.Text = "接触面材质：";
		surfaceLabel.AddThemeColorOverride("font_color", Colors.White);
		surfaceHBox.AddChild(surfaceLabel);
		surfaceSelector = new OptionButton();
		surfaceSelector.AddItem("木板", (int)SurfaceType.Wood);
		surfaceSelector.AddItem("玻璃", (int)SurfaceType.Glass);
		surfaceSelector.AddItem("布面", (int)SurfaceType.Cloth);
		surfaceSelector.AddItem("金属", (int)SurfaceType.Metal);
		surfaceSelector.ItemSelected += OnSurfaceSelected;
		surfaceHBox.AddChild(surfaceSelector);
		mainVBox.AddChild(surfaceHBox);
		forceValueLabel = new Label();
		forceValueLabel.AddThemeColorOverride("font_color", Colors.Cyan);
		forceValueLabel.AddThemeFontSizeOverride("font_size", 24);
		mainVBox.AddChild(forceValueLabel);
		var buttonHBox = new HBoxContainer();
		buttonHBox.Alignment = BoxContainer.AlignmentMode.Center;
		startPullButton = new Button();
		startPullButton.Text = "开始拉动";
		startPullButton.Pressed += OnStartPull;
		buttonHBox.AddChild(startPullButton);
		recordDataButton = new Button();
		recordDataButton.Text = "记录数据";
		recordDataButton.Pressed += OnRecordData;
		buttonHBox.AddChild(recordDataButton);
		mainVBox.AddChild(buttonHBox);
		dataPanel = new PanelContainer();
		dataPanel.CustomMinimumSize = new Vector2(0, 200);
		var dataScroll = new ScrollContainer();
		dataContainer = new VBoxContainer();
		dataScroll.AddChild(dataContainer);
		dataPanel.AddChild(dataScroll);
		mainVBox.AddChild(dataPanel);
		analysisButton = new Button();
		analysisButton.Text = "分析实验结果";
		analysisButton.Pressed += OnAnalyzeData;
		mainVBox.AddChild(analysisButton);
		analysisText = new RichTextLabel();
		analysisText.CustomMinimumSize = new Vector2(0, 150);
		analysisText.BbcodeEnabled = true;
		mainVBox.AddChild(analysisText);
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

	private void ResolveUIElements() {
		stepLabel = experimentUIPanel.FindChild("StepLabel", true, false) as Label;
		instructionLabel = experimentUIPanel.FindChild("InstructionLabel", true, false) as Label;
		nextStepButton = experimentUIPanel.FindChild("NextStepButton", true, false) as Button;
		previousStepButton = experimentUIPanel.FindChild("PreviousStepButton", true, false) as Button;
		dataPanel = experimentUIPanel.FindChild("DataPanel", true, false) as Control;
		dataContainer = experimentUIPanel.FindChild("DataContainer", true, false) as VBoxContainer;
		forceValueLabel = experimentUIPanel.FindChild("ForceValueLabel", true, false) as Label;
		surfaceSelector = experimentUIPanel.FindChild("SurfaceSelector", true, false) as OptionButton;
		startPullButton = experimentUIPanel.FindChild("StartPullButton", true, false) as Button;
		recordDataButton = experimentUIPanel.FindChild("RecordDataButton", true, false) as Button;
		analysisButton = experimentUIPanel.FindChild("AnalysisButton", true, false) as Button;
		analysisText = experimentUIPanel.FindChild("AnalysisText", true, false) as RichTextLabel;
	}

	private void UpdateStepUI() {
		if (stepLabel == null || instructionLabel == null) return;

		switch (currentStep) {
			case ExperimentStep.Introduction:
				stepLabel.Text = "步骤 1: 实验介绍";
				instructionLabel.Text = @"欢迎来到摩擦力实验！

本实验将探究滑动摩擦力大小的影响因素。

实验目标：
1. 了解滑动摩擦力的概念
2. 探究接触面粗糙程度对摩擦力的影响
3. 探究压力大小对摩擦力的影响

实验原理：
滑动摩擦力 f = μN
其中：μ为动摩擦因数，N为正压力";
				break;
			case ExperimentStep.SelectBlock:
				stepLabel.Text = "步骤 2: 选择物块";
				instructionLabel.Text = "点击一个物块开始实验。不同的物块有不同的质量，质量越大，对接触面的压力越大。";
				break;
			case ExperimentStep.PlaceBlock:
				stepLabel.Text = "步骤 3: 放置物块";
				instructionLabel.Text = "拖动物块放置到实验平台上。";
				break;
			case ExperimentStep.SelectSurface:
				stepLabel.Text = "步骤 4: 选择接触面";
				instructionLabel.Text = "从下拉菜单中选择不同的接触面材质。不同材质的粗糙程度不同，摩擦因数也不同。";
				break;
			case ExperimentStep.PullBlock:
				stepLabel.Text = "步骤 5: 拉动物块";
				instructionLabel.Text = "点击\"开始拉动\"按钮，使用测力计匀速拉动物块。观察测力计显示的拉力大小（等于摩擦力）。";
				break;
			case ExperimentStep.RecordData:
				stepLabel.Text = "步骤 6: 记录数据";
				instructionLabel.Text = "点击\"记录数据\"按钮，将实验数据记录到表格中。";
				break;
			case ExperimentStep.ChangeCondition:
				stepLabel.Text = "步骤 7: 改变条件";
				instructionLabel.Text = "改变物块质量或接触面材质，重复实验至少3次，收集对比数据。";
				break;
			case ExperimentStep.Analysis:
				stepLabel.Text = "步骤 8: 分析结论";
				instructionLabel.Text = "点击\"分析实验结果\"查看实验数据分析和结论。";
				break;
		}
		UpdateButtonStates();
	}

	private void UpdateButtonStates() {
		if (previousStepButton != null) {
			previousStepButton.Disabled = currentStep == ExperimentStep.Introduction;
		}
		if (startPullButton != null) {
			startPullButton.Visible = currentStep == ExperimentStep.PullBlock;
		}
		if (recordDataButton != null) {
			recordDataButton.Visible = currentStep == ExperimentStep.RecordData;
		}
		if (surfaceSelector != null) {
			surfaceSelector.Disabled = currentStep != ExperimentStep.SelectSurface;
		}
		if (analysisButton != null) {
			analysisButton.Visible = currentStep == ExperimentStep.Analysis;
			analysisButton.Disabled = experimentDataList.Count < 3;
		}
	}

	private void OnNextStep() {
		if (currentStep < ExperimentStep.Analysis) {
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

	private void OnBlockSelected(ExperimentBlock block) {
		currentBlock = block;
		GD.Print($"选择了物块：{block.BlockName}，质量：{block.Mass}kg");
		if (currentStep == ExperimentStep.SelectBlock) {
			currentStep = ExperimentStep.PlaceBlock;
			UpdateStepUI();
		}
	}

	private void OnSurfaceSelected(long index) {
		currentSurface = (SurfaceType)index;
		GD.Print($"选择了接触面：{currentSurface}");
		if (currentStep == ExperimentStep.SelectSurface) {
			currentStep = ExperimentStep.PullBlock;
			UpdateStepUI();
		}
	}

	private void OnStartPull() {
		if (currentBlock == null) {
			GD.PushWarning("请先选择物块！");
			return;
		}
		float frictionCoefficient = GetFrictionCoefficient(currentSurface);
		float normalForce = currentBlock.Mass * 9.8f;
		float frictionForce = frictionCoefficient * normalForce;
		float randomError = (float)GD.RandRange(-0.5f, 0.5f);
		frictionForce += randomError;
		if (forceValueLabel != null) {
			forceValueLabel.Text = $"拉力（摩擦力）：{frictionForce:F2} N";
		}
		if (forceMeter != null) {
			forceMeter.SetForceValue(frictionForce);
		}
		currentStep = ExperimentStep.RecordData;
		UpdateStepUI();
	}

	private void OnRecordData() {
		if (currentBlock == null || forceValueLabel == null) return;
		string text = forceValueLabel.Text;
		int startIndex = text.IndexOf("：") + 1;
		int endIndex = text.IndexOf(" N");
		if (startIndex > 0 && endIndex > startIndex) {
			string valueStr = text.Substring(startIndex, endIndex - startIndex);
			if (float.TryParse(valueStr, out float force)) {
				var data = new ExperimentData {
					BlockName = currentBlock.BlockName,
					Mass = currentBlock.Mass,
					Surface = currentSurface,
					FrictionForce = force
				};
				experimentDataList.Add(data);
				UpdateDataDisplay();

				currentStep = ExperimentStep.ChangeCondition;
				UpdateStepUI();
			}
		}
	}

	private void UpdateDataDisplay() {
		if (dataContainer == null) return;
		foreach (var child in dataContainer.GetChildren()) {
			child.QueueFree();
		}
		var headerLabel = new Label();
		headerLabel.Text = "实验序号 | 物块 | 质量(kg) | 接触面 | 摩擦力(N)";
		headerLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		dataContainer.AddChild(headerLabel);
		for (int i = 0; i < experimentDataList.Count; i++) {
			var data = experimentDataList[i];
			var dataLabel = new Label();
			dataLabel.Text = $"{i + 1} | {data.BlockName} | {data.Mass:F1} | {data.Surface} | {data.FrictionForce:F2}";
			dataLabel.AddThemeColorOverride("font_color", Colors.White);
			dataContainer.AddChild(dataLabel);
		}
	}

	private void OnAnalyzeData() {
		if (experimentDataList.Count < 3 || analysisText == null) return;
		string analysis = "[center][b][color=yellow]实验数据分析[/color][/b][/center]\n\n";
		analysis += "[b]一、压力对摩擦力的影响：[/b]\n";
		var sameSurfaceData = new List<ExperimentData>();
		foreach (var data in experimentDataList) {
			if (sameSurfaceData.Count == 0 || data.Surface == sameSurfaceData[0].Surface) {
				sameSurfaceData.Add(data);
			}
		}
		if (sameSurfaceData.Count >= 2) {
			sameSurfaceData.Sort((a, b) => a.Mass.CompareTo(b.Mass));
			analysis += $"在相同接触面({sameSurfaceData[0].Surface})下：\n";
			analysis += $"- 质量 {sameSurfaceData[0].Mass}kg 时，摩擦力 {sameSurfaceData[0].FrictionForce:F2}N\n";
			analysis += $"- 质量 {sameSurfaceData[^1].Mass}kg 时，摩擦力 {sameSurfaceData[^1].FrictionForce:F2}N\n";
			analysis += "[color=green]结论：压力越大，摩擦力越大[/color]\n\n";
		}
		analysis += "[b]二、接触面粗糙程度的影响：[/b]\n";
		var sameMassData = new Dictionary<SurfaceType, float>();
		foreach (var data in experimentDataList) {
			if (!sameMassData.ContainsKey(data.Surface)) {
				sameMassData[data.Surface] = data.FrictionForce;
			}
		}
		if (sameMassData.Count >= 2) {
			foreach (var kvp in sameMassData) {
				analysis += $"- {kvp.Key}表面：摩擦力 {kvp.Value:F2}N\n";
			}
			analysis += "[color=green]结论：接触面越粗糙，动摩擦因数越大，摩擦力越大[/color]\n\n";
		}
		analysis += "[b]三、实验总结：[/b]\n";
		analysis += "[color=cyan]滑动摩擦力的大小与以下因素有关：\n";
		analysis += "1. 压力大小：压力越大，滑动摩擦力越大\n";
		analysis += "2. 接触面粗糙程度：接触面越粗糙，滑动摩擦力越大\n";
		analysis += "滑动摩擦力与接触面积大小无关[/color]";
		analysisText.Text = analysis;
	}

	private float GetFrictionCoefficient(SurfaceType surface) {
		return surface switch {
			SurfaceType.Wood => 0.3f,
			SurfaceType.Glass => 0.1f,
			SurfaceType.Cloth => 0.4f,
			SurfaceType.Metal => 0.15f,
			_ => 0.3f
		};
	}

	private void ResetExperiment() {
		currentStep = ExperimentStep.Introduction;
		currentBlock = null;
		experimentDataList.Clear();
		if (dataContainer != null) {
			foreach (var child in dataContainer.GetChildren()) {
				child.QueueFree();
			}
		}
		if (analysisText != null) {
			analysisText.Text = "";
		}
		if (forceValueLabel != null) {
			forceValueLabel.Text = "";
		}
	}
}

public enum SurfaceType {
	Wood,
	Glass,
	Cloth,
	Metal
}

public struct ExperimentData {
	public string BlockName;
	public float Mass;
	public SurfaceType Surface;
	public float FrictionForce;
}
