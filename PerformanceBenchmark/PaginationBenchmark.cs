using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BLite.Core.Query;
using LiteDB;

namespace PerformanceBenchmark;

/// <summary>
/// Pagination Performance Benchmark: BLite vs LiteDB.
/// Benchmark Categories:
/// Paginate-NoSort: Offset-based pagination without ORDER BY (tests the pushdown efficiency of Skip+Take)
/// Paginate-Sorted — Pagination with ORDER BY (forces full-table or indexed sorting; tests sorting overhead)
/// Paginate-Filtered — WHERE clause + offset-based pagination (tests index-assisted range scans)
/// Paginate-LargePage — Large-page-number test (Take 200) (tests data materialization throughput)
/// </summary>
[SimpleJob(2, 5, 10, id: "FastAndAccurate")]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[JsonExporterAttribute.Full]
[Orderer(SummaryOrderPolicy.Declared)]
public class PaginationBenchmark
{
    private BenchmarkBLiteDbContext _bliteDb = null!;
    private string _bliteDbPath = null!;
    private ILiteCollection<BenchmarkPhotoPo> _liteCollection = null!;
    private LiteDatabase _liteDb = null!;
    private string _liteDbPath = null!;
    private List<string> _sampleSourceIds = null!;

    [Params(50)]
    public int TotalFolders;

    [Params(10_000)]
    public int TotalPhotos;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine("=> Initializing pagination benchmark environment ({TotalPhotos} records)...");
        var (photos, sourceIds, _) = DataGenerator.Generate(TotalPhotos, TotalFolders);
        _sampleSourceIds = sourceIds;

        var tempDir = Path.Combine(Path.GetTempPath(), "DatabaseBenchmark");
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        _liteDbPath = Path.Combine(tempDir, $"litedb_paging_{Guid.NewGuid()}.db");
        _bliteDbPath = Path.Combine(tempDir, $"blitedb_paging_{Guid.NewGuid()}.db");

        // ── LiteDB setup ──
        _liteDb = new LiteDatabase($"Filename={_liteDbPath};Connection=Direct");
        _liteCollection = _liteDb.GetCollection<BenchmarkPhotoPo>("Photos");
        _liteCollection.EnsureIndex(x => x.Id, true);
        _liteCollection.EnsureIndex(x => x.SourceId);
        _liteCollection.EnsureIndex(x => x.FilePath, true);
        _liteCollection.EnsureIndex(x => x.DateTaken);
        _liteCollection.EnsureIndex("idx_Date_Id", "[$.DateTaken, $.Id]");

        _liteDb.BeginTrans();
        _liteCollection.InsertBulk(photos);
        _liteDb.Commit();

