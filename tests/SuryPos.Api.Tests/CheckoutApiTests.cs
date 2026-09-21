using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SuryPos.Api.Tests;

public class CheckoutApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CheckoutApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static StringContent JsonBody(string raw)
        => new(raw, Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static void AssertMinimalErrorShape(JsonElement body)
    {
        Assert.False(body.TryGetProperty("status", out _));
        Assert.False(body.TryGetProperty("instance", out _));
    }

    [Fact]
    public async Task GetProducts_Returns200()
    {
        var response = await _client.GetAsync("/pos/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyObject_Returns422_WithItemsKey()
    {
        var response = await _client.PostAsync("/pos/checkout", JsonBody("{}"));
        var body = await ParseAsync(response);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal("Validation Error", body.RootElement.GetProperty("title").GetString());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("items", out _));
        AssertMinimalErrorShape(body.RootElement);
    }

    [Fact]
    public async Task Post_EmptyItems_Returns422()
    {
        var response = await _client.PostAsync("/pos/checkout", JsonBody("""{"items":[]}"""));
        var body = await ParseAsync(response);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("items", out _));
        AssertMinimalErrorShape(body.RootElement);
    }

    [Fact]
    public async Task Post_UnknownProduct_Returns422_WithProductIdKey()
    {
        var response = await _client.PostAsync("/pos/checkout",
            JsonBody("""{"items":[{"productId":"99999999-9999-9999-9999-999999999999","quantity":1}]}"""));
        var body = await ParseAsync(response);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("errors")
            .TryGetProperty("items[0].productId", out _));
        AssertMinimalErrorShape(body.RootElement);
    }

    [Fact]
    public async Task Post_Overstock_Returns422_WithQuantityKey()
    {
        var response = await _client.PostAsync("/pos/checkout",
            JsonBody("""{"items":[{"productId":"11111111-1111-1111-1111-111111111111","quantity":1222}]}"""));
        var body = await ParseAsync(response);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("errors")
            .TryGetProperty("items[0].quantity", out _));
        AssertMinimalErrorShape(body.RootElement);
    }

    [Fact]
    public async Task Post_MalformedQuantity_Returns400()
    {
        var response = await _client.PostAsync("/pos/checkout",
            JsonBody("""{"items":[{"productId":"11111111-1111-1111-1111-111111111111","quantity":"abc"}]}"""));
        var body = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
        AssertMinimalErrorShape(body.RootElement);
    }

    [Fact]
    public async Task Post_ValidRequest_Returns200_WithTotals()
    {
        var response = await _client.PostAsync("/pos/checkout",
            JsonBody("""{"items":[{"productId":"33333333-3333-3333-3333-333333333333","quantity":1}]}"""));
        var body = await ParseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5000, body.RootElement.GetProperty("totalAmount").GetDecimal());
        Assert.False(string.IsNullOrWhiteSpace(
            body.RootElement.GetProperty("invoiceNumber").GetString()));
    }
}
