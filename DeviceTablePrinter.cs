namespace DeviceCfg;

/// <summary>
/// Turns DeviceConfig objects into readable console output. Kept separate
/// from DeviceRepository so formatting changes never touch data-access code.
/// </summary>
public static class DeviceTablePrinter
{
    private const string NotSet = "(not set)";

    public static void PrintTable(List<DeviceConfig> devices)
    {
        if (devices.Count == 0)
        {
            Console.WriteLine("No devices found in the table.");
            return;
        }

        string header = string.Format(
            "{0,-8} {1,-6} {2,-28} {3,-16} {4,-7} {5}",
            "EquipId", "Type", "Name", "IP", "Enable", "Warnings");

        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length + 30));

        // TaskID -> the EquipIds of every device using it. Computed once
        // here from the in-memory list, since the full list is already
        // loaded — no need for an extra database round trip.
        Dictionary<int, List<int>> equipIdsByTaskId = devices
            .GroupBy(d => d.TaskId)
            .ToDictionary(g => g.Key, g => g.Select(d => d.EquipId).ToList());

        foreach (DeviceConfig device in devices)
        {
            List<string> warnings = device.GetDataWarnings();
            string warningText = warnings.Count > 0 ? string.Join("; ", warnings) : "-";

            Console.WriteLine(string.Format(
                "{0,-8} {1,-6} {2,-28} {3,-16} {4,-7} {5}",
                device.EquipId,
                device.EquipType,
                Truncate(device.EquipName, 28),
                device.IpAddress ?? NotSet,
                device.IsEnabled ? "Yes" : "No",
                warningText));
        }

        PrintSharedTaskIdNotes(equipIdsByTaskId);
    }

    public static void PrintDetail(DeviceConfig d, List<int> equipIdsSharingTaskId)
    {
        Console.WriteLine($"EquipId    : {d.EquipId}");
        Console.WriteLine($"TaskId     : {d.TaskId}");
        Console.WriteLine($"EquipType  : {d.EquipType}");
        Console.WriteLine($"EquipName  : {d.EquipName}");
        Console.WriteLine($"IpAddress  : {d.IpAddress ?? NotSet}");
        Console.WriteLine($"Port       : {FormatNullable(d.Port)}");
        Console.WriteLine($"SlotIndex  : {FormatNullable(d.SlotIndex)}");
        Console.WriteLine($"Enable     : {(d.IsEnabled ? "Yes" : "No")} (raw value: {d.EnableRaw})");
        Console.WriteLine($"Version    : {(d.Version.HasValue ? d.Version.Value.ToString("0.00") : NotSet)}");
        Console.WriteLine($"Trigger    : {d.Trigger ?? NotSet}");
        Console.WriteLine($"ScanRate   : {FormatNullable(d.ScanRate)}");
        Console.WriteLine($"LastUpdate : {(d.LastUpdate.HasValue ? d.LastUpdate.Value.ToString("yyyy-MM-dd HH:mm:ss") : NotSet)}");

        List<string> warnings = d.GetDataWarnings();
        if (warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Data warnings:");
            foreach (string w in warnings)
            {
                Console.WriteLine($"  - {w}");
            }
        }

        if (equipIdsSharingTaskId.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Note: TaskId {d.TaskId} is also used by EquipId {string.Join(", ", equipIdsSharingTaskId)}.");
            Console.WriteLine("This is not treated as an error — the brief does not state TaskID must be unique.");
        }
    }

    private static void PrintSharedTaskIdNotes(Dictionary<int, List<int>> equipIdsByTaskId)
    {
        List<KeyValuePair<int, List<int>>> shared = equipIdsByTaskId.Where(kv => kv.Value.Count > 1).ToList();
        if (shared.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Notes:");
        foreach (var kv in shared)
        {
            Console.WriteLine($"  - TaskId {kv.Key} is shared by EquipId {string.Join(", ", kv.Value)} " +
                               "(not treated as an error — TaskID is not declared unique).");
        }
    }

    private static string FormatNullable(int? value) => value.HasValue ? value.Value.ToString() : NotSet;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
