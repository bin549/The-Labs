using Godot;
using System.Collections.Generic;
using System.Linq;

// 实验管理器 - 测试版（带调试输出）
public partial class ExperimentManager_Debug : Node {
	[Export] public NodePath PlayerPath { get; set; }
	[Export] public Godot.Collections.Array<ExperimentInfo> Experiments { get; set; } = new();
	
	private Node3D player;
	private Control experimentMenu;
	private VBoxContainer categoryContainer;
	private bool isMenuVisible = false;
	
	private Dictionary<ExperimentCategory, List<ExperimentInfo>> categorizedExperiments = new();
	
	public override void _Ready() {
		GD.Print("=== ExperimentManager _Ready 开始 ===");
		this.ProcessMode = ProcessModeEnum.Always;
		
		ResolvePlayer();
		GD.Print($"Player找到: {player != null}");
		if (player != null) {
			GD.Print($"Player位置: {player.GlobalPosition}");
		}
		
		CreateExperimentMenu();
		GD.Print($"菜单创建: {experimentMenu != null}");
		
		CategorizeExperiments();
		GD.Print($"实验数量: {Experiments.Count}");
		
		UpdateMenuUI();
		GD.Print("=== ExperimentManager _Ready 完成 ===");
	}
	
	public override void _Input(InputEvent @event) {
		// 测试P键直接按下
		if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
			if (keyEvent.Keycode == Key.P || keyEvent.PhysicalKeycode == Key.P) {
				GD.Print("检测到P键按下（直接检测）");
			}
		}
		
		// P键切换实验菜单
		if (@event.IsActionPressed("toggle_experiment_menu")) {
			GD.Print("toggle_experiment_menu 动作触发！");
			ToggleExperimentMenu();
			GetViewport().SetInputAsHandled();
		}
		
