using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    /// 每行右侧直接就是一个输入框：改完按回车 / 点到别处 / 停手一会儿就自动生效，
    /// 不用先选中再到底部应用。
    /// </summary>
    internal class WorldgenConfigState : UIState
    {
        private const int PanelW = 800;
        private const int PanelH = 642;

        private readonly UIWorldCreation _previous;
        private readonly List<WorldgenConfig.Entry> _entries;
        private readonly List<ConfigRow> _rows = new List<ConfigRow>();

        private UIScrollViewer2 _scroll;
        private UIText _subtitle;
        private UIText _status;
        private UITextBox _searchBox;
        private FilterButton _onlyModifiedButton;

        private string _search = "";
        private bool _onlyModified;
        private bool _pendingRebuild;

        public WorldgenConfigState(UIWorldCreation previous)
        {
            _previous = previous;

            WorldGenConfiguration config = WorldgenConfig.LoadWithOverrides();
            _entries = WorldgenConfig.Collect(config);

            Build();
        }

        private void Build()
        {
            UIPanel panel = new UIPanel();
            panel.Width.Set(PanelW, 0f);
            panel.Height.Set(PanelH, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.SetPadding(0f);
            Append(panel);

            UIText title = new UIText("原版世界生成配置", 1f, false);
            title.HAlign = 0.5f;
            title.Top.Set(8f, 0f);
            panel.Append(title);

            _subtitle = new UIText("", 0.8f);
            _subtitle.HAlign = 0.5f;
            _subtitle.Top.Set(40f, 0f);
            _subtitle.TextColor = new Color(190, 205, 235);
            panel.Append(_subtitle);

            // ---- 工具栏：搜索 + 只看已修改 ----
            Box toolbar = new Box(new Color(14, 18, 32, 175), new Color(90, 110, 170, 130));
            toolbar.Left.Set(20f, 0f);
            toolbar.Top.Set(66f, 0f);
            toolbar.Width.Set(-40f, 1f);
            toolbar.Height.Set(40f, 0f);
            panel.Append(toolbar);

            _searchBox = new UITextBox("搜索名称或路径…");
            TextBoxCompat.LeftAlign(_searchBox);
            _searchBox.MaxInputLength = 40;
            _searchBox.Left.Set(10f, 0f);
            _searchBox.Top.Set(4f, 0f);
            _searchBox.Width.Set(330f, 0f);
            _searchBox.Height.Set(32f, 0f);
            _searchBox.OnTextChanged = text =>
            {
                _search = text ?? "";
                _pendingRebuild = true;
            };
            toolbar.Append(_searchBox);

            _onlyModifiedButton = new FilterButton("只看已修改", () => _onlyModified,
                () =>
                {
                    CommitPending();
                    _onlyModified = !_onlyModified;
                    _pendingRebuild = true;
                });
            _onlyModifiedButton.Left.Set(350f, 0f);
            _onlyModifiedButton.Top.Set(5f, 0f);
            _onlyModifiedButton.Width.Set(136f, 0f);
            _onlyModifiedButton.Height.Set(30f, 0f);
            toolbar.Append(_onlyModifiedButton);

            UIText tip = new UIText("点数值直接改 · 右键恢复默认", 0.75f);
            tip.HAlign = 1f;
            tip.Left.Set(-12f, 0f);
            tip.Top.Set(11f, 0f);
            tip.TextColor = new Color(180, 195, 225);
            toolbar.Append(tip);

            // ---- 列表 ----
            _scroll = new UIScrollViewer2(false);
            _scroll.Left.Set(20f, 0f);
            _scroll.Top.Set(114f, 0f);
            _scroll.Width.Set(-40f, 1f);
            _scroll.Height.Set(444f, 0f);
            _scroll.ItemMargin = 2;
            panel.Append(_scroll);

            _status = new UIText("", 0.75f);
            _status.Left.Set(22f, 0f);
            _status.Top.Set(562f, 0f);
            _status.Width.Set(-44f, 1f);
            _status.TextColor = new Color(255, 226, 150);
            panel.Append(_status);

            TextButton resetAll = new TextButton("全部恢复默认");
            resetAll.Left.Set(PanelW - 306f, 0f);
            resetAll.Top.Set(590f, 0f);
            resetAll.Width.Set(140f, 0f);
            resetAll.Height.Set(34f, 0f);
            resetAll.OnLeftClick += (evt, el) => ResetAll();
            panel.Append(resetAll);

            TextButton back = new TextButton("返回");
            back.Left.Set(PanelW - 156f, 0f);
            back.Top.Set(590f, 0f);
            back.Width.Set(136f, 0f);
            back.Height.Set(34f, 0f);
            back.OnLeftClick += (evt, el) => GoBack();
            panel.Append(back);

            RebuildRows();
        }

        // ------------------------------------------------------------------
        // 列表
        // ------------------------------------------------------------------

        private bool Visible(WorldgenConfig.Entry entry)
        {
            if (_onlyModified && !entry.Overridden)
                return false;

            if (_search.Length == 0)
                return true;

            return entry.Label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                || entry.Path.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RebuildRows()
        {
            CommitPending();

            _rows.Clear();
            _scroll.ClearChild();

            string lastGroup = null;
            int shown = 0;

            foreach (WorldgenConfig.Entry entry in _entries)
            {
                if (!Visible(entry))
                    continue;

                if (entry.Group != lastGroup)
                {
                    lastGroup = entry.Group;
                    _scroll.AddChild(new GroupHeader(
                        entry.Group == "Passes" ? "生成步骤参数（Passes）" : "群系参数（Biomes）"));
                }

                ConfigRow row = new ConfigRow(entry, Apply, ApplyChoice, ResetOne);
                row.Width.Set(-8f, 1f);
                row.Height.Set(34f, 0f);
                row.Left.Set(2f, 0f);
                _scroll.AddChild(row);
                _rows.Add(row);
                shown++;
            }

            if (shown == 0)
            {
                UIText empty = new UIText("没有匹配的项", 0.85f);
                empty.Height.Set(30f, 0f);
                empty.Left.Set(12f, 0f);
                empty.TextColor = new Color(170, 185, 215);
                _scroll.AddChild(empty);
            }

            UpdateHeader(shown);
        }

        private void UpdateHeader(int shown = -1)
        {
            int modified = 0;
            foreach (WorldgenConfig.Entry entry in _entries)
            {
                if (entry.Overridden)
                    modified++;
            }

            if (shown < 0)
                shown = _rows.Count;

            if (_subtitle != null)
            {
                _subtitle.SetText("共 " + _entries.Count + " 项 · 显示 " + shown
                    + " 项 · 已修改 " + modified + " 项 · 改动只影响新建世界");
            }
        }

        // ------------------------------------------------------------------
        // 写入
        // ------------------------------------------------------------------

        private bool Apply(ConfigRow row, WorldgenConfig.Entry entry, string text)
        {
            if (!ApplyValue(entry, text))
                return false;

            SetStatus("已保存：" + entry.Label + " = " + text);
            return true;
        }

        private bool ApplyChoice(WorldgenConfig.Entry entry, string text)
        {
            if (!ApplyValue(entry, text))
                return false;

            SetStatus("已保存：" + entry.Label + " = " + WorldgenConfig.DescribeChoice(text));
            return true;
        }

        private bool ApplyValue(WorldgenConfig.Entry entry, string text)
        {
            if (!WorldgenConfig.SetValue(entry, text, out string error))
            {
                SetStatus("无效值：" + error);
                return false;
            }

            AwgConfig cfg = AwgConfig.Instance;
            cfg.WorldGenOverrides = cfg.WorldGenOverrides ?? new Dictionary<string, string>();
            cfg.WorldGenOverrides[entry.Path] = text;
            AwgConfig.Save();

            entry.Overridden = true;

            try
            {
                if (GenVars.configuration != null)
                    WorldgenConfig.ApplyOverrides(GenVars.configuration);
            }
            catch
            {
            }

            UpdateHeader();
            return true;
        }

        private void ResetOne(WorldgenConfig.Entry entry)
        {
            CommitPending();

            AwgConfig cfg = AwgConfig.Instance;
            bool removed = cfg.WorldGenOverrides != null && cfg.WorldGenOverrides.Remove(entry.Path);

            if (!removed)
            {
                SetStatus("这一项已经是默认值：" + entry.Label);
                return;
            }

            AwgConfig.Save();

            try
            {
                Newtonsoft.Json.Linq.JValue token = WorldgenConfig.DefaultValue(entry.Path);
                if (token != null && entry.Value != null)
                    entry.Value.Value = token.Value;
            }
            catch (Exception e)
            {
                Log.Write("AWG: 读取默认值失败 " + entry.Path + ": " + e.Message);
            }

            entry.Overridden = false;

            SetStatus("已恢复默认：" + entry.Label);
            UpdateHeader();

            // 刷新对应输入框
            foreach (ConfigRow row in _rows)
            {
                if (ReferenceEquals(row.Entry, entry))
                {
                    row.SyncFromEntry();
                    break;
                }
            }

            if (_onlyModified)
                _pendingRebuild = true;
        }

        private void ResetAll()
        {
            CommitPending();

            AwgConfig cfg = AwgConfig.Instance;
            if (cfg.WorldGenOverrides == null || cfg.WorldGenOverrides.Count == 0)
            {
                SetStatus("当前没有任何修改。");
                return;
            }

            cfg.WorldGenOverrides.Clear();
            AwgConfig.Save();

            Log.Write("AWG: 已清除全部原版配置覆盖");

            try
            {
                Main.MenuUI.SetState(new WorldgenConfigState(_previous));
            }
            catch (Exception e)
            {
                Log.Write("AWG: 刷新配置面板失败: " + e.Message);
            }
        }

        private void CommitPending()
        {
            foreach (ConfigRow row in _rows)
            {
                try
                {
                    row.Commit();
                }
                catch
                {
                }
            }
        }

        private void SetStatus(string text)
        {
            _status?.SetText(text);
        }

        private void GoBack()
        {
            CommitPending();

            try
            {
                Main.MenuUI.SetState(new SeedOptionsState(_previous));
            }
            catch (Exception e)
            {
                Log.Write("AWG: 返回种子选项失败: " + e.Message);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_pendingRebuild)
            {
                _pendingRebuild = false;
                RebuildRows();
                return;
            }

            // ESC 之类直接离开时也别丢改动
            if (!Main.menuMultiplayer && Main.keyState.IsKeyDown(Keys.Escape))
                CommitPending();
        }
    }

    // ------------------------------------------------------------------
    // 行
    // ------------------------------------------------------------------

    internal class ConfigRow : UIElement
    {
        private const int CommitDelayFrames = 36;

        private readonly WorldgenConfig.Entry _entry;
        private readonly Func<ConfigRow, WorldgenConfig.Entry, string, bool> _apply;
        private readonly Func<WorldgenConfig.Entry, string, bool> _applyChoice;
        private readonly Action<WorldgenConfig.Entry> _reset;
        private readonly UITextBox _box;
        private readonly ChoiceButton _choice;

        private string _applied;
        private string _lastSeen;
        private string _failedText;
        private int _idleFrames;
        private bool _enterHeld;
        private bool _committing;

        public WorldgenConfig.Entry Entry
        {
            get { return _entry; }
        }

        public ConfigRow(WorldgenConfig.Entry entry,
            Func<ConfigRow, WorldgenConfig.Entry, string, bool> apply,
            Func<WorldgenConfig.Entry, string, bool> applyChoice,
            Action<WorldgenConfig.Entry> reset)
        {
            _entry = entry;
            _apply = apply;
            _applyChoice = applyChoice;
            _reset = reset;

            _applied = WorldgenConfig.Format(entry.Value);
            _lastSeen = _applied;

            // 英文枚举（ScaleWith）改成点击切换的选项，不用手打
            if (WorldgenConfig.IsChoiceEntry(entry))
            {
                _choice = new ChoiceButton(entry, _applyChoice);
                Append(_choice);
            }
            else
            {
                _box = new UITextBox("");
                TextBoxCompat.LeftAlign(_box);
                _box.MaxInputLength = 40;
                _box.HAlign = 1f;
                _box.Left.Set(-12f, 0f);
                _box.Top.Set(1f, 0f);
                _box.Width.Set(192f, 0f);
                _box.Height.Set(32f, 0f);
                _box.Text = _applied;
                _box.OnLostFocus = Commit;
                Append(_box);
            }

            OnRightClick += (evt, el) => _reset?.Invoke(_entry);
        }

        public void SyncFromEntry()
        {
            _applied = WorldgenConfig.Format(_entry.Value);
            _lastSeen = _applied;

            if (_choice != null)
            {
                _choice.SyncFromEntry();
                return;
            }

            if (_box != null)
            {
                _box.Text = _applied;
                TextBoxCompat.Normal(_box);
            }
        }

        public void Commit()
        {
            if (_committing || _box == null)
                return;

            string text = _box.Text ?? "";

            if (text == _applied)
            {
                TextBoxCompat.Normal(_box);
                return;
            }

            _committing = true;

            bool ok;
            try
            {
                ok = _apply(this, _entry, text);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 写入配置失败: " + e.Message);
                ok = false;
            }
            finally
            {
                _committing = false;
            }

            if (ok)
            {
                _applied = text;
                _failedText = null;
                TextBoxCompat.Saved(_box);
            }
            else
            {
                _failedText = text;
                TextBoxCompat.Error(_box);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_box == null)
                return;

            string text = _box.Text ?? "";

            // 回车立即生效
            bool enter = Main.keyState.IsKeyDown(Keys.Enter);
            if (enter && !_enterHeld && _box.Focus)
            {
                Commit();
                _box.Focus = false;
            }
            _enterHeld = enter;

            // 停手一会儿也自动生效，避免忘点别处就离开
            if (text != _lastSeen)
            {
                _lastSeen = text;
                _idleFrames = 0;
            }
            else if (text != _applied && text != _failedText)
            {
                _idleFrames++;
                if (_idleFrames >= CommitDelayFrames)
                {
                    _idleFrames = 0;
                    Commit();
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);

            Color bg = IsMouseHovering
                ? new Color(86, 108, 176)
                : new Color(70, 88, 150);

            UiPaint.RowPanel(spriteBatch, rect, bg);

            // 已修改的项前面点一个黄点
            if (_entry.Overridden)
            {
                UiPaint.Fill(spriteBatch,
                    new Rectangle(rect.X + 6, rect.Y + rect.Height / 2 - 3, 6, 6),
                    new Color(255, 214, 96));
            }

            UiPaint.Text(spriteBatch, _entry.Label,
                new Vector2(rect.X + 18f, rect.Y + 5f),
                _entry.Overridden ? new Color(255, 232, 160) : new Color(214, 226, 248), 0.8f);
        }
    }

    // ------------------------------------------------------------------
    // 小控件
    // ------------------------------------------------------------------

    /// <summary>点击循环切换的选项按钮（用于 ScaleWith 这类英文枚举）。</summary>
    internal class ChoiceButton : UIElement
    {
        private static readonly string[] Values = { "None", "WorldWidth", "WorldArea" };
        private static readonly string[] Labels = { "不缩放", "按世界宽度", "按世界面积" };

        private readonly WorldgenConfig.Entry _entry;
        private readonly Func<WorldgenConfig.Entry, string, bool> _apply;
        private int _index;

        public ChoiceButton(WorldgenConfig.Entry entry, Func<WorldgenConfig.Entry, string, bool> apply)
        {
            _entry = entry;
            _apply = apply;

            HAlign = 1f;
            Left.Set(-12f, 0f);
            Top.Set(1f, 0f);
            Width.Set(192f, 0f);
            Height.Set(32f, 0f);

            SyncFromEntry();

            OnLeftClick += (evt, el) => Step(1);
        }

        public void SyncFromEntry()
        {
            string value = WorldgenConfig.Format(_entry.Value);
            _index = Array.IndexOf(Values, value);

            if (_index < 0)
                _index = 0;
        }

        private void Step(int dir)
        {
            int next = (_index + dir + Values.Length) % Values.Length;

            if (_apply(_entry, Values[next]))
                _index = next;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);

            Color bg = IsMouseHovering
                ? new Color(92, 116, 190)
                : new Color(73, 94, 171);

            UiPaint.RowPanel(spriteBatch, rect, bg);

            UiPaint.Text(spriteBatch, "<",
                new Vector2(rect.X + 10f, rect.Y + 6f), new Color(200, 215, 245), 0.85f);

            UiPaint.Text(spriteBatch, ">",
                new Vector2(rect.Right - 22f, rect.Y + 6f), new Color(200, 215, 245), 0.85f);

            string label = _index >= 0 && _index < Labels.Length
                ? Labels[_index]
                : WorldgenConfig.Format(_entry.Value);

            UiPaint.CenteredText(spriteBatch, label, rect, Color.White, 0.86f);
        }
    }

    internal class FilterButton : UIElement
    {
        private readonly string _text;
        private readonly Func<bool> _isOn;
        private readonly Action _toggle;

        public FilterButton(string text, Func<bool> isOn, Action toggle)
        {
            _text = text;
            _isOn = isOn;
            _toggle = toggle;
            OnLeftClick += (evt, el) => _toggle?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);
            bool on = _isOn != null && _isOn();

            Color bg = on
                ? new Color(70, 122, 86)
                : new Color(70, 88, 150);

            if (IsMouseHovering)
                bg = new Color(Math.Min(255, bg.R + 24), Math.Min(255, bg.G + 24),
                    Math.Min(255, bg.B + 30));

            UiPaint.RowPanel(spriteBatch, rect, bg);
            UiPaint.CenteredText(spriteBatch, _text, rect,
                on ? new Color(230, 255, 235) : new Color(225, 232, 250), 0.8f);
        }
    }
}
