using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient
    {
        private record ItemResponse([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("content")] string Content, [property: JsonPropertyName("labels")] List<string> Labels);
    }
}
