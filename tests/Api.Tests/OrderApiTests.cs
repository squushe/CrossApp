using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.Contracts;
using Core;
using Core.Abstractions;
using Core.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Tests;

public sealed class OrderApiTests
{
    private static WebApplicationFactory<Program> Factory(string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(environment));

    private static SaveOrderRequest Request(string customer = "C-1", string name = "Кабель", int quantity = 2) =>
        new(customer, [new("P-1", name, 125.5m, quantity)]);

    private static async Task<OrderDto> Create(HttpClient client, SaveOrderRequest request)
    {
        using var response = await client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal($"/api/orders/{created.Id}", response.Headers.Location!.OriginalString);
        // Вимога додаткового завдання: GET за отриманим Location повертає 200.
        using var read = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        return created;
    }

    [Fact]
    public async Task Crud_ReturnsLocationPreservesCreationTimeAndDeletes()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        var created = await Create(client, Request());
        var path = $"/api/orders/{created.Id}";
        Assert.Equal(251m, created.Total);
        var total = await client.GetFromJsonAsync<OrderTotalDto>(path + "/total");
        Assert.Equal(251m, total!.Total);
        using var put = await client.PutAsJsonAsync(path, Request("C-2", quantity: 3));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = (await put.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(376.5m, updated.Total);
        Assert.Equal("C-2", updated.Customer);
        using var invalid = await client.PutAsJsonAsync(path, new { customer = "", lines = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var unchanged = (await client.GetFromJsonAsync<OrderDto>(path))!;
        Assert.Equal(updated.Total, unchanged.Total);
        Assert.Equal(updated.Customer, unchanged.Customer);
        using var delete = await client.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var missing = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var repeatedDelete = await client.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, repeatedDelete.StatusCode);
    }

    [Fact]
    public async Task Paging_FiltersBeforeCountingAndReturnsStableNonOverlappingPages()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        var first = await Create(client, Request("Alpha", "USB кабель"));
        var second = await Create(client, Request("Alpha", "Миша"));
        await Create(client, Request("Beta", "Інший товар"));
        var page1 = (await client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders?page=1&pageSize=1&search=alpha"))!;
        var page2 = (await client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders?page=2&pageSize=1&search=alpha"))!;
        Assert.Equal(2, page1.Total);
        Assert.Equal(2, page2.Total);
        Assert.Equal(first.Id, Assert.Single(page1.Items).Id);
        Assert.Equal(second.Id, Assert.Single(page2.Items).Id);
        var byProduct = (await client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders?search=usb"))!;
        Assert.Equal(first.Id, Assert.Single(byProduct.Items).Id);
        var beyond = (await client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders?page=2147483647&pageSize=100"))!;
        Assert.Equal(3, beyond.Total);
        Assert.Empty(beyond.Items);
        var noMatches = (await client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders?search=missing"))!;
        Assert.Equal(0, noMatches.Total);
        Assert.Empty(noMatches.Items);
    }

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("page=-1", "page")]
    [InlineData("pageSize=0", "pageSize")]
    [InlineData("pageSize=101", "pageSize")]
    public async Task InvalidPaging_ReturnsFieldErrors(string query, string key)
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/orders?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(key, out _));
    }

    [Theory]
    [InlineData("{}", "customer")]
    [InlineData("{\"customer\":\"C\",\"lines\":[]}", "lines")]
    [InlineData("{\"customer\":\"C\",\"lines\":[null]}", "lines[0]")]
    [InlineData("{\"customer\":\"C\",\"lines\":[{\"productId\":\"P\",\"name\":\"\",\"price\":1,\"quantity\":1}]}", "lines[0].name")]
    [InlineData("{\"customer\":\"C\",\"lines\":[{\"productId\":\"P\",\"name\":\"X\",\"price\":0,\"quantity\":1}]}", "lines[0].price")]
    public async Task ValidationFilter_ReturnsCamelCaseFieldErrorsAndDoesNotWrite(string json, string key)
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/api/orders", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(key, out _));
        Assert.Equal(0, (await client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/orders"))!.Total);
    }

    [Fact]
    public async Task ValidationFilter_RejectsOverlongFieldsAndTotalOverflow()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        using var tooLong = await client.PostAsJsonAsync("/api/orders", Request(new string('x', 101), new string('x', 201)));
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        var errors = (await tooLong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("customer", out _));
        Assert.True(errors.TryGetProperty("lines[0].name", out _));
        using var overflow = await client.PostAsJsonAsync("/api/orders", new SaveOrderRequest("C", [new("P", "X", decimal.MaxValue, 2)]));
        Assert.Equal(HttpStatusCode.BadRequest, overflow.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"customer\":\"C\",\"lines\":[{\"quantity\":\"abc\"}]}")]
    public async Task InvalidJson_ReturnsProblemWithoutInternalDetails(string json)
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/api/orders", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", text);
        Assert.DoesNotContain("/Users/", text);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.Conflict)]
    [InlineData(false, HttpStatusCode.InternalServerError)]
    public async Task ExceptionHandlers_ReturnExpectedStatusAndHideExceptionDetails(bool domain, HttpStatusCode status)
    {
        using var factory = Factory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAsyncOrderStore>();
            services.AddSingleton<IAsyncOrderStore>(new ThrowingStore(domain));
        }));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/orders");
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PRIVATE", text);
        Assert.DoesNotContain("Exception", text);
        Assert.DoesNotContain("StackTrace", text);
    }

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task Documentation_IsOnlyAvailableInDevelopment(string environment, HttpStatusCode status)
    {
        using var factory = Factory(environment);
        using var client = factory.CreateClient();
        using var document = await client.GetAsync("/openapi/v1.json");
        using var scalar = await client.GetAsync("/scalar/v1");
        Assert.Equal(status, document.StatusCode);
        Assert.Equal(status, scalar.StatusCode);
        if (status == HttpStatusCode.OK)
        {
            var spec = await document.Content.ReadFromJsonAsync<JsonElement>();
            var orderPath = spec.GetProperty("paths").EnumerateObject()
                .Single(path => path.Name.TrimEnd('/') == "/api/orders").Value;
            var responses = orderPath.GetProperty("post").GetProperty("responses");
            Assert.True(responses.TryGetProperty("400", out _));
            Assert.True(responses.TryGetProperty("409", out _));
        }
    }

    private sealed class ThrowingStore(bool domain) : IAsyncOrderStore
    {
        public Task<IReadOnlyList<Order>> ListAsync(CancellationToken ct = default) =>
            throw (domain ? new DomainException("PRIVATE domain detail") : new InvalidOperationException("PRIVATE internal detail"));
        public Task<Order?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult<Order?>(null);
        public Task AddAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> UpdateAsync(Order order, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> RemoveAsync(string id, CancellationToken ct = default) => Task.FromResult(false);
    }
}
