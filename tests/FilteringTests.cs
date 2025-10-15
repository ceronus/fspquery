using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FspQuery;

[TestClass]
public class FilteringTests
{
    public class PageableQuery : IFspQueryOptions
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public ListSortDirection SortDirection { get; set; }
        public string? SortPropertyName { get; set; }
        public HashSet<FilterCondition>? Filters { get; set; }
    }

    private class Customer
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tier")]
        public int? Tier { get; set; }

        [JsonPropertyName("lovedOne")]
        public Animal? Pet { get; set; }
    }

    private class Animal
    {
        [JsonPropertyName("nickname")]
        public string? Name { get; set; }

        [JsonPropertyName("ageInYears")]
        public int Age { get; set; }

        [JsonPropertyName("numberOfTimesHugged")]
        public int? Points { get; set; }
    }

    private static JsonSerializerOptions? _options;
    private static ObjectIndexer? _indexer;
    private static FspQueryLogic? _logic;

    [ClassInitialize]
#pragma warning disable IDE0060 // Remove unused parameter
    public static void Initialize(TestContext testContext)
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
    public void UseMultipleFilters()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        const string customerName = "john";
        const int customerTier = 1;
        const string petName = "scratch";
        const int petAge = 2;

        List<Customer>? customers =
        [
            new() { Name = customerName, Tier = customerTier, Pet = new() { Name = petName, Age = petAge } },
            new() { Name = "jane", Tier = 2, Pet = new() { Name = "meowie", Age = 7 } },
        ];

        HashSet<FilterCondition> filters =
        [
            // note: property accessors uses dot notation
            FilterCondition.CreateNotContains("name", "r"),
            FilterCondition.CreateEndsWith("lovedOne.nickname", "h"),
            FilterCondition.CreateEquals("tier", 1),
            FilterCondition.CreateGreaterThan("lovedOne.ageInYears", 1)
        ];

        PageableQuery getCustomersQuery = new()
        {
            Filters = filters
        };

        IQueryable<Customer> query = customers.AsQueryable();
        query = _logic.ApplyFiltering(query, getCustomersQuery);

        Assert.AreEqual(1, query.Count());

        Customer? customer = query?.First();
        Assert.IsNotNull(customer);
        Assert.AreEqual(customerName, customer.Name);
        Assert.AreEqual(customerTier, customer.Tier);
        Assert.IsNotNull(customer.Pet);

        Animal? pet = customer.Pet;
        Assert.IsNotNull(pet);
        Assert.AreEqual(petName, pet.Name);
        Assert.AreEqual(petAge, pet.Age);
    }

    [TestMethod]
    public void EqualsNullableInt32()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        const string customerName = "john";
        const int customerTier = 1;
        const string petName = "scratch";
        const int petAge = 2;

        List<Customer>? customers =
        [
            new() { Name = customerName, Tier = customerTier, Pet = new() { Name = petName, Age = petAge, Points = null } },
            new() { Name = "joseph", Tier = 2, Pet = new() { Name = "meowie", Age = 7, Points = 10_000 } },
        ];

        HashSet<FilterCondition> filterConditions =
        [
            FilterCondition.CreateEquals("lovedOne.numberOfTimesHugged", null)
        ];
        HashSet<FilterCondition> filters = filterConditions;

        PageableQuery getCustomersQuery = new()
        {
            Filters = filters
        };

        IQueryable<Customer> query = customers.AsQueryable();
        query = _logic.ApplyFiltering(query, getCustomersQuery);

        Assert.AreEqual(1, query.Count());

        Customer? customer = query?.First();
        Assert.IsNotNull(customer);
        Assert.AreEqual(customerName, customer.Name);
        Assert.AreEqual(customerTier, customer.Tier);
        Assert.IsNotNull(customer.Pet);

        Animal? pet = customer.Pet;
        Assert.IsNotNull(pet);
        Assert.AreEqual(petName, pet.Name);
        Assert.AreEqual(petAge, pet.Age);
    }

    [TestMethod]
    public void GreaterThanOrEqualNullableInt()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Customer>? customers =
        [
            new() { Name = "", Tier = null },
            new() { Name = "", Tier = 9 },
            new() { Name = "", Tier = 10 },
            new() { Name = "", Tier = 11 },
            new() { Name = "", Tier = 12 },
        ];

        PageableQuery getCustomersQuery = new()
        {
            Filters =
            [
                FilterCondition.CreateGreaterThanOrEqual("tier", 11)
            ]
        };

        IQueryable<Customer> query = customers.AsQueryable();
        query = _logic.ApplyFiltering(query, getCustomersQuery);

        Assert.AreEqual(2, query.Count());
    }

    [TestMethod]
    public void NotGreaterThanOrEqualNullableInt()
    {
        Assert.IsNotNull(_indexer);
        Assert.IsNotNull(_logic);

        List<Customer>? customers =
        [
            new() { Name = "", Tier = null },
            new() { Name = "", Tier = 9 },
            new() { Name = "", Tier = 10 },
            new() { Name = "", Tier = 11 },
            new() { Name = "", Tier = 12 },
        ];

        PageableQuery getCustomersQuery = new()
        {
            Filters =
            [
                FilterCondition.CreateNotGreaterThanOrEqual("tier", 11)
            ]
        };

        IQueryable<Customer> query = customers.AsQueryable();
        query = _logic.ApplyFiltering(query, getCustomersQuery);

        Assert.AreEqual(3, query.Count());
    }
}