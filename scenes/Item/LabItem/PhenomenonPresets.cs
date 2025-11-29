using Godot;
using System.Collections.Generic;

public static class PhenomenonPresets {
	
	#region 化学反应现象
	
	public static ExperimentPhenomenon CreateAcidBaseReaction() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "酸碱中和反应";
		phenomenon.Description = "酸和碱发生中和反应，生成盐和水，并放出热量";
		phenomenon.TriggerItemType = "acid";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "base" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 0.0f;
		phenomenon.EffectColor = new Color(1.0f, 1.0f, 0.0f); // 黄色
		phenomenon.EffectDuration = 3.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "⚗️ 酸碱中和反应发生！\n溶液温度升高，pH值趋向中性\n生成盐和水";
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	public static ExperimentPhenomenon CreateMetalAcidReaction() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "金属与酸反应";
		phenomenon.Description = "活泼金属与酸反应产生氢气";
		phenomenon.TriggerItemType = "metal";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "acid" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 0.2f;
		phenomenon.EffectColor = new Color(0.0f, 1.0f, 1.0f); // 青色
		phenomenon.EffectDuration = 5.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "💨 金属溶解，产生大量气泡！\n检验证明是氢气（H₂）\n化学方程式：M + 2H⁺ → M²⁺ + H₂↑";
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	public static ExperimentPhenomenon CreateSodiumWaterReaction() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "钠与水的反应";
		phenomenon.Description = "钠与水发生剧烈反应，产生氢气和氢氧化钠";
		phenomenon.TriggerItemType = "sodium";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "water" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 0.3f;
		phenomenon.EffectColor = new Color(1.0f, 0.5f, 0.0f); // 橙色
		phenomenon.EffectDuration = 4.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "⚠️ 危险！钠与水剧烈反应！\n钠块在水面快速游动\n产生嘶嘶声和火焰\n2Na + 2H₂O → 2NaOH + H₂↑";
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	public static ExperimentPhenomenon CreatePrecipitationReaction() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "沉淀反应";
		phenomenon.Description = "两种溶液混合产生不溶于水的沉淀";
		phenomenon.TriggerItemType = "silver_nitrate";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "sodium_chloride" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 0.1f;
		phenomenon.EffectColor = Colors.White;
		phenomenon.EffectDuration = 3.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "🌫️ 产生白色沉淀！\n这是氯化银（AgCl）\nAgNO₃ + NaCl → AgCl↓ + NaNO₃";
		phenomenon.ProduceNewItem = false;
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	public static ExperimentPhenomenon CreateCombustionReaction() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "燃烧反应";
		phenomenon.Description = "可燃物在氧气中燃烧";
		phenomenon.TriggerItemType = "combustible";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "oxygen" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 0.5f;
		phenomenon.EffectColor = new Color(1.0f, 0.3f, 0.0f); // 火焰红
		phenomenon.EffectDuration = 5.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "🔥 物质剧烈燃烧！\n发出耀眼的光芒\n温度迅速升高\n燃烧产物释放到空气中";
		phenomenon.ConsumeItems = true;
		return phenomenon;
	}
	
	#endregion
	
	#region 物理现象
	
	public static ExperimentPhenomenon CreateDissolutionPhenomenon() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "溶解现象";
		phenomenon.Description = "溶质在溶剂中溶解形成溶液";
		phenomenon.TriggerItemType = "solute";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "solvent" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 1.0f;
		phenomenon.EffectColor = new Color(0.5f, 0.8f, 1.0f); // 淡蓝
		phenomenon.EffectDuration = 3.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "💧 物质逐渐溶解...\n溶液变得均匀透明\n溶质分子均匀分散在溶剂中";
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	public static ExperimentPhenomenon CreateCrystallizationPhenomenon() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "结晶现象";
		phenomenon.Description = "饱和溶液中析出晶体";
		phenomenon.TriggerItemType = "saturated_solution";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "cooling" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 2.0f;
		phenomenon.EffectColor = Colors.LightBlue;
		phenomenon.EffectDuration = 4.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "💎 晶体开始析出！\n观察到美丽的晶体形成\n溶液中出现固体颗粒";
		phenomenon.ProduceNewItem = false;
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	public static ExperimentPhenomenon CreateBoilingPhenomenon() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "沸腾现象";
		phenomenon.Description = "液体加热至沸点产生气泡";
		phenomenon.TriggerItemType = "liquid";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "heat" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 1.5f;
		phenomenon.EffectColor = Colors.White;
		phenomenon.EffectDuration = 6.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "🌡️ 液体开始沸腾！\n大量气泡从底部升起\n温度保持在沸点\n液体快速汽化";
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	public static ExperimentPhenomenon CreateMagnetizationPhenomenon() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "磁化现象";
		phenomenon.Description = "磁铁吸引铁质物品";
		phenomenon.TriggerItemType = "magnet";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "iron" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 0.0f;
		phenomenon.EffectColor = new Color(0.8f, 0.0f, 0.8f); // 紫色
		phenomenon.EffectDuration = 2.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "🧲 磁力吸引！\n铁质物品被磁铁吸引\n观察到磁场效应";
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	#endregion
	
	#region 生物现象
	
	public static ExperimentPhenomenon CreateEnzymeCatalysisPhenomenon() {
		var phenomenon = new ExperimentPhenomenon();
		phenomenon.PhenomenonName = "酶催化反应";
		phenomenon.Description = "酶加速生化反应";
		phenomenon.TriggerItemType = "enzyme";
		phenomenon.RequiredItemTypes = new Godot.Collections.Array<string> { "substrate" };
		phenomenon.RequireAllItems = true;
		phenomenon.TriggerDelay = 0.5f;
		phenomenon.EffectColor = new Color(0.0f, 1.0f, 0.5f); // 绿色
		phenomenon.EffectDuration = 4.0f;
		phenomenon.ShowMessage = true;
		phenomenon.ResultMessage = "🧬 酶催化反应进行中！\n反应速度显著加快\n底物快速转化为产物";
		phenomenon.ConsumeItems = false;
		return phenomenon;
	}
	
	#endregion
	
	#region 工具方法
	
	public static List<ExperimentPhenomenon> GetAllPresets() {
		var presets = new List<ExperimentPhenomenon> {
			CreateAcidBaseReaction(),
			CreateMetalAcidReaction(),
			CreateSodiumWaterReaction(),
			CreatePrecipitationReaction(),
			CreateCombustionReaction(),
			CreateDissolutionPhenomenon(),
			CreateCrystallizationPhenomenon(),
			CreateBoilingPhenomenon(),
			CreateMagnetizationPhenomenon(),
			CreateEnzymeCatalysisPhenomenon()
		};
		return presets;
	}
	
	public static ExperimentPhenomenon GetPresetByName(string name) {
		return name switch {
			"acid_base" => CreateAcidBaseReaction(),
			"metal_acid" => CreateMetalAcidReaction(),
			"sodium_water" => CreateSodiumWaterReaction(),
			"precipitation" => CreatePrecipitationReaction(),
			"combustion" => CreateCombustionReaction(),
			"dissolution" => CreateDissolutionPhenomenon(),
			"crystallization" => CreateCrystallizationPhenomenon(),
			"boiling" => CreateBoilingPhenomenon(),
			"magnetization" => CreateMagnetizationPhenomenon(),
			"enzyme" => CreateEnzymeCatalysisPhenomenon(),
			_ => null
		};
	}
	
	public static Godot.Collections.Array<ExperimentPhenomenon> GetChemistryPresets() {
		var presets = new Godot.Collections.Array<ExperimentPhenomenon> {
			CreateAcidBaseReaction(),
			CreateMetalAcidReaction(),
			CreateSodiumWaterReaction(),
			CreatePrecipitationReaction(),
			CreateCombustionReaction()
		};
		return presets;
	}
	
	public static Godot.Collections.Array<ExperimentPhenomenon> GetPhysicsPresets() {
		var presets = new Godot.Collections.Array<ExperimentPhenomenon> {
			CreateDissolutionPhenomenon(),
			CreateCrystallizationPhenomenon(),
			CreateBoilingPhenomenon(),
			CreateMagnetizationPhenomenon()
		};
		return presets;
	}
	#endregion
}

