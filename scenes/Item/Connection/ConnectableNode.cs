using Godot;
using Godot.Collections;

public partial class ConnectableNode : Node3D {
    [Export] public Color SelectedColor { get; set; } = Colors.Yellow;
    [Export] public Color ConnectedColor { get; set; } = Colors.Green;
    
    // 不再使用 Export 属性，避免场景文件中的类型错误
    // 改为在 _Ready() 中自动查找子节点
    private MeshInstance3D meshInstance;
    private MeshInstance3D outlineMesh;
    
    private bool isSelected = false;
    private bool isHovered = false;
    private StandardMaterial3D material;
    private ConnectionManager connectionManager;

    public bool IsSelected {
        get => this.isSelected;
        set {
            this.isSelected = value;
            if (this.isSelected && this.isHovered) {
                this.isHovered = false;
                if (this.outlineMesh != null) {
                    this.outlineMesh.Visible = false;
                }
            }
            this.UpdateColor();
        }
    }

    public bool IsHovered {
        get => this.isHovered;
        set {
            this.isHovered = value;
        }
    }

    public override void _Ready() {
        // 自动查找 meshInstance：查找第一个 MeshInstance3D 子节点（排除 outlineMesh）
        if (this.meshInstance == null || !GodotObject.IsInstanceValid(this.meshInstance)) {
            foreach (Node child in GetChildren()) {
                if (child is MeshInstance3D mesh && child != this.outlineMesh) {
                    this.meshInstance = mesh;
                    break;
                }
            }
        }
        
        // 自动查找 outlineMesh：查找名为 "outline" 或包含 "outline" 的 MeshInstance3D 子节点
        if (this.outlineMesh == null || !GodotObject.IsInstanceValid(this.outlineMesh)) {
            foreach (Node child in GetChildren()) {
                if (child is MeshInstance3D mesh && 
                    (child.Name.ToString().ToLower().Contains("outline") || 
                     child.Name.ToString().ToLower().Contains("轮廓"))) {
                    this.outlineMesh = mesh;
                    break;
                }
            }
        }
        
        this.EnsurePhysicsBody();
        this.SetupCollisionLayers();
        if (this.outlineMesh != null) {
            this.outlineMesh.Visible = false;
        }
        this.ResolveConnectionManager();
    }

    private void ResolveConnectionManager() {
        if (this.connectionManager != null && GodotObject.IsInstanceValid(this.connectionManager)) return;
        this.connectionManager = GetTree().Root.GetNodeOrNull<ConnectionManager>("World/ConnectionManager");
        if (this.connectionManager == null) {
            this.connectionManager = GetTree().Root.FindChild("ConnectionManager", true, false) as ConnectionManager;
        }
    }

    private bool IsConnectionManagerEnabled() {
        this.ResolveConnectionManager();
        return this.connectionManager != null && this.connectionManager.IsEnabled;
    }

    private void SetupCollisionLayers() {
        var staticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (staticBody != null) {
            staticBody.CollisionLayer = 1 << 19;
            staticBody.CollisionMask = 0;
        } else if (GetParent() is StaticBody3D parentBody) {
            parentBody.CollisionLayer = 1 << 19;
            parentBody.CollisionMask = 0;
        }
    }

    private void EnsurePhysicsBody() {
        var staticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (staticBody != null) {
            var collisionShape = staticBody.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (collisionShape != null) {
                return;
            } 
            collisionShape = new CollisionShape3D();
            var shape = new BoxShape3D();
            shape.Size = new Vector3(0.3f, 0.3f, 0.3f);
            collisionShape.Shape = shape;
            staticBody.AddChild(collisionShape);
            if (GetTree()?.EditedSceneRoot != null)
                collisionShape.Owner = GetTree().EditedSceneRoot;
            return;
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
        }
    }

    public void OnClicked() {
        var manager = GetTree().Root.GetNode<ConnectionManager>("World/ConnectionManager");
        if (manager != null) {
            manager.OnNodeClicked(this);
        }
    }

    public void UpdateColor() {
        if (this.meshInstance == null) return;
        if (this.isSelected) {
            if (this.material == null) {
                this.material = new StandardMaterial3D();
            }
            this.material.AlbedoColor = this.SelectedColor;
            this.meshInstance.MaterialOverride = this.material;
        } else {
            this.meshInstance.MaterialOverride = null;
        }
    }

    public void OnHoverEnter() {
        this.IsHovered = true;
        if (this.outlineMesh != null && !this.isSelected) {
            this.outlineMesh.Visible = true;
        }
    }

    public void OnHoverExit() {
        this.IsHovered = false;
        if (this.outlineMesh != null) {
            this.outlineMesh.Visible = false;
        }
    }

    public override void _Input(InputEvent @event) {
        if (!this.IsConnectionManagerEnabled()) return;
        if (@event is InputEventMouseMotion motionEvent) {
            var intersect = this.GetMouseIntersect(motionEvent.Position);
            if (intersect != null && this.IsClickOnSelf(intersect)) {
                if (!this.isHovered && !this.isSelected) {
                    this.OnHoverEnter();
                }
            } else {
                if (this.isHovered) {
                    this.OnHoverExit();
                }
            }
        }
    }

    private Dictionary GetMouseIntersect(Vector2 mousePos) {
        var currentCamera = GetViewport().GetCamera3D();
        if (currentCamera == null) {
            return null;
        }
        var from = currentCamera.ProjectRayOrigin(mousePos);
        var to = from + currentCamera.ProjectRayNormal(mousePos) * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;
        query.CollideWithAreas = true;
        query.CollisionMask = 1 << 19;
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        return result;
    }

    private bool IsClickOnSelf(Dictionary intersect) {
        if (intersect == null || !intersect.ContainsKey("collider")) {
            return false;
        }
        var colliderVariant = intersect["collider"];
        var collider = colliderVariant.As<Node3D>();
        if (collider == null) {
            return false;
        }
        Node current = collider;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == this) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    public Vector3 GetConnectionPoint() {
        return GlobalPosition;
    }
}