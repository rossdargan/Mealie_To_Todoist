using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient
    {
        private record ProjectResponse([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name")] string Name);
    }
}
