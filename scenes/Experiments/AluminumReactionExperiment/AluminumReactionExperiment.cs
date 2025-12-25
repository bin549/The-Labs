using Godot;

public enum AluminumReactionExperimentStep {
    Step01,
    Step02,
    Step03,
    Step04,
    Step05,
    Step06,
    Step07,
    Step08
}

public enum AluminumReactionExperimentItem {
    Beaker,
    AluminumStrip,
    SodiumHydroxideSolution,
    TestTube,
    GasTube,
    WaterTank,
    DataBoard,
    Thermometer
}

public partial class AluminumReactionExperiment : StepExperimentLabItem<AluminumReactionExperimentStep, AluminumReactionExperimentItem> {
    [Export]
    protected override AluminumReactionExperimentStep currentStep { get; set; } = AluminumReactionExperimentStep.Step01;

    [ExportGroup("通用设置")]
    [Export] private AnimationPlayer animationPlayer;
    [Export] private Node3D fillObjects;
    [Export] private Node3D dropObjects;
    [Export] private float tweezersDragRotationAngle = 45.0f;
    private Transform3D sodiumHydroxideSolutionInitialTransform;
    private Transform3D tweezersInitialTransform;
    private Transform3D matchInitialTransform;
    private AluminumReactionExperimentStep stepWhenFillAnimationStarted = AluminumReactionExperimentStep.Step01;

    [ExportGroup("步骤 1-2: 氢氧化钠溶液")]
    [Export] private PlacableItem sodiumHydroxideSolution;
    [Export] private Node3D emptyReagent1;
    [Export] private Area3D triggerArea1;
    [Export] private Label3D collisionLabel1;
    [Export] private Node3D emptyReagent2;
    [Export] private Area3D triggerArea2;
    [Export] private Label3D collisionLabel2;
    private bool isSodiumHydroxideInArea = false;
    private bool isItemPlaced = false;
    private bool isStepTwoCollisionInitialized = false;

    [ExportGroup("步骤 3-4: 镊子和铝片")]
    [Export] private SwitchablePlacableItem tweezers;
    [Export] private Node3D aluminumStrip1;
    [Export] private Node3D aluminumStripInTube1;
    [Export] private Area3D triggerArea3;
    [Export] private Label3D collisionLabel3;
    [Export] private Area3D triggerArea3Reagent;
    [Export] private Label3D collisionLabel3Reagent;
    [Export] private Node3D aluminumStrip2;
    [Export] private Node3D aluminumStripInTube2;
    [Export] private Area3D triggerArea4;
    [Export] private Label3D collisionLabel4;
    [Export] private Area3D triggerArea4Reagent;
    [Export] private Label3D collisionLabel4Reagent;
    private bool isTweezersInArea3 = false;
    private bool isItemPlaced3 = false;
    private bool isStepThreeCollisionInitialized = false;
    private bool isTweezersInArea4 = false;
    private bool isItemPlaced4 = false;
    private bool isStepFourCollisionInitialized = false;
    private bool wasTweezersDragging = false;
    private bool isPickupAluminum = false;
    private bool isTweezersInReagentArea3 = false;
    private bool isTweezersInReagentArea4 = false;

    [ExportGroup("步骤 5: 火柴和木棍")]
    [Export] private SwitchablePlacableItem match;
    [Export] private Node3D matchbox;
    [Export] private Area3D triggerAreaMatchbox;
    [Export] private Label3D collisionLabelMatchbox;
    [Export] private SwitchablePlacableItem woodenStick;
    [Export] private Area3D triggerAreaWoodenStick;
    [Export] private Label3D collisionLabelWoodenStick;
    private bool isMatchInMatchboxArea = false;
    private bool isMatchLit = false;
    private bool wasMatchDragging = false;
    private bool isMatchInWoodenStickArea = false;
    private bool isWoodenStickLit = false;
    private bool isStepFiveCollisionInitialized = false;

    [ExportGroup("步骤 6-7: 试管检测")]
    [Export] private Area3D triggerAreaTestTube1;
    [Export] private Label3D collisionLabelTestTube1;
    [Export] private Area3D triggerAreaTestTube2;
    [Export] private Label3D collisionLabelTestTube2;
    private bool isWoodenStickInTestTube1Area = false;
    private bool isWoodenStickInTestTube2Area = false;
    private bool isTestTube1Detected = false;
    private bool isTestTube2Detected = false;
    private bool isStepSixCollisionInitialized = false;
    private bool isStepSevenCollisionInitialized = false;

