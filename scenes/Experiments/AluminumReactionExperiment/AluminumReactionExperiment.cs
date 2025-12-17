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
    [Export] protected override AluminumReactionExperimentStep currentStep { get; set; } = AluminumReactionExperimentStep.Step01;
    [Export] private PlacableItem sodiumHydroxideSolution;
    [Export] private Node3D emptyReagent1;
    [Export] private Area3D triggerArea1;
    [Export] private Label3D collisionLabel1;
    private bool isSodiumHydroxideInArea = false;
    private bool isItemPlaced = false;
    [Export] private Node3D fillObjects;
    [Export] private AnimationPlayer animationPlayer;
    private Transform3D sodiumHydroxideSolutionInitialTransform;
    [Export] private Node3D emptyReagent2;
    [Export] private Area3D triggerArea2;
    [Export] private Label3D collisionLabel2;
    private bool isStepTwoCollisionInitialized = false;
    [Export] private SwitchablePlacableItem tweezers;
    [Export] private Node3D aluminumStrip1;
    [Export] private Area3D triggerArea3;
    [Export] private Label3D collisionLabel3;
    private bool isTweezersInArea3 = false;
    private bool isItemPlaced3 = false;
    private bool isStepThreeCollisionInitialized = false;
    [Export] private Node3D aluminumStrip2;
    [Export] private Area3D triggerArea4;
    [Export] private Label3D collisionLabel4;
    private bool isTweezersInArea4 = false;
    private bool isItemPlaced4 = false;
    private bool isStepFourCollisionInitialized = false;
    private Transform3D tweezersInitialTransform;
    private bool wasTweezersDragging = false;
    [Export] private float tweezersDragRotationAngle = 45.0f;
    // tweezer aluminum stuff
    private bool isPickupAluminum = false;
    // 试管口的触发区域和标签（步骤三和步骤四）
    [Export] private Area3D triggerArea3Reagent;
    [Export] private Label3D collisionLabel3Reagent;
    [Export] private Area3D triggerArea4Reagent;
    [Export] private Label3D collisionLabel4Reagent;
    private bool isTweezersInReagentArea3 = false;
    private bool isTweezersInReagentArea4 = false;

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
        if (isDragging && !this.wasTweezersDragging) {
            this.ApplyTweezersDragRotation();
        } else if (!isDragging && this.wasTweezersDragging) {
            this.RestoreTweezersRotation();
        }
        this.wasTweezersDragging = isDragging;
        
        // 如果已经夹住铝片，检测与试管口的碰撞
        if (this.isPickupAluminum) {
            bool isInReagentArea = false;
            if (this.currentStep == AluminumReactionExperimentStep.Step03) {
                isInReagentArea = this.isTweezersInReagentArea3;
            } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
                isInReagentArea = this.isTweezersInReagentArea4;
            }
            if (isDragging) {
                this.ShowReagentCollisionLabel(isInReagentArea);
            } else {
                if (isInReagentArea && !this.isItemPlaced3 && !this.isItemPlaced4) {
                    this.OnAluminumDroppedIntoReagent();
                }
            }
        } else {
            // 第一步：检测与铝片的碰撞
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

    private void SwitchTweezersToWithAluminum() {
        // 使用 SwitchablePlacableItem 的切换方法
        if (this.tweezers != null) {
            this.tweezers.SwitchToSwitched();
        }
    }

    private void SwitchTweezersToNormal() {
        // 使用 SwitchablePlacableItem 的切换方法
        if (this.tweezers != null) {
            this.tweezers.SwitchToNormal();
        }
    }

    private void OnItemPlaced() {
        if (this.isItemPlaced) {
            return;
        }
        this.ShowCollisionLabel(false);
        this.isItemPlaced = true;
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
        // 第一步：夹住铝片
        if (this.isPickupAluminum) {
            return;
        }
        this.ShowAluminumCollisionLabel(false);
        this.isPickupAluminum = true;
        
        // 切换镊子显示（隐藏普通镊子，显示夹着铝片的镊子）
        this.SwitchTweezersToWithAluminum();
        
        // 隐藏铝片
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (this.aluminumStrip1 != null) {
                this.aluminumStrip1.Visible = false;
    }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.aluminumStrip2 != null) {
                this.aluminumStrip2.Visible = false;
            }
        }
        
        // 初始化试管口碰撞检测
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            this.SetupStepThreeCollisionReagent();
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            this.SetupStepFourCollisionReagent();
        }
    }

    private void OnAluminumDroppedIntoReagent() {
        // 第二步：将铝片放入试管，触发动画
        if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (this.isItemPlaced3) {
                return;
            }
            this.ShowReagentCollisionLabel(false);
            this.isItemPlaced3 = true;
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.isItemPlaced4) {
                return;
            }
            this.ShowReagentCollisionLabel(false);
            this.isItemPlaced4 = true;
        }
        
        // 隐藏镊子（节点由 SwitchablePlacableItem 管理）
        if (this.tweezers != null) {
            this.tweezers.Visible = false;
        }
        
        // 播放动画
        this.fillObjects.Visible = true;
        this.animationPlayer.Play("fill");
    }

    private void OnItemPlacedDone() {
        if (this.currentStep == AluminumReactionExperimentStep.Step01 || 
            this.currentStep == AluminumReactionExperimentStep.Step02) {
            if (this.sodiumHydroxideSolution != null) {
                this.sodiumHydroxideSolution.GlobalTransform = this.sodiumHydroxideSolutionInitialTransform;
    }
            this.sodiumHydroxideSolution.Visible = true;
            if (this.currentStep == AluminumReactionExperimentStep.Step01) {
                this.emptyReagent1.Visible = true;
            } else if (this.currentStep == AluminumReactionExperimentStep.Step02) {
                this.emptyReagent2.Visible = true;
                this.sodiumHydroxideSolution.IsDraggable = false;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            if (this.tweezers != null) {
                this.tweezers.GlobalTransform = this.tweezersInitialTransform;
            }
            this.tweezers.Visible = true;
            // 恢复镊子显示（显示普通镊子，隐藏夹着铝片的镊子）
            this.SwitchTweezersToNormal();
            // 重置状态
            this.isPickupAluminum = false;
            if (this.aluminumStrip1 != null) {
                this.aluminumStrip1.Visible = true;
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            if (this.tweezers != null) {
                this.tweezers.GlobalTransform = this.tweezersInitialTransform;
            }
            this.tweezers.Visible = true;
            // 恢复镊子显示（显示普通镊子，隐藏夹着铝片的镊子）
            this.SwitchTweezersToNormal();
            // 重置状态
            this.isPickupAluminum = false;
            if (this.aluminumStrip2 != null) {
                this.aluminumStrip2.Visible = true;
            }
        }
        this.fillObjects.Visible = false;
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
        } else if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            // 检测是否在铝片区域（第一步）
            if (triggerArea == this.triggerArea3) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea3 = true;
                    if (this.tweezers != null && this.tweezers.IsDragging) {
                        this.ShowAluminumCollisionLabel(true);
                    }
                }
            }
            // 检测是否在试管口区域（第二步）
            else if (triggerArea == this.triggerArea3Reagent) {
                if (this.IsNodePartOfItem(node, this.tweezers) && this.isPickupAluminum) {
                    this.isTweezersInReagentArea3 = true;
                    if (this.tweezers != null && this.tweezers.IsDragging) {
                        this.ShowReagentCollisionLabel(true);
                    }
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            // 检测是否在铝片区域（第一步）
            if (triggerArea == this.triggerArea4) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea4 = true;
                    if (this.tweezers != null && this.tweezers.IsDragging) {
                        this.ShowAluminumCollisionLabel(true);
                    }
                }
            }
            // 检测是否在试管口区域（第二步）
            else if (triggerArea == this.triggerArea4Reagent) {
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
        } else if (this.currentStep == AluminumReactionExperimentStep.Step03) {
            // 铝片区域退出
            if (triggerArea == this.triggerArea3) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea3 = false;
                    this.ShowAluminumCollisionLabel(false);
                }
            }
            // 试管口区域退出
            else if (triggerArea == this.triggerArea3Reagent) {
                if (this.IsNodePartOfItem(node, this.tweezers) && this.isPickupAluminum) {
                    this.isTweezersInReagentArea3 = false;
                    this.ShowReagentCollisionLabel(false);
                }
            }
        } else if (this.currentStep == AluminumReactionExperimentStep.Step04) {
            // 铝片区域退出
            if (triggerArea == this.triggerArea4) {
                if (this.IsNodePartOfItem(node, this.tweezers) && !this.isPickupAluminum) {
                    this.isTweezersInArea4 = false;
                    this.ShowAluminumCollisionLabel(false);
                }
            }
            // 试管口区域退出
            else if (triggerArea == this.triggerArea4Reagent) {
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
        // 恢复镊子显示（显示普通镊子，隐藏夹着铝片的镊子）
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
            this.triggerArea3Reagent.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerArea3Reagent);
            this.triggerArea3Reagent.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea3Reagent);
            this.triggerArea3Reagent.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerArea3Reagent);
            this.triggerArea3Reagent.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea3Reagent);
        }
    }

    private void SetupStepFourCollisionReagent() {
        this.isTweezersInReagentArea4 = false;
        if (this.collisionLabel4Reagent != null) {
            this.collisionLabel4Reagent.Visible = false;
        }
        if (this.triggerArea4Reagent != null) {
            this.triggerArea4Reagent.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerArea4Reagent);
            this.triggerArea4Reagent.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea4Reagent);
            this.triggerArea4Reagent.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerArea4Reagent);
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
        // 恢复镊子显示（显示普通镊子，隐藏夹着铝片的镊子）
        this.SwitchTweezersToNormal();
        if (this.triggerArea4 != null) {
            this.triggerArea4.BodyEntered += (body) => this.HandleTriggerAreaCollision(body, this.triggerArea4);
            this.triggerArea4.BodyExited += (body) => this.HandleTriggerAreaExit(body, this.triggerArea4);
            this.triggerArea4.AreaEntered += (area) => this.HandleTriggerAreaCollision(area, this.triggerArea4);
            this.triggerArea4.AreaExited += (area) => this.HandleTriggerAreaExit(area, this.triggerArea4);
        }
    }

    private void InitializeStepHints() {
        base.stepHints[AluminumReactionExperimentStep.Step01] = 
            "[b]步骤 1：将氢氧化钠倒入试剂1";
        base.stepHints[AluminumReactionExperimentStep.Step02] = 
            "[b]步骤 2：将氢氧化钠倒入试剂2";
        base.stepHints[AluminumReactionExperimentStep.Step03] = 
            "[b]步骤 3：使用镊子夹将铝片1放到试剂1";
        base.stepHints[AluminumReactionExperimentStep.Step04] = 
            "[b]步骤 4：使用镊子夹将铝片2放到试剂2";
        base.stepHints[AluminumReactionExperimentStep.Step05] = 
            "[b]步骤 5：拾取火柴并点燃木棍";
        base.stepHints[AluminumReactionExperimentStep.Step06] = 
            "[b]步骤 6：点燃的木棍靠近试剂1试管口检测生成气体";
        base.stepHints[AluminumReactionExperimentStep.Step07] = 
            "[b]步骤 7：点燃的木棍靠近试剂2     试管口检测生成气体";
        base.stepHints[AluminumReactionExperimentStep.Step08] = 
            "[b]实验完成！[/b]实验结束";
    }

    protected override string GetStepName(AluminumReactionExperimentStep step) {
        switch (step) {
            case AluminumReactionExperimentStep.Step01:
                return "将氢氧化钠倒入试剂1";
            case AluminumReactionExperimentStep.Step02:
                return "将氢氧化钠倒入试剂2";
            case AluminumReactionExperimentStep.Step03:
                return "使用镊子夹将铝片1放到试剂1";
            case AluminumReactionExperimentStep.Step04:
                return "使用镊子夹将铝片2放到试剂2";
            case AluminumReactionExperimentStep.Step05:
                return "拾取火柴并点燃木棍";
            case AluminumReactionExperimentStep.Step06:
                return "点燃的木棍靠近试管口1检测生成气体";
            case AluminumReactionExperimentStep.Step07:
                return "点燃的木棍靠近试管口2检测生成气体";
            case AluminumReactionExperimentStep.Step08:
                return "实验完成";
            default:
                return "未知步骤";
        }
    }

    protected override AluminumReactionExperimentStep SetupStep => AluminumReactionExperimentStep.Step01;
    
    protected override AluminumReactionExperimentStep CompletedStep => AluminumReactionExperimentStep.Step08;
}