using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WorkBoxHighlight;

[StaticConstructorOnStartup]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        Harmony harmony = new Harmony("nuff.WorkBoxHighlight");

        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(PawnTable), nameof(PawnTable.PawnTableOnGUI))]
    public static class PawnTable_PawnTableOnGUI_Prefix
    {
        public static void Prefix(PawnTable __instance)
        {
            var dict = WorkTypeCache.workTypeDefCache;
            dict.Clear();
            List<Pawn> cachedPawns = __instance.cachedPawns;
            for (int i = 0; i < cachedPawns.Count; i++)
            {
                Pawn pawn = cachedPawns[i];
                WorkTypeDef jobWorkType = pawn.CurJob?.workGiverDef?.workType;
                dict[pawn] = jobWorkType;
            }
        }
    }

    [HarmonyPatch(typeof(WidgetsWork), "DrawWorkBoxBackground")]
    [StaticConstructorOnStartup]
    public static class WidgetsWork_DrawWorkBoxBackground_Postfix
    {
        public static readonly Texture2D WorkBoxOverlay_Working = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxOverlay_Working");

        public static void Postfix(Rect rect, Pawn p, WorkTypeDef workDef)
        {
            WorkTypeCache.workTypeDefCache.TryGetValue(p, out var workType);

            if (workType != workDef)
                return;

            GUI.DrawTexture(rect.ContractedBy(-2f), WorkBoxOverlay_Working);
        }
    }
}