		// ESC关闭菜单
		if (isMenuVisible && (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("pause"))) {
			GD.Print("ESC关闭菜单");
			HideExperimentMenu();
			GetViewport().SetInputAsHandled();
		}
	}
	
	private void ResolvePlayer() {
		if (!string.IsNullOrEmpty(PlayerPath?.ToString())) {
			player = GetNodeOrNull<Node3D>(PlayerPath);
			GD.Print($"通过PlayerPath找到玩家: {player != null}");
		}
		
		if (player == null) {
			player = GetTree().Root.FindChild("Player", true, false) as Node3D;
			GD.Print($"通过FindChild找到玩家: {player != null}");
		}
		
		if (player == null) {
			GD.PushWarning("ExperimentManager: 未找到Player节点，传送功能将不可用。");
		}
	}
	
	private void CreateExperimentMenu() {
		GD.Print("开始创建菜单...");
		
		experimentMenu = new PanelContainer();
		experimentMenu.Name = "ExperimentMenu";
		experimentMenu.Visible = false;
		
		experimentMenu.SetAnchorsPreset(Control.LayoutPreset.Center);
		experimentMenu.CustomMinimumSize = new Vector2(600, 400);
		experimentMenu.Position = new Vector2(-300, -200);
		
		var mainVBox = new VBoxContainer();
		experimentMenu.AddChild(mainVBox);
		
		var titleLabel = new Label();
		titleLabel.Text = "实验选择菜单（测试版）";
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.AddThemeColorOverride("font_color", Colors.White);
		titleLabel.AddThemeFontSizeOverride("font_size", 32);
		mainVBox.AddChild(titleLabel);
		
		var subtitleLabel = new Label();
		subtitleLabel.Text = "按P键显示/隐藏菜单 | 选择实验后将传送到实验位置";
		subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		subtitleLabel.AddThemeColorOverride("font_color", Colors.Gray);
		subtitleLabel.AddThemeFontSizeOverride("font_size", 14);
		mainVBox.AddChild(subtitleLabel);
		
		mainVBox.AddChild(new HSeparator());
		
		var scrollContainer = new ScrollContainer();
		scrollContainer.CustomMinimumSize = new Vector2(0, 300);
		mainVBox.AddChild(scrollContainer);
		
		categoryContainer = new VBoxContainer();
		scrollContainer.AddChild(categoryContainer);
		
		mainVBox.AddChild(new HSeparator());
		
		var closeButton = new Button();
		closeButton.Text = "关闭 (ESC)";
		closeButton.Pressed += HideExperimentMenu;
		mainVBox.AddChild(closeButton);
		
		// 添加到场景树
		AddChild(experimentMenu);
		GD.Print("菜单已添加到场景树");
	}
	
	private void CategorizeExperiments() {
		categorizedExperiments.Clear();
		categorizedExperiments[ExperimentCategory.Mechanics] = new List<ExperimentInfo>();
		categorizedExperiments[ExperimentCategory.Electricity] = new List<ExperimentInfo>();
		categorizedExperiments[ExperimentCategory.Chemistry] = new List<ExperimentInfo>();
		
		foreach (var exp in Experiments) {
			if (exp != null && categorizedExperiments.ContainsKey(exp.Category)) {
				categorizedExperiments[exp.Category].Add(exp);
				GD.Print($"添加实验: {exp.ExperimentName} ({exp.Category})");
			}
		}
	}
	
	private void UpdateMenuUI() {
		if (categoryContainer == null) {
			GD.PushWarning("categoryContainer为空！");
			return;
		}
		
		foreach (var child in categoryContainer.GetChildren()) {
			child.QueueFree();
		}
		
		var categoryNames = new Dictionary<ExperimentCategory, string> {
			{ ExperimentCategory.Mechanics, "力学物理" },
			{ ExperimentCategory.Electricity, "电学物理" },
			{ ExperimentCategory.Chemistry, "化学实验" }
		};
		
		var categoryColors = new Dictionary<ExperimentCategory, Color> {
			{ ExperimentCategory.Mechanics, Colors.Cyan },
			{ ExperimentCategory.Electricity, Colors.Yellow },
			{ ExperimentCategory.Chemistry, Colors.LightGreen }
		};
		
		foreach (var category in categorizedExperiments.Keys) {
			var experiments = categorizedExperiments[category];
			if (experiments.Count == 0) continue;
			
			var categoryLabel = new Label();
			categoryLabel.Text = $"【{categoryNames[category]}】";
			categoryLabel.AddThemeColorOverride("font_color", categoryColors[category]);
			categoryLabel.AddThemeFontSizeOverride("font_size", 24);
			categoryContainer.AddChild(categoryLabel);
			
			foreach (var exp in experiments) {
				var expButton = CreateExperimentButton(exp);
				categoryContainer.AddChild(expButton);
			}
			
			categoryContainer.AddChild(new HSeparator());
		}
		
		GD.Print($"菜单UI更新完成，分类数: {categorizedExperiments.Count}");
	}
	
	private Button CreateExperimentButton(ExperimentInfo exp) {
		var button = new Button();
		button.CustomMinimumSize = new Vector2(0, 40);
		button.Text = $"{exp.ExperimentName} - {exp.Description}";
		button.Pressed += () => OnExperimentSelected(exp);
		return button;
	}
	
	private void OnExperimentSelected(ExperimentInfo exp) {
		GD.Print($"选择实验：{exp.ExperimentName}");
		
		if (player != null && exp.Position != Vector3.Zero) {
			player.GlobalPosition = exp.Position;
			GD.Print($"传送到位置：{exp.Position}");
		}
		
		HideExperimentMenu();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	
	private void ToggleExperimentMenu() {
		GD.Print($"ToggleExperimentMenu 被调用，当前状态: {isMenuVisible}");
		if (isMenuVisible) {
			HideExperimentMenu();
		} else {
			ShowExperimentMenu();
		}
	}
	
	private void ShowExperimentMenu() {
		GD.Print("显示实验菜单");
		if (experimentMenu == null) {
			GD.PushWarning("experimentMenu为空！");
			return;
		}
		
		experimentMenu.Visible = true;
		isMenuVisible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GD.Print("菜单已显示");
	}
	
	private void HideExperimentMenu() {
		GD.Print("隐藏实验菜单");
		if (experimentMenu == null) return;
		
		experimentMenu.Visible = false;
		isMenuVisible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		GD.Print("菜单已隐藏");
	}
	
	public void RegisterExperiment(ExperimentInfo exp) {
		Experiments.Add(exp);
		CategorizeExperiments();
		UpdateMenuUI();
	}
	
	public void UnregisterExperiment(string experimentName) {
		for (int i = Experiments.Count - 1; i >= 0; i--) {
			if (Experiments[i].ExperimentName == experimentName) {
				Experiments.RemoveAt(i);
			}
		}
		CategorizeExperiments();
		UpdateMenuUI();
	}
}

