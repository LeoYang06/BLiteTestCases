using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BLite.Core.Query;
using LiteDB;

namespace PerformanceBenchmark;

// LaunchCount=2, run 2 processes, take the second result, the cold start effect basically disappears
[SimpleJob(2, 5, 10, id: "FastAndAccurate")]
// [ShortRunJob]
// [InProcess]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[JsonExporterAttribute.Full]
public class ReadBenchmark
{
    private BenchmarkBLiteDbContext _bliteDb = null!;
    private string _bliteDbPath = null!;
    private ILiteCollection<BenchmarkPhotoPo> _liteCollection = null!;

    private LiteDatabase _liteDb = null!;
    private string _liteDbPath = null!;
    private List<string> _sampleFilePaths = null!;

    private List<string> _sampleSourceIds = null!;

    [Params(20)]
    public int TotalFolders;

    [Params(5_000)]
    public int TotalPhotos;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine("=> Initializing pagination benchmark environment ({TotalPhotos} records)...");
        var (photos, sourceIds, filePaths) = DataGenerator.Generate(TotalPhotos, TotalFolders);
        _sampleSourceIds = sourceIds;
        _sampleFilePaths = filePaths;

        var tempDir = Path.Combine(Path.GetTempPath(), "DatabaseBenchmark");
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        _liteDbPath = Path.Combine(tempDir, $"litedb_read_{Guid.NewGuid()}.db");
        _bliteDbPath = Path.Combine(tempDir, $"blitedb_read_{Guid.NewGuid()}.db");

        _liteDb = new LiteDatabase($"Filename={_liteDbPath};Connection=Direct");
        _liteCollection = _liteDb.GetCollection<BenchmarkPhotoPo>("Photos");
        _liteCollection.EnsureIndex(x => x.Id, true);
        _liteCollection.EnsureIndex(x => x.SourceId);
        _liteCollection.EnsureIndex(x => x.FilePath, true);
        _liteDb.BeginTrans();
        _liteCollection.InsertBulk(photos);
        _liteDb.Commit();

        _bliteDb = new BenchmarkBLiteDbContext(_bliteDbPath);
        _bliteDb.Photos.InsertBulkAsync(photos).GetAwaiter().GetResult();
        _bliteDb.SaveChangesAsync().GetAwaiter().GetResult();

        Console.WriteLine("=> After the database is initialized, start executing the query Benchmark...");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _liteDb?.Dispose();
        _bliteDb?.Dispose();
    }

    // ==========================================
    // 1. Test SourceId index query (1-to-N)
    // ==========================================

    [Benchmark(Baseline = true, Description = "LiteDB: Query by SourceId (1-to-N)")]
    [BenchmarkCategory("1-to-N")]
    public void LiteDB_QueryBySourceId()
    {
        foreach (var sourceId in _sampleSourceIds)
        {
            var result = _liteCollection.Find(x => x.SourceId == sourceId).ToList();
        }
    }

    [Benchmark(Description = "BLite: Query by SourceId (1-to-N)")]
    [BenchmarkCategory("1-to-N")]
    public async Task BLite_QueryBySourceId()
    {
        foreach (var sourceId in _sampleSourceIds)
        {
            var result = await _bliteDb.Photos.FindAsync(x => x.SourceId == sourceId).ToListAsync();
        }
    }

    [Benchmark(Description = "BLite: Query by SourceId via LINQ (1-to-N)")]
    [BenchmarkCategory("1-to-N")]
    public async Task BLite_QueryBySourceIdViaLINQ()
    {
        foreach (var sourceId in _sampleSourceIds)
        {
            var result = await _bliteDb.Photos.AsQueryable().Where(x => x.SourceId == sourceId).ToListAsync();
        }
    }

    // ==========================================
    // 2. Test FilePath exact match query (1-to-1)
    // ==========================================

    [Benchmark(Baseline = true, Description = "LiteDB: Query by FilePath (1-to-1)")]
    [BenchmarkCategory("1-to-1")]
    public void LiteDB_QueryByFilePath()
    {
        foreach (var filePath in _sampleFilePaths)
        {
            var result = _liteCollection.FindOne(x => x.FilePath == filePath);
        }
    }

    [Benchmark(Description = "BLite: Query by FilePath (1-to-1)")]
    [BenchmarkCategory("1-to-1")]
    public async Task BLite_QueryByFilePath()
    {
        foreach (var filePath in _sampleFilePaths)
        {
            var result = await _bliteDb.Photos.FindOneAsync(x => x.FilePath == filePath);
        }
    }

    [Benchmark(Description = "BLite: Query by FilePath Via FirstOrDefaultAsync (1-to-1)")]
    [BenchmarkCategory("1-to-1")]
    public async Task BLite_QueryByFilePathViaFirstOrDefault()
    {
        foreach (var filePath in _sampleFilePaths)
        {
            var result = await _bliteDb.Photos.AsQueryable().FirstOrDefaultAsync(x => x.FilePath == filePath);
        }
    }

    [Benchmark(Description = "BLite: Query by FilePath Via FindAsync & FirstOrDefaultAsync (1-to-1)")]
    [BenchmarkCategory("1-to-1")]
    public async Task BLite_QueryByFilePathViaFindAndFirstOrDefault()
    {
        foreach (var filePath in _sampleFilePaths)
        {
            var result = await _bliteDb.Photos.FindAsync(x => x.FilePath == filePath).FirstOrDefaultAsync();
        }
    }

    [Benchmark(Description = "BLite: Query by FilePath via LINQ (1-to-1)")]
    [BenchmarkCategory("1-to-1")]
    public async Task BLite_QueryByFilePathViaLINQ()
    {
        foreach (var filePath in _sampleFilePaths)
        {
            var result = await _bliteDb.Photos.AsQueryable().Where(x => x.FilePath == filePath).FirstOrDefaultAsync();
        }
    }
}