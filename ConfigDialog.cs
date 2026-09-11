namespace ProcessGuard;

public class ConfigDialog : Form
{
    public WatchConfig? Result { get; private set; }

    private readonly WatchConfig _src;
    private readonly IReadOnlyList<WatchConfig> _allConfigs;
    private readonly TextBox _name = new();
    private readonly TextBox _interval = new();
    private readonly TextBox _cooldown = new();
    private readonly ComboBox _logic = new();
    private readonly ToggleSwitch _enabled = new();
    private readonly ListBox _conds = new();
    private readonly ListBox _acts = new();
    private readonly List<WatchCondition> _conditions = new();
    private readonly List<WatchAction> _actions = new();

    public ConfigDialog(WatchConfig? existing, IReadOnlyList<WatchConfig>? allConfigs = null)
    {
        _src = existing ?? new WatchConfig();
        _allConfigs = allConfigs ?? new[] { _src };
        _conditions.AddRange(_src.Conditions);
        _actions.AddRange(_src.Actions);

        Text = existing == null ? "添加配置" : "编辑配置";
        Font = UiTheme.Ui;
        Width = 920;
        Height = 1040;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var y = 20;
        Controls.Add(LabelAt("配置名称", 20, y, 100));
        _name.SetBounds(130, y - 2, 750, 32);
        _name.Font = UiTheme.Ui;
        _name.Text = _src.Name;
        Controls.Add(_name);
        y += 48;

        Controls.Add(LabelAt("配置开关", 20, y, 100));
        _enabled.SetBounds(130, y - 2, 54, 30);
        _enabled.Checked = _src.Enabled;
        Controls.Add(_enabled);
        Controls.Add(new Label { Text = "开启后才会参与检测", Left = 195, Top = y + 2, Width = 220, Height = 28, Font = UiTheme.Ui, ForeColor = Color.DimGray });
        y += 48;

        Controls.Add(LabelAt("检测间隔(分钟)", 20, y, 140));
        _interval.SetBounds(160, y - 2, 80, 32);
        _interval.Font = UiTheme.Ui;
        _interval.Text = _src.IntervalMin.ToString();
        Controls.Add(_interval);

        Controls.Add(LabelAt("触发后冷却(分钟)", 270, y, 150));
        _cooldown.SetBounds(430, y - 2, 80, 32);
        _cooldown.Font = UiTheme.Ui;
        _cooldown.Text = _src.CooldownMin.ToString();
        Controls.Add(_cooldown);

        Controls.Add(LabelAt("条件逻辑", 540, y, 90));
        _logic.SetBounds(630, y - 2, 100, 32);
        _logic.Font = UiTheme.Ui;
        _logic.DropDownStyle = ComboBoxStyle.DropDownList;
        _logic.Items.AddRange(new object[] { "OR", "AND" });
        _logic.SelectedItem = _src.Logic is "AND" or "OR" ? _src.Logic : "OR";
        Controls.Add(_logic);
        Controls.Add(LabelAt("OR任一 / AND全部", 740, y + 4, 150));
        y += 52;

        Controls.Add(LabelAt("条件 (IF)", 20, y, 200));
        y += 28;
        _conds.SetBounds(20, y, 860, 240);
        _conds.Font = UiTheme.Ui;
        _conds.ItemHeight = 28;
        ConfigureSelectionStyle(_conds);
        Controls.Add(_conds);
        y += 250;
        var bp = Btn("添加进程条件", 20, y, 170, 40, AddProcess);
        var bport = Btn("添加网络端口", 200, y, 170, 40, AddPort);
        var bserial = Btn("添加串口条件", 380, y, 170, 40, AddSerial);
        var bdc = Btn("删除选中条件", 560, y, 170, 40, () =>
        {
            if (_conds.SelectedIndex >= 0)
            {
                _conditions.RemoveAt(_conds.SelectedIndex);
                RefreshConds();
            }
        });
        var blink = Btn("添加连携条件", 740, y, 140, 40, AddLinkCondition);
        Controls.AddRange(new Control[] { bp, bport, bserial, bdc, blink });
        _conds.DoubleClick += (_, _) => EditSelectedCondition();
        y += 56;

        Controls.Add(LabelAt("动作 (THEN)", 20, y, 200));
        y += 28;
        _acts.SetBounds(20, y, 860, 200);
        _acts.Font = UiTheme.Ui;
        _acts.ItemHeight = 28;
        ConfigureSelectionStyle(_acts);
        Controls.Add(_acts);
        y += 212;
        Controls.Add(Btn("添加运行文件", 20, y, 180, 40, () => AddOpen()));
        Controls.Add(Btn("添加连携动作", 410, y, 180, 40, () => AddLinkAction()));
        Controls.Add(Btn("删除选中动作", 220, y, 180, 40, () =>
        {
            if (_acts.SelectedIndex >= 0)
            {
                _actions.RemoveAt(_acts.SelectedIndex);
                RefreshActs();
            }
        }));
        _acts.DoubleClick += (_, _) => EditSelectedAction();

        var save = Btn("保存配置", 620, 950, 130, 44, Save);
        var cancel = Btn("取消", 760, 950, 120, 44, () => { DialogResult = DialogResult.Cancel; Close(); });
        Controls.AddRange(new Control[] { save, cancel });

        RefreshConds();
        RefreshActs();
    }

