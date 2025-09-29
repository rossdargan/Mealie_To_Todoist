namespace MealieToTodoist.Domain
{
    public class Settings
    {
        public string MealieApiKey { get; set; }
        public string MealieBaseUrl { get; set; }
        public string TodoistApiKey { get; set; }
        public string TodoistShoppingListName { get; set; }
        public bool RemoveCompletedMealieItems { get; set; } = false;
    }
}