        // ── BLite setup ──
        _bliteDb = new BenchmarkBLiteDbContext(_bliteDbPath);
        _bliteDb.Photos.InsertBulkAsync(photos).GetAwaiter().GetResult();
        _bliteDb.SaveChangesAsync().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _liteDb?.Dispose();
        _bliteDb?.Dispose();
    }

    // =====================================================
    // Category 1: Paginate-NoSort (Unsorted Pagination)            
    // =====================================================
    [Benchmark(Baseline = true, Description = "LiteDB: NoSort")]
    [Arguments(0, 20)]
    [Arguments(980, 20)]
    [Arguments(4980, 20)]
    [Arguments(9980, 20)]
    [BenchmarkCategory("1_Paginate-NoSort")]
    public List<BenchmarkPhotoPo> LiteDB_NoSort(int skip, int take)
    {
        return _liteCollection.Find(Query.All(), skip, take).ToList();
    }

    [Benchmark(Description = "BLite: NoSort")]
    [Arguments(0, 20)]
    [Arguments(980, 20)]
    [Arguments(4980, 20)]
    [Arguments(9980, 20)]
    [BenchmarkCategory("1_Paginate-NoSort")]
    public async Task<List<BenchmarkPhotoPo>> BLite_NoSort(int skip, int take)
    {
        return await _bliteDb.Photos.AsQueryable().Skip(skip).Take(take).ToListAsync();
    }

    // =====================================================
    // Category 2: Paginate-Sorted (Single-field sorted pagination)                
    // =====================================================
    [Benchmark(Baseline = true, Description = "LiteDB: Sorted By DateTaken DESC")]
    [Arguments(0, 20)]
    [Arguments(980, 20)]
    [Arguments(4980, 20)]
    [Arguments(9980, 20)]
    [BenchmarkCategory("2_Paginate-Sorted")]
    public List<BenchmarkPhotoPo> LiteDB_Sorted(int skip, int take)
    {
        return _liteCollection.Query().OrderByDescending(x => x.DateTaken).Skip(skip).Limit(take).ToEnumerable().ToList();
    }

    [Benchmark(Description = "BLite: Sorted By DateTaken DESC")]
    [Arguments(0, 20)]
    [Arguments(980, 20)]
    [Arguments(4980, 20)]
    [Arguments(9980, 20)]
    [BenchmarkCategory("2_Paginate-Sorted")]
    public async Task<List<BenchmarkPhotoPo>> BLite_Sorted(int skip, int take)
    {
        return await _bliteDb.Photos.AsQueryable().OrderByDescending(x => x.DateTaken).Skip(skip).Take(take).ToListAsync();
    }

    // =====================================================
    // Category 3: Paginate-MultiSorted (Multi-field combined sorting)      
    // =====================================================
    [Benchmark(Baseline = true, Description = "LiteDB: MultiSorted DateTaken DESC, Id DESC")]
    [Arguments(0, 20)]
    [Arguments(980, 20)]
    [Arguments(4980, 20)]
    [Arguments(9980, 20)]
    [BenchmarkCategory("3_Paginate-MultiSorted")]
    public List<BenchmarkPhotoPo> LiteDB_MultiSorted(int skip, int take)
    {
        return _liteCollection.Query().OrderByDescending("[$.DateTaken, $.Id]").Skip(skip).Limit(take).ToEnumerable().ToList();
    }

    [Benchmark(Description = "BLite: MultiSorted DateTaken DESC, Id DESC")]
    [Arguments(0, 20)]
    [Arguments(980, 20)]
    [Arguments(4980, 20)]
    [Arguments(9980, 20)]
    [BenchmarkCategory("3_Paginate-MultiSorted")]
    public async Task<List<BenchmarkPhotoPo>> BLite_MultiSorted(int skip, int take)
    {
        return await _bliteDb.Photos.AsQueryable().OrderByDescending(x => x.DateTaken).ThenByDescending(x => x.Id).Skip(skip).Take(take).ToListAsync();
    }

    // =====================================================
    // Category 4: Paginate-Filtered (Filter then paginate)           
    // =====================================================
    [Benchmark(Baseline = true, Description = "LiteDB: Filtered By SourceId")]
    [Arguments(0, 20)]
    [Arguments(80, 20)]
    [Arguments(180, 20)]
    [BenchmarkCategory("4_Paginate-Filtered")]
    public List<BenchmarkPhotoPo> LiteDB_Filtered(int skip, int take)
    {
        var sid = _sampleSourceIds[0];
        return _liteCollection.Find(x => x.SourceId == sid, skip, take).ToList();
    }

    [Benchmark(Description = "BLite: Filtered By SourceId")]
    [Arguments(0, 20)]
    [Arguments(80, 20)]
    [Arguments(180, 20)]
    [BenchmarkCategory("4_Paginate-Filtered")]
    public async Task<List<BenchmarkPhotoPo>> BLite_Filtered(int skip, int take)
    {
        var sid = _sampleSourceIds[0];
        return await _bliteDb.Photos.AsQueryable().Where(x => x.SourceId == sid).Skip(skip).Take(take).ToListAsync();
    }

    // =====================================================
    // Category 5: Paginate-LargePage (Batch pagination read)           
    // =====================================================
    [Benchmark(Baseline = true, Description = "LiteDB: LargePage (200 items)")]
    [Arguments(0, 200)]
    [Arguments(9800, 200)]
    [BenchmarkCategory("5_Paginate-LargePage")]
    public List<BenchmarkPhotoPo> LiteDB_LargePage(int skip, int take)
    {
        return _liteCollection.Find(Query.All(), skip, take).ToList();
    }

    [Benchmark(Description = "BLite: LargePage (200 items)")]
    [Arguments(0, 200)]
    [Arguments(9800, 200)]
    [BenchmarkCategory("5_Paginate-LargePage")]
    public async Task<List<BenchmarkPhotoPo>> BLite_LargePage(int skip, int take)
    {
        return await _bliteDb.Photos.AsQueryable().Skip(skip).Take(take).ToListAsync();
    }

    // =====================================================
    // Category 6: Count-Operations (Record Statistics)           
    // =====================================================
    [Benchmark(Baseline = true, Description = "LiteDB: Count All (Total records)")]
    [BenchmarkCategory("6_Count-Operations")]
    public int LiteDB_Count_All()
    {
        // Should be extremely fast (O(1) read of collection head metadata)
        return _liteCollection.Count();
    }

    [Benchmark(Description = "LiteDB: Count All Via LINQ (Total records)")]
    [BenchmarkCategory("6_Count-Operations")]
    public int LiteDB_Count_All_Via_LINQ()
    {
        // Should be extremely fast (O(1) read of collection head metadata)
        return _liteCollection.Query().Count();
    }

    [Benchmark(Description = "BLite: Count All (Total records)")]
    [BenchmarkCategory("6_Count-Operations")]
    public async Task<int> BLite_Count_All()
    {
        return await _bliteDb.Photos.CountAsync();
    }

    [Benchmark(Description = "BLite: Count All Via LINQ (Total records))")]
    [BenchmarkCategory("6_Count-Operations")]
    public async Task<int> BLite_Count_All_Via_LINQ()
    {
        return await _bliteDb.Photos.AsQueryable().CountAsync();
    }

    [Benchmark(Description = "LiteDB: Count Filtered (Index assisted)")]
    [BenchmarkCategory("6_Count-Operations")]
    public int LiteDB_Count_Filtered()
    {
        // Should be fast (O(log N) B-Tree range count, without reading the original documents)
        var sid = _sampleSourceIds[0];
        return _liteCollection.Count(x => x.SourceId == sid);
    }

    [Benchmark(Description = "BLite: Count Filtered (Index assisted)")]
    [BenchmarkCategory("6_Count-Operations")]
    public async Task<int> BLite_Count_Filtered()
    {
        var sid = _sampleSourceIds[0];
        return await _bliteDb.Photos.AsQueryable().Where(x => x.SourceId == sid).CountAsync();
    }
}