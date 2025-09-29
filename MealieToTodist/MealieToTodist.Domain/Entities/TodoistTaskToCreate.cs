using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealieToTodoist.Domain.Entities
{
    public record TodoistTaskToCreate(string Content, string? Label, string Description)
    {
        public string TodoistId { get; set; }
    }
}
