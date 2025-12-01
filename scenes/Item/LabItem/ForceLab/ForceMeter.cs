using Godot;

public partial class ForceMeter : Node3D {
    [Export] public NodePath PointerPath { get; set; }
    [Export] public NodePath DisplayLabelPath { get; set; }
    [Export] public float MaxForce { get; set; } = 50.0f;
    [Export] public float MaxRotation { get; set; } = 180.0f;
    private Node3D pointer;
    private Label3D displayLabel;
    private float currentForce = 0.0f;
    private float targetForce = 0.0f;
    private float smoothSpeed = 5.0f;

    public override void _Ready() {
        ResolveComponents();
        UpdateDisplay();
    }

    public override void _Process(double delta) {
        if (!Mathf.IsEqualApprox(currentForce, targetForce)) {
            currentForce = Mathf.Lerp(currentForce, targetForce, smoothSpeed * (float)delta);
            UpdateDisplay();
        }
    }

    private void ResolveComponents() {
        if (!string.IsNullOrEmpty(PointerPath?.ToString())) {
            pointer = GetNodeOrNull<Node3D>(PointerPath);
        }
        if (pointer == null) {
            pointer = FindChild("Pointer", true, false) as Node3D;
        }
        if (pointer == null) {
            GD.PushWarning($"{Name}: 未找到指针节点，将创建默认指针。");
            this.CreateDefaultPointer();
        }
        if (!string.IsNullOrEmpty(DisplayLabelPath?.ToString())) {
            displayLabel = GetNodeOrNull<Label3D>(DisplayLabelPath);
        }
        if (displayLabel == null) {
            displayLabel = FindChild("DisplayLabel", true, false) as Label3D;
        }
        if (displayLabel == null) {
            this.CreateDefaultLabel();
        }
    }

    private void CreateDefaultPointer() {
        pointer = new Node3D();
        pointer.Name = "Pointer";
        AddChild(pointer);
        var mesh = new MeshInstance3D();
        var cylinder = new CylinderMesh();
        cylinder.TopRadius = 0.002f;
        cylinder.BottomRadius = 0.002f;
        cylinder.Height = 0.05f;
        mesh.Mesh = cylinder;
        var material = new StandardMaterial3D();
        material.AlbedoColor = Colors.Red;
        material.EmissionEnabled = true;
        material.Emission = Colors.Red;
        mesh.MaterialOverride = material;
        mesh.Position = new Vector3(0, 0.025f, 0);
        pointer.AddChild(mesh);
    }

    private void CreateDefaultLabel() {
        displayLabel = new Label3D();
        displayLabel.Name = "DisplayLabel";
        displayLabel.Text = "0.00 N";
        displayLabel.FontSize = 32;
        displayLabel.Position = new Vector3(0, -0.05f, 0);
        displayLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        AddChild(displayLabel);
    }

    public void SetForceValue(float force) {
        targetForce = Mathf.Clamp(force, 0, MaxForce);
    }

    private void UpdateDisplay() {
        if (pointer != null) {
            float angle = (currentForce / MaxForce) * MaxRotation;
            pointer.RotationDegrees = new Vector3(0, 0, -angle);
        }
        if (displayLabel != null) {
            displayLabel.Text = $"{currentForce:F2} N";
            if (currentForce < MaxForce * 0.3f) {
                displayLabel.Modulate = Colors.Green;
            } else if (currentForce < MaxForce * 0.7f) {
                displayLabel.Modulate = Colors.Yellow;
            } else {
                displayLabel.Modulate = Colors.Red;
            }
        }
    }

    public void Reset() {
        currentForce = 0.0f;
        targetForce = 0.0f;
        this.UpdateDisplay();
    }
}