﻿using HarmonyLib;
using NebulaWorld.Player;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NebulaPatcher.Patches.Transpiler
{
    [HarmonyPatch(typeof(MechaDroneLogic))]
    class MechaDroneLogic_Transpiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch("UpdateTargets")]
        static IEnumerable<CodeInstruction> UpdateTargets_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var codes = new List<CodeInstruction>(instructions);
            /*
             * Call DroneManager.BroadcastDroneOrder(int droneId, int entityId, int stage) when drone gets new order
             */
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Pop &&
                    codes[i - 1].opcode == OpCodes.Callvirt &&
                    codes[i + 1].opcode == OpCodes.Ldarg_0 &&
                    codes[i - 2].opcode == OpCodes.Ldloc_3)
                {
                    found = true;
                    codes.InsertRange(i + 1, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_S, 11),
                        new CodeInstruction(OpCodes.Ldloc_3),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DroneManager), "BroadcastDroneOrder", new System.Type[] { typeof(int), typeof(int), typeof(int) }))
                        });
                    break;
                }
            }

            if(!found)
                NebulaModel.Logger.Log.Error("UpdateTargets transpiler 1 failed");

            found = false;

            /*
             * Update search for new targets. Do not include those for whose build request was sent and client is waiting for the response from server
             * Change:
                 if (a.sqrMagnitude > this.sqrMinBuildAlt && sqrMagnitude <= num2 && !this.serving.Contains(num4))
             * 
             * To:
                 if (a.sqrMagnitude > this.sqrMinBuildAlt && sqrMagnitude <= num2 && !this.serving.Contains(num4) && !DroneManager.IsPendingBuildRequest(num4) && DroneManager.AmIClosestPlayer(sqrMagnitude, ref a))
             */
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Brtrue &&
                    codes[i - 1].opcode == OpCodes.Callvirt &&
                    codes[i + 1].opcode == OpCodes.Ldloc_S &&
                    codes[i - 2].opcode == OpCodes.Ldloc_S &&
                    codes[i + 2].opcode == OpCodes.Stloc_2)
                {
                    found = true;
                    codes.InsertRange(i + 1, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_S, 6),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DroneManager), nameof(DroneManager.IsPendingBuildRequest), new System.Type[] { typeof(int) })),
                        new CodeInstruction(OpCodes.Brtrue_S, codes[i].operand),
                        new CodeInstruction(OpCodes.Ldloca_S, 7),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DroneManager), nameof(DroneManager.AmIClosestPlayer), new System.Type[] { typeof(Vector3).MakeByRefType() })),
                        new CodeInstruction(OpCodes.Brfalse_S, codes[i].operand),
                        });
                    break;
                }
            }

            if (!found)
                NebulaModel.Logger.Log.Error("UpdateTargets transpiler 2 failed. Mod version not compatible with game version.");

            return codes;
        }

        /*
         * Call DroneManager.BroadcastDroneOrder(int droneId, int entityId, int stage) when drone's stage changes
         */
        [HarmonyTranspiler]
        [HarmonyPatch("UpdateDrones")]
        static IEnumerable<CodeInstruction> UpdateDrones_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Brfalse &&
                    codes[i - 1].opcode == OpCodes.Call &&
                    codes[i + 2].opcode == OpCodes.Ldloc_S &&
                    codes[i + 1].opcode == OpCodes.Ldloc_0 &&
                    codes[i - 2].opcode == OpCodes.Ldloca_S)
                {
                    found = true;
                    codes.InsertRange(i + 1, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_S, 4),
                        new CodeInstruction(OpCodes.Ldloc_S, 6),
                        new CodeInstruction(OpCodes.Ldc_I4_2),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DroneManager), "BroadcastDroneOrder", new System.Type[] { typeof(int), typeof(int), typeof(int) }))
                        });
                    break;
                }
            }

            if (!found)
                NebulaModel.Logger.Log.Error("UpdateDrones transpiler 1 failed. Mod version not compatible with game version.");
            
            found = false;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Br &&
                    codes[i - 1].opcode == OpCodes.Pop &&
                    codes[i + 2].opcode == OpCodes.Ldloc_S &&
                    codes[i + 1].opcode == OpCodes.Ldloc_0 &&
                    codes[i - 2].opcode == OpCodes.Callvirt &&
                    codes[i - 3].opcode == OpCodes.Ldloc_S)
                {
                    found = true;
                    codes.InsertRange(i + 5, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_S, 4),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldloc_S, 4),
                        new CodeInstruction(OpCodes.Ldelema, typeof(MechaDrone)),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MechaDrone), "targetObject")),
                        new CodeInstruction(OpCodes.Ldc_I4_3),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DroneManager), "BroadcastDroneOrder", new System.Type[] { typeof(int), typeof(int), typeof(int) }))
                        });
                    break;
                }
            }

            if (!found)
                NebulaModel.Logger.Log.Error("UpdateDrones transpiler 2 failed. Mod version not compatible with game version.");

            return codes;
        }

        /*
         * Changes
         *     if (vector.sqrMagnitude > this.sqrMinBuildAlt && zero2.sqrMagnitude <= num && sqrMagnitude <= num2 && !this.serving.Contains(num4))
         * To
         *     if (vector.sqrMagnitude > this.sqrMinBuildAlt && zero2.sqrMagnitude <= num && sqrMagnitude <= num2 && !this.serving.Contains(num4) && !DroneManager.IsPendingBuildRequest(num4))
         * To avoid client's drones from trying to target pending request (caused by drone having additional tasks unlocked via the Communication control tech)
        */
        [HarmonyTranspiler]
        [HarmonyPatch("FindNext")]
        static IEnumerable<CodeInstruction> FindNext_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
        {
            var codeMatcher = new CodeMatcher(instructions, iL)
                .MatchForward(false,
                    new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "serving"),
                    new CodeMatch(i => i.IsLdloc()),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Brtrue)
                );

            if(codeMatcher.IsInvalid)
            {
                NebulaModel.Logger.Log.Error("MechaDroneLogic_Transpiler.FindNext failed. Mod version not compatible with game version.");
                return instructions;
            }

            var target = codeMatcher.InstructionAt(1);
            var jump = codeMatcher.InstructionAt(3).operand;
            return codeMatcher
                   .Advance(4)
                   .InsertAndAdvance(target)
                   .InsertAndAdvance(HarmonyLib.Transpilers.EmitDelegate<Func<int, bool>>((targetId) =>
                   {
                       return DroneManager.IsPendingBuildRequest(targetId);
                   }))
                   .Insert(new CodeInstruction(OpCodes.Brtrue, jump))
                   .InstructionEnumeration();
        }
    }
}
