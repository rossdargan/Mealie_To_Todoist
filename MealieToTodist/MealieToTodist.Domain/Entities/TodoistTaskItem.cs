using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealieToTodoist.Domain.Entities
{
    public record TodoistTaskItem(string Id, string Name, ICollection<string> Labels)
    {
    }
}
