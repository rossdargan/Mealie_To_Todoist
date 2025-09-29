using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MealieToTodoist.Domain.DTOs.Mealie
{
    public class MealiePagedResponse<T>
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public T[]? Items { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
    }

    public class ShoppingListItem
    {
        public float Quantity { get; set; }
        public Unit Unit { get; set; }
        public Food Food { get; set; }
        public string Note { get; set; }
        public string Display { get; set; }
        public string ShoppingListId { get; set; }
        public bool Checked { get; set; }
        public int Position { get; set; }
        public string FoodId { get; set; }
        public string LabelId { get; set; }
        public string UnitId { get; set; }
        public JsonElement Extras { get; set; }
        public string Id { get; set; }
        public string GroupId { get; set; }
        public string HouseholdId { get; set; }
        public Label Label { get; set; }
        public RecipeReference[] RecipeReferences { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? TodoistId
        {
            get
            {
                JsonElement todoistId;
                if (Extras.TryGetProperty("todoist_id", out todoistId))
                {
                    return todoistId.GetString();
                }
                return null;
            }
            set
            {
                using var doc = JsonDocument.Parse(Extras.GetRawText());
                var extrasDict = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
                extrasDict["todoist_id"] = JsonDocument.Parse($"\"{value}\"").RootElement.Clone();

                var newExtrasJson = JsonSerializer.Serialize(extrasDict);
                Extras = JsonDocument.Parse(newExtrasJson).RootElement;
            }
        }
    }

    public class Unit
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public object PluralName { get; set; }
        public string Description { get; set; }
        public JsonElement Extras { get; set; }
        public bool Fraction { get; set; }
        public string Abbreviation { get; set; }
        public string PluralAbbreviation { get; set; }
        public bool UseAbbreviation { get; set; }
        public object[] Aliases { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Food
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PluralName { get; set; }
        public string Description { get; set; }
        public JsonElement Extras { get; set; }
        public string LabelId { get; set; }
        public object[] Aliases { get; set; }
        public object[] HouseholdsWithIngredientFood { get; set; }
        public Label Label { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Label
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string GroupId { get; set; }
        public string Id { get; set; }
    }

    public class RecipeReference
    {
        public string RecipeId { get; set; }
        public float RecipeQuantity { get; set; }
        public float RecipeScale { get; set; }
        public string RecipeNote { get; set; }
        public string Id { get; set; }
        public string ShoppingListItemId { get; set; }
    }
}
