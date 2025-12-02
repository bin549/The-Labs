using Godot;
using System.Collections.Generic;

public partial class ConnectionManager : Node3D {
    [Export] public float MaxRaycastDistance { get; set; } = 100f;
    private ConnectableNode selectedNode = null;
    private List<IConnectionLine> connections = new List<IConnectionLine>();
    private IConnectionLine hoveredLine = null;
    private Camera3D camera;

    public override void _Ready() {
        this.camera = GetViewport().GetCamera3D();
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed) {
                this.HandleLeftClick(mouseEvent.Position);
            } else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed) {
                this.HandleRightClick(mouseEvent.Position);
            }
        }
        if (@event is InputEventMouseMotion motionEvent) {
            this.HandleMouseMotion(motionEvent.Position);
        }
    }

    private void HandleLeftClick(Vector2 mousePos) {
        var rayResult = PerformRaycast(mousePos);
        if (rayResult == null || rayResult.Count == 0 || !rayResult.ContainsKey("collider")) {
            GD.Print("射线未击中任何物体");
            if (this.selectedNode != null) {
                this.selectedNode.IsSelected = false;
                this.selectedNode = null;
                GD.Print("取消选择");
            }
            return;
        }
        var collider = rayResult["collider"].As<Node>();
        if (collider == null) {
            GD.Print("左键：collider 为 null");
            return;
        }
        GD.Print($"射线击中: {collider.Name} (类型: {collider.GetType().Name})");
        var clickedNode = FindConnectableNode(collider);
        if (clickedNode != null) {
            GD.Print($"找到 ConnectableNode: {clickedNode.Name}");
            OnNodeClicked(clickedNode);
        } else {
            GD.Print("未找到 ConnectableNode");
        }
    }

    private void HandleRightClick(Vector2 mousePos) {
        GD.Print("=== 右键点击检测 ===");
        var rayResult = PerformRaycastForLines(mousePos);
        if (rayResult == null || rayResult.Count == 0 || !rayResult.ContainsKey("collider")) {
            GD.Print("右键：射线未击中任何物体（21层）");
            return;
        }
        var collider = rayResult["collider"].As<Node>();
        if (collider == null) {
            GD.Print("右键：collider 为 null");
            return;
        }
        GD.Print($"右键击中: {collider.Name} (类型: {collider.GetType().Name})");
        GD.Print($"  - 节点路径: {collider.GetPath()}");
        var line = FindConnectionLine(collider);
        if (line != null) {
            GD.Print($"✓ 找到连线，准备删除");
            RemoveConnection(line);
        } else {
            GD.Print("✗ 未找到连线（FindConnectionLine 返回 null）");
            GD.Print($"当前共有 {this.connections.Count} 条连线");
            foreach (var conn in this.connections) {
                if (conn is Node3D node) {
                    GD.Print($"  - 连线: {node.Name}");
                }
            }
        }
    }

    private void HandleMouseMotion(Vector2 mousePos) {
        var rayResult = PerformRaycastForLines(mousePos);
        if (this.hoveredLine != null) {
            this.hoveredLine.OnHoverExit();
            this.hoveredLine = null;
        }
        if (rayResult == null || rayResult.Count == 0 || !rayResult.ContainsKey("collider")) {
            return;
        }
        var collider = rayResult["collider"].As<Node>();
        if (collider == null) return;
        var line = FindConnectionLine(collider);
        if (line != null) {
            this.hoveredLine = line;
            this.hoveredLine.OnHoverEnter();
        }
    }

    public void OnNodeClicked(ConnectableNode clickedNode) {
        if (this.selectedNode == null) {
            this.selectedNode = clickedNode;
            this.selectedNode.IsSelected = true;
            GD.Print($"✓ 选中节点: {this.selectedNode.Name}");
        } else if (this.selectedNode == clickedNode) {
            this.selectedNode.IsSelected = false;
            GD.Print($"✓ 取消选中: {this.selectedNode.Name}");
            this.selectedNode = null;
        } else {
            GD.Print($"✓ 创建连线: {this.selectedNode.Name} -> {clickedNode.Name}");
            this.CreateConnection(this.selectedNode, clickedNode);
            this.selectedNode.IsSelected = false;
            this.selectedNode = null;
        }
    }

    private void CreateConnection(ConnectableNode startNode, ConnectableNode endNode) {
        foreach (var conn in this.connections) {
            if ((conn.StartNode == startNode && conn.EndNode == endNode) ||
                (conn.StartNode == endNode && conn.EndNode == startNode)) {
                GD.Print("连线已存在");
                return;
            }
        }
        var line = new ConnectionLine_ImmediateMesh();
        line.LineRadius = 0.01f;
        line.LineColor = new Color(0.1f, 0.1f, 0.1f);
        line.HoverColor = new Color(0.8f, 0.2f, 0.2f);
        line.CableSlack = 0.15f;
        AddChild(line);
        line.Owner = GetTree().EditedSceneRoot;
        line.Initialize(startNode, endNode);
        this.connections.Add(line);
        GD.Print($"创建连线: {startNode.Name} -> {endNode.Name}");
    }

    private void RemoveConnection(IConnectionLine line) {
        this.connections.Remove(line);
        line.Destroy();
        GD.Print("删除连线");
    }

    private Godot.Collections.Dictionary PerformRaycast(Vector2 screenPos) {
        if (this.camera == null) {
            this.camera = GetViewport().GetCamera3D();
            if (this.camera == null) return null;
        }
        var from = this.camera.ProjectRayOrigin(screenPos);
        var to = from + this.camera.ProjectRayNormal(screenPos) * MaxRaycastDistance;
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        query.CollisionMask = 1 << 19;
        return spaceState.IntersectRay(query);
    }

    private Godot.Collections.Dictionary PerformRaycastForLines(Vector2 screenPos) {
        if (this.camera == null) {
            this.camera = GetViewport().GetCamera3D();
            if (this.camera == null) {
                GD.Print("  [射线检测] 相机为 null");
                return null;
            }
        }
        var from = this.camera.ProjectRayOrigin(screenPos);
        var to = from + this.camera.ProjectRayNormal(screenPos) * MaxRaycastDistance;
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        query.CollisionMask = 1 << 20;
        var result = spaceState.IntersectRay(query);
        if (result.Count > 0) {
            GD.Print($"  [射线检测] 击中物体在第21层");
        }
        return result;
    }

    private ConnectableNode FindConnectableNode(Node collider) {
        if (collider is ConnectableNode cn1) {
            GD.Print($"  -> 碰撞器本身是 ConnectableNode");
            return cn1;
        }
        Node current = collider;
        int depth = 0;
        while (current != null && depth < 10) {
            GD.Print($"  -> 检查父节点 {depth}: {current.Name} ({current.GetType().Name})");
            if (current is ConnectableNode connectableNode) {
                GD.Print($"  -> 在父节点层级 {depth} 找到 ConnectableNode");
                return connectableNode;
            }
            foreach (Node child in current.GetChildren()) {
                if (child is ConnectableNode cn) {
                    GD.Print($"  -> 在子节点中找到 ConnectableNode: {cn.Name}");
                    return cn;
                }
            }
            current = current.GetParent();
            depth++;
        }
        GD.Print($"  -> 未找到 ConnectableNode（检查了 {depth} 层）");
        return null;
    }

    private IConnectionLine FindConnectionLine(Node collider) {
        GD.Print("  查找连线:");
        Node current = collider;
        int depth = 0;
        while (current != null && depth < 10) {
            GD.Print($"    层级 {depth}: {current.Name} ({current.GetType().Name})");
            if (current is IConnectionLine line) {
                GD.Print($"    ✓ 找到连线: {current.Name}");
                return line;
            }
            if (current is ConnectionLine_ImmediateMesh) {
                GD.Print($"    ✓ 找到 ConnectionLine_ImmediateMesh: {current.Name}");
                return (IConnectionLine)current;
            }
            if (current is ConnectionLine) {
                GD.Print($"    ✓ 找到 ConnectionLine: {current.Name}");
                return (IConnectionLine)current;
            }
            current = current.GetParent();
            depth++;
        }
        GD.Print($"    ✗ 未找到连线（遍历了 {depth} 层）");
        return null;
    }
}