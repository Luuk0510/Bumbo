using Bumbo.Models;

namespace Bumbo.ViewModels
{
    public class MaandUrenRegistratieJaarViewModel
    {
        public int Year { get; set; }
        
        public int CurrentMonth { get; set; }

        public List<MonthGroup> Months = new List<MonthGroup>();
    }
}
