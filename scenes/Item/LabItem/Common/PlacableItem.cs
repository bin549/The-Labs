using Godot;
using System;

public partial class PlacableItem : Node3D {
	[ExportGroup("物品属性")]
	[Export] public string ItemName { get; set; } = "实验物品";
	[Export] public string ItemType { get; set; } = "default";
	[Export] public Color ItemColor { get; set; } = Colors.White;
	[ExportGroup("拖拽设置")]
	[Export] public bool IsDraggable { get; set; } = true;
	[Export] public float DragHeight { get; set; } = 0.0f; 
	[Export] public bool KeepOriginalHeight { get; set; } = true;
	[ExportGroup("视觉设置")]
	[Export] public NodePath MeshPath { get; set; }
	[Export] public float HoverBrightness { get; set; } = 0.3f;
	[Export] public float DragBrightness { get; set; } = 0.5f;
	[ExportGroup("碰撞检测")]
	[Export] public NodePath CollisionAreaPath { get; set; }
	[Export] public bool AutoCreateCollisionArea { get; set; } = true;
	[Signal] public delegate void OnItemDragStartedEventHandler(PlacableItem item);
	[Signal] public delegate void OnItemDragEndedEventHandler(PlacableItem item);
	[Signal] public delegate void OnItemPlacedEventHandler(PlacableItem item, Vector3 position);
	[Signal] public delegate void OnItemOverlapStartedEventHandler(PlacableItem item, PlacableItem other);
	[Signal] public delegate void OnItemOverlapEndedEventHandler(PlacableItem item, PlacableItem other);
	protected bool isDragging = false;
	protected bool isHovered = false;
	protected Vector3 dragOffset;
	protected Camera3D camera;
	protected Vector3 originalPosition;
	protected MeshInstance3D mesh;
	protected StandardMaterial3D material;
	protected Color originalColor;
	protected Area3D collisionArea;
	protected CollisionShape3D collisionShape;
	private Godot.Collections.Array<PlacableItem> overlappingItems = new();
	
	public override void _Ready() {
		originalPosition = GlobalPosition;
		camera = GetViewport().GetCamera3D();
		ResolveMesh();
		InitializeMaterial();
		SetupCollisionArea();
	}
	
	public override void _Process(double delta) {
		if (isDragging && camera != null) {
			UpdateDragPosition();
		}
		UpdateVisuals();
	}
	
	public override void _Input(InputEvent @event) {
		if (!IsDraggable) return;
		if (@event is InputEventMouseButton mouseButton) {
			if (mouseButton.ButtonIndex == MouseButton.Left) {
				if (mouseButton.Pressed && isHovered && !isDragging) {
					StartDrag();
				} else if (!mouseButton.Pressed && isDragging) {
					EndDrag();
				}
			}
		}
	}
	
	private void StartDrag() {
		isDragging = true;
		if (camera != null) {
			var mousePos = GetViewport().GetMousePosition();
			var from = camera.ProjectRayOrigin(mousePos);
			var to = from + camera.ProjectRayNormal(mousePos) * 1000;
			var spaceState = GetWorld3D().DirectSpaceState;
			var query = PhysicsRayQueryParameters3D.Create(from, to);
			query.CollideWithAreas = true;
			var result = spaceState.IntersectRay(query);
			if (result.Count > 0 && result.ContainsKey("position")) {
				var hitPos = result["position"].AsVector3();
				dragOffset = GlobalPosition - hitPos;
			} else {
				dragOffset = Vector3.Zero;
			}
		}
		EmitSignal(SignalName.OnItemDragStarted, this);
		GD.Print($"[PlacableItem] 开始拖动：{ItemName}");
	}
	
	private void UpdateDragPosition() {
		var mousePos = GetViewport().GetMousePosition();
		var from = camera.ProjectRayOrigin(mousePos);
		var normal = camera.ProjectRayNormal(mousePos);
		float targetY = KeepOriginalHeight ? originalPosition.Y : DragHeight;
		float t = (targetY - from.Y) / normal.Y;
		if (t > 0) {
			Vector3 newPos = from + normal * t + dragOffset;
			if (KeepOriginalHeight) {
				newPos.Y = originalPosition.Y;
			} else {
				newPos.Y = DragHeight;
			}
			GlobalPosition = newPos;
		}
	}
	
	private void EndDrag() {
		isDragging = false;
		EmitSignal(SignalName.OnItemDragEnded, this);
		EmitSignal(SignalName.OnItemPlaced, this, GlobalPosition);
		GD.Print($"[PlacableItem] 放置物品：{ItemName} at {GlobalPosition}");
		CheckOverlappingPhenomena();
	}
	
