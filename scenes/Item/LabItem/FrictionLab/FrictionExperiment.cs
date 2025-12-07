using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
    public DragPlaneType DragPlane { get; set; } = DragPlaneType.Horizontal;
    private Node3D draggingObject;
    private Vector3 mousePosition;
    private bool isDragging = false;
    private List<PlacableItem> placableItems = new();
    private Node3D surfacePlatform;
    private Vector3 dragOffset;
    private Vector3 initialDragPosition;

    public override void _Ready() {
        base._Ready();
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
                } 
                this.isDragging = true;
                this.StartDrag(intersect);
                GetViewport().SetInputAsHandled();
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
    
    private void StartDrag(Godot.Collections.Dictionary intersect) {
        if (intersect == null || intersect.Count == 0) {
            return;
        }
        if (!intersect.ContainsKey("collider")) {
            return;
        }
        var colliderVariant = intersect["collider"];
        var collider = colliderVariant.As<Node3D>();
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
            this.draggingObject.GlobalPosition = mousePosInPlane;
            this.initialDragPosition = mousePosInPlane;
            this.mousePosition = mousePosInPlane;
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
        Vector3 planePoint = referencePoint;
        Vector3 planeNormal = Vector3.Zero;
        switch (this.DragPlane) {
            case DragPlaneType.Horizontal:
                planeNormal = Vector3.Up;
                break;
            case DragPlaneType.VerticalX:
                planeNormal = Vector3.Right;
                break;
        }
        float denom = normal.Dot(planeNormal);
        float t = (planePoint - from).Dot(planeNormal) / denom;
        return from + normal * t;
    }
}

public enum DragPlaneType {
    Horizontal,
    VerticalX,
}
