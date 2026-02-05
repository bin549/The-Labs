using Godot;

[GlobalClass]
public partial class DialogueEntry : Resource {
    [Export] public AudioStream Audio { get; set; }
    [Export(PropertyHint.MultilineText)] public string Text { get; set; } = "";
    [Export] public bool PlayBeforeInteraction { get; set; } = true;
    [Export] public float Duration { get; set; } = 3.0f;
}