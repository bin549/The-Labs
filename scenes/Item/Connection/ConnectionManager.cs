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
            if (this.selectedNode != null) {
                this.selectedNode.IsSelected = false;
                this.selectedNode = null;
            }
            return;
        }
        var collider = rayResult["collider"].As<Node>();
        if (collider == null) {
            return;
        }
        var clickedNode = this.FindConnectableNode(collider);
        if (clickedNode != null) {
            this.OnNodeClicked(clickedNode);
        } 
    }
    
    private void HandleRightClick(Vector2 mousePos) {
        var rayResult = PerformRaycastForLines(mousePos);
        if (rayResult == null || rayResult.Count == 0 || !rayResult.ContainsKey("collider")) {
            return;
        }
        var collider = rayResult["collider"].As<Node>();
        if (collider == null) {
            return;
        }
        var line = this.FindConnectionLine(collider);
        if (line != null) {
            this.RemoveConnection(line);
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
        var line = this.FindConnectionLine(collider);
        if (line != null) {
            this.hoveredLine = line;
            this.hoveredLine.OnHoverEnter();
        }
    }

    public void OnNodeClicked(ConnectableNode clickedNode) {
        if (this.selectedNode == null) {
            this.selectedNode = clickedNode;
            this.selectedNode.IsSelected = true;
        } else if (this.selectedNode == clickedNode) {
            this.selectedNode.IsSelected = false;
            this.selectedNode = null;
        } else {
            this.CreateConnection(this.selectedNode, clickedNode);
            this.selectedNode.IsSelected = false;
            this.selectedNode = null;
        }
    }

    private void CreateConnection(ConnectableNode startNode, ConnectableNode endNode) {
        foreach (var conn in this.connections) {
            if ((conn.StartNode == startNode && conn.EndNode == endNode) ||
                (conn.StartNode == endNode && conn.EndNode == startNode)) {
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
    }

    private void RemoveConnection(IConnectionLine line) {
        this.connections.Remove(line);
        line.Destroy();
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
        return result;
    }

    private ConnectableNode FindConnectableNode(Node collider) {
        if (collider is ConnectableNode cn1) {
            return cn1;
        }
        Node current = collider;
        int depth = 0;
        while (current != null && depth < 10) {
            if (current is ConnectableNode connectableNode) {
                return connectableNode;
            }
            foreach (Node child in current.GetChildren()) {
                if (child is ConnectableNode cn) {
                    return cn;
                }
            }
            current = current.GetParent();
            depth++;
        }
        return null;
    }

    private IConnectionLine FindConnectionLine(Node collider) {
        Node current = collider;
        int depth = 0;
        while (current != null && depth < 10) {
            if (current is IConnectionLine line) {
                return line;
            }
            current = current.GetParent();
            depth++;
        }
        return null;
    }
}