using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
	[Export] public NodePath SurfacePlatformPath { get; set; }
	[Export] public Godot.Collections.Array<NodePath> DraggableObjectPaths { get; set; } = new();
	[Export] public float GridSize { get; set; } = 0.5f;
	[Export] public bool EnableGridSnapping { get; set; } = false; // 默认禁用网格吸附
	[ExportGroup("拖拽约束")]
	[Export] public DragPlaneType DragPlane { get; set; } = DragPlaneType.Horizontal; // 默认水平移动 
	private Node3D draggingObject; 
	private Vector3 mousePosition; 
	private bool isDragging = false; 
	private List<Node3D> draggableObjects = new(); 
	private List<PlacableItem> placableItems = new(); // PlacableItem 列表（用于禁用其拖拽）
	private Node3D surfacePlatform; 
	private Vector3 dragOffset; 
	private Vector3 initialDragPosition; // 开始拖拽时的初始位置（用于平面约束）
	private Label infoLabel;

	public override void _Ready() {
		base._Ready();
		ResolveComponents();
		CreateSimpleUI();
		GD.Print("========================================");
		GD.Print($"[FrictionExperiment] 初始化完成");
		GD.Print($"[FrictionExperiment] 可拖拽物体数量: {draggableObjects.Count}");
		GD.Print($"[FrictionExperiment] PlacableItem 数量: {placableItems.Count}");
		GD.Print("========================================");
		GD.Print("[FrictionExperiment] ⚠️ 重要提示：");
		GD.Print("[FrictionExperiment] 1. 请走到 FrictionExperiment 物体附近");
		GD.Print("[FrictionExperiment] 2. 用鼠标对准物体（会显示高亮和 [E] 提示）");
		GD.Print("[FrictionExperiment] 3. 然后按 E 键进入交互模式");
		GD.Print("[FrictionExperiment] 4. 进入交互模式后才能点击拖拽物体");
		GD.Print("========================================");
	}

	public override void _Input(InputEvent @event) {
		// 参考 AluminumReactionExperiment：只在交互模式下处理输入
		if (!base.isInteracting) {
			// 非交互模式下，不拦截任何输入，让 GameManager 处理 ESC 键显示暂停菜单
			return;
		}
		
		// 交互模式下：处理 ESC 键退出交互（参考 AluminumReactionExperiment）
		if (@event.IsActionPressed("pause") || @event.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			ExitInteraction();
			return;
		}
		
		// 处理 G 键切换网格吸附
		if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
			if (keyEvent.Keycode == Key.G) {
				EnableGridSnapping = !EnableGridSnapping;
				GD.Print($"[FrictionExperiment] 网格吸附: {(EnableGridSnapping ? "已启用" : "已禁用")}");
				if (infoLabel != null) {
					string status = EnableGridSnapping ? "已启用" : "已禁用";
					infoLabel.Text = $"网格吸附: {status}\n按 G 键切换";
					// 2秒后恢复原文本
					GetTree().CreateTimer(2.0f).Timeout += () => {
						if (infoLabel != null && draggingObject == null) {
							infoLabel.Text = "✅ 交互模式已激活\n\n点击并拖拽物体到实验平台上\n按ESC退出交互模式";
						}
					};
				}
				GetViewport().SetInputAsHandled();
				return;
			}
		}
		
		// 处理鼠标按键事件（支持左键和右键）
		if (@event is InputEventMouseButton mouseButton) {
			string buttonName = mouseButton.ButtonIndex == MouseButton.Left ? "左键" : 
			                   mouseButton.ButtonIndex == MouseButton.Right ? "右键" : 
			                   mouseButton.ButtonIndex.ToString();
			
			// 只处理左键拖拽，右键用于调试
			bool leftButtonPressed = mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed;
			bool leftButtonReleased = mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed;
			bool rightButtonPressed = mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed;
			
			if (leftButtonPressed || rightButtonPressed) {
				string action = leftButtonPressed ? "拖拽" : "调试检测";
				if (rightButtonPressed) {
					GD.Print("========================================");
					GD.Print("=== 右键点击检测 ===");
					GD.Print("========================================");
				}
				GD.Print($"[FrictionExperiment] 鼠标{buttonName}按下，开始{action}");
				
				var intersect = GetMouseIntersect(mouseButton.Position, rightButtonPressed);
				
				// 计算鼠标位置（优先使用射线检测结果，否则计算平面投影）
				if (intersect != null && intersect.ContainsKey("position")) {
					mousePosition = (Vector3)intersect["position"];
				} else {
					// 如果射线未命中，计算鼠标在平面上的位置
					// 注意：此时 draggingObject 可能还没设置，所以使用临时方法
					var camera = GetViewport().GetCamera3D();
					if (camera != null) {
						var from = camera.ProjectRayOrigin(mouseButton.Position);
						var normal = camera.ProjectRayNormal(mouseButton.Position);
						// 使用默认距离作为临时位置
						mousePosition = from + normal * 5.0f;
					}
				}
				
				if (leftButtonPressed) {
					isDragging = true;
					StartDrag(intersect);
					GetViewport().SetInputAsHandled();
				} else if (rightButtonPressed) {
					// 右键仅用于调试，不做拖拽
					GD.Print("========================================");
					GD.Print($"[FrictionExperiment] 右键点击检测完成");
					GD.Print("========================================");
				}
			} else if (leftButtonReleased) {
				GD.Print("[FrictionExperiment] 鼠标左键释放");
				isDragging = false;
				EndDrag();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	public override void _Process(double delta) {
		base._Process(delta); 
		
		// 如果正在拖拽，持续更新鼠标位置和物体位置
		if (draggingObject != null && isDragging) {
			UpdateDragPosition();
		}
	}
	
	/// <summary>
	/// 更新拖拽位置，让物体跟随鼠标
	/// </summary>
	private void UpdateDragPosition() {
		if (draggingObject == null) return;
		
		// 计算鼠标在平面上的位置（统一方法，确保一致）
		Vector3 mousePosInPlane = CalculateMousePositionInPlane();
		
		// 直接使用鼠标位置，让物体跟随鼠标（无偏移）
		Vector3 targetPosition = mousePosInPlane;
		
		// 应用平面约束（基于开始拖拽时的初始位置）
		targetPosition = ApplyPlaneConstraint(targetPosition, initialDragPosition);
		
		// 网格吸附（默认禁用，可通过编辑器或运行时切换）
		if (EnableGridSnapping) {
			targetPosition = SnapToGrid(targetPosition);
		}
		
		draggingObject.GlobalPosition = targetPosition;
		
		if (infoLabel != null) {
			string gridInfo = EnableGridSnapping ? " [网格吸附]" : "";
			infoLabel.Text = $"拖拽中: {draggingObject.Name}\n位置: {targetPosition}\n平面: {DragPlane}{gridInfo}";
		}
	}	

	public override void EnterInteraction() {
		GD.Print("[FrictionExperiment] EnterInteraction 被调用");
		
		base.EnterInteraction();
		
		// 重要：手动设置交互状态（基类不会自动设置）
		base.isInteracting = true;
		
		GD.Print("========================================");
		GD.Print("[FrictionExperiment] ✓✓✓ 已进入交互模式 ✓✓✓");
		GD.Print($"[FrictionExperiment] 交互状态已设置: isInteracting={base.isInteracting}");
		GD.Print("========================================");
		
		// 禁用 PlacableItem 自己的拖拽系统，让 FrictionExperiment 统一管理
		foreach (var placableItem in placableItems) {
			if (GodotObject.IsInstanceValid(placableItem)) {
				placableItem.IsDraggable = false;
				GD.Print($"[FrictionExperiment] 已禁用 {placableItem.Name} 的 PlacableItem 拖拽系统");
				// 确保 CollisionArea 设置正确
				FixPlacableItemCollisionArea(placableItem);
			}
		}
		
		// 确保鼠标模式正确
		if (Input.MouseMode != Input.MouseModeEnum.Visible) {
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GD.Print("[FrictionExperiment] 已设置鼠标模式为 Visible");
		}
		
		if (infoLabel != null) {
			infoLabel.Visible = true;
			string gridStatus = EnableGridSnapping ? "已启用" : "已禁用";
			infoLabel.Text = $"✅ 交互模式已激活\n\n点击并拖拽物体到实验平台上\n按 G 键切换网格吸附（当前: {gridStatus}）\n按ESC退出交互模式";
			GD.Print("[FrictionExperiment] UI标签已显示");
		}
		
		// 检查是否有阻挡鼠标的 UI
		CheckBlockingUI();
		
		GD.Print($"[FrictionExperiment] 交互状态检查: isInteracting={base.isInteracting}");
		GD.Print($"[FrictionExperiment] 可拖拽物体数量: {draggableObjects.Count}");
		GD.Print($"[FrictionExperiment] PlacableItem 数量: {placableItems.Count}");
		GD.Print($"[FrictionExperiment] 鼠标模式: {Input.MouseMode}");
		
		// 检查当前相机
		var camera = GetViewport().GetCamera3D();
		if (camera != null) {
			GD.Print($"[FrictionExperiment] 当前相机: {camera.Name} (类型: {camera.GetType().Name})");
			GD.Print($"[FrictionExperiment] 相机位置: {camera.GlobalPosition}");
			GD.Print($"[FrictionExperiment] 相机投影: {camera.Projection}");
		} else {
			GD.PushWarning("[FrictionExperiment] 警告：未找到相机！");
		}
		
		GD.Print("[FrictionExperiment] 现在可以点击并拖拽物体了！");
		GD.Print("[FrictionExperiment] 提示：右键点击可查看详细调试信息");
		GD.Print("========================================");
	}
	
	/// <summary>
	/// 检查并报告可能阻挡鼠标的 UI 元素
	/// </summary>
	private void CheckBlockingUI() {
		var player = GetTree().Root.FindChild("Player", true, false);
		if (player != null) {
			var canvasLayer = player.FindChild("CanvasLayer", false, false);
			if (canvasLayer != null) {
				CheckUIChildren(canvasLayer);
			}
		}
	}
	
	/// <summary>
	/// 递归检查并修复阻挡鼠标的 UI 子节点
	/// </summary>
	private void CheckUIChildren(Node parent, int depth = 0, bool autoFix = true) {
		foreach (Node child in parent.GetChildren()) {
			if (child is Control control) {
				string indent = new string(' ', depth * 2);
				GD.Print($"{indent}[FrictionExperiment] UI节点: {control.Name}, Visible: {control.Visible}, MouseFilter: {control.MouseFilter}");
				
				// 如果 UI 可见且 MouseFilter 是 Stop，可能会阻挡鼠标事件
				if (control.Visible && control.MouseFilter == Control.MouseFilterEnum.Stop) {
					var rect = control.GetRect();
					var viewport = GetViewport();
					bool isLarge = false;
					
					if (viewport != null) {
						var viewportSize = viewport.GetVisibleRect().Size;
						isLarge = rect.Size.X >= viewportSize.X * 0.5f || rect.Size.Y >= viewportSize.Y * 0.5f;
						
						if (isLarge) {
							GD.PushWarning($"[FrictionExperiment] 警告: {control.Name} 可能是全屏 UI (大小: {rect.Size})，可能会阻挡鼠标事件！");
							
							// 自动修复：如果不是关键 UI（如按钮），设置为 Pass 以允许鼠标事件穿透
							if (autoFix && !(control is Button)) {
								control.MouseFilter = Control.MouseFilterEnum.Pass;
								GD.Print($"{indent}[FrictionExperiment] ✓ 已修复 {control.Name} 的 MouseFilter 为 Pass");
							}
						}
					}
					
					// 对于 RichTextLabel 等非交互式 UI，设置为 Ignore
					if (autoFix && (control is RichTextLabel || control is Label)) {
						control.MouseFilter = Control.MouseFilterEnum.Ignore;
						GD.Print($"{indent}[FrictionExperiment] ✓ 已修复 {control.Name} 的 MouseFilter 为 Ignore（文本标签）");
					}
				}
			}
			if (depth < 5) { // 增加递归深度以检查更多子节点
				CheckUIChildren(child, depth + 1, autoFix);
			}
		}
	}

	public override void ExitInteraction() {
		// 恢复 PlacableItem 的拖拽系统
		foreach (var placableItem in placableItems) {
			if (GodotObject.IsInstanceValid(placableItem)) {
				placableItem.IsDraggable = true;
			}
		}
		
		if (infoLabel != null) {
			infoLabel.Visible = false;
		}
		if (draggingObject != null) {
			draggingObject = null;
		}
		isDragging = false;
		
		// 最后调用基类方法（参考 AluminumReactionExperiment）
		base.ExitInteraction();
	}

	private void ResolveComponents() {
		if (!string.IsNullOrEmpty(SurfacePlatformPath?.ToString())) {
			surfacePlatform = GetNodeOrNull<Node3D>(SurfacePlatformPath);
			if (surfacePlatform != null) {
				GD.Print($"[FrictionExperiment] 找到实验平台: {surfacePlatform.Name}");
			}
		}
		foreach (var path in DraggableObjectPaths) {
			var obj = GetNodeOrNull<Node3D>(path);
			if (obj != null) {
				draggableObjects.Add(obj);
				if (!obj.IsInGroup("moveable")) {
					obj.AddToGroup("moveable");
				}
				// 如果是 PlacableItem，也添加到列表
				if (obj is PlacableItem placableItem && !placableItems.Contains(placableItem)) {
					placableItems.Add(placableItem);
				}
				GD.Print($"[FrictionExperiment] 添加可拖拽物体: {obj.Name}");
			}
		}
		if (draggableObjects.Count == 0) {
			GD.PushWarning("[FrictionExperiment] 未配置可拖拽物体路径，尝试自动查找...");
			FindDraggableObjects(this);
		}
	}

	private void FindDraggableObjects(Node parent) {
		foreach (Node child in parent.GetChildren()) {
			if (child is Node3D node3D) {
				// 检查是否是 PlacableItem 类型
				if (child is PlacableItem placableItem) {
					// 保存 PlacableItem 引用
					if (!placableItems.Contains(placableItem)) {
						placableItems.Add(placableItem);
						GD.Print($"[FrictionExperiment] ✓ 找到 PlacableItem: {node3D.Name}");
					}
					// 自动添加到 moveable 组
					if (!node3D.IsInGroup("moveable")) {
						node3D.AddToGroup("moveable");
						GD.Print($"[FrictionExperiment] ✓ 自动将 PlacableItem {node3D.Name} 添加到 moveable 组");
					}
					// 确保 CollisionArea 设置正确
					FixPlacableItemCollisionArea(placableItem);
				}
				
				// 添加到可拖拽列表
				if (child.IsInGroup("moveable")) {
					if (!draggableObjects.Contains(node3D)) {
						draggableObjects.Add(node3D);
						GD.Print($"[FrictionExperiment] ✓ 自动找到可拖拽物体: {node3D.Name} (类型: {node3D.GetType().Name})");
					}
				}
			}
			FindDraggableObjects(child);
		}
		if (parent == this) {
			GD.Print($"[FrictionExperiment] 总共找到 {draggableObjects.Count} 个可拖拽物体，{placableItems.Count} 个 PlacableItem");
			foreach (var obj in draggableObjects) {
				GD.Print($"  - {obj.Name} (路径: {obj.GetPath()})");
			}
		}
	}
	
	/// <summary>
	/// 修复 PlacableItem 的 CollisionArea 设置，确保可以被射线检测到
	/// </summary>
	private void FixPlacableItemCollisionArea(PlacableItem placableItem) {
		// 查找 CollisionArea
		var collisionArea = placableItem.FindChild("CollisionArea", true, false) as Area3D;
		if (collisionArea == null) {
			// 尝试查找子节点中的 Area3D
			foreach (Node child in placableItem.GetChildren()) {
				if (child is Area3D area) {
					collisionArea = area;
					break;
				}
			}
		}
		
		if (collisionArea != null) {
			// 确保设置正确
			collisionArea.InputRayPickable = true;
			collisionArea.Monitorable = true;
			collisionArea.Monitoring = true;
			
			// 设置碰撞层，确保可以被检测到（使用层1，因为这是最常用的）
			if (collisionArea.CollisionLayer == 0) {
				collisionArea.CollisionLayer = 1; // 设置为层1
				GD.Print($"[FrictionExperiment] 修复 {placableItem.Name} 的 CollisionArea 碰撞层为 1");
			}
			
			GD.Print($"[FrictionExperiment] {placableItem.Name} CollisionArea 设置: Layer={collisionArea.CollisionLayer}, InputRayPickable={collisionArea.InputRayPickable}");
		} else {
			GD.PushWarning($"[FrictionExperiment] {placableItem.Name} 未找到 CollisionArea！");
		}
	}

	private void CreateSimpleUI() {
		infoLabel = new Label();
		infoLabel.Name = "InfoLabel";
		infoLabel.Position = new Vector2(20, 20);
		infoLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		infoLabel.AddThemeFontSizeOverride("font_size", 20);
		infoLabel.Visible = false;
		// 关键：设置为 Ignore，这样 UI 不会阻挡鼠标事件传递给 3D 场景
		infoLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		var player = GetTree().Root.FindChild("Player", true, false);
		if (player != null) {
			var canvasLayer = player.FindChild("CanvasLayer", false, false);
			if (canvasLayer != null) {
				canvasLayer.AddChild(infoLabel);
				GD.Print("[FrictionExperiment] UI创建成功 (MouseFilter: Ignore)");
			}
		}
	}

	private void StartDrag(Godot.Collections.Dictionary intersect) {
		// 保存点击时的鼠标位置（用于立即移动物体）
		Vector3 clickPosition = Vector3.Zero;
		if (intersect != null && intersect.ContainsKey("position")) {
			clickPosition = (Vector3)intersect["position"];
		}
		
		if (intersect == null || intersect.Count == 0) {
			GD.Print("[FrictionExperiment] 射线没有检测到任何物体");
			return;
		}
		if (!intersect.ContainsKey("collider")) {
			GD.Print("[FrictionExperiment] 射线检测结果中没有 collider 字段");
			return;
		}
		var colliderVariant = intersect["collider"];
		var collider = colliderVariant.As<Node3D>();
		if (collider == null) {
			GD.Print("[FrictionExperiment] 检测到的 collider 无法转换为 Node3D");
			return;
		}
		GD.Print($"[FrictionExperiment] 检测到物体: {collider.Name}, 类型: {collider.GetType().Name}");
		
		// 检查是否是 FrictionLabItem 自己的 StaticBody3D（交互碰撞体），如果是则跳过
		if (collider.Name == "StaticBody3D") {
			Node parent = collider.GetParent();
			if (parent == this || (parent != null && parent.Name == "FrictionLabItem")) {
				GD.Print("[FrictionExperiment] 检测到 FrictionLabItem 自己的 StaticBody3D，已排除");
				GD.Print("[FrictionExperiment] 提示：请直接点击 PlacableItem 物体（如方块）而不是实验台本身");
				return;
			}
		}
		
		// 向上遍历节点树，找到可拖拽的节点（PlacableItem 或在 moveable 组中的节点）
		Node3D draggableNode = null;
		Node current = collider;
		int depth = 0;
		const int maxDepth = 10; // 限制最大深度，避免无限循环
		
		while (current != null && depth < maxDepth) {
			if (current is Node3D node3D) {
				// 检查是否是 PlacableItem
				if (current is PlacableItem placableItem) {
					draggableNode = node3D;
					GD.Print($"[FrictionExperiment] ✓ 找到 PlacableItem: {node3D.Name} (深度: {depth})");
					break;
				}
				
				// 检查是否在 moveable 组中
				if (current.IsInGroup("moveable")) {
					draggableNode = node3D;
					GD.Print($"[FrictionExperiment] ✓ 找到 moveable 组节点: {node3D.Name} (深度: {depth})");
					break;
				}
				
				// 检查是否在可拖拽列表中
				if (draggableObjects.Contains(node3D)) {
					draggableNode = node3D;
					GD.Print($"[FrictionExperiment] ✓ 找到可拖拽列表节点: {node3D.Name} (深度: {depth})");
					break;
				}
			}
			
			// 向上查找父节点
			current = current.GetParent();
			depth++;
		}
		
		bool canMove = draggableNode != null;
		if (canMove && draggingObject == null && draggableNode != null) {
			draggingObject = draggableNode;
			
			// 保存物体的原始位置（用于平面约束的固定轴）
			Vector3 originalPos = draggingObject.GlobalPosition;
			
			// 先设置 initialDragPosition，这样 UpdateDragPosition 才能正确工作
			initialDragPosition = originalPos;
			
			// 计算鼠标在平面上的位置
			Vector3 mousePosInPlane = CalculateMousePositionInPlane();
			
			// 应用平面约束（使用原始位置作为参考）
			Vector3 targetPosition = ApplyPlaneConstraint(mousePosInPlane, originalPos);
			
			// 网格吸附（如果启用）
			if (EnableGridSnapping) {
				targetPosition = SnapToGrid(targetPosition);
			}
			
			// 立即将物体移动到鼠标位置（无偏移）
			draggingObject.GlobalPosition = targetPosition;
			
			// 更新 initialDragPosition 为移动后的位置（用于后续拖拽的平面约束）
			initialDragPosition = targetPosition;
			mousePosition = targetPosition;
			
			// 不设置偏移，让物体直接跟随鼠标位置
			dragOffset = Vector3.Zero;
			GD.Print($"[FrictionExperiment] ✓ 开始拖拽: {draggingObject.Name}, 平面: {DragPlane}");
		} else if (!canMove) {
			GD.PushWarning($"[FrictionExperiment] 物体 {collider.Name} 不可拖拽！");
			GD.Print($"[FrictionExperiment] 提示：请确保物体或其父节点是 PlacableItem，或在 'moveable' 组中");
			GD.Print($"[FrictionExperiment] 检测路径: {collider.GetPath()}");
			
			// 显示节点树信息，帮助调试
			Node node = collider;
			int showDepth = 0;
			while (node != null && showDepth < 5) {
				GD.Print($"[FrictionExperiment]   层级 {showDepth}: {node.Name} (类型: {node.GetType().Name})");
				if (node is Node3D n3d) {
					GD.Print($"[FrictionExperiment]     在 moveable 组: {n3d.IsInGroup("moveable")}");
					GD.Print($"[FrictionExperiment]     是 PlacableItem: {node is PlacableItem}");
				}
				node = node.GetParent();
				showDepth++;
			}
		}
	}

	private void EndDrag() {
		if (draggingObject != null) {
			GD.Print($"[FrictionExperiment] 结束拖拽: {draggingObject.Name}，最终位置: {draggingObject.GlobalPosition}");
			if (infoLabel != null) {
				infoLabel.Text = $"已放置: {draggingObject.Name}\n位置: {draggingObject.GlobalPosition}";
			}
			draggingObject = null;
			dragOffset = Vector3.Zero;
			initialDragPosition = Vector3.Zero;
		}
	}

	private Godot.Collections.Dictionary GetMouseIntersect(Vector2 mousePos, bool detailedDebug = false) {
		var currentCamera = GetViewport().GetCamera3D();
		if (currentCamera == null) {
			GD.PushWarning("[FrictionExperiment] 未找到相机！");
			return null;
		}
		
		// 调试：显示相机信息
		if (detailedDebug) {
			GD.Print($"[FrictionExperiment] === 相机信息 ===");
			GD.Print($"[FrictionExperiment] 相机名称: {currentCamera.Name}");
			GD.Print($"[FrictionExperiment] 相机类型: {currentCamera.GetType().Name}");
			GD.Print($"[FrictionExperiment] 相机位置: {currentCamera.GlobalPosition}");
			GD.Print($"[FrictionExperiment] 相机旋转: {currentCamera.GlobalRotation}");
			GD.Print($"[FrictionExperiment] 相机朝向: {currentCamera.GlobalTransform.Basis.Z}");
			GD.Print($"[FrictionExperiment] ==============");
		}
		
		var from = currentCamera.ProjectRayOrigin(mousePos);
		var to = from + currentCamera.ProjectRayNormal(mousePos) * 1000f;
		
		if (detailedDebug) {
			GD.Print($"[FrictionExperiment] 射线起点: {from}");
			GD.Print($"[FrictionExperiment] 射线终点: {to}");
			GD.Print($"[FrictionExperiment] 射线方向: {currentCamera.ProjectRayNormal(mousePos)}");
		}
		
		// 创建多个查询，分别检测不同层
		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithBodies = true; 
		query.CollideWithAreas = true; 
		// 检测所有层（包括层1和层2，PlacableItem 通常使用层2）
		query.CollisionMask = 0xFFFFFFFF; // 0xFFFFFFFF = 所有32层
		
		// 收集要排除的碰撞体
		var excludeList = new Godot.Collections.Array<Rid>();
		
		// 排除正在拖拽的物体
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
		
		// 重要：排除 FrictionLabItem 自己的 StaticBody3D（交互碰撞体）
		var staticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
		if (staticBody != null) {
			excludeList.Add(staticBody.GetRid());
			if (detailedDebug) {
				GD.Print($"[FrictionExperiment] 已排除 FrictionLabItem 的 StaticBody3D");
			}
		}
		
		if (excludeList.Count > 0) {
			query.Exclude = excludeList;
		}
		
		var spaceState = GetWorld3D().DirectSpaceState;
		var result = spaceState.IntersectRay(query);
		
		// 详细的调试信息（只在详细模式或未命中时显示）
		if (!isDragging || detailedDebug) {
			if (result.Count > 0) {
				GD.Print($"[FrictionExperiment] ✓ 射线检测命中！结果数量: {result.Count}");
				if (result.ContainsKey("collider")) {
					var colliderVariant = result["collider"];
					if (detailedDebug) {
						GD.Print($"[FrictionExperiment] 命中物体: {colliderVariant}");
					}
					var collider = colliderVariant.As<GodotObject>();
					if (collider != null) {
						GD.Print($"[FrictionExperiment] 物体类型: {collider.GetType().Name}");
						if (collider is Node node) {
							GD.Print($"[FrictionExperiment] 物体名称: {node.Name}, 路径: {node.GetPath()}");
							if (node is CollisionObject3D collisionObj) {
								GD.Print($"[FrictionExperiment] 碰撞层: {collisionObj.CollisionLayer}, 碰撞掩码: {collisionObj.CollisionMask}");
							}
						}
					}
				}
				if (result.ContainsKey("position")) {
					var pos = result["position"].AsVector3();
					GD.Print($"[FrictionExperiment] 命中位置: {pos}");
				}
			} else {
				// 未命中时总是显示简单信息
				GD.Print($"[FrictionExperiment] 射线未命中任何物体" + (detailedDebug ? "" : "（右键点击查看详细信息）"));
				
				// 详细调试信息
				if (detailedDebug) {
					GD.Print($"[FrictionExperiment] 射线起点: {from}, 终点: {to}");
					GD.Print($"[FrictionExperiment] 鼠标位置: {mousePos}");
					GD.Print($"[FrictionExperiment] === 开始详细检测 ===");
					
					// 尝试检测不同层
					bool foundAny = false;
					for (uint layer = 1; layer <= 32; layer++) {
						var layerQuery = PhysicsRayQueryParameters3D.Create(from, to);
						layerQuery.CollideWithBodies = true;
						layerQuery.CollideWithAreas = true;
						layerQuery.CollisionMask = 1u << (int)(layer - 1);
						var layerResult = spaceState.IntersectRay(layerQuery);
						if (layerResult.Count > 0) {
							foundAny = true;
							GD.Print($"[FrictionExperiment] ✓ 在层 {layer} 检测到物体！");
							if (layerResult.ContainsKey("collider")) {
								var colliderVariant = layerResult["collider"];
								var collider = colliderVariant.As<GodotObject>();
								if (collider != null) {
									GD.Print($"[FrictionExperiment]   物体类型: {collider.GetType().Name}");
									if (collider is Node node) {
										GD.Print($"[FrictionExperiment]   物体名称: {node.Name}");
									}
								}
							}
						}
					}
					if (!foundAny) {
						GD.Print($"[FrictionExperiment] 在所有32层都未检测到物体");
					}
					
					// 尝试直接检测可拖拽物体
					GD.Print($"[FrictionExperiment] === 直接检测可拖拽物体 ===");
					GD.Print($"[FrictionExperiment] 当前相机视野大小: {currentCamera.Size}");
					var viewport = GetViewport();
					var viewportSize = viewport.GetVisibleRect().Size;
					GD.Print($"[FrictionExperiment] 视口大小: {viewportSize}");
					
					foreach (var obj in draggableObjects) {
						if (!GodotObject.IsInstanceValid(obj)) continue;
						var distance = currentCamera.GlobalPosition.DistanceTo(obj.GlobalPosition);
						GD.Print($"[FrictionExperiment] 可拖拽物体: {obj.Name}, 距离相机: {distance:F2}m");
						
						// 检查物体是否在相机视野内
						var objScreenPos = currentCamera.UnprojectPosition(obj.GlobalPosition);
						var screenDistance = mousePos.DistanceTo(objScreenPos);
						GD.Print($"[FrictionExperiment]   物体世界位置: {obj.GlobalPosition}");
						GD.Print($"[FrictionExperiment]   物体屏幕位置: {objScreenPos}");
						GD.Print($"[FrictionExperiment]   鼠标位置: {mousePos}");
						GD.Print($"[FrictionExperiment]   屏幕距离: {screenDistance:F1}");
						
						// 检查物体是否在相机前方
						var cameraToObject = obj.GlobalPosition - currentCamera.GlobalPosition;
						var cameraForward = -currentCamera.GlobalTransform.Basis.Z; // 相机前方方向
						float dot = cameraToObject.Normalized().Dot(cameraForward.Normalized());
						bool isInFrontOfCamera = dot > 0; // 点积 > 0 表示在相机前方
						
						// 检查物体是否在屏幕内
						bool isOnScreen = objScreenPos.X >= 0 && objScreenPos.X <= viewportSize.X &&
						                  objScreenPos.Y >= 0 && objScreenPos.Y <= viewportSize.Y &&
						                  isInFrontOfCamera;
						GD.Print($"[FrictionExperiment]   物体在相机前方: {isInFrontOfCamera} (点积: {dot:F3})");
						GD.Print($"[FrictionExperiment]   物体在屏幕内: {isOnScreen}");
						
						// 如果物体很近，尝试直接创建射线检测
						if (screenDistance < 100 && isOnScreen) {
							GD.Print($"[FrictionExperiment]   尝试直接从相机到物体的射线检测");
							var directQuery = PhysicsRayQueryParameters3D.Create(
								currentCamera.GlobalPosition,
								obj.GlobalPosition
							);
							directQuery.CollideWithBodies = true;
							directQuery.CollideWithAreas = true;
							directQuery.CollisionMask = 0xFFFFFFFF;
							var directResult = spaceState.IntersectRay(directQuery);
							if (directResult.Count > 0) {
								GD.Print($"[FrictionExperiment]   ✓ 直接从相机到物体的射线检测成功！");
							} else {
								GD.Print($"[FrictionExperiment]   ✗ 直接从相机到物体的射线检测失败");
							}
						}
					}
					
					GD.Print($"[FrictionExperiment] === 详细检测完成 ===");
				}
			}
		}
		
		return result;
	}

	/// <summary>
	/// 计算鼠标在指定平面上的位置
	/// </summary>
	private Vector3 CalculateMousePositionInPlane() {
		var mousePos = GetViewport().GetMousePosition();
		var camera = GetViewport().GetCamera3D();
		
		if (camera == null) return draggingObject != null ? draggingObject.GlobalPosition : Vector3.Zero;
		
		var from = camera.ProjectRayOrigin(mousePos);
		var normal = camera.ProjectRayNormal(mousePos);
		
		// 确定参考点（物体当前位置或初始位置）
		Vector3 referencePoint = draggingObject != null ? draggingObject.GlobalPosition : initialDragPosition;
		
		// 方法1：尝试射线检测获取3D位置
		var intersect = GetMouseIntersect(mousePos);
		Vector3 hitPosition = Vector3.Zero;
		bool hasHit = false;
		
		if (intersect != null && intersect.ContainsKey("position")) {
			hitPosition = (Vector3)intersect["position"];
			hasHit = true;
		}
		
		// 根据平面类型计算最终位置
		Vector3 planePoint = referencePoint;
		Vector3 planeNormal = Vector3.Zero;
		
		switch (DragPlane) {
			case DragPlaneType.Horizontal:
				planeNormal = Vector3.Up; // Y轴向上 (0, 1, 0)
				if (hasHit) {
					// 如果射线命中，使用命中位置的X和Z，但保持Y不变
					hitPosition.Y = referencePoint.Y;
					return hitPosition;
				}
				break;
				
			case DragPlaneType.VerticalX:
				planeNormal = Vector3.Right; // X轴向右 (1, 0, 0)
				if (hasHit) {
					// 如果射线命中，使用命中位置的Y和Z，但保持X不变
					hitPosition.X = referencePoint.X;
					return hitPosition;
				}
				break;
				
			case DragPlaneType.VerticalZ:
				planeNormal = new Vector3(0, 0, 1); // Z轴向前 (0, 0, 1)
				if (hasHit) {
					// 如果射线命中，使用命中位置的X和Y，但保持Z不变
					hitPosition.Z = referencePoint.Z;
					return hitPosition;
				}
				break;
				
			case DragPlaneType.Free:
				// 自由移动：如果有命中就用命中位置，否则使用默认距离
				if (hasHit) {
					return hitPosition;
				}
				float defaultDistance = 5.0f;
				return from + normal * defaultDistance;
		}
		
		// 方法2：如果射线未命中，使用平面投影计算
		// 计算射线与平面的交点
		float denom = normal.Dot(planeNormal);
		if (Mathf.Abs(denom) > 0.0001f) {
			float t = (planePoint - from).Dot(planeNormal) / denom;
			if (t > 0) {
				return from + normal * t;
			} else {
				// 如果交点在相机后方，使用固定距离
				return from + normal * 5.0f;
			}
		} else {
			// 射线与平面平行，使用固定距离
			return from + normal * 5.0f;
		}
	}

	/// <summary>
	/// 应用平面约束，限制物体在指定平面内移动
	/// </summary>
	private Vector3 ApplyPlaneConstraint(Vector3 targetPosition, Vector3 currentPosition) {
		switch (DragPlane) {
			case DragPlaneType.Horizontal:
				// 水平平面：固定 Y 轴（在 XZ 平面移动）
				targetPosition.Y = currentPosition.Y;
				break;
				
			case DragPlaneType.VerticalX:
				// 垂直平面（YZ平面）：固定 X 轴
				targetPosition.X = currentPosition.X;
				break;
				
			case DragPlaneType.VerticalZ:
				// 垂直平面（XY平面）：固定 Z 轴
				targetPosition.Z = currentPosition.Z;
				break;
				
			case DragPlaneType.Free:
				// 自由移动：不约束任何轴
				break;
		}
		
		return targetPosition;
	}

	private Vector3 SnapToGrid(Vector3 position) {
		return new Vector3(
			Mathf.Round(position.X / GridSize) * GridSize,
			Mathf.Round(position.Y / GridSize) * GridSize,
			Mathf.Round(position.Z / GridSize) * GridSize
		);
	}
}


/*
[Export] public NodePath ExperimentUIPanelPath { get; set; }
[Export] public NodePath ForceMeterPath { get; set; }
[Export] public Godot.Collections.Array<NodePath> BlockPaths { get; set; } = new();
private Control experimentUIPanel;
private ForceMeter forceMeter;
private List<ExperimentBlock> blocks = new();
private ExperimentBlock currentBlock;
private SurfaceType currentSurface = SurfaceType.Wood;

private enum ExperimentStep {
	Introduction,
	SelectBlock,
	PlaceBlock,
	SelectSurface,
	PullBlock,
	RecordData,
	ChangeCondition,
	Analysis
}

private ExperimentStep currentStep = ExperimentStep.Introduction;
private List<ExperimentData> experimentDataList = new();
private Label stepLabel;
private Label instructionLabel;
private Button nextStepButton;
private Button previousStepButton;
private Control dataPanel;
private VBoxContainer dataContainer;
private Label forceValueLabel;
private OptionButton surfaceSelector;
private Button startPullButton;
private Button recordDataButton;
private Button analysisButton;
private RichTextLabel analysisText;

// ... 所有复杂的UI创建、步骤管理、数据分析等方法都已注释
// ... 待基础拖拽功能测试完成后，再逐步恢复这些功能
*/

/// <summary>
/// 拖拽平面类型
/// </summary>
public enum DragPlaneType {
	Horizontal,  // 水平平面（XZ平面，Y轴固定）
	VerticalX,   // 垂直平面（YZ平面，X轴固定）
	VerticalZ,   // 垂直平面（XY平面，Z轴固定）
	Free         // 自由移动（无约束）
}

/*
public enum SurfaceType {
	Wood,
	Glass,
	Cloth,
	Metal
}

public struct ExperimentData {
	public string BlockName;
	public float Mass;
	public SurfaceType Surface;
	public float FrictionForce;
}
*/
