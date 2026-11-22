using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkillGapAnalyzer;

/// <summary>Standalone (non-tab) window showing the current skill gap, pick one skill or see them all.</summary>
public class Window_SkillGap : Window
{
    private enum SortKey { Name, Max, Avg, Gap }

    private const int TopShown = 3;

    private const float RowHeight = 34f;
    private const float HeaderHeight = 28f;
    // Keeps Reset/Refresh clear of the window's close (X) button.
    private const float TopRightInset = 14f;
    private const float RefreshInterval = 1f;
    private const int MaxLevel = 20;
    private const int MaxGroupSize = 50;

    // Stored in the mod settings so they survive restarts.
    private static SkillGapSettings Settings => SkillGapMod.Settings;

    private static bool includeSlaves
    {
        get => Settings.includeSlaves;
        set => Settings.includeSlaves = value;
    }

    private static bool includePrisoners
    {
        get => Settings.includePrisoners;
        set => Settings.includePrisoners = value;
    }

    private static bool includeChildren
    {
        get => Settings.includeChildren;
        set => Settings.includeChildren = value;
    }

    private static int groupSize
    {
        get => Settings.groupSize;
        set => Settings.groupSize = value;
    }

    private static int minLevel
    {
        get => Settings.minLevel;
        set => Settings.minLevel = value;
    }

    // null = all skills
    private static SkillDef selectedSkill
    {
        get => Settings.selectedSkillDefName == null ? null : DefDatabase<SkillDef>.GetNamedSilentFail(Settings.selectedSkillDefName);
        set => Settings.selectedSkillDefName = value?.defName;
    }

    // Default: biggest gap first.
    private SortKey sortKey = SortKey.Gap;
    private bool sortDescending = true;
    private Vector2 scroll;
    private Vector2 rankScroll;
    private float nextRefreshTime;
    private List<Pawn> pawns = new List<Pawn>();
    private List<SkillStat> stats = new List<SkillStat>();

    public override Vector2 InitialSize => new Vector2(700f, 480f);

    public Window_SkillGap()
    {
        draggable = true;
        resizeable = true;
        doCloseX = true;
        closeOnAccept = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        forcePause = false;
    }

    public static void Toggle()
    {
        var existing = Find.WindowStack.WindowOfType<Window_SkillGap>();
        if (existing != null)
        {
            existing.Close();
        }
        else
        {
            Find.WindowStack.Add(new Window_SkillGap());
        }
    }

    public override void PostOpen()
    {
        Recalculate();
    }

