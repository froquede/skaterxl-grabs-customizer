using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RapidGUI;
using UnityEngine;
using UnityModManagerNet;

namespace grabs_customizer
{
    class FoldObj
    {
        public bool enabled;
        public string text;

        public FoldObj(bool enabled, string text)
        {
            this.enabled = enabled;
            this.text = text;
        }
    }

    class UIController : MonoBehaviour
    {
        Color on_press = new Color(46 / 255, 204 / 255, 113 / 255, 1);
        private bool showMainMenu;
        private Rect MainMenuRect;
        string selected_grab = "";
        int selected_grab_index = 0;
        string[] grabNames = new string[] { "Indy", "Melon", "NoseGrab", "TailGrab", "WeddleGrab", "Stalefish" };
        string[] animNames = new string[] { "Disabled", "Falling" };
        bool on_pressed = false;

        private void Start()
        {
            selected_grab = grabNames[0];
            showMainMenu = false;
            MainMenuRect = new Rect(24f, 24f, 100f, 200f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(Main.settings.Hotkey.keyCode))
            {
                if (showMainMenu)
                {
                    Close();
                }
                else
                {
                    Open();
                }
            }
        }

        private void Open()
        {
            showMainMenu = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Close()
        {
            showMainMenu = false;
            Cursor.visible = false;
            Main.settings.Save(Main.modEntry);
        }

        private void OnGUI()
        {
            if (showMainMenu)
            {
                MainMenuRect = GUILayout.Window(420, MainMenuRect, MainMenu, "<b>Grabs Customizer v1.6.0</b>");
            }
        }

        void Fold(FoldObj obj)
        {
            if (GUILayout.Button("<b><size=14><color=#fdcb6e>" + (obj.enabled ? "▶" : "▼") + "</color>" + obj.text + "</size></b>", "Label"))
            {
                obj.enabled = !obj.enabled;
                MainMenuRect.height = 20;
                MainMenuRect.width = Screen.width / 6;
            }
        }

        FoldObj position_fold = new FoldObj(true, "Skate position");
        FoldObj rotation_fold = new FoldObj(true, "Skate rotation");
        FoldObj feet_fold = new FoldObj(true, "Feet options");
        FoldObj options_fold = new FoldObj(true, "Options");

        private void MainMenu(int windowID)
        {
            GUI.backgroundColor = Color.black;
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginVertical("Box");

            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Customize grab:</b>");
            selected_grab_index = RGUI.SelectionPopup(selected_grab_index, grabNames);
            GUILayout.EndHorizontal();

            RGUI.WarningLabelNoStyle("Use this option to create and toggle to a new customized grab stance when the right stick is pressed");
            if (RGUI.Button(on_pressed, "Right stick press stance:"))
            {
                on_pressed = !on_pressed;
            }

            Fold(position_fold);
            if (!position_fold.enabled)
            {
                //GUILayout.Label("Position offset", GUILayout.Height(26f));
                Vector3 temp_vector_pos = !on_pressed ? Main.settings.position_offset[selected_grab_index] : Main.settings.position_offset_onbutton[selected_grab_index];
                temp_vector_pos.x = RGUI.SliderFloat(temp_vector_pos.x, -30f, 30f, 0f, "Left | Right");
                temp_vector_pos.y = RGUI.SliderFloat(temp_vector_pos.y, -40f, 40f, 0f, "Down | Up");
                temp_vector_pos.z = RGUI.SliderFloat(temp_vector_pos.z, -30f, 30f, 0f, "Backward | Forward");
                if (!on_pressed)
                {
                    Main.settings.position_offset[selected_grab_index] = temp_vector_pos;
                }
                else
                {
                    Main.settings.position_offset_onbutton[selected_grab_index] = temp_vector_pos;
                }
            }

            Fold(rotation_fold);
            if (!rotation_fold.enabled)
            {
                //GUILayout.Label("Rotation offset", GUILayout.Height(26f));
                Vector3 temp_vector_rot = !on_pressed ? Main.settings.rotation_offset[selected_grab_index] : Main.settings.rotation_offset_onbutton[selected_grab_index];
                temp_vector_rot.z = RGUI.SliderFloat(temp_vector_rot.z, -180f, 180f, 0f, "Roll");
                temp_vector_rot.x = RGUI.SliderFloat(temp_vector_rot.x, -180f, 180f, 0f, "Pitch");
                temp_vector_rot.y = RGUI.SliderFloat(temp_vector_rot.y, -180f, 180f, 0f, "Yaw");
                if (!on_pressed)
                {
                    Main.settings.rotation_offset[selected_grab_index] = temp_vector_rot;
                }
                else
                {
                    Main.settings.rotation_offset_onbutton[selected_grab_index] = temp_vector_rot;
                }
                //GUILayout.Label("<b></b>", GUILayout.Height(16f));
            }

            Fold(feet_fold);
            if (!feet_fold.enabled)
            {

                if (RGUI.Button(!on_pressed ? Main.settings.left_foot_speed[selected_grab_index] : Main.settings.left_foot_speed_onbutton[selected_grab_index], "Detach <b>left</b> foot"))
                {
                    if (!on_pressed) Main.settings.left_foot_speed[selected_grab_index] = !Main.settings.left_foot_speed[selected_grab_index];
                    else Main.settings.left_foot_speed_onbutton[selected_grab_index] = !Main.settings.left_foot_speed_onbutton[selected_grab_index];
                }


                if (RGUI.Button(!on_pressed ? Main.settings.right_foot_speed[selected_grab_index] : Main.settings.right_foot_speed_onbutton[selected_grab_index], "Detach <b>right</b> foot"))
                {
                    if (!on_pressed) Main.settings.right_foot_speed[selected_grab_index] = !Main.settings.right_foot_speed[selected_grab_index];
                    else Main.settings.right_foot_speed_onbutton[selected_grab_index] = !Main.settings.right_foot_speed_onbutton[selected_grab_index];
                }
            }

            if (!on_pressed)
            {
                Main.settings.animation_length[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_length[selected_grab_index], 1, 60f, 36f, "Animation length (frames)");
            }
            else
            {
                Main.settings.animation_length_onbutton[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_length_onbutton[selected_grab_index], 1, 60f, 36f, "Animation length (frames)");
            }

            /*if (!on_pressed)
            {
                Main.settings.left_foot_weight_speed[selected_grab_index] = RGUI.SliderFloat(Main.settings.left_foot_weight_speed[selected_grab_index], 0, 4f, 1f, "Left foot speed");
            }
            else
            {
                Main.settings.left_foot_weight_speed[selected_grab_index] = RGUI.SliderFloat(Main.settings.left_foot_weight_speed[selected_grab_index], 0, 4f, 1f, "Left foot speed");
            }*/

            /*if (!on_pressed)
            {
                Main.settings.right_foot_weight_speed[selected_grab_index] = RGUI.SliderFloat(Main.settings.right_foot_weight_speed[selected_grab_index], 0, 4f, 1f, "Right foot speed");
            }
            else
            {
                Main.settings.right_foot_weight_speed[selected_grab_index] = RGUI.SliderFloat(Main.settings.right_foot_weight_speed[selected_grab_index], 0, 4f, 1f, "Right foot speed");
            }*/

            /*GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Play animation: (experimental)</b>");
            if(!on_pressed) Main.settings.selected_anim_index[selected_grab_index] = RGUI.SelectionPopup(Main.settings.selected_anim_index[selected_grab_index], animNames);
            else Main.settings.selected_anim_index_onbutton[selected_grab_index] = RGUI.SelectionPopup(Main.settings.selected_anim_index_onbutton[selected_grab_index], animNames);
            GUILayout.EndHorizontal();*/

            /*if (RGUI.Button(Main.settings.hands[selected_grab_index], "Detach Hands"))
            {
                Main.settings.hands[selected_grab_index] = !Main.settings.hands[selected_grab_index];
            }*/

            GUILayout.EndVertical();

            Fold(options_fold);
            if (!options_fold.enabled)
            {
                GUILayout.BeginVertical("Box");
                if (RGUI.Button(Main.settings.BonedGrab, "Customized Grab"))
                {
                    Main.settings.BonedGrab = !Main.settings.BonedGrab;
                }
                /*GUILayout.EndVertical();

                GUILayout.BeginVertical("Box");*/
                if (RGUI.Button(Main.settings.continuously_detect, "Continuously detect type of grab"))
                {
                    Main.settings.continuously_detect = !Main.settings.continuously_detect;
                }
                RGUI.WarningLabelNoStyle("Use the grab hand but tweak to another preset without releasing LB/RB using the sticks");
                /* GUILayout.EndVertical();

                 GUILayout.BeginVertical("Box");*/
                if (RGUI.Button(Main.settings.config_mode, "Config mode"))
                {
                    Main.settings.config_mode = !Main.settings.config_mode;
                }
                RGUI.WarningLabelNoStyle("This option will add constant force upwards after you pop for configuring stances");
                GUILayout.EndVertical();
            }
        }

        public Vector3 getCustomRotation(int grab)
        {
            return Main.settings.rotation_offset[grab];
        }

        public Vector3 getCustomPosition(int grab)
        {
            return Main.settings.position_offset[grab];
        }

        string getNextGrab()
        {
            selected_grab_index++;
            if (grabNames.ElementAtOrDefault(selected_grab_index) != null) { return grabNames[selected_grab_index]; }
            else
            {
                selected_grab_index = 0;
                return grabNames[selected_grab_index];
            }
        }

        private void Title()
        {/*
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b></b>", GUILayout.Height(32f));
            if (GUILayout.Button("<b>X</b>", GUILayout.Height(32f), GUILayout.Width(32f)))
            {
               Close();
            }
            GUILayout.EndHorizontal();*/
        }

        private void OnApplicationQuit()
        {
            Main.settings.Save(Main.modEntry);
        }
    }
}
