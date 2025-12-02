using Godot;

[Tool]
public partial class ConnectionLine : Node3D, IConnectionLine {
    [Export] public float LineRadius { get; set; } = 0.02f;
    [Export] public Color LineColor { get; set; } = Colors.Blue;
    [Export] public Color HoverColor { get; set; } = Colors.Red;
    public ConnectableNode StartNode { get; private set; }
    public ConnectableNode EndNode { get; private set; }
    private MeshInstance3D meshInstance;
    private StaticBody3D staticBody;
    private CollisionShape3D _collision;
    private bool _isHovered = false;
    private StandardMaterial3D _material;

    public void Initialize(ConnectableNode startNode, ConnectableNode endNode) {
        StartNode = startNode;
        EndNode = endNode;
        this.meshInstance = new MeshInstance3D();
        AddChild(this.meshInstance);
        if (GetTree()?.EditedSceneRoot != null)
            this.meshInstance.Owner = GetTree().EditedSceneRoot;
        var cylinderMesh = new CylinderMesh();
        cylinderMesh.TopRadius = LineRadius;
        cylinderMesh.BottomRadius = LineRadius;
        cylinderMesh.Height = 1.0f;
        cylinderMesh.Rings = 1;
        cylinderMesh.RadialSegments = 16;
        this.meshInstance.Mesh = cylinderMesh;
        _material = new StandardMaterial3D();
        _material.AlbedoColor = LineColor;
        this.meshInstance.MaterialOverride = _material;
        this.staticBody = new StaticBody3D();
        this.meshInstance.AddChild(this.staticBody);
        if (GetTree()?.EditedSceneRoot != null)
            this.staticBody.Owner = GetTree().EditedSceneRoot;
        this.staticBody.CollisionLayer = 1 << 20;
        this.staticBody.CollisionMask = 0;
        _collision = new CollisionShape3D();
        this.staticBody.AddChild(_collision);
        if (GetTree()?.EditedSceneRoot != null)
            _collision.Owner = GetTree().EditedSceneRoot;
        this.UpdatePath();
    }

    public override void _Process(double delta) {
        if (StartNode != null && EndNode != null) {
            this.UpdatePath();
        }
    }

    private void UpdatePath() {
        if (this.meshInstance == null || StartNode == null || EndNode == null) return;
        Vector3 startPos = StartNode.GetConnectionPoint();
        Vector3 endPos = EndNode.GetConnectionPoint();
        float distance = startPos.DistanceTo(endPos);
        Vector3 midPoint = (startPos + endPos) / 2;
        this.meshInstance.GlobalPosition = midPoint;
        if (this.meshInstance.Mesh is CylinderMesh cylinderMesh) {
            cylinderMesh.Height = distance;
        }
        if (distance > 0.001f) {
            Vector3 direction = (endPos - startPos).Normalized();
            Vector3 up = Vector3.Up;
            if (direction.Cross(up).Length() > 0.001f) {
                this.meshInstance.LookAt(endPos, up);
                this.meshInstance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
            }
        }
        if (_collision != null) {
            var capsule = new CapsuleShape3D();
            capsule.Radius = LineRadius * 2;
            capsule.Height = distance;
            _collision.Shape = capsule;
        }
    }

    public void OnHoverEnter() {
        _isHovered = true;
        if (_material != null) {
            _material.AlbedoColor = HoverColor;
        }
    }

    public void OnHoverExit() {
        _isHovered = false;
        if (_material != null) {
            _material.AlbedoColor = LineColor;
        }
    }

    public void Destroy() {
        QueueFree();
    }
}