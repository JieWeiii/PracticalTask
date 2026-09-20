namespace DeviceCfg;

/// <summary>
/// One row of dbo.t_DeviceCfg — one physical device on the warehouse floor
/// (a PLC, a barcode scanner, a display, a checkweigher, etc).
///
/// Several fields are nullable because the table itself allows NULL for
/// them (see 01-setup.sql), and the sample data actually uses that in a
/// few rows — this model reflects that instead of pretending every field
/// is always filled in.
/// </summary>
public class DeviceConfig
{
    public int EquipId { get; set; }
    public int TaskId { get; set; }
    public string EquipType { get; set; } = string.Empty;
    public string EquipName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public int? SlotIndex { get; set; }

    /// <summary>
    /// The raw value straight from the Enable column. The table only
    /// declares this as SMALLINT NOT NULL — there is no CHECK constraint
    /// limiting it to 0/1, so the raw value is kept around instead of being
    /// collapsed into a bool immediately. IsEnabled below does that
    /// collapse, and GetDataWarnings() flags anything that isn't actually
    /// 0 or 1 instead of silently treating it as "enabled".
    /// </summary>
    public short EnableRaw { get; set; }

    public bool IsEnabled => EnableRaw != 0;

    public double? Version { get; set; }
    public string? Trigger { get; set; }
    public int? ScanRate { get; set; }
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// Looks at this device's own values and returns plain-language warnings
    /// for anything missing or suspicious — WITHOUT throwing or rejecting the
    /// row. This only looks at this one row; anything that requires
    /// comparing across rows (like a shared TaskID) is handled separately,
    /// since this method has no way to see the rest of the table.
    /// </summary>
    public List<string> GetDataWarnings()
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(IpAddress))
            warnings.Add("IP address not set");

        if (!Port.HasValue)
            warnings.Add("Port not set");

        if (!SlotIndex.HasValue)
            warnings.Add("SlotIndex not set");
        else if (SlotIndex.Value < 0)
            warnings.Add($"SlotIndex is negative ({SlotIndex.Value})");

        if (!ScanRate.HasValue)
            warnings.Add("ScanRate not set");
        else if (ScanRate.Value == 0)
            warnings.Add("ScanRate is 0");

        if (EnableRaw != 0 && EnableRaw != 1)
            warnings.Add($"Enable has an unexpected value ({EnableRaw}), expected 0 or 1");
        else if (!IsEnabled)
            warnings.Add("Device is disabled (Enable = 0)");

        if (!Version.HasValue)
            warnings.Add("Version not set");

        if (string.IsNullOrWhiteSpace(Trigger))
            warnings.Add("Trigger not set");

        if (!LastUpdate.HasValue)
            warnings.Add("LastUpdate has never been recorded");

        return warnings;
    }
}
