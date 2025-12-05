using Godot;

public partial class ConnectableNode : Node3D {
    [Export] public Color NormalColor { get; set; } = Colors.White;
    [Export] public Color SelectedColor { get; set; } = Colors.Yellow;
    [Export] public Color ConnectedColor { get; set; } = Colors.Green;
    private bool isSelected = false;
    private MeshInstance3D meshInstance;
    private StandardMaterial3D material;

    public bool IsSelected {
        get => this.isSelected;
        set {
            this.isSelected = value;
            UpdateColor();
        }
    }

    public override void _Ready() {
        this.EnsurePhysicsBody();
        this.SetupCollisionLayers();
        this.meshInstance = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        if (this.meshInstance == null) {
            foreach (var child in GetChildren()) {
                if (child is MeshInstance3D mesh) {
                    this.meshInstance = mesh;
                    break;
                }
            }
        }
        if (this.meshInstance == null) {
            this.meshInstance = new MeshInstance3D();
            var sphereMesh = new SphereMesh();
            sphereMesh.Radius = 0.15f;
            sphereMesh.Height = 0.3f;
            this.meshInstance.Mesh = sphereMesh;
            AddChild(this.meshInstance);
            if (GetTree()?.EditedSceneRoot != null)
                this.meshInstance.Owner = GetTree().EditedSceneRoot;
        }
        this.material = new StandardMaterial3D();
        this.material.AlbedoColor = NormalColor;
        if (this.meshInstance != null) {
            this.meshInstance.MaterialOverride = this.material;
        }
        GD.Print($"ConnectableNode {Name} 已初始化");
    }

    private void SetupCollisionLayers() {
        var staticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (staticBody != null) {
            staticBody.CollisionLayer = 1 << 19;
            staticBody.CollisionMask = 0;
            GD.Print($"{Name} 碰撞层已设置为 Layer 20");
        } else if (GetParent() is StaticBody3D parentBody) {
            parentBody.CollisionLayer = 1 << 19;
            parentBody.CollisionMask = 0;
            GD.Print($"{Name} 父节点碰撞层已设置为 Layer 20");
        }
    }

    private void EnsurePhysicsBody() {
        var staticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (staticBody != null) {
            var collisionShape = staticBody.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (collisionShape != null) {
                GD.Print($"{Name} 已有完整的物理体配置");
                return;
            } else {
                collisionShape = new CollisionShape3D();
                var shape = new BoxShape3D();
                shape.Size = new Vector3(0.3f, 0.3f, 0.3f);
                collisionShape.Shape = shape;
                staticBody.AddChild(collisionShape);
                if (GetTree()?.EditedSceneRoot != null)
                    collisionShape.Owner = GetTree().EditedSceneRoot;
                GD.Print($"为 {Name} 的 StaticBody3D 添加了碰撞形状");
                return;
            }
        }
        if (GetParent() is StaticBody3D || GetParent() is Area3D) {
            var collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (collisionShape == null) {
                collisionShape = new CollisionShape3D();
                var shape = new BoxShape3D();
                shape.Size = new Vector3(0.3f, 0.3f, 0.3f);
                collisionShape.Shape = shape;
                GetParent().AddChild(collisionShape);
                if (GetTree()?.EditedSceneRoot != null)
                    collisionShape.Owner = GetTree().EditedSceneRoot;
                GD.Print($"为 {Name} 的父节点添加了碰撞形状");
            }
        } else {
            staticBody = new StaticBody3D();
            staticBody.Name = "StaticBody3D";
            AddChild(staticBody);
            if (GetTree()?.EditedSceneRoot != null)
                staticBody.Owner = GetTree().EditedSceneRoot;
            var collisionShape = new CollisionShape3D();
            var shape = new BoxShape3D();
            shape.Size = new Vector3(0.3f, 0.3f, 0.3f);
            collisionShape.Shape = shape;
            staticBody.AddChild(collisionShape);
            if (GetTree()?.EditedSceneRoot != null)
                collisionShape.Owner = GetTree().EditedSceneRoot;
            GD.Print($"为 {Name} 创建了新的 StaticBody3D 和碰撞体");
        }
    }

    public void OnClicked() {
        var manager = GetTree().Root.GetNode<ConnectionManager>("World/ConnectionManager");
        if (manager != null) {
            manager.OnNodeClicked(this);
        }
    }

    public void UpdateColor() {
        if (this.material == null) return;
        if (this.isSelected) {
            this.material.AlbedoColor = SelectedColor;
        } else {
            this.material.AlbedoColor = NormalColor;
        }
    }

    public Vector3 GetConnectionPoint() {
        return GlobalPosition;
    }
}