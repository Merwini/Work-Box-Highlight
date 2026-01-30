using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using UnityEngine.UI;

namespace WorkBoxHighlight;

public class WBH_Settings : ModSettings
{
    internal static Color backgroundColor = ColorLibrary.YellowGreen;

    internal static Color borderColor = ColorLibrary.Green;

    internal static Color checkOrNumberColor = ColorLibrary.Orange;

    public WBH_Mod.TargetElement editTarget = WBH_Mod.TargetElement.Border;
    public string currentHexInput = "420690";
    public string currentError = null;

    public void SyncHexFromPreset()
    {
        Color c = editTarget switch
        {
            WBH_Mod.TargetElement.Background => backgroundColor,
            WBH_Mod.TargetElement.Border => borderColor,
            _ => checkOrNumberColor,
        };

        currentHexInput = WBH_Mod.ToHexReadable(c);
        currentError = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref backgroundColor, "backgroundColor", ColorLibrary.YellowGreen);
        Scribe_Values.Look(ref borderColor, "borderColor", ColorLibrary.Green);
        Scribe_Values.Look(ref checkOrNumberColor, "checkOrNumberColor", ColorLibrary.Orange);
    }
}
