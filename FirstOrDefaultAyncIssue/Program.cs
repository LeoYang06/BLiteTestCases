using BLite.Core.Query;

namespace FirstOrDefaultAyncIssue;

internal class Program
{
    private static async Task Main(string[] args)
    {
        using var db = new DeviceDbContext("global.db");
        const string targetId = "L0JRaD_5";
        for (var i = 1; i <= 3; i++)
        {
            var device = i switch
            {
                1 => await db.Devices.AsQueryable().FirstOrDefaultAsync(x => x.Id == targetId),
                2 => await db.Devices.AsQueryable().FirstOrDefaultAsync(x => x.Id.ToString() == targetId),
                3 => await db.Devices.FindByIdAsync(targetId),
                _ => null
            };

            Console.WriteLine($"\r\n{i}:");
            Console.WriteLine(device != null ? $"Found device: Id={device.Id}, SearchIndexId={device.Identifier}" : "Device not found.");
        }
    }
}