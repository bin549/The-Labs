using Godot;

public static class ItemTypePresets {
    public const string ACID = "acid"; // 酸
    public const string BASE = "base"; // 碱
    public const string SALT = "salt"; // 盐
    public const string WATER = "water"; // 水
    public const string METAL = "metal"; // 金属
    public const string SODIUM = "sodium"; // 钠
    public const string OXYGEN = "oxygen"; // 氧气
    public const string HYDROGEN = "hydrogen"; // 氢气
    public const string CARBON_DIOXIDE = "carbon_dioxide"; // 二氧化碳
    public const string SILVER_NITRATE = "silver_nitrate"; // 硝酸银
    public const string SODIUM_CHLORIDE = "sodium_chloride"; // 氯化钠
    public const string COPPER_SULFATE = "copper_sulfate"; // 硫酸铜
    public const string SODIUM_HYDROXIDE = "sodium_hydroxide"; // 氢氧化钠
    public const string SOLID = "solid"; // 固体
    public const string LIQUID = "liquid"; // 液体
    public const string GAS = "gas"; // 气体
    public const string SOLUTION = "solution"; // 溶液
    public const string SATURATED_SOLUTION = "saturated_solution"; // 饱和溶液
    public const string FIRE = "fire"; // 火源
    public const string HEAT = "heat"; // 加热器
    public const string COOLING = "cooling"; // 冷却
    public const string CONTAINER = "container"; // 容器
    public const string MAGNET = "magnet"; // 磁铁
    public const string IRON = "iron"; // 铁
    public const string COMBUSTIBLE = "combustible"; // 可燃物
    public const string SOLUTE = "solute"; // 溶质
    public const string SOLVENT = "solvent"; // 溶剂
    public const string ENZYME = "enzyme"; // 酶
    public const string SUBSTRATE = "substrate"; // 底物

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
            ACID => new Color(1.0f, 0.2f, 0.2f), // 红色
            BASE => new Color(0.2f, 0.2f, 1.0f), // 蓝色
            WATER => new Color(0.5f, 0.7f, 1.0f), // 浅蓝
            METAL => new Color(0.7f, 0.7f, 0.7f), // 灰色
            SODIUM => new Color(0.9f, 0.9f, 0.5f), // 浅黄
            FIRE => new Color(1.0f, 0.5f, 0.0f), // 橙红
            MAGNET => new Color(0.8f, 0.0f, 0.0f), // 深红
            IRON => new Color(0.5f, 0.5f, 0.5f), // 深灰
            _ => Colors.White
        };
    }
}