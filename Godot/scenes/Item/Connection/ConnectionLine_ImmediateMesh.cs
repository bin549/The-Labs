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
    private MeshInstance3D meshInstance;
    private ImmediateMesh mesh;
    private StandardMaterial3D material;
    private Curve3D curve;
    private StaticBody3D staticBody;
    private CollisionShape3D collision;

    public void Initialize(ConnectableNode startNode, ConnectableNode endNode) {
        this.StartNode = startNode;
        this.EndNode = endNode;
        this.mesh = new ImmediateMesh();
        this.meshInstance = new MeshInstance3D();
        this.meshInstance.Mesh = this.mesh;
        AddChild(this.meshInstance);
        if (GetTree()?.EditedSceneRoot != null)
            this.meshInstance.Owner = GetTree().EditedSceneRoot;
        this.material = new StandardMaterial3D();
        this.material.AlbedoColor = LineColor;
        this.material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        this.meshInstance.MaterialOverride = this.material;
        this.curve = new Curve3D();
        this.staticBody = new StaticBody3D();
        this.staticBody.Name = "LineCollisionBody";
        this.meshInstance.AddChild(this.staticBody);
        if (GetTree()?.EditedSceneRoot != null)
            this.staticBody.Owner = GetTree().EditedSceneRoot;
        this.staticBody.CollisionLayer = 1 << 20;
        this.staticBody.CollisionMask = 0;
        this.collision = new CollisionShape3D();
        this.collision.Name = "LineCollisionShape";
        this.staticBody.AddChild(this.collision);
        if (GetTree()?.EditedSceneRoot != null)
            this.collision.Owner = GetTree().EditedSceneRoot;
        this.UpdatePath();
    }

    public override void _Process(double delta) {
        if (this.StartNode != null && this.EndNode != null) {
            this.UpdatePath();
        }
    }

    private void UpdatePath() {
        if (this.curve == null || this.StartNode == null || this.EndNode == null) return;
        Vector3 startPos = this.StartNode.GetConnectionPoint();
        Vector3 endPos = this.EndNode.GetConnectionPoint();
        this.curve.ClearPoints();
        Vector3 midPoint = (startPos + endPos) / 2;
        float distance = startPos.DistanceTo(endPos);
        Vector3 sagOffset = Vector3.Left * (distance * CableSlack);
        Vector3 controlPoint = midPoint + sagOffset;
        this.curve.AddPoint(startPos, Vector3.Zero, (controlPoint - startPos).Normalized() * distance * 0.3f);
        this.curve.AddPoint(controlPoint);
        this.curve.AddPoint(endPos, (controlPoint - endPos).Normalized() * distance * 0.3f, Vector3.Zero);
        this.GenerateCableMesh();
        if (this.collision != null && this.staticBody != null) {
            var capsule = new CapsuleShape3D();
            capsule.Radius = LineRadius * 10;
            capsule.Height = distance;
            this.collision.Shape = capsule;
            this.staticBody.GlobalPosition = midPoint;
            Vector3 direction = (endPos - startPos).Normalized();
            if (direction.Length() > 0.01f) {
                this.staticBody.LookAt(endPos, Vector3.Up);
                this.staticBody.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
            }
        }
    }

    private void GenerateCableMesh() {
        if (this.mesh == null || this.curve == null) return;
        this.mesh.ClearSurfaces();
        float pathLength = this.curve.GetBakedLength();
        if (pathLength < 0.001f) return;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var normals = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i <= PathSegments; i++) {
            float t = (float)i / PathSegments;
            float offset = t * pathLength;
            Vector3 pos = this.curve.SampleBaked(offset);
            Vector3 forward;
            float delta = Mathf.Min(0.001f, pathLength * 0.01f);
            if (i == 0) {
                Vector3 nextPos = this.curve.SampleBaked(offset + delta);
                forward = (nextPos - pos).Normalized();
            } else if (i == PathSegments) {
                Vector3 prevPos = this.curve.SampleBaked(offset - delta);
                forward = (pos - prevPos).Normalized();
            } else {
                Vector3 prevPos = this.curve.SampleBaked(offset - delta);
                Vector3 nextPos = this.curve.SampleBaked(offset + delta);
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
        this.mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < PathSegments; i++) {
            for (int j = 0; j < RadialSegments; j++) {
                int current = i * (RadialSegments + 1) + j;
                int next = current + RadialSegments + 1;
                this.mesh.SurfaceSetNormal(normals[current]);
                this.mesh.SurfaceAddVertex(vertices[current]);
                this.mesh.SurfaceSetNormal(normals[next]);
                this.mesh.SurfaceAddVertex(vertices[next]);
                this.mesh.SurfaceSetNormal(normals[current + 1]);
                this.mesh.SurfaceAddVertex(vertices[current + 1]);
                this.mesh.SurfaceSetNormal(normals[current + 1]);
                this.mesh.SurfaceAddVertex(vertices[current + 1]);
                this.mesh.SurfaceSetNormal(normals[next]);
                this.mesh.SurfaceAddVertex(vertices[next]);
                this.mesh.SurfaceSetNormal(normals[next + 1]);
                this.mesh.SurfaceAddVertex(vertices[next + 1]);
            }
        }
        this.mesh.SurfaceEnd();
    }

    public void OnHoverEnter() {
        if (this.material != null) {
            this.material.AlbedoColor = HoverColor;
        }
    }

    public void OnHoverExit() {
        if (this.material != null) {
            this.material.AlbedoColor = LineColor;
        }
    }

    public void Destroy() {
        QueueFree();
    }

    public void SetColor(Color color) {
        this.LineColor = color;
        if (this.material != null) {
            this.material.AlbedoColor = color;
        }
    }
}