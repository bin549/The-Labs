# 摩擦力实验 - 快速设置指南

## 方法1：在现有LabItem上设置（推荐）

### 1. 修改LabItem脚本

在Godot编辑器中：
1. 打开 `scenes/Item/LabItem/LabItem.tscn`
2. 选择根节点 `LabItem`
3. 在Inspector的Script属性中，将 `LabItem.cs` 改为 `FrictionExperiment.cs`
4. 保存场景

### 2. 添加实验物块

在LabItem节点下添加：

```
LabItem (现在使用FrictionExperiment.cs)
├── Blocks (Node3D) - 新建
│   ├── BlockLight (ExperimentBlock.cs)
│   │   ├── Mesh (MeshInstance3D)
│   │   │   └── 设置为BoxMesh，Size(0.15, 0.15, 0.15)
│   │   └── Area3D
│   │       └── CollisionShape3D (BoxShape3D)
│   │
│   ├── BlockMedium (ExperimentBlock.cs)
│   │   └── (同上结构)
│   │
│   └── BlockHeavy (ExperimentBlock.cs)
│       └── (同上结构)
```

**物块属性配置**：

BlockLight:
- Script: ExperimentBlock.cs
- Block Name: "轻物块"
- Mass: 0.5
- Block Color: Blue (0, 0, 1, 1)
- Mesh Path: Mesh

BlockMedium:
- Script: ExperimentBlock.cs
- Block Name: "中等物块"  
- Mass: 1.0
- Block Color: Green (0, 1, 0, 1)
- Mesh Path: Mesh

BlockHeavy:
- Script: ExperimentBlock.cs
- Block Name: "重物块"
- Mass: 2.0
- Block Color: Red (1, 0, 0, 1)
- Mesh Path: Mesh

**物块位置**：
- BlockLight: (0.3, 0.8, 0)
- BlockMedium: (0, 0.8, 0)
- BlockHeavy: (-0.3, 0.8, 0)

### 3. 添加实验平台

```
LabItem
├── SurfacePlatform (Node3D) - 新建
    └── Platform (MeshInstance3D)
        └── 设置为BoxMesh，Size(1.0, 0.05, 0.6)
```

Position: (0, 0.6, -0.3)

### 4. 添加测力计

```
LabItem
├── ForceMeter (ForceMeter.cs) - 新建
    ├── Pointer (Node3D)
    │   └── PointerMesh (MeshInstance3D - CylinderMesh)
    └── DisplayLabel (Label3D)
```

ForceMeter Position: (0.5, 0.9, -0.3)
- Max Force: 50.0
- Max Rotation: 180.0

### 5. 配置FrictionExperiment节点

选择LabItem节点，在Inspector中设置：

**Block Paths数组**（点击+添加3个元素）：
- [0]: Blocks/BlockLight
- [1]: Blocks/BlockMedium
- [2]: Blocks/BlockHeavy

**其他路径**：
- Force Meter Path: ForceMeter
- Surface Platform Path: SurfacePlatform

### 6. 连接Area3D信号

对每个物块：
1. 选择 BlockLight/Area3D
2. 转到Node标签（信号）
3. 双击 `mouse_entered` 信号
4. 连接到 BlockLight 节点
5. 方法名: `_OnArea3DMouseEntered`
6. 同样连接 `mouse_exited` 到 `_OnArea3DMouseExited`
7. 对其他两个物块重复

### 7. 设置Area3D属性

对每个物块的Area3D：
- Monitoring: true
- Monitorable: true
- Input Ray Pickable: true

### 8. 测试

1. 运行场景
2. 走到LabItem前，按E进入实验
3. 按照步骤操作实验

## 方法2：在World.tscn中直接配置

### 修改World.tscn

打开 `scenes/World/World.tscn`，找到Labs/LabItem节点：

```gdscript
[node name="LabItem" parent="Labs" ...]
# 将场景改为使用FrictionExperiment
```

然后在场景中添加上述的Blocks、SurfacePlatform和ForceMeter。

## 快速创建工具（可选）

可以在Godot编辑器中运行这个工具脚本快速创建节点结构：

```gdscript
@tool
extends EditorScript

func _run():
    var lab_item = get_scene().get_node("LabItem")
    if lab_item == null:
        print("找不到LabItem节点")
        return
    
    # 创建Blocks容器
    var blocks_container = Node3D.new()
    blocks_container.name = "Blocks"
    lab_item.add_child(blocks_container)
    blocks_container.owner = get_scene()
    
    # 创建3个物块
    var block_configs = [
        {"name": "BlockLight", "mass": 0.5, "color": Color.BLUE, "pos": Vector3(0.3, 0.8, 0)},
        {"name": "BlockMedium", "mass": 1.0, "color": Color.GREEN, "pos": Vector3(0, 0.8, 0)},
        {"name": "BlockHeavy", "mass": 2.0, "color": Color.RED, "pos": Vector3(-0.3, 0.8, 0)}
    ]
    
    for config in block_configs:
        var block = create_block(config)
        blocks_container.add_child(block)
        block.owner = get_scene()
    
    print("实验节点创建完成！")

func create_block(config):
    var block = Node3D.new()
    block.name = config.name
    block.position = config.pos
    
    var mesh_inst = MeshInstance3D.new()
    mesh_inst.name = "Mesh"
    var box_mesh = BoxMesh.new()
    box_mesh.size = Vector3(0.15, 0.15, 0.15)
    mesh_inst.mesh = box_mesh
    block.add_child(mesh_inst)
    mesh_inst.owner = get_scene()
    
    var area = Area3D.new()
    area.name = "Area3D"
    var collision = CollisionShape3D.new()
    var shape = BoxShape3D.new()
    shape.size = Vector3(0.15, 0.15, 0.15)
    collision.shape = shape
    area.add_child(collision)
    block.add_child(area)
    area.owner = get_scene()
    collision.owner = get_scene()
    
    return block
```

将此脚本保存为 `setup_friction_experiment.gd`，在Godot中 File -> Run 运行。

## 常见问题

### Q: 物块无法拖拽
A: 检查Area3D是否正确设置，input_ray_pickable是否为true

### Q: 进入实验没有UI
A: UI会自动创建，如果没有出现，检查Console是否有错误

### Q: 测力计不显示
A: 测力计会自动创建默认显示，确保ForceMeter脚本已附加

### Q: 分析按钮不可用
A: 需要至少记录3组实验数据

## 下一步

设置完成后，查看 `README_FrictionExperiment.md` 了解详细使用说明。

