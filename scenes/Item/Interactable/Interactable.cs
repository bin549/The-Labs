using Godot;
using PhantomCamera;
using System.Collections.Generic;

public partial class Interactable : Node3D {
    [Export] protected string DisplayName { get; set; } = "物体";
    [Export] protected string ActionName { get; set; } = "交互";
    [Export] public bool LockPlayerControl { get; set; } = true;
    [Export] protected ShaderMaterial outlineMat;
    [Export] protected float outlineSize = 1.05f;
    [Export] public Godot.Collections.Array<NodePath> OutlineTargetPaths { get; set; } = new();
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
	[Export] public Godot.Collections.Array<DialogueEntry> Dialogues { get; set; } = new();
    protected bool isFocus = false;
    protected bool isInteracting = false;
    [Export] protected GameManager gameManager;
	private AudioStreamPlayer dialogueAudioPlayer;
	private int currentDialogueIndex = 0;
	private static Label dialogueLabel;
	private Timer dialogueTimer;
    private readonly List<GeometryInstance3D> outlineTargets = new();
    private readonly Dictionary<GeometryInstance3D, Material> originalOverlays = new();
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

    public override void _Ready() {
        this.CacheOutlineTargets();
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
		if (useCurvedArrow) {
			UpdateCurvedArrow();
			if (lineRoot != null) lineRoot.Visible = false;
		} else {
			if (curveRoot != null) curveRoot.Visible = false;
			this.UpdateLineSegment();
		}
        if (Input.IsActionJustPressed("interact") && this.isFocus && (this.gameManager == null || !this.gameManager.IsBusy)) {
            this.EnterInteraction();
        }
    }

    public virtual void EnterInteraction() {
        if (this.gameManager != null && LockPlayerControl) {
            this.gameManager.SetCurrentInteractable(this);
        } else if (this.gameManager == null) {
            GD.PushWarning($"{Name}: GameManager 未绑定，交互状态无法同步。");
        }
        if (LockPlayerControl) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
		this.PlayDialogue(true);
    }

