using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace boned_grabs
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
            harmonyInstance.UnpatchAll();
            GameObject.Destroy(BG);
            GameObject.Destroy(UI);
            return true;
        }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            harmonyInstance = new Harmony(modEntry.Info.Id);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnUnload = Unload;

            BG = new GameObject().AddComponent<BonedGrabs>();
            UI = new GameObject().AddComponent<UIController>();
            UnityEngine.Object.DontDestroyOnLoad(BG);
            UnityEngine.Object.DontDestroyOnLoad(UI);

            Main.modEntry = modEntry;

            UnityModManager.Logger.Log("Loaded " + modEntry.Info.Id);

            return true;
        }
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Box("<b>Background Color</b>", GUILayout.Height(21f));
            settings.BGColor.r = RapidGUI.RGUI.SliderFloat(settings.BGColor.r, 0f, 1f, 0f, "Red");
            settings.BGColor.g = RapidGUI.RGUI.SliderFloat(settings.BGColor.g, 0f, 1f, 0f, "Green");
            settings.BGColor.b = RapidGUI.RGUI.SliderFloat(settings.BGColor.b, 0f, 1f, 0f, "Blue");
            settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            UnityModManager.Logger.Log("Toggled " + modEntry.Info.Id);
            return true;
        }
    }
}