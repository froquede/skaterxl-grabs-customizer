using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;
using SkaterXL.Core;

namespace boned_grabs
{
    class BonedGrabs : MonoBehaviour
    {
        public static BonedGrabs Instance { get; private set; }
        public bool run;

        private void Start()
        {
            UnityModManager.Logger.Log("Controller started");
        }

        public void Update()
        {
            if (Main.settings.BonedGrab && (EventManager.Instance.IsGrabbing || PlayerController.Instance.currentStateEnum.ToString() == "Grabs"))
            {
                var rotation = PlayerController.Instance.boardController.gameObject.transform.localRotation;
                PlayerController.Instance.boardController.boardControlTransform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.localPosition, PlayerController.Instance.boardController.boardControlTransform.localPosition + new Vector3(Main.settings.GrabBoardBoned_x, Main.settings.GrabBoardBoned_y, Main.settings.GrabBoardBoned_z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
                Vector3 lerpedRotation = Vector3.Lerp(rotation.eulerAngles, rotation.eulerAngles + new Vector3(Main.settings.GrabBoardBoned_rotation_x, Main.settings.GrabBoardBoned_rotation_y, Main.settings.GrabBoardBoned_rotation_z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
                PlayerController.Instance.boardController.gameObject.transform.localRotation = Quaternion.Euler(lerpedRotation.x, lerpedRotation.y, lerpedRotation.z);

                PlayerController.Instance.ikController.LeftIKWeight(Main.settings.GrabBoardBoned_left_speed);
                PlayerController.Instance.ikController.RightIKWeight(Main.settings.GrabBoardBoned_right_speed);
                /*run = true;*/
            }
            else
            {
                /*if (PlayerController.Instance.currentStateEnum.ToString() == "InAir" && run) {
                    PlayerController.Instance.boardController.boardControlTransform.localPosition = new Vector3(0f,0f,0f);
                    PlayerController.Instance.boardController.boardControlTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    run = false;
                }*/
                PlayerController.Instance.ikController.LeftIKWeight(1f);
                PlayerController.Instance.ikController.RightIKWeight(1f);
            }
        }
    }
}
