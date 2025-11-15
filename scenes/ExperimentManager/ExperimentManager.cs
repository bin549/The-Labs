using Godot;
using System.Collections.Generic;
using System.Linq;

// 实验管理器 - 管理所有实验和实验选择菜单
public partial class ExperimentManager : Node {
	[Export] public NodePath PlayerPath { get; set; }
	[Export] public Godot.Collections.Array<ExperimentInfo> Experiments { get; set; } = new();
	
	private Node3D player;
	private Control experimentMenu;
	private VBoxContainer categoryContainer;
	private bool isMenuVisible = false;
	private GameManager gameManager;
	
	// 实验分类
	private Dictionary<ExperimentCategory, List<ExperimentInfo>> categorizedExperiments = new();
	
	public override void _Ready() {
		this.ProcessMode = ProcessModeEnum.Always;
		ResolvePlayer();
		ResolveGameManager();
		CreateExperimentMenu();
		CategorizeExperiments();
		UpdateMenuUI();
	}
	
	public override void _PhysicsProcess(double delta) {
		// 确保gameManager引用始终有效
		if (gameManager == null || !GodotObject.IsInstanceValid(gameManager)) {
			ResolveGameManager();
		}
	}
	
	public override void _Input(InputEvent @event) {
		// P键切换实验菜单
		if (@event.IsActionPressed("toggle_experiment_menu")) {
			ToggleExperimentMenu();
			GetViewport().SetInputAsHandled();
		}
		
		// ESC关闭菜单
		if (isMenuVisible && (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("pause"))) {
			HideExperimentMenu();
			GetViewport().SetInputAsHandled();
		}
	}
	
	private void ResolvePlayer() {
		if (!string.IsNullOrEmpty(PlayerPath?.ToString())) {
			player = GetNodeOrNull<Node3D>(PlayerPath);
		}
		
		if (player == null) {
			player = GetTree().Root.FindChild("Player", true, false) as Node3D;
		}
		
		if (player == null) {
			GD.PushWarning("ExperimentManager: 未找到Player节点，传送功能将不可用。");
		}
	}
	
	private void ResolveGameManager() {
		gameManager = GetTree().Root.GetNodeOrNull<GameManager>("GameManager") ??
			GetTree().Root.FindChild("GameManager", true, false) as GameManager;
		
		if (gameManager == null) {
			GD.PushWarning("ExperimentManager: 未找到GameManager节点，菜单显示时摄像头控制可能无法禁用。");
		}
	}
	
	private void CreateExperimentMenu() {
		// 创建主面板
		experimentMenu = new PanelContainer();
		experimentMenu.Name = "ExperimentMenu";
		experimentMenu.Visible = false;
		
		// 居中显示
		experimentMenu.SetAnchorsPreset(Control.LayoutPreset.Center);
		experimentMenu.CustomMinimumSize = new Vector2(600, 400);
		experimentMenu.Position = new Vector2(-300, -200);
		
		// 主容器
		var mainVBox = new VBoxContainer();
		experimentMenu.AddChild(mainVBox);
		
		// 标题
		var titleLabel = new Label();
		titleLabel.Text = "实验选择菜单";
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
		
		// 滚动容器
		var scrollContainer = new ScrollContainer();
		scrollContainer.CustomMinimumSize = new Vector2(0, 300);
		mainVBox.AddChild(scrollContainer);
		
		// 分类容器
		categoryContainer = new VBoxContainer();
		scrollContainer.AddChild(categoryContainer);
		
		mainVBox.AddChild(new HSeparator());
		
		// 关闭按钮
		var closeButton = new Button();
		closeButton.Text = "关闭 (ESC)";
		closeButton.Pressed += HideExperimentMenu;
		mainVBox.AddChild(closeButton);
		
		// 添加到场景树
		AddChild(experimentMenu);
	}
	
