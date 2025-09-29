using MealieToTodoist.Domain.DTOs.Mealie;
using MealieToTodoist.Domain.Entities;
using MealieToTodoist.Domain.Repositories;
using System.Text.Json;

namespace MealieToTodoist.Domain
{
    public class SyncService
    {
        private readonly MealieRepository _mealieRepository;
        private readonly TodoistRepository _todoistRepository;

        public SyncService(MealieRepository mealieRepository, TodoistRepository todoistRepository)
        {
            _mealieRepository = mealieRepository;
            _todoistRepository = todoistRepository;
        }
        public async Task SyncShoppingList(string mealieShoppingListId)
        {
            var shoppingList = await _mealieRepository.GetShoppingListDetailsAsync();
            var todoistItems = await _todoistRepository.GetAllTasks();            
            // Find all the todoist items that are in the mealie shopping list - we should not change these.
            List<ShoppingListItem> itemsToMarkComplete = new List<ShoppingListItem>();
            Dictionary<TodoistTaskToCreate, ShoppingListItem> itemsToCreate = new();
            foreach (var mealieItem in shoppingList.Where(p=>!p.Checked))
            {
                if (mealieItem.TodoistId == null)
                {
                    TodoistTaskToCreate todoistTaskToCreate = new TodoistTaskToCreate(mealieItem.Display, mealieItem.Label?.Name, "Meal");
                    itemsToCreate.Add(todoistTaskToCreate, mealieItem);
                }
                else
                {
                    if (todoistItems.Any(t => t.Id == mealieItem.TodoistId))
                    {
                        // Item exists in todoist, see if it needs updating.
                        var todoistItem = todoistItems.First(t => t.Id == mealieItem.TodoistId);
                        if (todoistItem.Name != mealieItem.Display)
                        {
                            TodoistTaskToCreate todoistTaskToCreate = new TodoistTaskToCreate(mealieItem.Display, mealieItem.Label?.Name, "Meal");
                            todoistTaskToCreate.TodoistId = todoistItem.Id;
                            // We need to update the item in todoist
                            itemsToCreate.Add(todoistTaskToCreate, mealieItem);
                            continue;
                        }
                    }
                    else
                    {
                        // Item does not exist in todoist, we should remove it from mealie
                        itemsToMarkComplete.Add(mealieItem);
                    }
                }
            }
            List<ShoppingListItem> itemsToUpdate = new List<ShoppingListItem>();
            await _todoistRepository.AddItemsAndSetTodoistId(itemsToCreate.Keys);
            foreach(var item in itemsToCreate)
            {
                // Add to Extras in the ShoppingListItem
                var shoppingListItem = item.Value;
                var todoistTask = item.Key;
                shoppingListItem.TodoistId = todoistTask.TodoistId;
                itemsToUpdate.Add(shoppingListItem);
            }

            foreach(var item in itemsToMarkComplete)
            {
                item.Checked = true;
                itemsToUpdate.Add(item);
            }

            await _mealieRepository.UpdateShoppingListDetailsAsync(itemsToUpdate);
        }
    }
}
