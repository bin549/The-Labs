using Godot;

public partial class ColorGrid : GridContainer {
    private PainterImage painterImage;

    public override void _Ready() {
        painterImage = GetNode<PainterImage>("../../PainterImage");
        foreach (Node child in GetChildren()) {
            if (child is ColorRect colorRect) {
                colorRect.GuiInput += (InputEvent inputEvent) => {
                    if (inputEvent is InputEventMouseButton mb) {
                        if (mb.Pressed && mb.ButtonIndex == MouseButton.Left) {
                            if (painterImage is null)
                                return;
                            painterImage.PaintColor = colorRect.Color;
                            painterImage.Set("paint_color", colorRect.Color);
                        }
                    }
                };
            }
        }
    }
}