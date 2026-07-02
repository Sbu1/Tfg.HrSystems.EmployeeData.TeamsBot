namespace EmployeeData.TeamsBot.Domain.Constants;

/// <summary>Azure OpenAI function (intent) names - the contract between the NLU layer and the dispatcher (spec section 22.1).</summary>
public static class IntentNames
{
    public const string GetMyHours = "get_my_hours";
    public const string GetMyHistory = "get_my_history";
    public const string GetPeerStanding = "get_peer_standing";
    public const string ListMotivations = "list_motivations";
    public const string AddMotivation = "add_motivation";
    public const string RemoveMotivation = "remove_motivation";
    public const string GetTeamThisMonth = "get_team_this_month";
    public const string GetTeamHistory = "get_team_history";
    public const string GetAtRisk = "get_at_risk";
    public const string Help = "help";
    public const string Clarify = "clarify";

    // Internal button-only intents (from a confirmation card's Confirm tap; never emitted by the LLM).
    public const string AddMotivationConfirmed = "add_motivation_confirmed";
    public const string RemoveMotivationConfirmed = "remove_motivation_confirmed";
}
