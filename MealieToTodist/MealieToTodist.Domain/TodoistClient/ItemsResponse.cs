using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient
    {
        private record ItemsResponse([property: JsonPropertyName("results")] List<ItemResponse> Results);
    }
}
