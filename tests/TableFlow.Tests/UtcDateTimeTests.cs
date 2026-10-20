using System.Text.Json;
using TableFlow.Api.Serialization;

namespace TableFlow.Tests;

public class UtcDateTimeTests
{
    [Theory]
    [InlineData("2030-01-01T12:00:00Z", "2030-01-01T12:00:00Z")]
    [InlineData("2030-01-01T09:00:00-03:00", "2030-01-01T12:00:00Z")]
    [InlineData("2030-01-01T14:00:00+02:00", "2030-01-01T12:00:00Z")]
    public void ExplicitOffsetsNormalizeToUtc(string input, string expected)
    {
        Assert.True(UtcDateTime.TryParse(input, out var actual));
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
        Assert.Equal(expected, actual.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    [Theory]
    [InlineData("2030-01-01T12:00:00")]
    [InlineData("2030-01-01")]
    [InlineData("2030-02-30T12:00:00Z")]
    [InlineData("invalid")]
    public void AmbiguousOrInvalidDatesAreRejected(string input)
    {
        Assert.False(UtcDateTime.TryParse(input, out _));
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UtcDateTimeJsonConverter());
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(JsonSerializer.Serialize(input), options));
    }
}
