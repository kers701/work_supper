namespace ProcessGuard;

public class ConfigDialog : Form
{
    public WatchConfig? Result { get; private set; }

    private readonly WatchConfig _src;
    private readonly TextBox _name = new();
    private readonly TextBox _interval = new();
    private readonly TextBox _cooldown = new();
    private readonly ComboBox _logic = new();
    private readonly ListBox _conds = new();
    private readonly ListBox _acts = new();
    private readonly List<WatchCondition> _conditions = new();
    private readonly List<WatchAction> _actions = new();

    public ConfigDialog(WatchConfig? existing)
    {
        _src = existing ?? new WatchConfig();
        _conditions.AddRange(_src.Conditions);
        _actions.AddRange(_src.Actions);

        Text = existing == null ? "添加配置" : "编辑配置";
        Width = 760;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var y = 16;
        Controls.Add(LabelAt("配置名称", 16, y, 80));
        _name.SetBounds(100, y - 2, 620, 24);
        _name.Text = _src.Name;
        Controls.Add(_name);
        y += 36;

        Controls.Add(LabelAt("检测间隔(分钟)", 16, y, 110));
        _interval.SetBounds(126, y - 2, 60, 24);
        _interval.Text = _src.IntervalMin.ToString();
        Controls.Add(_interval);

        Controls.Add(LabelAt("触发后冷却(分钟)", 200, y, 120));
        _cooldown.SetBounds(322, y - 2, 60, 24);
        _cooldown.Text = _src.CooldownMin.ToString();
        Controls.Add(_cooldown);

        Controls.Add(LabelAt("条件逻辑", 400, y, 70));
        _logic.SetBounds(470, y - 2, 80, 24);
        _logic.DropDownStyle = ComboBoxStyle.DropDownList;
        _logic.Items.AddRange(new object[] { "OR", "AND" });
        _logic.SelectedItem = _src.Logic is "AND" or "OR" ? _src.Logic : "OR";
        Controls.Add(_logic);
        Controls.Add(LabelAt("OR任一成立  AND全部成立", 560, y + 2, 180));
        y += 40;

        Controls.Add(LabelAt("条件 (IF)", 16, y, 200));
        y += 22;
        _conds.SetBounds(16, y, 710, 150);
        Controls.Add(_conds);
        y += 158;
        var bp = Btn("添加进程条件", 16, y, 120, AddProcess);
        var bport = Btn("添加端口条件", 146, y, 120, AddPort);
        var bdc = Btn("删除选中条件", 276, y, 120, () =>
        {
            if (_conds.SelectedIndex >= 0)
            {
                _conditions.RemoveAt(_conds.SelectedIndex);
                RefreshConds();
            }
        });
        Controls.AddRange(new Control[] { bp, bport, bdc });
        y += 40;

        Controls.Add(LabelAt("动作 (THEN)", 16, y, 200));
        y += 22;
        _acts.SetBounds(16, y, 710, 130);
        Controls.Add(_acts);
        y += 138;
        Controls.Add(Btn("添加打开软件", 16, y, 120, AddOpen));
        Controls.Add(Btn("删除选中动作", 146, y, 120, () =>
        {
            if (_acts.SelectedIndex >= 0)
            {
                _actions.RemoveAt(_acts.SelectedIndex);
                RefreshActs();
            }
        }));

        var save = Btn("保存配置", 520, 620, 100, Save);
        var cancel = Btn("取消", 630, 620, 90, () => { DialogResult = DialogResult.Cancel; Close(); });
        Controls.AddRange(new Control[] { save, cancel });

        RefreshConds();
        RefreshActs();
    }

    private static Label LabelAt(string text, int x, int y, int w) =>
        new() { Text = text, Left = x, Top = y, Width = w };

    private static Button Btn(string text, int x, int y, int w, Action click)
    {
        var b = new Button { Text = text, Left = x, Top = y, Width = w };
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

    private void AddProcess()
    {
        using var f = new SimpleForm("进程条件", 460, 220);
        var name = f.AddText("进程名（如 ScanApp.exe）", "");
        var type = f.AddCombo("类型：process_missing=消失成立，process_running=存在成立",
            new[] { "process_missing", "process_running" }, "process_missing");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            MessageBox.Show("请填写进程名");
            return;
        }
        _conditions.Add(new WatchCondition { Type = type.Text, Process = name.Text.Trim() });
        RefreshConds();
    }

