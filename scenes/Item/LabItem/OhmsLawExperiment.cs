using Godot;
using System.Collections.Generic;

public partial class OhmsLawExperiment : LabItem {
	[Export] public NodePath ExperimentUIPanelPath { get; set; }
	[Export] public NodePath CircuitBoardPath { get; set; }
	[Export] public NodePath AmmeterPath { get; set; }
	[Export] public NodePath VoltmeterPath { get; set; }

	private Control experimentUIPanel;
	private Node3D circuitBoard;
	private Node3D ammeter;
	private Node3D voltmeter;

	private float currentVoltage = 0.0f;
	private float currentCurrent = 0.0f;
	private float resistance = 10.0f;
	private List<ExperimentData> dataList = new();

	private Label stepLabel;
	private Label instructionLabel;
	private HSlider voltageSlider;
	private Label voltageValueLabel;
	private Label currentValueLabel;
	private Label resistanceLabel;
	private OptionButton resistorSelector;
	private Button recordDataButton;
	private Button analyzeButton;
	private VBoxContainer dataTableContainer;
	private RichTextLabel analysisText;
	private TextureRect circuitDiagram;

	private enum ExperimentStep {
		Introduction,
		SetupCircuit,
		AdjustVoltage,
		RecordData,
		ChangeResistor,
		Analysis
	}

	private ExperimentStep currentStep = ExperimentStep.Introduction;

	public override void _Ready() {
		base._Ready();
		ResolveComponents();
		InitializeUI();
		UpdateStepUI();
		UpdateReadings();
	}

	public override void _Process(double delta) {
		base._Process(delta);
		UpdateCircuitVisuals();
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
		UpdateStepUI();
	}

