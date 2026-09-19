namespace DeviceCfg;

/// <summary>
/// Thrown for problems this program specifically expects and knows how to
/// explain in plain language: can't reach the database, config file
/// missing/broken, that kind of thing. Program.cs catches this one type
/// and prints ex.Message directly instead of a raw stack trace.
/// </summary>
public class DeviceConfigException : Exception
{
    public DeviceConfigException(string message) : base(message)
    {
    }

    public DeviceConfigException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
