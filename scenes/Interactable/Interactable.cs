using Godot;
using PhantomCamera;
using System.Collections.Generic;

public partial class Interactable : Node3D {
    [Export] protected string DisplayName { get; set; } = "物体";
    [Export] protected string ActionName { get; set; } = "交互";
    [Export] protected ShaderMaterial outlineMat;
    [Export] protected float outlineSize = 1.05f;
    [Export]
    public Godot.Collections.Array<NodePath> OutlineTargetPaths { get; set; } = new();
    [Export] protected Label3D nameLabel;
    [Export] protected Node3D lineNode;
    protected bool isFocus = false;
    protected bool isInteracting = false;
    [Export] protected GameManager gameManager;

    private readonly List<GeometryInstance3D> outlineTargets = new();
    private readonly Dictionary<GeometryInstance3D, Material> originalOverlays = new();

    public override void _Ready() {
        CacheOutlineTargets();
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
            this.nameLabel.Visible = true;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = true;
        }
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("interact") && this.isFocus && !this.gameManager.IsBusy) {
            this.EnterInteraction();
        }
    }

    public virtual void EnterInteraction() {
        this.gameManager.SetCurrentInteractable(this);
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public virtual void ExitInteraction() {
    }

    public virtual void OnFocusEnter() {
        ApplyOutline(true);
        if (this.nameLabel != null) {
            this.nameLabel.Text = $"[E] {ActionName}";
        }
        this.isFocus = true;
    }

    public virtual void OnFocusExit() {
        ApplyOutline(false);
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
        }
        this.isFocus = false;
    }

    protected virtual void Interact(Node3D interactor) {
    }

    private void ApplyOutline(bool enable) {
        if (outlineMat == null) return;
        SetOutlineActive(enable);
    }

    protected void SetOutlineActive(bool enable) {
        if (outlineMat == null || outlineTargets.Count == 0) return;

        foreach (var instance in outlineTargets) {
            if (!GodotObject.IsInstanceValid(instance)) continue;

            if (!originalOverlays.ContainsKey(instance)) {
                originalOverlays[instance] = instance.MaterialOverlay;
            }

            instance.MaterialOverlay = enable ? outlineMat : originalOverlays[instance];
        }

        if (outlineMat != null) {
            outlineMat.SetShaderParameter("size", enable ? outlineSize : 0.0f);
        }
    }

    private void CacheOutlineTargets() {
        outlineTargets.Clear();
        originalOverlays.Clear();

        if (OutlineTargetPaths == null || OutlineTargetPaths.Count == 0) {
            GD.PushWarning($"{Name}: 未设置 OutlineTargetPaths，无法应用描边。");
            return;
        }

        foreach (var path in OutlineTargetPaths) {
            if (path.ToString() == string.Empty) continue;

            var instance = GetNodeOrNull<GeometryInstance3D>(path);
            if (instance != null) {
                outlineTargets.Add(instance);
                originalOverlays[instance] = instance.MaterialOverlay;
            } else {
                GD.PushWarning($"{Name}: 未找到描边目标节点 {path}");
            }
        }
    }
}
