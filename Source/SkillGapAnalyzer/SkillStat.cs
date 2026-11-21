using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SkillGapAnalyzer;

/// <summary>Result for one skill across the analyzed pawns.</summary>
public class SkillStat
{
    public SkillDef def;
    public int count;
    public int min;
    public int max;
    public float avg;
    public List<Pawn> minPawns = new List<Pawn>();
    public List<Pawn> maxPawns = new List<Pawn>();

    public int Gap => max - min;
}
