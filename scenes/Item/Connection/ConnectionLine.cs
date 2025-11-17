using Godot;

[Tool]
public partial class ConnectionLine : Node3D, IConnectionLine {
    [Export] public float LineRadius { get; set; } = 0.02f;
    [Export] public Color LineColor { get; set; } = Colors.Blue;
    [Export] public Color HoverColor { get; set; } = Colors.Red;
    public ConnectableNode StartNode { get; private set; }
    public ConnectableNode EndNode { get; private set; }
    private MeshInstance3D _meshInstance;
    private StaticBody3D _staticBody;
    private CollisionShape3D _collision;
    private bool _isHovered = false;
    private StandardMaterial3D _material;

    public void Initialize(ConnectableNode startNode, ConnectableNode endNode) {
        StartNode = startNode;
        EndNode = endNode;
        _meshInstance = new MeshInstance3D();
        AddChild(_meshInstance);
        if (GetTree()?.EditedSceneRoot != null)
            _meshInstance.Owner = GetTree().EditedSceneRoot;
        var cylinderMesh = new CylinderMesh();
        cylinderMesh.TopRadius = LineRadius;
        cylinderMesh.BottomRadius = LineRadius;
        cylinderMesh.Height = 1.0f;
        cylinderMesh.Rings = 1;
        cylinderMesh.RadialSegments = 16;
        _meshInstance.Mesh = cylinderMesh;
        _material = new StandardMaterial3D();
        _material.AlbedoColor = LineColor;
        _meshInstance.MaterialOverride = _material;
        _staticBody = new StaticBody3D();
        _meshInstance.AddChild(_staticBody);
        if (GetTree()?.EditedSceneRoot != null)
            _staticBody.Owner = GetTree().EditedSceneRoot;
        _staticBody.CollisionLayer = 1 << 20;
        _staticBody.CollisionMask = 0;
        _collision = new CollisionShape3D();
        _staticBody.AddChild(_collision);
        if (GetTree()?.EditedSceneRoot != null)
            _collision.Owner = GetTree().EditedSceneRoot;
        UpdatePath();
    }

    public override void _Process(double delta) {
        if (StartNode != null && EndNode != null) {
            this.UpdatePath();
        }
    }

    private void UpdatePath() {
        if (_meshInstance == null || StartNode == null || EndNode == null) return;
        Vector3 startPos = StartNode.GetConnectionPoint();
        Vector3 endPos = EndNode.GetConnectionPoint();
        float distance = startPos.DistanceTo(endPos);
        Vector3 midPoint = (startPos + endPos) / 2;
        _meshInstance.GlobalPosition = midPoint;
        if (_meshInstance.Mesh is CylinderMesh cylinderMesh) {
            cylinderMesh.Height = distance;
        }
        if (distance > 0.001f) {
            Vector3 direction = (endPos - startPos).Normalized();
            Vector3 up = Vector3.Up;
            if (direction.Cross(up).Length() > 0.001f) {
                _meshInstance.LookAt(endPos, up);
                _meshInstance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
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
