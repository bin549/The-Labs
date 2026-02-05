using Godot;
using System;

public partial class World : Node3D {
    public override void _Ready() {
        var simpleGrass = GetNode<Node>("/root/SimpleGrass");
        simpleGrass?.Call("set_interactive", true);
    }

    public override void _Process(double delta) {
    }
}