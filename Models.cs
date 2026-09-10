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
    public double IntervalMin { get; set; } = 2;
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
    public string Process { get; set; } = "";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; }
}

public class WatchAction
{
    public string Type { get; set; } = "open_program";
    public string Path { get; set; } = "";
    public string Args { get; set; } = "";
    public string Cwd { get; set; } = "";
    public bool KillBefore { get; set; } = true;
    public string KillProcess { get; set; } = "";
}
