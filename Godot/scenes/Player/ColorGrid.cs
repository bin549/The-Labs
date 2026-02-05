using Godot;

public partial class ColorGrid : GridContainer {
    private PainterImage painterImage;

    public override void _Ready() {
        this.painterImage = GetNode<PainterImage>("../../PainterImage");
        foreach (Node child in GetChildren()) {
            if (child is ColorRect colorRect) {
                colorRect.GuiInput += (InputEvent inputEvent) => {
                    if (inputEvent is InputEventMouseButton mb) {
                        if (mb.Pressed && mb.ButtonIndex == MouseButton.Left) {
                            if (this.painterImage is null)
                                return;
                            this.painterImage.PaintColor = colorRect.Color;
                            this.painterImage.Set("paint_color", colorRect.Color);
                        }
                    }
                };
            }
        }
    }
}
