namespace ProcessGuard;

public class AppState
{
    public bool MasterEnabled { get; set; } = true;
    public List<WatchConfig> Configs { get; set; } = new();
}

public class WatchConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..10];
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    /// <summary>检测间隔，单位：分钟</summary>
    public double IntervalMin { get; set; } = 2;
    /// <summary>触发后冷却，单位：分钟，可自定义</summary>
    public double CooldownMin { get; set; } = 3;
    public string Logic { get; set; } = "OR";
    public List<WatchCondition> Conditions { get; set; } = new();
    public List<WatchAction> Actions { get; set; } = new();
    public int TriggerCount { get; set; }
    public string LastTrigger { get; set; } = "";
    public string LastMessage { get; set; } = "";
}

public class WatchCondition
{
    public string Type { get; set; } = "process_missing";
    /// <summary>连携目标配置 ID，用于 link_* 条件。</summary>
    public string LinkedConfigId { get; set; } = "";
    public string Process { get; set; } = "";
    /// <summary>进程匹配模式：exact 精确、contains 模糊、regex 正则。</summary>
    public string ProcessMatch { get; set; } = "exact";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    /// <summary>串口名，如 COM3</summary>
    public string SerialPort { get; set; } = "";
}

public class WatchAction
{
    public string Type { get; set; } = "open_program";
    /// <summary>连携目标配置 ID，用于 link_config 动作。</summary>
    public string LinkedConfigId { get; set; } = "";
    public string Path { get; set; } = "";
    public string Args { get; set; } = "";
    public string Cwd { get; set; } = "";
    public bool KillBefore { get; set; } = true;
    public string KillProcess { get; set; } = "";
}
