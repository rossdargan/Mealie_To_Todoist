using MealieToTodoist.Domain.DTOs.Mealie;
using MealieToTodoist.Domain.Entities;
using MealieToTodoist.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Todoist.Net.Models;

namespace MealieToTodoist.Domain
{
    public class SyncService
    {
        private readonly MealieRepository _mealieRepository;
        private readonly TodoistRepository _todoistRepository;
        private readonly IOptions<Settings> _settings;
        private readonly ILogger<SyncService> _logger;

        public SyncService(MealieRepository mealieRepository, TodoistRepository todoistRepository, IOptions<Settings> settings, ILogger<SyncService> logger)
        {
            _mealieRepository = mealieRepository;
            _todoistRepository = todoistRepository;
            _settings = settings;
            _logger = logger;
        }
        public async Task SyncShoppingList()
        {
            _logger.LogInformation("Starting shopping list sync.");

            var shoppingList = await _mealieRepository.GetShoppingListDetailsAsync();
            _logger.LogInformation("Fetched {Count} items from Mealie shopping list.", shoppingList.Length);

            var todoistItems = await _todoistRepository.GetAllTasks();
            _logger.LogInformation("Fetched {Count} tasks from Todoist.", todoistItems.Count);

            List<ShoppingListItem> mealieItemsToMarkComplete = new List<ShoppingListItem>();
            List<ShoppingListItem> mealieItemsToUpdate = new List<ShoppingListItem>();
            List<ShoppingListItem> mealieItemsToDelete = new List<ShoppingListItem>();
            List<TodoistTaskItem> todoistItemsToComplete = new List<TodoistTaskItem>();
            Dictionary<TodoistTaskToCreateOrUpdate, ShoppingListItem> todoistItemsToCreateOrUpdate = new();

            foreach (var mealieItem in shoppingList.Where(p => !p.Checked))
            {
                if (mealieItem.TodoistId == null)
                {
                    _logger.LogDebug("Mealie item '{Display}' has no TodoistId. Will create in Todoist.", mealieItem.Display);
                    TodoistTaskToCreateOrUpdate todoistTaskToCreate = new TodoistTaskToCreateOrUpdate(mealieItem.Display, mealieItem.Label?.Name, "Meal");
                    todoistItemsToCreateOrUpdate.Add(todoistTaskToCreate, mealieItem);
                }
                else
                {
                    if (todoistItems.Any(t => t.Id == mealieItem.TodoistId))
                    {
                        var todoistItem = todoistItems.First(t => t.Id == mealieItem.TodoistId);
                        if (todoistItem.Name != mealieItem.Display)
                        {
                            _logger.LogDebug("Mealie item '{Display}' differs from Todoist task '{Name}'. Will update Todoist.", mealieItem.Display, todoistItem.Name);
                            TodoistTaskToCreateOrUpdate todoistTaskToCreate = new TodoistTaskToCreateOrUpdate(mealieItem.Display, mealieItem.Label?.Name, "Meal");
                            todoistTaskToCreate.TodoistId = todoistItem.Id;
                            todoistItemsToCreateOrUpdate.Add(todoistTaskToCreate, mealieItem);
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Mealie item '{Display}' has TodoistId '{TodoistId}' but not found in Todoist. Will mark complete in Mealie.", mealieItem.Display, mealieItem.TodoistId);
                        mealieItemsToMarkComplete.Add(mealieItem);
                    }
                }
            }

            foreach (var mealieItem in shoppingList.Where(p => p.Checked && p.TodoistId != null))
            {
                var todoistItem = todoistItems.FirstOrDefault(t => t.Id == mealieItem.TodoistId);
                if (todoistItem != null)
                {
                    _logger.LogDebug("Mealie item '{Display}' is checked and exists in Todoist. Will mark complete in Todoist.", mealieItem.Display);
                    todoistItemsToComplete.Add(todoistItem);
                }

                if (_settings.Value.RemoveCompletedMealieItems)
                {
                    _logger.LogDebug("Mealie item '{Display}' is checked and will be deleted from Mealie.", mealieItem.Display);
                    mealieItemsToDelete.Add(mealieItem);
                }
                else
                {
                    _logger.LogDebug("Mealie item '{Display}' is checked. Will clear TodoistId and update in Mealie.", mealieItem.Display);
                    mealieItem.TodoistId = null;
                    mealieItemsToUpdate.Add(mealieItem);
                }
            }

            _logger.LogInformation("Adding/updating {Count} items in Todoist and completing {CompleteCount} tasks.", todoistItemsToCreateOrUpdate.Count, todoistItemsToComplete.Count);
            await _todoistRepository.AddItemsAndSetTodoistId(todoistItemsToCreateOrUpdate.Keys, todoistItemsToComplete.Select(p => p.Id));

            foreach (var item in todoistItemsToCreateOrUpdate)
            {
                var shoppingListItem = item.Value;
                var todoistTask = item.Key;
                shoppingListItem.TodoistId = todoistTask.TodoistId;
                mealieItemsToUpdate.Add(shoppingListItem);
            }

            foreach (var item in mealieItemsToMarkComplete)
            {
                if (_settings.Value.RemoveCompletedMealieItems)
                {
                    _logger.LogDebug("Marking mealie item '{Display}' for deletion.", item.Display);
                    mealieItemsToDelete.Add(item);
                }
                else
                {
                    _logger.LogDebug("Marking mealie item '{Display}' as checked and clearing TodoistId.", item.Display);
                    item.Checked = true;
                    item.TodoistId = null;
                    mealieItemsToUpdate.Add(item);
                }
            }

            _logger.LogInformation("Updating {Count} items in Mealie.", mealieItemsToUpdate.Count);
            await _mealieRepository.UpdateShoppingListDetailsAsync(mealieItemsToUpdate);

            _logger.LogInformation("Deleting {Count} items from Mealie.", mealieItemsToDelete.Count);
            await _mealieRepository.DeleteShoppingListDetailsAsync(mealieItemsToDelete);

            _logger.LogInformation("Shopping list sync complete. " + DateTime.Now.ToString("G"));
        }
    }
}
