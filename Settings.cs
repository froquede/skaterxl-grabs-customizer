using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace grabs_customizer
{
    [Serializable]
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw(DrawType.KeyBinding)] public KeyBinding Hotkey = new KeyBinding { keyCode = KeyCode.F4 };

        public bool BonedGrab = true;

        public bool continuously_detect = false;

        public List<Vector3> position_offset = new List<Vector3>();
        public List<Vector3> position_offset_onbutton = new List<Vector3>();
        public List<Vector3> rotation_offset = new List<Vector3>();
        public List<Vector3> rotation_offset_onbutton = new List<Vector3>();

        public float GrabBoardBoned_speed = 3f;
        public float GrabBoardBoned_left_speed = 1f;
        public float GrabBoardBoned_right_speed = 1f;

        public List<bool> left_foot_speed = new List<Boolean>();
        public List<bool> right_foot_speed = new List<Boolean>();
        public List<bool> left_foot_speed_onbutton = new List<Boolean>();
        public List<bool> right_foot_speed_onbutton = new List<Boolean>();

        public List<float> left_foot_weight_speed = new List<float>();
        public List<float> left_foot_weight_speed_onbutton = new List<float>();
        public List<float> right_foot_weight_speed = new List<float>();
        public List<float> right_foot_weight_speed_onbutton = new List<float>();

        public List<bool> hands = new List<Boolean>();

        public List<int> selected_anim_index = new List<int>();
        public List<int> selected_anim_index_onbutton = new List<int>();

        public List<int> animation_length = new List<int>();
        public List<int> animation_length_onbutton = new List<int>();

        public bool config_mode = false;

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
