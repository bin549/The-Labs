using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ExperimentManager : Node {
    [Export] public NodePath PlayerPath { get; set; }
    [Export] public Godot.Collections.Array<ExperimentInfo> Experiments { get; set; } = new();
    private Node3D player;
    private Control experimentMenu;
    private VBoxContainer categoryContainer;
    private bool isMenuVisible = false;
    private GameManager gameManager;
    private Dictionary<ExperimentCategory, List<ExperimentInfo>> categorizedExperiments = new();

    public override void _Ready() {
        this.ProcessMode = ProcessModeEnum.Always;
        this.ResolvePlayer();
        this.ResolveGameManager();
        this.CreateExperimentMenu();
        this.CategorizeExperiments();
        this.UpdateMenuUI();
    }

    public override void _PhysicsProcess(double delta) {
        if (gameManager == null || !GodotObject.IsInstanceValid(gameManager)) {
            this.ResolveGameManager();
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("toggle_experiment_menu")) {
            this.ToggleExperimentMenu();
            GetViewport().SetInputAsHandled();
        }
        if (isMenuVisible && (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("pause"))) {
            this.HideExperimentMenu();
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
        experimentMenu = new PanelContainer();
        experimentMenu.Name = "ExperimentMenu";
        experimentMenu.Visible = false;
        experimentMenu.SetAnchorsPreset(Control.LayoutPreset.Center);
        experimentMenu.CustomMinimumSize = new Vector2(600, 400);
        experimentMenu.Position = new Vector2(-300, -200);
        var mainVBox = new VBoxContainer();
        experimentMenu.AddChild(mainVBox);
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
        AddChild(experimentMenu);
    }

    private void CategorizeExperiments() {
        this.categorizedExperiments.Clear();
        this.categorizedExperiments[ExperimentCategory.Mechanics] = new List<ExperimentInfo>();
        this.categorizedExperiments[ExperimentCategory.Electricity] = new List<ExperimentInfo>();
        this.categorizedExperiments[ExperimentCategory.Chemistry] = new List<ExperimentInfo>();
        foreach (var exp in this.Experiments) {
            if (exp != null && this.categorizedExperiments.ContainsKey(exp.Category)) {
                this.categorizedExperiments[exp.Category].Add(exp);
            }
        }
    }

    private void UpdateMenuUI() {
        if (categoryContainer == null) return;
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
        foreach (var category in this.categorizedExperiments.Keys) {
            var experiments = this.categorizedExperiments[category];
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
    }

    private Button CreateExperimentButton(ExperimentInfo exp) {
        var button = new Button();
        button.CustomMinimumSize = new Vector2(0, 40);
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
        if (isMenuVisible) {
            this.HideExperimentMenu();
        } else {
            this.ShowExperimentMenu();
        }
    }

    private void ShowExperimentMenu() {
        if (experimentMenu == null) return;
        experimentMenu.Visible = true;
        isMenuVisible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (gameManager != null) {
            gameManager.IsMenuOpen = true;
        }
    }

    private void HideExperimentMenu() {
        if (experimentMenu == null) return;
        experimentMenu.Visible = false;
        isMenuVisible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        if (gameManager != null) {
            gameManager.IsMenuOpen = false;
        }
    }

    public void RegisterExperiment(ExperimentInfo exp) {
        this.Experiments.Add(exp);
        this.CategorizeExperiments();
        this.UpdateMenuUI();
    }

    public void UnregisterExperiment(string experimentName) {
        for (int i = this.Experiments.Count - 1; i >= 0; i--) {
            if (this.Experiments[i].ExperimentName == experimentName) {
                this.Experiments.RemoveAt(i);
            }
        }
        this.CategorizeExperiments();
        this.UpdateMenuUI();
    }
}