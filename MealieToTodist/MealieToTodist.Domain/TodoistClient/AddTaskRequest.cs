using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient
    {
        private record AddTaskRequest([property: JsonPropertyName("project_id")] string ProjectId, [property: JsonPropertyName("content")] string Content, [property: JsonPropertyName("description")] string Description, [property: JsonPropertyName("labels")] List<string> Labels);
    }
}
