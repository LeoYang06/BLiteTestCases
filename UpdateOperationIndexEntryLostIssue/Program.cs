using BLite.Core.Query;

namespace IndexUpdateErrorIssue;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"blite_idx_repro_{Guid.NewGuid()}.db");
        if (File.Exists(dbPath)) File.Delete(dbPath);

        var db = new ReproDbContext(dbPath);

        Console.WriteLine("==========================================================");
        Console.WriteLine(" SCENARIO 1: Filter Index Loss (Where/Contains)");
        Console.WriteLine("==========================================================");

        var deviceId = "DEV_001";
        var deviceIds = new HashSet<string> { deviceId };

        var folder = new LinkedFolder { Id = "F_001", DeviceId = deviceId, Name = "ShortName" };
        await db.LinkedFolders.InsertAsync(folder);

        // Non-in-place update
        folder.Name = new string('A', 500);
        await db.LinkedFolders.UpdateAsync(folder);

        var s1IndexResult = await db.LinkedFolders.AsQueryable().Where(x => deviceIds.Contains(x.DeviceId)).ToListAsync();
        var s1ScanResult = await db.LinkedFolders.AsQueryable().Where(x => deviceIds.Contains(x.DeviceId.ToString())).ToListAsync();

        Console.ForegroundColor = s1IndexResult.Count == 1 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"[Index Query] Found: {s1IndexResult.Count} (Expected: 1) <- THE BUG");
        Console.ResetColor();
        Console.WriteLine($"[Scan Query]  Found: {s1ScanResult.Count} (Expected: 1) <- WORKAROUND");


        Console.WriteLine("\n==========================================================");
        Console.WriteLine(" SCENARIO 2: DateTaken Index Loss");
        Console.WriteLine("==========================================================");

        var photo = new Photo { Id = Guid.NewGuid(), SourceId = "SRC_OLD", DateTaken = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        await db.Photos.InsertAsync(photo);

        // Modify SourceId to force reallocation of physical space while keeping DateTaken completely unchanged
        photo.SourceId = new string('B', 500);
        await db.Photos.UpdateAsync(photo);

        var totalCount = await db.Photos.AsQueryable().CountAsync();

        // BUG Verification: Force use of the DateTaken secondary index!
        var indexQuery = await db.Photos.AsQueryable().Where(x => x.DateTaken == photo.DateTaken).ToListAsync();

        Console.WriteLine($"[CountAsync]  Total: {totalCount} (Expected: 1)");
        Console.ForegroundColor = indexQuery.Count == 1 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"[Index Query] Found: {indexQuery.Count} (Expected: 1) <- THE BUG (Index Entry Permanently Lost!)");
        Console.ResetColor();

        db.Dispose();
        if (File.Exists(dbPath)) File.Delete(dbPath);
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}