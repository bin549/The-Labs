using Godot;
using System;

[Tool]
public partial class ConnectionLine_ImmediateMesh : Node3D, IConnectionLine {
    [Export] public float LineRadius { get; set; } = 0.02f;
    [Export] public int RadialSegments { get; set; } = 8;
    [Export] public int PathSegments { get; set; } = 20;
    [Export] public Color LineColor { get; set; } = Colors.Blue;
    [Export] public Color HoverColor { get; set; } = Colors.Red;
    [Export] public float CableSlack { get; set; } = 0.3f;
    public ConnectableNode StartNode { get; private set; }
    public ConnectableNode EndNode { get; private set; }
    private MeshInstance3D _meshInstance;
    private ImmediateMesh _mesh;
    private StandardMaterial3D _material;
    private Curve3D _curve;
    private StaticBody3D _staticBody;
    private CollisionShape3D _collision;

    public void Initialize(ConnectableNode startNode, ConnectableNode endNode) {
        StartNode = startNode;
        EndNode = endNode;
        _mesh = new ImmediateMesh();
        _meshInstance = new MeshInstance3D();
        _meshInstance.Mesh = _mesh;
        AddChild(_meshInstance);
        if (GetTree()?.EditedSceneRoot != null)
            _meshInstance.Owner = GetTree().EditedSceneRoot;
        _material = new StandardMaterial3D();
        _material.AlbedoColor = LineColor;
        _material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        _meshInstance.MaterialOverride = _material;
        _curve = new Curve3D();
        _staticBody = new StaticBody3D();
        _staticBody.Name = "LineCollisionBody";
        _meshInstance.AddChild(_staticBody);
        if (GetTree()?.EditedSceneRoot != null)
            _staticBody.Owner = GetTree().EditedSceneRoot;
        _staticBody.CollisionLayer = 1 << 20;
        _staticBody.CollisionMask = 0;
        GD.Print($"[连线] 创建碰撞体:");
        GD.Print($"  - CollisionLayer: {_staticBody.CollisionLayer}");
        GD.Print($"  - 二进制: {Convert.ToString(_staticBody.CollisionLayer, 2).PadLeft(32, '0')}");
        GD.Print($"  - 第21层 = {1 << 20}");
        _collision = new CollisionShape3D();
        _collision.Name = "LineCollisionShape";
        _staticBody.AddChild(_collision);
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
        if (_curve == null || StartNode == null || EndNode == null) return;
        Vector3 startPos = StartNode.GetConnectionPoint();
        Vector3 endPos = EndNode.GetConnectionPoint();
        _curve.ClearPoints();
        Vector3 midPoint = (startPos + endPos) / 2;
        float distance = startPos.DistanceTo(endPos);
        Vector3 sagOffset = Vector3.Left * (distance * CableSlack);
        Vector3 controlPoint = midPoint + sagOffset;
        _curve.AddPoint(startPos, Vector3.Zero, (controlPoint - startPos).Normalized() * distance * 0.3f);
        _curve.AddPoint(controlPoint);
        _curve.AddPoint(endPos, (controlPoint - endPos).Normalized() * distance * 0.3f, Vector3.Zero);
        GenerateCableMesh();
        if (_collision != null && _staticBody != null) {
            var capsule = new CapsuleShape3D();
            capsule.Radius = LineRadius * 10;
            capsule.Height = distance;
            _collision.Shape = capsule;
            _staticBody.GlobalPosition = midPoint;
            Vector3 direction = (endPos - startPos).Normalized();
            if (direction.Length() > 0.01f) {
                _staticBody.LookAt(endPos, Vector3.Up);
                _staticBody.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
            }
            GD.Print($"[连线] 更新碰撞体:");
            GD.Print($"  - 半径: {capsule.Radius}m");
            GD.Print($"  - 高度: {capsule.Height}m");
            GD.Print($"  - 位置: {_staticBody.GlobalPosition}");
            GD.Print($"  - StaticBody层: {_staticBody.CollisionLayer}");
        }
    }

    private void GenerateCableMesh() {
        if (_mesh == null || _curve == null) return;
        _mesh.ClearSurfaces();
        float pathLength = _curve.GetBakedLength();
        if (pathLength < 0.001f) return;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var normals = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i <= PathSegments; i++) {
            float t = (float)i / PathSegments;
            float offset = t * pathLength;
            Vector3 pos = _curve.SampleBaked(offset);
            Vector3 forward;
            float delta = Mathf.Min(0.001f, pathLength * 0.01f);
            if (i == 0) {
                Vector3 nextPos = _curve.SampleBaked(offset + delta);
                forward = (nextPos - pos).Normalized();
            } else if (i == PathSegments) {
                Vector3 prevPos = _curve.SampleBaked(offset - delta);
                forward = (pos - prevPos).Normalized();
            } else {
                Vector3 prevPos = _curve.SampleBaked(offset - delta);
                Vector3 nextPos = _curve.SampleBaked(offset + delta);
                forward = (nextPos - prevPos).Normalized();
            }
            if (forward.Length() < 0.001f) {
                forward = Vector3.Forward;
            }
            Vector3 up;
            if (Mathf.Abs(forward.Dot(Vector3.Up)) < 0.99f) {
                up = Vector3.Up;
            } else {
                up = Vector3.Right;
            }
            Vector3 right = forward.Cross(up).Normalized();
            up = right.Cross(forward).Normalized();
            for (int j = 0; j <= RadialSegments; j++) {
                float angle = Mathf.Tau * j / RadialSegments;
                float x = Mathf.Cos(angle) * LineRadius;
                float y = Mathf.Sin(angle) * LineRadius;
                Vector3 circlePos = pos + right * x + up * y;
                Vector3 normal = (right * x + up * y).Normalized();
                vertices.Add(circlePos);
                normals.Add(normal);
            }
        }
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < PathSegments; i++) {
            for (int j = 0; j < RadialSegments; j++) {
                int current = i * (RadialSegments + 1) + j;
                int next = current + RadialSegments + 1;
                _mesh.SurfaceSetNormal(normals[current]);
                _mesh.SurfaceAddVertex(vertices[current]);
                _mesh.SurfaceSetNormal(normals[next]);
                _mesh.SurfaceAddVertex(vertices[next]);
                _mesh.SurfaceSetNormal(normals[current + 1]);
                _mesh.SurfaceAddVertex(vertices[current + 1]);
                _mesh.SurfaceSetNormal(normals[current + 1]);
                _mesh.SurfaceAddVertex(vertices[current + 1]);
                _mesh.SurfaceSetNormal(normals[next]);
                _mesh.SurfaceAddVertex(vertices[next]);
                _mesh.SurfaceSetNormal(normals[next + 1]);
                _mesh.SurfaceAddVertex(vertices[next + 1]);
            }
        }
        _mesh.SurfaceEnd();
    }

    public void OnHoverEnter() {
        if (_material != null) {
            _material.AlbedoColor = HoverColor;
        }
    }

    public void OnHoverExit() {
        if (_material != null) {
            _material.AlbedoColor = LineColor;
        }
    }

    public void Destroy() {
        QueueFree();
    }
}