    private static Label LabelAt(string text, int x, int y, int w) =>
        new() { Text = text, Left = x, Top = y, Width = w, Height = 28, Font = UiTheme.Ui };

    private static void ConfigureSelectionStyle(ListBox list)
    {
        list.DrawMode = DrawMode.OwnerDrawFixed;
        list.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            var selected = (e.State & DrawItemState.Selected) != 0;
            using var background = new SolidBrush(selected
                ? Color.FromArgb(48, 93, 169, 235)
                : Color.White);
            e.Graphics.FillRectangle(background, e.Bounds);
            var text = list.Items[e.Index]?.ToString() ?? "";
            TextRenderer.DrawText(e.Graphics, text, list.Font, e.Bounds,
                Color.FromArgb(30, 40, 55),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }

    private static Button Btn(string text, int x, int y, int w, int h, Action click)
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

    private void RefreshConds()
    {
        _conds.Items.Clear();
        foreach (var c in _conditions) _conds.Items.Add(Engine.ConditionText(c));
    }

    private void RefreshActs()
    {
        _acts.Items.Clear();
        foreach (var a in _actions) _acts.Items.Add(Engine.ActionText(a));
    }

    private List<WatchConfig> LinkTargets() => _allConfigs.Where(x => x.Id != _src.Id).ToList();

    private static string TargetText(WatchConfig config) =>
        $"{config.Name} [{config.Id[..Math.Min(8, config.Id.Length)]}]";

    private void AddLinkCondition()
    {
        var targets = LinkTargets();
        if (targets.Count == 0)
        {
            MessageBox.Show(this, "请先创建另一套配置，才能建立连携。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var f = new SimpleForm("添加连携条件", 600, 300);
        var type = f.AddCombo("条件：上游冷却中 / 上游未触发",
            new[] { "link_cooling", "link_untriggered" }, "link_cooling");
        var target = f.AddCombo("上游配置", targets.Select(TargetText).ToArray(), TargetText(targets[0]));
        if (f.ShowDialog(this) != DialogResult.OK) return;
        var index = target.SelectedIndex;
        if (index < 0) return;
        _conditions.Add(new WatchCondition { Type = type.Text, LinkedConfigId = targets[index].Id });
        RefreshConds();
    }

    private void AddLinkAction(WatchAction? existing = null)
    {
        var targets = LinkTargets();
        if (targets.Count == 0)
        {
            MessageBox.Show(this, "请先创建另一套配置，才能建立连携。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var f = new SimpleForm("添加连携动作", 600, 250);
        var existingTarget = targets.FirstOrDefault(x => x.Id == existing?.LinkedConfigId) ?? targets[0];
        var target = f.AddCombo("触发后立即检测的配置", targets.Select(TargetText).ToArray(), TargetText(existingTarget));
        if (f.ShowDialog(this) != DialogResult.OK || target.SelectedIndex < 0) return;
        _actions.Add(new WatchAction { Type = "link_config", LinkedConfigId = targets[target.SelectedIndex].Id });
        RefreshActs();
    }

    private void EditLinkCondition(WatchCondition old)
    {
        var targets = LinkTargets();
        if (targets.Count == 0) return;
        using var f = new SimpleForm("编辑连携条件", 600, 300);
        var type = f.AddCombo("条件：上游冷却中 / 上游未触发",
            new[] { "link_cooling", "link_untriggered" }, old.Type);
        var existingTarget = targets.FirstOrDefault(x => x.Id == old.LinkedConfigId) ?? targets[0];
        var target = f.AddCombo("上游配置", targets.Select(TargetText).ToArray(), TargetText(existingTarget));
        if (f.ShowDialog(this) != DialogResult.OK || target.SelectedIndex < 0) return;
        _conditions.Add(new WatchCondition { Type = type.Text, LinkedConfigId = targets[target.SelectedIndex].Id });
    }

    private void EditSelectedCondition()
    {
        var index = _conds.SelectedIndex;
        if (index < 0) return;
        var old = _conditions[index];
        _conditions.RemoveAt(index);
        switch (old.Type)
        {
            case "process_missing":
            case "process_running":
                EditProcess(old);
                break;
            case "port_idle":
            case "port_open":
            case "port_occupied":
            case "port_free":
                EditPort(old);
                break;
            case "link_cooling":
            case "link_untriggered":
                EditLinkCondition(old);
                break;
            default:
                EditSerial(old);
                break;
        }
        if (_conditions.Count == index) _conditions.Insert(index, old);
        RefreshConds();
    }

    private void EditSelectedAction()
    {
        var index = _acts.SelectedIndex;
        if (index < 0) return;
        var old = _actions[index];
        _actions.RemoveAt(index);
        if (old.Type == "link_config") AddLinkAction(old);
        else AddOpen(old);
        if (_actions.Count == index) _actions.Insert(index, old);
        RefreshActs();
    }

    private void EditProcess(WatchCondition old)
    {
        using var f = new SimpleForm("编辑进程条件", 560, 360);
        var name = f.AddText("进程匹配内容", old.Process);
        var type = f.AddCombo("类型", new[] { "process_missing", "process_running" }, old.Type);
        var modeText = old.ProcessMatch?.ToLowerInvariant() switch { "contains" => "模糊包含", "regex" => "正则表达式", _ => "精确匹配" };
        var match = f.AddCombo("匹配方式", new[] { "精确匹配", "模糊包含", "正则表达式" }, modeText);
        if (f.ShowDialog(this) != DialogResult.OK) return;
        var mode = match.Text switch { "模糊包含" => "contains", "正则表达式" => "regex", _ => "exact" };
        if (!Engine.IsValidProcessPattern(name.Text.Trim(), mode))
        {
            MessageBox.Show(this, "正则表达式格式无效", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _conditions.Add(new WatchCondition { Type = type.Text, Process = name.Text.Trim(), ProcessMatch = mode });
    }

    private void EditPort(WatchCondition old)
    {
        using var f = new SimpleForm("编辑网络端口条件", 560, 340);
        var host = f.AddText("主机", old.Host);
        var port = f.AddText("端口", old.Port.ToString());
        var type = f.AddCombo("类型", new[] { "port_idle", "port_open", "port_occupied", "port_free" }, old.Type);
        if (f.ShowDialog(this) != DialogResult.OK || !int.TryParse(port.Text, out var p) || p is < 1 or > 65535) return;
        _conditions.Add(new WatchCondition { Type = type.Text, Host = host.Text.Trim(), Port = p });
    }

    private void EditSerial(WatchCondition old)
    {
        using var f = new SimpleForm("编辑串口条件", 600, 400);
        var name = f.AddText("串口名", old.SerialPort);
        var type = f.AddCombo("类型", new[] { "serial_missing", "serial_present", "serial_idle", "serial_busy" }, old.Type);
        if (f.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(name.Text)) return;
        _conditions.Add(new WatchCondition { Type = type.Text, SerialPort = Engine.NormalizeSerialName(name.Text) });
    }

    private void AddProcess()
    {
        using var picker = new ProcessPickerDialog();
        if (picker.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(picker.SelectedProcess))
            return;

        using var f = new SimpleForm("进程条件", 560, 360);
        var name = f.AddText("已选择进程（可修改）", picker.SelectedProcess);
        var type = f.AddCombo("类型：process_missing=消失成立，process_running=存在成立",
            new[] { "process_missing", "process_running" }, "process_missing");
        var match = f.AddCombo("匹配方式：精确 / 模糊包含 / 正则表达式",
            new[] { "精确匹配", "模糊包含", "正则表达式" }, "精确匹配");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show(this, "请填写进程名");
            return;
        }
        var mode = match.Text switch
        {
            "模糊包含" => "contains",
            "正则表达式" => "regex",
            _ => "exact"
        };
        if (!Engine.IsValidProcessPattern(name.Text.Trim(), mode))
        {
            MessageBox.Show(this, "正则表达式格式无效，请检查后重试", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _conditions.Add(new WatchCondition { Type = type.Text, Process = name.Text.Trim(), ProcessMatch = mode });
        RefreshConds();
    }

    private void AddPort()
    {
        using var f = new SimpleForm("网络端口条件", 560, 340);
        var host = f.AddText("主机", "127.0.0.1");
        var port = f.AddText("端口", "");
        var type = f.AddCombo("类型：掉线 / 可连通 / 被占用 / 空闲可绑定",
            new[] { "port_idle", "port_open", "port_occupied", "port_free" }, "port_idle");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (!int.TryParse(port.Text, out var p) || p < 1 || p > 65535)
        {
            MessageBox.Show(this, "端口必须是 1-65535");
            return;
        }
        var h = string.IsNullOrWhiteSpace(host.Text) ? "127.0.0.1" : host.Text.Trim();
        _conditions.Add(new WatchCondition { Type = type.Text, Host = h, Port = p });
        RefreshConds();
    }

    private void AddSerial()
    {
        var available = Engine.ListSerialPorts();
        var hint = available.Length > 0
            ? "当前系统串口: " + string.Join(", ", available)
            : "当前未检测到串口（可仍填写 COM3 等）";
        using var f = new SimpleForm("串口条件", 600, 400);
        var name = f.AddText("串口名（如 COM3）", available.Length > 0 ? available[0] : "COM3");
        var type = f.AddCombo(
            "类型说明见下方提示",
            new[] { "serial_missing", "serial_present", "serial_idle", "serial_busy" },
            "serial_idle");
        f.AddHint(hint);
        f.AddHint("missing=消失  present=存在  idle=空闲无人占用  busy=非空闲被占用");
        f.AddHint("软件应占用串口却变成空闲时，选 serial_idle 可触发重启");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show(this, "请填写串口名，例如 COM3");
            return;
        }
        _conditions.Add(new WatchCondition
        {
            Type = type.Text,
            SerialPort = Engine.NormalizeSerialName(name.Text)
        });
        RefreshConds();
    }

    private void AddOpen(WatchAction? existing = null)
    {
        using var f = new SimpleForm("编辑运行文件", 640, 620);
        var path = f.AddText("可运行文件路径（exe、bat、cmd、ps1、svg 等）", existing?.Path ?? "", browse: true,
            filter: "常用可运行文件|*.exe;*.bat;*.cmd;*.ps1;*.com;*.msc;*.svg|全部文件|*.*");
        var args = f.AddText("启动参数（可选）", existing?.Args ?? "");
        var cwd = f.AddText("工作目录（可选）", existing?.Cwd ?? "");
        var kill = f.AddCheck("打开前先结束残留进程", existing?.KillBefore ?? true);
        var kname = f.AddText("要结束的进程名（可选，默认用 exe 文件名）", existing?.KillProcess ?? "");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(path.Text) || !File.Exists(path.Text.Trim()))
        {
            MessageBox.Show(this, "请选择存在的运行文件");
            return;
        }
        _actions.Add(new WatchAction
        {
            Type = "open_program",
            Path = path.Text.Trim(),
            Args = args.Text.Trim(),
            Cwd = cwd.Text.Trim(),
            KillBefore = kill.Checked,
            KillProcess = kname.Text.Trim()
        });
        RefreshActs();
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show(this, "请填写配置名称");
            return;
        }
        if (!double.TryParse(_interval.Text, out var iv) || iv <= 0)
        {
            MessageBox.Show(this, "检测间隔必须大于 0");
            return;
        }
        if (!double.TryParse(_cooldown.Text, out var cv) || cv < 0)
        {
            MessageBox.Show(this, "冷却时间必须大于等于 0");
            return;
        }
        if (_conditions.Count == 0)
        {
            MessageBox.Show(this, "至少添加一个条件");
            return;
        }
        if (_actions.Count == 0)
        {
            MessageBox.Show(this, "至少添加一个动作");
            return;
        }

        Result = new WatchConfig
        {
            Id = _src.Id,
            Name = _name.Text.Trim(),
            Enabled = _enabled.Checked,
            IntervalMin = iv,
            CooldownMin = cv,
            Logic = _logic.SelectedItem?.ToString() ?? "OR",
            Conditions = _conditions.ToList(),
            Actions = _actions.ToList(),
            TriggerCount = _src.TriggerCount,
            LastTrigger = _src.LastTrigger,
            LastMessage = _src.LastMessage
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal class SimpleForm : Form
{
    private int _y = 20;

    public SimpleForm(string title, int w, int h)
    {
        Text = title;
        Font = UiTheme.Ui;
        Width = w;
        Height = h;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        var ok = new Button
        {
            Text = "确定",
            Width = 110,
            Height = 40,
            Font = UiTheme.Ui,
            DialogResult = DialogResult.OK
        };
        var cancel = new Button
        {
            Text = "取消",
            Width = 110,
            Height = 40,
            Font = UiTheme.Ui,
            DialogResult = DialogResult.Cancel
        };
        AcceptButton = ok;
        CancelButton = cancel;
        Load += (_, _) =>
        {
            ok.Left = Width - 260;
            ok.Top = ClientSize.Height - 56;
            cancel.Left = Width - 140;
            cancel.Top = ClientSize.Height - 56;
        };
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    public TextBox AddText(string label, string value, bool browse = false, string filter = "程序 (*.exe)|*.exe|全部文件 (*.*)|*.*")
    {
        Controls.Add(new Label { Text = label, Left = 20, Top = _y, Width = 560, Height = 28, Font = UiTheme.Ui });
        _y += 30;
        var tb = new TextBox
        {
            Left = 20,
            Top = _y,
            Width = browse ? 430 : 540,
            Height = 32,
            Font = UiTheme.Ui,
            Text = value
        };
        Controls.Add(tb);
        if (browse)
        {
            var b = new Button
            {
                Text = "浏览",
                Left = 460,
                Top = _y - 2,
                Width = 100,
                Height = 36,
                Font = UiTheme.Ui
            };
            b.Click += (_, _) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = filter
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                    tb.Text = ofd.FileName;
            };
            Controls.Add(b);
        }
        _y += 44;
        return tb;
    }

    public ComboBox AddCombo(string label, string[] items, string selected)
    {
        Controls.Add(new Label { Text = label, Left = 20, Top = _y, Width = 560, Height = 28, Font = UiTheme.Ui });
        _y += 30;
        var cb = new ComboBox
        {
            Left = 20,
            Top = _y,
            Width = 540,
            Height = 32,
            Font = UiTheme.Ui,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cb.Items.AddRange(items);
        cb.SelectedItem = selected;
        Controls.Add(cb);
        _y += 44;
        return cb;
    }

    public void AddHint(string text)
    {
        Controls.Add(new Label
        {
            Text = text,
            Left = 20,
            Top = _y,
            Width = 540,
            Height = 28,
            Font = UiTheme.Ui,
            ForeColor = Color.DimGray
        });
        _y += 32;
    }

    public CheckBox AddCheck(string text, bool value)
    {
        var cb = new CheckBox
        {
            Text = text,
            Left = 20,
            Top = _y,
            Width = 540,
            Height = 32,
            Font = UiTheme.Ui,
            Checked = value
        };
        Controls.Add(cb);
        _y += 40;
        return cb;
    }
}
