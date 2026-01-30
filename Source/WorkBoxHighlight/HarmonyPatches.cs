using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            var dict = WBH_Caches.workTypeDefCache;
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
    public static class WidgetsWork_DrawWorkBoxBackground_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);

            MethodInfo MI_AverageOfRelevantSkillsFor = AccessTools.Method(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.AverageOfRelevantSkillsFor));
            MethodInfo MI_MaxPassionOfRelevantSkillsFor = AccessTools.Method(typeof(Pawn_SkillTracker), nameof(Pawn_SkillTracker.MaxPassionOfRelevantSkillsFor));

            MethodInfo MI_CheckIfPawnIsWorking = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.CheckIfPawnIsWorking));
            MethodInfo MI_DrawWorkingBox = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.DrawWorkingBox));

            int ifIndex = -1;
            int passionIndex = -1;

            Label ifLabel = generator.DefineLabel();
            Label passionLabel = generator.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                /* 
                    Looking for:
	                    IL_0000: ldarg.1 <--- looking for
	                    IL_0001: ldfld class RimWorld.Pawn_SkillTracker Verse.Pawn::skills
	                    IL_0006: ldarg.2
	                    IL_0007: callvirt instance float32 RimWorld.Pawn_SkillTracker::AverageOfRelevantSkillsFor(class Verse.WorkTypeDef) <--- unique call
	                    IL_000c: stloc.0

                    Honestly I could probably just use index 0, but this is a better pattern I think
                 */
                if (ifIndex == -1 && codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo mi1 && mi1 == MI_AverageOfRelevantSkillsFor)
                {
                    // Backtrack from the callvirt to find ldarg.1
                    for (int j = i; j >= 0; j--)
                    {
                        if (codes[j].opcode == OpCodes.Ldarg_1)
                        {
                            ifIndex = j;
                            break;
                        }
                    }
                }

                /*
                    Looking for:
                        IL_010d: ldarg.1 <--- looking for
	                    IL_010e: ldfld class RimWorld.Pawn_SkillTracker Verse.Pawn::skills
	                    IL_0113: ldarg.2
	                    IL_0114: callvirt instance valuetype RimWorld.Passion RimWorld.Pawn_SkillTracker::MaxPassionOfRelevantSkillsFor(class Verse.WorkTypeDef) <--- unique call
	                    IL_0119: stloc.s 4
                 */
                else if (passionIndex == -1 && codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo mi2 && mi2 == MI_MaxPassionOfRelevantSkillsFor)
                {
                    // Backtrack from the callvirt to find ldarg.1
                    for (int j = i; j >= 0; j--)
                    {
                        if (codes[j].opcode == OpCodes.Ldarg_1)
                        {
                            passionIndex = j;
                            break;
                        }
                    }
                }

                else if (ifIndex != -1 && passionIndex != -1)
                {
                    break;
                }
            }

            // Label targets
            codes[ifIndex].labels.Add(ifLabel);
            codes[passionIndex].labels.Add(passionLabel);

            List<CodeInstruction> newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_1), // Load pawn
                new CodeInstruction(OpCodes.Ldarg_2), // Load workDef
                new CodeInstruction(OpCodes.Call, MI_CheckIfPawnIsWorking), // Call method to check if pawn is currently working. Consumes pawn and workDef, returns bool
                new CodeInstruction(OpCodes.Brfalse_S, ifLabel), // If pawn is not working (false on stack), let normal code draw the box
                new CodeInstruction(OpCodes.Ldarg_0), // Load rect
                new CodeInstruction(OpCodes.Call, MI_DrawWorkingBox), // Call method to draw the box myself. Consumes rect, returns void
                new CodeInstruction(OpCodes.Br, passionLabel) // Resume original code to let it draw passion overlay
            };

            // Jam my code at the beginning
            codes.InsertRange(ifIndex, newCodes);

            return codes.AsEnumerable();
        }
    }

    // Returns bool of if pawn is working
    public static bool CheckIfPawnIsWorking(Pawn pawn, WorkTypeDef workDef)
    {
        WBH_Caches.workTypeDefCache.TryGetValue(pawn, out var workType);

        return workType == workDef;
    }

    // Draws background and border for work box
    public static void DrawWorkingBox(Rect rect)
    {
        Color original = GUI.color;

        GUI.color = WBH_Settings.backgroundColor;
        GUI.DrawTexture(rect, WBH_Caches.WorkBoxBackground);

        GUI.color = WBH_Settings.borderColor;
        GUI.DrawTexture(rect, WBH_Caches.WorkBoxBorder);

        GUI.color = original;
    }


    [HarmonyPatch(typeof(WidgetsWork), "DrawWorkBoxFor")]
    public static class WidgetsWork_DrawWorkBoxFor_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);

            FieldInfo FI_WorkBoxCheckTex = AccessTools.Field(typeof(WidgetsWork), nameof(WidgetsWork.WorkBoxCheckTex));
            MethodInfo MI_ColorOfPriority = AccessTools.Method(typeof(WidgetsWork), nameof(WidgetsWork.ColorOfPriority));

            MethodInfo MI_CheckIfPawnIsWorking = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.CheckIfPawnIsWorking));
            MethodInfo MI_DrawWorkingCheck = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.DrawWorkingCheck));
            MethodInfo MI_SetNumberColor = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.SetNumberColor));

            int drawCheckIndex = -1;
            int buttonInvisibleIndex = -1;
            int setPriorityColorIndex = -1;
            int labelNumberIndex = -1;

            Label drawCheckLabel = generator.DefineLabel();
            Label buttonInvisibleLabel = generator.DefineLabel();
            Label setPriorityColorLabel = generator.DefineLabel();
            Label labelNumberLabel = generator.DefineLabel();

            /*
                Looking for:
                    IL_02a7: ldloc.0 <--- looking for
	                IL_02a8: ldsfld class [UnityEngine.CoreModule]UnityEngine.Texture2D RimWorld.WidgetsWork::WorkBoxCheckTex <--- unique field reference
	                IL_02ad: call void [UnityEngine.IMGUIModule]UnityEngine.GUI::DrawTexture(valuetype [UnityEngine.CoreModule]UnityEngine.Rect, class [UnityEngine.CoreModule]UnityEngine.Texture)

                Also looking for:
                    IL_02b2: ldloc.0 <--- looking for
	                IL_02b3: ldc.i4.1
	                IL_02b4: call bool Verse.Widgets::ButtonInvisible(valuetype [UnityEngine.CoreModule]UnityEngine.Rect, bool)
	                IL_02b9: brfalse IL_038a
            */
            for (int i = 0; i < codes.Count; i++)
            {
                if (drawCheckIndex == -1 && codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo fi && fi == FI_WorkBoxCheckTex)
                {
                    for (int j = i; j >= 0; j--)
                    {
                        if (codes[j].opcode == OpCodes.Ldloc_0)
                        {
                            drawCheckIndex = j;
                            break;
                        }
                    }

                    for (int k = i; k < codes.Count; k++)
                    {
                        if (codes[k].opcode == OpCodes.Ldloc_0)
                        {
                            buttonInvisibleIndex = k;
                            break;
                        }
                    }
                }

                /*
                    Looking for:
                        IL_0107: ldloc.3 <--- looking for
	                    IL_0108: call valuetype [UnityEngine.CoreModule]UnityEngine.Color RimWorld.WidgetsWork::ColorOfPriority(int32) <--- unique call
	                    IL_010d: call void [UnityEngine.IMGUIModule]UnityEngine.GUI::set_color(valuetype [UnityEngine.CoreModule]UnityEngine.Color)

                    Also looking for:
                        IL_0112: ldloc.0 <--- looking for
	                    IL_0113: ldc.r4 -3
	                    IL_0118: call valuetype [UnityEngine.CoreModule]UnityEngine.Rect Verse.GenUI::ContractedBy(valuetype [UnityEngine.CoreModule]UnityEngine.Rect, float32)
	                    IL_011d: ldloc.3
	                    IL_011e: call string Verse.GenString::ToStringCached(int32)
	                    IL_0123: call void Verse.Widgets::Label(valuetype [UnityEngine.CoreModule]UnityEngine.Rect, string)
                */

                else if (setPriorityColorIndex == -1 && codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo mi && mi == MI_ColorOfPriority)
                {
                    for (int j = i; j >= 0; j--)
                    {
                        if (codes[j].opcode == OpCodes.Ldloc_3)
                        {
                            setPriorityColorIndex = j;
                            break;
                        }
                    }

                    for (int k = i; k < codes.Count; k++)
                    {
                        if (codes[k].opcode == OpCodes.Ldloc_0)
                        {
                            labelNumberIndex = k;
                            break;
                        }
                    }
                }

                else if (drawCheckIndex != -1 && setPriorityColorIndex != -1)
                {
                    break;
                }
            }

            codes[drawCheckIndex].labels.Add(drawCheckLabel);
            codes[buttonInvisibleIndex].labels.Add(buttonInvisibleLabel);

            codes[setPriorityColorIndex].labels.Add(setPriorityColorLabel);
            codes[labelNumberIndex].labels.Add(labelNumberLabel);

            List<CodeInstruction> checkCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_2), // Load pawn
                new CodeInstruction(OpCodes.Ldarg_3), // Load wType
                new CodeInstruction(OpCodes.Call, MI_CheckIfPawnIsWorking), // Call method to check if pawn is currently working. Consumes pawn and workDef, returns bool
                new CodeInstruction(OpCodes.Brfalse_S, drawCheckLabel), // If pawn is not working (false on stack), let normal code draw the checkmark
                new CodeInstruction(OpCodes.Ldloc_0), // Load rect
                new CodeInstruction(OpCodes.Call, MI_DrawWorkingCheck), // Call method to draw the checkmark myself. Consumes rect, returns void
                new CodeInstruction(OpCodes.Br, buttonInvisibleLabel) // Resume original code to let it draw invisible button for toggling check
            };

            List<CodeInstruction> numberCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_2), // Load pawn
                new CodeInstruction(OpCodes.Ldarg_3), // Load wType
                new CodeInstruction(OpCodes.Call, MI_CheckIfPawnIsWorking), // Call method to check if pawn is currently working. Consumes pawn and workDef, returns bool
                new CodeInstruction(OpCodes.Brfalse_S, setPriorityColorLabel), // If pawn is not working (false on stack), let normal code set the color
                new CodeInstruction(OpCodes.Call, MI_SetNumberColor), // Call method to set color that will be used to draw the priority number
                new CodeInstruction(OpCodes.Br, labelNumberLabel) // Resume original code to let it draw the priority number
            };

            codes.InsertRange(drawCheckIndex, checkCodes); // Do the checkbox first, since the other one would throw off the index for this one
            codes.InsertRange(setPriorityColorIndex, numberCodes);

            return codes.AsEnumerable();
        }
    }

    public static void DrawWorkingCheck(Rect rect)
    {
        Color original = GUI.color;

        GUI.color = WBH_Settings.checkOrNumberColor;
        GUI.DrawTexture(rect, WBH_Caches.WorkBoxCheck);

        GUI.color = original;
    }

    public static void SetNumberColor()
    {
        GUI.color = WBH_Settings.checkOrNumberColor;
    }

    //[HarmonyPatch(typeof(WidgetsWork), "DrawWorkBoxBackground")]
    //[StaticConstructorOnStartup]
    //public static class WidgetsWork_DrawWorkBoxBackground_Postfix
    //{
    //    public static void Postfix(Rect rect, Pawn p, WorkTypeDef workDef)
    //    {
    //        WBH_Caches.workTypeDefCache.TryGetValue(p, out var workType);

    //        if (workType != workDef)
    //            return;

    //        GUI.DrawTexture(rect.ContractedBy(-2f), WBH_Caches.WorkBoxBorder);
    //    }
    //}
}
