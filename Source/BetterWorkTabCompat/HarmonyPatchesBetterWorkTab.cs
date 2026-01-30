using Better_Work_Tab;
using Better_Work_Tab.UI;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;
using WorkBoxHighlight;

namespace WorkBoxHighlight.Compat
{
    [StaticConstructorOnStartup]
    public class HarmonyPatchesBetterWorkTab
    {
        static HarmonyPatchesBetterWorkTab()
        {
            Harmony harmonyBWT = new Harmony("nuff.WorkBoxHighlight.Compat");
            harmonyBWT.PatchAll();
        }

        [HarmonyPatch(typeof(MainTabWindow_BetterWork), nameof(MainTabWindow_BetterWork.DrawAllRowContent))]
        public static class MainTabWindow_BetterWork_DrawAllRowContent_Prefix
        {
            public static void Prefix(PawnTable table)
            {
                var dict = WBH_Caches.workTypeDefCache;
                dict.Clear();
                List<Pawn> cachedPawns = table.cachedPawns;
                for (int i = 0; i < cachedPawns.Count; i++)
                {
                    Pawn pawn = cachedPawns[i];
                    WorkTypeDef jobWorkType = pawn.CurJob?.workGiverDef?.workType;
                    dict[pawn] = jobWorkType;
                }
            }
        }
    }
}
