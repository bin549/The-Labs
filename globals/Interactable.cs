using Godot;
using System.Collections.Generic;
using PhantomCamera;

public partial class Interactable : Node3D {
    [Export]
    public string DisplayName { get; set; } = "物体";
    [Export]
    public string ActionName { get; set; } = "交互";
    [Export]
    public NodePath VisualRootPath { get; set; } = new NodePath();
    [Export]
    public Color OutlineColor { get; set; } = new Color(1, 1, 1);
    [Export]
    public float OutlineThickness { get; set; } = 3.0f;
    [Export]
    public NodePath NameLabelPath { get; set; } = new NodePath();
    [Export]
    public NodePath LinePath { get; set; } = new NodePath();
    [Export]
    public NodePath FocusCameraPath { get; set; } = new NodePath();
    private Node3D _visualRoot;
    private readonly Dictionary<GeometryInstance3D, Material> _originalOverlays = new();
    private Shader _outlineShader;
    private ShaderMaterial _outlineMat;
    private Label3D _nameLabel;
    private Node3D _lineNode;
    private Camera3D _focusCamera;
    private Node3D _phantomCamNode;
    private int? _savedPhantomPriority;
    private bool _usedPhantom;

    public override void _Ready() {
        _visualRoot = GetNodeOrNull<Node3D>(VisualRootPath);
        _outlineShader = ResourceLoader.Load<Shader>("res://shaders/Outline.gdshader");
        if (_outlineShader != null) {
            _outlineMat = new ShaderMaterial { Shader = _outlineShader };
            _outlineMat.SetShaderParameter("thickness", 0.0f);
            _outlineMat.SetShaderParameter("edge_color", OutlineColor);
            foreach (var target in EnumerateGeometryInstances(_visualRoot ?? this))
            {
                target.MaterialOverlay = _outlineMat;
            }
        }
        _nameLabel = GetNodeOrNull<Label3D>(NameLabelPath);
        _lineNode = GetNodeOrNull<Node3D>(LinePath);
        _focusCamera = GetNodeOrNull<Camera3D>(FocusCameraPath);
        _phantomCamNode = GetNodeOrNull<Node3D>("PhantomCamera3D");

        if (_nameLabel != null) {
            _nameLabel.Text = DisplayName;
            _nameLabel.Visible = true;
        }
        if (_lineNode != null) {
            _lineNode.Visible = true;
        }
    }

    public virtual void OnFocusEnter() {
        ApplyOutline(true);
        if (_nameLabel != null) {
            _nameLabel.Text = $"[E] {ActionName}";
        }
    }

    public virtual void OnFocusExit() {
        ApplyOutline(false);
        if (_nameLabel != null) {
            _nameLabel.Text = DisplayName;
        }
    }

    public virtual void Interact(Node3D interactor) {
        GD.Print($"Interact with {DisplayName}");
        if (_phantomCamNode != null) {
            var pcam = _phantomCamNode.AsPhantomCamera3D();
            if (_savedPhantomPriority == null) _savedPhantomPriority = pcam.Priority;
            pcam.Priority = (_savedPhantomPriority ?? 0) + 1000;
            _usedPhantom = true;
            return;
        }
        if (_focusCamera != null) _focusCamera.Current = true;
    }

    public virtual void ExitInteraction() {
        if (_usedPhantom && _phantomCamNode != null) {
            var pcam = _phantomCamNode.AsPhantomCamera3D();
            if (_savedPhantomPriority != null) pcam.Priority = _savedPhantomPriority.Value;
        }
        _usedPhantom = false;
        _savedPhantomPriority = null;
        if (_nameLabel != null) _nameLabel.Text = DisplayName;
        ApplyOutline(false);
    }

    public Node3D GetActiveCameraNode() {
        return _phantomCamNode ?? (Node3D)_focusCamera;
    }


    private void ApplyOutline(bool enable) {
        if (_outlineMat == null) return;
        _outlineMat.SetShaderParameter("edge_color", OutlineColor);
        _outlineMat.SetShaderParameter("thickness", enable ? OutlineThickness : 0.0f);
    }

    private static IEnumerable<GeometryInstance3D> EnumerateGeometryInstances(Node root) {
        var stack = new Stack<Node>();
        stack.Push(root);
        while (stack.Count > 0) {
            var node = stack.Pop();
            if (node is GeometryInstance3D geom)
                yield return geom;
            foreach (Node child in node.GetChildren())
                stack.Push(child);
        }
    }
}
