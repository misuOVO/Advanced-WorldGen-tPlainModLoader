using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;
using tContentPatch.Content.UI;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 特殊种子组合：Terraria Wiki「特殊种子组合」页里的主要组合，
    /// 点击后直接把对应的官方种子选项写入配置。
    /// </summary>
    internal class SeedCombosState : UIState
    {
        private const string DefaultHint =
            "点击一个组合会直接替换当前勾选的种子选项；来源：Terraria Wiki「特殊种子组合」。";

        private readonly UIWorldCreation _previous;
        private readonly Dictionary<string, SeedEntry> _entries =
            new Dictionary<string, SeedEntry>(StringComparer.OrdinalIgnoreCase);

        private UIText _hintText;

        public SeedCombosState(UIWorldCreation previous)
        {
            _previous = previous;

            foreach (SeedEntry entry in SeedCatalog.All())
            {
                if (!string.IsNullOrEmpty(entry.Key))
                    _entries[entry.Key] = entry;
            }

            Build();
        }

        private void Build()
        {
            const int panelW = 780;
            const int panelH = 660;

            UIPanel panel = new UIPanel();
            panel.Width.Set(panelW, 0f);
            panel.Height.Set(panelH, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.SetPadding(0f);
            Append(panel);

            UIText title = new UIText("特殊种子组合", 1.0f, false);
            title.HAlign = 0.5f;
            title.Top.Set(10f, 0f);
            panel.Append(title);

            UIText subtitle = new UIText(
                "点击组合直接替换当前种子选项；鼠标悬停查看该组合的特殊效果。",
                0.78f);
            subtitle.HAlign = 0.5f;
            subtitle.Top.Set(42f, 0f);
            subtitle.TextColor = new Color(190, 205, 235);
            panel.Append(subtitle);

            UIScrollViewer2 scroll = new UIScrollViewer2(false);
            scroll.Left.Set(20f, 0f);
            scroll.Top.Set(72f, 0f);
            scroll.Width.Set(-40f, 1f);
            scroll.Height.Set(450f, 0f);
            scroll.ItemMargin = 2;
            panel.Append(scroll);

            foreach (SeedCombo combo in SeedCombos.All)
            {
                SeedCombo captured = combo;

                ToggleRow row = new ToggleRow(
                    TitleOf(captured),
                    null,
                    () => IsOn(captured),
                    () => Toggle(captured),
                    () => ShowHint(captured),
                    ResetHint,
                    () => IconOf(captured));

                row.Width.Set(-8f, 1f);
                row.Height.Set(40f, 0f);
                row.Left.Set(2f, 0f);
                scroll.AddChild(row);
            }

            Box hintBar = new Box(new Color(40, 52, 90, 180), new Color(90, 110, 170, 120));
            hintBar.Left.Set(20f, 0f);
            hintBar.Top.Set(532f, 0f);
            hintBar.Width.Set(-40f, 1f);
            hintBar.Height.Set(58f, 0f);
            panel.Append(hintBar);

            _hintText = new UIText(DefaultHint, 0.8f);
            _hintText.Left.Set(10f, 0f);
            _hintText.Top.Set(6f, 0f);
            _hintText.Width.Set(-20f, 1f);
            _hintText.Height.Set(-12f, 1f);
            _hintText.IsWrapped = true;
            _hintText.TextColor = new Color(210, 222, 245);
            hintBar.Append(_hintText);

            TextButton clear = MakeButton("清空（常规世界）", 20f, 602f, 190f);
            clear.OnLeftClick += (evt, el) => ClearAll();
            panel.Append(clear);

            TextButton back = MakeButton("返回", panelW - 170f, 602f, 150f);
            back.OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new SeedOptionsState(_previous));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 返回种子选项失败: " + e.Message);
                }
            };
            panel.Append(back);
        }

        private static TextButton MakeButton(string text, float left, float top, float width)
        {
            TextButton button = new TextButton(text);
            button.Left.Set(left, 0f);
            button.Top.Set(top, 0f);
            button.Width.Set(width, 0f);
            button.Height.Set(40f, 0f);
            return button;
        }

        private string TitleOf(SeedCombo combo)
        {
            List<string> names = new List<string>();

            foreach (string key in combo.Keys)
            {
                if (_entries.TryGetValue(key, out SeedEntry entry) && !string.IsNullOrEmpty(entry.Title))
                    names.Add(entry.Title);
                else
                    names.Add(key);
            }

            return string.Join(" + ", names);
        }

        private Texture2D IconOf(SeedCombo combo)
        {
            foreach (string key in combo.Keys)
            {
                if (!_entries.TryGetValue(key, out SeedEntry entry))
                    continue;

                Texture2D tex = SeedIcons.FromOption(entry.Option);

                if (tex == null)
                    tex = SeedIcons.FromItem(entry.FallbackIcon);

                if (tex != null)
                    return tex;
            }

            return null;
        }

        private static bool IsOn(SeedCombo combo)
        {
            AwgConfig cfg = AwgConfig.Instance;

            if (cfg.Enabled == null || cfg.Enabled.Count != combo.Keys.Length)
                return false;

            foreach (string key in combo.Keys)
            {
                if (!cfg.Has(key))
                    return false;
            }

            return true;
        }

        private void Apply(SeedCombo combo)
        {
            AwgConfig cfg = AwgConfig.Instance;
            cfg.Enabled.Clear();

            foreach (string key in combo.Keys)
                cfg.Enabled.Add(key);

            AwgConfig.ApplyAutoCustomSize(cfg);
            AwgConfig.Save();

            if (_hintText != null)
                _hintText.SetText("已应用：" + TitleOf(combo));

            Log.Write("AWG: 应用特殊种子组合 " + string.Join(" + ", combo.Keys));
        }

        private void Toggle(SeedCombo combo)
        {
            if (!IsOn(combo))
            {
                Apply(combo);
                return;
            }

            // 再次点击已选中的组合：取消，恢复常规世界
            AwgConfig cfg = AwgConfig.Instance;
            cfg.Enabled.Clear();
            cfg.Enabled.Add("Normal");
            AwgConfig.ApplyAutoCustomSize(cfg);
            AwgConfig.Save();

            if (_hintText != null)
                _hintText.SetText("已取消：" + TitleOf(combo));

            Log.Write("AWG: 取消特殊种子组合 " + string.Join(" + ", combo.Keys));
        }

        private void ClearAll()
        {
            AwgConfig cfg = AwgConfig.Instance;
            cfg.Enabled.Clear();
            cfg.Enabled.Add("Normal");
            AwgConfig.ApplyAutoCustomSize(cfg);
            AwgConfig.Save();

            if (_hintText != null)
                _hintText.SetText("已清空组合，恢复为常规世界。");
        }

        private void ShowHint(SeedCombo combo)
        {
            if (_hintText != null)
                _hintText.SetText(combo.Hint);
        }

        private void ResetHint()
        {
            if (_hintText != null)
                _hintText.SetText(DefaultHint);
        }
    }
}