    private void AddPort()
    {
        using var f = new SimpleForm("端口条件", 460, 260);
        var host = f.AddText("主机", "127.0.0.1");
        var port = f.AddText("端口", "");
        var type = f.AddCombo("类型：port_idle=掉线成立，port_open=能连通成立",
            new[] { "port_idle", "port_open" }, "port_idle");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (!int.TryParse(port.Text, out var p) || p < 1 || p > 65535)
        {
            MessageBox.Show("端口必须是 1-65535");
            return;
        }
        var h = string.IsNullOrWhiteSpace(host.Text) ? "127.0.0.1" : host.Text.Trim();
        _conditions.Add(new WatchCondition { Type = type.Text, Host = h, Port = p });
        RefreshConds();
    }

    private void AddOpen()
    {
        using var f = new SimpleForm("打开软件", 560, 360);
        var path = f.AddText("程序路径", "", browse: true);
        var args = f.AddText("启动参数（可选）", "");
        var cwd = f.AddText("工作目录（可选）", "");
        var kill = f.AddCheck("打开前先结束残留进程", true);
        var kname = f.AddText("要结束的进程名（可选，默认用 exe 文件名）", "");
        if (f.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(path.Text))
        {
            MessageBox.Show("请选择程序路径");
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
            MessageBox.Show("请填写配置名称");
            return;
        }
        if (!double.TryParse(_interval.Text, out var iv) || iv <= 0)
        {
            MessageBox.Show("检测间隔必须大于 0");
            return;
        }
        if (!double.TryParse(_cooldown.Text, out var cv) || cv < 0)
        {
            MessageBox.Show("冷却时间必须大于等于 0");
            return;
        }
        if (_conditions.Count == 0)
        {
            MessageBox.Show("至少添加一个条件");
            return;
        }
        if (_actions.Count == 0)
        {
            MessageBox.Show("至少添加一个动作");
            return;
        }

        Result = new WatchConfig
        {
            Id = _src.Id,
            Name = _name.Text.Trim(),
            Enabled = _src.Enabled,
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
    private int _y = 16;

    public SimpleForm(string title, int w, int h)
    {
        Text = title;
        Width = w;
        Height = h;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        var ok = new Button { Text = "确定", Width = 90, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", Width = 90, DialogResult = DialogResult.Cancel };
        AcceptButton = ok;
        CancelButton = cancel;
        Load += (_, _) =>
        {
            ok.Left = Width - 220;
            ok.Top = Height - 80;
            cancel.Left = Width - 120;
            cancel.Top = Height - 80;
        };
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    public TextBox AddText(string label, string value, bool browse = false)
    {
        Controls.Add(new Label { Text = label, Left = 16, Top = _y, Width = 500 });
        _y += 22;
        var tb = new TextBox { Left = 16, Top = _y, Width = browse ? 380 : 500, Text = value };
        Controls.Add(tb);
        if (browse)
        {
            var b = new Button { Text = "浏览", Left = 406, Top = _y - 2, Width = 80 };
            b.Click += (_, _) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "程序 (*.exe)|*.exe|全部文件 (*.*)|*.*"
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                    tb.Text = ofd.FileName;
            };
            Controls.Add(b);
        }
        _y += 32;
        return tb;
    }

    public ComboBox AddCombo(string label, string[] items, string selected)
    {
        Controls.Add(new Label { Text = label, Left = 16, Top = _y, Width = 500 });
        _y += 22;
        var cb = new ComboBox
        {
            Left = 16,
            Top = _y,
            Width = 500,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cb.Items.AddRange(items);
        cb.SelectedItem = selected;
        Controls.Add(cb);
        _y += 32;
        return cb;
    }

    public CheckBox AddCheck(string text, bool value)
    {
        var cb = new CheckBox { Text = text, Left = 16, Top = _y, Width = 500, Checked = value };
        Controls.Add(cb);
        _y += 32;
        return cb;
    }
}
