using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.DTOs.Mealie
{
    public class Recipe
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}