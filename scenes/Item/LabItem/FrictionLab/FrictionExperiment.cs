using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
    [Export] public NodePath SurfacePlatformPath { get; set; }
    [Export] public Godot.Collections.Array<NodePath> DraggableObjectPaths { get; set; } = new();
    [ExportGroup("拖拽约束")] [Export] public DragPlaneType DragPlane { get; set; } = DragPlaneType.Horizontal;
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
        ResolveComponents();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) {
            return;
        }
        if (@event is InputEventMouseButton mouseButton) {
            string buttonName = mouseButton.ButtonIndex == MouseButton.Left ? "左键" :
                mouseButton.ButtonIndex == MouseButton.Right ? "右键" :
                mouseButton.ButtonIndex.ToString();
            bool leftButtonPressed = mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed;
            bool leftButtonReleased = mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed;
            bool rightButtonPressed = mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed;
            if (leftButtonPressed || rightButtonPressed) {
                string action = leftButtonPressed ? "拖拽" : "调试检测";
                var intersect = GetMouseIntersect(mouseButton.Position, rightButtonPressed);
                if (intersect != null && intersect.ContainsKey("position")) {
                    mousePosition = (Vector3)intersect["position"];
                } else {
                    var camera = GetViewport().GetCamera3D();
                    if (camera != null) {
                        var from = camera.ProjectRayOrigin(mouseButton.Position);
                        var normal = camera.ProjectRayNormal(mouseButton.Position);
                        mousePosition = from + normal * 5.0f;
                    }
                }
                if (leftButtonPressed) {
                    isDragging = true;
                    StartDrag(intersect);
                    GetViewport().SetInputAsHandled();
                } 
            } else if (leftButtonReleased) {
                isDragging = false;
                EndDrag();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (draggingObject != null && isDragging) {
            UpdateDragPosition();
        }
    }

    private void UpdateDragPosition() {
        if (draggingObject == null) return;
        Vector3 mousePosInPlane = CalculateMousePositionInPlane();
        Vector3 targetPosition = mousePosInPlane;
        targetPosition = ApplyPlaneConstraint(targetPosition, initialDragPosition);
        draggingObject.GlobalPosition = targetPosition;
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        foreach (var placableItem in placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.IsDraggable = false;
                FixPlacableItemCollisionArea(placableItem);
            }
        }
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void ExitInteraction() {
        foreach (var placableItem in placableItems) {
            if (GodotObject.IsInstanceValid(placableItem)) {
                placableItem.IsDraggable = true;
            }
        }
        if (draggingObject != null) {
            draggingObject = null;
        }
        isDragging = false;
        base.ExitInteraction();
    }

    private void ResolveComponents() {
        foreach (var path in DraggableObjectPaths) {
            var obj = GetNodeOrNull<Node3D>(path);
            if (obj != null) {
                draggableObjects.Add(obj);
                if (!obj.IsInGroup("moveable")) {
                    obj.AddToGroup("moveable");
                }
                if (obj is PlacableItem placableItem && !placableItems.Contains(placableItem)) {
                    placableItems.Add(placableItem);
                }
            }
        }
        if (draggableObjects.Count == 0) {
            FindDraggableObjects(this);
        }
    }

    private void FindDraggableObjects(Node parent) {
        foreach (Node child in parent.GetChildren()) {
            if (child is Node3D node3D) {
                if (child is PlacableItem placableItem) {
                    if (!placableItems.Contains(placableItem)) {
                        placableItems.Add(placableItem);
                    }
                    if (!node3D.IsInGroup("moveable")) {
                        node3D.AddToGroup("moveable");
                    }
                    FixPlacableItemCollisionArea(placableItem);
                }
                if (child.IsInGroup("moveable")) {
                    if (!draggableObjects.Contains(node3D)) {
                        draggableObjects.Add(node3D);
                    }
                }
            }
            FindDraggableObjects(child);
        }
    }

    private void FixPlacableItemCollisionArea(PlacableItem placableItem) {
        var collisionArea = placableItem.FindChild("CollisionArea", true, false) as Area3D;
        if (collisionArea == null) {
            foreach (Node child in placableItem.GetChildren()) {
                if (child is Area3D area) {
                    collisionArea = area;
                    break;
                }
            }
        }
        if (collisionArea != null) {
            collisionArea.InputRayPickable = true;
            collisionArea.Monitorable = true;
            collisionArea.Monitoring = true;
            if (collisionArea.CollisionLayer == 0) {
                collisionArea.CollisionLayer = 1;
            }
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
                if (draggableObjects.Contains(node3D)) {
                    draggableNode = node3D;
                    break;
                }
            }
            current = current.GetParent();
            depth++;
        }

        bool canMove = draggableNode != null;
        if (canMove && draggingObject == null && draggableNode != null) {
            draggingObject = draggableNode;
            Vector3 originalPos = draggingObject.GlobalPosition;
            initialDragPosition = originalPos;
            Vector3 mousePosInPlane = CalculateMousePositionInPlane();
            Vector3 targetPosition = ApplyPlaneConstraint(mousePosInPlane, originalPos);
            draggingObject.GlobalPosition = targetPosition;
            initialDragPosition = targetPosition;
            mousePosition = targetPosition;
            dragOffset = Vector3.Zero;
        } else if (!canMove) {
            Node node = collider;
            int showDepth = 0;
            while (node != null && showDepth < 5) {
                node = node.GetParent();
                showDepth++;
            }
        }
    }

    private void EndDrag() {
        if (draggingObject != null) {
            draggingObject = null;
            dragOffset = Vector3.Zero;
            initialDragPosition = Vector3.Zero;
        }
    }

    private Godot.Collections.Dictionary GetMouseIntersect(Vector2 mousePos, bool detailedDebug = false) {
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
        if (draggingObject != null) {
            if (draggingObject is CollisionObject3D collisionObj) {
                excludeList.Add(collisionObj.GetRid());
            } else {
                var collider = draggingObject.FindChild("*", true, false) as CollisionObject3D;
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
        if (!isDragging || detailedDebug) {
            if (result.Count > 0) {
                if (result.ContainsKey("position")) {
                    var pos = result["position"].AsVector3();
                }
            } else {
                if (detailedDebug) {
                    bool foundAny = false;
                    for (uint layer = 1; layer <= 32; layer++) {
                        var layerQuery = PhysicsRayQueryParameters3D.Create(from, to);
                        layerQuery.CollideWithBodies = true;
                        layerQuery.CollideWithAreas = true;
                        layerQuery.CollisionMask = 1u << (int)(layer - 1);
                    }
                    var viewport = GetViewport();
                    var viewportSize = viewport.GetVisibleRect().Size;
                    foreach (var obj in draggableObjects) {
                        if (!GodotObject.IsInstanceValid(obj)) continue;
                        var objScreenPos = currentCamera.UnprojectPosition(obj.GlobalPosition);
                        var screenDistance = mousePos.DistanceTo(objScreenPos);
                        var cameraToObject = obj.GlobalPosition - currentCamera.GlobalPosition;
                        var cameraForward = -currentCamera.GlobalTransform.Basis.Z;
                        float dot = cameraToObject.Normalized().Dot(cameraForward.Normalized());
                        bool isInFrontOfCamera = dot > 0;
                        bool isOnScreen = objScreenPos.X >= 0 && objScreenPos.X <= viewportSize.X &&
                                          objScreenPos.Y >= 0 && objScreenPos.Y <= viewportSize.Y &&
                                          isInFrontOfCamera;
                        if (screenDistance < 100 && isOnScreen) {
                            var directQuery = PhysicsRayQueryParameters3D.Create(
                                currentCamera.GlobalPosition,
                                obj.GlobalPosition
                            );
                            directQuery.CollideWithBodies = true;
                            directQuery.CollideWithAreas = true;
                            directQuery.CollisionMask = 0xFFFFFFFF;
                        }
                    }
                }
            }
        }
        return result;
    }

    private Vector3 CalculateMousePositionInPlane() {
        var mousePos = GetViewport().GetMousePosition();
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return draggingObject != null ? draggingObject.GlobalPosition : Vector3.Zero;
        var from = camera.ProjectRayOrigin(mousePos);
        var normal = camera.ProjectRayNormal(mousePos);
        Vector3 referencePoint = draggingObject != null ? draggingObject.GlobalPosition : initialDragPosition;
        var intersect = GetMouseIntersect(mousePos);
        Vector3 hitPosition = Vector3.Zero;
        bool hasHit = false;
        if (intersect != null && intersect.ContainsKey("position")) {
            hitPosition = (Vector3)intersect["position"];
            hasHit = true;
        }
        Vector3 planePoint = referencePoint;
        Vector3 planeNormal = Vector3.Zero;
        switch (DragPlane) {
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
            case DragPlaneType.VerticalZ:
                planeNormal = new Vector3(0, 0, 1);
                if (hasHit) {
                    hitPosition.Z = referencePoint.Z;
                    return hitPosition;
                }
                break;
            case DragPlaneType.Free:
                if (hasHit) {
                    return hitPosition;
                }
                float defaultDistance = 5.0f;
                return from + normal * defaultDistance;
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
        switch (DragPlane) {
            case DragPlaneType.Horizontal:
                targetPosition.Y = currentPosition.Y;
                break;
            case DragPlaneType.VerticalX:
                targetPosition.X = currentPosition.X;
                break;
            case DragPlaneType.VerticalZ:
                targetPosition.Z = currentPosition.Z;
                break;
            case DragPlaneType.Free:
                break;
        }
        return targetPosition;
    }
}

public enum DragPlaneType {
    Horizontal,
    VerticalX,
    VerticalZ,
    Free
}
