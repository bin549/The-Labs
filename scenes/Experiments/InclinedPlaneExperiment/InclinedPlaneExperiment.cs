using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class ExperimentData {
    public int ExperimentNumber { get; set; }
    public float Time { get; set; }
    public float Distance { get; set; }
    public float? Angle { get; set; }
    
    public ExperimentData(int number, float time, float distance, float? angle = null) {
        this.ExperimentNumber = number;
        this.Time = time;
        this.Distance = distance;
        this.Angle = angle;
    }
    
    public override string ToString() {
        string angleStr = this.Angle.HasValue ? this.Angle.Value.ToString("F2") : "N/A";
        return $"实验 {ExperimentNumber}: 时间={Time:F2}s, 距离={Distance:F2}m, 角度={angleStr}°";
    }
}

public class CubeObjectConfig {
    public PlacableItem Cube { get; set; }
    public Node3D IndicateEffect { get; set; }
    public Area3D TriggerArea { get; set; }
    public Label3D CollisionLabel { get; set; }
    public Node3D PlacedObject { get; set; }
    public Node3D ArrowObject { get; set; }
    public PathFollow3D PathFollow { get; set; }
    public bool IsCubeInTriggerArea { get; set; } = false;
    public bool IsCubePlaced { get; set; } = false;
    public Tween MoveTween { get; set; }
    public ExperimentData ExperimentData { get; set; }
    public Vector3 InitialCubePosition { get; set; }
    public Vector3 InitialPlacedObjectPosition { get; set; }
    public float InitialPathFollowProgress { get; set; } = 0f;
}

public partial class InclinedPlaneExperiment : LabItem {
    [ExportGroup("Cube Objects")]
    [Export] private Godot.Collections.Array<PlacableItem> cubes = new Godot.Collections.Array<PlacableItem>();
    [Export] private Godot.Collections.Array<Node3D> indicateEffects = new Godot.Collections.Array<Node3D>();
    [Export] private Godot.Collections.Array<Area3D> triggerAreas = new Godot.Collections.Array<Area3D>();
    [Export] private Godot.Collections.Array<Label3D> hintLabels = new Godot.Collections.Array<Label3D>();
    [Export] private Godot.Collections.Array<Node3D> placedObjects = new Godot.Collections.Array<Node3D>();
    [Export] private Godot.Collections.Array<Node3D> arrowObjects = new Godot.Collections.Array<Node3D>();
    [Export] private Godot.Collections.Array<PathFollow3D> pathFollows = new Godot.Collections.Array<PathFollow3D>();
    [Export] private float moveDuration = 2.0f;
    private List<CubeObjectConfig> cubeConfigs = new List<CubeObjectConfig>();
    [ExportGroup("Arrow Hover")]
    [Export] private float arrowNormalAlpha = 0.35f;
    [Export] private float arrowHoverAlpha = 1.0f;
    [ExportGroup("Data Recording")]
    private List<ExperimentData> experimentDataList = new List<ExperimentData>();
    private int experimentCount = 0;
    private DataBoard dataBoard;
     
    public override void _Ready() {
        base._Ready();
        this.InitializeCubeConfigs();
        this.InitializeDataBoard();
    }
    
    private void InitializeCubeConfigs() {
        int count = this.cubes.Count;
        if (count == 0) {
            return;
        }
        for (int i = 0; i < count; i++) {
            var config = new CubeObjectConfig {
                Cube = this.cubes[i],
                IndicateEffect = i < this.indicateEffects.Count ? this.indicateEffects[i] : null,
                TriggerArea = i < this.triggerAreas.Count ? this.triggerAreas[i] : null,
                CollisionLabel = i < this.hintLabels.Count ? this.hintLabels[i] : null,
                PlacedObject = i < this.placedObjects.Count ? this.placedObjects[i] : null,
                ArrowObject = i < this.arrowObjects.Count ? this.arrowObjects[i] : null,
                PathFollow = i < this.pathFollows.Count ? this.pathFollows[i] : null
            };
            this.InitializeSingleCubeConfig(config, i);
            this.cubeConfigs.Add(config);
        }
    }
    
