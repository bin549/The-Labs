using Godot;

public partial class SwitchablePlacableItem : PlacableItem {
    [Export] private Node3D switchableNode;
    [Export] private MeshInstance3D switchableOutline;
    private bool isSwitched = false;
    public bool IsSwitched => this.isSwitched;

    public override void _Ready() {
        base._Ready();
        this.SwitchToNormal();
    }

    public void SwitchToNormal() {
        this.isSwitched = false;
        var meshField = typeof(PlacableItem).GetField("mesh", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mesh = meshField?.GetValue(this) as MeshInstance3D;
        if (mesh != null) {
            mesh.Visible = true;
        }
        if (this.switchableNode != null) {
            this.switchableNode.Visible = false;
        }
        if (this.switchableOutline != null) {
            this.switchableOutline.Visible = false;
        }
        this.UpdateOutlineVisibility();
    }

    public void SwitchToSwitched() {
        this.isSwitched = true;
        var meshField = typeof(PlacableItem).GetField("mesh", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mesh = meshField?.GetValue(this) as MeshInstance3D;
        if (mesh != null) {
            mesh.Visible = false;
        }
        if (this.switchableNode != null) {
            this.switchableNode.Visible = true;
        }
        if (this.switchableOutline != null) {
            this.switchableOutline.Visible = false; 
        }
        this.UpdateOutlineVisibility();
        this.SyncCurrentOutline();
    }

    public void ToggleState() {
        if (this.isSwitched) {
            this.SwitchToNormal();
        } else {
            this.SwitchToSwitched();
        }
    }

    public new void UpdateOutlineVisibility() {
        base.UpdateOutlineVisibility();
        this.SyncCurrentOutline();
    }

    private void SyncCurrentOutline() {
        var isHoveredField = typeof(PlacableItem).GetField("isHovered", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool isHovered = isHoveredField != null && (bool)(isHoveredField.GetValue(this) ?? false);
        if (!isHovered) {
            var outlineMeshField = typeof(PlacableItem).GetField("outlineMesh", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var outlineMesh = outlineMeshField?.GetValue(this) as MeshInstance3D;
            if (outlineMesh != null && outlineMesh.Visible) {
                isHovered = true;
            }
        }
        if (this.switchableOutline != null) {
            if (isHovered && this.IsNodeVisible(this.switchableOutline)) {
                this.switchableOutline.Visible = true;
            } else {
                this.switchableOutline.Visible = false;
            }
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        this.SyncCurrentOutline();
    }

    private bool IsNodeVisible(Node3D node) {
        if (node == null) return false;
        Node current = node;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current is Node3D node3d && !node3d.Visible) {
                return false;
            }
            current = current.GetParent();
            depth++;
        }
        return true;
    }
}

