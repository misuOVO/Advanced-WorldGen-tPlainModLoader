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

### 4. 原版世界生成配置（Worldgen Config）

- 把原版 `WorldGenConfiguration` 里的全部参数（共 72 项）搬到界面上：群系概率、沙丘高度、金字塔概率、大理石/花岗岩数量、地牢离海滩距离、埋藏宝箱数量、微型群系数量/长度等；
- 每行右侧就是输入框：**直接改、按回车**（或点到别处 / 停手约 0.6 秒）立刻保存，不用先选中再到底部应用；
- 支持**搜索**名称/路径、**只看已修改**、右键单项恢复默认、一键全部恢复默认；
- 「缩放方式」这类英文枚举做成**点击切换的中文选项**（不缩放 / 按世界宽度 / 按世界面积），不用手打英文；
- 已修改的项显示黄点，数值非法时输入框变红并提示；
- 通过 Harmony 后置于 `WorldGenConfiguration.FromEmbeddedPath`，新建世界时自动把覆盖值写进原版配置（与原模组 `ProcessWorldGenConfig` 等价）；
- 数值做了校验：整数/小数必须非负，`ScaleWith` 只接受 `None / WorldWidth / WorldArea`。

### 5. 自定义世界大小（突破原版 8400×2400）

- 面板里可直接**输入数字**或点预设（小 / 中 / 大 / 超大 / 极限 / 巨构）；
- 最大支持 **12000 × 3600**（需要 32 位 LAA + 足够内存，预算不够会自动缩小）；
- **省内存生成**（新）：生成前先释放旧世界的方块数组、地图存档数据和渲染缓存，再做一次带大对象堆压缩的完整 GC；旧世界的 `Tile` 对象会**搬进新数组复用**（每格 24 字节，扩容时能省几百 MB）；
- 「推荐尺寸」按钮会按当前进程内存 + LAA 状态，直接选出跑得动的最大预设；
- 面板实时显示「当前内存 / 新世界预计内存 / 预算」；
- 超过原版尺寸时自动处理这些写死尺寸的系统：
  - `Main.tile` / `Main.Map` 重新分配；
  - `MapRenderer` 分块网格 + 新块清空；
  - `ActiveSections.LastActiveTime`；
  - `WorldSections`（sectionManager）；
  - `LeashedEntity.BySection`；
- 冷启动读档前（`WorldGen.clearWorld` prefix）也会先扩容，避免“退出重进能进、重启加载失败”；
- 内存不足会自动缩小 / 回退，并写入日志。

### 6. 生成界面显示预计时间

- 世界生成进度条下方显示：**已用时间 · 预计剩余时间 · 总进度百分比**；
- 时间格式为「时/分/秒」，例如 `已用 0时12分34秒 · 预计剩余 0时03分21秒 (45.2%)`；
- 进度来自原版 `WorldGenerator.CurrentGenerationProgress`，用指数平滑避免剩余时间乱跳；
- 作为 `UIWorldLoad` 的子元素绘制，缩放和位置跟随原版进度条。

### 7. 32 位优化

- 生成前按当前进程内存 + 新地图预估做预算检查，自动缩小尺寸（LAA 预算 3.4 GB，未开 LAA 1.5 GB）；
- 生成前 / 生成后各做一次 GC + LOH 压缩，并记录内存；
- 附带 `tools/enable-laa.ps1`：给 **Terraria.exe / tPlainModLoader.exe / tPlainModLoaderInjector.exe** 统一开启 **Large Address Aware**，32 位进程可用 ~4GB 而不是 ~2GB（自动备份为 `.bak`）。

> **注意**：如果你是用 `tPlainModLoader.exe` 启动游戏，游戏实际运行在 `tPlainModLoader.exe` 进程里，
> 所以**这个 exe 也必须开启 LAA**（脚本会自动处理），只给 Terraria.exe 开是不够的。
> 脚本需要在**游戏完全关闭**时运行；Steam 校验或加载器更新后重跑一次即可。

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
- 「原版配置 ▸」修改原版世界生成参数；
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
  WorldGenHooks.cs      Harmony：Reset / GenerateWorld / setWorldSize / LoadWorld / clearWorld / checkMap / FromEmbeddedPath
  SeedOptions.cs        把配置写进 WorldGenerationOptions
  SeedCatalog.cs        官方选项 + 中文名/描述/备用图标
  WorldgenConfig.cs     原版 WorldGenConfiguration 覆盖读写
  WorldgenConfigUI.cs   原版世界生成配置编辑界面
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

- 32 位进程上限：12000×3600 已接近可用边界，建议开 LAA 并配合「省内存生成」，不要长期使用极限尺寸；
- 超出原版尺寸时，地图最右/最下边缘会显示空白（不会崩，属于分块贴图尺寸限制）；
- 原模组的 Star 小游戏、预设/剪贴板导入导出、世界生成重写（BetterVanillaWorldGen）尚未移植。

## 许可 / 致谢

- 原模组 **Advanced World Generation** by **Ishigh**：https://github.com/Ishigh1/AdvancedWorldGen （MIT）；
- 本项目沿用 MIT，图标来自原项目。
