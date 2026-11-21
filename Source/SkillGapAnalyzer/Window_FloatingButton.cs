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

    // Remembered for the session. Saved in settings in step 5. Null = not placed yet.
    private static Vector2? savedPosition;

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
        // so an unplaced button is positioned on its first draw instead.
        var pos = savedPosition ?? Vector2.zero;
        windowRect = new Rect(pos.x, pos.y, ButtonSize, ButtonSize);
    }

    private static Vector2 Clamp(Vector2 pos) => new Vector2(
        Mathf.Clamp(pos.x, 0f, UI.screenWidth - ButtonSize),
        Mathf.Clamp(pos.y, 0f, UI.screenHeight - ButtonSize));

    public override void DoWindowContents(Rect inRect)
    {
        if (savedPosition == null)
        {
            // Default: top right corner of the game screen.
            savedPosition = new Vector2(UI.screenWidth - ButtonSize - EdgeMargin, EdgeMargin);
            windowRect.position = savedPosition.Value;
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
                    savedPosition = windowRect.position;
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
