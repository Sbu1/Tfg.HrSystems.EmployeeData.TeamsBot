using EmployeeData.TeamsBot.Domain.Models;
using Newtonsoft.Json.Linq;

namespace EmployeeData.TeamsBot.Presentation.Bot;

/// <summary>
/// Turns an <c>Action.Submit</c> button payload (the inbound activity's Value) into an <see cref="IntentResult"/>,
/// so button taps route straight to the dispatcher and bypass the LLM (FR-4.2, section 13.4).
/// </summary>
public static class QuickActionParser
{
    public static bool TryParse(object? activityValue, out IntentResult intent)
    {
        intent = null!;
        if (activityValue is null)
        {
            return false;
        }

        JObject payload = activityValue as JObject ?? JObject.FromObject(activityValue);
        string? name = (string?)payload["intent"];
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (JProperty property in payload.Properties())
        {
            if (!property.Name.Equals("intent", StringComparison.OrdinalIgnoreCase) && property.Value.Type != JTokenType.Null)
            {
                arguments[property.Name] = property.Value.ToString();
            }
        }

        intent = new IntentResult(name, arguments);
        return true;
    }
}
