using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SkillGapAnalyzer;

public static class SkillGapCalculator
{
    /// <summary>
    /// Free colonists, optionally plus slaves and prisoners, across all maps and caravans.
    /// Babies are always excluded; children only when <paramref name="includeChildren"/> is set.
    /// </summary>
    public static List<Pawn> GetPawns(bool includeSlaves, bool includePrisoners, bool includeChildren)
    {
        var result = new List<Pawn>();
        var seen = new HashSet<Pawn>();

        void AddAll(List<Pawn> list)
        {
            foreach (var p in list)
            {
                if (p?.skills == null || p.Dead)
                {
                    continue;
                }

                var stage = p.DevelopmentalStage;
                if (stage.Newborn() || stage.Baby() || (stage.Child() && !includeChildren))
                {
                    continue;
                }

                if (seen.Add(p))
                {
                    result.Add(p);
                }
            }
        }

        AddAll(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists_NoSlaves);
        if (includeSlaves)
        {
            AddAll(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_SlavesOfColony);
        }
        if (includePrisoners)
        {
            AddAll(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony);
        }
        return result;
    }

    /// <summary>
    /// Per-skill min/max/avg/gap for every skill (including modded ones). Reads current levels every call.
    /// Average and gap are computed for the top N pawns of each skill
    /// (gap = highest - lowest level inside that group). N is capped by the number of pawns.
    /// </summary>
    public static List<SkillStat> Compute(List<Pawn> pawns, int groupSize)
    {
        var stats = new List<SkillStat>();
        foreach (var def in DefDatabase<SkillDef>.AllDefsListForReading)
        {
            var stat = new SkillStat { def = def, min = int.MaxValue, max = int.MinValue };
            long sum = 0;
            var levels = new List<int>();
            foreach (var pawn in pawns)
            {
                var rec = pawn.skills.GetSkill(def);
                // Incapable pawns have no meaningful level for this skill: leave them out.
                if (rec == null || rec.TotallyDisabled)
                {
                    continue;
                }

                int level = rec.Level;
                stat.count++;
                sum += level;
                levels.Add(level);
                stat.ranked.Add((pawn, level));

                if (level < stat.min)
                {
                    stat.min = level;
                    stat.minPawns.Clear();
                }
                if (level == stat.min)
                {
                    stat.minPawns.Add(pawn);
                }

                if (level > stat.max)
                {
                    stat.max = level;
                    stat.maxPawns.Clear();
                }
                if (level == stat.max)
                {
                    stat.maxPawns.Add(pawn);
                }
            }

            if (stat.count == 0)
            {
                continue;
            }
            stat.avg = (float)sum / stat.count;
            stat.ranked = stat.ranked.OrderByDescending(r => r.level).ThenBy(r => r.pawn.LabelShort).ToList();

            levels.Sort();
            int n = System.Math.Max(1, System.Math.Min(groupSize, stat.count));
            stat.groupSize = n;
            float top = 0f;
            for (int i = 0; i < n; i++)
            {
                top += levels[levels.Count - 1 - i];
            }
            stat.topAvg = top / n;
            stat.topLow = levels[levels.Count - n];
            stats.Add(stat);
        }
        return stats;
    }
}
