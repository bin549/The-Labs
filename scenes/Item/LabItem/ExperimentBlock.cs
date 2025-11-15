using Godot;
using System;

public partial class ExperimentBlock : Node3D {
	[Export] public string BlockName { get; set; } = "物块";
	[Export] public float Mass { get; set; } = 1.0f;
	[Export] public Color BlockColor { get; set; } = Colors.Blue;
	[Export] public NodePath MeshPath { get; set; }
	[Signal]
	public delegate void OnBlockSelectedEventHandler(ExperimentBlock block);
	private MeshInstance3D mesh;
	private bool isDragging = false;
	private bool isHovered = false;
	private Vector3 dragOffset;
	private Camera3D camera;
	private StandardMaterial3D material;
	private Color originalColor;
	private Vector3 originalPosition;

	public override void _Ready() {
		this.ResolveMesh();
		this.originalPosition = GlobalPosition;
		this.camera = GetViewport().GetCamera3D();
		if (mesh != null) {
			if (mesh.GetActiveMaterial(0) is StandardMaterial3D mat) {
				material = (StandardMaterial3D)mat.Duplicate();
			} else {
				material = new StandardMaterial3D();
			}
			originalColor = material.AlbedoColor;
			material.AlbedoColor = BlockColor;
			mesh.SetSurfaceOverrideMaterial(0, material);
		}
	}

	public override void _Process(double delta) {
		if (isDragging && camera != null) {
			UpdateDragPosition();
		}
		if (material != null) {
			if (isHovered && !isDragging) {
				material.AlbedoColor = BlockColor.Lightened(0.3f);
				material.Emission = BlockColor;
				material.EmissionEnergyMultiplier = 0.5f;
			} else if (isDragging) {
				material.AlbedoColor = BlockColor.Lightened(0.5f);
				material.Emission = BlockColor;
				material.EmissionEnergyMultiplier = 1.0f;
			} else {
				material.AlbedoColor = BlockColor;
				material.Emission = Colors.Black;
				material.EmissionEnergyMultiplier = 0.0f;
			}
		}
	}

	public override void _Input(InputEvent @event) {
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
			GD.PushWarning($"{Name}: 未找到MeshInstance3D，创建默认立方体。");
			CreateDefaultMesh();
		}
	}

	private void CreateDefaultMesh() {
		mesh = new MeshInstance3D();
		mesh.Name = "Mesh";
		var boxMesh = new BoxMesh();
		boxMesh.Size = new Vector3(0.1f, 0.1f, 0.1f);
		mesh.Mesh = boxMesh;
		AddChild(mesh);
	}

	private void StartDrag() {
		isDragging = true;
		if (camera != null) {
			var mousePos = GetViewport().GetMousePosition();
			var from = camera.ProjectRayOrigin(mousePos);
			var to = from + camera.ProjectRayNormal(mousePos) * 1000;

			var spaceState = GetWorld3D().DirectSpaceState;
			var query = PhysicsRayQueryParameters3D.Create(from, to);
			var result = spaceState.IntersectRay(query);

			if (result.Count > 0 && result.ContainsKey("position")) {
				var hitPos = result["position"].AsVector3();
				dragOffset = GlobalPosition - hitPos;
			}
		}

		EmitSignal(SignalName.OnBlockSelected, this);
		GD.Print($"开始拖动物块：{BlockName}");
	}

	private void UpdateDragPosition() {
		var mousePos = GetViewport().GetMousePosition();
		var from = camera.ProjectRayOrigin(mousePos);
		var normal = camera.ProjectRayNormal(mousePos);
		float t = (originalPosition.Y - from.Y) / normal.Y;
		if (t > 0) {
			Vector3 newPos = from + normal * t + dragOffset;
			newPos.Y = originalPosition.Y;
			GlobalPosition = newPos;
		}
	}

	private void EndDrag() {
		isDragging = false;
		GD.Print($"结束拖动物块：{BlockName}");
	}

	public void OnMouseEnter() {
		isHovered = true;
	}

	public void OnMouseExit() {
		isHovered = false;
	}

	public void ResetPosition() {
		GlobalPosition = originalPosition;
	}

	public void _OnArea3DMouseEntered() {
		OnMouseEnter();
	}

	public void _OnArea3DMouseExited() {
		OnMouseExit();
	}
}
