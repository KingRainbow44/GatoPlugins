namespace Visualizer;

public struct Config {
    public Config() { }

    public bool IsObfuscated { get; set; } = false;

    public bool EnableLogger { get; set; } = false;
    public string BindAddress { get; set; } = "0.0.0.0";
    public ushort BindPort { get; set; } = 8080;

    public bool HighlightedOnly { get; set; } = false;
    public List<string> Highlighted { get; set; } = [];
    public List<string> Blacklisted { get; set; } = ["UnionCmdNotify", "WorldPlayerRTTNotify", "PingReq", "PingRsp"];
}
