using Godot;
using System.Collections.Generic;

public partial class Page : Control {
    private Label numberLabel;
    private RichTextLabel textLabel;

    private Dictionary<int, string> pageContents = new Dictionary<int, string> {
        { 1, "[center][b]实验室操作指南[/b][/center]\n\n欢迎来到科学探究实验室！\n\n本手册将帮助你熟悉实验室的各项操作。" }, 
        {
            2,
            "[b]基础移动[/b]\n\n[color=cyan]WASD[/color] - 移动\n[color=cyan]空格[/color] - 跳跃\n[color=cyan]Shift[/color] - 跑步模式\n\n在实验室中自由行走，探索各个实验区域。"
        },
        { 3, "[b]视角控制[/b]\n\n[color=cyan]鼠标移动[/color] - 旋转视角\n[color=cyan]T键[/color] - 切换视角\n\n调整合适的视角以便更好地观察实验。" },
        { 4, "[b]交互操作[/b]\n\n[color=cyan]E键[/color] - 与物体交互\n[color=cyan]Esc键[/color] - 退出交互\n\n靠近可交互物体时，按E键进行交互。" },
        { 5, "[b]实验菜单[/b]\n\n[color=cyan]P键[/color] - 打开实验菜单\n\n通过实验菜单可以快速传送到不同的实验区域：\n• 力学物理\n• 电学物理\n• 化学实验" }, 
        {
            6, "[b]实验类型[/b]\n\n[color=yellow]力学实验[/color]\n研究摩擦力、牛顿定律等力学现象。\n\n[color=yellow]电学实验[/color]\n探究欧姆定律、电路原理。"
        },
        { 7, "[b]实验类型（续）[/b]\n\n[color=lightgreen]化学实验[/color]\n观察铝与氢氧化钠的反应等化学现象。\n\n每个实验都有详细的操作指导。" },
        { 8, "[b]安全提示[/b]\n\n• 仔细阅读实验说明\n• 按照正确步骤操作\n• 观察实验现象\n• 记录实验数据\n\n祝你实验愉快！" }
    };

    public override void _Ready() {
        this.numberLabel = GetNode<Label>("Background/Number");
        this.textLabel = GetNode<RichTextLabel>("Background/Text");
    }

    public void SetNumber(int value) {
        this.numberLabel.Text = $"- {value} -";
        this.textLabel.Text = this.GetPageContent(value);
    }

    private string GetPageContent(int pageNumber) {
        if (this.pageContents.ContainsKey(pageNumber)) {
            return this.pageContents[pageNumber];
        }
        if (pageNumber > this.pageContents.Count) {
            return "[center][i]--- 空白页 ---[/i][/center]\n\n此页暂无内容。";
        }
        return "";
    }
}