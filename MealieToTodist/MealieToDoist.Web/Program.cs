using MealieToTodoist.Domain;
using MealieToTodoist.Domain.Repositories;
using MealieToTodoist.Domain.TodoistClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Correct way to add user secrets in minimal hosting model
builder.Configuration.AddUserSecrets<Program>();
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));
builder.Services.AddTransient<MealieRepository>();
builder.Services.AddTransient<SyncService>();
builder.Services.AddTransient<TodoistRepository>();
builder.Services.AddSingleton<SyncTriggerChannel>();
builder.Services.AddHttpClient("MealieClient", (provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<Settings>>().Value;
    client.BaseAddress = new Uri(options.MealieBaseUrl);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.MealieApiKey);
});
builder.Services.AddHttpClient<IToDoClient, ToDoClient>("TodoistClient", (provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<Settings>>().Value;
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.TodoistApiKey);
});
builder.Services.AddHostedService<ShoppingListSyncBackgroundService>();

var app = builder.Build();

var notificationHandler = app.MapGroup("/notification");
notificationHandler.MapPost("/", (IHostApplicationLifetime appLifetime, IServiceProvider services) =>
{
    var syncTriggerChannel = services.GetRequiredService<SyncTriggerChannel>();
    syncTriggerChannel.Trigger();    
    return Results.Accepted();
});

app.Run();



