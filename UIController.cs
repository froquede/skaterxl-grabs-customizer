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
        private bool showMainMenu;
        private Rect MainMenuRect;
        string selected_grab = "";
        int selected_grab_index = 0;
        string[] grabNames = new string[] { "Indy", "Melon", "NoseGrab", "TailGrab", "WeddleGrab", "Stalefish" };

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
                MainMenuRect = GUILayout.Window(420, MainMenuRect, MainMenu, "<b>Grabs Customizer v1.3.0 - discord</b>");
            }
        }

        private void MainMenu(int windowID)
        {
            GUI.backgroundColor = Color.black;
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginVertical("Box");

            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Selected grab:</b>");
            selected_grab_index = RGUI.SelectionPopup(selected_grab_index, grabNames);
            GUILayout.EndHorizontal();
            GUILayout.Label("<b></b>", GUILayout.Height(16f));

            GUILayout.Label("<b>Position offset in world units</b>", GUILayout.Height(32f));
            Vector3 temp_vector_pos = Main.settings.position_offset[selected_grab_index];
            temp_vector_pos.x = RGUI.SliderFloat(temp_vector_pos.x, -20f, 20f, 0f, "Left | Right");
            temp_vector_pos.y = RGUI.SliderFloat(temp_vector_pos.y, -20f, 20f, 0f, "Down | Up");
            temp_vector_pos.z = RGUI.SliderFloat(temp_vector_pos.z, -20f, 20f, 0f, "Backward | Forward");
            Main.settings.position_offset[selected_grab_index] = temp_vector_pos;
            GUILayout.Label("<b></b>", GUILayout.Height(16f));

            GUILayout.Label("<b>Rotation offset in degrees</b>", GUILayout.Height(32f));
            Vector3 temp_vector_rot = Main.settings.rotation_offset[selected_grab_index];
            temp_vector_rot.x = RGUI.SliderFloat(temp_vector_rot.x, -359f, 359f, 0f, "Pitch");
            temp_vector_rot.y = RGUI.SliderFloat(temp_vector_rot.y, -359f, 359f, 0f, "Yaw");
            temp_vector_rot.z = RGUI.SliderFloat(temp_vector_rot.z, -359f, 359f, 0f, "Roll");
            Main.settings.rotation_offset[selected_grab_index] = temp_vector_rot;
            GUILayout.Label("<b></b>", GUILayout.Height(16f));

            if (RGUI.Button(Main.settings.left_foot_speed[selected_grab_index], "Detach <b>Left</b> foot"))
            {
                Main.settings.left_foot_speed[selected_grab_index] = !Main.settings.left_foot_speed[selected_grab_index];
            }
            if (RGUI.Button(Main.settings.right_foot_speed[selected_grab_index], "Detach <b>Right</b> foot"))
            {
                Main.settings.right_foot_speed[selected_grab_index] = !Main.settings.right_foot_speed[selected_grab_index];
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            if (RGUI.Button(Main.settings.BonedGrab, "Customized Grab"))
            {
                Main.settings.BonedGrab = !Main.settings.BonedGrab;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            if (RGUI.Button(Main.settings.continuously_detect, "Continuously detect type of grab"))
            {
                Main.settings.continuously_detect = !Main.settings.continuously_detect;
            }
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
            if(grabNames.ElementAtOrDefault(selected_grab_index) != null) { return grabNames[selected_grab_index]; }
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