    private void InitializeSingleCubeConfig(CubeObjectConfig config, int index) {
        if (config.Cube != null) {
            config.InitialCubePosition = config.Cube.GlobalPosition;
        }
        if (config.PlacedObject != null) {
            config.InitialPlacedObjectPosition = config.PlacedObject.GlobalPosition;
        }
        if (config.PathFollow != null) {
            config.InitialPathFollowProgress = config.PathFollow.ProgressRatio;
        }
        if (config.IndicateEffect != null) {
            config.IndicateEffect.Visible = false;
            if (config.TriggerArea == null) {
                config.TriggerArea = config.IndicateEffect.GetNodeOrNull<Area3D>("Area3D");
                if (config.TriggerArea == null) {
                    config.TriggerArea = config.IndicateEffect.FindChild("Area3D", true, false) as Area3D;
                }
            }
            if (config.CollisionLabel == null) {
                config.CollisionLabel = config.IndicateEffect.GetNodeOrNull<Label3D>("Label3D");
                if (config.CollisionLabel == null) {
                    config.CollisionLabel = config.IndicateEffect.FindChild("Label3D", true, false) as Label3D;
                }
            }
        }
        if (config.TriggerArea != null) {
            var currentConfig = config;
            config.TriggerArea.BodyEntered += (body) => OnTriggerAreaBodyEntered(body, currentConfig);
            config.TriggerArea.BodyExited += (body) => OnTriggerAreaBodyExited(body, currentConfig);
            config.TriggerArea.AreaEntered += (area) => OnTriggerAreaEntered(area, currentConfig);
            config.TriggerArea.AreaExited += (area) => OnTriggerAreaExited(area, currentConfig);
        } 
        if (config.CollisionLabel != null) {
            config.CollisionLabel.Visible = false;
        } 
        if (config.PlacedObject != null) {
            config.PlacedObject.Visible = false;
        }
        if (config.ArrowObject != null) {
            config.ArrowObject.Visible = false;
            bool useAreaInput = false;
            bool useRaycast = false;
            Area3D clickArea = null;
            if (config.ArrowObject is Area3D area) {
                clickArea = area;
            } else {
                clickArea = config.ArrowObject.FindChild("*", true, false) as Area3D;
            }
            if (clickArea != null) {
                clickArea.InputRayPickable = true;
                clickArea.Monitorable = true;
                clickArea.Monitoring = true;
                var currentConfig = config;
                clickArea.InputEvent += (camera, @event, position, normal, shapeIdx) => {
                    OnArrowInputEvent(camera, @event, position, normal, shapeIdx, currentConfig);
                };
            }
        }
    }
    
