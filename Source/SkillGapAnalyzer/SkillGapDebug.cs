using System.Linq;
using System.Text;
using LudeonTK;
using Verse;

namespace SkillGapAnalyzer;

/// <summary>Dev-mode helper: prints the calculation result to the log so it can be checked before any UI exists.</summary>
public static class SkillGapDebug
{
    [DebugAction("Skill Gap Analyzer", "Log skill gaps", allowedGameStates = AllowedGameStates.Playing)]
    private static void LogSkillGaps()
    {
        var pawns = SkillGapCalculator.GetPawns(includeSlaves: true, includePrisoners: true);
        var sb = new StringBuilder();
        sb.AppendLine($"[Skill Gap Analyzer] {pawns.Count} pawns (incl. slaves/prisoners)");
        foreach (var combat in new[] { true, false })
        {
            sb.AppendLine(combat ? "-- Combat --" : "-- Life --");
            foreach (var s in SkillGapCalculator.Compute(combat, pawns).OrderByDescending(x => x.Gap))
            {
                sb.AppendLine($"{s.def.skillLabel}: min {s.min} ({Names(s.minPawns)}), max {s.max} ({Names(s.maxPawns)}), avg {s.avg:F1}, gap {s.Gap}, n={s.count}");
            }
        }
        Log.Message(sb.ToString());
    }

    private static string Names(System.Collections.Generic.List<Pawn> list) =>
        string.Join(", ", list.Select(p => p.LabelShort));
}
