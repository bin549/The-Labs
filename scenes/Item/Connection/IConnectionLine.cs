using Godot;

public interface IConnectionLine {
    ConnectableNode StartNode { get; }
    ConnectableNode EndNode { get; }
    void Initialize(ConnectableNode startNode, ConnectableNode endNode);
    void OnHoverEnter();
    void OnHoverExit();
    void Destroy();
}
