using System.Text.Json;
using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.DTOs.Mealie
{
    public class Label
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalData { get; set; }
    }
}
