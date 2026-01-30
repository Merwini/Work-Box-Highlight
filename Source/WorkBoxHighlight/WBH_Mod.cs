using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WorkBoxHighlight;

public class WBH_Mod : Mod
{
    public WBH_Settings Settings;

    public enum TargetElement
    {
        Background,
        Border,
        Check
    }

    public WBH_Mod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<WBH_Settings>();
    }

    public override string SettingsCategory() => "Work Box Highlight";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        float curY = inRect.yMin;
        float width = inRect.width;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.xMin, curY, width, 32f), "WBH_SettingsLabel".Translate());
        Text.Font = GameFont.Small;
        curY += 36f;

        // Selector buttons
        Rect targetRow = new Rect(inRect.xMin, curY, width, 32f);
        DrawTargetButtons(targetRow);
        curY += 40f;

        // Hex input
        Rect hexBlockRect = new Rect(inRect.xMin, curY, width, 28f + 2f + 20f);
        float usedHexHeight = DrawHexBlock(hexBlockRect);
        curY += usedHexHeight + 8f;

        // Color presets
        Rect selectorRect = new Rect(inRect.xMin, curY, width, 9999f);
        ref Color selectedColor = ref GetSelectedColorRef();
        bool changed = Widgets.ColorSelector(
            selectorRect,
            ref selectedColor,
            WBH_Caches.colorPresets,
            out float selectorHeight,
            icon: null,
            colorSize: 22,
            colorPadding: 2,
            extraOnGUI: null
        );

        // Update hex if a preset is selected
        if (changed)
        {
            Settings.SyncHexFromPreset();
        }

        curY += selectorHeight + 12f;

        // Preview
        Rect previewRow = new Rect(inRect.xMin, curY, width, 56f);
        DrawPreview(previewRow);
    }

    private void DrawTargetButtons(Rect rect)
    {
        Rect labelRect = rect.LeftPart(0.32f);
        Widgets.Label(labelRect, "WBH_Editing".Translate());

        Rect buttonsRect = rect.RightPart(0.68f);
        float gap = 6f;

        Rect b1 = new Rect(buttonsRect.xMin, buttonsRect.yMin, (buttonsRect.width - gap * 2f) / 3f, buttonsRect.height);
        Rect b2 = new Rect(b1.xMax + gap, buttonsRect.yMin, b1.width, buttonsRect.height);
        Rect b3 = new Rect(b2.xMax + gap, buttonsRect.yMin, b1.width, buttonsRect.height);

        DrawToggleButton(b1, "WBH_Background".Translate(), TargetElement.Background);
        DrawToggleButton(b2, "WBH_Border".Translate(), TargetElement.Border);
        DrawToggleButton(b3, "WBH_Check".Translate(), TargetElement.Check);
    }

    private void DrawToggleButton(Rect rect, string label, TargetElement element)
    {
        bool isActive = Settings.editTarget == element;

        if (isActive)
        {
            Widgets.DrawBoxSolid(rect.ContractedBy(1f), new Color(1f, 1f, 1f, 0.08f));
            Widgets.DrawHighlight(rect);
        }

        if (Widgets.ButtonText(rect, label))
        {
            Settings.editTarget = element;
            Settings.SyncHexFromPreset();
        }
    }

    private float DrawHexBlock(Rect rect)
    {
        const float rowH = 28f;
        const float errH = 20f;
        const float gap = 2f;

        // Input
        Rect row = new Rect(rect.xMin, rect.yMin, rect.width, rowH);
        DrawHexRow(row);
        float used = rowH;

        // Error
        if (!Settings.currentError.NullOrEmpty())
        {
            Rect warn = new Rect(rect.xMin, row.yMax + gap, rect.width, errH);
            GUI.color = ColorLibrary.RedReadable;
            Widgets.Label(warn, Settings.currentError);
            GUI.color = Color.white;

            used += gap + errH;
        }

        return used;
    }

    private void DrawHexRow(Rect rect)
    {
        float gap = 6f;

        // Label
        Rect label = rect.LeftPart(0.18f);
        Widgets.Label(label, "WBH_Hex".Translate());

        Rect rest = rect.RightPart(0.82f);

        // #
        Rect hashRect = new Rect(rest.xMin, rest.yMin, 18f, rest.height);
        Widgets.Label(hashRect, "#"); // No way this needs a Translate, right?

        // Input TextField
        Rect field = new Rect(hashRect.xMax + 2f, rest.yMin, rest.width * 0.50f, rest.height);
        Rect apply = new Rect(field.xMax + gap, rest.yMin, rest.width * 0.18f, rest.height);
        Rect swatch = new Rect(apply.xMax + gap, rest.yMin,
            rest.width - (field.width + apply.width + hashRect.width + gap * 2f),
            rest.height);

        string before = Settings.currentHexInput;
        Settings.currentHexInput = Widgets.TextField(field, Settings.currentHexInput);

        // Capitalize
        if (Settings.currentHexInput != before)
        {
            Settings.currentHexInput = Settings.currentHexInput.ToUpperInvariant();
            Settings.currentError = null;
        }

        // Button
        if (Widgets.ButtonText(apply, "WBH_Apply".Translate()))
        {
            if (!TryParseHexColor(Settings.currentHexInput, out var c, out string err))
            {
                Settings.currentError = err;
            }
            else
            {
                Settings.currentError = null;
                SetSelectedColor(c);
                Settings.SyncHexFromPreset();
            }
        }

        // Show what color is selected
        Widgets.DrawBoxSolid(swatch.ContractedBy(2f), GetSelectedColor());
        Widgets.DrawBox(swatch);

        if (!Settings.currentError.NullOrEmpty())
        {
            TooltipHandler.TipRegion(field, Settings.currentError);
            TooltipHandler.TipRegion(apply, Settings.currentError);

            Rect warn = new Rect(rect.xMin, rect.yMax + 2f, rect.width, 20f);
            GUI.color = ColorLibrary.RedReadable;
            Widgets.Label(warn, Settings.currentError);
            GUI.color = Color.white;
        }
    }

    private void DrawPreview(Rect rect)
    {
        Widgets.DrawMenuSection(rect);

        Rect inner = rect.ContractedBy(8f);

        Rect label = inner.LeftPart(0.55f);
        Widgets.Label(label, "WBH_Preview".Translate());

        Rect box = inner.RightPart(0.40f);
        box.height = 30f;
        box.y = inner.yMin + (inner.height - box.height) / 2f;

        DrawPreviewCheckbox(box, WBH_Settings.backgroundColor, WBH_Settings.borderColor, WBH_Settings.checkOrNumberColor);
    }

    private static void DrawPreviewCheckbox(Rect rect, Color bg, Color border, Color check)
    {
        float size = Mathf.Min(rect.width, rect.height);
        Rect square = new Rect(
            rect.xMin + (rect.width - size) * 0.5f,
            rect.yMin + (rect.height - size) * 0.5f,
            size,
            size
        );

        Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.12f));
        Rect inner = rect.ContractedBy(2f);

        Color old = GUI.color;

        GUI.color = bg;
        if (WBH_Caches.WorkBoxBackground != null)
        {
            GUI.DrawTexture(square, WBH_Caches.WorkBoxBackground, ScaleMode.StretchToFill, true);
        }

        GUI.color = border;
        if (WBH_Caches.WorkBoxBorder != null)
        {
            GUI.DrawTexture(square, WBH_Caches.WorkBoxBorder, ScaleMode.StretchToFill, true);
        }
        
        GUI.color = check;
        if (WBH_Caches.WorkBoxCheck != null)
        {
            GUI.DrawTexture(square, WBH_Caches.WorkBoxCheck, ScaleMode.StretchToFill, true);
        }

        GUI.color = old;

        //TooltipHandler.TipRegion(rect, $"Background: {ToHexReadable(bg)}\nBorder: {ToHexReadable(border)}\nCheck: {ToHexReadable(check)}");
        TooltipHandler.TipRegion(rect, "WBH_PreviewTooltip".Translate(ToHexReadable(bg), ToHexReadable(border), ToHexReadable(check)));
    }

    private ref Color GetSelectedColorRef()
    {
        switch (Settings.editTarget)
        {
            case TargetElement.Background:
                return ref WBH_Settings.backgroundColor;

            case TargetElement.Border:
                return ref WBH_Settings.borderColor;

            case TargetElement.Check:
            default:
                return ref WBH_Settings.checkOrNumberColor;
        }
    }

    private Color GetSelectedColor()
    {
        switch (Settings.editTarget)
        {
            case TargetElement.Background:
                return WBH_Settings.backgroundColor;

            case TargetElement.Border:
                return WBH_Settings.borderColor;

            case TargetElement.Check:
            default:
                return WBH_Settings.checkOrNumberColor;
        }
    }

    private void SetSelectedColor(Color color)
    {
        switch (Settings.editTarget)
        {
            case TargetElement.Background: 
                WBH_Settings.backgroundColor = color;
                break;

            case TargetElement.Border: 
                WBH_Settings.borderColor = color;
                break;

            case TargetElement.Check: 
                WBH_Settings.checkOrNumberColor = color;
                break;
        }
    }

    private static bool TryParseHexColor(string input, out Color color, out string error)
    {
        color = Color.white;
        error = null;

        if (input.NullOrEmpty())
        {
            error = "WBH_Error_NoInput".Translate();
            return false;
        }

        string trimmed = input.Trim();

        if (trimmed.Length != 6)
        {
            error = "WBH_Error_Length".Translate();
            return false;
        }

        if (!uint.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint val))
        {
            error = "WBH_Error_Invalid".Translate();
            return false;
        }

        // Thanks StackOverflow for not making me do a regex
        byte r = (byte)((val >> 16) & 0xFF);
        byte g = (byte)((val >> 8) & 0xFF);
        byte b = (byte)(val & 0xFF);

        color = new Color(r / 255f, g / 255f, b / 255f, 1f);
        return true;
    }

    internal static string ToHexReadable(Color c)
    {
        byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
        byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
        byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);

        return $"{r:X2}{g:X2}{b:X2}";
    }
}
