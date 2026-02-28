using MealieToTodoist.Domain.DTOs.Mealie;
using MealieToTodoist.Domain.Entities;
using MealieToTodoist.Domain.TodoistClient;
using System.Net.Http.Json;
using System.Runtime.InteropServices;

namespace MealieToTodoist.Domain.Repositories
{
    public class MealieRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MealieRepository(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateMealieClient()
        {
            var client = _httpClientFactory.CreateClient("MealieClient");
            return client;
        }


        public async Task<ShoppingListItem[]> GetShoppingListDetailsAsync()
        {
            var client = CreateMealieClient();
            // Example endpoint, adjust as needed
            var response = await client.GetAsync($"api/households/shopping/items?perPage=10000");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<MealiePagedResponse<ShoppingListItem>>();
            if (content?.Items == null)
            {
                return Array.Empty<ShoppingListItem>();
            }

        
            return content.Items;
        }

        internal async Task UpdateShoppingListDetailsAsync(IEnumerable<ShoppingListItem> values)
        {
            var client = CreateMealieClient();
            // Example endpoint, adjust as needed
            var response = await client.PutAsJsonAsync($"api/households/shopping/items", values);
            response.EnsureSuccessStatusCode();
        }

        internal async Task DeleteShoppingListDetailsAsync(List<ShoppingListItem> itemsToDelete)
        {
            var client = CreateMealieClient();

            // Build query string with ids
            var ids = itemsToDelete
                .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                .Select(i => $"ids={Uri.EscapeDataString(i.Id)}");

            var queryString = string.Join("&", ids);

            var requestUri = $"api/households/shopping/items?{queryString}";

            var response = await client.DeleteAsync(requestUri);
            response.EnsureSuccessStatusCode();
        }
    }
}
