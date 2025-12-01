using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class ExperimentPhenomenon : Resource {
	[ExportGroup("现象基本信息")]
	[Export] public string PhenomenonName { get; set; } = "实验现象";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
	[Export] public bool IsEnabled { get; set; } = true;
	
	[ExportGroup("触发条件")]
	[Export] public string TriggerItemType { get; set; } = "";
	[Export] public Godot.Collections.Array<string> RequiredItemTypes { get; set; } = new();
	[Export] public bool RequireAllItems { get; set; } = true;
	[Export] public float TriggerDelay { get; set; } = 0.0f;
	
	[ExportGroup("视觉效果")]
	[Export] public PackedScene ParticleEffect { get; set; }
	[Export] public Color EffectColor { get; set; } = Colors.White;
	[Export] public float EffectDuration { get; set; } = 3.0f;
	[Export] public bool PlaySound { get; set; } = false;
	[Export] public AudioStream SoundEffect { get; set; }
	
	[ExportGroup("现象结果")]
	[Export] public bool ShowMessage { get; set; } = true;
	[Export(PropertyHint.MultilineText)] public string ResultMessage { get; set; } = "";
	[Export] public bool ProduceNewItem { get; set; } = false;
	[Export] public PackedScene ProducedItemScene { get; set; }
	[Export] public bool ConsumeItems { get; set; } = false;
	
	public bool CheckTriggerCondition(PlacableItem mainItem, Godot.Collections.Array<PlacableItem> overlappingItems) {
		if (!IsEnabled) return false;
		if (mainItem.ItemType != TriggerItemType) return false;
		if (RequiredItemTypes.Count == 0) return true;
		var foundTypes = new HashSet<string>();
		foreach (var item in overlappingItems) {
			if (RequiredItemTypes.Contains(item.ItemType)) {
				foundTypes.Add(item.ItemType);
			}
		}
		if (RequireAllItems) {
			return foundTypes.Count == RequiredItemTypes.Count;
		} else {
			return foundTypes.Count > 0;
		}
	}
}

public partial class ExperimentPhenomenonManager : Node {
	[Export] public Godot.Collections.Array<ExperimentPhenomenon> Phenomena { get; set; } = new();
	[Export] public Node3D EffectsParent { get; set; } 
	
	[Signal] public delegate void OnPhenomenonTriggeredEventHandler(ExperimentPhenomenon phenomenon, PlacableItem triggerItem);
	
	private Dictionary<string, Timer> activeTimers = new();
	private Dictionary<string, Node3D> activeEffects = new();
	private AudioStreamPlayer3D audioPlayer;
	private Label3D messageLabel;
	
	public override void _Ready() {
		InitializeAudioPlayer();
		InitializeMessageLabel();
	}
	
	private void InitializeAudioPlayer() {
		audioPlayer = new AudioStreamPlayer3D();
		audioPlayer.Name = "PhenomenonAudioPlayer";
		AddChild(audioPlayer);
	}
	
	private void InitializeMessageLabel() {
		messageLabel = new Label3D();
		messageLabel.Name = "PhenomenonMessageLabel";
		messageLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		messageLabel.Modulate = new Color(1, 1, 0, 0);
		messageLabel.OutlineSize = 10;
		messageLabel.FontSize = 32;
		if (EffectsParent != null) {
			EffectsParent.AddChild(messageLabel);
		}
	}
	
	public void RegisterItem(PlacableItem item) {
		if (item == null) return;
		item.OnItemPlaced += (placedItem, position) => OnItemPlaced(placedItem);
		item.OnItemOverlapStarted += (item1, item2) => CheckPhenomena(item1);
		GD.Print($"[PhenomenonManager] 注册物品：{item.ItemName}");
	}
	
	private void OnItemPlaced(PlacableItem item) {
		CheckPhenomena(item);
	}
	
	public void CheckPhenomena(PlacableItem item) {
		if (item == null || Phenomena.Count == 0) return;
		var overlappingItems = item.GetOverlappingItems();
		foreach (var phenomenon in Phenomena) {
			if (phenomenon.CheckTriggerCondition(item, overlappingItems)) {
				TriggerPhenomenon(phenomenon, item);
			}
		}
	}
	
