using HarmonyLib;
using RootMotion.Dynamics;
using SkaterXL.Core;
using System;
using TMPro;
using UnityEngine;
using UnityModManagerNet;
using VacuumBreather;

namespace grabs_customizer
{
    public class BonedGrabs : MonoBehaviour
    {
        public static BonedGrabs Instance { get; private set; }

        LayerMask layerMask = ~(1 << LayerMask.NameToLayer("Skateboard"));

        string last_state;
        bool save_last_pos = true;
        bool debug = false;
        bool determine_grab = true;
        bool on_button = false, left_stick = false;

        int grab_frame = 0;

        GrabType actual_grab;
        Vector3 last_pos;
        Vector3 first_rotation;

        GameObject left_leg;
        GameObject right_leg;
        GameObject left_foot;
        GameObject right_foot;

        bool anim_forced = false;

        private void Start()
        {
            reset();
            Log("Controller started");

            left_leg = GameObject.Find("Skater_UpLeg_l");
            right_leg = GameObject.Find("Skater_UpLeg_r");
            left_foot = GameObject.Find("Skater_foot_l");
            right_foot = GameObject.Find("Skater_foot_r");
        }

        void reset()
        {
            PlayerController.Instance.playerSM.StartSM();
            PlayerController.Instance.respawn.DoRespawn();
        }

        void reset_values()
        {
            switch_origin = false;
            determine_grab = true;
            save_last_pos = true;
            anim_forced = false;
            fakegrab_started = false;
            hand_frame = 0;
            grab_frame = 0;
            leftWeight = 0;
            rightWeight = 0;

            if (last_state == PlayerController.CurrentState.Grabs.ToString() && !PlayerController.Instance.respawn.respawning)
            {
                EventManager.Instance.OnCatched(true, true);
                EventManager.Instance.EndTrickCombo(false, false);
                PlayerController.Instance.EnableArmPhysics();
                PlayerController.Instance.SetHandIKWeight(0f, 0f);
                PlayerController.Instance.ikController.ForceLeftLerpValue(0);
                PlayerController.Instance.ikController.ForceRightLerpValue(0);
                PlayerController.Instance.AnimSetGrabToeside(false);
                PlayerController.Instance.AnimSetGrabHeelside(false);
                PlayerController.Instance.AnimSetGrabNose(false);
                PlayerController.Instance.AnimSetGrabTail(false);
                PlayerController.Instance.AnimSetGrabStale(false);
                PlayerController.Instance.AnimSetGrabMute(false);
                PlayerController.Instance.ikController.LeftIKWeight(1f);
                PlayerController.Instance.ikController.RightIKWeight(1f);
            }
        }

        string[] animNames = new string[] { "Disabled", "Falling", "InAir", "Riding" };
        bool switch_origin = false;
        string actual_input = "";
        Quaternion last_rot;

        private void Update()
        {
            if (!Main.settings.BonedGrab) return;
            if (pelvis == null) getPelvis();

            if (PlayerController.Instance.inputController.player.GetButton("Right Stick Button"))
            {
                if (!on_button)
                {
                    last_input = actual_input;
                    actual_input = "right";
                    grab_frame = 0;
                    switch_origin = true;
                }
                on_button = true;
                left_stick = false;
            }
            else
            {
                if (PlayerController.Instance.inputController.player.GetButton("Left Stick Button"))
                {
                    if (!left_stick)
                    {
                        last_input = actual_input;
                        actual_input = "left";
                        grab_frame = 0;
                        switch_origin = true;
                    }
                    left_stick = true;
                    on_button = false;
                }
                else
                {
                    if (on_button || left_stick)
                    {
                        last_input = actual_input;
                        actual_input = "";
                        grab_frame = 0;
                        switch_origin = false;
                    }
                    on_button = false;
                    left_stick = false;
                }
            }

            if (IsGrabbing())
            {
                PlayerController.Instance.boardController.currentCatchRotationTarget = PlayerController.Instance.boardController.boardTransform.rotation;

                if (determine_grab)
                {
                    actual_grab = DetermineGrab();
                    determine_grab = false;
                }

                PlayerController.Instance.DisableArmPhysics();
                PlayerController.Instance.SetHandIKTarget(actual_grab);
                PlayerController.Instance.CorrectHandIKRotation(backwards);
            }
            else
            {
                leftWeight = Mathf.SmoothStep(leftWeight, 0f, Time.deltaTime);
                rightWeight = Mathf.SmoothStep(rightWeight, 0f, Time.deltaTime);
            }

            if (!IsGrounded())
            {
                last_rot = PlayerController.Instance.boardController.boardRigidbody.transform.rotation;
            }
        }

