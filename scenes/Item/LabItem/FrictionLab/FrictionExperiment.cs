using Godot;
using System.Collections.Generic;

public partial class FrictionExperiment : LabItem {
    [Export] public NodePath SurfacePlatformPath { get; set; }
    [Export] public Godot.Collections.Array<NodePath> DraggableObjectPaths { get; set; } = new();
    [Export] public float GridSize { get; set; } = 0.5f;
    [Export] public bool EnableGridSnapping { get; set; } = false;
    [ExportGroup("拖拽约束")] [Export] public DragPlaneType DragPlane { get; set; } = DragPlaneType.Horizontal;
    private Node3D draggingObject;
    private Vector3 mousePosition;
    private bool isDragging = false;
    private List<Node3D> draggableObjects = new();
    private List<PlacableItem> placableItems = new();
    private Node3D surfacePlatform;
    private Vector3 dragOffset;
    private Vector3 initialDragPosition;
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
        if (!base.isInteracting) {
            return;
        }
        if (@event.IsActionPressed("pause") || @event.IsActionPressed("ui_cancel")) {
            GetViewport().SetInputAsHandled();
            ExitInteraction();
            return;
        }
        if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
            if (keyEvent.Keycode == Key.G) {
                EnableGridSnapping = !EnableGridSnapping;
                GD.Print($"[FrictionExperiment] 网格吸附: {(EnableGridSnapping ? "已启用" : "已禁用")}");
                if (infoLabel != null) {
                    string status = EnableGridSnapping ? "已启用" : "已禁用";
                    infoLabel.Text = $"网格吸附: {status}\n按 G 键切换";
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
        if (@event is InputEventMouseButton mouseButton) {
            string buttonName = mouseButton.ButtonIndex == MouseButton.Left ? "左键" :
                mouseButton.ButtonIndex == MouseButton.Right ? "右键" :
                mouseButton.ButtonIndex.ToString();
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
                } else if (rightButtonPressed) {
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
        if (draggingObject != null && isDragging) {
            UpdateDragPosition();
        }
    }

    private void UpdateDragPosition() {
        if (draggingObject == null) return;
        Vector3 mousePosInPlane = CalculateMousePositionInPlane();
        Vector3 targetPosition = mousePosInPlane;
        targetPosition = ApplyPlaneConstraint(targetPosition, initialDragPosition);
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
        if (infoLabel != null) {
            infoLabel.Visible = true;
            string gridStatus = EnableGridSnapping ? "已启用" : "已禁用";
            infoLabel.Text = $"✅ 交互模式已激活\n\n点击并拖拽物体到实验平台上\n按 G 键切换网格吸附（当前: {gridStatus}）\n按ESC退出交互模式";
        }
        CheckBlockingUI();
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

    private void CheckBlockingUI() {
        var player = GetTree().Root.FindChild("Player", true, false);
        if (player != null) {
            var canvasLayer = player.FindChild("CanvasLayer", false, false);
            if (canvasLayer != null) {
                CheckUIChildren(canvasLayer);
            }
        }
    }

    private void CheckUIChildren(Node parent, int depth = 0, bool autoFix = true) {
        foreach (Node child in parent.GetChildren()) {
            if (child is Control control) {
                string indent = new string(' ', depth * 2);
                GD.Print(
                    $"{indent}[FrictionExperiment] UI节点: {control.Name}, Visible: {control.Visible}, MouseFilter: {control.MouseFilter}");
                if (control.Visible && control.MouseFilter == Control.MouseFilterEnum.Stop) {
                    var rect = control.GetRect();
                    var viewport = GetViewport();
                    bool isLarge = false;
                    if (viewport != null) {
                        var viewportSize = viewport.GetVisibleRect().Size;
                        isLarge = rect.Size.X >= viewportSize.X * 0.5f || rect.Size.Y >= viewportSize.Y * 0.5f;
                        if (isLarge) {
                            GD.PushWarning(
                                $"[FrictionExperiment] 警告: {control.Name} 可能是全屏 UI (大小: {rect.Size})，可能会阻挡鼠标事件！");
                            if (autoFix && !(control is Button)) {
                                control.MouseFilter = Control.MouseFilterEnum.Pass;
                                GD.Print($"{indent}[FrictionExperiment] ✓ 已修复 {control.Name} 的 MouseFilter 为 Pass");
                            }
                        }
                    }
                    if (autoFix && (control is RichTextLabel || control is Label)) {
                        control.MouseFilter = Control.MouseFilterEnum.Ignore;
                        GD.Print($"{indent}[FrictionExperiment] ✓ 已修复 {control.Name} 的 MouseFilter 为 Ignore（文本标签）");
                    }
                }
            }
            if (depth < 5) {
                CheckUIChildren(child, depth + 1, autoFix);
            }
        }
    }

    public override void ExitInteraction() {
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
                if (child is PlacableItem placableItem) {
                    if (!placableItems.Contains(placableItem)) {
                        placableItems.Add(placableItem);
                        GD.Print($"[FrictionExperiment] ✓ 找到 PlacableItem: {node3D.Name}");
                    }
                    if (!node3D.IsInGroup("moveable")) {
                        node3D.AddToGroup("moveable");
                        GD.Print($"[FrictionExperiment] ✓ 自动将 PlacableItem {node3D.Name} 添加到 moveable 组");
                    }
                    FixPlacableItemCollisionArea(placableItem);
                }
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
                GD.Print($"[FrictionExperiment] 修复 {placableItem.Name} 的 CollisionArea 碰撞层为 1");
            }
            GD.Print(
                $"[FrictionExperiment] {placableItem.Name} CollisionArea 设置: Layer={collisionArea.CollisionLayer}, InputRayPickable={collisionArea.InputRayPickable}");
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
        if (collider.Name == "StaticBody3D") {
            Node parent = collider.GetParent();
            if (parent == this || (parent != null && parent.Name == "FrictionLabItem")) {
                GD.Print("[FrictionExperiment] 检测到 FrictionLabItem 自己的 StaticBody3D，已排除");
                GD.Print("[FrictionExperiment] 提示：请直接点击 PlacableItem 物体（如方块）而不是实验台本身");
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
                    GD.Print($"[FrictionExperiment] ✓ 找到 PlacableItem: {node3D.Name} (深度: {depth})");
                    break;
                }
                if (current.IsInGroup("moveable")) {
                    draggableNode = node3D;
                    GD.Print($"[FrictionExperiment] ✓ 找到 moveable 组节点: {node3D.Name} (深度: {depth})");
                    break;
                }
                if (draggableObjects.Contains(node3D)) {
                    draggableNode = node3D;
                    GD.Print($"[FrictionExperiment] ✓ 找到可拖拽列表节点: {node3D.Name} (深度: {depth})");
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
            if (EnableGridSnapping) {
                targetPosition = SnapToGrid(targetPosition);
            }
            draggingObject.GlobalPosition = targetPosition;
            initialDragPosition = targetPosition;
            mousePosition = targetPosition;
            dragOffset = Vector3.Zero;
            GD.Print($"[FrictionExperiment] ✓ 开始拖拽: {draggingObject.Name}, 平面: {DragPlane}");
        } else if (!canMove) {
            GD.PushWarning($"[FrictionExperiment] 物体 {collider.Name} 不可拖拽！");
            GD.Print($"[FrictionExperiment] 提示：请确保物体或其父节点是 PlacableItem，或在 'moveable' 组中");
            GD.Print($"[FrictionExperiment] 检测路径: {collider.GetPath()}");
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
            if (detailedDebug) {
                GD.Print($"[FrictionExperiment] 已排除 FrictionLabItem 的 StaticBody3D");
            }
        }
        if (excludeList.Count > 0) {
            query.Exclude = excludeList;
        }
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
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
                                GD.Print(
                                    $"[FrictionExperiment] 碰撞层: {collisionObj.CollisionLayer}, 碰撞掩码: {collisionObj.CollisionMask}");
                            }
                        }
                    }
                }
                if (result.ContainsKey("position")) {
                    var pos = result["position"].AsVector3();
                    GD.Print($"[FrictionExperiment] 命中位置: {pos}");
                }
            } else {
                GD.Print($"[FrictionExperiment] 射线未命中任何物体" + (detailedDebug ? "" : "（右键点击查看详细信息）"));
                if (detailedDebug) {
                    GD.Print($"[FrictionExperiment] 射线起点: {from}, 终点: {to}");
                    GD.Print($"[FrictionExperiment] 鼠标位置: {mousePos}");
                    GD.Print($"[FrictionExperiment] === 开始详细检测 ===");
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
                    GD.Print($"[FrictionExperiment] === 直接检测可拖拽物体 ===");
                    GD.Print($"[FrictionExperiment] 当前相机视野大小: {currentCamera.Size}");
                    var viewport = GetViewport();
                    var viewportSize = viewport.GetVisibleRect().Size;
                    GD.Print($"[FrictionExperiment] 视口大小: {viewportSize}");
                    foreach (var obj in draggableObjects) {
                        if (!GodotObject.IsInstanceValid(obj)) continue;
                        var distance = currentCamera.GlobalPosition.DistanceTo(obj.GlobalPosition);
                        GD.Print($"[FrictionExperiment] 可拖拽物体: {obj.Name}, 距离相机: {distance:F2}m");
                        var objScreenPos = currentCamera.UnprojectPosition(obj.GlobalPosition);
                        var screenDistance = mousePos.DistanceTo(objScreenPos);
                        GD.Print($"[FrictionExperiment]   物体世界位置: {obj.GlobalPosition}");
                        GD.Print($"[FrictionExperiment]   物体屏幕位置: {objScreenPos}");
                        GD.Print($"[FrictionExperiment]   鼠标位置: {mousePos}");
                        GD.Print($"[FrictionExperiment]   屏幕距离: {screenDistance:F1}");
                        var cameraToObject = obj.GlobalPosition - currentCamera.GlobalPosition;
                        var cameraForward = -currentCamera.GlobalTransform.Basis.Z;
                        float dot = cameraToObject.Normalized().Dot(cameraForward.Normalized());
                        bool isInFrontOfCamera = dot > 0;
                        bool isOnScreen = objScreenPos.X >= 0 && objScreenPos.X <= viewportSize.X &&
                                          objScreenPos.Y >= 0 && objScreenPos.Y <= viewportSize.Y &&
                                          isInFrontOfCamera;
                        GD.Print($"[FrictionExperiment]   物体在相机前方: {isInFrontOfCamera} (点积: {dot:F3})");
                        GD.Print($"[FrictionExperiment]   物体在屏幕内: {isOnScreen}");
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

    private Vector3 SnapToGrid(Vector3 position) {
        return new Vector3(
            Mathf.Round(position.X / GridSize) * GridSize,
            Mathf.Round(position.Y / GridSize) * GridSize,
            Mathf.Round(position.Z / GridSize) * GridSize
        );
    }
}

public enum DragPlaneType {
    Horizontal,
    VerticalX,
    VerticalZ,
    Free
}
