using Godot;

public partial class Page : Control {
    private string[] nouns = { "knight", "dragon", "forest", "castle", "moon", "star", "adventure", "treasure", "storm" };
    private string[] verbs = { "seeks", "fights", "journeys", "discovers", "challenges", "protects", "defends", "questions" };
    private string[] adjectives = { "brave", "mysterious", "ancient", "glowing", "forgotten", "fearsome", "legendary", "hidden" };
    private string[] adverbs = { "boldly", "mysteriously", "bravely", "fiercely", "quickly", "cautiously", "silently", "relentlessly" };

    private Label numberLabel;
    private RichTextLabel textLabel;

    public override void _Ready() {
        numberLabel = GetNode<Label>("Background/Number");
        textLabel = GetNode<RichTextLabel>("Background/Text");
    }

    public void SetNumber(int value) {
        numberLabel.Text = $"- {value} -";
        textLabel.Text = GeneratePlaceholderText(value);
    }

    private string GeneratePlaceholderText(int seed) {
        int nounIndex = PositiveModulo(seed, nouns.Length);
        int verbIndex = PositiveModulo(seed, verbs.Length);
        int adjectiveIndex = PositiveModulo(seed, adjectives.Length);
        int adverbIndex = PositiveModulo(seed, adverbs.Length);
        string randomNoun = nouns[nounIndex];
        string randomVerb = verbs[verbIndex];
        string randomAdjective = adjectives[adjectiveIndex];
        string randomAdverb = adverbs[adverbIndex];
        string sentence = $"The {randomAdjective} {randomNoun} {randomVerb} {randomAdverb}.";
        return sentence;
    }

    private int PositiveModulo(int value, int length) {
        int result = value % length;
        if (result < 0)
            result += length;
        return result;
    }
}
