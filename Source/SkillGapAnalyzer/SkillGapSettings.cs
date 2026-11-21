using Verse;

namespace SkillGapAnalyzer;

/// <summary>Everything that is remembered between game sessions.</summary>
public class SkillGapSettings : ModSettings
{
    public const int DefaultGroupSize = 3;

    // Analysis window options.
    public bool includeSlaves;
    public bool includePrisoners;
    public bool includeChildren;
    public int groupSize = DefaultGroupSize;
    public string selectedSkillDefName; // null = all skills

    // Floating button position (top-left corner). Only valid when hasButtonPosition is true.
    public bool hasButtonPosition;
    public float buttonX;
    public float buttonY;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref includeSlaves, "includeSlaves");
        Scribe_Values.Look(ref includePrisoners, "includePrisoners");
        Scribe_Values.Look(ref includeChildren, "includeChildren");
        Scribe_Values.Look(ref groupSize, "groupSize", DefaultGroupSize);
        Scribe_Values.Look(ref selectedSkillDefName, "selectedSkill");
        Scribe_Values.Look(ref hasButtonPosition, "hasButtonPosition");
        Scribe_Values.Look(ref buttonX, "buttonX");
        Scribe_Values.Look(ref buttonY, "buttonY");
    }

    /// <summary>Back to the default analysis view. The button position is left alone.</summary>
    public void ResetView()
    {
        includeSlaves = false;
        includePrisoners = false;
        includeChildren = false;
        groupSize = DefaultGroupSize;
        selectedSkillDefName = null;
    }
}
