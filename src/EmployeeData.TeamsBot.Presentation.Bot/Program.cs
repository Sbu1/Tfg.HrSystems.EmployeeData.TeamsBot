using EmployeeData.TeamsBot.Application.ConversationAi;
using EmployeeData.TeamsBot.Application.EmployeeDataApi;
using EmployeeData.TeamsBot.Application.Graph;
using EmployeeData.TeamsBot.Presentation.Bot;
using EmployeeData.TeamsBot.Presentation.Bot.Dependencies;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConversationAi(builder.Configuration);
builder.Services.AddEmployeeDataApi(builder.Configuration);
builder.Services.AddGraphDirectory(builder.Configuration);
builder.Services.AddDomainServices();
builder.Services.AddHandlers();
builder.Services.AddSingleton<TurnResponseRenderer>();
builder.Services.AddBotAdapter();

var app = builder.Build();

// Plain host (CP-08): the Bot Connector / Emulator posts activities to the raw /api/messages endpoint with no
// AutoWrapper over the response, which the full Tfg.ApiShell host would otherwise apply and break.
app.MapPost("/api/messages", (
    HttpRequest request, HttpResponse response, CloudAdapter adapter, IBot bot, CancellationToken ct) =>
    adapter.ProcessAsync(request, response, bot, ct));

app.MapGet("/healthz", () => Results.Ok("healthy"));

app.Run();
