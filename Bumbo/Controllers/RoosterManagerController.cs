using Bumbo.Models;
using Bumbo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Bumbo.Controllers
{
    [Authorize(Roles = "Manager")]
    public class RoosterManagerController : Controller
    {
        private readonly BumboContext _context;

        public RoosterManagerController(BumboContext context)
        {
            _context = context;
        }


        public Medewerker GetLoggedInUser()
        {
            var userEmail = User.Identity.Name;

            // Assuming there's a field in Medewerker that links to the IdentityUser's ID
            var medewerker = _context.Medewerkers.FirstOrDefault(m => m.Email == userEmail);

            return medewerker;
        }

        public IActionResult Index(int year)
        {
            int yearIndex = year;
            int CurrentYear = DateTime.Now.Year;
            int lastYear = yearIndex - 1;
            int? filiaalId = GetLoggedInUser()?.FiliaalId;


            int currentWeek = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                DateTime.Now,
                CalendarWeekRule.FirstDay,
                DayOfWeek.Monday
                ) - 1;

            var checkDienstenCoverage = CheckDienstenCoverage(year, filiaalId);

            var allWeeks = Enumerable.Range(1, 52).Select(weekNumber => new WeekGroup
            {
                WeekNumber = weekNumber,
                Amount = 0,
                isComplete = checkDienstenCoverage.Any(cdc => cdc.WeekNumber == weekNumber && cdc.isComplete)
            }).ToList();

            var roosterData = _context.Dienstens
                .Where(d => d.Datum.Year == year)
                .Join(_context.Medewerkers,
                      dienst => dienst.MedewerkerId,
                      medewerker => medewerker.MedewerkerId,
                      (dienst, medewerker) => new { dienst, medewerker })
                .Where(dm => dm.medewerker.FiliaalId == filiaalId)
                .AsEnumerable()
                .GroupBy(dm => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dm.dienst.Datum, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday))
                .Select(g => new WeekGroup
                {
                    WeekNumber = g.Key,
                    Amount = g.Count(),
                })
                .ToList();

            var dataMerge = allWeeks
                .GroupJoin(roosterData,
                           allWeek => allWeek.WeekNumber,
                           roosterWeek => roosterWeek.WeekNumber,
                           (allWeek, roosterWeeks) => new WeekGroup
                           {
                               WeekNumber = allWeek.WeekNumber,
                               Amount = roosterWeeks.FirstOrDefault()?.Amount ?? 0,
                               isComplete = allWeek.isComplete
                           })
                        .OrderBy(w => w.WeekNumber)
            .ToList();

            if (roosterData.Any() == false && year < CurrentYear)
            {
                TempData["TempData"] = $"Er bestaan geen roosters uit het jaar <b>{year}</b>";
                return RedirectToAction("Index", new {year = DateTime.Now.Year }) ;
            }


            var WeekViewModel = new RoosterJaarViewModel
            {
                Year = year,
                CurrentWeek = currentWeek,
                MergedData = dataMerge
            };

            return View(WeekViewModel);
        }


        private List<WeekGroup> CheckDienstenCoverage(int year, int? filiaalId)
        {
            var cultureInfo = new CultureInfo("nl-NL");
            var calendar = cultureInfo.Calendar;

            // Get all Prognoses for the given year and filiaal
            var prognoses = _context.Prognoses
                .Include(p => p.Afdeling)
                .Where(p => p.Datum.Year == year && p.FiliaalId == filiaalId)
                .ToList();

            // Get all Diensten for the given year and filiaal, including the Medewerker and their Afdeling
            var diensten = _context.Dienstens
                .Include(d => d.Medewerker)
                    .ThenInclude(m => m.Functie)
                        .ThenInclude(f => f.Afdelings)
                .Where(d => d.Datum.Year == year && d.Medewerker.FiliaalId == filiaalId)
                .ToList();

            var weeksCoverage = new List<WeekGroup>();

            // Get the distinct weeks in the prognoses
            var weeksInYear = prognoses.Select(p => calendar.GetWeekOfYear(p.Datum, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)).Distinct();

            foreach (var week in weeksInYear)
            {
                var prognosesInWeek = prognoses.Where(p => calendar.GetWeekOfYear(p.Datum, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == week);
                var dienstenInWeek = diensten.Where(d => calendar.GetWeekOfYear(d.Datum, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == week);

                // Group by AfdelingId to compare the hours
                var isWeekComplete = prognosesInWeek.GroupBy(p => p.AfdelingId).All(group =>
                {
                    var afdelingId = group.Key;
                    var prognoseHours = group.Sum(p => p.Uren);
                    var dienstHours = dienstenInWeek
                        .Where(d => d.Medewerker.Functie.Afdelings.Any(a => a.AfdelingId == afdelingId))
                        .Sum(d => (d.EindTijd - d.StartTijd).TotalHours);

                    return dienstHours >= prognoseHours;
                });

                weeksCoverage.Add(new WeekGroup
                {
                    WeekNumber = week,
                    isComplete = isWeekComplete
                });
            }

            return weeksCoverage;
        }
    }

}
