using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TableFlow.Api.Serialization;

public static partial class UtcDateTime
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex IsoWithZone();

    public static bool TryParse(string? input, out DateTime utc)
    {
        utc = default;
        if (input is null || !IsoWithZone().IsMatch(input)
            || !DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            return false;
        utc = value.UtcDateTime;
        return true;
    }
}

public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || !UtcDateTime.TryParse(reader.GetString(), out var utc))
            throw new JsonException("Use an ISO 8601 date with Z or an explicit offset.");
        return utc;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
}

public sealed class UtcDateTimeBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => (Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType) == typeof(DateTime)
            ? new UtcDateTimeBinder() : null;
}

public sealed class UtcDateTimeBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var result = context.ValueProvider.GetValue(context.ModelName);
        if (result == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, result);
        if (UtcDateTime.TryParse(result.FirstValue, out var utc))
            context.Result = ModelBindingResult.Success(utc);
        else
            context.ModelState.TryAddModelError(context.ModelName, "Use an ISO 8601 date with Z or an explicit offset.");
        return Task.CompletedTask;
    }
}