	public override void ExitInteraction() {
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = false;
		}
		base.ExitInteraction();
	}

	private void ResolveComponents() {
		if (!string.IsNullOrEmpty(CircuitBoardPath?.ToString())) {
			circuitBoard = GetNodeOrNull<Node3D>(CircuitBoardPath);
		}

		if (!string.IsNullOrEmpty(AmmeterPath?.ToString())) {
			ammeter = GetNodeOrNull<Node3D>(AmmeterPath);
		}

		if (!string.IsNullOrEmpty(VoltmeterPath?.ToString())) {
			voltmeter = GetNodeOrNull<Node3D>(VoltmeterPath);
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
		experimentUIPanel.Name = "OhmsLawUI";
		experimentUIPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);

		var mainVBox = new VBoxContainer();
		experimentUIPanel.AddChild(mainVBox);

		var titleLabel = new Label();
		titleLabel.Text = "欧姆定律实验 - 探究电流与电压、电阻的关系";
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

		var formulaLabel = new RichTextLabel();
		formulaLabel.CustomMinimumSize = new Vector2(0, 60);
		formulaLabel.BbcodeEnabled = true;
		formulaLabel.Text = "[center][b][color=cyan]欧姆定律：I = U / R[/color][/b]\nI - 电流(A)    U - 电压(V)    R - 电阻(Ω)[/center]";
		formulaLabel.AddThemeFontSizeOverride("normal_font_size", 18);
		mainVBox.AddChild(formulaLabel);

		var resistorHBox = new HBoxContainer();
		var resistorLabel = new Label();
		resistorLabel.Text = "选择电阻：";
		resistorLabel.AddThemeColorOverride("font_color", Colors.White);
		resistorHBox.AddChild(resistorLabel);

		resistorSelector = new OptionButton();
		resistorSelector.AddItem("5Ω 电阻", 0);
		resistorSelector.AddItem("10Ω 电阻", 1);
		resistorSelector.AddItem("20Ω 电阻", 2);
		resistorSelector.AddItem("50Ω 电阻", 3);
		resistorSelector.Selected = 1;
		resistorSelector.ItemSelected += OnResistorChanged;
		resistorHBox.AddChild(resistorSelector);

		resistanceLabel = new Label();
		resistanceLabel.Text = $"当前电阻：{resistance}Ω";
		resistanceLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		resistorHBox.AddChild(resistanceLabel);
		mainVBox.AddChild(resistorHBox);
		var voltageHBox = new HBoxContainer();
		var voltLabel = new Label();
		voltLabel.Text = "调节电压：";
		voltLabel.AddThemeColorOverride("font_color", Colors.White);
		voltageHBox.AddChild(voltLabel);
		voltageSlider = new HSlider();
		voltageSlider.MinValue = 0;
		voltageSlider.MaxValue = 12;
		voltageSlider.Step = 0.1;
		voltageSlider.Value = 0;
		voltageSlider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		voltageSlider.ValueChanged += OnVoltageChanged;
		voltageHBox.AddChild(voltageSlider);
		voltageValueLabel = new Label();
		voltageValueLabel.Text = "0.0 V";
		voltageValueLabel.AddThemeColorOverride("font_color", Colors.Cyan);
		voltageValueLabel.AddThemeFontSizeOverride("font_size", 20);
		voltageHBox.AddChild(voltageValueLabel);
		mainVBox.AddChild(voltageHBox);
		var currentHBox = new HBoxContainer();
		var currentLabel = new Label();
		currentLabel.Text = "电流读数：";
		currentLabel.AddThemeColorOverride("font_color", Colors.White);
		currentHBox.AddChild(currentLabel);

		currentValueLabel = new Label();
		currentValueLabel.Text = "0.00 A";
		currentValueLabel.AddThemeColorOverride("font_color", Colors.Orange);
		currentValueLabel.AddThemeFontSizeOverride("font_size", 24);
		currentHBox.AddChild(currentValueLabel);

		mainVBox.AddChild(currentHBox);

		recordDataButton = new Button();
		recordDataButton.Text = "记录当前数据";
		recordDataButton.Pressed += OnRecordData;
		mainVBox.AddChild(recordDataButton);

		mainVBox.AddChild(new HSeparator());

		var tableLabel = new Label();
		tableLabel.Text = "实验数据记录：";
		tableLabel.AddThemeColorOverride("font_color", Colors.Cyan);
		tableLabel.AddThemeFontSizeOverride("font_size", 18);
		mainVBox.AddChild(tableLabel);

		var scrollContainer = new ScrollContainer();
		scrollContainer.CustomMinimumSize = new Vector2(0, 120);
		dataTableContainer = new VBoxContainer();
		scrollContainer.AddChild(dataTableContainer);
		mainVBox.AddChild(scrollContainer);

		analyzeButton = new Button();
		analyzeButton.Text = "分析实验数据";
		analyzeButton.Pressed += OnAnalyzeData;
		analyzeButton.Disabled = true;
		mainVBox.AddChild(analyzeButton);

		analysisText = new RichTextLabel();
		analysisText.CustomMinimumSize = new Vector2(0, 100);
		analysisText.BbcodeEnabled = true;
		mainVBox.AddChild(analysisText);

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
				instructionLabel.Text = @"欢迎来到欧姆定律实验！

实验目的：
1. 探究电流与电压的关系
2. 探究电流与电阻的关系
3. 验证欧姆定律 I = U / R";
				break;

			case ExperimentStep.SetupCircuit:
				stepLabel.Text = "步骤 2: 连接电路";
				instructionLabel.Text = "按照电路图连接：电源 - 开关 - 电流表 - 定值电阻 - 电压表";
				break;

			case ExperimentStep.AdjustVoltage:
				stepLabel.Text = "步骤 3: 调节电压";
				instructionLabel.Text = "拖动滑块调节电源电压，观察电流表读数的变化。";
				break;

			case ExperimentStep.RecordData:
				stepLabel.Text = "步骤 4: 记录数据";
				instructionLabel.Text = "点击\"记录当前数据\"按钮，记录不同电压下的电流值。建议记录至少5组数据。";
				break;

			case ExperimentStep.ChangeResistor:
				stepLabel.Text = "步骤 5: 更换电阻";
				instructionLabel.Text = "选择不同阻值的电阻，重复实验。记录相同电压下，不同电阻的电流值。";
				break;

			case ExperimentStep.Analysis:
				stepLabel.Text = "步骤 6: 数据分析";
				instructionLabel.Text = "点击\"分析实验数据\"，查看实验结论。";
				break;
		}
	}

	private void OnVoltageChanged(double value) {
		currentVoltage = (float)value;
		UpdateReadings();
	}

	private void OnResistorChanged(long index) {
		resistance = index switch {
			0 => 5.0f,
			1 => 10.0f,
			2 => 20.0f,
			3 => 50.0f,
			_ => 10.0f
		};

		if (resistanceLabel != null) {
			resistanceLabel.Text = $"当前电阻：{resistance}Ω";
		}

		UpdateReadings();
		currentStep = ExperimentStep.ChangeResistor;
		UpdateStepUI();
	}

	private void UpdateReadings() {
		currentCurrent = resistance > 0 ? currentVoltage / resistance : 0;

		float error = (float)GD.RandRange(-0.01f, 0.01f);
		currentCurrent += error;

		if (voltageValueLabel != null) {
			voltageValueLabel.Text = $"{currentVoltage:F1} V";
		}

		if (currentValueLabel != null) {
			currentValueLabel.Text = $"{currentCurrent:F3} A";

			if (currentCurrent > 0.5f) {
				currentValueLabel.AddThemeColorOverride("font_color", Colors.Red);
			} else if (currentCurrent > 0.2f) {
				currentValueLabel.AddThemeColorOverride("font_color", Colors.Orange);
			} else {
				currentValueLabel.AddThemeColorOverride("font_color", Colors.Green);
			}
		}
	}

	private void UpdateCircuitVisuals() {
	}

	private void OnRecordData() {
		if (currentVoltage < 0.1f) {
			GD.Print("电压太低，请调高电压后再记录！");
			return;
		}

		var data = new ExperimentData {
			Voltage = currentVoltage,
			Current = currentCurrent,
			Resistance = resistance
		};

		dataList.Add(data);
		UpdateDataTable();

		if (dataList.Count >= 3) {
			if (analyzeButton != null) {
				analyzeButton.Disabled = false;
			}
		}

		currentStep = ExperimentStep.RecordData;
		UpdateStepUI();
	}

	private void UpdateDataTable() {
		if (dataTableContainer == null) return;

		foreach (var child in dataTableContainer.GetChildren()) {
			child.QueueFree();
		}

		var headerLabel = new Label();
		headerLabel.Text = "序号 | 电压(V) | 电流(A) | 电阻(Ω) | U/I";
		headerLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		dataTableContainer.AddChild(headerLabel);

		for (int i = 0; i < dataList.Count; i++) {
			var data = dataList[i];
			float ratio = data.Current > 0 ? data.Voltage / data.Current : 0;

			var dataLabel = new Label();
			dataLabel.Text = $"{i + 1} | {data.Voltage:F1} | {data.Current:F3} | {data.Resistance:F0} | {ratio:F2}";
			dataLabel.AddThemeColorOverride("font_color", Colors.White);
			dataTableContainer.AddChild(dataLabel);
		}
	}

	private void OnAnalyzeData() {
		if (dataList.Count < 3 || analysisText == null) return;

		string analysis = "[b][color=yellow]实验数据分析[/color][/b]\n\n";

		var sameResistorData = new List<ExperimentData>();
		float targetResistance = dataList[0].Resistance;

		foreach (var data in dataList) {
			if (Mathf.Abs(data.Resistance - targetResistance) < 0.1f) {
				sameResistorData.Add(data);
			}
		}

		if (sameResistorData.Count >= 2) {
			analysis += $"[b]一、电流与电压的关系（电阻 {targetResistance}Ω）[/b]\n";
			foreach (var data in sameResistorData) {
				float ratio = data.Current > 0 ? data.Voltage / data.Current : 0;
				analysis += $"电压{data.Voltage:F1}V → 电流{data.Current:F3}A → U/I = {ratio:F2}Ω\n";
			}
			analysis += "[color=green]结论：在电阻一定时，电流与电压成正比[/color]\n\n";
		}

		analysis += "[b]二、电流与电阻的关系[/b]\n";
		var resistorGroups = new Dictionary<float, List<ExperimentData>>();

		foreach (var data in dataList) {
			if (!resistorGroups.ContainsKey(data.Resistance)) {
				resistorGroups[data.Resistance] = new List<ExperimentData>();
			}
			resistorGroups[data.Resistance].Add(data);
		}

		if (resistorGroups.Count >= 2) {
			foreach (var kvp in resistorGroups) {
				float avgCurrent = 0;
				foreach (var data in kvp.Value) {
					avgCurrent += data.Current;
				}
				avgCurrent /= kvp.Value.Count;
				analysis += $"电阻{kvp.Key}Ω → 平均电流{avgCurrent:F3}A\n";
			}
			analysis += "[color=green]结论：在电压一定时，电流与电阻成反比[/color]\n\n";
		}

		analysis += "[b]三、实验总结[/b]\n";
		analysis += "[color=cyan]欧姆定律：I = U / R\n";
		analysis += "1. 导体中的电流与导体两端的电压成正比\n";
		analysis += "2. 导体中的电流与导体的电阻成反比\n";
		analysis += "3. 从数据中可以看出，U/I的值近似等于电阻R[/color]";

		analysisText.Text = analysis;
		currentStep = ExperimentStep.Analysis;
		UpdateStepUI();
	}

	private struct ExperimentData {
		public float Voltage;
		public float Current;
		public float Resistance;
	}
}
