using UnityEngine;
using Verse;

namespace SkillGapAnalyzer;

/// <summary>Opens the floating button when a game starts and listens for the optional keyboard shortcut.</summary>
public class SkillGapGameComponent : GameComponent
{
    private static KeyBindingDef toggleKey;

    public SkillGapGameComponent(Game game)
    {
    }

    public override void FinalizeInit()
    {
        Window_FloatingButton.Sync();
    }

    public override void GameComponentOnGUI()
    {
        if (!SkillGapMod.Settings.useHotkey)
        {
            return;
        }

        toggleKey ??= DefDatabase<KeyBindingDef>.GetNamedSilentFail("SGA_ToggleWindow");
        if (toggleKey != null && toggleKey.KeyDownEvent)
        {
            Window_SkillGap.Toggle();
            Event.current.Use();
        }
    }
}
