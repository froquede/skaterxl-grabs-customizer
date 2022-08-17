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
            BG = null;
            UI = null;

            try
            {
                harmonyInstance.UnpatchAll(harmonyInstance.Id);
            }
            catch { }

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

            try
            {
                harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch { }

            /*modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = new Action<UnityModManager.ModEntry>(OnSaveGUI);
            modEntry.OnToggle = new Func<UnityModManager.ModEntry, bool, bool>(OnToggle);*/
            modEntry.OnUnload = Unload;
            Main.modEntry = modEntry;

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

            if (settings.hands.Count == 0)
            {
                settings.hands = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.selected_anim_index.Count == 0)
            {
                settings.selected_anim_index = new List<int>(new int[6]);
                settings.Save(modEntry);
            }

            if (settings.selected_anim_index_onbutton.Count == 0)
            {
                settings.selected_anim_index_onbutton = new List<int>(new int[6]);
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

            if (settings.left_foot_weight_speed.Count == 0)
            {
                settings.left_foot_weight_speed = new List<float> { 1, 1, 1, 1, 1, 1 };
                settings.Save(modEntry);
            }
            if (settings.left_foot_weight_speed_onbutton.Count == 0)
            {
                settings.left_foot_weight_speed_onbutton = new List<float> { 1, 1, 1, 1, 1, 1 };
                settings.Save(modEntry);
            }

            if (settings.right_foot_weight_speed.Count == 0)
            {
                settings.right_foot_weight_speed = new List<float> { 1, 1, 1, 1, 1, 1 };
                settings.Save(modEntry);
            }
            if (settings.right_foot_weight_speed_onbutton.Count == 0)
            {
                settings.right_foot_weight_speed_onbutton = new List<float> { 1, 1, 1, 1, 1, 1 };
                settings.Save(modEntry);
            }
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