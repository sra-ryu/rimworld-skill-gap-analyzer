using UnityEngine;
using Verse;

namespace SkillGapAnalyzer;

public class SkillGapMod : Mod
{
    public static SkillGapSettings Settings { get; private set; }

    public SkillGapMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<SkillGapSettings>();
        Log.Message("[Skill Gap Analyzer] Loaded.");
    }

    public static void Save() => LoadedModManager.GetMod<SkillGapMod>()?.WriteSettings();

    public override string SettingsCategory() => "SGA_SettingsTitle".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.Label("SGA_OpenMethod".Translate());
        bool useHotkey = Settings.useHotkey;
        if (listing.RadioButton("SGA_OpenWithButton".Translate(), !useHotkey))
        {
            useHotkey = false;
        }
        if (listing.RadioButton("SGA_OpenWithHotkey".Translate(), useHotkey, tooltip: "SGA_HotkeyHint".Translate()))
        {
            useHotkey = true;
        }
        if (useHotkey != Settings.useHotkey)
        {
            Settings.useHotkey = useHotkey;
            Window_FloatingButton.Sync();
        }

        if (Settings.useHotkey)
        {
            GUI.color = Color.gray;
            listing.Label("SGA_HotkeyHint".Translate());
            GUI.color = Color.white;
        }
        else
        {
            listing.Gap();
            listing.CheckboxLabeled("SGA_LockButton".Translate(), ref Settings.lockButton);
            if (listing.ButtonText("SGA_ResetButtonPosition".Translate()))
            {
                Window_FloatingButton.ResetPosition();
            }
        }

        listing.End();
    }
}