	private void SetupCollisionArea() {
		if (!string.IsNullOrEmpty(CollisionAreaPath?.ToString())) {
			collisionArea = GetNodeOrNull<Area3D>(CollisionAreaPath);
		}
		if (collisionArea == null) {
			collisionArea = GetNodeOrNull<Area3D>("CollisionArea");
		}
		if (collisionArea != null) {
			collisionArea.AreaEntered += OnAreaEntered;
			collisionArea.AreaExited += OnAreaExited;
			collisionArea.MouseEntered += OnMouseEnter;
			collisionArea.MouseExited += OnMouseExit;
			collisionArea.CollisionLayer = 2;
			collisionArea.CollisionMask = 2; 
			collisionArea.Monitorable = true;
			collisionArea.Monitoring = true;
			collisionArea.InputRayPickable = true;
		}
	}
	
	private void OnAreaEntered(Area3D area) {
		var otherItem = area.GetParent() as PlacableItem;
		if (otherItem != null && otherItem != this) {
			if (!overlappingItems.Contains(otherItem)) {
				overlappingItems.Add(otherItem);
				EmitSignal(SignalName.OnItemOverlapStarted, this, otherItem);
				GD.Print($"[PlacableItem] {ItemName} 与 {otherItem.ItemName} 开始重叠");
			}
		}
	}
	
	private void OnAreaExited(Area3D area) {
		var otherItem = area.GetParent() as PlacableItem;
		if (otherItem != null && overlappingItems.Contains(otherItem)) {
			overlappingItems.Remove(otherItem);
			EmitSignal(SignalName.OnItemOverlapEnded, this, otherItem);
			GD.Print($"[PlacableItem] {ItemName} 与 {otherItem.ItemName} 结束重叠");
		}
	}
	
	private void CheckOverlappingPhenomena() {
		if (overlappingItems.Count > 0) {
			GD.Print($"[PlacableItem] {ItemName} 检测到 {overlappingItems.Count} 个重叠物品");
		}
	}
	
	private void OnMouseEnter() {
		if (!isDragging) {
			isHovered = true;
		}
	}
	
	private void OnMouseExit() {
		isHovered = false;
	}
	
	public void _OnArea3DMouseEntered() {
		OnMouseEnter();
	}
	
	public void _OnArea3DMouseExited() {
		OnMouseExit();
	}
	
	private void ResolveMesh() {
		if (!string.IsNullOrEmpty(MeshPath?.ToString())) {
			mesh = GetNodeOrNull<MeshInstance3D>(MeshPath);
		}
		if (mesh == null) {
			mesh = GetNodeOrNull<MeshInstance3D>("Mesh");
		}
		if (mesh == null) {
			mesh = FindChild("*", false, false) as MeshInstance3D;
		}
		if (mesh == null) {
			GD.PushWarning($"[PlacableItem] {Name}: 未找到 MeshInstance3D");
		}
	}
	
	private void InitializeMaterial() {
		if (mesh == null) return;
		if (mesh.GetActiveMaterial(0) is StandardMaterial3D existingMat) {
			material = (StandardMaterial3D)existingMat.Duplicate();
		} else {
			material = new StandardMaterial3D();
		}
		originalColor = ItemColor != Colors.White ? ItemColor : material.AlbedoColor;
		material.AlbedoColor = originalColor;
		mesh.SetSurfaceOverrideMaterial(0, material);
	}
	
	private void UpdateVisuals() {
		if (material == null) return;
		if (isHovered && !isDragging) {
			material.AlbedoColor = originalColor.Lightened(HoverBrightness);
			material.Emission = originalColor;
			material.EmissionEnergyMultiplier = 0.5f;
		} else if (isDragging) {
			material.AlbedoColor = originalColor.Lightened(DragBrightness);
			material.Emission = originalColor;
			material.EmissionEnergyMultiplier = 1.0f;
		} else {
			material.AlbedoColor = originalColor;
			material.Emission = Colors.Black;
			material.EmissionEnergyMultiplier = 0.0f;
		}
	}
	
	public void ResetPosition() {
		GlobalPosition = originalPosition;
		GD.Print($"[PlacableItem] 重置 {ItemName} 位置");
	}
	
	public Godot.Collections.Array<PlacableItem> GetOverlappingItems() {
		return new Godot.Collections.Array<PlacableItem>(overlappingItems);
	}
	
	public bool IsOverlappingWithType(string itemType) {
		foreach (var item in overlappingItems) {
			if (item.ItemType == itemType) {
				return true;
			}
		}
		return false;
	}
	
	public PlacableItem GetOverlappingItemByType(string itemType) {
		foreach (var item in overlappingItems) {
			if (item.ItemType == itemType) {
				return item;
			}
		}
		return null;
	}
}
