using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Verse;

namespace WorkBoxHighlight;

[StaticConstructorOnStartup]
public static class WBH_Caches
{
    public static Dictionary<Pawn, WorkTypeDef> workTypeDefCache = new Dictionary<Pawn, WorkTypeDef>();

    public static readonly Texture2D WorkBoxBackground = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxHighlight_Background");
    public static readonly Texture2D WorkBoxBorder = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxHighlight_Border");
    public static readonly Texture2D WorkBoxCheck = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxHighlight_Check");

    public static List<Color> colorPresets = new List<Color>()
    {
        ColorLibrary.Black,
        ColorLibrary.White,
        ColorLibrary.Grey,
        ColorLibrary.RedReadable,
        ColorLibrary.Orange,
        ColorLibrary.Yellow,
        ColorLibrary.YellowGreen,
        ColorLibrary.Green,
        ColorLibrary.Cyan,
        ColorLibrary.Blue,
        ColorLibrary.Indigo,
        ColorLibrary.Violet,
        ColorLibrary.Magenta
    };
}
