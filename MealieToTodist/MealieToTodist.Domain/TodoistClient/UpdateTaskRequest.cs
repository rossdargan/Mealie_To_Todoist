using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient
    {
        private record UpdateTaskRequest([property: JsonPropertyName("content")] string Content, [property: JsonPropertyName("description")] string Description, [property: JsonPropertyName("labels")] List<string> Labels);
    }
}