    private void InitializeDataBoard() {
        this.dataBoard = this.FindChild("DataBoard", true, false) as DataBoard;
        if (this.dataBoard == null) {
            var children = this.GetChildren();
            foreach (Node child in children) {
                if (child.Name.ToString().Contains("DataBoard", System.StringComparison.OrdinalIgnoreCase)) {
                    this.dataBoard = child as DataBoard;
                    break;
                }
            }
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
        foreach (var config in this.cubeConfigs) {
            this.HideIndicateEffect(config);
            this.SetArrowHover(config, false);
        }
        this.EnableAllCubesDrag();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        foreach (var config in this.cubeConfigs) {
            this.UpdateIndicateEffect(config);
        }
        if (base.isInteracting) {
            Vector2 mousePos = GetViewport().GetMousePosition();
            foreach (var config in this.cubeConfigs) {
                if (config.IsCubePlaced && config.ArrowObject != null && config.ArrowObject.Visible) {
                    var intersect = this.GetMouseIntersectForArrow(mousePos, config);
                    bool isHovering = intersect != null && this.IsClickOnArrow(intersect, config);
                    this.SetArrowHover(config, isHovering);
                }
            }
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed) {
            int placedCount = 0;
            foreach (var cfg in this.cubeConfigs) {
                if (cfg.IsCubePlaced && cfg.ArrowObject != null) {
                    placedCount++;
                }
            }
        }
        if (!base.isInteracting) {
            return;
        }
        if (@event is InputEventMouseButton mouseButton) {
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed && !mouseButton.IsEcho()) {
                var arrowConfigs = new List<(CubeObjectConfig config, float distance)>();
                foreach (var config in this.cubeConfigs) {
                    if (config.IsCubePlaced && config.ArrowObject != null && config.ArrowObject.Visible) {
                        var intersect = this.GetMouseIntersectForArrow(mouseButton.Position, config);
                        if (intersect != null && this.IsClickOnArrow(intersect, config)) {
                            float distance = 0f;
                            if (intersect.ContainsKey("position") && intersect.ContainsKey("collider")) {
                                var currentCamera = GetViewport().GetCamera3D();
                                if (currentCamera != null) {
                                    var hitPos = intersect["position"].As<Vector3>();
                                    var cameraPos = currentCamera.GlobalPosition;
                                    distance = cameraPos.DistanceTo(hitPos);
                                }
                            }
                            arrowConfigs.Add((config, distance));
                        }
                    }
                }
                if (arrowConfigs.Count > 0) {
                    arrowConfigs.Sort((a, b) => a.distance.CompareTo(b.distance));
                    this.OnArrowClicked(arrowConfigs[0].config);
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }
    }

    private void SetArrowHover(CubeObjectConfig config, bool hovered) {
        if (config.ArrowObject == null) return;
        float alpha = hovered ? this.arrowNormalAlpha : this.arrowHoverAlpha;
        if (config.ArrowObject is MeshInstance3D arrowMesh) {
            this.SetArrowAlpha(arrowMesh, alpha);
        } else {
            this.SetArrowAlphaRecursive(config.ArrowObject, alpha);
        }
    }
    
    private void SetArrowAlphaRecursive(Node3D node, float alpha) {
        if (node == null) return;
        if (node is MeshInstance3D meshInstance) {
            this.SetArrowAlpha(meshInstance, alpha);
        }
        if (node is MultiMeshInstance3D multiMeshInstance) {
            if (multiMeshInstance.MaterialOverride is BaseMaterial3D material) {
                var newMaterial = (BaseMaterial3D)material.Duplicate();
                newMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                alpha = Mathf.Clamp(alpha, 0f, 1f);
                var c = newMaterial.AlbedoColor;
                newMaterial.AlbedoColor = new Color(c.R, c.G, c.B, alpha);
                multiMeshInstance.MaterialOverride = newMaterial;
            }
        }
        foreach (Node child in node.GetChildren()) {
            if (child is Node3D child3D) {
                this.SetArrowAlphaRecursive(child3D, alpha);
            }
        }
    }

    private void SetArrowAlpha(MeshInstance3D arrowMesh, float alpha) {
        if (arrowMesh == null) return;
        BaseMaterial3D material = null;
        if (arrowMesh.MaterialOverride is BaseMaterial3D existingMaterial) {
            material = (BaseMaterial3D)existingMaterial.Duplicate();
        } else {
            Material baseMaterial = arrowMesh.MaterialOverride;
            if (baseMaterial == null && arrowMesh.Mesh != null && arrowMesh.Mesh.GetSurfaceCount() > 0) {
                baseMaterial = arrowMesh.Mesh.SurfaceGetMaterial(0);
            }
            if (baseMaterial is BaseMaterial3D bm3d) {
                material = (BaseMaterial3D)bm3d.Duplicate();
            } else {
                material = new StandardMaterial3D();
            }
        }
        if (material != null) {
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            alpha = Mathf.Clamp(alpha, 0f, 1f);
            var c = material.AlbedoColor;
            material.AlbedoColor = new Color(c.R, c.G, c.B, alpha);
            arrowMesh.MaterialOverride = material;
        }
    }

    private void UpdateIndicateEffect(CubeObjectConfig config) {
        if (config.Cube == null || config.IndicateEffect == null) {
            return;
        }
        if (config.IsCubePlaced) {
            this.HideIndicateEffect(config);
            return;
        }
        if (config.Cube.IsDragging) {
            this.ShowIndicateEffect(config);
        } else {
            this.HideIndicateEffect(config);
            if (config.IsCubeInTriggerArea && !config.IsCubePlaced) {
                this.OnCubePlaced(config);
            }
        }
    }    

    private void ShowIndicateEffect(CubeObjectConfig config) {
        if (config.IndicateEffect == null || config.Cube == null) {
            return;
        }
        if (!config.IndicateEffect.Visible) {
            config.IndicateEffect.Visible = true;
        }
    }

    private void HideIndicateEffect(CubeObjectConfig config) {
        if (config.IndicateEffect == null) {
            return;
        }
        if (config.IndicateEffect.Visible) {
            config.IndicateEffect.Visible = false;
        }
        this.HideCollisionLabel(config);
    }

    private void OnTriggerAreaBodyEntered(Node3D body, CubeObjectConfig config) {
        this.HandleTriggerAreaCollision(body, config);
    }

    private void OnTriggerAreaBodyExited(Node3D body, CubeObjectConfig config) {
        this.HandleTriggerAreaExit(body, config);
    }

    private void OnTriggerAreaEntered(Area3D area, CubeObjectConfig config) {
        this.HandleTriggerAreaCollision(area, config);
    }

    private void OnTriggerAreaExited(Area3D area, CubeObjectConfig config) {
        this.HandleTriggerAreaExit(area, config);
    }

    private void HandleTriggerAreaCollision(Node node, CubeObjectConfig config) {
        if (config.Cube == null || config.IsCubePlaced) {
            return;
        }
        if (this.IsNodePartOfCube(node, config)) {
            config.IsCubeInTriggerArea = true;
            if (config.Cube.IsDragging) {
                this.ShowCollisionLabel(config);
            }
        }
    }

    private void HandleTriggerAreaExit(Node node, CubeObjectConfig config) {
        if (config.Cube == null || config.IsCubePlaced) {
            return;
        }
        if (this.IsNodePartOfCube(node, config)) {
            config.IsCubeInTriggerArea = false;
            this.HideCollisionLabel(config);
        }
    }

    private bool IsNodePartOfCube(Node node, CubeObjectConfig config) {
        if (node == null) {
            return false;
        }
        if (node == config.Cube) {
            return true;
        }
        Node current = node;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == config.Cube) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private void ShowCollisionLabel(CubeObjectConfig config) {
        if (config.CollisionLabel != null) {
            config.CollisionLabel.Visible = true;
        }
    }

    private void HideCollisionLabel(CubeObjectConfig config) {
        if (config.CollisionLabel != null) {
            config.CollisionLabel.Visible = false;
        }
    }

    private void OnCubePlaced(CubeObjectConfig config) {
        if (config.Cube == null || config.IsCubePlaced) {
            return;
        }
        foreach (var otherConfig in this.cubeConfigs) {
            if (otherConfig != config && otherConfig.IsCubePlaced) {
                this.ResetCubeToInitialPosition(otherConfig);
            }
        }
        config.IsCubePlaced = true;
        if (config.Cube != null) {
            config.Cube.Visible = false;
        }
        if (config.PlacedObject != null) {
            config.PlacedObject.Visible = true;
        }
        if (config.ArrowObject != null) {
            config.ArrowObject.Visible = true;
            this.SetArrowHover(config, false);
            this.DebugArrowClickability(config);
        } 
        this.HideIndicateEffect(config);
        this.HideCollisionLabel(config);
        config.IsCubeInTriggerArea = false;
        this.DisableOtherCubesDrag(config);
    }
    
    private void ResetCubeToInitialPosition(CubeObjectConfig config) {
        if (config.MoveTween != null && config.MoveTween.IsValid()) {
            config.MoveTween.Kill();
            config.MoveTween = null;
        }
        if (config.PathFollow != null) {
            config.PathFollow.ProgressRatio = config.InitialPathFollowProgress;
        }
        if (config.PlacedObject != null) {
            config.PlacedObject.GlobalPosition = config.InitialPlacedObjectPosition;
            config.PlacedObject.Visible = false;
        }
        if (config.Cube != null) {
            config.Cube.GlobalPosition = config.InitialCubePosition;
            config.Cube.Visible = true;
        }
        if (config.ArrowObject != null) {
            config.ArrowObject.Visible = false;
            this.SetArrowHover(config, false);
        }
        config.IsCubePlaced = false;
        config.IsCubeInTriggerArea = false;
    }
    
    private void DisableOtherCubesDrag(CubeObjectConfig placedConfig) {
        foreach (var config in this.cubeConfigs) {
            if (config != placedConfig && config.Cube != null) {
                config.Cube.StopDragging();
                config.Cube.IsDraggable = false;
            }
        }
    }
    
    private void EnableAllCubesDrag() {
        foreach (var config in this.cubeConfigs) {
            if (config.Cube != null) {
                config.Cube.StopDragging();
                config.Cube.IsDraggable = true;
            }
        }
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
        foreach (var cfg in this.cubeConfigs) {
            if (cfg.TriggerArea != null) {
                excludeList.Add(cfg.TriggerArea.GetRid());
            }
        }
        if (excludeList.Count > 0) {
            query.Exclude = excludeList;
        }
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        return result;
    }
    
    private Dictionary GetMouseIntersectForArrow(Vector2 mousePos, CubeObjectConfig targetConfig) {
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
        foreach (var cfg in this.cubeConfigs) {
            if (cfg.TriggerArea != null) {
                excludeList.Add(cfg.TriggerArea.GetRid());
            }
        }
        foreach (var cfg in this.cubeConfigs) {
            if (cfg != targetConfig && cfg.ArrowObject != null) {
                this.AddCollisionRidsToExclude(cfg.ArrowObject, excludeList);
            }
        }
        if (excludeList.Count > 0) {
            query.Exclude = excludeList;
        }
        var spaceState = GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(query);
        return result;
    }
    
    private void AddCollisionRidsToExclude(Node node, Godot.Collections.Array<Rid> excludeList) {
        if (node == null) return;
        if (node is CollisionObject3D collisionObj) {
            excludeList.Add(collisionObj.GetRid());
        }
        foreach (Node child in node.GetChildren()) {
            this.AddCollisionRidsToExclude(child, excludeList);
        }
    }

    private bool IsClickOnArrow(Dictionary intersect, CubeObjectConfig config) {
        if (intersect == null || !intersect.ContainsKey("collider") || config.ArrowObject == null) {
            return false;
        }
        var colliderVariant = intersect["collider"];
        var collider = colliderVariant.As<Node>();
        if (collider == null) {
            return false;
        }
        if (collider == config.ArrowObject) {
            return true;
        }
        Node current = collider;
        int depth = 0;
        const int maxDepth = 20;
        while (current != null && depth < maxDepth) {
            if (current == config.ArrowObject) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }
    
    private bool IsDescendantOf(Node node, Node potentialAncestor) {
        if (node == null || potentialAncestor == null) {
            return false;
        }
        Node current = node;
        int depth = 0;
        const int maxDepth = 15;
        while (current != null && depth < maxDepth) {
            if (current == potentialAncestor) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private void DebugArrowClickability(CubeObjectConfig config) {
        if (config.ArrowObject == null) return;
        this.PrintNodeTree(config.ArrowObject, 8);
        Area3D area = null;
        if (config.ArrowObject is Area3D a) {
            area = a;
        } else {
            area = config.ArrowObject.FindChild("*", true, false) as Area3D;
        }
        if (area != null) {
            var shape = area.FindChild("*", true, false) as CollisionShape3D;
        } else {
            var body = config.ArrowObject.FindChild("*", true, false) as StaticBody3D;
            if (body != null) {
                var shape = body.FindChild("*", true, false) as CollisionShape3D;
            }
        }
    }
    
    private void PrintNodeTree(Node node, int indent, int depth = 0, int maxDepth = 3) {
        if (depth > maxDepth) return;
        foreach (Node child in node.GetChildren()) {
            PrintNodeTree(child, indent, depth + 1, maxDepth);
        }
    }
    
    private void OnArrowInputEvent(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx, CubeObjectConfig config) {
        if (!base.isInteracting) {
            return;
        }
        if (@event is InputEventMouseButton mouseButton) {
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed && !mouseButton.IsEcho()) {
                this.OnArrowClicked(config);
            }
        }
    }
    
    private void OnArrowClicked(CubeObjectConfig config) {
        if (config.PathFollow == null) {
            return;
        }
        if (config.MoveTween != null && config.MoveTween.IsValid()) {
            config.MoveTween.Kill();
        }
        config.MoveTween = CreateTween();
        config.MoveTween.TweenProperty(config.PathFollow, "progress_ratio", 1.0f, this.moveDuration);
        config.MoveTween.TweenCallback(Callable.From(() => this.OnObjectMoveCompleted(config)));
        if (config.ArrowObject != null) {
            config.ArrowObject.Visible = false;
            this.SetArrowHover(config, false);
        }
        this.EnableAllCubesDrag();
    }
    
    private void OnObjectMoveCompleted(CubeObjectConfig config) {
        this.experimentCount++;
        float time = this.moveDuration;
        float distance = this.CalculatePathDistance(config);
        ExperimentData data = new ExperimentData(this.experimentCount, time, distance);
        config.ExperimentData = data;
        this.experimentDataList.Add(data);
        this.UpdateDataBoardDisplay();
    }
    
    private float CalculatePathDistance(CubeObjectConfig config) {
        if (config.PathFollow == null || config.PathFollow.GetParent() is not Path3D path3D) {
            return 0f;
        }
        var curve = path3D.Curve;
        if (curve == null) {
            return 0f;
        }
        return curve.GetBakedLength();
    }
    
    private void UpdateDataBoardDisplay() {
        if (this.dataBoard == null) {
            return;
        }
        var label = this.dataBoard.GetNodeOrNull<Label3D>("Label3D");
        if (label == null) {
            label = this.dataBoard.FindChild("Label3D", true, false) as Label3D;
        }
        if (label != null) {
            string displayText = "实验数据记录:\n";
            foreach (var data in this.experimentDataList) {
                displayText += $"{data}\n";
            }
            label.Text = displayText;
            label.Visible = true;
        }
    }
}
