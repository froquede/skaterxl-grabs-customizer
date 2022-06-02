using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RapidGUI;
using UnityEngine;
using UnityModManagerNet;

namespace boned_grabs
{
    class UIController : MonoBehaviour
    {
        private bool showMainMenu;
        private Rect MainMenuRect;

        private void Start()
        {
            showMainMenu = false;
            MainMenuRect = new Rect(24f, 24f, 100f, 200f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(Main.settings.Hotkey.keyCode) && (!Input.GetKeyDown(Main.settings.RightCtrlkey.keyCode) && !Input.GetKeyDown(Main.settings.LeftCtrlkey.keyCode)))
            {
                UnityModManager.Logger.Log("Pressed: " + showMainMenu.ToString());
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
                GUI.backgroundColor = Main.settings.BGColor;
                MainMenuRect = GUILayout.Window(420, MainMenuRect, MainMenu, "<b>Boned Grabs v1.1.0</b>");
            }
        }

        private void MainMenu(int windowID)
        {
            GUI.backgroundColor = Color.black;
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            Title();

            GUILayout.BeginVertical("Box");

            if (RGUI.Button(Main.settings.BonedGrab, "Boned Grab"))
            {
                Main.settings.BonedGrab = !Main.settings.BonedGrab;
            }

            GUILayout.EndVertical();

            GUILayout.Label("<b>Board offset in world units</b>", GUILayout.Height(24f));
            GUILayout.BeginVertical("Box");
            Main.settings.GrabBoardBoned_x = RGUI.SliderFloat(Main.settings.GrabBoardBoned_x, -20f, 20f, 0f, "Board Offset X");
            Main.settings.GrabBoardBoned_y = RGUI.SliderFloat(Main.settings.GrabBoardBoned_y, -20f, 20f, 0f, "Board Offset Y");
            Main.settings.GrabBoardBoned_z = RGUI.SliderFloat(Main.settings.GrabBoardBoned_z, -20f, 20f, 0f, "Board Offset Z");
            GUILayout.EndVertical();

            GUILayout.Label("<b>Rotation offset in degrees (-360 to 360)</b>", GUILayout.Height(24f));
            GUILayout.BeginVertical("Box");
            Main.settings.GrabBoardBoned_rotation_x = RGUI.SliderFloat(Main.settings.GrabBoardBoned_rotation_x, -360f, 360f, 0f, "Board Rotation X");
            Main.settings.GrabBoardBoned_rotation_y = RGUI.SliderFloat(Main.settings.GrabBoardBoned_rotation_y, -360f, 360f, 0f, "Board Rotation Y");
            Main.settings.GrabBoardBoned_rotation_z = RGUI.SliderFloat(Main.settings.GrabBoardBoned_rotation_z, -360f, 360f, 0f, "Board Rotation Z");
            GUILayout.EndVertical();

            GUILayout.Label("<b>Interpolation speed multiplier (default is 3)</b>", GUILayout.Height(24f));
            GUILayout.BeginVertical("Box");
            Main.settings.GrabBoardBoned_speed = RGUI.SliderFloat(Main.settings.GrabBoardBoned_speed, .1f, 12f, 3f, "Grab Speed");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            Main.settings.GrabBoardBoned_left_speed = RGUI.SliderFloat(Main.settings.GrabBoardBoned_left_speed, 0f, 100f, 1f, "Left foot to skate speed");
            Main.settings.GrabBoardBoned_right_speed = RGUI.SliderFloat(Main.settings.GrabBoardBoned_right_speed, 0f, 100f, 1f, "Right feet to skate speed");
            GUILayout.EndVertical();
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
