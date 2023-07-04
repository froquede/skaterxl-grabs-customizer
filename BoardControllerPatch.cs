using HarmonyLib;
using System;
using UnityEngine;

namespace grabs_customizer
{
    [HarmonyPatch(typeof(BoardController), nameof(BoardController.SnapRotation), new Type[] { })]
    class BoardControllerPatch
    {
        static bool Prefix()
        {
            if (Main.BG.IsGrabbing() && Main.settings.BonedGrab)
            {
                if (Main.BG.leftWeight >= .5f || Main.BG.rightWeight >= .5f) return true;
                return false;
            }
            else return true;
        }
    }

    [HarmonyPatch(typeof(BoardController), nameof(BoardController.CatchRotation), new Type[] { })]
    class BoardControllerPatchCatch
    {
        static bool Prefix()
        {
            if (Main.BG.IsGrabbing() && Main.settings.BonedGrab)
            {
                if (Main.BG.leftWeight >= .95f || Main.BG.rightWeight >= .95f) return true;
                return false;
            }
            else return true;
        }
    }
}
