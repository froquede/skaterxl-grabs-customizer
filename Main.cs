using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace grabs_customizer
{
    [EnableReloading]
    static class Main
    {
        public static Settings settings;
        public static Harmony harmonyInstance;
        public static BonedGrabs BG;
        public static UIController UI;
        public static UnityModManager.ModEntry modEntry;
        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            UnityEngine.Object.Destroy(BG);
            UnityEngine.Object.Destroy(UI);

            harmonyInstance.UnpatchAll(harmonyInstance.Id);

            return true;
        }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            harmonyInstance = new Harmony(modEntry.Info.Id);
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            checkLists(modEntry);

            BG = new GameObject().AddComponent<BonedGrabs>();
            UI = new GameObject().AddComponent<UIController>();
            UnityEngine.Object.DontDestroyOnLoad(BG);
            UnityEngine.Object.DontDestroyOnLoad(UI);

            /*modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = new Action<UnityModManager.ModEntry>(OnSaveGUI);*/
            /*modEntry.OnToggle = OnToggle;*/
            modEntry.OnUnload = Unload;
            Main.modEntry = modEntry;

            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            UnityModManager.Logger.Log("Loaded " + modEntry.Info.Id);
            return true;
        }

        private static void checkLists(UnityModManager.ModEntry modEntry)
        {
            if (settings.position_offset.Count == 0)
            {
                settings.position_offset = new List<Vector3>(new Vector3[6]);
                settings.Save(modEntry);
            }

            if (settings.position_offset_onbutton.Count == 0)
            {
                settings.position_offset_onbutton = new List<Vector3>(new Vector3[6]);
                settings.Save(modEntry);
            }

            if (settings.position_offset_leftstick.Count == 0)
            {
                settings.position_offset_leftstick = new List<Vector3>(new Vector3[6]);
                settings.Save(modEntry);
            }

            if (settings.rotation_offset.Count == 0)
            {
                settings.rotation_offset = new List<Vector3>(new Vector3[6]);
                settings.Save(modEntry);
            }

            if (settings.rotation_offset_onbutton.Count == 0)
            {
                settings.rotation_offset_onbutton = new List<Vector3>(new Vector3[6]);
                settings.Save(modEntry);
            }

            if (settings.rotation_offset_leftstick.Count == 0)
            {
                settings.rotation_offset_leftstick = new List<Vector3>(new Vector3[6]);
                settings.Save(modEntry);
            }

            if (settings.left_foot_speed.Count == 0)
            {
                settings.left_foot_speed = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.left_foot_speed_onbutton.Count == 0)
            {
                settings.left_foot_speed_onbutton = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.left_foot_speed_leftstick.Count == 0)
            {
                settings.left_foot_speed_leftstick = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.right_foot_speed.Count == 0)
            {
                settings.right_foot_speed = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.right_foot_speed_onbutton.Count == 0)
            {
                settings.right_foot_speed_onbutton = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.right_foot_speed_leftstick.Count == 0)
            {
                settings.right_foot_speed_leftstick = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.hands.Count == 0)
            {
                settings.hands = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.animation_length.Count == 0)
            {
                settings.animation_length = new List<int> { 36, 36, 36, 36, 36, 36 };
                settings.Save(modEntry);
            }

            if (settings.animation_length_onbutton.Count == 0)
            {
                settings.animation_length_onbutton = new List<int> { 36, 36, 36, 36, 36, 36 };
                settings.Save(modEntry);
            }

            if (settings.animation_length_leftstick.Count == 0)
            {
                settings.animation_length_leftstick = new List<int> { 36, 36, 36, 36, 36, 36 };
                settings.Save(modEntry);
            }

            if (settings.animation_detach_length.Count == 0)
            {
                settings.animation_detach_length = new List<int> { 24, 24, 24, 24, 24, 24 };
                settings.Save(modEntry);
            }

            if (settings.animation_detach_length_onbutton.Count == 0)
            {
                settings.animation_detach_length_onbutton = new List<int> { 24, 24, 24, 24, 24, 24 };
                settings.Save(modEntry);
            }

            if (settings.animation_detach_length_leftstick.Count == 0)
            {
                settings.animation_detach_length_leftstick = new List<int> { 24, 24, 24, 24, 24, 24 };
                settings.Save(modEntry);
            }

            if (settings.detach_feet.Count == 0)
            {
                settings.detach_feet = new List<string> { getNormalDetach(0), getNormalDetach(1), getNormalDetach(2), getNormalDetach(3), getNormalDetach(4), getNormalDetach(5) };
                settings.Save(modEntry);
            }

            if (settings.detach_feet_onbutton.Count == 0)
            {
                settings.detach_feet_onbutton = new List<string> { getPressedDetach(0), getPressedDetach(1), getPressedDetach(2), getPressedDetach(3), getPressedDetach(4), getPressedDetach(5) };
                settings.Save(modEntry);
            }

            if (settings.detach_feet_leftstick.Count == 0)
            {
                settings.detach_feet_leftstick = new List<string> { "None", "None", "None", "None", "None", "None" };
                settings.Save(modEntry);
            }

            Vector3 detach_default_l = new Vector3(0, -2, .5f);
            if (settings.detach_left.Count == 0)
            {
                settings.detach_left = new List<Vector3>{ detach_default_l, detach_default_l, detach_default_l, detach_default_l, detach_default_l, detach_default_l };
                settings.Save(modEntry);
            }

            if (settings.detach_left_onbutton.Count == 0)
            {
                settings.detach_left_onbutton = new List<Vector3> { detach_default_l, detach_default_l, detach_default_l, detach_default_l, detach_default_l, detach_default_l };
                settings.Save(modEntry);
            }

            if (settings.detach_left_leftstick.Count == 0)
            {
                settings.detach_left_leftstick = new List<Vector3> { detach_default_l, detach_default_l, detach_default_l, detach_default_l, detach_default_l, detach_default_l };
                settings.Save(modEntry);
            }

            Vector3 detach_default_r = new Vector3(0, -2, -.5f);
            if (settings.detach_right.Count == 0)
            {
                settings.detach_right = new List<Vector3> { detach_default_r, detach_default_r, detach_default_r, detach_default_r, detach_default_r, detach_default_r };
                settings.Save(modEntry);
            }

            if (settings.detach_right_onbutton.Count == 0)
            {
                settings.detach_right_onbutton = new List<Vector3> { detach_default_r, detach_default_r, detach_default_r, detach_default_r, detach_default_r, detach_default_r };
                settings.Save(modEntry);
            }

            if (settings.detach_right_leftstick.Count == 0)
            {
                settings.detach_right_leftstick = new List<Vector3> { detach_default_r, detach_default_r, detach_default_r, detach_default_r, detach_default_r, detach_default_r };
                settings.Save(modEntry);
            }
        }

        static string getNormalDetach(int i)
        {
            string result = "None";
            if (settings.left_foot_speed[i]) result = "Left";
            if (settings.right_foot_speed[i]) result = "Right";
            if (settings.left_foot_speed[i] && settings.right_foot_speed[0]) result = "Both";

            return result;
        }

        static string getPressedDetach(int i)
        {
            string result = "None";
            if (settings.left_foot_speed_onbutton[i]) result = "Left";
            if (settings.right_foot_speed_onbutton[i]) result = "Right";
            if (settings.left_foot_speed_onbutton[i] && settings.right_foot_speed_onbutton[0]) result = "Both";

            return result;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {

        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            UnityModManager.Logger.Log("Toggled " + modEntry.Info.Id);

            if (value)
            {
                harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            }
            else
            {
                UnityEngine.Object.Destroy(BG);
                UnityEngine.Object.Destroy(UI);
                harmonyInstance.UnpatchAll(harmonyInstance.Id);
            }

            return true;
        }
    }
}