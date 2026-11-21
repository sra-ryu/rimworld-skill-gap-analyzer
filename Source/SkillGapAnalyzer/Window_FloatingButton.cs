using UnityEngine;
using Verse;

namespace SkillGapAnalyzer;

/// <summary>Small always-visible icon button. Clicking it toggles the skill gap window.</summary>
public class Window_FloatingButton : Window
{
    private const float ButtonSize = 40f;

    // Loaded lazily: FinalizeInit runs on the loading thread, textures may only be loaded on the main thread.
    private static Texture2D icon;
    private static Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get("UI/SkillGapIcon");

    private const float EdgeMargin = 12f;
    private const float DragThreshold = 4f;

    private bool placed;
    private bool pressed;
    private bool dragging;
    private Vector2 pressPosition;

    public override Vector2 InitialSize => new Vector2(ButtonSize, ButtonSize);

    protected override float Margin => 0f;

    public Window_FloatingButton()
    {
        layer = WindowLayer.GameUI;
        doWindowBackground = false;
        drawShadow = false;
        doCloseX = false;
        draggable = true;
        closeOnAccept = false;
        closeOnCancel = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
        focusWhenOpened = false;
        forcePause = false;
        soundAppear = null;
        soundClose = null;
    }

    public static void EnsureOpen()
    {
        if (Find.WindowStack.WindowOfType<Window_FloatingButton>() == null)
        {
            Find.WindowStack.Add(new Window_FloatingButton());
        }
    }

    protected override void SetInitialSizeAndPosition()
    {
        // Screen size is only safe to read on the main thread (this can run while a save is loading),
        // so the real position is applied on the first draw instead.
        windowRect = new Rect(0f, 0f, ButtonSize, ButtonSize);
    }

    private static Vector2 Clamp(Vector2 pos) => new Vector2(
        Mathf.Clamp(pos.x, 0f, UI.screenWidth - ButtonSize),
        Mathf.Clamp(pos.y, 0f, UI.screenHeight - ButtonSize));

    public override void DoWindowContents(Rect inRect)
    {
        if (!placed)
        {
            placed = true;
            var settings = SkillGapMod.Settings;
            // Saved position (kept on screen in case the resolution changed), else the top right corner.
            windowRect.position = settings.hasButtonPosition
                ? Clamp(new Vector2(settings.buttonX, settings.buttonY))
                : new Vector2(UI.screenWidth - ButtonSize - EdgeMargin, EdgeMargin);
        }

        HandleMouse(inRect);

        GUI.color = Mouse.IsOver(inRect) || dragging ? Color.white : new Color(1f, 1f, 1f, 0.8f);
        GUI.DrawTexture(inRect, Icon);
        GUI.color = Color.white;

        if (!dragging)
        {
            Widgets.DrawHighlightIfMouseover(inRect);
            TooltipHandler.TipRegion(inRect, "SGA_ButtonTip".Translate());
        }
    }

    /// <summary>
    /// Click toggles the window. Moving is done by Unity's own GUI.DragWindow (draggable = true), which
    /// updates the window rect the way the base class expects. Events are deliberately not consumed here.
    /// </summary>
    private void HandleMouse(Rect inRect)
    {
        var ev = Event.current;

        if (ev.type == EventType.MouseDown && ev.button == 0 && Mouse.IsOver(inRect))
        {
            pressed = true;
            pressPosition = windowRect.position;
        }
        else if (pressed)
        {
            dragging = (windowRect.position - pressPosition).magnitude > DragThreshold;

            if (ev.type == EventType.MouseUp)
            {
                pressed = false;
                if (dragging)
                {
                    dragging = false;
                    windowRect.position = Clamp(windowRect.position);
                    var settings = SkillGapMod.Settings;
                    settings.hasButtonPosition = true;
                    settings.buttonX = windowRect.position.x;
                    settings.buttonY = windowRect.position.y;
                    SkillGapMod.Save();
                }
                else
                {
                    windowRect.position = pressPosition;
                    Window_SkillGap.Toggle();
                }
            }
        }
    }
}
