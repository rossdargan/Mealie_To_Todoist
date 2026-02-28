using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Configuration;
using MealieToTodoist.Domain;
using MealieToTodoist.Domain.Repositories;
using MealieToTodoist.Domain.TodoistClient;
using Microsoft.Extensions.Options;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddUserSecrets<Program>();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<Settings>(context.Configuration.GetSection("Settings"));
        services.AddTransient<MealieRepository>();
        services.AddTransient<SyncService>();
        services.AddTransient<TodoistRepository>();
        services.AddHttpClient("MealieClient", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<Settings>>().Value;
            client.BaseAddress = new Uri(options.MealieBaseUrl);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.MealieApiKey);
        });
        services.AddHttpClient<IToDoClient, ToDoClient>("TodoistClient", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<Settings>>().Value;
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.TodoistApiKey);
        });
    })
    .Build();

using var serviceScope = host.Services.CreateScope();
var services = serviceScope.ServiceProvider;
var repo = services.GetService<SyncService>();

await repo.SyncShoppingList();


