using Godot;
using Godot.Collections;
using System;

public partial class DataBoard : Node3D {
    [Export] private MeshInstance3D outlineMesh;
    [Export] private Marker3D targetMarker;
    private bool isHovered = false;
    private bool hasMoved = false;
    private Vector3 initialPosition;
    private Vector3 initialRotationDegrees;
    private Vector3 initialScale;
    
    public override void _Ready() {
        base._Ready();
        if (this.outlineMesh != null) {
            this.outlineMesh.Visible = false;
        }
        this.initialPosition = this.GlobalPosition;
        this.initialRotationDegrees = this.RotationDegrees;
        this.initialScale = this.Scale;
    }
    
    private void OnMouseEntered() {
        if (this.isHovered) return;
        this.isHovered = true;
        if (this.outlineMesh != null) {
            this.outlineMesh.Visible = true;
        }
    }
    
    private void OnMouseExited() {
        if (!this.isHovered) return;
        this.isHovered = false;
        if (this.outlineMesh != null) {
            this.outlineMesh.Visible = false;
        }
    }
    
    public override void _Input(InputEvent @event) {
        if (!this.IsParentLabItemInteracting()) return;
        if (this.hasMoved && @event is InputEventKey keyEvent) {
            if (keyEvent.Keycode == Key.Escape && keyEvent.Pressed && !keyEvent.IsEcho()) {
                this.RestorePosition();
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        if (!this.hasMoved && @event is InputEventMouseMotion motionEvent) {
            var intersect = this.GetMouseIntersect(motionEvent.Position);
            if (intersect != null && this.IsClickOnSelf(intersect)) {
                this.OnMouseEntered();
            } else {
                this.OnMouseExited();
            }
        }
        if (@event is InputEventMouseButton mouseButton) {
            if (this.hasMoved && mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed && !mouseButton.IsEcho()) {
                this.RestorePosition();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed && !mouseButton.IsEcho()) {
                if (this.isHovered) {
                    var intersect = this.GetMouseIntersect(mouseButton.Position);
                    if (intersect != null && this.IsClickOnSelf(intersect)) {
                        this.MoveToMarker();
                        GetViewport().SetInputAsHandled();
                    }
                }
            }
        }
    }
    
    
    private bool IsParentLabItemInteracting() {
        Node current = GetParent();
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current is LabItem labItem) {
                return labItem.IsInteracting;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }
    
    private bool IsClickOnSelf(Dictionary intersect) {
        if (intersect == null || !intersect.ContainsKey("collider")) {
            return false;
        }
        var colliderVariant = intersect["collider"];
        var collider = colliderVariant.As<Node>();
        if (collider == null) {
            return false;
        }
        Node current = collider;
        int depth = 0;
        const int maxDepth = 20;
        while (current != null && depth < maxDepth) {
            if (current == this) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }
    
    private void MoveToMarker() {
        if (this.targetMarker != null && GodotObject.IsInstanceValid(this.targetMarker)) {
            this.GlobalPosition = this.targetMarker.GlobalPosition;
            this.RotationDegrees = new Vector3(-24, 0, 0);
            this.Scale = Vector3.One * 2f;
            this.hasMoved = true;
            this.OnMouseExited();
        }
    }
    
    private void RestorePosition() {
        this.GlobalPosition = this.initialPosition;
        this.RotationDegrees = this.initialRotationDegrees;
        this.Scale = this.initialScale;
        this.hasMoved = false;
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
        query.CollisionMask = 0xFFFFFFFF;
        var excludeList = new Godot.Collections.Array<Rid>();
        Node current = GetParent();
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current is LabItem labItem) {
                var staticBody = labItem.GetNodeOrNull<StaticBody3D>("StaticBody3D");
                if (staticBody != null) {
                    excludeList.Add(staticBody.GetRid());
                }
                break;
            }
            current = current.GetParent();
            depth++;
        }
        if (excludeList.Count > 0) {
            query.Exclude = excludeList;
        }
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        return result;
    }
}
