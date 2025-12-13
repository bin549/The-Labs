using Godot;
using Godot.Collections;

public enum InclinedPlaneExperimentStep {
    Setup,
    PlaceObject,
    AdjustAngle,
    MeasureAngle,
    ReleaseObject,
    RecordData,
    AnalyzeResult,
    Completed
}

public enum InclinedPlaneExperimentItem {
    InclinedPlane,
    SliderObject,
    AngleMeter,
    Ruler,
    Timer,
    DataBoard,
    SupportStand
}

public partial class InclinedPlaneExperiment : StepExperimentLabItem<InclinedPlaneExperimentStep, InclinedPlaneExperimentItem> {
    [Export] protected override InclinedPlaneExperimentStep currentStep { get; set; } = InclinedPlaneExperimentStep.Setup;
    [ExportGroup("Drag and Drop")]
    [Export] private PlacableItem cube;
    [Export] private Node3D indicateEffect;
    [Export] private Area3D triggerArea;
    [Export] private Label3D collisionLabel;
    [ExportGroup("Placed Objects")]
    [Export] private Node3D placedObject;
    [Export] private Node3D arrowObject;
    [Export] private PathFollow3D pathFollow;
    [Export] private float moveDuration = 2.0f;
    private bool isCubeInTriggerArea = false;
    private bool isCubePlaced = false;
    private Tween moveTween;
     
    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.InitializeIndicateEffect();
    }

    private void InitializeIndicateEffect() {
        if (this.indicateEffect != null) {
            this.indicateEffect.Visible = false;
        }
        this.InitializeTriggerArea();
        this.InitializeCollisionLabel();
        this.InitializePlacedObject();
        this.InitializeArrowObject();
    }

    private void InitializeTriggerArea() {
        if (this.triggerArea == null && this.indicateEffect != null) {
            this.triggerArea = this.indicateEffect.GetNodeOrNull<Area3D>("Area3D");
        }
        if (this.triggerArea != null) {
            this.triggerArea.BodyEntered += OnTriggerAreaBodyEntered;
            this.triggerArea.BodyExited += OnTriggerAreaBodyExited;
            this.triggerArea.AreaEntered += OnTriggerAreaEntered;
            this.triggerArea.AreaExited += OnTriggerAreaExited;
        }
    }

    private void InitializeCollisionLabel() {
        if (this.collisionLabel != null) {
            this.collisionLabel.Visible = false;
        }
    }

    private void InitializePlacedObject() {
        if (this.placedObject != null) {
            this.placedObject.Visible = false;
        }
    }

    private void InitializeArrowObject() {
        if (this.arrowObject != null) {
            this.arrowObject.Visible = false;
        }
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void ExitInteraction() {
        base.isInteracting = false;
        base.ExitInteraction();
        this.HideIndicateEffect();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        this.UpdateIndicateEffect();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) {
            return;
        }
        if (this.isCubePlaced && this.arrowObject != null && this.arrowObject.Visible) {
            if (@event is InputEventMouseButton mouseButton) {
                if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed && !mouseButton.IsEcho()) {
                    var intersect = this.GetMouseIntersect(mouseButton.Position);
                    if (intersect != null) {                        
                        if (this.IsClickOnArrow(intersect)) {
                            this.OnArrowClicked();
                            GetViewport().SetInputAsHandled();
                        }
                    }
                }
            }
        }
    }

    private void UpdateIndicateEffect() {
        if (this.cube == null || this.indicateEffect == null) {
            return;
        }
        if (this.isCubePlaced) {
            return;
        }
        if (this.cube.IsDragging) {
            this.ShowIndicateEffect();
        } else {
            this.HideIndicateEffect();
            if (this.isCubeInTriggerArea && !this.isCubePlaced) {
                this.OnCubePlaced();
            }
        }
    }    

    private void ShowIndicateEffect() {
        if (this.indicateEffect == null || this.cube == null) {
            return;
        }
        this.indicateEffect.Visible = true;
    }

    private void HideIndicateEffect() {
        if (this.indicateEffect == null) {
            return;
        }
        this.indicateEffect.Visible = false;
        this.HideCollisionLabel();
    }

    private void OnTriggerAreaBodyEntered(Node3D body) {
        this.HandleTriggerAreaCollision(body);
    }

    private void OnTriggerAreaBodyExited(Node3D body) {
        this.HandleTriggerAreaExit(body);
    }

    private void OnTriggerAreaEntered(Area3D area) {
        this.HandleTriggerAreaCollision(area);
    }

    private void OnTriggerAreaExited(Area3D area) {
        this.HandleTriggerAreaExit(area);
    }

    private void HandleTriggerAreaCollision(Node node) {
        if (this.cube == null || this.isCubePlaced) {
            return;
        }
        if (this.IsNodePartOfCube(node)) {
            this.isCubeInTriggerArea = true;
            if (this.cube.IsDragging) {
                this.ShowCollisionLabel();
            }
        }
    }

    private void HandleTriggerAreaExit(Node node) {
        if (this.cube == null || this.isCubePlaced) {
            return;
        }
        if (this.IsNodePartOfCube(node)) {
            this.isCubeInTriggerArea = false;
            this.HideCollisionLabel();
        }
    }

    private bool IsNodePartOfCube(Node node) {
        if (node == null) {
            return false;
        }
        if (node == this.cube) {
            return true;
        }
        Node current = node;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == this.cube) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private void ShowCollisionLabel() {
        if (this.collisionLabel != null) {
            this.collisionLabel.Visible = true;
        }
    }

    private void HideCollisionLabel() {
        if (this.collisionLabel != null) {
            this.collisionLabel.Visible = false;
        }
    }

    private void OnCubePlaced() {
        if (this.cube == null || this.isCubePlaced) {
            return;
        }
        this.isCubePlaced = true;
        if (this.cube != null) {
            this.cube.Visible = false;
        }
        if (this.placedObject != null) {
            this.placedObject.Visible = true;
        }
        if (this.arrowObject != null) {
            this.arrowObject.Visible = true;
        }
        this.HideIndicateEffect();
        this.HideCollisionLabel();
        this.isCubeInTriggerArea = false;
        this.GoToNextStep();
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
        var labStaticBody = GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (labStaticBody != null) {
            excludeList.Add(labStaticBody.GetRid());
        }
        if (this.triggerArea != null) {
            excludeList.Add(this.triggerArea.GetRid());
        }
        if (excludeList.Count > 0) {
            query.Exclude = excludeList;
        }
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        return result;
    }

    private bool IsClickOnArrow(Dictionary intersect) {
        if (intersect == null || !intersect.ContainsKey("collider")) {
            return false;
        }
        var colliderVariant = intersect["collider"];
        var collider = colliderVariant.As<Node3D>();
        if (collider == null) {
            return false;
        }
        var parent = collider.GetParent();
        if (parent == this.arrowObject) {
            return true;
        }
        Node current = collider;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == this.arrowObject) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private void OnArrowClicked() {
        if (this.pathFollow == null) {
            return;
        }
        if (this.moveTween != null && this.moveTween.IsValid()) {
            this.moveTween.Kill();
        }
        this.moveTween = CreateTween();
        this.moveTween.TweenProperty(this.pathFollow, "progress_ratio", 1.0f, this.moveDuration);
    }

    private void InitializeStepHints() {
        base.stepHints[InclinedPlaneExperimentStep.Setup] = 
            "[b]步骤 1：拖拽和放置物品";
        base.stepHints[InclinedPlaneExperimentStep.PlaceObject] = 
            "[b]步骤 2：放置物体[/b]\n\n" +
            "[color=yellow]提示：[/color] 物体应该放在斜坡顶部，准备进行实验";
        base.stepHints[InclinedPlaneExperimentStep.AdjustAngle] = 
            "[b]步骤 3：调整角度[/b]\n\n" +
            "• 使用支撑架（SupportStand）来调整斜坡的角度\n" +
            "• 可以逐渐增加或减少斜坡的倾斜角度\n" +
            "• 观察物体在不同角度下的状态\n" +
            "• 选择合适的角度进行实验（建议从较小角度开始）\n\n" +
            "[color=yellow]提示：[/color] 角度越大，物体下滑的速度越快";
        base.stepHints[InclinedPlaneExperimentStep.MeasureAngle] = 
            "[b]步骤 4：测量角度[/b]\n\n" +
            "• 使用角度测量器（AngleMeter）来测量斜坡的倾斜角度\n" +
            "• 将角度测量器放置在斜坡表面\n" +
            "• 读取并记录角度数值\n" +
            "• 可以尝试测量多个不同的角度\n\n" +
            "[color=yellow]提示：[/color] 准确记录角度值，这对后续数据分析很重要";
        base.stepHints[InclinedPlaneExperimentStep.ReleaseObject] = 
            "[b]步骤 5：释放物体[/b]\n\n" +
            "• 确认物体已放置在斜坡顶部\n" +
            "• 准备好计时器（Timer）开始计时\n" +
            "• 释放物体，让它沿着斜坡自由滑动\n" +
            "• 观察物体的运动情况\n\n" +
            "[color=yellow]提示：[/color] 释放时要确保物体从静止状态开始运动";
        base.stepHints[InclinedPlaneExperimentStep.RecordData] = 
            "[b]步骤 6：记录数据[/b]\n\n" +
            "• 使用计时器记录物体从顶部滑到底部的时间\n" +
            "• 使用尺子（Ruler）测量斜坡的长度\n" +
            "• 将测量数据记录到数据记录板（DataBoard）上\n" +
            "• 可以尝试不同角度，记录多组数据\n" +
            "• 记录内容包括：角度、时间、距离等\n\n" +
            "[color=yellow]提示：[/color] 多次测量取平均值可以提高实验精度";
        base.stepHints[InclinedPlaneExperimentStep.AnalyzeResult] = 
            "[b]步骤 7：分析结果[/b]\n\n" +
            "• 查看数据记录板上的所有实验数据\n" +
            "• 分析角度与运动时间的关系\n" +
            "• 观察是否存在规律性\n" +
            "• 思考：为什么角度越大，物体下滑越快？\n" +
            "• 可以绘制角度-时间的图表来直观分析\n\n" +
            "[color=yellow]提示：[/color] 这与重力分力和摩擦力有关，角度越大，重力沿斜坡的分力越大";
        base.stepHints[InclinedPlaneExperimentStep.Completed] = 
            "[b]实验完成！[/b]\n\n" +
            "恭喜你完成了斜坡实验！\n\n" +
            "[color=lightgreen]实验总结：[/color]\n" +
            "• 你已经成功完成了所有实验步骤\n" +
            "• 记录了实验数据并进行了分析\n" +
            "• 理解了斜坡角度对物体运动的影响\n\n" +
            "可以重新开始实验，尝试不同的角度和物体，探索更多有趣的物理现象！";
    }

    protected override string GetStepName(InclinedPlaneExperimentStep step) {
        switch (step) {
            case InclinedPlaneExperimentStep.Setup:
                return "准备阶段";
            case InclinedPlaneExperimentStep.PlaceObject:
                return "放置物体";
            case InclinedPlaneExperimentStep.AdjustAngle:
                return "调整角度";
            case InclinedPlaneExperimentStep.MeasureAngle:
                return "测量角度";
            case InclinedPlaneExperimentStep.ReleaseObject:
                return "释放物体";
            case InclinedPlaneExperimentStep.RecordData:
                return "记录数据";
            case InclinedPlaneExperimentStep.AnalyzeResult:
                return "分析结果";
            case InclinedPlaneExperimentStep.Completed:
                return "实验完成";
            default:
                return "未知步骤";
        }
    }

    protected override InclinedPlaneExperimentStep SetupStep => InclinedPlaneExperimentStep.Setup;
    
    protected override InclinedPlaneExperimentStep CompletedStep => InclinedPlaneExperimentStep.Completed;
}