using System.Diagnostics;
using System.Net.Sockets;

namespace ProcessGuard;

public static class Engine
{
    public static bool ProcessRunning(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim();
        if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            n = n[..^4];
        try
        {
            return Process.GetProcessesByName(n).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static int KillByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        var n = name.Trim();
        if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            n = n[..^4];
        Process[] list;
        try { list = Process.GetProcessesByName(n); }
        catch { return 0; }

        var count = 0;
        foreach (var p in list)
        {
            try
            {
                if (p.Id == Environment.ProcessId) continue;
                p.CloseMainWindow();
                count++;
            }
            catch { }
        }
        Thread.Sleep(400);
        try { list = Process.GetProcessesByName(n); }
        catch { return count; }
        foreach (var p in list)
        {
            try
            {
                if (p.Id == Environment.ProcessId) continue;
                p.Kill(true);
            }
            catch { }
        }
        return count;
    }

    public static bool PortOpen(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host)) host = "127.0.0.1";
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(host, port);
            return task.Wait(1000) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public static bool EvalCondition(WatchCondition c) => c.Type switch
    {
        "process_missing" => !ProcessRunning(c.Process),
        "process_running" => ProcessRunning(c.Process),
        "port_idle" => !PortOpen(c.Host, c.Port),
        "port_open" => PortOpen(c.Host, c.Port),
        _ => false
    };

    public static bool EvalConditions(WatchConfig cfg)
    {
        if (cfg.Conditions.Count == 0) return false;
        var results = cfg.Conditions.Select(EvalCondition).ToList();
        return string.Equals(cfg.Logic, "AND", StringComparison.OrdinalIgnoreCase)
            ? results.All(x => x)
            : results.Any(x => x);
    }

    public static string RunAction(WatchAction action)
    {
        if (action.Type != "open_program") return "未知动作";
        if (string.IsNullOrWhiteSpace(action.Path)) return "未指定程序路径";

        var notes = new List<string>();
        if (action.KillBefore)
        {
            var pname = string.IsNullOrWhiteSpace(action.KillProcess)
                ? Path.GetFileName(action.Path)
                : action.KillProcess;
            var n = KillByName(pname);
            notes.Add(n > 0 ? $"已结束残留 {n} 个 {pname}" : $"无残留进程 {pname}");
            Thread.Sleep(500);
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = action.Path,
                Arguments = action.Args ?? "",
                UseShellExecute = true
            };
            if (!string.IsNullOrWhiteSpace(action.Cwd))
                psi.WorkingDirectory = action.Cwd;
            Process.Start(psi);
            notes.Add("已启动: " + action.Path);
        }
        catch (Exception ex)
        {
            notes.Add("启动失败: " + ex.Message);
        }
        return string.Join("；", notes);
    }

    public static string ConditionText(WatchCondition c) => c.Type switch
    {
        "process_missing" => "进程消失: " + c.Process,
        "process_running" => "进程存在: " + c.Process,
        "port_idle" => $"端口空闲/掉线: {c.Host}:{c.Port}",
        "port_open" => $"端口可连通: {c.Host}:{c.Port}",
        _ => c.Type
    };

    public static string ActionText(WatchAction a)
    {
        var extra = string.IsNullOrWhiteSpace(a.Args) ? "" : "  参数:" + a.Args;
        var kill = a.KillBefore ? "先结束残留" : "不结束残留";
        return $"打开软件: {a.Path}{extra}  [{kill}]";
    }
}
