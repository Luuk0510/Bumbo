using Bumbo.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bumbo.ViewModels
{
    public class RoosterOverviewViewModel
    {
        public SelectList DayOptions { get; set; }
        public int WeekNumber { get; set; }
        public int year { get; set; }
        public DateTime SelectedDate { get; set; }
        public List<Diensten> Diensten { get; set; }
        public List<Afdelingen> Afdelingen { get; set; }
        public List<Medewerker> Medewerkers { get; set; }

    }
}