// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation version 3 of the License.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.Collections.Generic;
using System.Reflection.Emit;
using BehaviorTree;
using BepInEx;
using BepInEx.NetLauncher.Common;
using HarmonyLib;
using JumpKing;
using JumpKing.GameManager;
using JumpKing.GameManager.TitleScreen;
using JumpKing.PauseMenu;
using JumpKing.PauseMenu.BT.Actions;
using JumpKing.Util;
using static HarmonyLib.AccessTools;

namespace QuickJumpKing
{
    [BepInAutoPlugin]
    [HarmonyPatch]
    public partial class Plugin : BasePlugin
    {
        public Harmony Harmony { get; } = new(Id);

        public override void Load()
        {
            Harmony.PatchAll();
        }

        [HarmonyPatch(typeof(JumpGame), nameof(JumpGame.MakeBT))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RemoveNexileLogo(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_2),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, Field(typeof(JumpGame), nameof(JumpGame.m_nexile_logo))),
                    new CodeMatch(OpCodes.Callvirt, Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild)))
                )
                .RemoveInstructions(8)
                .InstructionEnumeration();
        }

        [HarmonyPatch(typeof(GameTitleScreen), nameof(GameTitleScreen.MakeBT))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SkipWaitForPressStart(IEnumerable<CodeInstruction> instructions)
        {
            var codeMatcher = new CodeMatcher(instructions);
            var start = codeMatcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Newobj, Constructor(typeof(PauseNode), new[] { typeof(float) })),
                new CodeMatch(OpCodes.Callvirt, Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild)))
            ).Pos;
            var end = codeMatcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Newobj, Constructor(typeof(GameTitleScreen.KillAllStart))),
                new CodeMatch(OpCodes.Callvirt, Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild)))
            ).Pos;
            return codeMatcher.RemoveInstructionsInRange(start, end).InstructionEnumeration();
        }

        [HarmonyPatch(typeof(LogoEntity), nameof(LogoEntity.TOTAL_PLAY_DURATION), MethodType.Getter)]
        [HarmonyPrefix]
        private static bool QuickLogoEntity(out float __result)
        {
            __result = Shake.TotalDuration;
            return false;
        }

        [HarmonyPatch(typeof(LogoEntity), nameof(LogoEntity.MakeBT))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SkipLogoEntityAnimations(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_2),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, Field(typeof(LogoEntity), nameof(LogoEntity.m_move))),
                    new CodeMatch(OpCodes.Callvirt, Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild)))
                )
                .RemoveInstructions(4)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_1),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, Field(typeof(LogoEntity), nameof(LogoEntity.m_fade_out))),
                    new CodeMatch(OpCodes.Callvirt, Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild)))
                )
                .RemoveInstructions(4)
                .InstructionEnumeration();
        }

        [HarmonyPatch(typeof(IntroState), nameof(IntroState.MakeBT))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> RemoveIntroPauses(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldc_R4, 1f),
                    new CodeMatch(OpCodes.Newobj, Constructor(typeof(PauseNode), new[] { typeof(float) })),
                    new CodeMatch(OpCodes.Callvirt, Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild))),
                    new CodeMatch(OpCodes.Dup)
                )
                .RemoveInstructions(8)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Dup),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Callvirt)
                )
                .RemoveInstructions(4)
                .InstructionEnumeration();
        }

        [HarmonyPatch(typeof(MenuFactory), nameof(MenuFactory.CreateGiveUp))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> DontExitGiveUp(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_1),
                    new CodeMatch(OpCodes.Newobj, Constructor(typeof(Quit))),
                    new CodeMatch(OpCodes.Callvirt, Method(typeof(IBTcomposite), nameof(IBTcomposite.AddChild)))
                )
                .Advance(1)
                .SetOperandAndAdvance(Constructor(typeof(ExitToMainMenu)))
                .InstructionEnumeration();
        }
    }
}
