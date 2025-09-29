using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Configuration;
using MealieToTodoist.Domain;
using Todoist.Net.Extensions;
using MealieToTodoist.Domain.Repositories;
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
        services.AddTodoistClient();
        services.AddHttpClient("MealieClient", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<Settings>>().Value;
            client.BaseAddress = new Uri(options.MealieBaseUrl);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.MealieApiKey);
        });
        // Register other services here as needed
    })
    .Build();

using var serviceScope = host.Services.CreateScope();
var services = serviceScope.ServiceProvider;
var repo = services.GetService<SyncService>();

await repo.SyncShoppingList("ec65c231-9d2a-4801-a657-5ce843b53270");