        public float leftWeight = 0, rightWeight = 0, hand_frame = 0;
        bool fakegrab_started = false;
        bool backwards = false;
        bool pressed = false;
        public Quaternion last_targetRot;
        PidQuaternionController _pidRotController = new PidQuaternionController(8f, 0f, 0.05f);

        public void FixedUpdate()
        {
            if (!Main.settings.BonedGrab) return;

            if (ToggleState() && PlayerController.Instance.inputController.player.GetButtonLongPress("LB") && PlayerController.Instance.inputController.player.GetButtonLongPress("RB"))
            {
                if (!pressed)
                {
                    Main.settings.config_mode = !Main.settings.config_mode;
                    NotificationManager.Instance.ShowNotification($"Grabs config mode { (Main.settings.config_mode ? "enabled" : "disabled") }", 1f, false, NotificationManager.NotificationType.Normal, TextAlignmentOptions.TopRight, 0.1f);
                    pressed = true;
                }
            }
            else
            {
                pressed = false;
            }

            if (Main.settings.config_mode)
            {
                PlayerController.Instance.boardController.boardRigidbody.AddForce(0, -Physics.gravity.y / 70f, 0, ForceMode.Impulse);
                PlayerController.Instance.skaterController.skaterRigidbody.AddForce(0, -Physics.gravity.y / 125f, 0, ForceMode.Impulse);
            }

            if (save_last_pos)
            {
                last_pos = PlayerController.Instance.boardController.boardControlTransform.localPosition;
                first_rotation = PlayerController.Instance.boardController.gameObject.transform.localRotation.eulerAngles;
            }

            if (Main.settings.catch_anytime && !fakegrab_started)
            {
                if (last_state == PlayerController.CurrentState.Pop.ToString() || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.InAir || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Release)
                {
                    if (PlayerController.Instance.inputController.player.GetButton("LB") || PlayerController.Instance.inputController.player.GetButton("RB"))
                    {
                        PlayerController.Instance.boardController.currentRotationTarget = PlayerController.Instance.boardController.gameObject.transform.rotation;
                        PlayerController.Instance.ResetAllAnimationsExceptSpeed();
                        PlayerController.Instance.ToggleFlipColliders(false);
                        PlayerController.Instance.ResetIKOffsets();
                        SetIKOnOff(0.4f);
                        PlayerController.Instance.currentStateEnum = PlayerController.CurrentState.Grabs;
                        EventManager.Instance.OnCatched(true, true);
                        SetBoardBackwards();
                        PlayerController.Instance.CorrectHandIKRotation(backwards);
                        //PlayerController.Instance.boardController.CatchRotation();
                        //PlayerController.Instance.boardController.SetCatchForwardRotation();
                        //PlayerController.Instance.boardController.UpdateBoardPosition();

                        fakegrab_started = true;
                        determine_grab = true;
                    }
                }
            }

            if (IsGrabbing())
            {
                PlayerController.Instance.animationController.ScaleAnimSpeed(1f);

                lastGrabSide = grabSide;
                grabSide = DetermineSide();

                if (lastGrabSide != grabSide && grabSide != GrabSide.Both) actual_grab = DetermineGrab(false);

                save_last_pos = false;

                DoGrabOffsetPosition();
                DoGrabOffsetRotation();
                DoGrabFeetDetachment();
                PlayerController.Instance.SetRotationTarget(true);

                PlayerController.Instance.SetKneeBendWeightManually(Main.settings.kneeBendWeight);
                //PlayerController.Instance.ToggleFlipColliders(false);
                //PlayerController.Instance.DisableArmPhysics();

                float length = getAnimationLength(actual_grab) / 3;
                length = length < 0 ? 0 : length;

                float anim = map01(grab_frame, 0, length);
                anim = anim > 1 ? 1 : anim;

                float hand_anim = map01(grab_frame, 0, Main.settings.hand_animation_length - 1);
                hand_anim = hand_anim > 1 ? 1 : hand_anim;

                if (grabSide == GrabSide.Left || grabSide == GrabSide.Both)
                {
                    leftWeight = Mathf.SmoothStep(leftWeight, 1, hand_anim);
                }
                else
                {
                    leftWeight = Mathf.SmoothStep(leftWeight, 0, hand_anim);
                }

                if (grabSide == GrabSide.Right || grabSide == GrabSide.Both)
                {
                    rightWeight = Mathf.SmoothStep(rightWeight, 1, hand_anim);
                }
                else
                {
                    rightWeight = Mathf.SmoothStep(rightWeight, 0, hand_anim);
                }

                if (Main.BG.leftWeight >= .9f || Main.BG.rightWeight >= .9f)
                {
                    
                }
                else {
                    PlayerController.Instance.boardController.firstVel /= 1.05f;
                    PlayerController.Instance.boardController.thirdVel /= 1.01f;
                    PlayerController.Instance.FlipTrickRotation();
                }

                PlayerController.Instance.ikController.SetHandIKWeight(leftWeight, rightWeight);
                PlayerController.Instance.LerpKneeIkWeight();

                grab_frame++;
                hand_frame++;
            }
            else
            {
                if (last_state == PlayerController.CurrentState.Grabs.ToString() && PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Bailed) PreventBail();
            }

            if (PlayerController.Instance.currentStateEnum != PlayerController.CurrentState.Grabs || (!PlayerController.Instance.inputController.player.GetButton("LB") && !PlayerController.Instance.inputController.player.GetButton("RB")))
            {
                reset_values();
            }

            LogState();
        }

