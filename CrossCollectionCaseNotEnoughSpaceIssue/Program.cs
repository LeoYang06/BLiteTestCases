namespace CrossCollectionCaseNotEnoughSpaceIssue;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"blite_repro_{Guid.NewGuid()}.db");
        if (File.Exists(dbPath)) File.Delete(dbPath);

        try
        {
            // ====================================================================
            // PHASE 1: Warm up the database to allocate shared physical pages.
            // Insert documents to force the engine to allocate physical data pages that will later be shared in Single-File mode.
            // ====================================================================
            using (var db = new TestDbContext(dbPath))
            {
                var docsA = new List<DocA>();
                for (var i = 0; i < 50; i++) docsA.Add(new DocA());

                await db.ColA.InsertBulkAsync(docsA);
                await db.SaveChangesAsync();
                Console.WriteLine("=>[Phase 1] Database initialized with shared physical pages.");
            }

            // ====================================================================
            // PHASE 2: Trigger the Cross-Collection FSI Desync
            // Restart the DB. Both ColA and ColB will scan the physical pages and load the EXACT SAME free space data into their ISOLATED _fsi caches.
            // ====================================================================
            using (var db = new TestDbContext(dbPath))
            {
                Console.WriteLine("=> [Phase 2] DB Restarted. ColA & ColB loaded identical FSIs.");

                var docsA = new List<DocA>();
                for (var i = 0; i < 100; i++) docsA.Add(new DocA());

                var docsB = new List<DocB>();
                for (var i = 0; i < 100; i++) docsB.Add(new DocB());

                // ColA inserts data, consuming physical space and updating ColA's own FSI.
                Console.WriteLine("=> Executing ColA.InsertBulkAsync...");
                await db.ColA.InsertBulkAsync(docsA);

                // ColB attempts to insert. Its FSI is unaware of ColA's consumption,routes the document to a full page, and fatally crashes!
                Console.WriteLine("=> Executing ColB.InsertBulkAsync...");
                await db.ColB.InsertBulkAsync(docsB);
                await db.SaveChangesAsync();

                Console.WriteLine("=> If you see this, the bug didn't trigger.");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Not enough space"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[BINGO! BUG REPRODUCED SUCCESSFULLY!]");
            Console.WriteLine(ex.Message);
            // Expected Output: Need ~580, Have ~120 | FSI=~3600
            Console.ResetColor();
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}