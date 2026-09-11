using System.Diagnostics;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text.RegularExpressions;

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

    public static bool ProcessRunning(WatchCondition condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Process)) return false;
        var pattern = condition.Process.Trim();
        var exactPattern = pattern.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? pattern[..^4] : pattern;
        try
        {
            return Process.GetProcesses().Any(p =>
            {
                try
                {
                    var processName = p.ProcessName + ".exe";
                    return condition.ProcessMatch?.ToLowerInvariant() switch
                    {
                        "contains" => processName.Contains(pattern, StringComparison.OrdinalIgnoreCase),
                        "regex" => Regex.IsMatch(processName, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
                        _ => string.Equals(p.ProcessName, exactPattern, StringComparison.OrdinalIgnoreCase)
                    };
                }
                catch { return false; }
                finally { p.Dispose(); }
            });
        }
        catch { return false; }
    }

    public static bool IsValidProcessPattern(string pattern, string? mode)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return false;
        if (!string.Equals(mode, "regex", StringComparison.OrdinalIgnoreCase)) return true;
        try
        {
            _ = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
            return true;
        }
        catch (ArgumentException) { return false; }
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

    public static bool PortOccupied(string host, int port)
    {
        if (port is < 1 or > 65535) return false;
        try
        {
            var address = string.IsNullOrWhiteSpace(host) || host is "localhost" or "127.0.0.1"
                ? System.Net.IPAddress.Loopback
                : System.Net.IPAddress.Parse(host);
            using var listener = new TcpListener(address, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.AddressAlreadyInUse or SocketError.AccessDenied)
        {
            return true;
        }
        catch { return false; }
    }

    public static bool PortFree(string host, int port) => !PortOccupied(host, port);

    /// <summary>规范化串口名：COM3 / com3 / 3 → COM3</summary>
    public static string NormalizeSerialName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var s = name.Trim().ToUpperInvariant();
        var m = Regex.Match(s, @"^(?:COM)?(\d+)$");
        return m.Success ? "COM" + m.Groups[1].Value : s;
    }

    public static bool SerialPortPresent(string name)
    {
        var target = NormalizeSerialName(name);
        if (string.IsNullOrEmpty(target)) return false;
        try
        {
            var ports = SerialPort.GetPortNames();
            return ports.Any(p => NormalizeSerialName(p) == target);
        }
        catch
        {
            return false;
        }
    }

    public static string[] ListSerialPorts()
    {
        try
        {
            return SerialPort.GetPortNames()
                .Select(NormalizeSerialName)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// 串口空闲：设备在系统中，且当前可独占打开（没有程序占用）。
    /// 软件本应占用串口却变成空闲时，可用此条件触发重启。
    /// </summary>
    public static bool SerialPortIdle(string name)
    {
        var target = NormalizeSerialName(name);
        if (string.IsNullOrEmpty(target) || !SerialPortPresent(target))
            return false;
        try
        {
            using var sp = new SerialPort(target)
            {
                ReadTimeout = 100,
                WriteTimeout = 100
            };
            sp.Open();
            sp.Close();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // 已被其他进程占用 → 非空闲
            return false;
        }
        catch (IOException)
        {
            // 打开失败，视为非空闲或异常占用
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>串口非空闲（被占用）：存在且无法独占打开</summary>
    public static bool SerialPortBusy(string name)
    {
        var target = NormalizeSerialName(name);
        if (string.IsNullOrEmpty(target) || !SerialPortPresent(target))
            return false;
        return !SerialPortIdle(target);
    }

    public static bool EvalCondition(WatchCondition c) => c.Type switch
    {
        "process_missing" => !ProcessRunning(c),
        "process_running" => ProcessRunning(c),
        "port_idle" => !PortOpen(c.Host, c.Port),
        "port_open" => PortOpen(c.Host, c.Port),
        "port_occupied" => PortOccupied(c.Host, c.Port),
        "port_free" => PortFree(c.Host, c.Port),
        "serial_missing" => !SerialPortPresent(c.SerialPort),
        "serial_present" => SerialPortPresent(c.SerialPort),
        "serial_idle" => SerialPortIdle(c.SerialPort),
        "serial_busy" => SerialPortBusy(c.SerialPort),
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
        var extension = Path.GetExtension(action.Path).ToLowerInvariant();
        if (action.KillBefore && (extension is ".exe" or ".com"))
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
            var psi = new ProcessStartInfo { UseShellExecute = true };
            if (extension is ".ps1")
            {
                psi.FileName = "powershell.exe";
                psi.Arguments = $"-ExecutionPolicy Bypass -File {Quote(action.Path)} {action.Args ?? ""}";
            }
            else if (extension is ".bat" or ".cmd")
            {
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c {Quote(action.Path)} {action.Args ?? ""}";
            }
            else
            {
                psi.FileName = action.Path;
                psi.Arguments = action.Args ?? "";
            }
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

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    public static string ConditionText(WatchCondition c) => c.Type switch
    {
        "process_missing" => ProcessText("进程消失", c),
        "process_running" => ProcessText("进程存在", c),
        "port_idle" => $"网络端口空闲/掉线: {c.Host}:{c.Port}",
        "port_open" => $"网络端口可连通: {c.Host}:{c.Port}",
        "port_occupied" => $"网络端口被占用: {c.Host}:{c.Port}",
        "port_free" => $"网络端口空闲可绑定: {c.Host}:{c.Port}",
        "serial_missing" => "串口消失/掉线: " + NormalizeSerialName(c.SerialPort),
        "serial_present" => "串口存在: " + NormalizeSerialName(c.SerialPort),
        "serial_idle" => "串口空闲(无人占用): " + NormalizeSerialName(c.SerialPort),
        "serial_busy" => "串口非空闲(被占用): " + NormalizeSerialName(c.SerialPort),
        _ => c.Type
    };

    private static string ProcessText(string prefix, WatchCondition c)
    {
        var mode = c.ProcessMatch?.ToLowerInvariant() switch
        {
            "contains" => "模糊",
            "regex" => "正则",
            _ => "精确"
        };
        return $"{prefix}({mode}): {c.Process}";
    }

    public static string ActionText(WatchAction a)
    {
        var extra = string.IsNullOrWhiteSpace(a.Args) ? "" : "  参数:" + a.Args;
        var kill = a.KillBefore ? "先结束残留" : "不结束残留";
        return $"运行文件: {a.Path}{extra}  [{kill}]";
    }
}
