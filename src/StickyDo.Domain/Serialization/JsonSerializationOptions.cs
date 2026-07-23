using System.Text.Json;
using System.Text.Json.Serialization;
using StickyDo.Domain.Models;

namespace StickyDo.Domain.Serialization;

/// <summary>
/// Centralized JSON serialization options for consistent handling across the domain.
/// </summary>
public static class JsonSerializationOptions
{
    private static JsonSerializerOptions? _options;

    /// <summary>
    /// Gets the configured JsonSerializerOptions for domain serialization.
    /// </summary>
    public static JsonSerializerOptions Default
    {
        get
        {
            _options ??= CreateOptions();
            return _options;
        }
    }

    /// <summary>
    /// Creates a new instance of JsonSerializerOptions with domain-specific configuration.
    /// </summary>
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = null,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
                new StickyNoteJsonConverter(),
                new StickyNoteTaskJsonConverter(),
            }
        };

        return options;
    }
}
