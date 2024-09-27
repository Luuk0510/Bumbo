using Bumbo.Models;

namespace Bumbo.ViewModels
{
    public class WeekUrenRegistratieJaarViewModel
    {
        public int Year { get; set; }

        public int CurrentWeek { get; set; }

        public List<WeekGroup> WeekData { get; set; }
    }
}
