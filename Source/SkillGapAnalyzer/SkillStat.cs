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
    public float topAvg;
    public float bottomAvg;

    /// <summary>Lowest level inside the top-N group (its highest is <see cref="max"/>).</summary>
    public int topLow;
    /// <summary>Highest level inside the bottom-N group (its lowest is <see cref="min"/>).</summary>
    public int bottomHigh;

    /// <summary>Spread inside the top-N group: highest - lowest of those N pawns.</summary>
    public int TopGap => max - topLow;
    /// <summary>Spread inside the bottom-N group: highest - lowest of those N pawns.</summary>
    public int BottomGap => bottomHigh - min;
}
