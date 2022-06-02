using System;
using UnityEngine;
using UnityModManagerNet;

namespace boned_grabs
{
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw(DrawType.KeyBinding)] public KeyBinding Hotkey = new KeyBinding { keyCode = KeyCode.F6 };
        [Draw(DrawType.KeyBinding)] public KeyBinding RightCtrlkey = new KeyBinding { keyCode = KeyCode.RightControl };
        [Draw(DrawType.KeyBinding)] public KeyBinding LeftCtrlkey = new KeyBinding { keyCode = KeyCode.LeftControl };

        public Color BGColor = new Color(0f,0f,0f);

        public bool BonedGrab = true;

        public float GrabBoardBoned_x = 0f;
        public float GrabBoardBoned_y = 0f;
        public float GrabBoardBoned_z = 0f;

        public float GrabBoardBoned_rotation_x = 0f;
        public float GrabBoardBoned_rotation_y = 0f;
        public float GrabBoardBoned_rotation_z = 0f;

        public float GrabBoardBoned_speed = 6f;

        public float GrabBoardBoned_left_speed = 1f;
        public float GrabBoardBoned_right_speed = 1f;

        public void OnChange()
        {
            throw new NotImplementedException();
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save<Settings>(this, modEntry);
        }
    }
}
