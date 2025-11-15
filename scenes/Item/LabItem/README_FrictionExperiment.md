# 摩擦力实验系统使用说明

## 概述
这是一个交互式摩擦力实验系统，模拟真实物理实验，探究滑动摩擦力的影响因素。参考《The Room》游戏的交互风格设计。

## 系统组成

### 1. FrictionExperiment.cs
主实验控制器，管理实验流程、UI和数据分析。

### 2. ExperimentBlock.cs
可拖拽的实验物块，不同质量模拟不同压力。

### 3. ForceMeter.cs
测力计，显示拉力大小（等于摩擦力）。

## 场景设置步骤

### 步骤1：创建实验场景

1. 在World.tscn中找到LabItem节点
2. 将LabItem的脚本改为`FrictionExperiment.cs`

### 步骤2：添加物块

```
LabItem (FrictionExperiment)
├── Blocks (Node3D)
│   ├── Block_Light (ExperimentBlock)  # 轻物块
│   │   └── MeshInstance3D (立方体)
│   ├── Block_Medium (ExperimentBlock) # 中等物块
│   │   └── MeshInstance3D (立方体)
│   └── Block_Heavy (ExperimentBlock)  # 重物块
│       └── MeshInstance3D (立方体)
```

**物块属性设置**：
- Block_Light: 
  - BlockName: "轻物块"
  - Mass: 0.5 kg
  - BlockColor: Blue
  
- Block_Medium:
  - BlockName: "中等物块"
  - Mass: 1.0 kg
  - BlockColor: Green
  
- Block_Heavy:
  - BlockName: "重物块"
  - Mass: 2.0 kg
  - BlockColor: Red

### 步骤3：添加测力计

```
LabItem (FrictionExperiment)
├── ForceMeter (ForceMeter)
│   ├── Pointer (Node3D)
│   │   └── MeshInstance3D (指针)
│   └── DisplayLabel (Label3D)
```

**测力计属性**：
- MaxForce: 50.0 N
- MaxRotation: 180.0 度

### 步骤4：添加实验平台

```
LabItem (FrictionExperiment)
├── SurfacePlatform (Node3D)
│   └── MeshInstance3D (平板)
```

### 步骤5：设置物块的鼠标检测

为每个物块添加Area3D进行鼠标交互：

```
Block_Light (ExperimentBlock)
├── MeshInstance3D
└── Area3D
    └── CollisionShape3D (BoxShape3D)
```

连接信号：
- Area3D.mouse_entered -> ExperimentBlock._OnArea3DMouseEntered
- Area3D.mouse_exited -> ExperimentBlock._OnArea3DMouseExited

### 步骤6：配置FrictionExperiment

在Inspector中设置：
- Block Paths: [NodePath to Block_Light, Block_Medium, Block_Heavy]
- Force Meter Path: NodePath to ForceMeter
- Surface Platform Path: NodePath to SurfacePlatform

## 实验流程

### 1. 实验介绍
显示实验目标、原理和步骤说明。

### 2. 选择物块
点击一个物块，观察不同质量。

### 3. 放置物块
拖拽物块到实验平台。

### 4. 选择接触面
从下拉菜单选择：木板、玻璃、布面、金属。

### 5. 拉动物块
点击"开始拉动"，测力计显示摩擦力。

### 6. 记录数据
点击"记录数据"，将结果保存到表格。

### 7. 改变条件
更换物块或接触面，重复实验至少3次。

### 8. 分析结论
查看数据分析和实验结论。

## 摩擦系数设置

不同接触面的动摩擦因数：
- 木板: 0.3
- 玻璃: 0.1
- 布面: 0.4
- 金属: 0.15

计算公式：摩擦力 f = μN = μmg

## 交互特性

### The Room风格特点：
1. **平滑的相机切换** - 进入实验时切换到专用摄像机
2. **精细的拖拽** - 物块拖拽平滑自然
3. **视觉反馈** - 悬停和拖拽时的高亮效果
4. **步骤引导** - 清晰的步骤说明和提示
5. **数据可视化** - 实时显示测力数据
6. **完整的实验循环** - 从选择到分析的完整流程

## 快捷键

- E: 进入实验
- ESC: 退出实验
- 鼠标左键: 选择/拖拽物块
- 鼠标悬停: 高亮显示可交互物体

## 扩展建议

1. **添加更多物块材质**
   - 木块、铁块、塑料块等

2. **添加更多接触面**
   - 砂纸、冰面、橡胶等

3. **添加动画效果**
   - 物块拉动动画
   - 测力计读数动画

4. **添加音效**
   - 拖拽音效
   - 物块放置音效
   - 数据记录提示音

5. **添加3D模型**
   - 更精细的测力计模型
   - 真实的物块模型
   - 实验台模型

## 注意事项

1. 确保所有NodePath正确设置
2. 物块需要Area3D才能检测鼠标
3. ExperimentUIPanel会自动创建，也可以手动预制
4. 至少需要3组实验数据才能进行分析
5. 建议准备不同质量的物块和不同粗糙程度的表面

## 调试提示

如果遇到问题：
1. 检查Console输出的警告信息
2. 确认NodePath是否正确
3. 确认信号连接是否正确
4. 检查物块的Area3D和CollisionShape3D设置
5. 确认GameManager存在且可访问

