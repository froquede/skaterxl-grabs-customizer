using HarmonyLib;
using System;
using UnityModManagerNet;

namespace grabs_customizer
{
    [HarmonyPatch(typeof(StickInput), "OnStickPressed")]
    class OnStickPressedPatchStickInput
    {
        static bool Prefix(bool right)
        {
            return !Main.BG.IsGrabbing();
        }
    }
}
