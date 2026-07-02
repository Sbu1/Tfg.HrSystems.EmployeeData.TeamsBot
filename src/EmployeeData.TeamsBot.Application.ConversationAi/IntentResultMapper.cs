using System.Text.Json;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Application.ConversationAi;

/// <summary>Maps an Azure OpenAI tool call (function name + raw JSON arguments) to an <see cref="IntentResult"/>.</summary>
public static class IntentResultMapper
{
    public static IntentResult FromToolCall(string functionName, string argumentsJson)
    {
        var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            try
            {
                using var document = JsonDocument.Parse(argumentsJson);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in document.RootElement.EnumerateObject())
                    {
                        string? value = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString(),
                            JsonValueKind.Null or JsonValueKind.Undefined => null,
                            _ => property.Value.GetRawText()
                        };
                        if (value is not null)
                        {
                            arguments[property.Name] = value;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Malformed argument payloads degrade to "no parameters"; the dispatcher will clarify if needed.
            }
        }

        return new IntentResult(functionName, arguments);
    }
}
