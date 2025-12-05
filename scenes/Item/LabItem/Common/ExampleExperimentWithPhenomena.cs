using Godot;

public partial class ExampleExperimentWithPhenomena : LabItem {
	[ExportGroup("实验物品")]
	[Export] public Godot.Collections.Array<NodePath> PlacableItemPaths { get; set; } = new();
	[ExportGroup("实验现象")]
	[Export] public Godot.Collections.Array<ExperimentPhenomenon> Phenomena { get; set; } = new();
	[ExportGroup("UI设置")]
	[Export] public NodePath ExperimentUIPanelPath { get; set; }
	[Export] public bool ShowInstructions { get; set; } = true;
	private ExperimentPhenomenonManager phenomenonManager;
	private Godot.Collections.Array<PlacableItem> placableItems = new();
	private Control experimentUIPanel;
	private RichTextLabel instructionLabel;
	private RichTextLabel logLabel;
	private Button resetButton;
	private Godot.Collections.Array<string> experimentLog = new();
	
	public override void _Ready() {
		base._Ready();
		this.InitializePhenomenonManager();
		this.CollectPlacableItems();
		this.InitializeUI();
		if (Phenomena.Count == 0) {
			this.CreateExamplePhenomena();
		}
	}
	
	public override void EnterInteraction() {
		base.EnterInteraction();
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = true;
		}
		LogMessage("开始实验，请拖拽物品进行实验...");
	}
	
	public override void ExitInteraction() {
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = false;
		}
		base.ExitInteraction();
	}
	
	private void InitializePhenomenonManager() {
		phenomenonManager = new ExperimentPhenomenonManager();
		phenomenonManager.Name = "PhenomenonManager";
		phenomenonManager.Phenomena = Phenomena;
		phenomenonManager.EffectsParent = this;
		AddChild(phenomenonManager);
		phenomenonManager.OnPhenomenonTriggered += OnPhenomenonTriggered;
		GD.Print($"[ExampleExperiment] 现象管理器已初始化，共 {Phenomena.Count} 个现象");
	}
	
	private void CollectPlacableItems() {
		placableItems.Clear();
		foreach (var path in PlacableItemPaths) {
			if (string.IsNullOrEmpty(path?.ToString())) continue;
			var item = GetNodeOrNull<PlacableItem>(path);
			if (item != null) {
				placableItems.Add(item);
				this.RegisterPlacableItem(item);
			}
		}
		var children = GetChildren();
		foreach (var child in children) {
			if (child is PlacableItem item && !placableItems.Contains(item)) {
				placableItems.Add(item);
				this.RegisterPlacableItem(item);
			}
		}
		GD.Print($"[ExampleExperiment] 收集到 {placableItems.Count} 个可放置物品");
	}
	
	private void RegisterPlacableItem(PlacableItem item) {
		phenomenonManager.RegisterItem(item);
		item.OnItemDragStarted += OnItemDragStarted;
		item.OnItemDragEnded += OnItemDragEnded;
		item.OnItemPlaced += OnItemPlaced;
		item.OnItemOverlapStarted += OnItemOverlapStarted;
		item.OnItemOverlapEnded += OnItemOverlapEnded;
		GD.Print($"[ExampleExperiment] 注册物品：{item.ItemName} (类型: {item.ItemType})");
	}
	
	private void InitializeUI() {
		if (!string.IsNullOrEmpty(ExperimentUIPanelPath?.ToString())) {
			experimentUIPanel = GetNodeOrNull<Control>(ExperimentUIPanelPath);
		}
		if (experimentUIPanel == null) {
			CreateRuntimeUI();
		}
		if (experimentUIPanel != null) {
			experimentUIPanel.Visible = false;
		}
	}
	
	private void CreateRuntimeUI() {
		experimentUIPanel = new PanelContainer();
		experimentUIPanel.Name = "ExperimentUI";
		experimentUIPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		experimentUIPanel.SetAnchor(Side.Left, 0.65f);
		experimentUIPanel.SetAnchor(Side.Top, 0.05f);
		experimentUIPanel.SetAnchor(Side.Right, 0.95f);
		experimentUIPanel.SetAnchor(Side.Bottom, 0.95f);
		var mainVBox = new VBoxContainer();
		experimentUIPanel.AddChild(mainVBox);
		var titleLabel = new Label();
		titleLabel.Text = $"实验：{ExperimentName}";
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.AddThemeColorOverride("font_color", Colors.White);
		titleLabel.AddThemeFontSizeOverride("font_size", 24);
		mainVBox.AddChild(titleLabel);
		mainVBox.AddChild(new HSeparator());
		if (ShowInstructions) {
			instructionLabel = new RichTextLabel();
			instructionLabel.BbcodeEnabled = true;
			instructionLabel.CustomMinimumSize = new Vector2(0, 120);
			instructionLabel.Text = @"[b]操作说明：[/b]
[color=yellow]• 左键点击[/color]物品开始拖拽
[color=yellow]• 移动鼠标[/color]调整物品位置
[color=yellow]• 松开左键[/color]放置物品
[color=cyan]• 将物品重叠放置[/color]会触发实验现象

[b]提示：[/b]尝试将不同类型的物品放在一起！";
			mainVBox.AddChild(instructionLabel);
		}
		mainVBox.AddChild(new HSeparator());
		var logTitleLabel = new Label();
		logTitleLabel.Text = "实验记录：";
		logTitleLabel.AddThemeColorOverride("font_color", Colors.Cyan);
		logTitleLabel.AddThemeFontSizeOverride("font_size", 18);
		mainVBox.AddChild(logTitleLabel);
		logLabel = new RichTextLabel();
		logLabel.BbcodeEnabled = true;
		logLabel.CustomMinimumSize = new Vector2(0, 250);
		logLabel.ScrollFollowing = true;
		mainVBox.AddChild(logLabel);
		resetButton = new Button();
		resetButton.Text = "重置实验";
		resetButton.Pressed += OnResetExperiment;
		mainVBox.AddChild(resetButton);
		var player = GetTree().Root.FindChild("Player", true, false);
		if (player != null) {
			var canvasLayer = player.FindChild("CanvasLayer", false, false);
			if (canvasLayer != null) {
				canvasLayer.AddChild(experimentUIPanel);
			}
		}
	}
	
	private void UpdateLogDisplay() {
		if (logLabel == null) return;
		string display = "";
		int startIndex = Mathf.Max(0, this.experimentLog.Count - 20); 
		for (int i = startIndex; i < this.experimentLog.Count; i++) {
			display += this.experimentLog[i] + "\n";
		}
		logLabel.Text = display;
	}
	
	private void LogMessage(string message, string color = "white") {
		string timestamp = Time.GetTimeStringFromSystem();
		string formattedMsg = $"[color={color}][{timestamp}][/color] {message}";
		this.experimentLog.Add(formattedMsg);
		this.UpdateLogDisplay();
		GD.Print($"[实验日志] {message}");
	}
	
	private void OnItemDragStarted(PlacableItem item) {
		LogMessage($"开始拖动 [b]{item.ItemName}[/b]", "yellow");
	}
	
	private void OnItemDragEnded(PlacableItem item) {
		LogMessage($"放下 [b]{item.ItemName}[/b]", "lightgreen");
	}
	
	private void OnItemPlaced(PlacableItem item, Vector3 position) {
		LogMessage($"[b]{item.ItemName}[/b] 已放置在 {position}", "lightblue");
	}
	
	private void OnItemOverlapStarted(PlacableItem item1, PlacableItem item2) {
		LogMessage($"[b]{item1.ItemName}[/b] 与 [b]{item2.ItemName}[/b] 接触", "orange");
	}
	
	private void OnItemOverlapEnded(PlacableItem item1, PlacableItem item2) {
		LogMessage($"[b]{item1.ItemName}[/b] 与 [b]{item2.ItemName}[/b] 分离", "gray");
	}
	
	private void OnPhenomenonTriggered(ExperimentPhenomenon phenomenon, PlacableItem triggerItem) {
		LogMessage($"[color=red]★ 触发现象：{phenomenon.PhenomenonName}！[/color]", "red");
		if (!string.IsNullOrEmpty(phenomenon.ResultMessage)) {
			LogMessage($"[i]{phenomenon.ResultMessage}[/i]", "cyan");
		}
	}
	
	private void OnResetExperiment() {
		foreach (var item in placableItems) {
			if (GodotObject.IsInstanceValid(item)) {
				item.ResetPosition();
			}
		}
		if (phenomenonManager != null) {
			phenomenonManager.ClearAllEffects();
		}
		this.experimentLog.Clear();
		this.UpdateLogDisplay();
		LogMessage("实验已重置", "yellow");
		GD.Print("[ExampleExperiment] 实验已重置");
	}
	
	private void CreateExamplePhenomena() {
		var acidBaseReaction = new ExperimentPhenomenon();
		acidBaseReaction.PhenomenonName = "酸碱中和反应";
		acidBaseReaction.Description = "酸和碱混合产生中和反应";
		acidBaseReaction.TriggerItemType = "acid";
		acidBaseReaction.RequiredItemTypes = new Godot.Collections.Array<string> { "base" }; 
		acidBaseReaction.EffectColor = Colors.Yellow;
		acidBaseReaction.EffectDuration = 3.0f;
		acidBaseReaction.ShowMessage = true;
		acidBaseReaction.ResultMessage = "发生中和反应，产生盐和水！";
		Phenomena.Add(acidBaseReaction);
		var metalAcidReaction = new ExperimentPhenomenon();
		metalAcidReaction.PhenomenonName = "金属与酸反应";
		metalAcidReaction.Description = "活泼金属与酸反应产生氢气";
		metalAcidReaction.TriggerItemType = "metal"; 
		metalAcidReaction.RequiredItemTypes = new Godot.Collections.Array<string> { "acid" };
		metalAcidReaction.EffectColor = Colors.Cyan;
		metalAcidReaction.EffectDuration = 5.0f;
		metalAcidReaction.ShowMessage = true;
		metalAcidReaction.ResultMessage = "金属与酸反应，产生氢气！观察到气泡产生";
		Phenomena.Add(metalAcidReaction);
		var sodiumWaterReaction = new ExperimentPhenomenon();
		sodiumWaterReaction.PhenomenonName = "钠与水反应";
		sodiumWaterReaction.Description = "钠与水发生剧烈反应";
		sodiumWaterReaction.TriggerItemType = "sodium"; 
		sodiumWaterReaction.RequiredItemTypes = new Godot.Collections.Array<string> { "water" };
		sodiumWaterReaction.TriggerDelay = 0.5f; 
		sodiumWaterReaction.EffectColor = Colors.OrangeRed;
		sodiumWaterReaction.EffectDuration = 4.0f;
		sodiumWaterReaction.ShowMessage = true;
		sodiumWaterReaction.ResultMessage = "危险！钠与水剧烈反应，产生火焰和氢气！";
		sodiumWaterReaction.ConsumeItems = false; 
		Phenomena.Add(sodiumWaterReaction);
		GD.Print($"[ExampleExperiment] 已创建 {Phenomena.Count} 个示例现象");
	}
}
