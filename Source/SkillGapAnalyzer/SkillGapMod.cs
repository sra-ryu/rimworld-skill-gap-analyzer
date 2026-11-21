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
}