	private void CategorizeExperiments() {
		categorizedExperiments.Clear();
		
		// 初始化分类
		categorizedExperiments[ExperimentCategory.Mechanics] = new List<ExperimentInfo>();
		categorizedExperiments[ExperimentCategory.Electricity] = new List<ExperimentInfo>();
		categorizedExperiments[ExperimentCategory.Chemistry] = new List<ExperimentInfo>();
		
		// 分类实验
		foreach (var exp in Experiments) {
			if (exp != null && categorizedExperiments.ContainsKey(exp.Category)) {
				categorizedExperiments[exp.Category].Add(exp);
			}
		}
	}
	
	private void UpdateMenuUI() {
		if (categoryContainer == null) return;
		
		// 清空现有内容
		foreach (var child in categoryContainer.GetChildren()) {
			child.QueueFree();
		}
		
		// 为每个分类创建UI
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
			
			// 分类标题
			var categoryLabel = new Label();
			categoryLabel.Text = $"【{categoryNames[category]}】";
			categoryLabel.AddThemeColorOverride("font_color", categoryColors[category]);
			categoryLabel.AddThemeFontSizeOverride("font_size", 24);
			categoryContainer.AddChild(categoryLabel);
			
			// 实验列表
			foreach (var exp in experiments) {
				var expButton = CreateExperimentButton(exp);
				categoryContainer.AddChild(expButton);
			}
			
			// 分隔线
			categoryContainer.AddChild(new HSeparator());
		}
	}
	
	private Button CreateExperimentButton(ExperimentInfo exp) {
		var button = new Button();
		button.CustomMinimumSize = new Vector2(0, 40);
		
		// 按钮文本
		var hbox = new HBoxContainer();
		button.AddChild(hbox);
		
		var nameLabel = new Label();
		nameLabel.Text = $"  {exp.ExperimentName}";
		nameLabel.AddThemeFontSizeOverride("font_size", 18);
		hbox.AddChild(nameLabel);
		
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
		
		var descLabel = new Label();
		descLabel.Text = exp.Description;
		descLabel.AddThemeColorOverride("font_color", Colors.Gray);
		descLabel.AddThemeFontSizeOverride("font_size", 14);
		hbox.AddChild(descLabel);
		
		// 点击事件
		button.Pressed += () => OnExperimentSelected(exp);
		
		return button;
	}
	
	private void OnExperimentSelected(ExperimentInfo exp) {
		GD.Print($"选择实验：{exp.ExperimentName}");
		
		// 传送玩家
		if (player != null && exp.Position != Vector3.Zero) {
			player.GlobalPosition = exp.Position;
			GD.Print($"传送到位置：{exp.Position}");
		}
		
		// 关闭菜单
		HideExperimentMenu();
		
		// 恢复鼠标控制
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	
	private void ToggleExperimentMenu() {
		if (isMenuVisible) {
			HideExperimentMenu();
		} else {
			ShowExperimentMenu();
		}
	}
	
	private void ShowExperimentMenu() {
		if (experimentMenu == null) return;
		
		experimentMenu.Visible = true;
		isMenuVisible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		
		// 通知GameManager菜单已打开，禁用玩家摄像头和移动控制
		if (gameManager != null) {
			gameManager.IsMenuOpen = true;
		}
		
		// 暂停游戏（可选）
		// GetTree().Paused = true;
	}
	
	private void HideExperimentMenu() {
		if (experimentMenu == null) return;
		
		experimentMenu.Visible = false;
		isMenuVisible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// 通知GameManager菜单已关闭，恢复玩家摄像头和移动控制
		if (gameManager != null) {
			gameManager.IsMenuOpen = false;
		}
		
		// 恢复游戏（可选）
		// GetTree().Paused = false;
	}
	
	// 添加实验
	public void RegisterExperiment(ExperimentInfo exp) {
		Experiments.Add(exp);
		CategorizeExperiments();
		UpdateMenuUI();
	}
	
	// 移除实验
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
