using Godot;
using PhantomCamera;
using System.Collections.Generic;
using Godot.Collections;

public partial class Interactable : Node3D {
    [Export] protected string DisplayName { get; set; } = "物体";
    [Export] protected string ActionName { get; set; } = "交互";
    [Export] public bool lockPlayerControl { get; set; } = true;
    [Export] protected Label3D nameLabel;
    [Export] protected Node3D lineNode;
    [Export] public NodePath linePointAPath { get; set; } = new();
    [Export] public NodePath linePointBPath { get; set; } = new();
    [Export] public Color lineColor { get; set; } = Colors.Cyan;
    [Export] public float lineThickness { get; set; } = 0.035f;
    [Export] public float lineGlowStrength { get; set; } = 1.6f;
    [Export] public bool useCurvedArrow { get; set; } = true;
    [Export] public float bendHeight { get; set; } = 0.5f;
    [Export] public int segmentCount { get; set; } = 16;
    [Export] public bool useDashes { get; set; } = false;
    [Export] public float curveThicknessMultiplier { get; set; } = 3.0f;
    [Export] public float arrowThicknessMultiplier { get; set; } = 0.5f;
    [Export] public float arrowLengthMultiplier { get; set; } = 4.0f;
    [Export] public Array<DialogueEntry> Dialogues { get; set; } = new();
    protected bool isFocus = false;
    protected bool isInteracting = false;
    [Export] protected GameManager gameManager;
    private AudioStreamPlayer dialogueAudioPlayer;
    private int currentDialogueIndex = 0;
    private static Label dialogueLabel;
    private Timer dialogueTimer;
    private Node3D lineRoot;
    private MeshInstance3D lineMeshInstance;
    private BoxMesh lineBoxMesh;
    private StandardMaterial3D lineMaterial;
    private Node3D curveRoot;
    private MultiMeshInstance3D curveBodyInstance;
    private MultiMesh curveBody;
    private BoxMesh curveSegmentMesh;
    private MeshInstance3D arrowHeadInstance;
    private CylinderMesh arrowConeMesh;
    private StandardMaterial3D curveMaterial;
    [Export] private Array<MeshInstance3D> outlineMeshs;

