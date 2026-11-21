using Verse;

namespace SkillGapAnalyzer;

/// <summary>Makes sure the floating button exists whenever a game is loaded or started.</summary>
public class SkillGapGameComponent : GameComponent
{
    public SkillGapGameComponent(Game game)
    {
    }

    public override void FinalizeInit()
    {
        Window_FloatingButton.EnsureOpen();
    }
}
