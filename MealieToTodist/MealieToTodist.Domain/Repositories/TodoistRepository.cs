using MealieToTodoist.Domain.Entities;
using MealieToTodoist.Domain.TodoistClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MealieToTodoist.Domain.Repositories
{
    public class TodoistRepository
    {
        private readonly ILogger<TodoistRepository> _logger;
        private readonly IOptions<Settings> _settings;
        private readonly IToDoClient _toDoClient;
        private Lazy<Task<Project>> _lazyShoppingProject;

        public TodoistRepository(ILogger<TodoistRepository> logger, IOptions<Settings> settings, IToDoClient toDoClient)
        {
            _logger = logger;
            _settings = settings;
            _toDoClient = toDoClient;
            _lazyShoppingProject = new Lazy<Task<Project>>(async () => await GetShoppingProject());
        }

        private async Task<Project> GetShoppingProject()
        {
            var shoppingProject = await _toDoClient.GetProjectByNameAsync(_settings.Value.TodoistShoppingListName);
            return shoppingProject;
        }

        public async Task<HashSet<TodoistTaskItem>> GetAllTasks()
        {
            var shoppingProject = await _lazyShoppingProject.Value;
            var projectData = await _toDoClient.GetProjectDataAsync(shoppingProject.Id);

            var results = new HashSet<TodoistTaskItem>();
            foreach (var item in projectData.Tasks)
            {
                TodoistTaskItem taskItem = new TodoistTaskItem(item.Id, item.Content, item.Labels.ToList());
                results.Add(taskItem);
            }
            return results;
        }

        public async Task AddItemsAndSetTodoistId(IEnumerable<TodoistTaskToCreateOrUpdate> taskToCreate, IEnumerable<string> tasksToComplete)
        {
            if (!(taskToCreate.Any() || tasksToComplete.Any()))
            {
                return;
            }

            var shoppingProject = await _lazyShoppingProject.Value;

            foreach (var task in taskToCreate)
            {
                var labels = string.IsNullOrEmpty(task.Label) ? Enumerable.Empty<string>() : new[] { task.Label };

                if (task.TodoistId != null)
                {
                    await _toDoClient.UpdateTaskAsync(task.TodoistId, task.Content, labels, task.Description);
                }
                else
                {
                    var newTaskId = await _toDoClient.AddTaskAsync(shoppingProject.Id, task.Content, labels, task.Description);
                    task.TodoistId = newTaskId.ToString();
                }
            }

            foreach (var itemToComplete in tasksToComplete)
            {
                await _toDoClient.CompleteTaskAsync(itemToComplete);
            }
        }
    }
}
