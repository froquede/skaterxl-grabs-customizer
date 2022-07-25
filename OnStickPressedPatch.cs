using HarmonyLib;
using System;
using UnityModManagerNet;

namespace grabs_customizer
{
    [HarmonyPatch(typeof(PlayerState_InAir), "OnStickPressed")]
    class OnStickPressedPatch
    {
        static bool Prefix(bool p_right)
        {
            return !Main.BG.IsGrabbing();
        }
    }
}
