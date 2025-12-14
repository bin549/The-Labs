using Godot;
using System.Collections.Generic;

public partial class TeleportManager : Node {
    [Export] public NodePath PlayerPath { get; set; }
    [Export] public Godot.Collections.Array<ExperimentInfo> Experiments { get; set; } = new();
    [Export] public Godot.Collections.Array<TeleportPointInfo> TeleportPoints { get; set; } = new();
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
        CallDeferred(MethodName.RegisterSpawnPoint);
        CallDeferred(MethodName.RegisterPaintBoards);
        CallDeferred(MethodName.UpdateMenuUI);
    }

    public override void _PhysicsProcess(double delta) {
        if (this.gameManager == null || !GodotObject.IsInstanceValid(this.gameManager)) {
            this.ResolveGameManager();
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("toggle_experiment_menu")) {
            this.ToggleExperimentMenu();
            GetViewport().SetInputAsHandled();
        }
        if (this.isMenuVisible && (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("pause"))) {
            this.HideExperimentMenu();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ResolvePlayer() {
        if (!string.IsNullOrEmpty(PlayerPath?.ToString())) {
            this.player = GetNodeOrNull<Node3D>(PlayerPath);
        }
        if (this.player == null) {
            this.player = GetTree().Root.FindChild("Player", true, false) as Node3D;
        }
    }

    private void ResolveGameManager() {
        this.gameManager = GetTree().Root.GetNodeOrNull<GameManager>("GameManager") ??
                      GetTree().Root.FindChild("GameManager", true, false) as GameManager;
    }

    private void CreateExperimentMenu() {
        this.experimentMenu = new PanelContainer();
        this.experimentMenu.Name = "ExperimentMenu";
        this.experimentMenu.Visible = false;
        this.experimentMenu.SetAnchorsPreset(Control.LayoutPreset.Center);
        this.experimentMenu.CustomMinimumSize = new Vector2(600, 400);
        this.experimentMenu.Position = new Vector2(-300, -200);
        var mainVBox = new VBoxContainer();
        this.experimentMenu.AddChild(mainVBox);
        var titleLabel = new Label();
        titleLabel.Text = "传送菜单";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeColorOverride("font_color", Colors.White);
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        mainVBox.AddChild(titleLabel);
        var subtitleLabel = new Label();
        subtitleLabel.Text = "按P键显示/隐藏菜单 | 选择后将传送到目标位置";
        subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        subtitleLabel.AddThemeColorOverride("font_color", Colors.Gray);
        subtitleLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(subtitleLabel);
        mainVBox.AddChild(new HSeparator());
        var scrollContainer = new ScrollContainer();
        scrollContainer.CustomMinimumSize = new Vector2(0, 300);
        mainVBox.AddChild(scrollContainer);
        this.categoryContainer = new VBoxContainer();
        scrollContainer.AddChild(this.categoryContainer);
        mainVBox.AddChild(new HSeparator());
        var closeButton = new Button();
        closeButton.Text = "关闭 (ESC)";
        closeButton.Pressed += HideExperimentMenu;
        mainVBox.AddChild(closeButton);
        AddChild(this.experimentMenu);
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
        if (this.categoryContainer == null) return;
        foreach (var child in this.categoryContainer.GetChildren()) {
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
            this.categoryContainer.AddChild(categoryLabel);
            foreach (var exp in experiments) {
                var expButton = CreateExperimentButton(exp);
                this.categoryContainer.AddChild(expButton);
            }
            this.categoryContainer.AddChild(new HSeparator());
        }
        if (this.TeleportPoints.Count > 0) {
            var teleportLabel = new Label();
            teleportLabel.Text = "【默认位置】";
            teleportLabel.AddThemeColorOverride("font_color", Colors.Orange);
            teleportLabel.AddThemeFontSizeOverride("font_size", 24);
            this.categoryContainer.AddChild(teleportLabel);
            foreach (var point in this.TeleportPoints) {
                if (point == null) continue;
                var pointButton = CreateTeleportPointButton(point);
                this.categoryContainer.AddChild(pointButton);
            }
            this.categoryContainer.AddChild(new HSeparator());
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
        if (this.player != null && exp.Position != Vector3.Zero) {
            this.player.GlobalPosition = exp.Position;
        }
        this.HideExperimentMenu();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private Button CreateTeleportPointButton(TeleportPointInfo point) {
        var button = new Button();
        button.CustomMinimumSize = new Vector2(0, 40);
        var hbox = new HBoxContainer();
        button.AddChild(hbox);
        var nameLabel = new Label();
        nameLabel.Text = $"  {point.PointName}";
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        hbox.AddChild(nameLabel);
        hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var descLabel = new Label();
        descLabel.Text = point.Description;
        descLabel.AddThemeColorOverride("font_color", Colors.Gray);
        descLabel.AddThemeFontSizeOverride("font_size", 14);
        hbox.AddChild(descLabel);
        button.Pressed += () => OnTeleportPointSelected(point);
        return button;
    }

    private void OnTeleportPointSelected(TeleportPointInfo point) {
        if (this.player != null && point.Position != Vector3.Zero) {
            this.player.GlobalPosition = point.Position;
        }
        this.HideExperimentMenu();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void ToggleExperimentMenu() {
        if (this.isMenuVisible) {
            this.HideExperimentMenu();
        } else {
            this.ShowExperimentMenu();
        }
    }

    private void ShowExperimentMenu() {
        if (this.experimentMenu == null) return;
        this.experimentMenu.Visible = true;
        this.isMenuVisible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (this.gameManager != null) {
            this.gameManager.IsMenuOpen = true;
        }
    }

    private void HideExperimentMenu() {
        if (this.experimentMenu == null) return;
        this.experimentMenu.Visible = false;
        this.isMenuVisible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        if (this.gameManager != null) {
            this.gameManager.IsMenuOpen = false;
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

    private void RegisterSpawnPoint() {
        if (this.player == null) {
            this.ResolvePlayer();
            if (this.player == null) {
                CallDeferred(MethodName.RegisterSpawnPoint);
                return;
            }
        }
        if (!GodotObject.IsInstanceValid(this.player)) {
            CallDeferred(MethodName.RegisterSpawnPoint);
            return;
        }
        Vector3 spawnPosition = this.player.GlobalPosition;
        bool alreadyRegistered = false;
        foreach (var point in this.TeleportPoints) {
            if (point != null && point.PointName == "出生点") {
                alreadyRegistered = true;
                break;
            }
        }
        if (!alreadyRegistered) {
            var pointInfo = new TeleportPointInfo {
                PointName = "出生点",
                Description = "场景加载时的初始位置",
                Position = spawnPosition,
                PointNodePath = this.player.GetPath()
            };
            this.RegisterTeleportPoint(pointInfo);
        }
    }

    private void RegisterPaintBoards() {
        var paintBoards = new List<PaintBoard>();
        FindPaintBoards(GetTree().Root, paintBoards);
        
        foreach (var paintBoard in paintBoards) {
            if (paintBoard == null || !GodotObject.IsInstanceValid(paintBoard)) continue;
            bool alreadyRegistered = false;
            foreach (var point in this.TeleportPoints) {
                if (point != null && point.PointNodePath == paintBoard.GetPath()) {
                    alreadyRegistered = true;
                    break;
                }
            }
            if (!alreadyRegistered) {
                var pointInfo = new TeleportPointInfo {
                    PointName = "画板",
                    Description = "在画板上进行绘画创作",
                    Position = paintBoard.GlobalPosition,
                    PointNodePath = paintBoard.GetPath()
                };
                this.RegisterTeleportPoint(pointInfo);
            }
        }
    }

    public void RegisterTeleportPoint(TeleportPointInfo point) {
        this.TeleportPoints.Add(point);
        this.UpdateMenuUI();
    }

    public void UnregisterTeleportPoint(string pointName) {
        for (int i = this.TeleportPoints.Count - 1; i >= 0; i--) {
            if (this.TeleportPoints[i].PointName == pointName) {
                this.TeleportPoints.RemoveAt(i);
            }
        }
        this.UpdateMenuUI();
    }

    private void FindPaintBoards(Node node, List<PaintBoard> result) {
        if (node is PaintBoard paintBoard) {
            result.Add(paintBoard);
        }
        foreach (Node child in node.GetChildren()) {
            FindPaintBoards(child, result);
        }
    }
}