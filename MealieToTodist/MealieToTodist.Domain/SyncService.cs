using MealieToTodoist.Domain.DTOs.Mealie;
using MealieToTodoist.Domain.Entities;
using MealieToTodoist.Domain.Repositories;
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

        public SyncService(MealieRepository mealieRepository, TodoistRepository todoistRepository, IOptions<Settings> settings)
        {
            _mealieRepository = mealieRepository;
            _todoistRepository = todoistRepository;
            _settings = settings;
        }
        public async Task SyncShoppingList()
        {
            var shoppingList = await _mealieRepository.GetShoppingListDetailsAsync();
            var todoistItems = await _todoistRepository.GetAllTasks();            
            // Find all the todoist items that are in the mealie shopping list - we should not change these.
            List<ShoppingListItem> mealieItemsToMarkComplete = new List<ShoppingListItem>();
            List<ShoppingListItem> mealieItemsToUpdate = new List<ShoppingListItem>();
            List<ShoppingListItem> mealieItemsToDelete = new List<ShoppingListItem>();
            List<TodoistTaskItem> todoistItemsToComplete = new List<TodoistTaskItem>();
            Dictionary<TodoistTaskToCreateOrUpdate, ShoppingListItem> todoistItemsToCreateOrUpdate = new();

            foreach (var mealieItem in shoppingList.Where(p=>!p.Checked))
            {
                if (mealieItem.TodoistId == null)
                {
                    TodoistTaskToCreateOrUpdate todoistTaskToCreate = new TodoistTaskToCreateOrUpdate(mealieItem.Display, mealieItem.Label?.Name, "Meal");
                    todoistItemsToCreateOrUpdate.Add(todoistTaskToCreate, mealieItem);
                }
                else
                {
                    if (todoistItems.Any(t => t.Id == mealieItem.TodoistId))
                    {
                        // Item exists in todoist, see if it needs updating.
                        var todoistItem = todoistItems.First(t => t.Id == mealieItem.TodoistId);
                        if (todoistItem.Name != mealieItem.Display)
                        {
                            TodoistTaskToCreateOrUpdate todoistTaskToCreate = new TodoistTaskToCreateOrUpdate(mealieItem.Display, mealieItem.Label?.Name, "Meal");
                            todoistTaskToCreate.TodoistId = todoistItem.Id;
                            // We need to update the item in todoist
                            todoistItemsToCreateOrUpdate.Add(todoistTaskToCreate, mealieItem);
                            continue;
                        }
                    }
                    else
                    {
                        // Item does not exist in todoist, we should remove it from mealie
                        mealieItemsToMarkComplete.Add(mealieItem);
                    }
                }
            }
            
            foreach(var mealieItem in shoppingList.Where(p=>p.Checked && p.TodoistId != null))
            {
                // If the item is marked complete in mealie, but has a todoist id, we should mark it complete in todoist or remove it.
                // We need to mark it as complete in todoist if it's still there
                var todoistItem = todoistItems.FirstOrDefault(t => t.Id == mealieItem.TodoistId);
                if (todoistItem!=null)
                {
                    todoistItemsToComplete.Add(todoistItem);
                }

                if (_settings.Value.RemoveCompletedMealieItems)
                {
                    mealieItemsToDelete.Add(mealieItem);
                }
                else
                {
                    mealieItem.TodoistId = null;
                    mealieItemsToUpdate.Add(mealieItem);
                }
            }


            await _todoistRepository.AddItemsAndSetTodoistId(todoistItemsToCreateOrUpdate.Keys, todoistItemsToComplete.Select(p => p.Id));
            foreach(var item in todoistItemsToCreateOrUpdate)
            {
                // Add to Extras in the ShoppingListItem
                var shoppingListItem = item.Value;
                var todoistTask = item.Key;
                shoppingListItem.TodoistId = todoistTask.TodoistId;
                mealieItemsToUpdate.Add(shoppingListItem);
            }
            
            foreach (var item in mealieItemsToMarkComplete)
            {
                if (_settings.Value.RemoveCompletedMealieItems)
                {
                    mealieItemsToDelete.Add(item);
                }
                else
                {
                    item.Checked = true;
                    item.TodoistId = null;
                    mealieItemsToUpdate.Add(item);
                }
            }

            await _mealieRepository.UpdateShoppingListDetailsAsync(mealieItemsToUpdate);
            await _mealieRepository.DeleteShoppingListDetailsAsync(mealieItemsToDelete);

        }
    }
}
