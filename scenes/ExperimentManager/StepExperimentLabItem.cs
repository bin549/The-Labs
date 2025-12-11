using Godot;
using System;
using System.Collections.Generic;

public abstract partial class StepExperimentLabItem<TStep, TItem> : LabItem
    where TStep : struct, Enum {
    [Export] public Godot.Collections.Array<NodePath> placableItemPaths { get; set; } = new();
    [Export] public Label3D hintLabel { get; set; }
    [Export] public Button nextStepButton { get; set; }
    [Export] public Button playVoiceButton { get; set; }
    [Export] public AudioStreamPlayer voicePlayer { get; set; }
    [Export] public Godot.Collections.Array<AudioStream> stepVoiceResources { get; set; } = new();
    protected Dictionary<TItem, Node3D> experimentItems = new Dictionary<TItem, Node3D>();
    protected Dictionary<TStep, bool> stepCompletionStatus = new Dictionary<TStep, bool>();
    protected Dictionary<TStep, string> stepHints = new Dictionary<TStep, string>();
    protected Dictionary<TStep, AudioStream> stepVoices = new Dictionary<TStep, AudioStream>();
    protected Dictionary<TStep, float> stepHintDisplayDurations = new Dictionary<TStep, float>();
    private Timer hintHideTimer;
    protected abstract TStep currentStep { get; set; }
    protected abstract TStep SetupStep { get; }
    protected abstract TStep CompletedStep { get; }
    protected abstract string GetStepName(TStep step);
    
    public override void EnterInteraction() {
        base.EnterInteraction();
        this.ShowExperimentButtons(true);
    }

    public override void ExitInteraction() {
        this.ShowExperimentButtons(false);
        base.ExitInteraction();
    }
    
    protected void InitializeStepExperiment() {
        this.InitializeExperimentItems();
        this.InitializeStepStatus();
        this.InitializeButton();
        this.InitializeVoiceButton();
        this.InitializeVoiceResources();
        this.InitializeHintTimer();
        this.HideHintLabel();
        this.ShowExperimentButtons(false);
    }

    protected virtual void InitializeExperimentItems() {
        foreach (var path in placableItemPaths) {
            if (path != null && !path.IsEmpty) {
                var item = GetNodeOrNull<Node3D>(path);
                if (item != null) {
                }
            }
        }
    }

    protected virtual void InitializeStepStatus() {
        foreach (TStep step in Enum.GetValues(typeof(TStep))) {
            this.stepCompletionStatus[step] = false;
        }
    }

    protected virtual void InitializeButton() {
        if (this.nextStepButton != null) {
            this.nextStepButton.Pressed += OnNextStepButtonPressed;
            this.UpdateButtonState();
        }
    }

    protected virtual void InitializeVoiceButton() {
        if (this.playVoiceButton != null) {
            this.playVoiceButton.Pressed += OnPlayVoiceButtonPressed;
        }
    }

    protected virtual void InitializeVoiceResources() {
        var steps = Enum.GetValues(typeof(TStep));
        for (int i = 0; i < steps.Length && i < this.stepVoiceResources.Count; i++) {
            var step = (TStep)steps.GetValue(i);
            if (this.stepVoiceResources[i] != null) {
                this.stepVoices[step] = this.stepVoiceResources[i];
            }
        }
    }

    protected virtual void InitializeHintTimer() {
        this.hintHideTimer = new Timer();
        this.hintHideTimer.OneShot = true;
        this.hintHideTimer.Timeout += this.HideHintLabel;
        AddChild(this.hintHideTimer);
    }

    private int stepToInt(TStep step) => Convert.ToInt32(step);

    protected virtual void OnPlayVoiceButtonPressed() {
        if (!base.IsInteracting) {
            return;
        }
        this.PlayCurrentStepVoice();
    }

    protected virtual void PlayCurrentStepVoice() {
        if (!base.IsInteracting) {
            return;
        }
        if (this.voicePlayer == null) {
            return;
        }
        if (this.stepVoices.ContainsKey(this.currentStep) && this.stepVoices[this.currentStep] != null) {
            this.voicePlayer.Stream = this.stepVoices[this.currentStep];
            this.voicePlayer.Play();
            this.ShowHintLabelWithDuration();
        } 
    }

    protected virtual void OnNextStepButtonPressed() {
        if (!base.IsInteracting) {
            return;
        }
        if (this.CanGoToNextStep()) {
            this.GoToNextStep();
            this.UpdateButtonState();
        }
    }

    protected virtual void UpdateButtonState() {
        if (this.nextStepButton != null) {
            this.nextStepButton.Disabled = !this.CanGoToNextStep();
            if (this.stepToInt(this.currentStep) >= this.stepToInt(this.CompletedStep)) {
                this.nextStepButton.Text = "实验完成";
            } else {
                this.nextStepButton.Text = "下一步";
            }
        }
    }

    public void SetCurrentStep(TStep step) {
        this.currentStep = step;
    }

    public void CompleteCurrentStep() {
        if (this.stepCompletionStatus.ContainsKey(this.currentStep)) {
            this.stepCompletionStatus[this.currentStep] = true;
        }
        this.GoToNextStep();
    }

    public bool GoToNextStep() {
        if (this.stepToInt(this.currentStep) >= this.stepToInt(this.CompletedStep)) {
            return false;
        }
        var previousStep = this.currentStep;
        var nextValue = this.stepToInt(this.currentStep) + 1;
        this.currentStep = (TStep)Enum.ToObject(typeof(TStep), nextValue);
        this.OnStepChanged(previousStep, this.currentStep);
        return true;
    }

    public bool GoToPreviousStep() {
        if (this.stepToInt(this.currentStep) <= this.stepToInt(SetupStep)) {
            return false;
        }
        var previousStep = this.currentStep;
        var nextValue = this.stepToInt(this.currentStep) - 1;
        this.currentStep = (TStep)Enum.ToObject(typeof(TStep), nextValue);
        this.OnStepChanged(previousStep, this.currentStep);
        return true;
    }

    public bool CanGoToNextStep() {
        return this.stepToInt(this.currentStep) < this.stepToInt(this.CompletedStep);
    }

    public bool CanGoToPreviousStep() {
        return this.stepToInt(this.currentStep) > this.stepToInt(SetupStep);
    }

    protected virtual void OnStepChanged(TStep previousStep, TStep newStep) {
        this.StopCurrentVoice();
        this.HideHintLabel();
        this.UpdateHintLabel();
        this.UpdateButtonState();
    }

    protected virtual void StopCurrentVoice() {
        if (this.voicePlayer != null && this.voicePlayer.Playing) {
            this.voicePlayer.Stop();
        }
        if (this.hintHideTimer != null && this.hintHideTimer.TimeLeft > 0) {
            this.hintHideTimer.Stop();
        }
        this.HideHintLabel();
    }

    protected virtual void UpdateHintLabel() {
        if (this.hintLabel != null) {
            this.hintLabel.Text = GetCurrentStepHint();
        }
    }

    protected virtual void ShowHintLabelWithDuration() {
        if (this.hintLabel == null) {
            return;
        }
        this.hintLabel.Text = GetCurrentStepHint();
        this.hintLabel.Visible = true;
        float displayDuration = this.GetStepHintDisplayDuration(this.currentStep);
        if (this.hintHideTimer != null && this.hintHideTimer.TimeLeft > 0) {
            this.hintHideTimer.Stop();
        }
        if (this.hintHideTimer != null) {
            this.hintHideTimer.WaitTime = displayDuration;
            this.hintHideTimer.Start();
        }
    }

    protected virtual void HideHintLabel() {
        if (this.hintLabel != null) {
            this.hintLabel.Visible = false;
        }
    }

    protected virtual float GetStepHintDisplayDuration(TStep step) {
        if (this.stepHintDisplayDurations.ContainsKey(step) && this.stepHintDisplayDurations[step] > 0) {
            return this.stepHintDisplayDurations[step];
        }
        return 5.0f;
    }

    public void SetStepHintDisplayDuration(TStep step, float duration) {
        this.stepHintDisplayDurations[step] = duration;
    }

    public bool IsStepCompleted(TStep step) {
        return this.stepCompletionStatus.ContainsKey(step) && this.stepCompletionStatus[step];
    }

    public Node3D GetExperimentItem(TItem itemType) {
        return this.experimentItems.ContainsKey(itemType) ? this.experimentItems[itemType] : null;
    }

    public void RegisterExperimentItem(TItem itemType, Node3D itemNode) {
        this.experimentItems[itemType] = itemNode;
    }

    public string GetCurrentStepHint() {
        return this.GetStepHint(this.currentStep);
    }

    public string GetStepHint(TStep step) {
        return this.stepHints.ContainsKey(step) ? this.stepHints[step] : "";
    }

    public Dictionary<TStep, string> GetAllStepHints() {
        return new Dictionary<TStep, string>(this.stepHints);
    }

    public string GetCurrentStepName() {
        return this.GetStepName(this.currentStep);
    }

    protected virtual void ShowExperimentButtons(bool visible) {
        if (this.nextStepButton != null) {
            this.nextStepButton.Visible = visible;
        }
        if (this.playVoiceButton != null) {
            this.playVoiceButton.Visible = visible;
        }
    }
}
