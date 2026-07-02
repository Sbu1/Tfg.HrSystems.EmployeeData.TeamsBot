namespace EmployeeData.TeamsBot.Application.ConversationAi;

public sealed class ConversationAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;

    public string Deployment { get; set; } = "gpt-5.4-mini";

    public string ApiKey { get; set; } = string.Empty;
}
