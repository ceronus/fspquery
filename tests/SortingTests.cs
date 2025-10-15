using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FspQuery;

[TestClass]
public class SortingTests
{
    public class PageableQueryOptions : IFspQueryOptions
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public ListSortDirection SortDirection { get; set; }
        public string? SortPropertyName { get; set; }
        public HashSet<FilterCondition>? Filters { get; set; }
    }

    private class Computer
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("mainboard")] public Mainboard? Mainboard { get; set; }
    }

    private class Mainboard
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("cpu")] public CentralProcessingUnit? CentralProcessingUnit { get; set; }
    }

    private class CentralProcessingUnit
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("cores")] public int NumberOfCores { get; set; }
    }

    private static JsonSerializerOptions? _options;
    private static ObjectIndexer? _indexer;
    private static FspQueryLogic? _logic;

    [ClassInitialize]
#pragma warning disable IDE0060 // Remove unused parameter
    public static void InitializeTestClass(TestContext context)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _options = new()
        {
            MaxDepth = 2048,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _indexer = new(_options);
        _logic = new(_indexer);
    }

    [TestMethod]
    public void OrderByAscendingIsDefault()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Computer>? unsorted =
        [
            // not in sequential order
            new() { Name = "PC-03" },
            new() { Name = "PC-02" },
            new() { Name = "PC-01" },
            new() { Name = "PC-05" },
            new() { Name = "PC-04" },
        ];

        PageableQueryOptions options = new()
        {
            SortPropertyName = "name",
        };

        List<Computer> sorted = [.. _logic.ApplySorting(unsorted.AsQueryable(), options)];

        Assert.AreEqual("PC-01", sorted[0].Name);
        Assert.AreEqual("PC-02", sorted[1].Name);
        Assert.AreEqual("PC-03", sorted[2].Name);
        Assert.AreEqual("PC-04", sorted[3].Name);
        Assert.AreEqual("PC-05", sorted[4].Name);
    }


    [TestMethod]
    public void OrderByAscendingStringOnFirstLevel()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Computer>? unsorted =
        [
            // not in sequential order
            new() { Name = "PC-03" },
            new() { Name = "PC-02" },
            new() { Name = "PC-01" },
            new() { Name = "PC-05" },
            new() { Name = "PC-04" },
        ];

        PageableQueryOptions options = new()
        {
            SortPropertyName = "name",
            SortDirection = ListSortDirection.Ascending
        };

        List<Computer> sorted = [.. _logic.ApplySorting(unsorted.AsQueryable(), options)];

        Assert.AreEqual("PC-01", sorted[0].Name);
        Assert.AreEqual("PC-02", sorted[1].Name);
        Assert.AreEqual("PC-03", sorted[2].Name);
        Assert.AreEqual("PC-04", sorted[3].Name);
        Assert.AreEqual("PC-05", sorted[4].Name);
    }

    [TestMethod]
    public void OrderByDescendingStringOnFirstLevel()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Computer>? unsorted =
        [
            // not in sequential order
            new() { Name = "PC-03" },
            new() { Name = "PC-02" },
            new() { Name = "PC-01" },
            new() { Name = "PC-05" },
            new() { Name = "PC-04" },
        ];

        PageableQueryOptions options = new()
        {
            SortPropertyName = "name",
            SortDirection = ListSortDirection.Descending
        };

        List<Computer> sorted = [.. _logic.ApplySorting(unsorted.AsQueryable(), options)];

        Assert.AreEqual("PC-05", sorted[0].Name);
        Assert.AreEqual("PC-04", sorted[1].Name);
        Assert.AreEqual("PC-03", sorted[2].Name);
        Assert.AreEqual("PC-02", sorted[3].Name);
        Assert.AreEqual("PC-01", sorted[4].Name);
    }

    [TestMethod]
    public void OrderByAscendingStringOnSecondLevel()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Computer>? unsorted =
        [
            // not in sequential order
            new() { Name = "PC-03", Mainboard = new() { Name = "C" } },
            new() { Name = "PC-02", Mainboard = new() { Name = "B" } },
            new() { Name = "PC-01", Mainboard = new() { Name = "A" } },
            new() { Name = "PC-05", Mainboard = new() { Name = "E" } },
            new() { Name = "PC-04", Mainboard = new() { Name = "D" } },
        ];

        PageableQueryOptions options = new()
        {
            SortPropertyName = "mainboard.name",
            SortDirection = ListSortDirection.Ascending
        };

        List<Computer> sorted = [.. _logic.ApplySorting(unsorted.AsQueryable(), options)];

        Assert.AreEqual("A", sorted[0].Mainboard?.Name);
        Assert.AreEqual("B", sorted[1].Mainboard?.Name);
        Assert.AreEqual("C", sorted[2].Mainboard?.Name);
        Assert.AreEqual("D", sorted[3].Mainboard?.Name);
        Assert.AreEqual("E", sorted[4].Mainboard?.Name);
    }

    [TestMethod]
    public void OrderByAscendingStringOnThirdLevel()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Computer>? unsorted =
        [
            // not in sequential order
            new() { Name = "PC-03", Mainboard = new() { Name = "MB-03", CentralProcessingUnit = new() { Name = "Quad Core", NumberOfCores = 4 } } },
            new() { Name = "PC-02", Mainboard = new() { Name = "MB-02", CentralProcessingUnit = new() { Name = "Dual Core", NumberOfCores = 2 } } },
            new() { Name = "PC-01", Mainboard = new() { Name = "MB-01", CentralProcessingUnit = new() { Name = "Single Core", NumberOfCores = 1 } } },
            new() { Name = "PC-05", Mainboard = new() { Name = "MB-05", CentralProcessingUnit = new() { Name = "Hexa Core", NumberOfCores = 16 } } },
            new() { Name = "PC-04", Mainboard = new() { Name = "MB-04", CentralProcessingUnit = new() { Name = "Octa Core", NumberOfCores = 8 } } },
        ];

        PageableQueryOptions getCustomersQuery = new()
        {
            SortPropertyName = "mainboard.cpu.name",
            SortDirection = ListSortDirection.Ascending
        };

        List<Computer> sorted = [.. _logic.ApplySorting(unsorted.AsQueryable(), getCustomersQuery)];

        Assert.AreEqual("Dual Core", sorted[0].Mainboard?.CentralProcessingUnit?.Name);
        Assert.AreEqual("Hexa Core", sorted[1].Mainboard?.CentralProcessingUnit?.Name);
        Assert.AreEqual("Octa Core", sorted[2].Mainboard?.CentralProcessingUnit?.Name);
        Assert.AreEqual("Quad Core", sorted[3].Mainboard?.CentralProcessingUnit?.Name);
        Assert.AreEqual("Single Core", sorted[4].Mainboard?.CentralProcessingUnit?.Name);
    }

    [TestMethod]
    public void OrderByAscendingNumericOnThirdLevel()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Computer>? unsorted =
        [
            // not in sequential order
            new() { Name = "PC-03", Mainboard = new() { Name = "MB-03", CentralProcessingUnit = new() { Name = "Quad Core", NumberOfCores = 4 } } },
            new() { Name = "PC-02", Mainboard = new() { Name = "MB-02", CentralProcessingUnit = new() { Name = "Dual Core", NumberOfCores = 2 } } },
            new() { Name = "PC-01", Mainboard = new() { Name = "MB-01", CentralProcessingUnit = new() { Name = "Single Core", NumberOfCores = 1 } } },
            new() { Name = "PC-05", Mainboard = new() { Name = "MB-05", CentralProcessingUnit = new() { Name = "Hexa Core", NumberOfCores = 16 } } },
            new() { Name = "PC-04", Mainboard = new() { Name = "MB-04", CentralProcessingUnit = new() { Name = "Octa Core", NumberOfCores = 8 } } },
        ];

        PageableQueryOptions query = new()
        {
            SortPropertyName = "mainboard.cpu.cores",
            SortDirection = ListSortDirection.Ascending
        };

        List<Computer> sorted = [.. _logic.ApplySorting(unsorted.AsQueryable(), query)];

        Assert.AreEqual(1, sorted[0].Mainboard?.CentralProcessingUnit?.NumberOfCores);
        Assert.AreEqual(2, sorted[1].Mainboard?.CentralProcessingUnit?.NumberOfCores);
        Assert.AreEqual(4, sorted[2].Mainboard?.CentralProcessingUnit?.NumberOfCores);
        Assert.AreEqual(8, sorted[3].Mainboard?.CentralProcessingUnit?.NumberOfCores);
        Assert.AreEqual(16, sorted[4].Mainboard?.CentralProcessingUnit?.NumberOfCores);
    }
}
