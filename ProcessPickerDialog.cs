using System.Diagnostics;

namespace ProcessGuard;

public sealed class ProcessPickerDialog : Form
{
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();
    private readonly Label _status = new();
    private readonly List<ProcessRow> _items = new();
    public string? SelectedProcess { get; private set; }

    private sealed record ProcessRow(string Name, int Id, string WindowTitle, long MemoryMb);

    public ProcessPickerDialog()
    {
        Text = "选择正在运行的进程";
        Font = UiTheme.Ui;
        Width = 760;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(620, 420);

        Controls.Add(new Label { Text = "搜索进程", Left = 20, Top = 20, Width = 100, Height = 30, Font = UiTheme.UiBold });
        _search.SetBounds(120, 16, 430, 36);
        _search.Font = UiTheme.Ui;
        _search.PlaceholderText = "输入进程名或窗口标题";
        _search.TextChanged += (_, _) => RefreshRows();
        Controls.Add(_search);
        var refresh = new Button { Text = "刷新", Left = 565, Top = 16, Width = 80, Height = 36, Font = UiTheme.Ui };
        refresh.Click += (_, _) => LoadProcesses();
        Controls.Add(refresh);

        _grid.SetBounds(20, 68, 705, 430);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 225, 242, 255);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 55);
        _grid.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 225, 242, 255);
        _grid.RowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 55);
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 225, 242, 255);
        _grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 55);
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowTemplate.Height = 42;
        _grid.ColumnHeadersHeight = 42;
        _grid.Columns.Add("name", "进程");
        _grid.Columns.Add("id", "PID");
        _grid.Columns.Add("title", "窗口标题");
        _grid.Columns.Add("memory", "内存");
        _grid.Columns["name"]!.FillWeight = 125;
        _grid.Columns["id"]!.FillWeight = 55;
        _grid.Columns["title"]!.FillWeight = 180;
        _grid.Columns["memory"]!.FillWeight = 75;
        _grid.CellDoubleClick += (_, _) => Choose();
        Controls.Add(_grid);

        _status.SetBounds(20, 510, 480, 30);
        _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _status.ForeColor = Color.DimGray;
        Controls.Add(_status);
        var cancel = new Button { Text = "取消", Left = 520, Top = 506, Width = 90, Height = 40, Font = UiTheme.Ui, DialogResult = DialogResult.Cancel };
        cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        var choose = new Button { Text = "添加选中", Left = 620, Top = 506, Width = 105, Height = 40, Font = UiTheme.Ui, DialogResult = DialogResult.None };
        choose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        choose.Click += (_, _) => Choose();
        Controls.AddRange(new Control[] { cancel, choose });
        AcceptButton = choose;
        CancelButton = cancel;
        Load += (_, _) => LoadProcesses();
    }

    private void LoadProcesses()
    {
        var rows = new List<ProcessRow>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                var name = p.ProcessName + ".exe";
                var title = p.MainWindowTitle ?? "";
                var memory = p.WorkingSet64 / (1024 * 1024);
                rows.Add(new ProcessRow(name, p.Id, title, memory));
            }
            catch { }
            finally { p.Dispose(); }
        }
        _items.Clear();
        _items.AddRange(rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id));
        RefreshRows();
    }

    private void RefreshRows()
    {
        var query = _search.Text.Trim();
        _grid.Rows.Clear();
        // 保留首行作为绘制缓冲，避免搜索刷新或滚动时首条数据残留重叠。
        _grid.Rows.Add("", "", "", "");
        foreach (var item in _items.Where(x => query.Length == 0 || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || x.WindowTitle.Contains(query, StringComparison.OrdinalIgnoreCase)))
            _grid.Rows.Add(item.Name, item.Id, item.WindowTitle, $"{item.MemoryMb:N0} MB");
        _grid.ClearSelection();
        _grid.CurrentCell = null;
        _status.Text = $"共 {_items.Count} 个进程，显示 {_grid.Rows.Count - 1} 个 · 双击或点击“添加选中”";
    }

    private void Choose()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Index == 0)
        {
            MessageBox.Show(this, "请先选中一个进程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SelectedProcess = Convert.ToString(_grid.SelectedRows[0].Cells["name"].Value);
        DialogResult = DialogResult.OK;
        Close();
    }
}