	private void TriggerPhenomenon(ExperimentPhenomenon phenomenon, PlacableItem triggerItem) {
		string key = $"{phenomenon.PhenomenonName}_{triggerItem.GetInstanceId()}";
		if (activeTimers.ContainsKey(key)) {
			return;
		}
		GD.Print($"[PhenomenonManager] 触发现象：{phenomenon.PhenomenonName}");
		EmitSignal(SignalName.OnPhenomenonTriggered, phenomenon, triggerItem);
		if (phenomenon.TriggerDelay > 0) {
			var timer = new Timer();
			timer.WaitTime = phenomenon.TriggerDelay;
			timer.OneShot = true;
			timer.Timeout += () => {
				ExecutePhenomenon(phenomenon, triggerItem);
				activeTimers.Remove(key);
				timer.QueueFree();
			};
			AddChild(timer);
			activeTimers[key] = timer;
			timer.Start();
		} else {
			ExecutePhenomenon(phenomenon, triggerItem);
		}
	}
	
	private void ExecutePhenomenon(ExperimentPhenomenon phenomenon, PlacableItem triggerItem) {
		Vector3 position = triggerItem.GlobalPosition;
		if (phenomenon.ParticleEffect != null) {
			SpawnParticleEffect(phenomenon, position);
		}
		if (phenomenon.PlaySound && phenomenon.SoundEffect != null) {
			PlaySoundEffect(phenomenon.SoundEffect, position);
		}
		if (phenomenon.ShowMessage && !string.IsNullOrEmpty(phenomenon.ResultMessage)) {
			ShowMessage(phenomenon.ResultMessage, position);
		}
		if (phenomenon.ProduceNewItem && phenomenon.ProducedItemScene != null) {
			ProduceNewItem(phenomenon.ProducedItemScene, position);
		}
		if (phenomenon.ConsumeItems) {
			ConsumeItem(triggerItem);
		}
	}
	
	private void SpawnParticleEffect(ExperimentPhenomenon phenomenon, Vector3 position) {
		if (EffectsParent == null) return;
		var effect = phenomenon.ParticleEffect.Instantiate<Node3D>();
		effect.GlobalPosition = position;
		EffectsParent.AddChild(effect);
		if (effect is GpuParticles3D particles) {
			particles.Emitting = true;
		}
		var timer = GetTree().CreateTimer(phenomenon.EffectDuration);
		timer.Timeout += () => {
			if (GodotObject.IsInstanceValid(effect)) {
				effect.QueueFree();
			}
		};
		string key = $"effect_{phenomenon.PhenomenonName}";
		activeEffects[key] = effect;
		GD.Print($"[PhenomenonManager] 生成粒子效果：{phenomenon.PhenomenonName} at {position}");
	}
	
	private void PlaySoundEffect(AudioStream sound, Vector3 position) {
		if (audioPlayer == null) return;
		audioPlayer.Stream = sound;
		audioPlayer.GlobalPosition = position;
		audioPlayer.Play();
		GD.Print($"[PhenomenonManager] 播放音效 at {position}");
	}
	
	private void ShowMessage(string message, Vector3 position) {
		if (messageLabel == null) return;
		messageLabel.Text = message;
		messageLabel.GlobalPosition = position + Vector3.Up * 0.5f;
		messageLabel.Modulate = new Color(1, 1, 0, 1);
		var tween = CreateTween();
		tween.TweenProperty(messageLabel, "modulate:a", 0.0f, 2.0f).SetDelay(1.0f);
		GD.Print($"[PhenomenonManager] 显示消息：{message}");
	}
	
	private void ProduceNewItem(PackedScene itemScene, Vector3 position) {
		if (EffectsParent == null) return;
		var newItem = itemScene.Instantiate<Node3D>();
		newItem.GlobalPosition = position + Vector3.Up * 0.2f;
		EffectsParent.AddChild(newItem);
		GD.Print($"[PhenomenonManager] 生成新物品 at {position}");
	}
	
	private void ConsumeItem(PlacableItem item) {
		var tween = CreateTween();
		tween.TweenProperty(item, "scale", Vector3.Zero, 0.5f);
		tween.TweenCallback(Callable.From(() => {
			if (GodotObject.IsInstanceValid(item)) {
				item.QueueFree();
			}
		}));
		
		GD.Print($"[PhenomenonManager] 消耗物品：{item.ItemName}");
	}
	
	public void ClearAllEffects() {
		foreach (var effect in activeEffects.Values) {
			if (GodotObject.IsInstanceValid(effect)) {
				effect.QueueFree();
			}
		}
		activeEffects.Clear();
		foreach (var timer in activeTimers.Values) {
			if (GodotObject.IsInstanceValid(timer)) {
				timer.QueueFree();
			}
		}
		activeTimers.Clear();
	}
}
