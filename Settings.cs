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
        public List<Vector3> position_offset_leftstick = new List<Vector3>();

        public List<Vector3> rotation_offset = new List<Vector3>();
        public List<Vector3> rotation_offset_onbutton = new List<Vector3>();
        public List<Vector3> rotation_offset_leftstick = new List<Vector3>();

        public float GrabBoardBoned_speed = 3f;
        public float GrabBoardBoned_left_speed = 1f;
        public float GrabBoardBoned_right_speed = 1f;

        public List<bool> left_foot_speed = new List<Boolean>();
        public List<bool> right_foot_speed = new List<Boolean>();
        public List<bool> left_foot_speed_onbutton = new List<Boolean>();
        public List<bool> right_foot_speed_onbutton = new List<Boolean>();
        public List<bool> left_foot_speed_leftstick = new List<Boolean>();
        public List<bool> right_foot_speed_leftstick = new List<Boolean>();

        public List<bool> hands = new List<Boolean>();

        public List<int> animation_length = new List<int>();
        public List<int> animation_length_onbutton = new List<int>();
        public List<int> animation_length_leftstick = new List<int>();
        public List<int> animation_detach_length = new List<int>();
        public List<int> animation_detach_length_onbutton = new List<int>();
        public List<int> animation_detach_length_leftstick = new List<int>();

        public List<string> detach_feet = new List<string>();
        public List<string> detach_feet_onbutton = new List<string>();
        public List<string> detach_feet_leftstick = new List<string>();

        public List<Vector3> detach_left = new List<Vector3>();
        public List<Vector3> detach_left_onbutton = new List<Vector3>();
        public List<Vector3> detach_left_leftstick = new List<Vector3>();

        public List<Vector3> detach_right = new List<Vector3>();
        public List<Vector3> detach_right_onbutton = new List<Vector3>();
        public List<Vector3> detach_right_leftstick = new List<Vector3>();

        public int hand_animation_length = 12;

        public bool config_mode = false;
        public bool catch_anytime = false;

        public float kneeBendWeight = .25f;

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
