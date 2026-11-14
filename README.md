# 高级世界生成 — tPlainModLoader 移植

把 tModLoader 的 **Advanced World Generation**（作者 Ishigh，MIT）核心功能移植到 **tPlainModLoader / Terraria 1.4.5.8**。

原项目：https://github.com/Ishigh1/AdvancedWorldGen

## 已实现功能

### 1. 官方种子选项（动态）

- 直接枚举原版 `WorldGenerationOptions.Options`，1.4.5 官方种子菜单有哪些项，这里就有哪些；
- 中文名 + 标准种子文本 + 悬停显示游戏内描述；
- 「常规世界」放在最后，勾选它会清空其它选项；
- 入口：世界创建界面点**种子栏右侧的齿轮**。

### 2. 秘密种子（1.4.5 新增 37 个）

- 按官方中文 Wiki 分类分组（世界刷漆 / 世界生成 / 生物群系 / 结构 / 被动 / 生成点 / 其他）；
- 每项显示中文名 + 标准种子文本 + 悬停显示游戏内描述；
- 每项前面有对应物品图标；
- 开关直接调用 `SecretSeed.Enable/Disable` 并写入世界种子串，**无需先手动解锁**；
- 支持「全部启用 / 全部关闭」。

### 3. 扩展选项

| 选项 | 说明 |
|---|---|
| 海滩出生 | 移植 `Celebrationmk10.BeachSpawn`，生成后把出生点移到海边 |
| 彩漆着色 | 移植 `Celebrationmk10.Painted`，按节庆配色给全世界刷漆 |
| 随机替换方块/墙 | 移植 `Random`，每种实心方块/墙映射成另一种随机类型 |
| 随机上色 | 移植 `Random.Painted`，随机替换后再随机刷漆 |

### 4. 自定义世界大小（突破原版 8400×2400）

- 面板里可直接**输入数字**或点预设（小/中/大/超大/极限）；
- 最大支持 **10000 × 3000**（32 位进程 + LAA 下实测可用）；
- 超过原版尺寸时自动处理这些写死尺寸的系统：
  - `Main.tile` / `Main.Map` 重新分配；
  - `MapRenderer` 分块网格 + 新块清空；
  - `ActiveSections.LastActiveTime`；
  - `WorldSections`（sectionManager）；
  - `LeashedEntity.BySection`；
- 冷启动读档前（`WorldGen.clearWorld` prefix）也会先扩容，避免“退出重进能进、重启加载失败”；
- 内存不足会自动缩小 / 回退，并写入日志。

### 5. 32 位优化

- 生成前按当前进程内存 + 新地图预估做预算检查，自动缩小尺寸；
- 生成后执行一次 GC 并记录内存；
- 附带 `tools/enable-laa.ps1`：给 Terraria.exe 开启 **Large Address Aware**，32 位进程可用 ~4GB 而不是 ~2GB（脚本自动备份为 `Terraria.exe.bak`）。

## 安装

把以下文件放到 `<Terraria>/Mods/AdvancedWorldGen/`：

```
AdvancedWorldGen.dll
info.json
loadConfig.json
ico.png
```

## 使用

- 世界创建界面 → 种子栏右侧齿轮 → 「种子选项」；
- 「秘密种子 ▸」进入秘密种子列表；
- 「世界大小 ▸」设置自定义尺寸；
- 也可以在「模组设置 → 高级世界生成」里调整。

配置保存在 `Mods\AdvancedWorldGen\config.json`。

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

需要 .NET SDK（自带 Roslyn）和游戏目录 `D:\steam\steamapps\common\Terraria`。

## 目录

```
src/
  ThisMod.cs            入口
  WorldGenHooks.cs      Harmony：Reset / GenerateWorld / setWorldSize / LoadWorld / clearWorld / checkMap
  SeedOptions.cs        把配置写进 WorldGenerationOptions
  SeedCatalog.cs        官方选项 + 中文名/描述/备用图标
  PostGen.cs            生成后处理：彩漆、海滩出生、随机替换
  WorldCreationUI.cs    世界创建界面面板（齿轮入口、种子输入、选项列表）
  SecretSeedsUI.cs      秘密种子页
  SizeUI.cs             世界大小页
  SettingPage.cs        游戏内模组设置页
  AwgConfig.cs          配置读写
  Log.cs
tools/enable-laa.ps1    32 位 LAA 补丁
assets/ico.png          模组图标
LICENSE                 MIT（原项目）
```

## 已知限制

- 32 位进程上限：10000×3000 已接近可用边界，建议开 LAA 并不要长期使用极限尺寸；
- 超出原版尺寸时，地图最右/最下边缘会显示空白（不会崩，属于分块贴图尺寸限制）；
- 原模组的 Star 小游戏、预设/剪贴板导入导出、世界生成重写（BetterVanillaWorldGen）尚未移植。

## 许可 / 致谢

- 原模组 **Advanced World Generation** by **Ishigh**：https://github.com/Ishigh1/AdvancedWorldGen （MIT）；
- 本项目沿用 MIT，图标来自原项目。
