using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SkillGapAnalyzer;

/// <summary>Standalone (non-tab) window showing the current skill gap, split into combat and life skills.</summary>
public class Window_SkillGap : Window
{
    private enum SortKey { Name, Min, Max, Avg, Gap }

    private const float RowHeight = 34f;
    private const float HeaderHeight = 28f;
    private const float RefreshInterval = 1f;
    private const int MaxLevel = 20;
    private const int MaxGroupSize = 50;

    // Kept between openings during a session. Persisted in settings in a later step.
    private static bool includeSlaves;
    private static bool includePrisoners;
    private static bool includeChildren;
    private static int groupSize = 3;
    private static bool avgOfTop = true;
    private static bool showCombat = true;

    private SortKey sortKey = SortKey.Gap;
    private bool sortDescending = true;
    private Vector2 scroll;
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
        optionalTitle = "SGA_WindowTitle".Translate();
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
        stats = SkillGapCalculator.Compute(showCombat, pawns, groupSize);
        nextRefreshTime = Time.realtimeSinceStartup + RefreshInterval;
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (Time.realtimeSinceStartup >= nextRefreshTime)
        {
            Recalculate();
        }

        float y = inRect.y;
        DrawTopBar(new Rect(inRect.x, y, inRect.width, 30f));
        y += 34f;
        DrawOptions(new Rect(inRect.x, y, inRect.width, 24f));
        y += 28f;
        DrawGroupSize(new Rect(inRect.x, y, inRect.width, 24f));
        y += 28f;

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
        var combatRect = new Rect(rect.x, rect.y, 110f, rect.height);
        var lifeRect = new Rect(combatRect.xMax + 6f, rect.y, 110f, rect.height);
        if (Widgets.ButtonText(combatRect, "SGA_TabCombat".Translate()) && !showCombat)
        {
            showCombat = true;
            scroll = Vector2.zero;
            Recalculate();
        }
        if (Widgets.ButtonText(lifeRect, "SGA_TabLife".Translate()) && showCombat)
        {
            showCombat = false;
            scroll = Vector2.zero;
            Recalculate();
        }
        Widgets.DrawHighlightSelected(showCombat ? combatRect : lifeRect);

        var refreshRect = new Rect(rect.xMax - 100f, rect.y, 100f, rect.height);
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

