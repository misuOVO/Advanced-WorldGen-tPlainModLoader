using System;
using Terraria.UI;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;

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
        }

        private UIItemSwitch Switch(string label, Func<bool> get, Action<bool> set)
        {
            UIItemSwitch item = new UIItemSwitch(null, label);
            item.SetVal(get());
            item.OnValUpdate = v =>
            {
                set(v);
                Commit();
            };
            return item;
        }

        private UIItemValueSlider Slider(string label, int min, int max, Func<int> get, Action<int> set)
        {
            UIItemValueSlider item = new UIItemValueSlider(min, max, null, label);
            item.FloatToString = v => v.ToString("0");
            item.SetVal(get());
            item.OnValUpdate = v =>
            {
                set((int)Math.Round(v));
                Commit();
            };
            return item;
        }

        public override UIElement GetUI()
        {
            UIScrollViewer2 root = new UIScrollViewer2();
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
                item.SetVal(IsOn(captured));
                item.OnValUpdate = v =>
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

            panel.Append(Slider("宽度（格）", 4200, 12000,
                () => M.CustomWidth, v => M.CustomWidth = v));

            panel.Append(Slider("高度（格）", 1200, 3600,
                () => M.CustomHeight, v => M.CustomHeight = v));

            panel.Append(Switch("省内存生成（生成前释放旧世界/地图数据）",
                () => M.MemorySaver, v => M.MemorySaver = v));

            panel.Append(new UIItemTitle(null, "也可以在创建世界界面点种子栏右侧的齿轮设置。"));

            panel.Append(new UIItemTitle(null,
                "原版配置修改：世界创建界面 → 齿轮 → 原版配置 ▸（宝箱概率、沙丘高度、地牢位置等）"));

            root.AddChild(panel);
            return root;
        }
    }
}
