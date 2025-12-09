using Godot;
using Godot.Collections;

public partial class PlacableItem : Node3D {
	[Export] public bool IsDraggable { get; set; } = true;
	private bool isDragging = false;
	[Export] private DragPlaneType dragPlane1 = DragPlaneType.VerticalX;
	[Export] private DragPlaneType dragPlane2 = DragPlaneType.Horizontal;
	private DragPlaneType dragPlane = DragPlaneType.VerticalX;
	private Vector3 initialDragPosition;
	[Export] private MeshInstance3D outlineMesh;
	[Export] private Area3D collisionArea;
	private bool isHovered = false;

	public DragPlaneType DragPlane {
		get => dragPlane;
		set => dragPlane = value;
	}

	public override void _Ready() {
		base._Ready();
		this.dragPlane = this.dragPlane1;
		if (this.outlineMesh != null) {
			this.outlineMesh.Visible = false;
		}
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
		if (@event is InputEventMouseMotion motionEvent) {
			var intersect = this.GetMouseIntersect(motionEvent.Position);
			if (intersect != null && this.IsClickOnSelf(intersect)) {
				this.OnMouseEntered();
			} else {
				this.OnMouseExited();
			}
		}
		
		if (!this.IsDraggable) return;
		if (@event is InputEventKey keyEvent) {
			if (keyEvent.Keycode == Key.Shift && keyEvent.Pressed && !keyEvent.IsEcho()) {
				// 切换到另一个配置的DragPlane
				this.DragPlane = this.dragPlane == this.dragPlane1 ? this.dragPlane2 : this.dragPlane1;
			}
			if (keyEvent.Keycode == Key.Shift && !keyEvent.Pressed) {
				// 恢复为第一个配置的DragPlane
				this.DragPlane = this.dragPlane1;
			}
		}
		if (@event is InputEventMouseButton mouseButton) {
			bool leftButtonPressed = mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed;
			bool leftButtonReleased = mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed;
			if (leftButtonPressed) {
				var intersect = this.GetMouseIntersect(mouseButton.Position);
				if (intersect != null && this.IsClickOnSelf(intersect)) {
					this.isDragging = true;
					this.StartDrag(intersect);
					GetViewport().SetInputAsHandled();
				}
			} else if (leftButtonReleased && this.isDragging) {
				this.isDragging = false;
				this.EndDrag();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	
	public override void _Process(double delta) {
		if (this.isDragging && this.IsDraggable) {
			this.UpdateDragPosition();
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

	private void StartDrag(Dictionary intersect) {
		if (intersect == null || !intersect.ContainsKey("position")) {
			return;
		}
		this.initialDragPosition = this.GlobalPosition;
		Vector3 mousePosInPlane = CalculateMousePositionInPlane();
		this.GlobalPosition = mousePosInPlane;
		this.initialDragPosition = mousePosInPlane;
	}

	private void EndDrag() {
		this.initialDragPosition = Vector3.Zero;
	}

	private void UpdateDragPosition() {
		Vector3 mousePosInPlane = CalculateMousePositionInPlane();
		this.GlobalPosition = mousePosInPlane;
	}

	private Vector3 CalculateMousePositionInPlane() {
		var mousePos = GetViewport().GetMousePosition();
		var camera = GetViewport().GetCamera3D();
		if (camera == null) {
			return this.GlobalPosition;
		}
		var from = camera.ProjectRayOrigin(mousePos);
		var normal = camera.ProjectRayNormal(mousePos);
		Vector3 planePoint = this.GlobalPosition;
		Vector3 planeNormal = Vector3.Zero;
		switch (this.dragPlane) {
			case DragPlaneType.Horizontal:
				planeNormal = Vector3.Up;
				break;
			case DragPlaneType.VerticalX:
				planeNormal = Vector3.Right;
				break;
			case DragPlaneType.VerticalY:
				planeNormal = Vector3.Forward;
				break;
		}
		float denom = normal.Dot(planeNormal);
		if (Mathf.Abs(denom) < 0.0001f) {
			return planePoint;
		}
		float t = (planePoint - from).Dot(planeNormal) / denom;
		return from + normal * t;
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

public enum DragPlaneType {
	Horizontal,
	VerticalX,
	VerticalY,
}