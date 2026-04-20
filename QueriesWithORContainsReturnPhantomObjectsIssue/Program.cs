using BLite.Core.Query;

namespace QueriesWithORContainsReturnPhantomObjectsIssue;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var dbPath = "repro_shared_page_bug.blite";
        if (File.Exists(dbPath)) File.Delete(dbPath); // Clean old db

        Console.WriteLine("Initializing BLite Database...");
        using var db = new PhotosDbContext(dbPath);

        // ==========================================
        // 3. Simulate AddRangeAsync in business logic, inserting into both main and related tables
        // ==========================================
        var recordCount = 710;
        Console.WriteLine($"Generating {recordCount} Photos and {recordCount} PhotoMetadata...");

        var photos = new List<PhotoPo>();
        var metadatas = new List<PhotoMetadataPo>();
        var sourceId = "4ZHgDWOz";

        for (var i = 0; i < recordCount; i++)
        {
            var photoId = Guid.NewGuid();
            photos.Add(new PhotoPo { Id = photoId, SourceId = sourceId, RelativePath = $"/images/img_{i}.jpg" });
            metadatas.Add(new PhotoMetadataPo { Id = Guid.NewGuid(), PhotoId = photoId, SourceId = sourceId, ExifData = "ISO 100" });
        }

        // BLite allocates physical pages, and these two types of data will be mixed in the same underlying data pages
        await db.Photos.InsertBulkAsync(photos);
        await db.PhotoMetadata.InsertBulkAsync(metadatas);

        // ==========================================
        // 4. Execute Test Cases
        // ==========================================
        Console.WriteLine("\n========== Running Query Tests ==========\n");

        // Test 1: CountAsync uses the primary index tree, accurately counting the Photos table
        var totalCount = await db.Photos.CountAsync();
        Console.WriteLine($"[Test 1] db.Photos.CountAsync() = {totalCount} (Expected: 710)");

        // Test 2: Single condition uses secondary index, accurate due to index isolation
        var indexCount = await db.Photos.AsQueryable().Where(x => x.SourceId == sourceId).CountAsync();
        Console.WriteLine($"[Test 2] Where(SourceId == '...').Count() = {indexCount} (Expected: 710)");

        // Test 3: Fatal Bug -> Triggers physical fallback scan!
        var idList = new[] { sourceId, "ANOTHER_ID" };
        var bugList = await db.Photos.AsQueryable().Where(x => idList.Contains(x.SourceId)).ToListAsync();

        Console.ForegroundColor = bugList.Count == 1420 ? ConsoleColor.Red : ConsoleColor.Green;
        Console.WriteLine($"[Test 3] Where(Contains).ToList().Count = {bugList.Count} ❌ (Expected: 710)");
        Console.ResetColor();

        // ==========================================
        // 5. Validate silent deserialization issue
        // Find data with empty RelativePath, proving they are actually PhotoMetadataPo disguised as PhotoPo
        // ==========================================
        var phantomPhotos = bugList.Where(x => string.IsNullOrEmpty(x.RelativePath)).ToList();
        if (phantomPhotos.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nBUG VERIFIED: Found {phantomPhotos.Count} phantom objects!");
            Console.WriteLine("These are PhotoMetadataPo documents mistakenly evaluated and deserialized as PhotoPo because they share the 'SourceId' BSON field and physical data pages!");
            Console.WriteLine("Sample Phantom Object:");
            Console.WriteLine($" - Id: {phantomPhotos.First().Id}");
            Console.WriteLine($" - SourceId: {phantomPhotos.First().SourceId}");
            Console.WriteLine($" - RelativePath: '{phantomPhotos.First().RelativePath}' (Notice this is the default empty string!)");
            Console.ResetColor();
        }
    }
}