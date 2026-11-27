using System;
using Terraria.UI;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.Item1;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 游戏内“模组设置”页面：动态列出原版所有世界选项 + 扩展选项。
    /// </summary>
    public class AwgSetting : ModSetting
    {
        public override string Name => "AdvancedWorldGen";
        public override string Title => "高级世界生成";
        public override string FilePath => "config.json";
        public override bool HasUI => true;
        public override Type DataType => typeof(AwgConfig);

        private static AwgConfig M => AwgConfig.Instance;

        public override void SetDefault()
        {
            AwgConfig.ResetToDefault();
        }

        public override void Load(object v)
        {
            if (v is AwgConfig cfg)
                AwgConfig.Instance = cfg;
        }

        public override object GetSaveData()
        {
            return M;
        }

        private void Commit()
        {
            NeedSave = true;
            Save();
        }

        private static bool IsOn(SeedEntry entry)
        {
            if (entry.IsNormal)
                return M.Enabled == null || M.Enabled.Count == 0 || M.Has(entry.Key);

            return M.Has(entry.Key);
        }

        private static void Toggle(SeedEntry entry, bool value)
        {
            SeedEntry normal = SeedCatalog.Normal();

            if (entry.IsNormal)
            {
                M.Enabled.Clear();
                M.Enabled.Add(entry.Key);
                return;
            }

            if (normal != null)
                M.Enabled.Remove(normal.Key);

            M.Set(entry.Key, value);
            AwgConfig.ApplyAutoCustomSize(M);
        }

        private UIItemSwitch Switch(string label, Func<bool> get, Action<bool> set)
        {
            UIItemSwitch item = new UIItemSwitch(null, label);
            item.OnGetIsChecked = get;
            item.OnSetIsChecked = v =>
            {
                set(v);
                Commit();
            };
            return item;
        }

        private UIItemValueSliderInt Slider(string label, int min, int max, Func<int> get, Action<int> set)
        {
            UIItemValueSliderInt item = new UIItemValueSliderInt(min, max, null, label);
            item.ValueToString = v => v.ToString("0");
            item.OnGetValue = get;
            item.OnSetValue = v =>
            {
                set(v);
                Commit();
            };
            return item;
        }

        public override UIElement GetUI()
        {
            UIScrollViewer2 root = new UIScrollViewer2(false);
            root.Width.Precent = 1f;
            root.Height.Precent = 1f;
            root.ItemMargin = 4;

            UIStackPanel panel = new UIStackPanel
            {
                Width = { Precent = 1f },
                IsAutoUpdateSize = true
            };

            panel.Append(new UIItemTitle(null, "世界选项（新建世界时生效）"));

            foreach (SeedEntry entry in SeedCatalog.All())
            {
                SeedEntry captured = entry;

                UIItemSwitch item = new UIItemSwitch(null, captured.Title);
                item.OnGetIsChecked = () => IsOn(captured);
                item.OnSetIsChecked = v =>
                {
                    Toggle(captured, v);
                    Commit();
                };
                panel.Append(item);
            }

            panel.Append(new UIItemTitle(null, "扩展选项"));

            panel.Append(Switch("海滩出生（需十周年庆典）",
                () => M.CelebrationBeachSpawn, v => M.CelebrationBeachSpawn = v));

            panel.Append(Switch("彩漆着色（需十周年庆典）",
                () => M.CelebrationPainted, v => M.CelebrationPainted = v));

            panel.Append(Switch("随机替换方块/墙",
                () => M.RandomWorld, v => M.RandomWorld = v));

            panel.Append(Switch("随机上色",
                () => M.RandomPainted, v => M.RandomPainted = v));

            panel.Append(new UIItemTitle(null, "自定义世界大小（可超过原版上限）"));

            panel.Append(Switch("启用自定义大小",
                () => M.CustomSize, v => M.CustomSize = v));

            panel.Append(Slider("宽度（格）", 1800, 12000,
                () => M.CustomWidth, v => M.CustomWidth = v));

            panel.Append(Slider("高度（格）", 600, 3600,
                () => M.CustomHeight, v => M.CustomHeight = v));

            panel.Append(Switch("省内存生成（生成前释放旧世界/地图数据）",
                () => M.MemorySaver, v => M.MemorySaver = v));

            panel.Append(new UIItemTitle(null, "世界创建设置"));
            panel.Append(Switch("保存上一次设置（创建世界后不恢复默认）",
                () => M.RememberSettings, v => M.RememberSettings = v));
            panel.Append(new UIItemTitle(null,
                "关闭时：每次创建完世界，种子选项/秘密种子/世界大小/原版配置都会恢复默认。"));

            panel.Append(new UIItemTitle(null, "也可以在创建世界界面点种子栏右侧的齿轮设置。"));

            panel.Append(new UIItemTitle(null,
                "原版配置修改：世界创建界面 → 齿轮 → 原版配置 ▸（宝箱概率、沙丘高度、地牢位置等）"));

            panel.Append(new UIItemTitle(null,
                "LAA（Large Address Aware）：让 32 位进程使用约 4GB 而不是 2GB，改完需要重启游戏"));

            UIItemSwitch laa = new UIItemSwitch(null, "启用 LAA（Terraria / tPlainModLoader）");
            laa.OnGetIsChecked = () => LaaToggle.IsOn();
            laa.OnSetIsChecked = v =>
            {
                bool ok = LaaToggle.SetEnabled(v);

                // LAA 不属于 config.json，直接写日志即可
                Log.Write("AWG: LAA 开关 -> " + (v ? "开" : "关") + (ok ? "" : "（部分失败）"));
            };
            panel.Append(laa);

            panel.Append(new UIItemTitle(null,
                "注意：加载器更新后 LAA 会被重置，需要回来重新打开这个开关。"));

            root.AddChild(panel);
            return root;
        }
    }
}
