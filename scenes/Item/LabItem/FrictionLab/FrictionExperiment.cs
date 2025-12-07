using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
    [Export] public NodePath SurfacePlatformPath { get; set; }
    [Export] public Godot.Collections.Array<NodePath> DraggableObjectPaths { get; set; } = new();
    public DragPlaneType DragPlane { get; set; } = DragPlaneType.Horizontal;
    private Node3D draggingObject;
    private Vector3 mousePosition;
    private bool isDragging = false;
    private List<Node3D> draggableObjects = new();
    private List<PlacableItem> placableItems = new();
    private Node3D surfacePlatform;
    private Vector3 dragOffset;
    private Vector3 initialDragPosition;

    public override void _Ready() {
        base._Ready();
        this.ResolveComponents();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) {
            return;
        }
        if (@event is InputEventMouseButton mouseButton) {
            bool leftButtonPressed = mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed;
            bool leftButtonReleased = mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed;
            if (leftButtonPressed) {
                var intersect = this.GetMouseIntersect(mouseButton.Position);
                if (intersect != null && intersect.ContainsKey("position")) {
                    this.mousePosition = (Vector3)intersect["position"];
                } else {
                    var camera = GetViewport().GetCamera3D();
                    if (camera != null) {
                        var from = camera.ProjectRayOrigin(mouseButton.Position);
                        var normal = camera.ProjectRayNormal(mouseButton.Position);
                        this.mousePosition = from + normal * 5.0f;
                    }
                }
                if (leftButtonPressed) {
                    this.isDragging = true;
                    this.StartDrag(intersect);
                    GetViewport().SetInputAsHandled();
                } 
            } else if (leftButtonReleased) {
                this.isDragging = false;
                this.EndDrag();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (this.draggingObject != null && this.isDragging) {
            this.UpdateDragPosition();
        }
    }

    private void UpdateDragPosition() {
        if (this.draggingObject == null) return;
        Vector3 mousePosInPlane = this.CalculateMousePositionInPlane();
        Vector3 targetPosition = mousePosInPlane;
        targetPosition = this.ApplyPlaneConstraint(targetPosition, this.initialDragPosition);
        this.draggingObject.GlobalPosition = targetPosition;
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        foreach (var placableItem in this.placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.IsDraggable = false;
            }
        }
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void ExitInteraction() {
        foreach (var placableItem in this.placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.IsDraggable = true;
            }
        }
        if (this.draggingObject != null) {
            this.draggingObject = null;
        }
        this.isDragging = false;
        base.ExitInteraction();
    }

    private void ResolveComponents() {
        foreach (var path in DraggableObjectPaths) {
            var obj = GetNodeOrNull<Node3D>(path);
            if (obj != null) {
                this.draggableObjects.Add(obj);
                if (!obj.IsInGroup("moveable")) {
                    obj.AddToGroup("moveable");
                }
                if (obj is PlacableItem placableItem && !this.placableItems.Contains(placableItem)) {
                    this.placableItems.Add(placableItem);
                }
            }
        }
        if (this.draggableObjects.Count == 0) {
            FindDraggableObjects(this);
        }
    }

    private void FindDraggableObjects(Node parent) {
        foreach (Node child in parent.GetChildren()) {
            if (child is Node3D node3D) {
                if (child is PlacableItem placableItem) {
                    if (!this.placableItems.Contains(placableItem)) {
                        this.placableItems.Add(placableItem);
                    }
                    if (!node3D.IsInGroup("moveable")) {
                        node3D.AddToGroup("moveable");
                    }
                }
                if (child.IsInGroup("moveable")) {
                    if (!this.draggableObjects.Contains(node3D)) {
                        this.draggableObjects.Add(node3D);
                    }
                }
            }
            this.FindDraggableObjects(child);
        }
    }

    private void StartDrag(Godot.Collections.Dictionary intersect) {
        if (intersect == null || intersect.Count == 0) {
            return;
        }
        if (!intersect.ContainsKey("collider")) {
            return;
        }
        var colliderVariant = intersect["collider"];
        var collider = colliderVariant.As<Node3D>();
        if (collider == null) {
            return;
        }
        if (collider.Name == "StaticBody3D") {
            Node parent = collider.GetParent();
            if (parent == this || (parent != null && parent.Name == "FrictionLabItem")) {
                return;
            }
        }
        Node3D draggableNode = null;
        Node current = collider;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current is Node3D node3D) {
                if (current is PlacableItem placableItem) {
                    draggableNode = node3D;
                    break;
                }
                if (current.IsInGroup("moveable")) {
                    draggableNode = node3D;
                    break;
                }
                if (this.draggableObjects.Contains(node3D)) {
                    draggableNode = node3D;
                    break;
                }
            }
            current = current.GetParent();
            depth++;
        }
        bool canMove = draggableNode != null;
        if (canMove && this.draggingObject == null && draggableNode != null) {
            this.draggingObject = draggableNode;
            Vector3 originalPos = this.draggingObject.GlobalPosition;
            this.initialDragPosition = originalPos;
            Vector3 mousePosInPlane = CalculateMousePositionInPlane();
            Vector3 targetPosition = ApplyPlaneConstraint(mousePosInPlane, originalPos);
            this.draggingObject.GlobalPosition = targetPosition;
            this.initialDragPosition = targetPosition;
            this.mousePosition = targetPosition;
            this.dragOffset = Vector3.Zero;
        } 
    }

    private void EndDrag() {
        if (this.draggingObject != null) {
            this.draggingObject = null;
            this.dragOffset = Vector3.Zero;
            this.initialDragPosition = Vector3.Zero;
        }
    }

    private Godot.Collections.Dictionary GetMouseIntersect(Vector2 mousePos) {
        var currentCamera = GetViewport().GetCamera3D();
        if (currentCamera == null) {
            return null;
        }
        var from = currentCamera.ProjectRayOrigin(mousePos);
        var to = from + currentCamera.ProjectRayNormal(mousePos) * 1000f;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithBodies = true;
        query.CollideWithAreas = true;
        query.CollisionMask = 0xFFFFFFFF;
        var excludeList = new Godot.Collections.Array<Rid>();
        if (this.draggingObject != null) {
            if (this.draggingObject is CollisionObject3D collisionObj) {
                excludeList.Add(collisionObj.GetRid());
            } else {
                var collider = this.draggingObject.FindChild("*", true, false) as CollisionObject3D;
                if (collider != null) {
                    excludeList.Add(collider.GetRid());
                }
            }
        }
        var staticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (staticBody != null) {
            excludeList.Add(staticBody.GetRid());
        }
        if (excludeList.Count > 0) {
            query.Exclude = excludeList;
        }
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        return result;
    }

    private Vector3 CalculateMousePositionInPlane() {
        var mousePos = GetViewport().GetMousePosition();
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return this.draggingObject != null ? this.draggingObject.GlobalPosition : Vector3.Zero;
        var from = camera.ProjectRayOrigin(mousePos);
        var normal = camera.ProjectRayNormal(mousePos);
        Vector3 referencePoint = this.draggingObject != null ? this.draggingObject.GlobalPosition : this.initialDragPosition;
        var intersect = GetMouseIntersect(mousePos);
        Vector3 hitPosition = Vector3.Zero;
        bool hasHit = false;
        if (intersect != null && intersect.ContainsKey("position")) {
            hitPosition = (Vector3)intersect["position"];
            hasHit = true;
        }
        Vector3 planePoint = referencePoint;
        Vector3 planeNormal = Vector3.Zero;
        switch (this.DragPlane) {
            case DragPlaneType.Horizontal:
                planeNormal = Vector3.Up;
                if (hasHit) {
                    hitPosition.Y = referencePoint.Y;
                    return hitPosition;
                }
                break;
            case DragPlaneType.VerticalX:
                planeNormal = Vector3.Right;
                if (hasHit) {
                    hitPosition.X = referencePoint.X;
                    return hitPosition;
                }
                break;
        }
        float denom = normal.Dot(planeNormal);
        if (Mathf.Abs(denom) > 0.0001f) {
            float t = (planePoint - from).Dot(planeNormal) / denom;
            if (t > 0) {
                return from + normal * t;
            } else {
                return from + normal * 5.0f;
            }
        } else {
            return from + normal * 5.0f;
        }
    }

    private Vector3 ApplyPlaneConstraint(Vector3 targetPosition, Vector3 currentPosition) {
        switch (this.DragPlane) {
            case DragPlaneType.Horizontal:
                targetPosition.Y = currentPosition.Y;
                break;
            case DragPlaneType.VerticalX:
                targetPosition.X = currentPosition.X;
                break;
        }
        return targetPosition;
    }
}

public enum DragPlaneType {
    Horizontal,
    VerticalX,
}
