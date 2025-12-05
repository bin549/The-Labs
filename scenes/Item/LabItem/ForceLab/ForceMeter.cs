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
        this.ResolveComponents();
        this.UpdateDisplay();
    }

    public override void _Process(double delta) {
        if (!Mathf.IsEqualApprox(this.currentForce, this.targetForce)) {
            this.currentForce = Mathf.Lerp(this.currentForce, this.targetForce, smoothSpeed * (float)delta);
            this.UpdateDisplay();
        }
    }

    private void ResolveComponents() {
        if (!string.IsNullOrEmpty(PointerPath?.ToString())) {
            this.pointer = GetNodeOrNull<Node3D>(PointerPath);
        }
        if (this.pointer == null) {
            this.pointer = FindChild("Pointer", true, false) as Node3D;
        }
        if (this.pointer == null) {
            GD.PushWarning($"{Name}: 未找到指针节点，将创建默认指针。");
            this.CreateDefaultPointer();
        }
        if (!string.IsNullOrEmpty(DisplayLabelPath?.ToString())) {
            this.displayLabel = GetNodeOrNull<Label3D>(DisplayLabelPath);
        }
        if (this.displayLabel == null) {
            this.displayLabel = FindChild("DisplayLabel", true, false) as Label3D;
        }
        if (this.displayLabel == null) {
            this.CreateDefaultLabel();
        }
    }

    private void CreateDefaultPointer() {
        this.pointer = new Node3D();
        this.pointer.Name = "Pointer";
        AddChild(this.pointer);
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
        this.pointer.AddChild(mesh);
    }

    private void CreateDefaultLabel() {
        this.displayLabel = new Label3D();
        this.displayLabel.Name = "DisplayLabel";
        this.displayLabel.Text = "0.00 N";
        this.displayLabel.FontSize = 32;
        this.displayLabel.Position = new Vector3(0, -0.05f, 0);
        this.displayLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        AddChild(this.displayLabel);
    }

    public void SetForceValue(float force) {
        this.targetForce = Mathf.Clamp(force, 0, MaxForce);
    }

    private void UpdateDisplay() {
        if (this.pointer != null) {
            float angle = (this.currentForce / MaxForce) * MaxRotation;
            this.pointer.RotationDegrees = new Vector3(0, 0, -angle);
        }
        if (this.displayLabel != null) {
            this.displayLabel.Text = $"{this.currentForce:F2} N";
            if (this.currentForce < MaxForce * 0.3f) {
                this.displayLabel.Modulate = Colors.Green;
            } else if (this.currentForce < MaxForce * 0.7f) {
                this.displayLabel.Modulate = Colors.Yellow;
            } else {
                this.displayLabel.Modulate = Colors.Red;
            }
        }
    }

    public void Reset() {
        this.currentForce = 0.0f;
        this.targetForce = 0.0f;
        this.UpdateDisplay();
    }
}