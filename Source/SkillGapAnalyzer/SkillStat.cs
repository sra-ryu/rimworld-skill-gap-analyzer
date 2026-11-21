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

    /// <summary>Group size actually used (the requested N, capped by the number of pawns).</summary>
    public int groupSize;

    /// <summary>All analyzed pawns for this skill, highest level first.</summary>
    public List<(Pawn pawn, int level)> ranked = new List<(Pawn pawn, int level)>();
    /// <summary>Average of the top-N levels.</summary>
    public float topAvg;

    /// <summary>Lowest level inside the top-N group (its highest is <see cref="max"/>).</summary>
    public int topLow;

    /// <summary>Spread inside the top-N group: highest - lowest of those N pawns.</summary>
    public int TopGap => max - topLow;
}
