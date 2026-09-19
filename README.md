# DeviceCfg

A C# console application that reads device configuration from the`dbo.t_DeviceCfg` table in SQL Server (ICAS Technology practical task).

## Prerequisites

- **.NET SDK 10.0 or later.** In a terminal, run:
  ```
  dotnet --version
  ```
  If it prints a version number starting with 10 or higher, you're set. If the command isn't recognized, or shows an older version, install the .NET SDK from Microsoft first.

- **A SQL Server instance with the `ICAS_Test` database already set up.**
  This project assumes `01-setup.sql` (provided separately with the task brief) has already been run once against your SQL Server instance — that script creates the `ICAS_Test` database, the `dbo.t_DeviceCfg` table, and loads the 10 sample rows. This README does not repeat those steps; see the brief for how to run it.

## Configure the connection

Open `appsettings.json` and check the connection string matches your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "ICAS_Test": "Server=localhost\\SQLEXPRESS;Database=ICAS_Test;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

The default assumes a local SQL Server Express instance named `SQLEXPRESS`, using Windows Authentication. If your instance has a different name, or you're using SQL Login instead, edit this line accordingly — no code changes are needed.

## Run it

Open a terminal in the project folder (the one containing `DeviceCfg.csproj`) and run:

```
dotnet restore
dotnet run
```

With no arguments, this connects to the database and prints every device.

To print just one device, add its EquipId after the command, the same way the brief's own example does (`DeviceCfg.exe 3`):

```
dotnet run -- 3
```

Everything after `--` is passed straight through to the program itself — `3` is not an option for `dotnet run`, it's the EquipId the program reads from `args[0]`. Any valid EquipId from the sample data (1–10) works the same way, e.g. `dotnet run -- 8`.

## Example output

Printing the full table (`dotnet run`, no arguments):

```
EquipId  Type   Name                         IP               Enable  Warnings
------------------------------------------------------------------------------------------------------------
1        PLC    Infeed Conveyor PLC          192.168.10.11    Yes     -
2        PLC    Sorter PLC                   192.168.10.12    Yes     -
3        SCAN   Induction Scanner 1          192.168.10.31    Yes     -
4        SCAN   Induction Scanner 2          (not set)        Yes     IP address not set
5        LED    Lane 1 Display               192.168.10.51    No      Device is disabled (Enable = 0)
6        PTL    Pick-to-Light Zone A         192.168.10.61    Yes     SlotIndex is negative (-1)
7        WGT    Checkweigher 1               192.168.10.71    Yes     ScanRate is 0
8        PLC    O'Brien Lift Controller      192.168.10.13    Yes     LastUpdate has never been recorded
9        SCAN   Outfeed Scanner              192.168.10.32    Yes     Trigger not set
10       LED    Marshalling Board            192.168.10.52    Yes     -

Notes:
  - TaskId 8 is shared by EquipId 8, 9 (not treated as an error — TaskID is not declared unique).
```

Looking up a single device (`dotnet run -- 8`):

```
EquipId    : 8
TaskId     : 8
EquipType  : PLC
EquipName  : O'Brien Lift Controller
IpAddress  : 192.168.10.13
Port       : 102
SlotIndex  : 7
Enable     : Yes (raw value: 1)
Version    : 1.05
Trigger    : CYCLIC
ScanRate   : 200
LastUpdate : (not set)

Data warnings:
  - LastUpdate has never been recorded

Note: TaskId 8 is also used by EquipId 9.
This is not treated as an error — the brief does not state TaskID must be unique.
```

## Build a standalone .exe

The brief's own example (`DeviceCfg.exe 3`) refers to a compiled executable, not `dotnet run` — this is how to build that:

```
dotnet publish -c Release -o publish
```

This compiles the program into an actual `.exe` (plus `appsettings.json`, copied automatically) inside a new `publish` folder, so it can be run directly from the command line without typing `dotnet run` each time:

```
publish\DeviceCfg.exe
publish\DeviceCfg.exe 3
```

## How it handles bad data

- Missing (NULL) values print as `(not set)` instead of being left blank.
- Values that are technically valid but look wrong for that field — a negative `SlotIndex`, a `ScanRate` of 0, an `Enable` value that isn't 0 or 1, a disabled device, a missing `Trigger` — are flagged under "Warnings" instead of being silently accepted.
- Devices that share the same `TaskID` are reported as a neutral note, not an error — the table has no constraint requiring TaskID to be unique, so this is flagged as something worth a human's attention, not treated as invalid data.

## Troubleshooting

These are the actual error messages the program gives for each failure case, so you can tell what's happening if something doesn't work:

| Symptom | Likely cause | What to check |
|---|---|---|
| `NETSDK1045` or "does not support targeting .NET 10.0" during build | Your machine has an older .NET SDK than this project targets | Either install .NET SDK 10, or open `DeviceCfg.csproj` and change `<TargetFramework>net10.0</TargetFramework>` to the version you have installed (e.g. `net8.0`) — the code doesn't use anything specific to .NET 10 |
| `Could not connect to the database` | SQL Server isn't running, or the server name in `appsettings.json` is wrong | Confirm the SQL Server / SQLEXPRESS service is running; double-check the `Server=` value |
| `The 'ICAS_Test' database... does not appear to exist` | `01-setup.sql` was never run on this instance | Run `01-setup.sql` (see the task brief) |
| `The table 'dbo.t_DeviceCfg' does not exist` | The database exists but the table/data wasn't created | Run `01-setup.sql` |
| `Config file not found: ...appsettings.json` | `appsettings.json` isn't next to the compiled program | If running via `dotnet publish`, confirm it copied into the `publish` folder; if running via `dotnet run`, confirm it's in the project root |
| `the device ID must be an integer` | A non-numeric value was passed as the argument | Pass a whole number, e.g. `dotnet run -- 3` |
| `no device found with EquipId = N` | That ID isn't in the table | Valid IDs in the sample data are 1–10 |

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | Entry point — parses arguments, wires everything together, catches errors |
| `DeviceConfig.cs` | The data model for one row, plus the data-quality warning logic |
| `DeviceRepository.cs` | All SQL Server access (parameterized queries only) |
| `DeviceConfigException.cs` | Custom exception for expected failure cases |
| `ConfigLoader.cs` | Reads the connection string out of `appsettings.json` |
| `DeviceTablePrinter.cs` | Console output formatting |
