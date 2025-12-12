using Godot;
using Godot.Collections;

public partial class InclinedPlanePlacableItem : PlacableItem {
	[Export] public InclinedPlaneExperimentItem itemType { get; set; } = InclinedPlaneExperimentItem.InclinedPlane;
	[Export] public Array<InclinedPlaneExperimentItem> collisionTargets { get; set; } = new();
	[Export] public AnimationPlayer animationPlayer { get; set; }
	[Export] public InclinedPlaneExperiment experiment { get; set; }

	public override void _Ready() {
		base._Ready();
		if (this.experiment == null) {
			Node current = GetParent();
			int depth = 0;
			const int maxDepth = 10;
			while (current != null && depth < maxDepth) {
				if (current is InclinedPlaneExperiment exp) {
					this.experiment = exp;
					break;
				}
				current = current.GetParent();
				depth++;
			}
		}
		if (this.experiment != null) {
			this.experiment.RegisterExperimentItem(this.itemType, this);
		}
		if (base.collisionArea != null) {
			base.collisionArea.BodyEntered += OnBodyEntered;
			base.collisionArea.AreaEntered += OnAreaEntered;
		}
	}
																					
	private void OnBodyEntered(Node3D body) {
		this.HandleCollision(body);
	}

	private void OnAreaEntered(Area3D area) {
		this.HandleCollision(area);
	}

	private void HandleCollision(Node otherNode) {
		if (otherNode == null) return;
		var otherItem = otherNode as InclinedPlanePlacableItem;
		if (otherItem == null) {
			Node current = otherNode;
			int depth = 0;
			const int maxDepth = 10;
			while (current != null && depth < maxDepth) {
				if (current is InclinedPlanePlacableItem item) {
					otherItem = item;
					break;
				}
				current = current.GetParent();
				depth++;
			}
		}
		if (otherItem == null) return;
		if (!this.collisionTargets.Contains(otherItem.itemType)) return;
		this.OnCollideWith(otherItem);
		this.experiment?.OnItemsCollided(this.itemType, otherItem.itemType, this, otherItem);
	}

	protected virtual void OnCollideWith(InclinedPlanePlacableItem other) {
		if (this.animationPlayer != null && !this.animationPlayer.IsPlaying()) {
			this.animationPlayer.Play("default");
		}
	}
}
