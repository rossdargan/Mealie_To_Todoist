using System.Text.Json;
using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.DTOs.Mealie
{
    public class ShoppingListItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("display")]
        public string Display { get; set; }

        [JsonPropertyName("checked")]
        public bool Checked { get; set; }

        [JsonPropertyName("label")]
        public Label Label { get; set; }

        // Extract TodoistId from the extras object
        [JsonIgnore]
        public string TodoistId
        {
            get
            {
                if (Extras.HasValue && Extras.Value.ValueKind == JsonValueKind.Object && Extras.Value.TryGetProperty("todoistId", out var todoistIdElement))
                {
                    return todoistIdElement.GetString();
                }
                return null;
            }
            set
            {
                if (Extras == null)
                {
                    Extras = JsonSerializer.SerializeToElement(new { todoistId = value });
                }
                else
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(Extras.Value.GetRawText());
                    dict["todoistId"] = value;
                    Extras = JsonSerializer.SerializeToElement(dict);
                }
            }
        }

        // Extract RecipeId from the first recipe reference
        [JsonIgnore]
        public string RecipeId
        {
            get
            {
                if (RecipeReferences.HasValue && RecipeReferences.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in RecipeReferences.Value.EnumerateArray())
                    {
                        if (element.TryGetProperty("recipeId", out var recipeIdElement))
                        {
                            return recipeIdElement.GetString();
                        }
                    }
                }
                return null;
            }
        }

        [JsonPropertyName("extras")]
        public JsonElement? Extras { get; set; }

        [JsonPropertyName("recipeReferences")]
        public JsonElement? RecipeReferences { get; set; }

        // This captures all other properties
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalData { get; set; }
    }
}
