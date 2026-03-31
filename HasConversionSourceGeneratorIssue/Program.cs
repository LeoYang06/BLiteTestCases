using BLite.Core.Query;
using Bogus;
using NanoidDotNet;
using Yitter.IdGenerator;

namespace HasConversionSourceGeneratorIssue;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var idOptions = new IdGeneratorOptions(26);
        YitIdHelper.SetIdGenerator(idOptions);

        using var db = new DeviceDbContext("myapp.db");
        var devices = db.Devices;

        var testDevices = new Faker<DevicePo>().CustomInstantiator(f => new DevicePo
        {
            Id = Nanoid.Generate(size: 6),
            SearchIndexId = (ulong)YitIdHelper.NextId(),
            Name = f.Name.FullName(),
            Identifier = Nanoid.Generate(Nanoid.Alphabets.LowercaseLetters, 12),
            AddedAt = DateTime.Now
        });

        var targetId = (await db.Devices.AsQueryable().FirstOrDefaultAsync())?.Id;
        if (string.IsNullOrEmpty(targetId))
        {
            var transaction = await db.BeginTransactionAsync();
            for (var i = 0; i < 5; i++)
            {
                var device = testDevices.Generate();
                await devices.InsertAsync(device);
                targetId ??= device.Id;
            }

            await transaction.CommitAsync();
        }

        var firstDevice = await devices.AsQueryable().FirstOrDefaultAsync(x => x.Id == targetId);
        Console.WriteLine(firstDevice != null ? $"Found device: Id={firstDevice.Id}, SearchIndexId={firstDevice.SearchIndexId}, Name={firstDevice.Name}" : "Device not found.");
    }
}