
namespace MealieToTodoist.Domain.TodoistClient
{
    public interface IToDoClient
    {
        Task<string> AddTaskAsync(string projectId, string content, IEnumerable<string> labels, string description);
        Task CompleteTaskAsync(string taskId);
        Task<Project> GetProjectByNameAsync(string projectName);
        Task<ProjectData> GetProjectDataAsync(string projectId);
        Task<IEnumerable<Project>> GetProjectsAsync();
        Task UpdateTaskAsync(string todoistId, string content, IEnumerable<string> labels, string description);
    }
}