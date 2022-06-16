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
            // harmonyInstance.UnpatchAll();
            UnityEngine.Object.Destroy(BG);
            UnityEngine.Object.Destroy(UI);
            BG = null;
            UI = null;

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

            // harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

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

            if (settings.rotation_offset.Count == 0)
            {
                settings.rotation_offset = new List<Vector3>(new Vector3[6]);
                settings.Save(modEntry);
            }

            if (settings.left_foot_speed.Count == 0)
            {
                settings.left_foot_speed = new List<Boolean>(new Boolean[6]);
                settings.Save(modEntry);
            }

            if (settings.right_foot_speed.Count == 0)
            {
                settings.right_foot_speed = new List<Boolean>(new Boolean[6]);
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
            return true;
        }
    }
}