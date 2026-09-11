namespace ProcessGuard;

public class MainForm : Form
{
    private readonly AppState _state;
    private readonly DataGridView _grid = new();
    private readonly ToggleSwitch _master = new();
    private readonly TextBox _filter = new();
    private readonly NotifyIcon _tray = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Dictionary<string, DateTime> _lastCheck = new();
    private readonly Dictionary<string, DateTime> _lastTrigger = new();
    private int _sortColumn = -1;
    private bool _sortAscending = true;
    private bool _reallyQuit;

    public MainForm()
    {
        _state = Storage.Load();
        Text = "现场守护";
        Font = UiTheme.Ui;
        Icon = AppIcon.Create();
        MinimumSize = new Size(1280, 720);
        Size = new Size(1360, 780);
        StartPosition = FormStartPosition.CenterScreen;

        var masterLabel = new Label
        {
            Text = "应用总开关",
            Font = UiTheme.UiBold,
            Left = 20,
            Top = 17,
            Width = 125,
            Height = 30
        };
        _master.SetBounds(145, 14, 54, 30);
        _master.Checked = _state.MasterEnabled;
        _master.CheckedChanged += (_, _) =>
        {
            _state.MasterEnabled = _master.Checked;
            Storage.Save(_state);
        };

        var filterLabel = new Label { Text = "筛选：", Left = 225, Top = 17, Width = 70, Height = 30, Font = UiTheme.UiBold, AutoSize = false };
        _filter.SetBounds(300, 12, 490, 36);
        _filter.Font = UiTheme.Ui;
        _filter.PlaceholderText = "按配置名称、逻辑或最近结果筛选";
        _filter.TextChanged += (_, _) => RefreshGrid();

        var add = MakeBtn("添加配置", 820, 12, 120, 36, AddConfig);
        var edit = MakeBtn("编辑选中", 950, 12, 120, 36, EditConfig);
        var del = MakeBtn("删除选中", 1080, 12, 120, 36, DeleteConfig);
        var trayBtn = MakeBtn("最小化到托盘", 1190, 12, 150, 36, HideToTray);

        _grid.SetBounds(20, 60, 1320, 580);
        _grid.Font = UiTheme.Grid;
        _grid.ColumnHeadersDefaultCellStyle.Font = UiTheme.UiBold;
        _grid.ColumnHeadersHeight = 40;
        _grid.RowTemplate.Height = 36;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 225, 242, 255);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 55);
        _grid.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 225, 242, 255);
        _grid.RowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 55);
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 225, 242, 255);
        _grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 55);
        _grid.BackgroundColor = Color.White;
        _grid.CellPainting += PaintHomeCell;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 244, 248);
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 55);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        _grid.Columns.Add("id", "id");
        _grid.Columns["id"]!.Visible = false;
        _grid.Columns.Add("enabled", "启用");
        _grid.Columns.Add("name", "配置名称");
        _grid.Columns.Add("logic", "逻辑");
        _grid.Columns.Add("interval", "间隔(分)");
        _grid.Columns.Add("cooldown", "冷却(分)");
        _grid.Columns.Add("count", "已触发次数");
        _grid.Columns.Add("last", "最近触发");
        _grid.Columns.Add("msg", "最近结果");
        _grid.Columns["enabled"]!.FillWeight = 55;
        _grid.Columns["name"]!.FillWeight = 150;
        _grid.Columns["logic"]!.FillWeight = 55;
        _grid.Columns["interval"]!.FillWeight = 80;
        _grid.Columns["cooldown"]!.FillWeight = 80;
        _grid.Columns["count"]!.FillWeight = 95;
        _grid.Columns["last"]!.FillWeight = 90;
        _grid.Columns["msg"]!.FillWeight = 380;
        foreach (DataGridViewColumn column in _grid.Columns)
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
        _grid.ColumnHeaderMouseClick += (_, e) =>
        {
            if (e.ColumnIndex == 0) return;
            if (_sortColumn == e.ColumnIndex) _sortAscending = !_sortAscending;
            else { _sortColumn = e.ColumnIndex; _sortAscending = true; }
            RefreshGrid();
        };

        var toggle = MakeBtn("切换选中配置开关", 20, 660, 200, 40, ToggleSelected);
        var export = MakeBtn("导出配置", 240, 660, 140, 40, ExportConfigs);
        var import = MakeBtn("导入配置", 390, 660, 140, 40, ImportConfigs);
        var tip = new Label
        {
            Text = "关闭窗口会藏到托盘继续检测。配置保存在：用户目录\\.process_guard\\configs.json",
            Font = UiTheme.Ui,
            Left = 550,
            Top = 668,
            Width = 1050,
            Height = 32
        };

        Controls.AddRange(new Control[] { masterLabel, _master, filterLabel, _filter, add, edit, del, trayBtn, _grid, toggle, export, import, tip });

        var menu = new ContextMenuStrip { Font = UiTheme.Ui };
        menu.Items.Add("显示窗口", null, (_, _) => ShowFromTray());
        menu.Items.Add("退出", null, (_, _) => Quit());
        _tray.Text = "现场守护";
        _tray.Visible = true;
        _tray.Icon = Icon;
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => ShowFromTray();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized) HideToTray();
            LayoutControls();
        };
        FormClosing += (_, e) =>
        {
            if (_reallyQuit) return;
            e.Cancel = true;
            HideToTray();
        };

        _timer.Interval = 1000;
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
        LayoutControls();
        RefreshGrid();
    }

    private void LayoutControls()
    {
        var pad = 20;
        var topH = 56;
        var bottomH = 64;
        _grid.Left = pad;
        _grid.Top = topH;
        _grid.Width = Math.Max(400, ClientSize.Width - pad * 2);
        _grid.Height = Math.Max(200, ClientSize.Height - topH - bottomH);
        var btnY = ClientSize.Height - 52;
        foreach (Control c in Controls)
        {
            if (c is Button b && b.Text == "切换选中配置开关")
                b.Top = btnY;
            if (c is Button b2 && (b2.Text == "导出配置" || b2.Text == "导入配置"))
                b2.Top = btnY;
            if (c is Label lb && lb.Text.Contains("托盘"))
                lb.Top = btnY + 8;
        }
    }

    private static Button MakeBtn(string text, int x, int y, int w, int h, Action click)
    {
        var b = new Button
        {
            Text = text,
            Left = x,
            Top = y,
            Width = w,
            Height = h,
            Font = UiTheme.Ui,
            UseVisualStyleBackColor = true
        };
        b.Click += (_, _) => click();
        return b;
    }

    private void RefreshGrid()
    {
        string? sel = null;
        if (_grid.SelectedRows.Count > 0)
            sel = Convert.ToString(_grid.SelectedRows[0].Cells["id"].Value);
        _grid.Rows.Clear();
        IEnumerable<WatchConfig> configs = _state.Configs;
        configs = _sortColumn switch
        {
            1 => _sortAscending ? configs.OrderBy(x => x.Enabled) : configs.OrderByDescending(x => x.Enabled),
            2 => _sortAscending ? configs.OrderBy(x => x.Name) : configs.OrderByDescending(x => x.Name),
            3 => _sortAscending ? configs.OrderBy(x => x.Logic) : configs.OrderByDescending(x => x.Logic),
            4 => _sortAscending ? configs.OrderBy(x => x.IntervalMin) : configs.OrderByDescending(x => x.IntervalMin),
            5 => _sortAscending ? configs.OrderBy(x => x.CooldownMin) : configs.OrderByDescending(x => x.CooldownMin),
            6 => _sortAscending ? configs.OrderBy(x => x.TriggerCount) : configs.OrderByDescending(x => x.TriggerCount),
            7 => _sortAscending ? configs.OrderBy(x => x.LastTrigger) : configs.OrderByDescending(x => x.LastTrigger),
            8 => _sortAscending ? configs.OrderBy(x => x.LastMessage) : configs.OrderByDescending(x => x.LastMessage),
            _ => configs
        };
        var query = _filter.Text.Trim();
        if (query.Length > 0)
            configs = configs.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || c.Logic.Contains(query, StringComparison.OrdinalIgnoreCase)
                || c.LastMessage.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (c.Enabled ? "开" : "关").Contains(query, StringComparison.OrdinalIgnoreCase));
        foreach (var c in configs)
        {
            _grid.Rows.Add(c.Id, c.Enabled ? "开" : "关", c.Name, c.Logic, c.IntervalMin,
                c.CooldownMin, c.TriggerCount, c.LastTrigger, c.LastMessage);
        }
        if (sel != null)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (Convert.ToString(row.Cells["id"].Value) == sel)
                {
                    row.Selected = true;
                    _grid.CurrentCell = row.Cells["name"];
                    break;
                }
            }
        }
    }

    private WatchConfig? Selected()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        var id = Convert.ToString(_grid.SelectedRows[0].Cells["id"].Value);
        return _state.Configs.FirstOrDefault(c => c.Id == id);
    }

    private static void PaintHomeCell(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        var selected = grid.Rows[e.RowIndex].Selected;
        var background = selected ? Color.FromArgb(225, 235, 250) :
            (e.RowIndex % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252));
        using var brush = new SolidBrush(background);
        e.Graphics.FillRectangle(brush, e.CellBounds);
        if (e.ColumnIndex == 1)
        {
            var value = Convert.ToString(e.FormattedValue) ?? "";
            TextRenderer.DrawText(e.Graphics, value, e.CellStyle.Font ?? grid.Font,
                e.CellBounds, Color.FromArgb(30, 40, 55),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            e.Handled = true;
            return;
        }
        e.PaintContent(e.CellBounds);
        e.Handled = true;
    }

    private void AddConfig()
    {
        using var dlg = new ConfigDialog(null, _state.Configs);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            _state.Configs.Add(dlg.Result);
            Storage.Save(_state);
            RefreshGrid();
        }
    }

    private void EditConfig()
    {
        var cfg = Selected();
        if (cfg == null)
        {
            MessageBox.Show(this, "请先选中一条配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new ConfigDialog(cfg, _state.Configs);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
        {
            var i = _state.Configs.FindIndex(c => c.Id == cfg.Id);
            if (i >= 0) _state.Configs[i] = dlg.Result;
            Storage.Save(_state);
            RefreshGrid();
        }
    }

    private void DeleteConfig()
    {
        var cfg = Selected();
        if (cfg == null) return;
        if (MessageBox.Show(this, $"删除配置「{cfg.Name}」？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _state.Configs.RemoveAll(c => c.Id == cfg.Id);
        Storage.Save(_state);
        RefreshGrid();
    }

    private void ToggleSelected()
    {
        var cfg = Selected();
        if (cfg == null)
        {
            MessageBox.Show(this, "请先选中一条配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        cfg.Enabled = !cfg.Enabled;
        Storage.Save(_state);
        RefreshGrid();
    }

    private void ExportConfigs()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "导出配置文件",
            Filter = "JSON 配置文件 (*.json)|*.json|全部文件 (*.*)|*.*",
            FileName = "process_guard_configs.json",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            Storage.Export(_state, dialog.FileName);
            MessageBox.Show(this, $"已导出 {_state.Configs.Count} 条配置。", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportConfigs()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "导入配置文件",
            Filter = "JSON 配置文件 (*.json)|*.json|全部文件 (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var imported = Storage.Import(dialog.FileName);
            if (imported?.Configs == null || imported.Configs.Count == 0)
            {
                MessageBox.Show(this, "文件中没有可导入的配置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var config in imported.Configs)
            {
                config.Id = Guid.NewGuid().ToString("N")[..10];
                _state.Configs.Add(config);
            }
            Storage.Save(_state);
            RefreshGrid();
            MessageBox.Show(this, $"已追加导入 {imported.Configs.Count} 条配置。", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "导入失败，请选择有效的 JSON 配置文件。\n\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HideToTray()
    {
        Hide();
        _tray.Visible = true;
        _tray.ShowBalloonTip(1200, "现场守护", "已在托盘继续检测", ToolTipIcon.Info);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void Quit()
    {
        _reallyQuit = true;
        _timer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        Close();
    }

    private void Tick()
    {
        if (!_state.MasterEnabled) return;
        var now = DateTime.Now;
        var changed = false;
        foreach (var cfg in _state.Configs)
        {
            if (!cfg.Enabled) continue;
            if (_lastTrigger.TryGetValue(cfg.Id, out var lt))
            {
                var remain = cfg.CooldownMin * 60 - (now - lt).TotalSeconds;
                if (cfg.CooldownMin > 0 && remain > 0)
                {
                    if ((int)remain % 15 == 0)
                    {
                        cfg.LastMessage = $"冷却中，剩余约 {(int)remain} 秒";
                        changed = true;
                    }
                    continue;
                }
            }
            var interval = cfg.IntervalMin <= 0 ? 0.1 : cfg.IntervalMin;
            if (_lastCheck.TryGetValue(cfg.Id, out var lc) && (now - lc).TotalMinutes < interval)
                continue;
            _lastCheck[cfg.Id] = now;
            try
            {
                if (Engine.EvalConditions(cfg, _state, _lastTrigger))
                {
                    _lastTrigger[cfg.Id] = DateTime.Now;
                    cfg.TriggerCount++;
                    cfg.LastTrigger = DateTime.Now.ToString("HH:mm:ss");
                    var msgs = cfg.Actions.Select(x => RunActionWithLinks(x, new HashSet<string> { cfg.Id })).ToList();
                    cfg.LastMessage = $"触发{cfg.TriggerCount}次 | " + string.Join("；", msgs);
                    changed = true;
                }
                else
                {
                    cfg.LastMessage = "条件未满足，未执行";
                    changed = true;
                }
            }
            catch (Exception ex)
            {
                cfg.LastMessage = "检测异常: " + ex.Message;
                changed = true;
            }
        }
        if (!changed) return;
        Storage.Save(_state);
        RefreshGrid();
    }

    private string RunActionWithLinks(WatchAction action, HashSet<string> chain)
    {
        if (action.Type != "link_config") return Engine.RunAction(action);
        var target = _state.Configs.FirstOrDefault(x => x.Id == action.LinkedConfigId);
        if (target == null) return "连携目标不存在";
        if (!target.Enabled) return $"连携目标「{target.Name}」已关闭";
        if (!chain.Add(target.Id)) return $"检测到连携循环: {target.Name}";
        if (!Engine.EvalConditions(target, _state, _lastTrigger)) return $"连携目标「{target.Name}」条件未满足";
        _lastTrigger[target.Id] = DateTime.Now;
        target.TriggerCount++;
        target.LastTrigger = DateTime.Now.ToString("HH:mm:ss");
        var messages = target.Actions.Select(x => RunActionWithLinks(x, chain)).ToList();
        target.LastMessage = $"连携触发{target.TriggerCount}次 | " + string.Join("；", messages);
        return $"已连携触发「{target.Name}」";
    }
}