    public virtual void ExitInteraction() {
		this.PlayDialogue(false);
		this.HideDialogue();
		if (this.gameManager != null && LockPlayerControl) {
			this.gameManager.SetCurrentInteractable(null);
		}
		if (LockPlayerControl) {
			Input.MouseMode = Input.MouseModeEnum.Captured;
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

    private void ApplyOutline(bool enable) {
        if (this.outlineMat == null) return;
        this.SetOutlineActive(enable);
    }

    protected void SetOutlineActive(bool enable) {
        if (this.outlineMat == null || this.outlineTargets.Count == 0) return;
        foreach (var instance in this.outlineTargets) {
            if (!GodotObject.IsInstanceValid(instance)) continue;
            if (!this.originalOverlays.ContainsKey(instance)) {
                this.originalOverlays[instance] = instance.MaterialOverlay;
            }
            instance.MaterialOverlay = enable ? this.outlineMat : this.originalOverlays[instance];
        }
        if (this.outlineMat != null) {
            this.outlineMat.SetShaderParameter("size", enable ? outlineSize : 0.0f);
        }
    }

    private void CacheOutlineTargets() {
        this.outlineTargets.Clear();
        this.originalOverlays.Clear();
        if (OutlineTargetPaths == null || OutlineTargetPaths.Count == 0) {
            GD.PushWarning($"{Name}: 未设置 OutlineTargetPaths，无法应用描边。");
            return;
        }
        foreach (var path in OutlineTargetPaths) {
            if (path.ToString() == string.Empty) continue;
            var instance = GetNodeOrNull<GeometryInstance3D>(path);
            if (instance != null) {
                this.outlineTargets.Add(instance);
                this.originalOverlays[instance] = instance.MaterialOverlay;
            } else {
                GD.PushWarning($"{Name}: 未找到描边目标节点 {path}");
            }
        }
    }

    protected void ResolveGameManager() {
        if (gameManager != null && GodotObject.IsInstanceValid(gameManager)) return;
        gameManager = GetTree().Root.GetNodeOrNull<GameManager>("GameManager") ??
            GetTree().Root.FindChild("GameManager", true, false) as GameManager;
        if (gameManager == null) {
            GD.PushWarning($"{Name}: 未找到 GameManager，某些交互功能可能不可用。");
        }
    }

	private void InitLineSegment() {
		var parent = this.lineNode ?? (Node3D)this;
		lineRoot = new Node3D();
		lineRoot.Name = "AutoLine";
		parent.AddChild(lineRoot);
		lineMeshInstance = new MeshInstance3D();
		lineMeshInstance.Name = "AutoLineMesh";
		lineBoxMesh = new BoxMesh();
		lineBoxMesh.Size = new Vector3(lineThickness * 2.0f, lineThickness * 2.0f, 1.0f);
		lineMeshInstance.Mesh = lineBoxMesh;
		lineMaterial = new StandardMaterial3D();
		lineMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		lineMaterial.AlbedoColor = lineColor;
		lineMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		lineMaterial.EmissionEnabled = true;
		lineMaterial.Emission = lineColor;
		lineMaterial.EmissionEnergyMultiplier = lineGlowStrength;
		lineMeshInstance.MaterialOverride = lineMaterial;
		lineMeshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		lineRoot.AddChild(lineMeshInstance);
	}

	private void UpdateLineSegment() {
		var a = GetNodeOrNull<Node3D>(linePointAPath);
		var b = GetNodeOrNull<Node3D>(linePointBPath);
		if (lineRoot == null || lineMeshInstance == null) return;
		if (a == null || b == null) {
			lineRoot.Visible = false;
			return;
		}
		Vector3 start = a.GlobalTransform.Origin;
		Vector3 end = b.GlobalTransform.Origin;
		Vector3 dir = end - start;
		float len = dir.Length();
		if (len < 0.001f) {
			lineRoot.Visible = false;
			return;
		}
		lineRoot.Visible = true;
		Vector3 mid = (start + end) * 0.5f;
		lineRoot.GlobalPosition = mid;
		lineRoot.LookAt(mid + dir, Vector3.Up);
		lineMeshInstance.Scale = new Vector3(1.0f, 1.0f, len);
		if (lineBoxMesh != null) {
			lineBoxMesh.Size = new Vector3(lineThickness * 2.0f, lineThickness * 2.0f, 1.0f);
		}
		if (lineMaterial != null) {
			lineMaterial.AlbedoColor = lineColor;
			lineMaterial.Emission = lineColor;
			lineMaterial.EmissionEnergyMultiplier = lineGlowStrength;
		}
	}

	private void InitCurvedArrow() {
		var parent = this.lineNode ?? (Node3D)this;
		curveRoot = new Node3D();
		curveRoot.Name = "AutoCurvedArrow";
		parent.AddChild(curveRoot);
		curveBodyInstance = new MultiMeshInstance3D();
		curveBody = new MultiMesh();
		curveSegmentMesh = new BoxMesh();
		curveSegmentMesh.Size = new Vector3(lineThickness * curveThicknessMultiplier, 1.0f, lineThickness * curveThicknessMultiplier);
		curveBody.Mesh = curveSegmentMesh;
		curveBody.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		curveBodyInstance.Multimesh = curveBody;
		curveMaterial = new StandardMaterial3D();
		curveMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		curveMaterial.AlbedoColor = lineColor;
		curveMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		curveMaterial.EmissionEnabled = true;
		curveMaterial.Emission = lineColor;
		curveMaterial.EmissionEnergyMultiplier = lineGlowStrength;
		curveBodyInstance.MaterialOverride = curveMaterial;
		curveBodyInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		curveRoot.AddChild(curveBodyInstance);
		arrowHeadInstance = new MeshInstance3D();
		arrowConeMesh = new CylinderMesh();
		var arrowRadius = Mathf.Max(lineThickness * arrowThicknessMultiplier, 0.002f);
		var arrowHeight = Mathf.Max(lineThickness * arrowLengthMultiplier, arrowRadius * 2.0f);
		arrowConeMesh.TopRadius = 0.0f;
		arrowConeMesh.BottomRadius = arrowRadius;
		arrowConeMesh.Height = arrowHeight;
		arrowHeadInstance.Mesh = arrowConeMesh;
		arrowHeadInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		var headMat = new StandardMaterial3D();
		headMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		headMat.AlbedoColor = lineColor;
		headMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		headMat.EmissionEnabled = true;
		headMat.Emission = lineColor;
		headMat.EmissionEnergyMultiplier = lineGlowStrength;
		arrowHeadInstance.MaterialOverride = headMat;
		curveRoot.AddChild(arrowHeadInstance);
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
		if (curveRoot == null || curveBodyInstance == null) return;
		var a = GetNodeOrNull<Node3D>(linePointAPath);
		var b = GetNodeOrNull<Node3D>(linePointBPath);
		if (a == null || b == null) {
			curveRoot.Visible = false;
			return;
		}
		curveRoot.Visible = true;
		Vector3 start = a.GlobalTransform.Origin;
		Vector3 end = b.GlobalTransform.Origin;
		Vector3 mid = (start + end) * 0.5f;
		Vector3 control = mid + Vector3.Up * bendHeight;
		int segs = Mathf.Clamp(segmentCount, 2, 256);
		curveBody.InstanceCount = segs;
		float dt = 1.0f / segs;
		var up = Vector3.Up;
		var invRoot = curveRoot.GlobalTransform.AffineInverse();
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
			var scaled = basis.Scaled(new Vector3(lineThickness, Mathf.Max(len, 0.0001f), lineThickness));
			var globalXform = new Transform3D(scaled, segMid);
			curveBody.SetInstanceTransform(i, invRoot * globalXform);
		}
		if (curveSegmentMesh != null) {
			curveSegmentMesh.Size = new Vector3(lineThickness * curveThicknessMultiplier, 1.0f, lineThickness * curveThicknessMultiplier);
		}
		if (curveMaterial != null) {
			curveMaterial.AlbedoColor = lineColor;
			curveMaterial.Emission = lineColor;
			curveMaterial.EmissionEnergyMultiplier = lineGlowStrength;
		}
		var tangent = BezierQuadraticTangent(start, control, end, 0.95f);
		if (tangent.Length() < 0.0001f) tangent = end - start;
		var headBasis = BuildBasisYAligned(tangent, up);
		var globalHead = new Transform3D(headBasis, end);
		arrowHeadInstance.GlobalTransform = globalHead;
		if (arrowConeMesh != null) {
			var arrowRadius = Mathf.Max(lineThickness * arrowThicknessMultiplier, 0.002f);
			var arrowHeight = Mathf.Max(lineThickness * arrowLengthMultiplier, arrowRadius * 2.0f);
			arrowConeMesh.TopRadius = 0.0f;
			arrowConeMesh.BottomRadius = arrowRadius;
			arrowConeMesh.Height = arrowHeight;
		}
	}

	private void InitDialoguePlayer() {
		dialogueAudioPlayer = new AudioStreamPlayer();
		dialogueAudioPlayer.Name = "DialogueAudioPlayer";
		dialogueAudioPlayer.Bus = "Master";
		AddChild(dialogueAudioPlayer);
		dialogueTimer = new Timer();
		dialogueTimer.Name = "DialogueTimer";
		dialogueTimer.OneShot = true;
		dialogueTimer.Timeout += OnDialogueTimeout;
		AddChild(dialogueTimer);
	}

	private void PlayDialogue(bool isEnterInteraction) {
		if (Dialogues == null || Dialogues.Count == 0) return;
		DialogueEntry dialogue = null;
		for (int i = 0; i < Dialogues.Count; i++) {
			var entry = Dialogues[i];
			if (entry != null && entry.PlayBeforeInteraction == isEnterInteraction) {
				dialogue = entry;
				currentDialogueIndex = i;
				break;
			}
		}
		if (dialogue == null) return;
		if (dialogue.Audio != null && dialogueAudioPlayer != null) {
			dialogueAudioPlayer.Stream = dialogue.Audio;
			dialogueAudioPlayer.Play();
		}
		if (!string.IsNullOrEmpty(dialogue.Text)) {
			ShowDialogue(dialogue.Text);
			GD.Print($"[{DisplayName}]: {dialogue.Text}");
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
		if (dialogue != null && dialogue.Duration > 0 && dialogueTimer != null) {
			dialogueTimer.Start(dialogue.Duration);
		}
	}

	private void HideDialogue() {
		if (dialogueTimer != null && !dialogueTimer.IsStopped()) {
			dialogueTimer.Stop();
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
	}

	private void ResolveDialogueLabel() {
		if (dialogueLabel != null) return;
		var player = GetTree().Root.FindChild("Player", true, false);
		if (player != null) {
			dialogueLabel = player.FindChild("DialogueLabel", true, false) as Label;
		}
		if (dialogueLabel == null) {
			GD.PushWarning($"{Name}: 未找到 DialogueLabel，对话文本将无法显示在屏幕上。");
		}
	}

	public DialogueEntry GetCurrentDialogue() {
		if (Dialogues == null || Dialogues.Count == 0 || currentDialogueIndex < 0 || currentDialogueIndex >= Dialogues.Count) {
			return null;
		}
		return Dialogues[currentDialogueIndex];
	}

	public void StopDialogueAudio() {
		if (dialogueAudioPlayer != null && dialogueAudioPlayer.Playing) {
			dialogueAudioPlayer.Stop();
		}
	}
}
