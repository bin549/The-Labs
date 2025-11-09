using Godot;
using PhantomCamera;

public partial class LabItem : Interactable {
    private PhantomCamera3D phantomCam;

    public override void _Ready() {
        base._Ready();
        this.InitPhantomCamera();
		this.EnsureCollisions();
    }

    private void InitPhantomCamera() {
        var phantomCamNode = GetNodeOrNull<Node3D>("PhantomCamera3D");
        if (phantomCamNode != null) this.phantomCam = phantomCamNode.AsPhantomCamera3D();
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        this.SetOutlineActive(true);
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 999;
        }
    }

    public override void ExitInteraction() {
        this.SetOutlineActive(false);
        base.ExitInteraction();
        if (this.phantomCam != null) {
            this.phantomCam.Priority = 1;
        }
        base.gameManager.SetCurrentInteractable(null);
    }

	private void EnsureCollisions() {
		// 如果已经有任意 CollisionObject3D，认为已配置，不再自动生成
		if (HasAnyCollider(this)) return;

		// 为场景中所有 MeshInstance3D 生成静态三角网格碰撞
		foreach (var meshInstance in GetTreeMeshes(this)) {
			if (meshInstance.Mesh == null) continue;
			var shape = meshInstance.Mesh.CreateTrimeshShape();
			if (shape == null) continue;

			var staticBody = new StaticBody3D();
			staticBody.Name = $"{meshInstance.Name}_AutoStaticBody";
			meshInstance.AddChild(staticBody);

			var collisionShape = new CollisionShape3D();
			collisionShape.Shape = shape;
			staticBody.AddChild(collisionShape);
		}
	}

	private static bool HasAnyCollider(Node node) {
		if (node is CollisionObject3D) return true;
		foreach (Node child in node.GetChildren()) {
			if (HasAnyCollider(child)) return true;
		}
		return false;
	}

	private static System.Collections.Generic.IEnumerable<MeshInstance3D> GetTreeMeshes(Node node) {
		if (node is MeshInstance3D mi) yield return mi;
		foreach (Node child in node.GetChildren()) {
			foreach (var sub in GetTreeMeshes(child)) yield return sub;
		}
	}
}
