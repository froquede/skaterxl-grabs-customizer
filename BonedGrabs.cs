using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace boned_grabs
{
    class BonedGrabs : MonoBehaviour
    {
        public static BonedGrabs Instance { get; private set; }

        private void Start()
        {
            UnityModManager.Logger.Log("Controller started");
        }

        public void Update()
        {
            if (EventManager.Instance.IsGrabbing && Main.settings.BonedGrab)
            {
                var rotation = PlayerController.Instance.boardController.gameObject.transform.localRotation;
                PlayerController.Instance.boardController.boardControlTransform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.localPosition, PlayerController.Instance.boardController.boardControlTransform.localPosition + new Vector3(Main.settings.GrabBoardBoned_x, Main.settings.GrabBoardBoned_y, Main.settings.GrabBoardBoned_z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
                Vector3 lerpedRotation = Vector3.Lerp(rotation.eulerAngles, rotation.eulerAngles + new Vector3(Main.settings.GrabBoardBoned_rotation_x, Main.settings.GrabBoardBoned_rotation_y, Main.settings.GrabBoardBoned_rotation_z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
                PlayerController.Instance.boardController.gameObject.transform.localRotation = Quaternion.Euler(lerpedRotation.x, lerpedRotation.y, lerpedRotation.z);
            }
        }
    }
}
