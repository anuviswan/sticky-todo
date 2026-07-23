using System.Text.Json;
using System.Text.Json.Serialization;
using StickyDo.Domain.Models;

namespace StickyDo.Domain.Serialization;

/// <summary>
/// Custom JSON converter for StickyNote serialization and deserialization.
/// Justification: Provides explicit control over property serialization format, null handling,
/// and type safety for nested collections. Ensures consistent format across persistence layers.
/// </summary>
public class StickyNoteJsonConverter : JsonConverter<StickyNote>
{
    public override StickyNote Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected JSON object for StickyNote");

        var id = Guid.Empty;
        var title = string.Empty;
        var status = StickyNoteStatus.Active;
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow;
        uint? colorArgb = null;
        var displayOrder = 0;
        var tasks = new List<StickyNoteTask>();
        var taskConverter = new StickyNoteTaskJsonConverter();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Id":
                    id = Guid.Parse(reader.GetString() ?? throw new JsonException("Id cannot be null"));
                    break;
                case "Title":
                    title = reader.GetString() ?? throw new JsonException("Title cannot be null");
                    break;
                case "Status":
                    if (Enum.TryParse<StickyNoteStatus>(reader.GetString(), out var statusVal))
                        status = statusVal;
                    break;
                case "CreatedAt":
                    if (DateTime.TryParse(reader.GetString(), out var createdAtVal))
                        createdAt = createdAtVal.ToUniversalTime();
                    break;
                case "UpdatedAt":
                    if (DateTime.TryParse(reader.GetString(), out var updatedAtVal))
                        updatedAt = updatedAtVal.ToUniversalTime();
                    break;
                case "ColorArgb":
                    if (uint.TryParse(reader.GetString(), out var colorVal))
                        colorArgb = colorVal;
                    break;
                case "DisplayOrder":
                    if (int.TryParse(reader.GetString(), out var orderVal))
                        displayOrder = orderVal;
                    break;
                case "Tasks":
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                var task = taskConverter.Read(ref reader, typeof(StickyNoteTask), options);
                                tasks.Add(task);
                            }
                        }
                    }
                    break;
            }
        }

        return new StickyNote
        {
            Id = id,
            Title = title,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            ColorArgb = colorArgb,
            DisplayOrder = displayOrder,
            Tasks = tasks
        };
    }

    public override void Write(Utf8JsonWriter writer, StickyNote value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Id", value.Id.ToString());
        writer.WriteString("Title", value.Title);
        writer.WriteString("Status", value.Status.ToString());
        writer.WriteString("CreatedAt", value.CreatedAt.ToString("O"));
        writer.WriteString("UpdatedAt", value.UpdatedAt.ToString("O"));

        if (value.ColorArgb.HasValue)
            writer.WriteString("ColorArgb", value.ColorArgb.Value.ToString());

        writer.WriteNumber("DisplayOrder", value.DisplayOrder);

        writer.WriteStartArray("Tasks");
        var taskConverter = new StickyNoteTaskJsonConverter();
        foreach (var task in value.Tasks)
        {
            taskConverter.Write(writer, task, options);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}

/// <summary>
/// Custom JSON converter for StickyNoteTask serialization and deserialization.
/// </summary>
public class StickyNoteTaskJsonConverter : JsonConverter<StickyNoteTask>
{
    public override StickyNoteTask Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected JSON object for StickyNoteTask");

        var id = Guid.Empty;
        var title = string.Empty;
        var isCompleted = false;
        var order = 0;
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Id":
                    id = Guid.Parse(reader.GetString() ?? throw new JsonException("Id cannot be null"));
                    break;
                case "Title":
                    title = reader.GetString() ?? throw new JsonException("Title cannot be null");
                    break;
                case "IsCompleted":
                    isCompleted = reader.GetBoolean();
                    break;
                case "Order":
                    order = reader.GetInt32();
                    break;
                case "CreatedAt":
                    if (DateTime.TryParse(reader.GetString(), out var createdAtVal))
                        createdAt = createdAtVal.ToUniversalTime();
                    break;
                case "UpdatedAt":
                    if (DateTime.TryParse(reader.GetString(), out var updatedAtVal))
                        updatedAt = updatedAtVal.ToUniversalTime();
                    break;
            }
        }

        return new StickyNoteTask
        {
            Id = id,
            Title = title,
            IsCompleted = isCompleted,
            Order = order,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public override void Write(Utf8JsonWriter writer, StickyNoteTask value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Id", value.Id.ToString());
        writer.WriteString("Title", value.Title);
        writer.WriteBoolean("IsCompleted", value.IsCompleted);
        writer.WriteNumber("Order", value.Order);
        writer.WriteString("CreatedAt", value.CreatedAt.ToString("O"));
        writer.WriteString("UpdatedAt", value.UpdatedAt.ToString("O"));

        writer.WriteEndObject();
    }
}