public static class ItemTypePresets {
	public const string ACID = "acid";                    // 酸
	public const string BASE = "base";                    // 碱
	public const string SALT = "salt";                    // 盐
	public const string WATER = "water";                  // 水
	public const string METAL = "metal";                  // 金属
	public const string SODIUM = "sodium";                // 钠
	public const string OXYGEN = "oxygen";                // 氧气
	public const string HYDROGEN = "hydrogen";            // 氢气
	public const string CARBON_DIOXIDE = "carbon_dioxide"; // 二氧化碳
	public const string SILVER_NITRATE = "silver_nitrate";     // 硝酸银
	public const string SODIUM_CHLORIDE = "sodium_chloride";   // 氯化钠
	public const string COPPER_SULFATE = "copper_sulfate";     // 硫酸铜
	public const string SODIUM_HYDROXIDE = "sodium_hydroxide"; // 氢氧化钠
	public const string SOLID = "solid";                  // 固体
	public const string LIQUID = "liquid";                // 液体
	public const string GAS = "gas";                      // 气体
	public const string SOLUTION = "solution";            // 溶液
	public const string SATURATED_SOLUTION = "saturated_solution"; // 饱和溶液
	public const string FIRE = "fire";                    // 火源
	public const string HEAT = "heat";                    // 加热器
	public const string COOLING = "cooling";              // 冷却
	public const string CONTAINER = "container";          // 容器
	public const string MAGNET = "magnet";                // 磁铁
	public const string IRON = "iron";                    // 铁
	public const string COMBUSTIBLE = "combustible";      // 可燃物
	public const string SOLUTE = "solute";                // 溶质
	public const string SOLVENT = "solvent";              // 溶剂
	public const string ENZYME = "enzyme";                // 酶
	public const string SUBSTRATE = "substrate";          // 底物
	
