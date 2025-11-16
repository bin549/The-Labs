using Godot;
using System;

public partial class World : Node3D {
    [Export]
    public bool CreateTestNodes { get; set; } = false;
    
    public override void _Ready() {
        var simpleGrass = GetNode<Node>("/root/SimpleGrass");
        simpleGrass?.Call("set_interactive", true);
        if (CreateTestNodes) {
            CallDeferred(nameof(SetupTestNodes));
        }
    }

    private void SetupTestNodes() {
        GD.Print("=== 开始创建连线测试节点 ===");
        var node1 = new ConnectableNode();
        node1.Name = "TestNode1";
        node1.Position = new Vector3(-2, 1.5f, -5);
        AddChild(node1);
        GD.Print($"✓ 创建测试节点: {node1.Name} at {node1.Position}");
        var node2 = new ConnectableNode();
        node2.Name = "TestNode2";
        node2.Position = new Vector3(2, 1.5f, -5);
        AddChild(node2);
        GD.Print($"✓ 创建测试节点: {node2.Name} at {node2.Position}");
        var node3 = new ConnectableNode();
        node3.Name = "TestNode3";
        node3.Position = new Vector3(0, 1.5f, -7);
        AddChild(node3);
        GD.Print($"✓ 创建测试节点: {node3.Name} at {node3.Position}");
        GD.Print("=== 测试节点创建完成 ===");
        GD.Print("提示：");
        GD.Print("  1. 左键点击节点进行选中（节点会变黄）");
        GD.Print("  2. 再次左键点击另一个节点创建连线");
        GD.Print("  3. 右键点击连线可以删除");
        GD.Print("  4. 鼠标悬停在连线上会变红");
        GD.Print("================================");
    }

    public override void _Process(double delta) {
    }
}
