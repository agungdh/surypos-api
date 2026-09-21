using SuryPos.Api.Exceptions;

namespace SuryPos.Api.Tests;

public class ValidationErrorKeysTests
{
    [Theory]
    [InlineData("Items", "items")]
    [InlineData("Items[0].ProductId", "items[0].productId")]
    [InlineData("Items[0].Quantity", "items[0].quantity")]
    [InlineData("$.items[0].quantity", "items[0].quantity")]
    [InlineData("request", "request")]
    [InlineData("", "")]
    public void Normalize_MatchesJsonFieldNames(string input, string expected)
    {
        Assert.Equal(expected, ValidationErrorKeys.Normalize(input));
    }
}
