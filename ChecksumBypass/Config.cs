namespace ChecksumBypass;

public struct Config {
    public Config() { }

    public bool Enabled { get; set; } = false;
    public string ReplaceWith { get; set; } = "";
}