	public static string[] GetAllTypes() {
		return new string[] {
			ACID, BASE, SALT, WATER, METAL, SODIUM, OXYGEN, HYDROGEN, CARBON_DIOXIDE,
			SILVER_NITRATE, SODIUM_CHLORIDE, COPPER_SULFATE, SODIUM_HYDROXIDE,
			SOLID, LIQUID, GAS, SOLUTION, SATURATED_SOLUTION,
			FIRE, HEAT, COOLING, CONTAINER, MAGNET, IRON,
			COMBUSTIBLE, SOLUTE, SOLVENT, ENZYME, SUBSTRATE
		};
	}
	
	public static string GetChineseName(string type) {
		return type switch {
			ACID => "酸",
			BASE => "碱",
			SALT => "盐",
			WATER => "水",
			METAL => "金属",
			SODIUM => "钠",
			OXYGEN => "氧气",
			HYDROGEN => "氢气",
			CARBON_DIOXIDE => "二氧化碳",
			SILVER_NITRATE => "硝酸银",
			SODIUM_CHLORIDE => "氯化钠",
			COPPER_SULFATE => "硫酸铜",
			SODIUM_HYDROXIDE => "氢氧化钠",
			SOLID => "固体",
			LIQUID => "液体",
			GAS => "气体",
			SOLUTION => "溶液",
			SATURATED_SOLUTION => "饱和溶液",
			FIRE => "火源",
			HEAT => "加热器",
			COOLING => "冷却",
			CONTAINER => "容器",
			MAGNET => "磁铁",
			IRON => "铁",
			COMBUSTIBLE => "可燃物",
			SOLUTE => "溶质",
			SOLVENT => "溶剂",
			ENZYME => "酶",
			SUBSTRATE => "底物",
			_ => "未知"
		};
	}
	
	public static Color GetRecommendedColor(string type) {
		return type switch {
			ACID => new Color(1.0f, 0.2f, 0.2f),        // 红色
			BASE => new Color(0.2f, 0.2f, 1.0f),        // 蓝色
			WATER => new Color(0.5f, 0.7f, 1.0f),       // 浅蓝
			METAL => new Color(0.7f, 0.7f, 0.7f),       // 灰色
			SODIUM => new Color(0.9f, 0.9f, 0.5f),      // 浅黄
			FIRE => new Color(1.0f, 0.5f, 0.0f),        // 橙红
			MAGNET => new Color(0.8f, 0.0f, 0.0f),      // 深红
			IRON => new Color(0.5f, 0.5f, 0.5f),        // 深灰
			_ => Colors.White
		};
	}
}

