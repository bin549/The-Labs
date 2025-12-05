using Godot;

public class ExperimentBuilder {
    private ExperimentSetupHelper helper;

    private ExperimentBuilder(Node3D root, string experimentName) {
        helper = new ExperimentSetupHelper(root);
        helper.CreateBasicExperiment(experimentName);
    }

    public static ExperimentBuilder Create(Node3D root, string experimentName = "实验") {
        return new ExperimentBuilder(root, experimentName);
    }

    public ExperimentBuilder AddItem(string name, string type, Vector3 position, Color? color = null) {
        helper.AddPlacableItem(name, type, position, color);
        return this;
    }

    public ExperimentBuilder AddAcid(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.ACID, position);
        return this;
    }

    public ExperimentBuilder AddBase(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.BASE, position);
        return this;
    }

    public ExperimentBuilder AddMetal(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.METAL, position);
        return this;
    }
`
    public ExperimentBuilder AddWater(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.WATER, position);
        return this;
    }

    public ExperimentBuilder AddSodium(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.SODIUM, position);
        return this;
    }

    public ExperimentBuilder AddFire(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.FIRE, position);
        return this;
    }

    public ExperimentBuilder AddMagnet(string name, Vector3 position) {
        helper.AddPlacableItem(name, ItemTypePresets.MAGNET, position);
        return this;
    }

    public ExperimentBuilder WithPhenomenon(ExperimentPhenomenon phenomenon) {
        helper.AddPhenomenon(phenomenon);
        return this;
    }

    public ExperimentBuilder WithAcidBaseReaction() {
        helper.AddPhenomenon(PhenomenonPresets.CreateAcidBaseReaction());
        return this;
    }

    public ExperimentBuilder WithMetalAcidReaction() {
        helper.AddPhenomenon(PhenomenonPresets.CreateMetalAcidReaction());
        return this;
    }

    public ExperimentBuilder WithSodiumWaterReaction() {
        helper.AddPhenomenon(PhenomenonPresets.CreateSodiumWaterReaction());
        return this;
    }

    public ExperimentBuilder WithCombustion() {
        helper.AddPhenomenon(PhenomenonPresets.CreateCombustionReaction());
        return this;
    }

    public ExperimentBuilder WithMagnetization() {
        helper.AddPhenomenon(PhenomenonPresets.CreateMagnetizationPhenomenon());
        return this;
    }

    public void Build() {
        helper.Finalize();
    }
}