        void SetIKOnOff(float p_value)
        {
            IKController ik = PlayerController.Instance.ikController;
            float random_left = p_value + UnityEngine.Random.Range(-.2f, .2f);
            float random_right = p_value + UnityEngine.Random.Range(-.2f, .2f);

            Traverse.Create(PlayerController.Instance.ikController).Field("_leftPositionWeight").SetValue(random_left);
            Traverse.Create(PlayerController.Instance.ikController).Field("_leftRotationWeight").SetValue(0);
            Traverse.Create(PlayerController.Instance.ikController).Field("_rightPositionWeight").SetValue(random_right);
            Traverse.Create(PlayerController.Instance.ikController).Field("_rightRotationWeight").SetValue(0);

            ik._finalIk.solver.leftFootEffector.positionWeight = random_left;
            ik._finalIk.solver.rightFootEffector.positionWeight = random_right;
            ik._finalIk.solver.rightFootEffector.rotationWeight = ik._finalIk.solver.leftFootEffector.rotationWeight = 0f;
        }

        bool ToggleState()
        {
            return (PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Pushing || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Riding || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Impact);
        }

        void SetBoardBackwards()
        {
            Transform skater = PlayerController.Instance.skaterController.skaterTransform;
            Transform board = PlayerController.Instance.boardController.boardTransform;

            Vector3 upBoardProjected = Vector3.ProjectOnPlane(board.up, skater.forward);
            float boardToSkateRelative = Vector3.Angle(upBoardProjected, skater.up);
            Vector3 projected = Vector3.Cross(board.forward, Vector3.ProjectOnPlane(boardToSkateRelative > 90f ? -board.right : board.right, skater.up));
            projected = Vector3.ProjectOnPlane(projected, skater.right);

            float angleUp = Vector3.Angle(projected, skater.up);
            float angleFwd = Vector3.Angle(projected, skater.forward);

            Log(angleUp + " " + angleFwd + " " + boardToSkateRelative);

            if (angleUp > 30f)
            {
                Vector3 planeNormal = Quaternion.AngleAxis(angleFwd < 90f ? -60f : 60f, skater.right) * skater.up;
                projected = Vector3.ProjectOnPlane(projected, planeNormal);
            }
            if (Vector3.Angle(Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(board.forward, projected), skater.right), projected), PlayerController.Instance.PlayerForward()) < 90f)
            {
                PlayerController.Instance.boardController.IsBoardBackwards = backwards = false;
            }
            else PlayerController.Instance.boardController.IsBoardBackwards = backwards = true;
        }

        private float CompareYAngle(Quaternion originRotation, Quaternion currentRotation)
        {
            // Convert the quaternions to Euler angles
            Vector3 originEulerAngles = originRotation.eulerAngles;
            Vector3 currentEulerAngles = currentRotation.eulerAngles;

            // Normalize Euler angles to -180 to 180 range
            originEulerAngles.x = NormalizeAngle(originEulerAngles.x);
            originEulerAngles.y = NormalizeAngle(originEulerAngles.y);
            originEulerAngles.z = NormalizeAngle(originEulerAngles.z);

            currentEulerAngles.x = NormalizeAngle(currentEulerAngles.x);
            currentEulerAngles.y = NormalizeAngle(currentEulerAngles.y);
            currentEulerAngles.z = NormalizeAngle(currentEulerAngles.z);

            // Set the x and z angles to 0
            originEulerAngles.x = 0;
            originEulerAngles.z = 0;

            currentEulerAngles.x = 0;
            currentEulerAngles.z = 0;

            float yAngleDifference = currentEulerAngles.y - originEulerAngles.y;

            // Account for full rotations
            if (yAngleDifference > 180)
            {
                yAngleDifference -= 360;
            }
            else if (yAngleDifference < -180)
            {
                yAngleDifference += 360;
            }

            return yAngleDifference;
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180)
            {
                angle -= 360;
            }

            while (angle < -180)
            {
                angle += 360;
            }

            return angle;
        }

        public static float map01(float value, float min, float max)
        {
            return (value - min) * 1f / (max - min);
        }

        void PreventBail()
        {
            if (PlayerController.Instance.respawn.respawning) return;
            PlayerController.Instance.respawn.behaviourPuppet.StopAllCoroutines();
            PlayerController.Instance.respawn.behaviourPuppet.unpinnedMuscleKnockout = false;
            PlayerController.Instance.respawn.bail.StopAllCoroutines();
            PlayerController.Instance.CancelRespawnInvoke();
            PlayerController.Instance.CancelInvoke("DoBail");
            PlayerController.Instance.CancelInvoke("DoBail");

            Transform[] componentsInChildren = PlayerController.Instance.ragdollHips.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.layer = LayerUtility.RagdollNoInternalCollision;
            }

            PlayerController.Instance.respawn.behaviourPuppet.BoostImmunity(1000f);
            PlayerController.Instance.respawn.puppetMaster.state = PuppetMaster.State.Alive;
            PlayerController.Instance.animationController.ScaleAnimSpeed(1f);
            PlayerController.Instance.ResetAllAnimations();
            PlayerController.Instance.AnimGrindTransition(false);
            PlayerController.Instance.AnimOllieTransition(false);
            PlayerController.Instance.AnimGrindTransition(false);
            PlayerController.Instance.AnimSetupTransition(false);
            PlayerController.Instance.respawn.behaviourPuppet.masterProps.normalMode = BehaviourPuppet.NormalMode.Unmapped;
            PlayerController.Instance.respawn.bail.bailed = false;

            PlayerController.Instance.skaterController.AddUpwardDisplacement(Time.deltaTime / 1000);
            PlayerController.Instance.skaterController.UpdateSkaterPosFromComPos();
            if (last_state == PlayerController.CurrentState.Grabs.ToString()) EventManager.Instance.ExitGrab();
        }


        private void LogState()
        {
            if (PlayerController.Instance.currentStateEnum.ToString() != last_state)
            {
                Log(PlayerController.Instance.currentStateEnum.ToString());
                last_state = PlayerController.Instance.currentStateEnum.ToString();
            }
        }
        int getLeftWeight()
        {
            if (!on_button)
            {
                if (left_stick) return (Main.settings.detach_feet_leftstick[(int)actual_grab] == "Left" || Main.settings.detach_feet_leftstick[(int)actual_grab] == "Both") ? 0 : 1;
                else return (Main.settings.detach_feet[(int)actual_grab] == "Left" || Main.settings.detach_feet[(int)actual_grab] == "Both") ? 0 : 1;
            }
            else return (Main.settings.detach_feet_onbutton[(int)actual_grab] == "Left" || Main.settings.detach_feet_onbutton[(int)actual_grab] == "Both") ? 0 : 1;
        }

        public int getDetachNoPress_Left(int grab)
        {
            return (Main.settings.detach_feet[grab] == "Left" || Main.settings.detach_feet[grab] == "Both") ? 0 : 1;
        }

        public int getDetachOnButton_Left(int grab)
        {
            return (Main.settings.detach_feet_onbutton[grab] == "Left" || Main.settings.detach_feet_onbutton[grab] == "Both") ? 0 : 1;
        }

        public int getDetachOnLeftStick_Left(int grab)
        {
            return (Main.settings.detach_feet_leftstick[grab] == "Left" || Main.settings.detach_feet_leftstick[grab] == "Both") ? 0 : 1;
        }

        public int getDetachNoPress_Right(int grab)
        {
            return (Main.settings.detach_feet[grab] == "Right" || Main.settings.detach_feet[grab] == "Both") ? 0 : 1;
        }

        public int getDetachOnButton_Right(int grab)
        {
            return (Main.settings.detach_feet_onbutton[grab] == "Right" || Main.settings.detach_feet_onbutton[grab] == "Both") ? 0 : 1;
        }

        public int getDetachOnLeftStick_Right(int grab)
        {
            return (Main.settings.detach_feet_leftstick[grab] == "Right" || Main.settings.detach_feet_leftstick[grab] == "Both") ? 0 : 1;
        }

        int getRightWeight()
        {
            if (!on_button)
            {
                if (left_stick) return (Main.settings.detach_feet_leftstick[(int)actual_grab] == "Right" || Main.settings.detach_feet_leftstick[(int)actual_grab] == "Both") ? 0 : 1;
                else return (Main.settings.detach_feet[(int)actual_grab] == "Right" || Main.settings.detach_feet[(int)actual_grab] == "Both") ? 0 : 1;
            }
            else return (Main.settings.detach_feet_onbutton[(int)actual_grab] == "Right" || Main.settings.detach_feet_onbutton[(int)actual_grab] == "Both") ? 0 : 1;
        }

        void IK()
        {
            PlayerController.Instance.ikController._finalIk.solver.iterations = 8;
            PlayerController.Instance.ikController._finalIk.solver.spineMapping.iterations = 8;
        }

        public float getDetachAnimationLength(GrabType grab)
        {
            return !on_button ? (left_stick ? Main.settings.animation_detach_length_leftstick[(int)grab] : Main.settings.animation_detach_length[(int)grab]) : Main.settings.animation_detach_length_onbutton[(int)grab];
        }

        public float getAnimationLength(GrabType grab)
        {
            return !on_button ? left_stick ? Main.settings.animation_length_leftstick[(int)grab] : Main.settings.animation_length[(int)grab] : Main.settings.animation_length_onbutton[(int)grab];
        }

        public Vector3 getCustomRotation(GrabType grab)
        {
            return !on_button ? left_stick ? Main.settings.rotation_offset_leftstick[(int)grab] : Main.settings.rotation_offset[(int)grab] : Main.settings.rotation_offset_onbutton[(int)grab];
        }

        public Vector3 getCustomRotationNoPress(GrabType grab)
        {
            return Main.settings.rotation_offset[(int)grab];
        }

        public Vector3 getCustomRotationOnButton(GrabType grab)
        {
            return Main.settings.rotation_offset_onbutton[(int)grab];
        }
        public Vector3 getCustomRotationOnLeftStick(GrabType grab)
        {
            return Main.settings.rotation_offset_leftstick[(int)grab];
        }

        public Vector3 getCustomPosition(GrabType grab)
        {
            return !on_button ? left_stick ? Main.settings.position_offset_leftstick[(int)grab] : Main.settings.position_offset[(int)grab] : Main.settings.position_offset_onbutton[(int)grab];
        }

        public Vector3 getCustomPositionNoPress(GrabType grab)
        {
            return Main.settings.position_offset[(int)grab];
        }

        public Vector3 getCustomPositionOnButton(GrabType grab)
        {
            return Main.settings.position_offset_onbutton[(int)grab];
        }

        public Vector3 getCustomPositionOnLeftStick(GrabType grab)
        {
            return Main.settings.position_offset_leftstick[(int)grab];
        }

        public void DoGrabOffsetPosition()
        {
            float anim = map01(grab_frame, 0, getAnimationLength(actual_grab));
            anim = anim > 1 ? 1 : anim;
            Vector3 offset = getCustomPosition(actual_grab);
            offset = offset * .035f;

            Vector3 origin = Vector3.zero;
            if (switch_origin)
            {
                origin = getCustomPositionNoPress(actual_grab) / 12;
                if (last_input == "right") origin = getCustomPositionOnButton(actual_grab) / 12;
                if (last_input == "left") origin = getCustomPositionOnLeftStick(actual_grab) / 12;
            }

            Vector3 target = Vector3.Slerp(origin, new Vector3(offset.x, offset.y, offset.z), anim);

            PlayerController.Instance.boardController.boardControlTransform.transform.Translate(target, Space.Self);
        }

        string last_input = "";
        public void DoGrabOffsetRotation()
        {
            if (Main.settings.continuously_detect) actual_grab = DetermineGrab();
            IK();

            Vector3 lerpedRotation;
            Vector3 offset = getCustomRotation(actual_grab);
            offset = offset / 12;

            float anim = map01(grab_frame, 0, getAnimationLength(actual_grab));
            anim = anim > 1 ? 1 : anim;

            Vector3 origin = Vector3.zero;
            if (switch_origin)
            {
                origin = getCustomRotationNoPress(actual_grab) / 12;
                if (last_input == "right") origin = getCustomRotationOnButton(actual_grab) / 12;
                if (last_input == "left") origin = getCustomRotationOnLeftStick(actual_grab) / 12;
            }

            if (backwards)
            {
                lerpedRotation = Vector3.Slerp(origin, new Vector3(-offset.x, offset.y, -offset.z), anim);
            }
            else
            {
                lerpedRotation = Vector3.Slerp(origin, new Vector3(offset.x, offset.y, offset.z), anim);
            }

            PlayerController.Instance.boardController.gameObject.transform.Rotate(lerpedRotation, Space.Self);
            PlayerController.Instance.boardController.UpdateBoardPosition();

            // PlayerController.Instance.SetRotationTarget(true);
            PlayerController.Instance.ScalePlayerCollider();
            //PlayerController.Instance.boardController.SetBoardControllerUpVector(PlayerController.Instance.skaterController.skaterTransform.up);
            //PlayerController.Instance.SnapRotation();

            //PlayerController.Instance.comController.UpdateCOM();
        }

        float lerped_left_weight, lerped_right_weight;
        public void DoGrabFeetDetachment()
        {
            float anim_detach = map01(grab_frame, 0, getDetachAnimationLength(actual_grab));
            anim_detach = anim_detach > 1 ? 1 : anim_detach;

            int weight_origin_left = 1, weight_origin_right = 1;

            if (last_input == "" && (on_button || left_stick)) weight_origin_left = getDetachNoPress_Left((int)actual_grab);
            if (last_input == "left") weight_origin_left = getDetachOnLeftStick_Left((int)actual_grab);
            if (last_input == "right") weight_origin_left = getDetachOnButton_Left((int)actual_grab);
            if (last_input == "" && (on_button || left_stick)) weight_origin_right = getDetachNoPress_Right((int)actual_grab);
            if (last_input == "left") weight_origin_right = getDetachOnLeftStick_Right((int)actual_grab);
            if (last_input == "right") weight_origin_right = getDetachOnButton_Right((int)actual_grab);

            lerped_left_weight = Mathf.SmoothStep(weight_origin_left, getLeftWeight(), anim_detach);
            lerped_right_weight = Mathf.SmoothStep(weight_origin_right, getRightWeight(), anim_detach);

            IKController ik = PlayerController.Instance.ikController;

            if (getLeftWeight() == 0)
            {
                Transform right_target = (Transform)Traverse.Create(PlayerController.Instance.ikController).Field("ikAnimLeftFootTarget").GetValue();
                right_target.position = TranslateWithRotation(pelvis.transform.position, GetLeftFootOffset(), pelvis.transform.rotation);

                ik._finalIk.solver.leftFootEffector.positionWeight = Mathf.SmoothStep(ik._finalIk.solver.leftFootEffector.positionWeight, 1f, anim_detach);
                ik._finalIk.solver.leftFootEffector.rotationWeight = Mathf.SmoothStep(ik._finalIk.solver.leftFootEffector.rotationWeight, 0f, anim_detach);

                ik.ForceLeftLerpValue(lerped_left_weight);
                Traverse.Create(PlayerController.Instance.ikController).Field("_leftRotationWeight").SetValue(ik._finalIk.solver.leftFootEffector.rotationWeight);
                Traverse.Create(PlayerController.Instance.ikController).Field("_ikLeftRotLerp").SetValue(ik._finalIk.solver.leftFootEffector.rotationWeight);
            }
            else
            {
                if (leftWeight >= .1f || rightWeight >= .1f) { 
                    ik._finalIk.solver.leftFootEffector.positionWeight = Mathf.SmoothStep(ik._finalIk.solver.leftFootEffector.positionWeight, 1f, Time.smoothDeltaTime * 4f);
                    ik._finalIk.solver.leftFootEffector.rotationWeight = Mathf.SmoothStep(ik._finalIk.solver.leftFootEffector.rotationWeight, 1f, Time.smoothDeltaTime * 4f);
                } 
            }

            if (getRightWeight() == 0)
            {
                Transform right_target = (Transform)Traverse.Create(PlayerController.Instance.ikController).Field("ikAnimRightFootTarget").GetValue();
                right_target.position = TranslateWithRotation(pelvis.transform.position, GetRightFootOffset(), pelvis.transform.rotation);

                ik._finalIk.solver.rightFootEffector.positionWeight = Mathf.SmoothStep(ik._finalIk.solver.rightFootEffector.positionWeight, 1f, anim_detach);
                ik._finalIk.solver.rightFootEffector.rotationWeight = Mathf.SmoothStep(ik._finalIk.solver.rightFootEffector.rotationWeight, 0f, anim_detach);

                ik.ForceRightLerpValue(lerped_right_weight);
                Traverse.Create(PlayerController.Instance.ikController).Field("_rightRotationWeight").SetValue(ik._finalIk.solver.rightFootEffector.rotationWeight);
                Traverse.Create(PlayerController.Instance.ikController).Field("_ikRightRotLerp").SetValue(ik._finalIk.solver.leftFootEffector.rotationWeight);
            }
            else
            {
                if (leftWeight >= .1f || rightWeight >= .1f)
                {
                    ik._finalIk.solver.rightFootEffector.positionWeight = Mathf.SmoothStep(ik._finalIk.solver.rightFootEffector.positionWeight, 1f, Time.smoothDeltaTime * 4f);
                    ik._finalIk.solver.rightFootEffector.rotationWeight = Mathf.SmoothStep(ik._finalIk.solver.rightFootEffector.rotationWeight, 1f, Time.smoothDeltaTime * 4f);
                }
            }
        }

        public static Vector3 TranslateWithRotation(Vector3 input, Vector3 translation, Quaternion rotation)
        {
            Vector3 rotatedTranslation = rotation * translation;
            Vector3 output = input + rotatedTranslation;
            return output;
        }

        Vector3 GetLeftFootOffset()
        {
            return !on_button ? left_stick ? Main.settings.detach_left_leftstick[(int)actual_grab] : Main.settings.detach_left[(int)actual_grab] : Main.settings.detach_left_onbutton[(int)actual_grab];
        }

        Vector3 GetRightFootOffset()
        {
            return !on_button ? left_stick ? Main.settings.detach_right_leftstick[(int)actual_grab] : Main.settings.detach_right[(int)actual_grab] : Main.settings.detach_right_onbutton[(int)actual_grab];
        }

        Transform pelvis;
        void getPelvis()
        {
            Transform parent = PlayerController.Instance.skaterController.gameObject.transform;
            Transform joints = parent.Find("Skater_Joints");
            pelvis = PlayerController.Instance.skaterController.gameObject.transform;
        }


        public void ResetWeights()
        {
        }

        public bool IsInAir() { return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.InAir; }
        public bool IsGrabbing() { return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Grabs || EventManager.Instance.IsGrabbing || fakegrab_started; }

        public bool IsGrounded()
        {
            return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Riding || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Setup || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Manual || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Powerslide || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Pushing || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Braking || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Impact || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Grinding || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.EnterCoping || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.ExitCoping;
        }

        void Log(System.Object text)
        {
            UnityModManager.Logger.Log(text.ToString());
        }

        private string[] grabNames = new string[] { "Nose Grab", "Indy Grab", "Tail Grab", "Melon Grab", "Mute Grab", "Stalefish" };

        void FootplantRaycast(bool left, bool right)
        {
            PlayerController.Instance.skaterController.leftFootCollider.isTrigger = false;
            RaycastHit hit;
            if (Physics.Raycast(left_foot.transform.position, Vector3.down, out hit, 0.056f + 1f, layerMask))
            {
                if (hit.collider.gameObject.name != "Skater_foot_l" && hit.collider.gameObject.name != "Skater_foot_r" && hit.collider.gameObject.layer != LayerMask.NameToLayer("Skateboard") && hit.collider.gameObject.layer != LayerMask.NameToLayer("Character"))
                {
                    Vector3 position = hit.point;
                    left_foot.transform.position = position;
                    position.y += 0.025f;
                    float distance = Vector3.Distance(left_foot.transform.position, position);
                    if (distance <= 0.09f)
                    {
                        PlayerController.Instance.skaterController.skaterRigidbody.velocity = new Vector3(0, 0, 0);
                        PlayerController.Instance.skaterController.skaterRigidbody.useGravity = false;
                        return;
                    }
                }
            }
        }

        public enum GrabSide
        {
            Left,
            Right,
            Both
        }

        public GrabSide grabSide, lastGrabSide;
        private GrabType DetermineGrab(bool determineSide = true)
        {
            GrabType result = GrabType.Indy;
            if (determineSide) grabSide = DetermineSide();

            if (grabSide != GrabSide.Left)
            {
                if (grabSide == GrabSide.Right)
                {
                    if (this.CanGrabNoseOrTail())
                    {
                        if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                        {
                            if (PlayerController.Instance.GetRightForwardAxis() < -0.3f || PlayerController.Instance.GetLeftForwardAxis() < -0.3f)
                            {
                                PlayerController.Instance.AnimSetGrabNose(true);
                                result = GrabType.NoseGrab;
                            }
                            else if (PlayerController.Instance.GetRightForwardAxis() > 0.3f || PlayerController.Instance.GetLeftForwardAxis() > 0.3f)
                            {
                                PlayerController.Instance.AnimSetGrabTail(true);
                                result = GrabType.TailGrab;
                            }
                        }
                        else
                        {
                            PlayerController.Instance.AnimSetGrabNose(true);
                            result = GrabType.NoseGrab;
                        }
                    }
                    else if (this.CanStaleOrMute())
                    {
                        if (SettingsManager.Instance.stance == Stance.Regular)
                        {
                            PlayerController.Instance.AnimSetGrabStale(true);
                            result = GrabType.Stalefish;
                        }
                        else
                        {
                            PlayerController.Instance.AnimSetGrabMute(true);
                            result = GrabType.Mute;
                        }
                    }
                    else if (SettingsManager.Instance.stance == Stance.Regular)
                    {
                        PlayerController.Instance.AnimSetGrabToeside(true);
                        result = GrabType.Indy;
                    }
                    else
                    {
                        PlayerController.Instance.AnimSetGrabHeelside(true);
                        result = GrabType.Melon;
                    }
                }
            }
            else if (this.CanGrabNoseOrTail())
            {
                if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                {
                    PlayerController.Instance.AnimSetGrabNose(true);
                    result = GrabType.NoseGrab;
                }
                else if (PlayerController.Instance.GetLeftForwardAxis() < -0.3f)
                {
                    PlayerController.Instance.AnimSetGrabNose(true);
                    result = GrabType.NoseGrab;
                }
                else if (PlayerController.Instance.GetLeftForwardAxis() > 0.3f)
                {
                    PlayerController.Instance.AnimSetGrabTail(true);
                    result = GrabType.TailGrab;
                }
            }
            else if (this.CanStaleOrMute())
            {
                if (SettingsManager.Instance.stance == Stance.Regular)
                {
                    PlayerController.Instance.AnimSetGrabMute(true);
                    result = GrabType.Mute;
                }
                else
                {
                    PlayerController.Instance.AnimSetGrabStale(true);
                    result = GrabType.Stalefish;
                }
            }
            else if (SettingsManager.Instance.stance == Stance.Regular)
            {
                PlayerController.Instance.AnimSetGrabHeelside(true);
                result = GrabType.Melon;
            }
            else
            {
                PlayerController.Instance.AnimSetGrabToeside(true);
                result = GrabType.Indy;
            }
            return result;
        }

        public string DetermineSideString()
        {
            return DetermineSide().ToString();
        }

        public GrabSide DetermineSide()
        {
            bool left = false;
            bool right = false;
            if (PlayerController.Instance.inputController.player.GetButton("LB")) left = true;
            if (PlayerController.Instance.inputController.player.GetButton("RB")) right = true;

            return left && right ? GrabSide.Both : left ? GrabSide.Left : GrabSide.Right;
        }

        private bool CanGrabNoseOrTail()
        {
            return (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftForwardAxis() + MonoBehaviourSingleton<PlayerController>.Instance.GetRightForwardAxis() < -0.3f || MonoBehaviourSingleton<PlayerController>.Instance.GetLeftForwardAxis() + MonoBehaviourSingleton<PlayerController>.Instance.GetRightForwardAxis() > 0.3f) && Mathf.Abs(MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis()) < 0.4f && Mathf.Abs(MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis()) < 0.4f;
        }

        private bool CanStaleOrMute()
        {
            bool result = false;
            switch (MonoBehaviourSingleton<SettingsManager>.Instance.controlType)
            {
                case ControlType.Same:
                    if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                    {
                        if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() < -0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() > 0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() > 0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() < -0.5f)
                    {
                        result = true;
                    }
                    break;
                case ControlType.Swap:
                    if (!MonoBehaviourSingleton<PlayerController>.Instance.IsSwitch)
                    {
                        if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                        {
                            if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() < -0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() > 0.5f)
                            {
                                result = true;
                            }
                        }
                        else if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() > 0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                    {
                        if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() > 0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() < -0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() > 0.5f)
                    {
                        result = true;
                    }
                    break;
                case ControlType.Simple:
                    if (!MonoBehaviourSingleton<PlayerController>.Instance.IsSwitch)
                    {
                        if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                        {
                            if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() < -0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() > 0.5f)
                            {
                                result = true;
                            }
                        }
                        else if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() > 0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                    {
                        if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() > 0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (MonoBehaviourSingleton<PlayerController>.Instance.GetLeftToeAxis() < -0.5f && MonoBehaviourSingleton<PlayerController>.Instance.GetRightToeAxis() > 0.5f)
                    {
                        result = true;
                    }
                    break;
            }
            return result;
        }
    }
}
