using Microsoft.Data.SqlClient;

namespace DeviceCfg;

/// <summary>
/// All database access for dbo.t_DeviceCfg lives here. Every query is
/// parameterized (no string concatenation), so a device name containing a
/// character like an apostrophe — see EquipId 8, "O'Brien Lift Controller" —
/// can never break a query or open the door to SQL injection.
/// </summary>
public class DeviceRepository
{
    private readonly string _connectionString;

    public DeviceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<DeviceConfig> GetAll()
    {
        var devices = new List<DeviceConfig>();

        using SqlConnection connection = OpenConnection();

        const string sql = @"
            SELECT EquipId, TaskID, EquipType, EquipName, IpAddress, Port,
                   SlotIndex, Enable, Version, [Trigger], ScanRate, LastUpdate
            FROM dbo.t_DeviceCfg
            ORDER BY EquipId;";

        using var command = new SqlCommand(sql, connection);

        try
        {
            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                devices.Add(MapRow(reader));
            }
        }
        catch (SqlException ex)
        {
            throw WrapQueryException(ex);
        }

        return devices;
    }

    public DeviceConfig? GetById(int equipId)
    {
        using SqlConnection connection = OpenConnection();

        const string sql = @"
            SELECT EquipId, TaskID, EquipType, EquipName, IpAddress, Port,
                   SlotIndex, Enable, Version, [Trigger], ScanRate, LastUpdate
            FROM dbo.t_DeviceCfg
            WHERE EquipId = @EquipId;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@EquipId", System.Data.SqlDbType.Int).Value = equipId;

        try
        {
            using SqlDataReader reader = command.ExecuteReader();
            return reader.Read() ? MapRow(reader) : null;
        }
        catch (SqlException ex)
        {
            throw WrapQueryException(ex);
        }
    }

    /// <summary>
    /// Returns the EquipIds of other devices that share the given TaskId.
    /// Used to give the user a neutral heads-up (not an error) that more
    /// than one device is tagged with the same TaskID — the brief states no
    /// rule that TaskID must be unique, so this is reported as an
    /// observation, not flagged as invalid data.
    /// </summary>
    public List<int> GetEquipIdsWithSameTaskId(int taskId, int excludeEquipId)
    {
        var equipIds = new List<int>();

        using SqlConnection connection = OpenConnection();

        const string sql = @"
            SELECT EquipId
            FROM dbo.t_DeviceCfg
            WHERE TaskID = @TaskId AND EquipId <> @ExcludeEquipId
            ORDER BY EquipId;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TaskId", System.Data.SqlDbType.Int).Value = taskId;
        command.Parameters.Add("@ExcludeEquipId", System.Data.SqlDbType.Int).Value = excludeEquipId;

        try
        {
            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                equipIds.Add(reader.GetInt32(0));
            }
        }
        catch (SqlException ex)
        {
            throw WrapQueryException(ex);
        }

        return equipIds;
    }

    private SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            connection.Open();
        }
        catch (SqlException ex)
        {
            connection.Dispose();
            throw WrapConnectionException(ex);
        }

        return connection;
    }

    /// <summary>
    /// Turns a raw SqlException from opening the connection into a message
    /// a non-DBA can act on. SQL Server error 4060 specifically means "the
    /// database you asked for doesn't exist" — which, for this project,
    /// almost always means 01-setup.sql was never run.
    /// </summary>
    private static DeviceConfigException WrapConnectionException(SqlException ex)
    {
        if (ex.Number == 4060)
        {
            return new DeviceConfigException(
                "Could not open the 'ICAS_Test' database — it does not appear to exist on this SQL Server instance.\n" +
                "Have you run 01-setup.sql yet? See README.md.\n" +
                $"Original error: {ex.Message}", ex);
        }

        return new DeviceConfigException(
            "Could not connect to the database. Please check that:\n" +
            "  1. The SQL Server service is running;\n" +
            "  2. The connection string in appsettings.json (server name, database name) is correct.\n" +
            $"Original error: {ex.Message}", ex);
    }

    /// <summary>
    /// Turns a raw SqlException from running a query into a message a
    /// non-DBA can act on. Error 208 specifically means "the table you
    /// asked for doesn't exist" — again, almost always a missed
    /// 01-setup.sql for this project.
    /// </summary>
    private static DeviceConfigException WrapQueryException(SqlException ex)
    {
        if (ex.Number == 208)
        {
            return new DeviceConfigException(
                "The table 'dbo.t_DeviceCfg' does not exist in this database.\n" +
                "Have you run 01-setup.sql yet? See README.md.\n" +
                $"Original error: {ex.Message}", ex);
        }

        return new DeviceConfigException($"The query against the database failed: {ex.Message}", ex);
    }

    private static DeviceConfig MapRow(SqlDataReader reader)
    {
        return new DeviceConfig
        {
            EquipId = reader.GetInt32(reader.GetOrdinal("EquipId")),
            TaskId = reader.GetInt32(reader.GetOrdinal("TaskID")),
            EquipType = reader.GetString(reader.GetOrdinal("EquipType")),
            EquipName = reader.GetString(reader.GetOrdinal("EquipName")),
            IpAddress = GetNullableString(reader, "IpAddress"),
            Port = GetNullableInt(reader, "Port"),
            SlotIndex = GetNullableInt(reader, "SlotIndex"),
            EnableRaw = reader.GetInt16(reader.GetOrdinal("Enable")),
            Version = GetNullableDouble(reader, "Version"),
            Trigger = GetNullableString(reader, "Trigger"),
            ScanRate = GetNullableInt(reader, "ScanRate"),
            LastUpdate = GetNullableDateTime(reader, "LastUpdate"),
        };
    }

    private static string? GetNullableString(SqlDataReader reader, string column)
    {
        int ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(SqlDataReader reader, string column)
    {
        int ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static double? GetNullableDouble(SqlDataReader reader, string column)
    {
        int ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string column)
    {
        int ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