    private void Recalculate()
    {
        pawns = SkillGapCalculator.GetPawns(includeSlaves, includePrisoners, includeChildren);
        stats = SkillGapCalculator.Compute(pawns, groupSize);
        nextRefreshTime = Time.realtimeSinceStartup + RefreshInterval;
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (Time.realtimeSinceStartup >= nextRefreshTime)
        {
            Recalculate();
        }

        // Title is drawn inside the content area (not via optionalTitle) so the rows can sit right under it
        // without being clipped by the window's content rect.
        float y = inRect.y;
        Text.Font = GameFont.Medium;
        Text.CurFontStyle.fontStyle = FontStyle.Bold;
        Widgets.Label(new Rect(inRect.x, y, inRect.width - 110f - TopRightInset, 32f), "SGA_WindowTitle".Translate());
        Text.CurFontStyle.fontStyle = FontStyle.Normal;
        Text.Font = GameFont.Small;

        var resetRect = new Rect(inRect.xMax - 100f - TopRightInset, y + 1f, 100f, 30f);
        if (Widgets.ButtonText(resetRect, "SGA_Reset".Translate()))
        {
            ResetView();
        }
        TooltipHandler.TipRegion(resetRect, "SGA_ResetTip".Translate());
        y += 40f;

        DrawTopBar(new Rect(inRect.x, y, inRect.width, 30f));
        y += 42f;
        DrawOptions(new Rect(inRect.x, y, inRect.width, 24f));
        y += 36f;
        DrawGroupSize(new Rect(inRect.x, y, inRect.width, 24f));
        y += 36f;

        if (stats.Count == 0)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 60f), "SGA_NoPawns".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            return;
        }

        var columns = Columns(inRect.width - 16f);
        DrawHeader(new Rect(inRect.x, y, inRect.width - 16f, HeaderHeight), columns);
        y += HeaderHeight;

        var selectedStat = selectedSkill == null ? null : stats.FirstOrDefault(s => s.def == selectedSkill);
        if (selectedStat != null)
        {
            DrawRow(new Rect(inRect.x, y, inRect.width - 16f, RowHeight), selectedStat, columns, false);
            y += RowHeight + 8f;
            DrawRanking(new Rect(inRect.x, y, inRect.width, inRect.yMax - y), selectedStat);
            return;
        }

        var outRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, stats.Count * RowHeight);
        Widgets.BeginScrollView(outRect, ref scroll, viewRect);
        var sorted = Sorted();
        for (int i = 0; i < sorted.Count; i++)
        {
            DrawRow(new Rect(0f, i * RowHeight, viewRect.width, RowHeight), sorted[i], columns, i % 2 == 1);
        }
        Widgets.EndScrollView();
    }

    private void DrawTopBar(Rect rect)
    {
        DrawSkillPicker(new Rect(rect.x, rect.y, 330f, rect.height));

        var refreshRect = new Rect(rect.xMax - 100f - TopRightInset, rect.y, 100f, rect.height);
        if (Widgets.ButtonText(refreshRect, "SGA_Refresh".Translate()))
        {
            Recalculate();
        }

        GUI.color = Color.gray;
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(new Rect(refreshRect.x - 206f, rect.y, 200f, rect.height), "SGA_PawnCount".Translate(pawns.Count));
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }

    private void DrawSkillPicker(Rect rect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        const float labelWidth = 90f;
        Widgets.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), "SGA_SkillLabel".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        string current = selectedSkill == null ? (string)"SGA_AllSkills".Translate() : selectedSkill.skillLabel.CapitalizeFirst();
        var buttonRect = new Rect(rect.x + labelWidth, rect.y, 230f, rect.height);
        if (Widgets.ButtonText(buttonRect, current + "  \u25BE"))
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("SGA_AllSkills".Translate(), () => SelectSkill(null))
            };
            foreach (var s in stats.OrderBy(x => x.def.listOrder))
            {
                var def = s.def;
                options.Add(new FloatMenuOption(def.skillLabel.CapitalizeFirst(), () => SelectSkill(def)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private void ResetView()
    {
        Settings.ResetView();
        SkillGapMod.Save();
        sortKey = SortKey.Gap;
        sortDescending = true;
        rankScroll = Vector2.zero;
        scroll = Vector2.zero;
        Recalculate();
    }

    private void SelectSkill(SkillDef def)
    {
        selectedSkill = def;
        SkillGapMod.Save();
        rankScroll = Vector2.zero;
        scroll = Vector2.zero;
    }

    // Single-skill view: pawns ranked by level (only those at or above the chosen minimum),
    // the pawns used for average/gap highlighted, passion shown by name colour and flame icons.
    private void DrawRanking(Rect rect, SkillStat s)
    {
        Text.Font = GameFont.Small;
        DrawMinLevel(new Rect(rect.x, rect.y, rect.width, 24f));

        GUI.color = Color.gray;
        Widgets.Label(new Rect(rect.x, rect.y + 28f, rect.width, 24f), "SGA_RankingNote".Translate());
        GUI.color = Color.white;

        // ranked is sorted by level, so the pawns at or above the minimum are a prefix of it.
        int shown = 0;
        while (shown < s.ranked.Count && s.ranked[shown].level >= minLevel)
        {
            shown++;
        }

        const float top = 54f;
        var outRect = new Rect(rect.x, rect.y + top, rect.width, rect.height - top);
        if (shown == 0)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(outRect.x, outRect.y, outRect.width, 40f), "SGA_NoPawnsAtLevel".Translate(minLevel));
            Text.Anchor = TextAnchor.UpperLeft;
            return;
        }

        const float rowHeight = 28f;
        const float iconSize = 20f;
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, shown * rowHeight);
        Widgets.BeginScrollView(outRect, ref rankScroll, viewRect);
        for (int i = 0; i < shown; i++)
        {
            var (pawn, level) = s.ranked[i];
            var row = new Rect(0f, i * rowHeight, viewRect.width, rowHeight);
            bool inGroup = i < s.groupSize;
            if (inGroup)
            {
                Widgets.DrawBoxSolid(row, new Color(0.35f, 0.7f, 0.4f, 0.18f));
            }
            else if (i % 2 == 1)
            {
                Widgets.DrawLightHighlight(row);
            }

            var passion = pawn.skills?.GetSkill(s.def)?.passion ?? Passion.None;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x + 6f, row.y, 30f, row.height), (i + 1).ToString());

            GUI.color = PassionColor(passion);
            float nameWidth = 170f;
            Widgets.Label(new Rect(row.x + 40f, row.y, nameWidth, row.height), pawn.LabelShortCap.Truncate(nameWidth));
            GUI.color = Color.white;

            // The game's major-passion icon already shows two flames, so one icon per pawn is enough.
            if (passion != Passion.None)
            {
                GUI.DrawTexture(new Rect(row.x + 214f, row.y + (row.height - iconSize) / 2f, iconSize, iconSize),
                    passion == Passion.Major ? SkillUI.PassionMajorIcon : SkillUI.PassionMinorIcon);
            }

            Widgets.Label(new Rect(row.x + 250f, row.y, 30f, row.height), level.ToString());
            var bar = new Rect(row.x + 286f, row.y + 8f, row.width - 296f, row.height - 16f);
            Widgets.DrawBoxSolid(bar, new Color(1f, 1f, 1f, 0.08f));
            Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(level / (float)MaxLevel), bar.height),
                new Color(0.55f, 0.65f, 0.85f));
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.DrawHighlightIfMouseover(row);
            string fullName = pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelCap;
            TooltipHandler.TipRegion(row, fullName + " - " + passion.GetLabel() + "\n\n" + "SGA_ClickToJump".Translate(pawn.LabelShort));
            if (Widgets.ButtonInvisible(row))
            {
                CameraJumper.TryJumpAndSelect(pawn);
            }
        }
        Widgets.EndScrollView();
    }

    // No passion keeps the default colour; the two passion levels are told apart by blue strength
    // (kept bright enough to read on the game's dark UI).
    private static Color PassionColor(Passion passion) => passion switch
    {
        Passion.Major => new Color(0.3f, 0.5f, 1f),
        Passion.Minor => new Color(0.65f, 0.85f, 1f),
        _ => Color.white,
    };

    private void DrawMinLevel(Rect rect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        const float labelWidth = 170f;
        Widgets.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), "SGA_MinLevelLabel".Translate());
        TooltipHandler.TipRegion(new Rect(rect.x, rect.y, labelWidth + 100f, rect.height), "SGA_MinLevelTip".Translate());

        int newLevel = minLevel;
        float x = rect.x + labelWidth;
        if (Widgets.ButtonText(new Rect(x, rect.y, 26f, rect.height), "-"))
        {
            newLevel--;
        }
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(x + 28f, rect.y, 36f, rect.height), minLevel.ToString());
        Text.Anchor = TextAnchor.MiddleLeft;
        if (Widgets.ButtonText(new Rect(x + 66f, rect.y, 26f, rect.height), "+"))
        {
            newLevel++;
        }
        Text.Anchor = TextAnchor.UpperLeft;

        newLevel = Mathf.Clamp(newLevel, 0, MaxLevel);
        if (newLevel != minLevel)
        {
            minLevel = newLevel;
            SkillGapMod.Save();
            rankScroll = Vector2.zero;
        }
    }

    private void DrawOptions(Rect rect)
    {
        bool slaves = includeSlaves;
        bool prisoners = includePrisoners;
        bool children = includeChildren;
        Text.Font = GameFont.Small;
        // Three equal columns spanning the whole row.
        const float columnGap = 12f;
        float columnWidth = (rect.width - columnGap * 2f) / 3f;
        Widgets.CheckboxLabeled(new Rect(rect.x, rect.y, columnWidth, rect.height), "SGA_IncludeSlaves".Translate(), ref slaves, placeCheckboxNearText: true);
        Widgets.CheckboxLabeled(new Rect(rect.x + columnWidth + columnGap, rect.y, columnWidth, rect.height), "SGA_IncludePrisoners".Translate(), ref prisoners, placeCheckboxNearText: true);
        Widgets.CheckboxLabeled(new Rect(rect.x + (columnWidth + columnGap) * 2f, rect.y, columnWidth, rect.height), "SGA_IncludeChildren".Translate(), ref children, placeCheckboxNearText: true);
        if (slaves != includeSlaves || prisoners != includePrisoners || children != includeChildren)
        {
            includeSlaves = slaves;
            includePrisoners = prisoners;
            includeChildren = children;
            SkillGapMod.Save();
            Recalculate();
        }
    }

    private void DrawGroupSize(Rect rect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        const float labelWidth = 170f;
        Widgets.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), "SGA_GroupSizeLabel".Translate());
        TooltipHandler.TipRegion(new Rect(rect.x, rect.y, labelWidth + 100f, rect.height), "SGA_GroupSizeTip".Translate());

        int newSize = groupSize;
        float x = rect.x + labelWidth;
        if (Widgets.ButtonText(new Rect(x, rect.y, 26f, rect.height), "-"))
        {
            newSize--;
        }
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(x + 28f, rect.y, 36f, rect.height), groupSize.ToString());
        Text.Anchor = TextAnchor.MiddleLeft;
        if (Widgets.ButtonText(new Rect(x + 66f, rect.y, 26f, rect.height), "+"))
        {
            newSize++;
        }
        Text.Anchor = TextAnchor.UpperLeft;

        newSize = Mathf.Clamp(newSize, 1, MaxGroupSize);
        if (newSize != groupSize)
        {
            groupSize = newSize;
            SkillGapMod.Save();
            Recalculate();
        }
    }

    // Column rects relative to a row's left edge.
    private static Rect[] Columns(float width)
    {
        float skill = width * 0.17f;
        float top = width * 0.46f;
        float avg = width * 0.11f;
        float gap = width - skill - top - avg;
        float x = 0f;
        var result = new Rect[4];
        float[] widths = { skill, top, avg, gap };
        for (int i = 0; i < 4; i++)
        {
            result[i] = new Rect(x, 0f, widths[i], 0f);
            x += widths[i];
        }
        return result;
    }

    private void DrawHeader(Rect rect, Rect[] columns)
    {
        string[] labels =
        {
            "SGA_ColSkill".Translate(), "SGA_ColTop".Translate(TopShown),
            "SGA_ColAvgTop".Translate(), "SGA_ColGap".Translate()
        };
        var keys = new[] { SortKey.Name, SortKey.Max, SortKey.Avg, SortKey.Gap };

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        for (int i = 0; i < keys.Length; i++)
        {
            var cell = new Rect(rect.x + columns[i].x, rect.y, columns[i].width, rect.height);
            string arrow = sortKey == keys[i] ? (sortDescending ? " \u25BC" : " \u25B2") : "";
            Widgets.Label(new Rect(cell.x + 6f, cell.y, cell.width - 6f, cell.height), labels[i] + arrow);
            Widgets.DrawHighlightIfMouseover(cell);
            if (keys[i] == SortKey.Gap)
            {
                TooltipHandler.TipRegion(cell, "SGA_GroupSizeTip".Translate());
            }
            if (keys[i] == SortKey.Avg)
            {
                TooltipHandler.TipRegion(cell, "SGA_AvgTip".Translate());
            }
            if (Widgets.ButtonInvisible(cell))
            {
                if (sortKey == keys[i])
                {
                    sortDescending = !sortDescending;
                }
                else
                {
                    sortKey = keys[i];
                    sortDescending = keys[i] != SortKey.Name;
                }
            }
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
    }

    private List<SkillStat> Sorted()
    {
        IEnumerable<SkillStat> q = sortKey switch
        {
            SortKey.Name => stats.OrderBy(s => s.def.listOrder),
            SortKey.Max => stats.OrderBy(s => s.max),
            SortKey.Avg => stats.OrderBy(AvgValue),
            _ => stats.OrderBy(GapValue),
        };
        var list = q.ToList();
        if (sortDescending)
        {
            list.Reverse();
        }
        return list;
    }

    private void DrawRow(Rect row, SkillStat s, Rect[] columns, bool alt)
    {
        if (alt)
        {
            Widgets.DrawLightHighlight(row);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.Label(new Rect(row.x + columns[0].x + 6f, row.y, columns[0].width - 6f, row.height), s.def.skillLabel.CapitalizeFirst());
        DrawTopPawns(CellRect(row, columns[1]), s);

        Widgets.Label(new Rect(row.x + columns[2].x + 6f, row.y, columns[2].width - 6f, row.height), AvgValue(s).ToString("F1"));

        // Gap: number + bar scaled to the max level.
        var gapCell = CellRect(row, columns[3]);
        Widgets.Label(new Rect(gapCell.x + 6f, gapCell.y, 28f, gapCell.height), GapValue(s).ToString());
        TooltipHandler.TipRegion(gapCell, "SGA_GapTip".Translate(s.groupSize, s.max, s.topLow));
        var bar = new Rect(gapCell.x + 38f, gapCell.y + 10f, gapCell.width - 48f, row.height - 20f);
        Widgets.DrawBoxSolid(bar, new Color(1f, 1f, 1f, 0.08f));
        float fill = Mathf.Clamp01(GapValue(s) / (float)MaxLevel);
        Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * fill, bar.height), GapColor(fill));

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static float AvgValue(SkillStat s) => s.topAvg;

    private static int GapValue(SkillStat s) => s.TopGap;

    private static Rect CellRect(Rect row, Rect column) => new Rect(row.x + column.x, row.y, column.width, row.height);

    private static Color GapColor(float fill) => Color.Lerp(new Color(0.35f, 0.7f, 0.4f), new Color(0.85f, 0.35f, 0.3f), fill);

    // Top few pawns side by side: "level name" each, name truncated to fit, full name on hover, click to jump.
    private static void DrawTopPawns(Rect cell, SkillStat s)
    {
        int shown = Mathf.Min(TopShown, s.ranked.Count);
        if (shown == 0)
        {
            return;
        }

        const float levelWidth = 22f;
        float entryWidth = (cell.width - 6f) / TopShown;
        for (int i = 0; i < shown; i++)
        {
            var (pawn, level) = s.ranked[i];
            var entry = new Rect(cell.x + 6f + entryWidth * i, cell.y, entryWidth - 4f, cell.height);
            Widgets.Label(new Rect(entry.x, entry.y, levelWidth, entry.height), level.ToString());
            float nameWidth = entry.width - levelWidth - 2f;
            Widgets.Label(new Rect(entry.x + levelWidth, entry.y, nameWidth, entry.height), pawn.LabelShortCap.Truncate(nameWidth));

            Widgets.DrawHighlightIfMouseover(entry);
            string fullName = pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelCap;
            TooltipHandler.TipRegion(entry, fullName + "\n\n" + "SGA_ClickToJump".Translate(pawn.LabelShort));
            if (Widgets.ButtonInvisible(entry))
            {
                CameraJumper.TryJumpAndSelect(pawn);
            }
        }
    }
}
