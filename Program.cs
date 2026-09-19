namespace DeviceCfg;

public static class Program
{
    public static int Main(string[] args)
    {
        string connectionString;
        try
        {
            connectionString = ConfigLoader.LoadConnectionString();
        }
        catch (DeviceConfigException ex)
        {
            Console.Error.WriteLine($"Configuration error: {ex.Message}");
            return 1;
        }

        var repository = new DeviceRepository(connectionString);

        try
        {
            if (args.Length == 0)
            {
                List<DeviceConfig> devices = repository.GetAll();
                DeviceTablePrinter.PrintTable(devices);
                return 0;
            }

            if (args.Length > 1)
            {
                Console.Error.WriteLine("Too many arguments. Usage: DeviceCfg <EquipId> (no argument prints every device)");
                return 1;
            }

            if (!int.TryParse(args[0], out int equipId))
            {
                Console.Error.WriteLine($"Error: the device ID must be an integer, you entered \"{args[0]}\".");
                Console.Error.WriteLine("Usage: DeviceCfg <EquipId>");
                return 1;
            }

            DeviceConfig? device = repository.GetById(equipId);
            if (device is null)
            {
                Console.Error.WriteLine($"Error: no device found with EquipId = {equipId}.");
                return 1;
            }

            List<int> sharedTaskIdEquipIds = repository.GetEquipIdsWithSameTaskId(device.TaskId, device.EquipId);
            DeviceTablePrinter.PrintDetail(device, sharedTaskIdEquipIds);
            return 0;
        }
        catch (DeviceConfigException ex)
        {
            // Expected failure modes: bad connection, missing table, etc. Clean message, no stack trace.
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            // Catch-all so nothing exits with a raw stack trace; anything
            // reaching here is something not specifically anticipated.
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }
}
