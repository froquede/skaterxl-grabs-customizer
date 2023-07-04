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
    class UIController : MonoBehaviour
    {
        Color on_press = new Color(46 / 255, 204 / 255, 113 / 255, 1);
        private bool showMainMenu;
        private Rect MainMenuRect;
        string selected_grab = "";
        int selected_grab_index = 0;
        string[] grabNames = new string[] { "Indy", "Melon", "NoseGrab", "TailGrab", "WeddleGrab", "Stalefish" };
        string[] animNames = new string[] { "Disabled", "Falling" };
        string[] stances = new string[] { "Default", "On right stick press", "On left stick press" };
        string[] detachOptions = new string[] { "None", "Left", "Right", "Both" };
        bool on_pressed = false, leftstick = false;
        GUIStyle bg = new GUIStyle("Box")
        {
            alignment = TextAnchor.UpperLeft
        };

        GUIStyle options = new GUIStyle("Label") { };
        Texture2D white_texture;

        private void Start()
        {
            selected_grab = grabNames[0];
            showMainMenu = false;

            white_texture = new Texture2D(1, 1);
            white_texture.SetPixels(new[] { new Color(1, 1, 1, 1) });
            white_texture.Apply();

            bg.border = new RectOffset(8, 8, 8, 8);
            bg.padding = new RectOffset(20, 20, 20, 20);
            bg.normal.background = white_texture;
            bg.fontSize = 20;
            options.fontSize = 16;

            MainMenuRect = new Rect(0, 0, 462, Screen.height);
            MainMenuRect = new Rect(-MainMenuRect.width, 0, 462, Screen.height);

            //Screen.SetResolution(720, 1280, FullScreenMode.Windowed);
        }

        private void Update()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown("g"))
            {
                if (showMainMenu)
                {
                    intention_to_close = true;
                }
                else
                {
                    showMainMenu = true;
                    intention_to_close = false;
                }
            }

            if (showMainMenu)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        int frame_open_count = 0;
        private void Open()
        {
            frame_count = 0;
            left = -MainMenuRect.width + ((MainMenuRect.width / 36) * frame_open_count);

            if (frame_open_count <= 36)
            {
                frame_open_count++;
            }
        }

        float left = 0;
        int frame_count = 0;
        bool intention_to_close = false;
        private void Close()
        {
            if (frame_count == 0)
            {
                Cursor.visible = false;
                Main.settings.Save(Main.modEntry);
            }

            if (frame_count >= 36)
            {
                frame_open_count = 0;
                showMainMenu = false;
            }
            else
            {
                left = -(MainMenuRect.width / 36) * frame_count;
                frame_count++;
            }
        }

        private void OnGUI()
        {
            if (showMainMenu)
            {
                MainMenuRect = new Rect(left, 0, 462, Screen.height);
                GUI.backgroundColor = new Color32(18, 23, 29, 230);
                MainMenuRect = GUILayout.Window(420, MainMenuRect, MainMenu, "<color=#FCFFFF><b>Grabs customizer</b></color>", bg);

                if (intention_to_close)
                {
                    if (frame_count >= 0 && frame_count <= 36) Close();
                }
                else if (frame_open_count >= 0 && frame_open_count <= 36) Open();
            }
        }

        string selected_stance = "Default";
        string selected_detach = "None";

        Vector3 temp_vector_detach_left, temp_vector_detach_right;
        Vector3 temp_vector_pos;
        Vector3 temp_vector_rot;
        int last_grab = -1;

        private void MainMenu(int windowID)
        {
            GUI.backgroundColor = Color.black;
            //GUI.DragWindow(new Rect(0, 0, 10000, 20));
            GUILayout.Space(18);
            GUILayout.Label("v1.8.0");
            GUILayout.Space(14);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Grab</b>");
            selected_grab_index = RGUI.SelectionPopup(selected_grab_index, grabNames);
            if (last_grab != selected_grab_index) selected_stance = "Default";
            last_grab = selected_grab_index;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Stance</b>");
            selected_stance = RGUI.SelectionPopup(selected_stance, stances);
            GUILayout.EndHorizontal();

            on_pressed = selected_stance == "On right stick press";
            leftstick = selected_stance == "On left stick press";

            GUILayout.Space(6);

            GUILayout.Label("<b>Position</b>", GUILayout.Height(26f));
            temp_vector_pos = !on_pressed ? leftstick ? Main.settings.position_offset_leftstick[selected_grab_index] : Main.settings.position_offset[selected_grab_index] : Main.settings.position_offset_onbutton[selected_grab_index];
            temp_vector_pos.x = RGUI.SliderFloat(temp_vector_pos.x, -30f, 30f, 0f, "Left | Right");
            temp_vector_pos.y = RGUI.SliderFloat(temp_vector_pos.y, -40f, 40f, 0f, "Down | Up");
            temp_vector_pos.z = RGUI.SliderFloat(temp_vector_pos.z, -30f, 30f, 0f, "Backward | Forward");
            if (!on_pressed)
            {
                if (leftstick) Main.settings.position_offset_leftstick[selected_grab_index] = temp_vector_pos;
                else Main.settings.position_offset[selected_grab_index] = temp_vector_pos;
            }
            else
            {
                Main.settings.position_offset_onbutton[selected_grab_index] = temp_vector_pos;
            }
            GUILayout.Space(6);

            GUILayout.Label("<b>Rotation</b>", GUILayout.Height(26f));
            temp_vector_rot = !on_pressed ? leftstick ? Main.settings.rotation_offset_leftstick[selected_grab_index] : Main.settings.rotation_offset[selected_grab_index] : Main.settings.rotation_offset_onbutton[selected_grab_index];
            temp_vector_rot.z = RGUI.SliderFloat(temp_vector_rot.z, -180f, 180f, 0f, "Roll");
            temp_vector_rot.x = RGUI.SliderFloat(temp_vector_rot.x, -180f, 180f, 0f, "Pitch");
            temp_vector_rot.y = RGUI.SliderFloat(temp_vector_rot.y, -180f, 180f, 0f, "Yaw");
            if (!on_pressed)
            {
                if (leftstick) Main.settings.rotation_offset_leftstick[selected_grab_index] = temp_vector_rot;
                else Main.settings.rotation_offset[selected_grab_index] = temp_vector_rot;
            }
            else
            {
                Main.settings.rotation_offset_onbutton[selected_grab_index] = temp_vector_rot;
            }
            GUILayout.Space(12);

            /*GUILayout.Label("<b>Pelvis rotation</b>", GUILayout.Height(26f));
            Vector3 temp_vector_pelvis_rot = !on_pressed ? Main.settings.pelvis_rotation_offset[selected_grab_index] : Main.settings.pelvis_rotation_offset_onbutton[selected_grab_index];
            temp_vector_pelvis_rot.z = RGUI.SliderFloat(temp_vector_pelvis_rot.z, -180f, 180f, 0f, "Roll");
            temp_vector_pelvis_rot.x = RGUI.SliderFloat(temp_vector_pelvis_rot.x, -180f, 180f, 0f, "Pitch");
            temp_vector_pelvis_rot.y = RGUI.SliderFloat(temp_vector_pelvis_rot.y, -180f, 180f, 0f, "Yaw");
            if (!on_pressed)
            {
                Main.settings.pelvis_rotation_offset[selected_grab_index] = temp_vector_pelvis_rot;
            }
            else
            {
                Main.settings.pelvis_rotation_offset_onbutton[selected_grab_index] = temp_vector_pelvis_rot;
            }
            GUILayout.Space(12);*/

            if (!on_pressed)
            {
                if (leftstick) Main.settings.animation_length_leftstick[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_length_leftstick[selected_grab_index], 1, 60f, 36f, "<b>Grab animation length</b>");
                else Main.settings.animation_length[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_length[selected_grab_index], 1, 60f, 36f, "<b>Grab animation length</b>");
            }
            else
            {
                Main.settings.animation_length_onbutton[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_length_onbutton[selected_grab_index], 1, 60f, 36f, "<b>Grab animation length</b>");
            }

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Detach foot</b>");
            if (!on_pressed)
            {
                if (leftstick) Main.settings.detach_feet_leftstick[selected_grab_index] = RGUI.SelectionPopup(Main.settings.detach_feet_leftstick[selected_grab_index], detachOptions);
                else Main.settings.detach_feet[selected_grab_index] = RGUI.SelectionPopup(Main.settings.detach_feet[selected_grab_index], detachOptions);
            }
            else Main.settings.detach_feet_onbutton[selected_grab_index] = RGUI.SelectionPopup(Main.settings.detach_feet_onbutton[selected_grab_index], detachOptions);
            GUILayout.EndHorizontal();

            if (!on_pressed)
            {
                if (leftstick) Main.settings.animation_detach_length_leftstick[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_detach_length_leftstick[selected_grab_index], 1, 60f, 24f, "<b>Detach animation length</b>");
                else Main.settings.animation_detach_length[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_detach_length[selected_grab_index], 1, 60f, 24f, "<b>Detach animation length</b>");
            }
            else
            {
                Main.settings.animation_detach_length_onbutton[selected_grab_index] = (int)RGUI.SliderFloat(Main.settings.animation_detach_length_onbutton[selected_grab_index], 1, 60f, 24f, "<b>Detach animation length</b>");
            }

            GUILayout.Space(6);

            if ((leftstick && Main.BG.getDetachOnLeftStick_Left(selected_grab_index) == 0) || (on_pressed && Main.BG.getDetachOnButton_Left(selected_grab_index) == 0) || ((!on_pressed && !leftstick) && Main.BG.getDetachNoPress_Left(selected_grab_index) == 0))
            {
                GUILayout.BeginHorizontal();
                //GUILayout.FlexibleSpace();
                GUILayout.Label("<b><color=#bdc3c7>L</color></b>", options, GUILayout.Width(18));
                temp_vector_detach_left = !on_pressed ? leftstick ? Main.settings.detach_left_leftstick[selected_grab_index] : Main.settings.detach_left[selected_grab_index] : Main.settings.detach_left_onbutton[selected_grab_index];
                temp_vector_detach_left.x = RGUI.SliderFloatCompact(temp_vector_detach_left.x, -10f, 10f, 0, "X");
                temp_vector_detach_left.y = RGUI.SliderFloatCompact(temp_vector_detach_left.y, -10f, 10f, -2f, "Y");
                temp_vector_detach_left.z = RGUI.SliderFloatCompact(temp_vector_detach_left.z, -10f, 10f, .5f, "Z");
                if (!on_pressed)
                {
                    if (leftstick) Main.settings.detach_left_leftstick[selected_grab_index] = temp_vector_detach_left;
                    else Main.settings.detach_left[selected_grab_index] = temp_vector_detach_left;
                }
                else
                {
                    Main.settings.detach_left_onbutton[selected_grab_index] = temp_vector_detach_left;
                }

                GUILayout.EndHorizontal();
            }

            if ((leftstick && Main.BG.getDetachOnLeftStick_Right(selected_grab_index) == 0) || (on_pressed && Main.BG.getDetachOnButton_Right(selected_grab_index) == 0) || ((!on_pressed && !leftstick) && Main.BG.getDetachNoPress_Right(selected_grab_index) == 0))
            {
                GUILayout.BeginHorizontal();
                //GUILayout.FlexibleSpace();
                GUILayout.Label("<b><color=#bdc3c7>R</color></b>", options, GUILayout.Width(18));
                temp_vector_detach_right = !on_pressed ? leftstick ? Main.settings.detach_right_leftstick[selected_grab_index] : Main.settings.detach_right[selected_grab_index] : Main.settings.detach_right_onbutton[selected_grab_index];
                temp_vector_detach_right.x = RGUI.SliderFloatCompact(temp_vector_detach_right.x, -10f, 10f, 0, "X");
                temp_vector_detach_right.y = RGUI.SliderFloatCompact(temp_vector_detach_right.y, -10f, 10f, -2f, "Y");
                temp_vector_detach_right.z = RGUI.SliderFloatCompact(temp_vector_detach_right.z, -10f, 10f, -.5f, "Z");
                if (!on_pressed)
                {
                    if (leftstick) Main.settings.detach_right_leftstick[selected_grab_index] = temp_vector_detach_right;
                    else Main.settings.detach_right[selected_grab_index] = temp_vector_detach_right;
                }
                else
                {
                    Main.settings.detach_right_onbutton[selected_grab_index] = temp_vector_detach_right;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.Label("<b><color=#718093>Options</color></b>", options);
            GUILayout.Space(6);

            if (RGUI.Button(Main.settings.BonedGrab, "<color=#718093>Enabled</color>"))
            {
                Main.settings.BonedGrab = !Main.settings.BonedGrab;
            }
            GUILayout.Space(4);
            /*GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");*/

            Main.settings.hand_animation_length = (int)RGUI.SliderFloat(Main.settings.hand_animation_length, 1, 60, 48, "<b><color=#718093>Hand animation length</color></b>");
            Main.settings.kneeBendWeight = RGUI.SliderFloat(Main.settings.kneeBendWeight, 0, 1, .7f, "<b><color=#718093>Knee bend weight</color></b>");
            GUILayout.Space(4);

            if (RGUI.Button(Main.settings.continuously_detect, "<color=#718093>Continuously detect grab</color>"))
            {
                Main.settings.continuously_detect = !Main.settings.continuously_detect;
            }
            GUILayout.Label("<color=#636e72>Use the hand of one grab with all of the other grab stances</color>");

            GUILayout.Space(4);
            /* GUILayout.EndVertical();
             GUILayout.BeginVertical("Box");*/
            if (RGUI.Button(Main.settings.catch_anytime, "<color=#718093>Catch board at any moment</color>"))
            {
                Main.settings.catch_anytime = !Main.settings.catch_anytime;
            }
            GUILayout.Label("<color=#636e72>Use with XXL3 grab delay disabled</color>");

            GUILayout.Space(4);
            /* GUILayout.EndVertical();
             GUILayout.BeginVertical("Box");*/
            if (RGUI.Button(Main.settings.config_mode, "<color=#718093>Config mode</color>"))
            {
                Main.settings.config_mode = !Main.settings.config_mode;
            }
            GUILayout.Label("<color=#636e72>Float after pop for easily configuring stances</color>");

            GUILayout.EndVertical();
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
