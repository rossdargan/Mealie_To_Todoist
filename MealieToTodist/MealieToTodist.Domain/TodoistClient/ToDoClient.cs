using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MealieToTodoist.Domain.TodoistClient
{

    public partial class ToDoClient : IToDoClient
    {
        private const string BaseUrl = "https://api.todoist.com/api/v1";
        private readonly HttpClient _httpClient;
        private readonly ILogger<ToDoClient> _logger;
    
        public ToDoClient(HttpClient httpClient, ILogger<ToDoClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<Project>> GetProjectsAsync()
        {
            _logger.LogInformation("Retrieving all projects from Todoist API");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/projects");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var contentString = await response.Content.ReadAsStringAsync();
                var content = System.Text.Json.JsonSerializer.Deserialize<ProjectsResponse>(contentString);

                _logger.LogInformation("Successfully retrieved {ProjectCount} projects from Todoist API", content.Results.Count);
                return content.Results.Select(p => new Project(p.Id, p.Name)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve projects from Todoist API");
                throw new InvalidOperationException("Failed to retrieve projects from Todoist API.", ex);
            }
        }

        public async Task<Project> GetProjectByNameAsync(string projectName)
        {
            _logger.LogInformation("Searching for project with name: {ProjectName}", projectName);
            var projects = await GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Name == projectName);
            
            if (project != null)
            {
                _logger.LogInformation("Found project '{ProjectName}' with ID: {ProjectId}", projectName, project.Id);
            }
            else
            {
                _logger.LogWarning("Project with name '{ProjectName}' not found", projectName);
            }
            
            return project;
        }

        public async Task<ProjectData> GetProjectDataAsync(string projectId)
        {
            _logger.LogInformation("Retrieving tasks for project ID: {ProjectId}", projectId);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/tasks?project_id={projectId}");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var contentString = await response.Content.ReadAsStringAsync();
                var content = System.Text.Json.JsonSerializer.Deserialize<ItemsResponse>(contentString);

                var items = content.Results.Select(item => new ToDoTask(item.Id, item.Content, item.Labels ?? Enumerable.Empty<string>())).ToList();

                _logger.LogInformation("Successfully retrieved {TaskCount} tasks for project {ProjectId}", items.Count, projectId);
                return new ProjectData(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve project data for project {ProjectId}", projectId);
                throw new InvalidOperationException($"Failed to retrieve project data for project {projectId} from Todoist API.", ex);
            }
        }

        public async Task<string> AddTaskAsync(string projectId, string content, IEnumerable<string> labels, string description)
        {
            _logger.LogInformation("Adding new task to project {ProjectId}: '{Content}'", projectId, content);
            try
            {
                var payload = new AddTaskRequest(projectId, content, description, labels.ToList());

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tasks");
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var contentString = await response.Content.ReadAsStringAsync();
                var taskResponse = System.Text.Json.JsonSerializer.Deserialize<AddTaskResponse>(contentString);

                _logger.LogInformation("Successfully created task with ID: {TaskId}", taskResponse.Id);
                return taskResponse.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add task to project {ProjectId}", projectId);
                throw new InvalidOperationException("Failed to add task to Todoist API.", ex);
            }
        }

        public async Task CompleteTaskAsync(string taskId)
        {
            _logger.LogInformation("Completing task with ID: {TaskId}", taskId);
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tasks/{taskId}/close");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully completed task {TaskId}", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete task {TaskId}", taskId);
                throw new InvalidOperationException($"Failed to complete task {taskId} in Todoist API.", ex);
            }
        }

        public async Task UpdateTaskAsync(string taskId, string content, IEnumerable<string> labels, string description)
        {
            _logger.LogInformation("Updating task {TaskId}: '{Content}'", taskId, content);
            try
            {
                var payload = new UpdateTaskRequest(content, description, labels.ToList());

                var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/tasks/{taskId}");
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully updated task {TaskId}", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update task {TaskId}", taskId);
                throw new InvalidOperationException($"Failed to update task {taskId} in Todoist API.", ex);
            }
        }
    }
}
