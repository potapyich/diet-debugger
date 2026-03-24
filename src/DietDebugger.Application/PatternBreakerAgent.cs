using DietDebugger.Application.Interfaces;
using DietDebugger.Domain.Entities;

namespace DietDebugger.Application;

public class PatternBreakerAgent : IPatternBreakerAgent
{
    private const int RateLimitDays = 3;

    public IReadOnlyList<PatternCandidate> Evaluate(PatternBreakerContext ctx)
    {
        var candidates = new List<PatternCandidate>();

        TryAdd(candidates, ctx, EvalWeekendOvereating(ctx));
        TryAdd(candidates, ctx, EvalLateNightCalories(ctx));
        TryAdd(candidates, ctx, EvalPostStreakCrash(ctx));
        TryAdd(candidates, ctx, EvalFridayRisk(ctx));
        TryAdd(candidates, ctx, EvalBreakfastSkipCompensation(ctx));
        TryAddDeficitStreak(candidates, ctx);
        TryAddOnce(candidates, ctx, EvalFirstFullWeek(ctx));
        TryAdd(candidates, ctx, EvalProteinStreak3(ctx));
        TryAddOnce(candidates, ctx, EvalNoLateEatingHabit(ctx));

        return candidates;
    }

    private static void TryAdd(List<PatternCandidate> list, PatternBreakerContext ctx, PatternCandidate? candidate)
    {
        if (candidate == null) return;
        if (IsRateLimited(ctx.RecentPatternEvents, candidate.PatternKey, RateLimitDays)) return;
        list.Add(candidate);
    }

    // Fire only if never fired before
    private static void TryAddOnce(List<PatternCandidate> list, PatternBreakerContext ctx, PatternCandidate? candidate)
    {
        if (candidate == null) return;
        if (ctx.RecentPatternEvents.Any(e => e.PatternKey == candidate.PatternKey)) return;
        list.Add(candidate);
    }

    private static bool IsRateLimited(IReadOnlyList<PatternEventSnapshot> events, string key, int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return events.Any(e => e.PatternKey == key && e.TriggeredAt >= cutoff);
    }

    // ── Warnings ──────────────────────────────────────────────────────────────

    // weekend_overeating: Sat+Sun average >120% of weekday average over last 2 weeks
    private static PatternCandidate? EvalWeekendOvereating(PatternBreakerContext ctx)
    {
        var cutoff = ctx.Today.AddDays(-14);
        var recent = ctx.RecentSummaries
            .Where(s => s.Date >= cutoff && s.CalorieTarget > 0)
            .ToList();

        var weekendDays = recent.Where(s => s.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday).ToList();
        var weekDays = recent.Where(s => s.Date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday).ToList();

        if (weekendDays.Count < 2 || weekDays.Count < 4) return null;

        var weekendAvg = weekendDays.Average(s => s.TotalCalories);
        var weekdayAvg = weekDays.Average(s => s.TotalCalories);

        if (weekdayAvg <= 0) return null;
        return weekendAvg > weekdayAvg * 1.2m
            ? new PatternCandidate("weekend_overeating", PatternEventType.Warning,
                "Your weekends average 20%+ more calories than weekdays. Watch out for weekend overeating patterns.")
            : null;
    }

    // late_night_calories: >40% of today's calories logged after 19:00
    private static PatternCandidate? EvalLateNightCalories(PatternBreakerContext ctx)
    {
        var totalCalories = ctx.TodaysMeals.Sum(m => m.Calories);
        if (totalCalories <= 0) return null;

        var lateCalories = ctx.TodaysMeals
            .Where(m => m.LoggedAt.Hour >= 19)
            .Sum(m => m.Calories);

        return lateCalories > totalCalories * 0.4m
            ? new PatternCandidate("late_night_calories", PatternEventType.Warning,
                "More than 40% of today's calories were consumed after 7 PM. Late eating can affect sleep and fat storage.")
            : null;
    }

    // post_streak_crash: 3+ day deficit streak broken by >130% calorie day (today)
    private static PatternCandidate? EvalPostStreakCrash(PatternBreakerContext ctx)
    {
        var todayCalories = ctx.TodaysMeals.Sum(m => m.Calories);

        // Find the most recent daily summary to get today's target
        var todaySummary = ctx.RecentSummaries
            .Where(s => s.Date < ctx.Today && s.CalorieTarget > 0)
            .OrderByDescending(s => s.Date)
            .FirstOrDefault();

        var target = todaySummary?.CalorieTarget ?? 0;
        if (target <= 0 || todayCalories <= target * 1.3m) return null;

        // Check for a 3+ day deficit streak before today
        var streak = 0;
        foreach (var s in ctx.RecentSummaries.OrderByDescending(s => s.Date))
        {
            if (s.Date >= ctx.Today) continue;
            if (s.CalorieTarget > 0 && s.TotalCalories <= s.CalorieTarget) streak++;
            else break;
        }

        return streak >= 3
            ? new PatternCandidate("post_streak_crash", PatternEventType.Warning,
                $"You broke a {streak}-day deficit streak! Today's intake is well above your target. Stay consistent!")
            : null;
    }