    public override void _Ready() {
        base._Ready();
        this.InitializeStepHints();
        base.InitializeStepExperiment();
        this.SetupStepOneCollision();
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolutionInitialTransform = this.sodiumHydroxideSolution.GlobalTransform;
        }
        if (this.tweezers != null) {
            this.tweezersInitialTransform = this.tweezers.GlobalTransform;
        }
        if (this.match != null) {
            this.matchInitialTransform = this.match.GlobalTransform;
        }
        if (this.woodenStick != null) {
            this.woodenStick.SwitchToNormal();
            this.woodenStick.IsDraggable = false;
        }
        if (this.animationPlayer != null) {
            this.animationPlayer.AnimationFinished += OnAnimationFinished;
        }
    }

    private void SetupStepOneCollision() {
        this.isItemPlaced = false;
        if (this.collisionLabel1 != null) {
            this.collisionLabel1.Visible = false;
        }
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolution.StopDragging();
        }
        if (this.emptyReagent1 is PlacableItem emptyReagentItem) {
            emptyReagentItem.StopDragging();
        }
        if (this.triggerArea1 != null) {
            this.triggerArea1.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerArea1);
            this.triggerArea1.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea1);
            this.triggerArea1.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerArea1);
            this.triggerArea1.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea1);
        }
    }

    public override void EnterInteraction() {
        base.EnterInteraction();
        base.isInteracting = true;
        if (Input.MouseMode != Input.MouseModeEnum.Visible) {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void ExitInteraction() {
        base.isInteracting = false;
        base.ExitInteraction();
    }

    public override void _Input(InputEvent @event) {
        if (!base.isInteracting) return;
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (!base.isInteracting) {
            return;
        }
        if (this.currentStep == AluminumReactionExperimentStep.Step01) {
            this.UpdateCollisionLabel();
        } else if (this.currentStep == AluminumReactionExperimentStep.Step02) {
            if (!this.isStepTwoCollisionInitialized) {
                this.SetupStepTwoCollision();
                this.isStepTwoCollisionInitialized = true;
            }
            this.UpdateCollisionLabel();
        } else if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (!this.isStepThreeCollisionInitialized) {
                this.SetupStepThreeCollisionAluminum();
                this.isStepThreeCollisionInitialized = true;
            }
            this.UpdateAluminumCollisionLabel();
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (!this.isStepFourCollisionInitialized) {
                this.SetupStepFourCollision();
                this.isStepFourCollisionInitialized = true;
            }
            this.UpdateAluminumCollisionLabel();
        } else if (this.currentStep == AluminumReactionExperimentStep.Step05) {
            if (!this.isStepFiveCollisionInitialized) {
                this.SetupStepFiveCollision();
                this.isStepFiveCollisionInitialized = true;
            }
            this.UpdateMatchCollisionLabel();
            this.UpdateWoodenStickCollisionLabel();
        } else if (this.currentStep == AluminumReactionExperimentStep.Step06 ||
                   this.currentStep == AluminumReactionExperimentStep.Step07) {
            if (this.currentStep == AluminumReactionExperimentStep.Step06 && !this.isStepSixCollisionInitialized) {
                this.SetupStepSixCollision();
                this.isStepSixCollisionInitialized = true;
            } else if (this.currentStep == AluminumReactionExperimentStep.Step07 && !this.isStepSevenCollisionInitialized) {
                this.SetupStepSevenCollision();
                this.isStepSevenCollisionInitialized = true;
            }
            this.UpdateTestTubeCollisionLabel();
        }
    }

    private void UpdateCollisionLabel() {
        if (this.sodiumHydroxideSolution == null || this.isItemPlaced) {
            return;
        }
        if (this.sodiumHydroxideSolution.IsDragging) {
            this.ShowCollisionLabel(this.isSodiumHydroxideInArea);
        } else {
            if (this.isSodiumHydroxideInArea && !this.isItemPlaced) {
                this.OnItemPlaced();
            }
        }
    }

    private void UpdateAluminumCollisionLabel() {
        if (this.tweezers == null) {
            return;
        }
        bool isDragging = this.tweezers.IsDragging;
        if (isDragging && !this.wasTweezersDragging && !this.isPickupAluminum) {
            this.ApplyTweezersDragRotation();
        } else if (!isDragging && this.wasTweezersDragging) {
            this.RestoreTweezersRotation();
        }
        this.wasTweezersDragging = isDragging;
        if (this.isPickupAluminum) {
            bool isInReagentArea = false;
            bool isPlaced = false;
            if (this.currentStep == AluminumReactionExperimentStep.Step03) {
                isInReagentArea = this.isTweezersInReagentArea3;
                isPlaced = this.isItemPlaced3;
            } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
                isInReagentArea = this.isTweezersInReagentArea4;
                isPlaced = this.isItemPlaced4;
            }
            if (isDragging) {
                this.ShowReagentCollisionLabel(isInReagentArea);
            } else {
                if (isInReagentArea && !isPlaced) {
                    this.OnAluminumDroppedIntoReagent();
                }
            }
        } else {
            bool isInArea = false;
            bool isPlaced = false;
            if (this.currentStep == AluminumReactionExperimentStep.Step03) {
                isInArea = this.isTweezersInArea3;
                isPlaced = this.isItemPlaced3;
            } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
                isInArea = this.isTweezersInArea4;
                isPlaced = this.isItemPlaced4;
            }
            if (isPlaced) {
                return;
            }
            if (isDragging) {
                this.ShowAluminumCollisionLabel(isInArea);
            } else {
                if (isInArea && !isPlaced) {
                    this.OnAluminumPickedUp();
                }
            }
        }
    }

    private void ApplyTweezersDragRotation() {
        if (this.tweezers == null) {
            return;
        }
        Vector3 initialRotation = this.tweezersInitialTransform.Basis.GetEuler();
        Vector3 initialRotationDegrees = new Vector3(
            Mathf.RadToDeg(initialRotation.X),
            Mathf.RadToDeg(initialRotation.Y),
            Mathf.RadToDeg(initialRotation.Z)
        );
        this.tweezers.RotationDegrees = new Vector3(
            initialRotationDegrees.X,
            initialRotationDegrees.Y,
            initialRotationDegrees.Z + this.tweezersDragRotationAngle
        );
    }

    private void RestoreTweezersRotation() {
        if (this.tweezers == null) {
            return;
        }
        Vector3 initialRotation = this.tweezersInitialTransform.Basis.GetEuler();
        this.tweezers.Rotation = initialRotation;
    }

    private void ApplyMatchDragRotation() {
        if (this.match == null) {
            return;
        }
        Vector3 initialRotation = this.matchInitialTransform.Basis.GetEuler();
        Vector3 initialRotationDegrees = new Vector3(
            Mathf.RadToDeg(initialRotation.X),
            Mathf.RadToDeg(initialRotation.Y),
            Mathf.RadToDeg(initialRotation.Z)
        );
        this.match.RotationDegrees = new Vector3(
            initialRotationDegrees.X,
            initialRotationDegrees.Y,
            initialRotationDegrees.Z + this.tweezersDragRotationAngle
        );
    }

    private void RestoreMatchRotation() {
        if (this.match == null) {
            return;
        }
        Vector3 initialRotation = this.matchInitialTransform.Basis.GetEuler();
        this.match.Rotation = initialRotation;
    }

    private void SwitchTweezersToWithAluminum() {
        if (this.tweezers != null) {
            this.tweezers.SwitchToSwitched();
        }
    }

    private void SwitchTweezersToNormal() {
        if (this.tweezers != null) {
            this.tweezers.SwitchToNormal();
        }
    }

    private void UpdateMatchCollisionLabel() {
        if (this.match == null || this.isMatchLit) {
            if (this.match != null && !this.match.IsDragging && this.wasMatchDragging) {
                this.RestoreMatchRotation();
                this.wasMatchDragging = false;
            }
            return;
        }
        bool isDragging = this.match.IsDragging;
        if (isDragging && !this.wasMatchDragging) {
            this.ApplyMatchDragRotation();
        } else if (!isDragging && this.wasMatchDragging) {
            this.RestoreMatchRotation();
        }
        this.wasMatchDragging = isDragging;
        if (isDragging) {
            this.ShowMatchboxCollisionLabel(this.isMatchInMatchboxArea);
        } else {
            if (this.isMatchInMatchboxArea && !this.isMatchLit) {
                this.OnMatchLit();
            }
        }
    }

    private void UpdateWoodenStickCollisionLabel() {
        if (this.currentStep != AluminumReactionExperimentStep.Step05) {
            return;
        }
        if (this.match == null || !this.isMatchLit || this.isWoodenStickLit) {
            return;
        }
        bool isDragging = this.match.IsDragging;
        if (isDragging && !this.wasMatchDragging) {
            this.ApplyMatchDragRotation();
        } else if (!isDragging && this.wasMatchDragging) {
            this.RestoreMatchRotation();
        }
        this.wasMatchDragging = isDragging;
        if (isDragging) {
            this.ShowWoodenStickCollisionLabel(this.isMatchInWoodenStickArea);
        } else {
            if (this.isMatchInWoodenStickArea && !this.isWoodenStickLit) {
                this.OnWoodenStickLit();
            }
        }
    }

    private void UpdateTestTubeCollisionLabel() {
        if (this.woodenStick == null || !this.isWoodenStickLit) {
            return;
        }
        bool isDragging = this.woodenStick.IsDragging;
        bool isInArea = false;
        bool isDetected = false;
        if (this.currentStep == AluminumReactionExperimentStep.Step06) {
            isInArea = this.isWoodenStickInTestTube1Area;
            isDetected = this.isTestTube1Detected;
        } else if (this.currentStep == AluminumReactionExperimentStep.Step07) {
            isInArea = this.isWoodenStickInTestTube2Area;
            isDetected = this.isTestTube2Detected;
        }
        if (isDragging) {
            if (this.currentStep == AluminumReactionExperimentStep.Step06) {
                this.ShowTestTube1CollisionLabel(isInArea);
            } else if (this.currentStep == AluminumReactionExperimentStep.Step07) {
                this.ShowTestTube2CollisionLabel(isInArea);
            }
        } else {
            if (isInArea && !isDetected) {
                this.OnTestTubeDetected();
            }
        }
    }

    private void OnMatchLit() {
        if (this.isMatchLit) {
            return;
        }
        this.ShowMatchboxCollisionLabel(false);
        this.isMatchLit = true;
        if (this.match != null) {
            this.match.SwitchToSwitched();
        }
    }

    private void OnWoodenStickLit() {
        if (this.isWoodenStickLit) {
            return;
        }
        this.ShowWoodenStickCollisionLabel(false);
        this.isWoodenStickLit = true;
        if (this.woodenStick != null) {
            this.woodenStick.SwitchToSwitched();
            this.woodenStick.IsDraggable = true;
        }
        if (this.match != null) {
            this.match.SwitchToNormal();
            this.match.GlobalTransform = this.matchInitialTransform;
            this.RestoreMatchRotation();
        }
        this.isMatchLit = false;
        this.wasMatchDragging = false;
        if (this.currentStep == AluminumReactionExperimentStep.Step05) {
            this.CompleteCurrentStep();
        }
    }

    private void OnItemPlaced() {
        if (this.isItemPlaced) {
            return;
        }
        this.ShowCollisionLabel(false);
        this.isItemPlaced = true;
        this.stepWhenFillAnimationStarted = this.currentStep;
        this.sodiumHydroxideSolution.Visible = false;
        if (this.currentStep == AluminumReactionExperimentStep.Step01) {
            this.emptyReagent1.Visible = false;
        } else if (this.currentStep == AluminumReactionExperimentStep.Step02) {
            this.emptyReagent2.Visible = false;
        }
        this.fillObjects.Visible = true;
        this.animationPlayer.Play("fill");
    }

    private void OnAluminumPickedUp() {
        if (this.isPickupAluminum) {
            return;
        }
        this.ShowAluminumCollisionLabel(false);
        this.isPickupAluminum = true;
        this.SwitchTweezersToWithAluminum();
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (this.aluminumStrip1 != null) {
                this.aluminumStrip1.Visible = false;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.aluminumStrip2 != null) {
                this.aluminumStrip2.Visible = false;
            }
        }
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            this.SetupStepThreeCollisionReagent();
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            this.SetupStepFourCollisionReagent();
        }
    }

    private void OnAluminumDroppedIntoReagent() {
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (this.isItemPlaced3) {
                return;
            }
            this.ShowReagentCollisionLabel(false);
            this.isItemPlaced3 = true;
            if (this.emptyReagent1 != null) {
                this.emptyReagent1.Visible = false;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.isItemPlaced4) {
                return;
            }
            this.ShowReagentCollisionLabel(false);
            this.isItemPlaced4 = true;
            if (this.emptyReagent2 != null) {
                this.emptyReagent2.Visible = false;
            }
        }
        if (this.tweezers != null) {
            this.tweezers.Visible = false;
        }
        if (this.dropObjects != null) {
            this.dropObjects.Visible = true;
        }
        this.animationPlayer.Play("drop");
    }

    private void OnAnimationFinished(StringName animName) {
        if (animName == "fill") {
            this.OnItemPlacedDone();
        } else if (animName == "drop") {
            this.OnAluminumDroppedDone();
        }
    }

    private void OnItemPlacedDone() {
        if (this.stepWhenFillAnimationStarted == AluminumReactionExperimentStep.Step01 ||
            this.stepWhenFillAnimationStarted == AluminumReactionExperimentStep.Step02) {
            if (this.currentStep != this.stepWhenFillAnimationStarted) {
                return;
            }
            if (this.sodiumHydroxideSolution != null) {
                this.sodiumHydroxideSolution.GlobalTransform = this.sodiumHydroxideSolutionInitialTransform;
            }
            this.sodiumHydroxideSolution.Visible = true;
            if (this.stepWhenFillAnimationStarted == AluminumReactionExperimentStep.Step01) {
                if (this.emptyReagent1 != null) {
                    this.emptyReagent1.Visible = true;
                }
            } else if (this.stepWhenFillAnimationStarted == AluminumReactionExperimentStep.Step02) {
                if (this.emptyReagent2 != null) {
                    this.emptyReagent2.Visible = true;
                }
                this.sodiumHydroxideSolution.IsDraggable = false;
            }
            this.fillObjects.Visible = false;
            this.CompleteCurrentStep();
        }
    }

    private void OnAluminumDroppedDone() {
        if (this.currentStep == AluminumReactionExperimentStep.Step03 ||
            this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.tweezers != null) {
                this.tweezers.GlobalTransform = this.tweezersInitialTransform;
            }
            this.tweezers.Visible = true;
            this.SwitchTweezersToNormal();
            this.isPickupAluminum = false;
            if (this.dropObjects != null) {
                this.dropObjects.Visible = false;
            }
            if (this.currentStep == AluminumReactionExperimentStep.Step03) {
                if (this.emptyReagent1 != null) {
                    this.emptyReagent1.Visible = true;
                }
                if (this.aluminumStripInTube1 != null) {
                    this.aluminumStripInTube1.Visible = true;
                }
            } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
                if (this.emptyReagent2 != null) {
                    this.emptyReagent2.Visible = true;
                }
                if (this.aluminumStripInTube2 != null) {
                    this.aluminumStripInTube2.Visible = true;
                }
            }
        }
        this.CompleteCurrentStep();
    }

    private void HandleTriggerAreaCollision(Node node, Area3D triggerArea) {
        if (this.currentStep == AluminumReactionExperimentStep.Step01 ||
            this.currentStep == AluminumReactionExperimentStep.Step02) {
            if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
                this.isSodiumHydroxideInArea = true;
                if (this.sodiumHydroxideSolution != null && this.sodiumHydroxideSolution.IsDragging) {
                    this.ShowCollisionLabel(true);
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step05) {
            if (triggerArea == this.triggerAreaMatchbox) {
                if (this.IsNodePartOfItem(node, this.match) && !this.isMatchLit) {
                    this.isMatchInMatchboxArea = true;
                    if (this.match != null && this.match.IsDragging) {
                        this.ShowMatchboxCollisionLabel(true);
                    }
                }
            } else if (triggerArea == this.triggerAreaWoodenStick) {
                if (this.IsNodePartOfItem(node, this.match) && this.isMatchLit && !this.isWoodenStickLit) {
                    this.isMatchInWoodenStickArea = true;
                    if (this.match != null && this.match.IsDragging) {
                        this.ShowWoodenStickCollisionLabel(true);
                    }
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step06) {
            if (triggerArea == this.triggerAreaTestTube1) {
                if (this.IsNodePartOfItem(node, this.woodenStick) && this.isWoodenStickLit && !this.isTestTube1Detected) {
                    this.isWoodenStickInTestTube1Area = true;
                    if (this.woodenStick != null && this.woodenStick.IsDragging) {
                        this.ShowTestTube1CollisionLabel(true);
                    }
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step07) {
            if (triggerArea == this.triggerAreaTestTube2) {
                if (this.IsNodePartOfItem(node, this.woodenStick) && this.isWoodenStickLit && !this.isTestTube2Detected) {
                    this.isWoodenStickInTestTube2Area = true;
                    if (this.woodenStick != null && this.woodenStick.IsDragging) {
                        this.ShowTestTube2CollisionLabel(true);
                    }
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (triggerArea == this.triggerArea3) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea3 = true;
                    if (this.tweezers != null && this.tweezers.IsDragging) {
                        this.ShowAluminumCollisionLabel(true);
                    }
                }
            } else if (triggerArea == this.triggerArea3Reagent) {
                if (this.IsNodePartOfItem(node, this.tweezers) && this.isPickupAluminum) {
                    this.isTweezersInReagentArea3 = true;
                    if (this.tweezers != null && this.tweezers.IsDragging) {
                        this.ShowReagentCollisionLabel(true);
                    }
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (triggerArea == this.triggerArea4) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea4 = true;
                    if (this.tweezers != null && this.tweezers.IsDragging) {
                        this.ShowAluminumCollisionLabel(true);
                    }
                }
            } else if (triggerArea == this.triggerArea4Reagent) {
                if (this.IsNodePartOfItem(node, this.tweezers) && this.isPickupAluminum) {
                    this.isTweezersInReagentArea4 = true;
                    if (this.tweezers != null && this.tweezers.IsDragging) {
                        this.ShowReagentCollisionLabel(true);
                    }
                }
            }
        }
    }

    private void HandleTriggerAreaExit(Node node, Area3D triggerArea) {
        if (this.currentStep == AluminumReactionExperimentStep.Step01 ||
            this.currentStep == AluminumReactionExperimentStep.Step02) {
            if (this.IsNodePartOfItem(node, this.sodiumHydroxideSolution)) {
                this.isSodiumHydroxideInArea = false;
                this.ShowCollisionLabel(false);
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step05) {
            if (triggerArea == this.triggerAreaMatchbox) {
                if (this.IsNodePartOfItem(node, this.match) && !this.isMatchLit) {
                    this.isMatchInMatchboxArea = false;
                    this.ShowMatchboxCollisionLabel(false);
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step05) {
            if (triggerArea == this.triggerAreaWoodenStick) {
                if (this.IsNodePartOfItem(node, this.match) && this.isMatchLit) {
                    this.isMatchInWoodenStickArea = false;
                    this.ShowWoodenStickCollisionLabel(false);
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step06) {
            if (triggerArea == this.triggerAreaTestTube1) {
                if (this.IsNodePartOfItem(node, this.woodenStick) && this.isWoodenStickLit) {
                    this.isWoodenStickInTestTube1Area = false;
                    this.ShowTestTube1CollisionLabel(false);
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step07) {
            if (triggerArea == this.triggerAreaTestTube2) {
                if (this.IsNodePartOfItem(node, this.woodenStick) && this.isWoodenStickLit) {
                    this.isWoodenStickInTestTube2Area = false;
                    this.ShowTestTube2CollisionLabel(false);
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (triggerArea == this.triggerArea3) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea3 = false;
                    this.ShowAluminumCollisionLabel(false);
                }
            } else if (triggerArea == this.triggerArea3Reagent) {
                if (this.IsNodePartOfItem(node, this.tweezers) && this.isPickupAluminum) {
                    this.isTweezersInReagentArea3 = false;
                    this.ShowReagentCollisionLabel(false);
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (triggerArea == this.triggerArea4) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea4 = false;
                    this.ShowAluminumCollisionLabel(false);
                }
            } else if (triggerArea == this.triggerArea4Reagent) {
                if (this.IsNodePartOfItem(node, this.tweezers) && this.isPickupAluminum) {
                    this.isTweezersInReagentArea4 = false;
                    this.ShowReagentCollisionLabel(false);
                }
            }
        }
    }

    private bool IsNodePartOfItem(Node node, PlacableItem item) {
        if (node == null || item == null) {
            return false;
        }
        if (node == item) {
            return true;
        }
        Node current = node;
        int depth = 0;
        const int maxDepth = 10;
        while (current != null && depth < maxDepth) {
            if (current == item) {
                return true;
            }
            current = current.GetParent();
            depth++;
        }
        return false;
    }

    private void ShowCollisionLabel(bool isShow) {
        if (this.currentStep == AluminumReactionExperimentStep.Step01) {
            this.collisionLabel1.Visible = isShow;
        } else if (this.currentStep == AluminumReactionExperimentStep.Step02) {
            this.collisionLabel2.Visible = isShow;
        }
    }

    private void ShowAluminumCollisionLabel(bool isShow) {
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (this.collisionLabel3 != null) {
                this.collisionLabel3.Visible = isShow;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.collisionLabel4 != null) {
                this.collisionLabel4.Visible = isShow;
            }
        }
    }

    private void ShowReagentCollisionLabel(bool isShow) {
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (this.collisionLabel3Reagent != null) {
                this.collisionLabel3Reagent.Visible = isShow;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.collisionLabel4Reagent != null) {
                this.collisionLabel4Reagent.Visible = isShow;
            }
        }
    }

    private void ShowMatchboxCollisionLabel(bool isShow) {
        if (this.collisionLabelMatchbox != null) {
            this.collisionLabelMatchbox.Visible = isShow;
        }
    }

    private void ShowWoodenStickCollisionLabel(bool isShow) {
        if (this.collisionLabelWoodenStick != null) {
            this.collisionLabelWoodenStick.Visible = isShow;
        }
    }

    private void ShowTestTube1CollisionLabel(bool isShow) {
        if (this.collisionLabelTestTube1 != null) {
            this.collisionLabelTestTube1.Visible = isShow;
        }
    }

    private void ShowTestTube2CollisionLabel(bool isShow) {
        if (this.collisionLabelTestTube2 != null) {
            this.collisionLabelTestTube2.Visible = isShow;
        }
    }

    private void OnTestTubeDetected() {
        if (this.currentStep == AluminumReactionExperimentStep.Step06) {
            if (this.isTestTube1Detected) {
                return;
            }
            this.ShowTestTube1CollisionLabel(false);
            this.isTestTube1Detected = true;
        } else if (this.currentStep == AluminumReactionExperimentStep.Step07) {
            if (this.isTestTube2Detected) {
                return;
            }
            this.ShowTestTube2CollisionLabel(false);
            this.isTestTube2Detected = true;
        }
        this.CompleteCurrentStep();
    }

    private void SetupStepTwoCollision() {
        this.isItemPlaced = false;
        this.isSodiumHydroxideInArea = false;
        if (this.collisionLabel2 != null) {
            this.collisionLabel2.Visible = false;
        }
        if (this.sodiumHydroxideSolution != null) {
            this.sodiumHydroxideSolution.StopDragging();
        }
        if (this.emptyReagent2 is PlacableItem emptyReagentItem) {
            emptyReagentItem.StopDragging();
        }
        if (this.triggerArea2 != null) {
            this.triggerArea2.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerArea2);
            this.triggerArea2.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea2);
            this.triggerArea2.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerArea2);
            this.triggerArea2.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea2);
        }
    }

    private void SetupStepThreeCollisionAluminum() {
        this.isItemPlaced3 = false;
        this.isTweezersInArea3 = false;
        this.isPickupAluminum = false;
        this.wasTweezersDragging = false;
        if (this.collisionLabel3 != null) {
            this.collisionLabel3.Visible = false;
        }
        this.SwitchTweezersToNormal();
        if (this.triggerArea3 != null) {
            this.triggerArea3.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerArea3);
            this.triggerArea3.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea3);
            this.triggerArea3.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerArea3);
            this.triggerArea3.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea3);
        }
    }

    private void SetupStepThreeCollisionReagent() {
        this.isTweezersInReagentArea3 = false;
        if (this.collisionLabel3Reagent != null) {
            this.collisionLabel3Reagent.Visible = false;
        }
        if (this.triggerArea3Reagent != null) {
            this.triggerArea3Reagent.BodyEntered +=
                (body) => this.HandleTriggerAreaCollision(body, this.triggerArea3Reagent);
            this.triggerArea3Reagent.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea3Reagent);
            this.triggerArea3Reagent.AreaEntered +=
                (area) => this.HandleTriggerAreaCollision(area, this.triggerArea3Reagent);
            this.triggerArea3Reagent.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea3Reagent);
        }
    }

    private void SetupStepFourCollisionReagent() {
        this.isTweezersInReagentArea4 = false;
        if (this.collisionLabel4Reagent != null) {
            this.collisionLabel4Reagent.Visible = false;
        }
        if (this.triggerArea4Reagent != null) {
            this.triggerArea4Reagent.BodyEntered +=
                (body) => this.HandleTriggerAreaCollision(body, this.triggerArea4Reagent);
            this.triggerArea4Reagent.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea4Reagent);
            this.triggerArea4Reagent.AreaEntered +=
                (area) => this.HandleTriggerAreaCollision(area, this.triggerArea4Reagent);
            this.triggerArea4Reagent.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea4Reagent);
        }
    }

    private void SetupStepFourCollision() {
        this.isItemPlaced4 = false;
        this.isTweezersInArea4 = false;
        this.isPickupAluminum = false;
        this.wasTweezersDragging = false;
        if (this.collisionLabel4 != null) {
            this.collisionLabel4.Visible = false;
        }
        if (this.tweezers != null) {
            this.tweezers.StopDragging();
            this.RestoreTweezersRotation();
        }
        this.SwitchTweezersToNormal();
        if (this.triggerArea4 != null) {
            this.triggerArea4.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerArea4);
            this.triggerArea4.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea4);
            this.triggerArea4.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerArea4);
            this.triggerArea4.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea4);
        }
    }

    private void SetupStepFiveCollision() {
        this.isMatchLit = false;
        this.isMatchInMatchboxArea = false;
        this.isMatchInWoodenStickArea = false;
        this.isWoodenStickLit = false;
        this.wasMatchDragging = false;
        if (this.collisionLabelMatchbox != null) {
            this.collisionLabelMatchbox.Visible = false;
        }
        if (this.collisionLabelWoodenStick != null) {
            this.collisionLabelWoodenStick.Visible = false;
        }
        if (this.match != null) {
            this.match.StopDragging();
            this.match.SwitchToNormal();
            this.match.GlobalTransform = this.matchInitialTransform;
            this.RestoreMatchRotation();
        }
        if (this.woodenStick != null) {
            this.woodenStick.SwitchToNormal();
            this.woodenStick.IsDraggable = false;
            this.woodenStick.StopDragging();
        }
        if (this.triggerAreaMatchbox != null) {
            this.triggerAreaMatchbox.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerAreaMatchbox);
            this.triggerAreaMatchbox.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerAreaMatchbox);
            this.triggerAreaMatchbox.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerAreaMatchbox);
            this.triggerAreaMatchbox.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerAreaMatchbox);
        }
        if (this.triggerAreaWoodenStick != null) {
            this.triggerAreaWoodenStick.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerAreaWoodenStick);
            this.triggerAreaWoodenStick.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerAreaWoodenStick);
            this.triggerAreaWoodenStick.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerAreaWoodenStick);
            this.triggerAreaWoodenStick.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerAreaWoodenStick);
        }
    }

    private void SetupStepSixCollision() {
        this.isWoodenStickInTestTube1Area = false;
        this.isTestTube1Detected = false;
        if (this.collisionLabelTestTube1 != null) {
            this.collisionLabelTestTube1.Visible = false;
        }
        if (this.woodenStick != null) {
            this.woodenStick.StopDragging();
            if (this.isWoodenStickLit) {
                this.woodenStick.SwitchToSwitched();
                this.woodenStick.IsDraggable = true;
            }
        }
        if (this.triggerAreaTestTube1 != null) {
            this.triggerAreaTestTube1.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerAreaTestTube1);
            this.triggerAreaTestTube1.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerAreaTestTube1);
            this.triggerAreaTestTube1.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerAreaTestTube1);
            this.triggerAreaTestTube1.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerAreaTestTube1);
        }
    }

    private void SetupStepSevenCollision() {
        this.isWoodenStickInTestTube2Area = false;
        this.isTestTube2Detected = false;
        if (this.collisionLabelTestTube2 != null) {
            this.collisionLabelTestTube2.Visible = false;
        }
        if (this.woodenStick != null) {
            this.woodenStick.StopDragging();
            if (this.isWoodenStickLit) {
                this.woodenStick.SwitchToSwitched();
                this.woodenStick.IsDraggable = true;
            }
        }
        if (this.triggerAreaTestTube2 != null) {
            this.triggerAreaTestTube2.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerAreaTestTube2);
            this.triggerAreaTestTube2.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerAreaTestTube2);
            this.triggerAreaTestTube2.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerAreaTestTube2);
            this.triggerAreaTestTube2.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerAreaTestTube2);
        }
    }

    private void InitializeStepHints() {
        base.stepHints[AluminumReactionExperimentStep.Step01] =
            "步骤 1：向第一支试管中加入氢氧化钠溶液";
        base.stepHints[AluminumReactionExperimentStep.Step02] =
            "步骤 2：向第二支试管中加入氢氧化钠溶液";
        base.stepHints[AluminumReactionExperimentStep.Step03] =
            "步骤 3：用镊子将第一片铝片放入第一支试管中";
        base.stepHints[AluminumReactionExperimentStep.Step04] =
            "步骤 4：用镊子将第二片铝片放入第二支试管中";
        base.stepHints[AluminumReactionExperimentStep.Step05] =
            "步骤 5：取一根火柴并点燃木条";
        base.stepHints[AluminumReactionExperimentStep.Step06] =
            "步骤 6：将点燃的木条靠近第一支试管口，检测生成的气体";
        base.stepHints[AluminumReactionExperimentStep.Step07] =
            "步骤 7：将点燃的木条靠近第二支试管口，检测生成的气体";
        base.stepHints[AluminumReactionExperimentStep.Step08] =
            "实验完成";
    }

    protected override AluminumReactionExperimentStep SetupStep => AluminumReactionExperimentStep.Step01;

    protected override AluminumReactionExperimentStep CompletedStep => AluminumReactionExperimentStep.Step08;
}