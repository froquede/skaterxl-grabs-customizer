using HarmonyLib;
using System;
using UnityModManagerNet;

namespace grabs_customizer
{
    [HarmonyPatch(typeof(PlayerState_Released), "OnStickPressed")]
    class OnStickPressedPatchRelease
    {
        static bool Prefix(bool p_right)
        {
            return !Main.BG.IsGrabbing();
        }
    }
}