    // friday_risk: fired on Fridays when last 4 Fridays averaged >110% of target
    private static PatternCandidate? EvalFridayRisk(PatternBreakerContext ctx)
    {
        if (ctx.Today.DayOfWeek != DayOfWeek.Friday) return null;

        var fridays = ctx.RecentSummaries
            .Where(s => s.Date.DayOfWeek == DayOfWeek.Friday && s.CalorieTarget > 0)
            .OrderByDescending(s => s.Date)
            .Take(4)
            .ToList();

        if (fridays.Count < 2) return null;

        var avgRatio = fridays.Average(s => s.TotalCalories / s.CalorieTarget);
        return avgRatio > 1.1m
            ? new PatternCandidate("friday_risk", PatternEventType.Warning,
                "Heads up — you tend to overeat on Fridays. Try to plan your meals ahead today.")
            : null;
    }

    // breakfast_skip_compensation: no meal before 10:00 on 3+ days this week
    private static PatternCandidate? EvalBreakfastSkipCompensation(PatternBreakerContext ctx)
    {
        // Current week Mon–Sun
        var daysThisWeek = ctx.RecentSummaries
            .Where(s => s.Date >= ctx.Today.AddDays(-6) && s.Date < ctx.Today)
            .ToList();

        var skipped = daysThisWeek.Count(s => !s.HasMealBefore10);
        return skipped >= 3
            ? new PatternCandidate("breakfast_skip_compensation", PatternEventType.Warning,
                $"You've skipped breakfast {skipped} times this week. Skipping breakfast often leads to overeating later in the day.")
            : null;
    }

    // ── Positives ─────────────────────────────────────────────────────────────

    // deficit_streak_N (N=3,5,7): N consecutive days under target
    private void TryAddDeficitStreak(List<PatternCandidate> candidates, PatternBreakerContext ctx)
    {
        var streak = 0;
        foreach (var s in ctx.RecentSummaries.OrderByDescending(s => s.Date))
        {
            if (s.Date >= ctx.Today) continue;
            if (s.CalorieTarget > 0 && s.TotalCalories <= s.CalorieTarget) streak++;
            else break;
        }

        int[] milestones = [7, 5, 3];
        foreach (var n in milestones)
        {
            if (streak >= n)
            {
                var key = $"deficit_streak_{n}";
                if (!IsRateLimited(ctx.RecentPatternEvents, key, RateLimitDays))
                    candidates.Add(new PatternCandidate(key, PatternEventType.Positive,
                        $"Amazing! You've stayed under your calorie target for {n} days in a row. Keep it up!"));
                break; // Only fire highest milestone
            }
        }
    }

    // first_full_week: first ever 7-day streak under calorie target
    private static PatternCandidate? EvalFirstFullWeek(PatternBreakerContext ctx)
    {
        var last7 = ctx.RecentSummaries
            .Where(s => s.Date < ctx.Today && s.CalorieTarget > 0)
            .OrderByDescending(s => s.Date)
            .Take(7)
            .ToList();

        if (last7.Count < 7) return null;
        return last7.All(s => s.TotalCalories <= s.CalorieTarget)
            ? new PatternCandidate("first_full_week", PatternEventType.Positive,
                "Your first full week on target! 7 consecutive days under your calorie goal. This is a huge milestone!")
            : null;
    }

    // protein_streak_3: protein >= target 3 days in a row
    private static PatternCandidate? EvalProteinStreak3(PatternBreakerContext ctx)
    {
        var last3 = ctx.RecentSummaries
            .Where(s => s.Date < ctx.Today && s.ProteinTarget > 0)
            .OrderByDescending(s => s.Date)
            .Take(3)
            .ToList();

        if (last3.Count < 3) return null;
        return last3.All(s => s.TotalProteinG >= s.ProteinTarget)
            ? new PatternCandidate("protein_streak_3", PatternEventType.Positive,
                "3 days of hitting your protein target in a row! Great job supporting your muscle health.")
            : null;
    }

    // no_late_eating_habit: no meals after 20:00 for 5 consecutive days (first time)
    private static PatternCandidate? EvalNoLateEatingHabit(PatternBreakerContext ctx)
    {
        var last5 = ctx.RecentSummaries
            .Where(s => s.Date < ctx.Today)
            .OrderByDescending(s => s.Date)
            .Take(5)
            .ToList();

        if (last5.Count < 5) return null;
        return last5.All(s => !s.HasMealAfter20)
            ? new PatternCandidate("no_late_eating_habit", PatternEventType.Positive,
                "5 days with no eating after 8 PM! You've established a healthy evening eating habit.")
            : null;
    }
}
