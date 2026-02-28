namespace MealieToTodoist.Domain.TodoistClient
{
    public record ToDoTask(string Id, string Content, IEnumerable<string> Labels);
}
