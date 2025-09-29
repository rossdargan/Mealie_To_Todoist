using MealieToTodoist.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Todoist.Net;
using Todoist.Net.Models;

namespace MealieToTodoist.Domain.Repositories
{
    public class TodoistRepository
    {
        private readonly ILogger<TodoistRepository> _logger;
        private readonly ITodoistClientFactory _todoistClientFactory;
        private readonly IOptions<Settings> _settings;
        private Lazy<TodoistClient> _lazyClient;
        private Lazy<Task<Project>> _lazyShoppingProject;
        public TodoistRepository(ILogger<TodoistRepository> logger, ITodoistClientFactory todoistClientFactory, IOptions<Settings> settings)
        {
            _logger = logger;            
            _settings = settings;
            _lazyClient = new Lazy<TodoistClient>(() => todoistClientFactory.CreateClient(settings.Value.TodoistApiKey));
            _lazyShoppingProject = new Lazy<Task<Project>>(async () => await GetShoppingProject());
        }
        private async Task<Project> GetShoppingProject()
        {
            var projects = await _lazyClient.Value.Projects.GetAsync();

            var shoppingProject = projects.FirstOrDefault(p => p.Name == _settings.Value.TodoistShoppingListName);

            return shoppingProject;
        }
        public async Task<HashSet<TodoistTaskItem>> GetAllTasks()
        {
            var shoppingProject = await _lazyShoppingProject.Value;
            var todoistItems = await _lazyClient.Value.Projects.GetDataAsync(shoppingProject.Id);
            var results = new HashSet<TodoistTaskItem>();
            foreach (var item in todoistItems.Items)
            {
                TodoistTaskItem taskItem = new TodoistTaskItem(item.Id.PersistentId, item.Content, item.Labels);
                results.Add(taskItem);
            }
            return results;
        }
        public async Task AddItemsAndSetTodoistId(IEnumerable<TodoistTaskToCreateOrUpdate> taskToCreate, IEnumerable<string> tasksToComplete)
        {
            if(!(taskToCreate.Any()|| tasksToComplete.Any()))
            {
                return;
            }
            var shoppingProject = await _lazyShoppingProject.Value;
            var transaction = _lazyClient.Value.CreateTransaction();
            Dictionary<TodoistTaskToCreateOrUpdate, AddItem> taskToItemMap = new Dictionary<TodoistTaskToCreateOrUpdate, AddItem>();
            foreach (var task in taskToCreate)
            {
                var labels = string.IsNullOrEmpty(task.Label) ? new Collection<string>() : new Collection<string> { task.Label };

                if (task.TodoistId != null)
                {
                    var item = new UpdateItem(task.TodoistId)
                    {
                        Content = task.Content,
                        Labels = labels,
                        Description = task.Description
                    };                    
                    await transaction.Items.UpdateAsync(item);
                }
                else
                {
                    var item = new AddItem(task.Content, shoppingProject.Id)
                    {
                        Labels = labels,
                        Description = task.Description
                    };
                    taskToItemMap[task] = item;
                    await transaction.Items.AddAsync(item);
                }
                
            }
            foreach(var itemToComplete in tasksToComplete)
            {
                await transaction.Items.CloseAsync(itemToComplete);
            }
            await transaction.CommitAsync();

            foreach (var task in taskToCreate)
            {

                if(task.TodoistId != null)
                {
                    continue;
                }
                task.TodoistId = taskToItemMap[task].Id.PersistentId;
            }
        }

    }
    
}
