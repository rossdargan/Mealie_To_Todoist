using System.Text.Json.Serialization;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient
    {
        // Internal class for deserializing the API response
        private record ProjectsResponse([property: JsonPropertyName("results")] List<ProjectResponse> Results);
    }
}
