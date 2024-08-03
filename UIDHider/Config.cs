namespace UIDHider;

public struct Config {
    public Config() { }

    public bool Enabled { get; set; } = true;
    public string ReplaceWith { get; set; } = "UID: 10001";
}