    public override void _Ready() {
        this.InitLineSegment();
        this.InitCurvedArrow();
        this.ResolveGameManager();
        this.InitDialoguePlayer();
        this.ResolveDialogueLabel();
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
            this.nameLabel.Visible = true;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = true;
        }
    }

    public override void _Process(double delta) {
        if (this.useCurvedArrow) {
            this.UpdateCurvedArrow();
            if (this.lineRoot != null) this.lineRoot.Visible = false;
        } else {
            if (this.curveRoot != null) this.curveRoot.Visible = false;
            this.UpdateLineSegment();
        }
        if (Input.IsActionJustPressed("interact") && this.isFocus &&
            (this.gameManager == null || !this.gameManager.IsBusy)) {
            this.EnterInteraction();
        }
    }

    public virtual void EnterInteraction() {
        if (this.gameManager != null && this.lockPlayerControl) {
            this.gameManager.SetCurrentInteractable(this);
        }
        if (this.lockPlayerControl) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        if (this.nameLabel != null) {
            this.nameLabel.Visible = false;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = false;
        }
        this.ApplyOutline(false);
        this.PlayDialogue(true);
    }

    public virtual void ExitInteraction() {
        this.PlayDialogue(false);
        this.HideDialogue();
        this.ApplyOutline(true);
        if (this.gameManager != null && this.lockPlayerControl) {
            this.gameManager.SetCurrentInteractable(null);
        }
        if (this.lockPlayerControl) {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        if (this.nameLabel != null) {
            this.nameLabel.Visible = true;
        }
        if (this.lineNode != null) {
            this.lineNode.Visible = true;
        }
    }

    public virtual void OnFocusEnter() {
        this.ApplyOutline(true);
        if (this.nameLabel != null) {
            this.nameLabel.Text = $"[E] {ActionName}";
        }
        this.isFocus = true;
    }

    public virtual void OnFocusExit() {
        this.ApplyOutline(false);
        if (this.nameLabel != null) {
            this.nameLabel.Text = DisplayName;
        }
        this.isFocus = false;
    }

    protected virtual void Interact(Node3D interactor) {
    }

    protected void ApplyOutline(bool enable) {
        if (this.outlineMeshs == null) return;
        foreach (var mesh in this.outlineMeshs) {
            if (mesh != null && GodotObject.IsInstanceValid(mesh)) {
                mesh.Visible = enable;
            }
        }
    }

    protected void ResolveGameManager() {
        if (gameManager != null && GodotObject.IsInstanceValid(gameManager)) return;
        gameManager = GetTree().Root.GetNodeOrNull<GameManager>("GameManager") ??
                      GetTree().Root.FindChild("GameManager", true, false) as GameManager;
    }

    private void InitLineSegment() {
        var parent = this.lineNode ?? (Node3D)this;
        this.lineRoot = new Node3D();
        this.lineRoot.Name = "AutoLine";
        parent.AddChild(this.lineRoot);
        this.lineMeshInstance = new MeshInstance3D();
        this.lineMeshInstance.Name = "AutoLineMesh";
        this.lineBoxMesh = new BoxMesh();
        this.lineBoxMesh.Size = new Vector3(this.lineThickness * 2.0f, this.lineThickness * 2.0f, 1.0f);
        this.lineMeshInstance.Mesh = this.lineBoxMesh;
        this.lineMaterial = new StandardMaterial3D();
        this.lineMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        this.lineMaterial.AlbedoColor = lineColor;
        this.lineMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        this.lineMaterial.EmissionEnabled = true;
        this.lineMaterial.Emission = lineColor;
        this.lineMaterial.EmissionEnergyMultiplier = this.lineGlowStrength;
        this.lineMeshInstance.MaterialOverride = this.lineMaterial;
        this.lineMeshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        this.lineRoot.AddChild(this.lineMeshInstance);
    }

    private void UpdateLineSegment() {
        var a = GetNodeOrNull<Node3D>(linePointAPath);
        var b = GetNodeOrNull<Node3D>(linePointBPath);
        if (this.lineRoot == null || this.lineMeshInstance == null) return;
        if (a == null || b == null) {
            this.lineRoot.Visible = false;
            return;
        }
        Vector3 start = a.GlobalTransform.Origin;
        Vector3 end = b.GlobalTransform.Origin;
        Vector3 dir = end - start;
        float len = dir.Length();
        if (len < 0.001f) {
            this.lineRoot.Visible = false;
            return;
        }
        this.lineRoot.Visible = true;
        Vector3 mid = (start + end) * 0.5f;
        this.lineRoot.GlobalPosition = mid;
        this.lineRoot.LookAt(mid + dir, Vector3.Up);
        this.lineMeshInstance.Scale = new Vector3(1.0f, 1.0f, len);
        if (this.lineBoxMesh != null) {
            this.lineBoxMesh.Size = new Vector3(this.lineThickness * 2.0f, this.lineThickness * 2.0f, 1.0f);
        }
        if (this.lineMaterial != null) {
            this.lineMaterial.AlbedoColor = lineColor;
            this.lineMaterial.Emission = lineColor;
            this.lineMaterial.EmissionEnergyMultiplier = this.lineGlowStrength;
        }
    }

    private void InitCurvedArrow() {
        var parent = this.lineNode ?? (Node3D)this;
        this.curveRoot = new Node3D();
        this.curveRoot.Name = "AutoCurvedArrow";
        parent.AddChild(this.curveRoot);
        this.curveBodyInstance = new MultiMeshInstance3D();
        this.curveBody = new MultiMesh();
        this.curveSegmentMesh = new BoxMesh();
        this.curveSegmentMesh.Size = new Vector3(this.lineThickness * curveThicknessMultiplier, 1.0f,
            this.lineThickness * curveThicknessMultiplier);
        this.curveBody.Mesh = this.curveSegmentMesh;
        this.curveBody.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        this.curveBodyInstance.Multimesh = this.curveBody;
        this.curveMaterial = new StandardMaterial3D();
        this.curveMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        this.curveMaterial.AlbedoColor = lineColor;
        this.curveMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        this.curveMaterial.EmissionEnabled = true;
        this.curveMaterial.Emission = lineColor;
        this.curveMaterial.EmissionEnergyMultiplier = this.lineGlowStrength;
        this.curveBodyInstance.MaterialOverride = this.curveMaterial;
        this.curveBodyInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        this.curveRoot.AddChild(this.curveBodyInstance);
        this.arrowHeadInstance = new MeshInstance3D();
        this.arrowConeMesh = new CylinderMesh();
        var arrowRadius = Mathf.Max(this.lineThickness * this.arrowThicknessMultiplier, 0.002f);
        var arrowHeight = Mathf.Max(this.lineThickness * this.arrowLengthMultiplier, arrowRadius * 2.0f);
        this.arrowConeMesh.TopRadius = 0.0f;
        this.arrowConeMesh.BottomRadius = arrowRadius;
        this.arrowConeMesh.Height = arrowHeight;
        this.arrowHeadInstance.Mesh = this.arrowConeMesh;
        this.arrowHeadInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        var headMat = new StandardMaterial3D();
        headMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        headMat.AlbedoColor = lineColor;
        headMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        headMat.EmissionEnabled = true;
        headMat.Emission = lineColor;
        headMat.EmissionEnergyMultiplier = this.lineGlowStrength;
        this.arrowHeadInstance.MaterialOverride = headMat;
        this.curveRoot.AddChild(this.arrowHeadInstance);
    }

    private static Vector3 BezierQuadratic(Vector3 a, Vector3 c, Vector3 b, float t) {
        float u = 1.0f - t;
        return (u * u) * a + 2.0f * u * t * c + (t * t) * b;
    }

    private static Vector3 BezierQuadraticTangent(Vector3 a, Vector3 c, Vector3 b, float t) {
        float u = 1.0f - t;
        return 2.0f * u * (c - a) + 2.0f * t * (b - c);
    }

    private static Basis BuildBasisYAligned(Vector3 dir, Vector3 fallbackUp) {
        var yAxis = dir.Normalized();
        var up = fallbackUp;
        if (Mathf.Abs(yAxis.Dot(up)) > 0.98f) {
            up = Vector3.Right;
        }
        var xAxis = up.Cross(yAxis).Normalized();
        var zAxis = xAxis.Cross(yAxis).Normalized();
        return new Basis(xAxis, yAxis, zAxis);
    }

    private void UpdateCurvedArrow() {
        if (this.curveRoot == null || this.curveBodyInstance == null) return;
        var a = GetNodeOrNull<Node3D>(linePointAPath);
        var b = GetNodeOrNull<Node3D>(linePointBPath);
        if (a == null || b == null) {
            this.curveRoot.Visible = false;
            return;
        }
        this.curveRoot.Visible = true;
        Vector3 start = a.GlobalTransform.Origin;
        Vector3 end = b.GlobalTransform.Origin;
        Vector3 mid = (start + end) * 0.5f;
        Vector3 control = mid + Vector3.Up * bendHeight;
        int segs = Mathf.Clamp(segmentCount, 2, 256);
        this.curveBody.InstanceCount = segs;
        float dt = 1.0f / segs;
        var up = Vector3.Up;
        var invRoot = this.curveRoot.GlobalTransform.AffineInverse();
        for (int i = 0; i < segs; i++) {
            float t0 = i * dt;
            float t1 = (i + 1) * dt;
            Vector3 p0 = BezierQuadratic(start, control, end, t0);
            Vector3 p1 = BezierQuadratic(start, control, end, t1);
            Vector3 segDir = p1 - p0;
            float len = segDir.Length();
            if (useDashes && (i % 2 == 1)) {
                len = 0.0f;
            }
            Vector3 segMid = (p0 + p1) * 0.5f;
            var basis = len < 0.0001f ? Basis.Identity : BuildBasisYAligned(segDir, up);
            var scaled = basis.Scaled(new Vector3(this.lineThickness, Mathf.Max(len, 0.0001f), this.lineThickness));
            var globalXform = new Transform3D(scaled, segMid);
            this.curveBody.SetInstanceTransform(i, invRoot * globalXform);
        }
        if (this.curveSegmentMesh != null) {
            this.curveSegmentMesh.Size = new Vector3(this.lineThickness * curveThicknessMultiplier, 1.0f,
                this.lineThickness * curveThicknessMultiplier);
        }
        if (this.curveMaterial != null) {
            this.curveMaterial.AlbedoColor = lineColor;
            this.curveMaterial.Emission = lineColor;
            this.curveMaterial.EmissionEnergyMultiplier = this.lineGlowStrength;
        }
        var tangent = BezierQuadraticTangent(start, control, end, 0.95f);
        if (tangent.Length() < 0.0001f) tangent = end - start;
        var headBasis = BuildBasisYAligned(tangent, up);
        var globalHead = new Transform3D(headBasis, end);
        this.arrowHeadInstance.GlobalTransform = globalHead;
        if (this.arrowConeMesh != null) {
            var arrowRadius = Mathf.Max(this.lineThickness * this.arrowThicknessMultiplier, 0.002f);
            var arrowHeight = Mathf.Max(this.lineThickness * this.arrowLengthMultiplier, arrowRadius * 2.0f);
            this.arrowConeMesh.TopRadius = 0.0f;
            this.arrowConeMesh.BottomRadius = arrowRadius;
            this.arrowConeMesh.Height = arrowHeight;
        }
    }

    private void InitDialoguePlayer() {
        this.dialogueAudioPlayer = new AudioStreamPlayer();
        this.dialogueAudioPlayer.Name = "DialogueAudioPlayer";
        this.dialogueAudioPlayer.Bus = "Master";
        AddChild(this.dialogueAudioPlayer);
        this.dialogueTimer = new Timer();
        this.dialogueTimer.Name = "DialogueTimer";
        this.dialogueTimer.OneShot = true;
        this.dialogueTimer.Timeout += OnDialogueTimeout;
        AddChild(this.dialogueTimer);
    }

    private void PlayDialogue(bool isEnterInteraction) {
        if (this.Dialogues == null || this.Dialogues.Count == 0) return;
        var matchingDialogues = new List<int>();
        for (int i = 0; i < this.Dialogues.Count; i++) {
            var entry = this.Dialogues[i];
            if (entry != null && entry.PlayBeforeInteraction == isEnterInteraction) {
                matchingDialogues.Add(i);
            }
        }
        if (matchingDialogues.Count == 0) return;
        if (matchingDialogues.Count == 1) {
            this.currentDialogueIndex = matchingDialogues[0];
        } else {
            int currentPosInList = matchingDialogues.IndexOf(this.currentDialogueIndex);
            if (currentPosInList < 0) {
                this.currentDialogueIndex = matchingDialogues[0];
            } else {
                int nextPos = (currentPosInList + 1) % matchingDialogues.Count;
                this.currentDialogueIndex = matchingDialogues[nextPos];
            }
        }
        var dialogue = this.Dialogues[this.currentDialogueIndex];
        if (dialogue == null) return;
        if (dialogue.Audio != null && this.dialogueAudioPlayer != null) {
            this.dialogueAudioPlayer.Stream = dialogue.Audio;
            this.dialogueAudioPlayer.Play();
        }
        if (!string.IsNullOrEmpty(dialogue.Text)) {
            this.ShowDialogue(dialogue.Text);
        }
    }

    private void ShowDialogue(string text) {
        if (dialogueLabel != null) {
            dialogueLabel.Text = text;
            dialogueLabel.Visible = true;
            var panel = dialogueLabel.GetParent()?.GetParent() as Control;
            if (panel != null) {
                panel.Visible = true;
            }
        }
        var dialogue = GetCurrentDialogue();
        if (dialogue != null && dialogue.Duration > 0 && this.dialogueTimer != null) {
            this.dialogueTimer.Start(dialogue.Duration);
        }
    }

    private void HideDialogue() {
        if (this.dialogueTimer != null && !this.dialogueTimer.IsStopped()) {
            this.dialogueTimer.Stop();
        }
        if (dialogueLabel != null) {
            dialogueLabel.Visible = false;
            var panel = dialogueLabel.GetParent()?.GetParent() as Control;
            if (panel != null) {
                panel.Visible = false;
            }
        }
    }

    private void OnDialogueTimeout() {
        this.HideDialogue();
        this.OnDialogueFinished();
    }

    protected virtual void OnDialogueFinished() {
    }

    private void ResolveDialogueLabel() {
        if (dialogueLabel != null) return;
        var player = GetTree().Root.FindChild("Player", true, false);
        if (player != null) {
            dialogueLabel = player.FindChild("DialogueLabel", true, false) as Label;
        }
    }

    public DialogueEntry GetCurrentDialogue() {
        if (this.Dialogues == null || this.Dialogues.Count == 0 || this.currentDialogueIndex < 0 ||
            this.currentDialogueIndex >= this.Dialogues.Count) {
            return null;
        }
        return this.Dialogues[this.currentDialogueIndex];
    }

    public void StopDialogueAudio() {
        if (this.dialogueAudioPlayer != null && this.dialogueAudioPlayer.Playing) {
            this.dialogueAudioPlayer.Stop();
        }
    }
}