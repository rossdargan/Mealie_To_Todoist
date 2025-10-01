namespace MealieToTodoist.Domain.DTOs.Mealie
{
    public class MealiePagedResponse<T>
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public T[]? Items { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
    }
}
