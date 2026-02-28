using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient
    {
        private record AddTaskResponse([property: JsonPropertyName("id")] string Id);
    }
}