    private void DrawOptions(Rect rect)
    {
        bool slaves = includeSlaves;
        bool prisoners = includePrisoners;
        bool children = includeChildren;
        Text.Font = GameFont.Small;
        // Three equal columns spanning the whole row.
        const float columnGap = 12f;
        float columnWidth = (rect.width - columnGap * 2f) / 3f;
        Widgets.CheckboxLabeled(new Rect(rect.x, rect.y, columnWidth, rect.height), "SGA_IncludeSlaves".Translate(), ref slaves);
        Widgets.CheckboxLabeled(new Rect(rect.x + columnWidth + columnGap, rect.y, columnWidth, rect.height), "SGA_IncludePrisoners".Translate(), ref prisoners);
        Widgets.CheckboxLabeled(new Rect(rect.x + (columnWidth + columnGap) * 2f, rect.y, columnWidth, rect.height), "SGA_IncludeChildren".Translate(), ref children);
        if (slaves != includeSlaves || prisoners != includePrisoners || children != includeChildren)
        {
            includeSlaves = slaves;
            includePrisoners = prisoners;
            includeChildren = children;
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

        // Which group the "average" column shows.
        var topRect = new Rect(x + 110f, rect.y, 120f, rect.height);
        var bottomRect = new Rect(topRect.xMax + 6f, rect.y, 120f, rect.height);
        if (Widgets.ButtonText(topRect, "SGA_AvgTopButton".Translate()) && !avgOfTop)
        {
            avgOfTop = true;
        }
        if (Widgets.ButtonText(bottomRect, "SGA_AvgBottomButton".Translate()) && avgOfTop)
        {
            avgOfTop = false;
        }
        Widgets.DrawHighlightSelected(avgOfTop ? topRect : bottomRect);
        TooltipHandler.TipRegion(new Rect(topRect.x, rect.y, bottomRect.xMax - topRect.x, rect.height), "SGA_AvgTip".Translate());

        newSize = Mathf.Clamp(newSize, 1, MaxGroupSize);
        if (newSize != groupSize)
        {
            groupSize = newSize;
            Recalculate();
        }
    }

    // Column rects relative to a row's left edge.
    private static Rect[] Columns(float width)
    {
        float skill = width * 0.18f;
        float min = width * 0.24f;
        float max = width * 0.24f;
        float avg = width * 0.12f;
        float gap = width - skill - min - max - avg;
        float x = 0f;
        var result = new Rect[5];
        float[] widths = { skill, min, max, avg, gap };
        for (int i = 0; i < 5; i++)
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
            "SGA_ColSkill".Translate(), "SGA_ColLowest".Translate(), "SGA_ColHighest".Translate(),
            (avgOfTop ? "SGA_ColAvgTop" : "SGA_ColAvgBottom").Translate(), "SGA_ColGap".Translate()
        };
        var keys = new[] { SortKey.Name, SortKey.Min, SortKey.Max, SortKey.Avg, SortKey.Gap };

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        for (int i = 0; i < 5; i++)
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
            SortKey.Min => stats.OrderBy(s => s.min),
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
        DrawPawnCell(CellRect(row, columns[1]), s.min, s.minPawns);
        DrawPawnCell(CellRect(row, columns[2]), s.max, s.maxPawns);

        Widgets.Label(new Rect(row.x + columns[3].x + 6f, row.y, columns[3].width - 6f, row.height), AvgValue(s).ToString("F1"));

        // Gap: number + bar scaled to the max level.
        var gapCell = CellRect(row, columns[4]);
        Widgets.Label(new Rect(gapCell.x + 6f, gapCell.y, 28f, gapCell.height), GapValue(s).ToString());
        TooltipHandler.TipRegion(gapCell, (avgOfTop ? "SGA_GapTipTop" : "SGA_GapTipBottom").Translate(
            s.groupSize, avgOfTop ? s.max : s.bottomHigh, avgOfTop ? s.topLow : s.min));
        var bar = new Rect(gapCell.x + 38f, gapCell.y + 10f, gapCell.width - 48f, row.height - 20f);
        Widgets.DrawBoxSolid(bar, new Color(1f, 1f, 1f, 0.08f));
        float fill = Mathf.Clamp01(GapValue(s) / (float)MaxLevel);
        Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * fill, bar.height), GapColor(fill));

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static float AvgValue(SkillStat s) => avgOfTop ? s.topAvg : s.bottomAvg;

    private static int GapValue(SkillStat s) => avgOfTop ? s.TopGap : s.BottomGap;

    private static Rect CellRect(Rect row, Rect column) => new Rect(row.x + column.x, row.y, column.width, row.height);

    private static Color GapColor(float fill) => Color.Lerp(new Color(0.35f, 0.7f, 0.4f), new Color(0.85f, 0.35f, 0.3f), fill);

    private static void DrawPawnCell(Rect cell, int level, List<Pawn> list)
    {
        string names = list[0].LabelShort;
        if (list.Count > 1)
        {
            names += " " + "SGA_AndMore".Translate(list.Count - 1);
        }

        Widgets.Label(new Rect(cell.x + 6f, cell.y, 26f, cell.height), level.ToString());
        Widgets.Label(new Rect(cell.x + 34f, cell.y, cell.width - 38f, cell.height), names.Truncate(cell.width - 38f));

        Widgets.DrawHighlightIfMouseover(cell);
        TooltipHandler.TipRegion(cell, string.Join(", ", list.Select(p => p.LabelShort)) + "\n\n" + "SGA_ClickToJump".Translate(list[0].LabelShort));
        if (Widgets.ButtonInvisible(cell))
        {
            CameraJumper.TryJumpAndSelect(list[0]);
        }
    }
}
