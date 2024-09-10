namespace DeathSwap;

public struct Config {
    public uint MinInterval { get; set; } = 45_000; // 45 seconds
    public uint MaxInterval { get; set; } = 60 * 2 * 1000; // 2 minutes

    public Config() { }
}
