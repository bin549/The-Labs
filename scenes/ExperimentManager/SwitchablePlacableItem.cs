using Godot;

/// <summary>
/// 可切换状态的 PlacableItem，支持在不同状态之间切换显示不同的节点
/// 在原有 PlacableItem 功能基础上添加状态切换功能
/// </summary>
public partial class SwitchablePlacableItem : PlacableItem {
    // 可切换的节点和轮廓
    [Export] private Node3D switchableNode;
    [Export] private MeshInstance3D switchableOutline;
    
    private bool isSwitched = false;

    public bool IsSwitched => this.isSwitched;

    public override void _Ready() {
        base._Ready();
        // 初始化：隐藏可切换节点
        this.SwitchToNormal();
    }

    /// <summary>
    /// 切换到正常状态（显示主 mesh，隐藏可切换节点）
    /// </summary>
    public void SwitchToNormal() {
        this.isSwitched = false;
        
        // 显示主 mesh（通过反射获取父类的 mesh）
        var meshField = typeof(PlacableItem).GetField("mesh", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mesh = meshField?.GetValue(this) as MeshInstance3D;
        if (mesh != null) {
            mesh.Visible = true;
        }
        
        // 隐藏可切换节点
        if (this.switchableNode != null) {
            this.switchableNode.Visible = false;
        }
        if (this.switchableOutline != null) {
            this.switchableOutline.Visible = false;
        }
        
        // 更新 outline 显示
        this.UpdateOutlineVisibility();
    }

    /// <summary>
    /// 切换到切换状态（隐藏主 mesh，显示可切换节点）
    /// </summary>
    public void SwitchToSwitched() {
        this.isSwitched = true;
        
        // 隐藏主 mesh（通过反射获取父类的 mesh）
        var meshField = typeof(PlacableItem).GetField("mesh", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mesh = meshField?.GetValue(this) as MeshInstance3D;
        if (mesh != null) {
            mesh.Visible = false;
        }
        
        // 显示可切换节点
        if (this.switchableNode != null) {
            this.switchableNode.Visible = true;
        }
        if (this.switchableOutline != null) {
            this.switchableOutline.Visible = false; // outline 由 hover 状态控制
        }
        
        // 更新 outline 显示
        this.UpdateOutlineVisibility();
        // 同步当前状态的 outline
        this.SyncCurrentOutline();
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void ToggleState() {
        if (this.isSwitched) {
            this.SwitchToNormal();
        } else {
            this.SwitchToSwitched();
        }
    }

    /// <summary>
    /// 重写 UpdateOutlineVisibility 以支持状态切换时的 outline 更新
    /// </summary>
    public new void UpdateOutlineVisibility() {
        // 先调用父类的方法，更新原有的 outlineMesh 和 outlineMeshs
        base.UpdateOutlineVisibility();
        // 然后同步当前状态的 outline
        this.SyncCurrentOutline();
    }

    /// <summary>
    /// 同步当前状态的 outline 显示（根据父类的 hover 状态）
    /// </summary>
    private void SyncCurrentOutline() {
        // 通过反射获取父类的 isHovered 字段来判断 hover 状态
        var isHoveredField = typeof(PlacableItem).GetField("isHovered", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool isHovered = isHoveredField != null && (bool)(isHoveredField.GetValue(this) ?? false);
        
        // 或者通过检查父类的 outlineMesh 可见性来判断
        if (!isHovered) {
            // 尝试通过检查 outlineMesh 的可见性
            var outlineMeshField = typeof(PlacableItem).GetField("outlineMesh", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var outlineMesh = outlineMeshField?.GetValue(this) as MeshInstance3D;
            if (outlineMesh != null && outlineMesh.Visible) {
                isHovered = true;
            }
        }
        
        // 根据 hover 状态显示/隐藏可切换节点的 outline
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
        // 持续同步当前状态的 outline 显示
        this.SyncCurrentOutline();
    }

    private bool IsNodeVisible(Node3D node) {
        if (node == null) return false;
        // 检查节点本身及其所有父节点是否可见
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

