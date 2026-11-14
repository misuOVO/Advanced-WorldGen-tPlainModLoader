using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;
using Terraria.WorldBuilding;
using tContentPatch.Content.UI;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 原版世界生成配置编辑器（Worldgen Config）。
    /// 修改的值会在新建世界时写进 WorldGenConfiguration。
    /// </summary>
    internal class WorldgenConfigState : UIState
    {
        private readonly UIWorldCreation _previous;

        private WorldgenConfig.Entry _selected;
        private ConfigRow _selectedRow;
        private tContentPatch.Content.UI.UITextBox _valueBox;
        private UIText _pathText;
        private UIText _statusText;

        public WorldgenConfigState(UIWorldCreation previous)
        {
            _previous = previous;
            Build();
        }

        private void Build()
        {
            const int panelW = 780;
            const int panelH = 680;

            UIPanel panel = new UIPanel();
            panel.Width.Set(panelW, 0f);
            panel.Height.Set(panelH, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.SetPadding(0f);
            Append(panel);

            UIText title = new UIText("原版世界生成配置", 1.25f, true);
            title.HAlign = 0.5f;
            title.Top.Set(12f, 0f);
            panel.Append(title);

            WorldGenConfiguration config = WorldgenConfig.LoadWithOverrides();
            List<WorldgenConfig.Entry> entries = WorldgenConfig.Collect(config);

            int modified = 0;
            foreach (WorldgenConfig.Entry entry in entries)
            {
                if (entry.Overridden)
                    modified++;
            }

            UIText subtitle = new UIText(
                "共 " + entries.Count + " 项，已修改 " + modified + " 项（只影响新建世界）", 0.78f);
            subtitle.Left.Set(22f, 0f);
            subtitle.Top.Set(44f, 0f);
            subtitle.TextColor = new Color(190, 205, 235);
            panel.Append(subtitle);

            UIScrollViewer2 scroll = new UIScrollViewer2();
            scroll.Left.Set(20f, 0f);
            scroll.Top.Set(74f, 0f);
            scroll.Width.Set(-40f, 1f);
            scroll.Height.Set(474f, 0f);
            scroll.ItemMargin = 2;
            panel.Append(scroll);

            string lastGroup = null;

            foreach (WorldgenConfig.Entry entry in entries)
            {
                if (entry.Group != lastGroup)
                {
                    lastGroup = entry.Group;
                    scroll.AddChild(new GroupHeader(
                        entry.Group == "Passes" ? "生成步骤参数（Passes）" : "群系参数（Biomes）"));
                }

                ConfigRow row = new ConfigRow(entry, Select);
                row.Width.Set(-8f, 1f);
                row.Height.Set(30f, 0f);
                row.Left.Set(2f, 0f);
                scroll.AddChild(row);
            }

            Log.Write("AWG: 原版世界生成配置项 " + entries.Count + " 个");

            Box editor = new Box(new Color(14, 18, 32, 190), new Color(90, 110, 170, 130));
            editor.Left.Set(20f, 0f);
            editor.Top.Set(560f, 0f);
            editor.Width.Set(-40f, 1f);
            editor.Height.Set(68f, 0f);
            panel.Append(editor);

            _pathText = new UIText("点击上方任意一项进行修改。", 0.8f);
            _pathText.Left.Set(12f, 0f);
            _pathText.Top.Set(6f, 0f);
            _pathText.Width.Set(-24f, 1f);
            _pathText.TextColor = new Color(200, 214, 240);
            editor.Append(_pathText);

            _valueBox = new tContentPatch.Content.UI.UITextBox("");
            _valueBox.Text_MaxLength = 40;
            _valueBox.SetTextScale(0.9f);
            _valueBox.BackgroundColor = new Color(10, 12, 22) * 0.95f;
            _valueBox.BorderColor = new Color(120, 140, 200);
            _valueBox.Left.Set(12f, 0f);
            _valueBox.Top.Set(32f, 0f);
            _valueBox.Width.Set(300f, 0f);
            _valueBox.Height.Set(30f, 0f);
            editor.Append(_valueBox);

            TextButton apply = MakeButton("应用", 324f, 32f, 110f, 30f);
            apply.OnLeftClick += (evt, el) => ApplySelected();
            editor.Append(apply);

            TextButton reset = MakeButton("恢复默认", 442f, 32f, 110f, 30f);
            reset.OnLeftClick += (evt, el) => ResetSelected();
            editor.Append(reset);

            TextButton back = MakeButton("返回", panelW - 164f, 32f, 110f, 30f);
            back.OnLeftClick += (evt, el) => GoBack();
            editor.Append(back);

            _statusText = new UIText("", 0.75f);
            _statusText.Left.Set(22f, 0f);
            _statusText.Top.Set(636f, 0f);
            _statusText.Width.Set(-44f, 1f);
            _statusText.TextColor = new Color(255, 226, 150);
            panel.Append(_statusText);
        }

        private static TextButton MakeButton(string text, float left, float top, float width, float height)
        {
            TextButton button = new TextButton(text);
            button.Left.Set(left, 0f);
            button.Top.Set(top, 0f);
            button.Width.Set(width, 0f);
            button.Height.Set(height, 0f);
            return button;
        }

        private void Select(WorldgenConfig.Entry entry, ConfigRow row)
        {
            _selected = entry;

            if (_selectedRow != null)
                _selectedRow.Selected = false;

            _selectedRow = row;
            if (_selectedRow != null)
                _selectedRow.Selected = true;

            try
            {
                _valueBox?.SetText(WorldgenConfig.Format(entry.Value));
            }
            catch
            {
            }

            string path = entry.Path;

            if (path.EndsWith(".ScaleWith", StringComparison.Ordinal))
                path += "    可选：None / WorldWidth / WorldArea";

            _pathText?.SetText(path);

            if (_statusText != null)
                _statusText.SetText("");
        }

        private void ApplySelected()
        {
            if (_selected == null)
            {
                _statusText?.SetText("请先点击一项配置。");
                return;
            }

            string text = _valueBox != null ? _valueBox.Text ?? "" : "";

            if (!WorldgenConfig.SetValue(_selected, text, out string error))
            {
                _statusText?.SetText("应用失败：" + error);
                return;
            }

            AwgConfig cfg = AwgConfig.Instance;
            cfg.WorldGenOverrides = cfg.WorldGenOverrides ?? new Dictionary<string, string>();
            cfg.WorldGenOverrides[_selected.Path] = text;
            AwgConfig.Save();

            _selected.Overridden = true;

            // 如果本次运行已经建过配置对象，也同步进去（下一次生成一定会重新读）
            try
            {
                if (GenVars.configuration != null)
                    WorldgenConfig.ApplyOverrides(GenVars.configuration);
            }
            catch
            {
            }

            _statusText?.SetText("已应用：" + _selected.Label + " = " + text);
            Log.Write("AWG: 配置覆盖 " + _selected.Path + " = " + text);
        }

        private void ResetSelected()
        {
            if (_selected == null)
            {
                _statusText?.SetText("请先点击一项配置。");
                return;
            }

            AwgConfig cfg = AwgConfig.Instance;
            if (cfg.WorldGenOverrides == null || !cfg.WorldGenOverrides.Remove(_selected.Path))
            {
                _statusText?.SetText("这一项本来就是默认值。");
                return;
            }

            AwgConfig.Save();
            Log.Write("AWG: 取消配置覆盖 " + _selected.Path);

            // 重新建一次面板，把默认值显示回来
            try
            {
                Main.MenuUI.SetState(new WorldgenConfigState(_previous));
            }
            catch (Exception e)
            {
                Log.Write("AWG: 刷新配置面板失败: " + e.Message);
            }
        }

        private void GoBack()
        {
            try
            {
                Main.MenuUI.SetState(new SeedOptionsState(_previous));
            }
            catch (Exception e)
            {
                Log.Write("AWG: 返回种子选项失败: " + e.Message);
            }
        }
    }

    internal class ConfigRow : UIElement
    {
        private readonly WorldgenConfig.Entry _entry;
        private readonly Action<WorldgenConfig.Entry, ConfigRow> _onClick;

        public bool Selected;

        public ConfigRow(WorldgenConfig.Entry entry, Action<WorldgenConfig.Entry, ConfigRow> onClick)
        {
            _entry = entry;
            _onClick = onClick;

            OnLeftClick += (evt, el) => _onClick?.Invoke(_entry, this);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);

            Color bg;
            if (Selected)
                bg = new Color(78, 100, 170, 220);
            else if (IsMouseHovering)
                bg = new Color(60, 78, 130, 200);
            else
                bg = new Color(24, 30, 50, 190);

            UiPaint.Fill(spriteBatch, rect, bg);
            UiPaint.Outline(spriteBatch, rect,
                Selected ? new Color(170, 200, 255, 220) : new Color(90, 110, 160, 120));

            // 已修改的项前面点一个黄点
            if (_entry.Overridden)
            {
                UiPaint.Fill(spriteBatch,
                    new Rectangle(rect.X + 6, rect.Y + rect.Height / 2 - 3, 6, 6),
                    new Color(255, 214, 96));
            }

            UiPaint.Text(spriteBatch, _entry.Label,
                new Vector2(rect.X + 18f, rect.Y + 5f), new Color(214, 226, 248), 0.8f);

            string value = WorldgenConfig.Format(_entry.Value);
            float scale = 0.82f;
            Vector2 size = FontAssets.MouseText.Value.MeasureString(value) * scale;

            UiPaint.Text(spriteBatch, value,
                new Vector2(rect.Right - size.X - 12f, rect.Y + 5f),
                _entry.Overridden ? new Color(255, 226, 150) : new Color(190, 205, 235),
                scale);
        }
    }
}
