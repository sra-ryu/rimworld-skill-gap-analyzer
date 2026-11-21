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
        var pawns = SkillGapCalculator.GetPawns(includeSlaves: true, includePrisoners: true, includeChildren: true);
        var sb = new StringBuilder();
        sb.AppendLine($"[Skill Gap Analyzer] {pawns.Count} pawns (incl. slaves/prisoners/children, no babies)");
        foreach (var s in SkillGapCalculator.Compute(pawns, 3).OrderByDescending(x => x.TopGap))
        {
            sb.AppendLine($"{s.def.skillLabel}: min {s.min} ({Names(s.minPawns)}), max {s.max} ({Names(s.maxPawns)}), top{s.groupSize} avg {s.topAvg:F1} gap {s.TopGap}, n={s.count}");
        }
        Log.Message(sb.ToString());
    }

    [DebugAction("Skill Gap Analyzer", "Open skill gap window", allowedGameStates = AllowedGameStates.Playing)]
    private static void OpenWindow() => Window_SkillGap.Toggle();

    private static string Names(System.Collections.Generic.List<Pawn> list) =>
        string.Join(", ", list.Select(p => p.LabelShort));
}
