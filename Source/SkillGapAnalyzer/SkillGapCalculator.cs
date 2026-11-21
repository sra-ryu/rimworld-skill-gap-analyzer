using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SkillGapAnalyzer;

public static class SkillGapCalculator
{
    // Combat group = Shooting + Melee. Everything else (incl. modded skills) is "life".
    private static readonly HashSet<string> CombatSkills = new HashSet<string> { "Shooting", "Melee" };

    public static bool IsCombat(SkillDef def) => CombatSkills.Contains(def.defName);

    /// <summary>Free colonists, optionally plus slaves and prisoners, across all maps and caravans.</summary>
    public static List<Pawn> GetPawns(bool includeSlaves, bool includePrisoners)
    {
        var result = new List<Pawn>();
        var seen = new HashSet<Pawn>();

        void AddAll(List<Pawn> list)
        {
            foreach (var p in list)
            {
                if (p?.skills != null && !p.Dead && seen.Add(p))
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

    /// <summary>Per-skill min/max/avg/gap for the combat or life group. Reads current levels every call.</summary>
    public static List<SkillStat> Compute(bool combat, List<Pawn> pawns)
    {
        var stats = new List<SkillStat>();
        foreach (var def in DefDatabase<SkillDef>.AllDefsListForReading)
        {
            if (IsCombat(def) != combat)
            {
                continue;
            }

            var stat = new SkillStat { def = def, min = int.MaxValue, max = int.MinValue };
            long sum = 0;
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
            stats.Add(stat);
        }
        return stats;
    }
}
