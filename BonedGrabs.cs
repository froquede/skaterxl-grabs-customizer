using System;
using UnityEngine;
using UnityModManagerNet;
using SkaterXL.Core;
using RootMotion.Dynamics;
using RootMotion.FinalIK;
using HarmonyLib;

namespace grabs_customizer
{
    public class BonedGrabs : MonoBehaviour
    {
        public static BonedGrabs Instance { get; private set; }

        LayerMask layerMask = ~(1 << LayerMask.NameToLayer("Skateboard"));

        int run;
        int limit = 36;
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
            run = limit + 1;
            PlayerController.Instance.playerSM.StartSM();
            PlayerController.Instance.respawn.DoRespawn();
        }

        void reset_values()
        {
            switch_origin = false;
            determine_grab = true;
            save_last_pos = true;
            anim_forced = false;
        }

        string[] animNames = new string[] { "Disabled", "Falling", "InAir", "Riding" };
        bool switch_origin = false;
        string actual_input = "";

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

            if (!IsGrabbing() && run <= limit)
            {
                if (run == 0)
                {
                    reset_values();

                    EventManager.Instance.ExitGrab();
                    PlayerController.Instance.EnableArmPhysics();
                    PlayerController.Instance.SetHandIKWeight(0f, 0f);
                    PlayerController.Instance.AnimSetGrabToeside(false);
                    PlayerController.Instance.AnimSetGrabHeelside(false);
                    PlayerController.Instance.AnimSetGrabNose(false);
                    PlayerController.Instance.AnimSetGrabTail(false);
                    PlayerController.Instance.AnimSetGrabStale(false);
                    PlayerController.Instance.AnimSetGrabMute(false);
                    PlayerController.Instance.ikController.LeftIKWeight(1f);
                    PlayerController.Instance.ikController.RightIKWeight(1f);
                }
                else
                {
                    if (PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Bailed) PreventBail();
                    PlayerController.Instance.boardController.boardControlTransform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.localPosition, last_pos, map01(run, 1, limit));
                }
            }

            if (IsGrabbing())
            {
                if (determine_grab)
                {
                    actual_grab = DetermineGrab();
                    determine_grab = false;
                    PlayerController.Instance.CorrectHandIKRotation(PlayerController.Instance.boardController.IsBoardBackwards);
                    PlayerController.Instance.DisableArmPhysics();
                    PlayerController.Instance.SetHandIKTarget(actual_grab);
                }
            }
            /*else
            {
                if (IsInAir() && run == 0)
                {
                    PreventInAirBail();
                }
            }*/
        }

        float leftWeight = 0, rightWeight = 0, hand_frame = 0;
        bool fakegrab_started = false;
        public void FixedUpdate()
        {

            if (!Main.settings.BonedGrab) return;

            if (Main.settings.config_mode)
            {
                PlayerController.Instance.boardController.boardRigidbody.AddForce(0, -Physics.gravity.y / 70f, 0, ForceMode.Impulse);
                PlayerController.Instance.skaterController.skaterRigidbody.AddForce(0, -Physics.gravity.y / 125f, 0, ForceMode.Impulse);
            }
            LogState();

            if (save_last_pos)
            {
                last_pos = PlayerController.Instance.boardController.boardControlTransform.localPosition;
                first_rotation = PlayerController.Instance.boardController.gameObject.transform.localRotation.eulerAngles;
            }

            if (Main.settings.catch_anytime && !fakegrab_started)
            {
                if (PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Pop || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.InAir || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Release)
                {
                    if (PlayerController.Instance.inputController.player.GetButton("LB") || PlayerController.Instance.inputController.player.GetButton("RB"))
                    {
                        //UnityModManager.Logger.Log("Fake grab");
                        //actual_grab = DetermineGrab();
                        PlayerController.Instance.boardController.currentRotationTarget = PlayerController.Instance.boardController.gameObject.transform.rotation;
                        PlayerController.Instance.ResetAllAnimationsExceptSpeed();
                        PlayerController.Instance.ToggleFlipColliders(false);
                        //PlayerController.Instance.DisableArmPhysics();
                        PlayerController.Instance.CorrectHandIKRotation(PlayerController.Instance.GetBoardBackwards());
                        PlayerController.Instance.SetIKOnOff(1f);
                        PlayerController.Instance.ResetIKOffsets();
                        PlayerController.Instance.boardController.SetCatchForwardRotation();

                        //EventManager.Instance.StartGrab(actual_grab);
                        PlayerController.Instance.currentStateEnum = PlayerController.CurrentState.Grabs;
                        PlayerController.Instance.currentState = PlayerController.CurrentState.Grabs.ToString();

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

                run = 0;
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

                float hand_anim = map01(grab_frame, 0, Main.settings.hand_animation_length);
                hand_anim = hand_anim > 1 ? 1 : hand_anim;

                if (grabSide == GrabSide.Left || grabSide == GrabSide.Both)
                {
                    leftWeight = Mathf.Lerp(PlayerController.Instance.ikController.leftHandWeight, 1, hand_anim);
                }
                else
                {
                    leftWeight = Mathf.Lerp(PlayerController.Instance.ikController.leftHandWeight, 0, hand_anim);
                }

                if (grabSide == GrabSide.Right || grabSide == GrabSide.Both)
                {
                    rightWeight = Mathf.Lerp(PlayerController.Instance.ikController.rightHandWeight, 1, hand_anim);
                }
                else
                {
                    rightWeight = Mathf.Lerp(PlayerController.Instance.ikController.rightHandWeight, 0, hand_anim);
                }

                PlayerController.Instance.SetHandIKWeight(leftWeight, rightWeight);
                PlayerController.Instance.LerpKneeIkWeight();

                grab_frame++;
                hand_frame++;
            }
            else
            {
                hand_frame = 0;
                grab_frame = 0;
                leftWeight = 0;
                rightWeight = 0;
                fakegrab_started = false;

                if (run <= limit)
                {
                    if (PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Bailed) PreventBail();
                }

                run++;
            }

            if (IsGrounded() && run <= limit)
            {
                reset_values();
                PreventBail();
                run = limit + 2;
            }
        }

        void InAirControl()
        {

            if (SettingsManager.Instance.stance == Stance.Regular)
            {
                PlayerController.Instance.SetBoardTargetPosition(Mathf.Abs(PlayerController.Instance.inputController.LeftStick.rawInput.pos.x) - Mathf.Abs(PlayerController.Instance.inputController.RightStick.rawInput.pos.x));
                PlayerController.Instance.SetFrontPivotRotation(PlayerController.Instance.inputController.RightStick.ToeAxis);
                PlayerController.Instance.SetBackPivotRotation(PlayerController.Instance.inputController.LeftStick.ToeAxis);
                PlayerController.Instance.SetPivotForwardRotation((PlayerController.Instance.inputController.LeftStick.ForwardDir + PlayerController.Instance.inputController.RightStick.ForwardDir) * 0.7f, 15f);
                PlayerController.Instance.SetPivotSideRotation(PlayerController.Instance.inputController.LeftStick.ToeAxis - PlayerController.Instance.inputController.RightStick.ToeAxis);
                return;
            }
            PlayerController.Instance.SetBoardTargetPosition(Mathf.Abs(PlayerController.Instance.inputController.RightStick.rawInput.pos.x) - Mathf.Abs(PlayerController.Instance.inputController.LeftStick.rawInput.pos.x));
            PlayerController.Instance.SetFrontPivotRotation(-PlayerController.Instance.inputController.LeftStick.ToeAxis);
            PlayerController.Instance.SetBackPivotRotation(-PlayerController.Instance.inputController.RightStick.ToeAxis);
            PlayerController.Instance.SetPivotForwardRotation((PlayerController.Instance.inputController.LeftStick.ForwardDir + PlayerController.Instance.inputController.RightStick.ForwardDir) * 0.7f, 15f);
            PlayerController.Instance.SetPivotSideRotation(PlayerController.Instance.inputController.LeftStick.ToeAxis - PlayerController.Instance.inputController.RightStick.ToeAxis, 20f);
            return;
        }

        private void LateUpdate()
        {
            if (!Main.settings.BonedGrab) return;

            if (!IsGrabbing() && run <= limit)
            {
                if (PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Bailed) PreventBail();
            }
        }

        public static float map01(float value, float min, float max)
        {
            return (value - min) * 1f / (max - min);
        }

        void PreventInAirBail()
        {
            EventManager.Instance.ExitGrab();
            PlayerController.Instance.ToggleFlipTrigger(false);
            PlayerController.Instance.BoardFreezedAfterRespawn = false;
            PlayerController.Instance.boardController.ResetAll();
            PlayerController.Instance.boardController.UpdateBoardPosition();
            //PlayerController.Instance.comController.UpdateCOM();
            PlayerController.Instance.playerSM.OnRespawnSM();
            //PlayerController.Instance.DisableArmPhysics();
            //PlayerController.Instance.ResetIKOffsets();
        }

        void PreventBail()
        {
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
            EventManager.Instance.ExitGrab();

            reset_values();
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
            PlayerController.Instance.ikController._finalIk.solver.iterations = 4;
            PlayerController.Instance.ikController._finalIk.solver.spineMapping.iterations = 4;
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

            if (PlayerController.Instance.GetBoardBackwards())
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
            PlayerController.Instance.boardController.SetBoardControllerUpVector(PlayerController.Instance.skaterController.skaterTransform.up);
            PlayerController.Instance.SnapRotation();

            //PlayerController.Instance.comController.UpdateCOM();
        }

        float lerped_left_weight, lerped_right_weight;
        public void DoGrabFeetDetachment()
        {
            float anim_detach = map01(grab_frame, 0, getDetachAnimationLength(actual_grab));
            anim_detach = anim_detach > 1 ? 1 : anim_detach;
            PlayerController.Instance.SetIKOnOff(anim_detach);

            int weight_origin_left = 1, weight_origin_right = 1;

            if (last_input == "" && (on_button || left_stick)) weight_origin_left = getDetachNoPress_Left((int)actual_grab);
            if (last_input == "left") weight_origin_left = getDetachOnLeftStick_Left((int)actual_grab);
            if (last_input == "right") weight_origin_left = getDetachOnButton_Left((int)actual_grab);
            if (last_input == "" && (on_button || left_stick)) weight_origin_right = getDetachNoPress_Right((int)actual_grab);
            if (last_input == "left") weight_origin_right = getDetachOnLeftStick_Right((int)actual_grab);
            if (last_input == "right") weight_origin_right = getDetachOnButton_Right((int)actual_grab);

            lerped_left_weight = Mathf.Lerp(weight_origin_left, getLeftWeight(), anim_detach);
            lerped_right_weight = Mathf.Lerp(weight_origin_right, getRightWeight(), anim_detach);

            UnityModManager.Logger.Log(weight_origin_left + " " + getLeftWeight() + " " + anim_detach);

            Traverse.Create(PlayerController.Instance.ikController).Field("_ikLeftRotLerp").SetValue(1 - lerped_left_weight);
            Traverse.Create(PlayerController.Instance.ikController).Field("_ikRightRotLerp").SetValue(1 - lerped_right_weight);
            Traverse.Create(PlayerController.Instance.ikController).Field("_leftPositionWeight").SetValue(1 - lerped_left_weight);
            Traverse.Create(PlayerController.Instance.ikController).Field("_rightPositionWeight").SetValue(1 - lerped_right_weight);

            if (getLeftWeight() == 0)
            {
                Vector3 l_offset = GetLeftFootOffset();
                GameObject pcopyl = new GameObject();
                pcopyl.transform.position = pelvis.transform.position;
                pcopyl.transform.rotation = pelvis.transform.rotation;
                pcopyl.transform.Translate(l_offset.x, l_offset.y, l_offset.z, Space.Self);
                Transform left_target = (Transform)Traverse.Create(PlayerController.Instance.ikController).Field("ikAnimLeftFootTarget").GetValue();
                left_target.position = pcopyl.transform.position;
                Destroy(pcopyl);
                PlayerController.Instance.ikController.ForceLeftLerpValue(lerped_left_weight);
            }

            if (getRightWeight() == 0)
            {
                Vector3 r_offset = GetRightFootOffset();
                GameObject pcopyr = new GameObject();
                pcopyr.transform.position = pelvis.transform.position;
                pcopyr.transform.rotation = pelvis.transform.rotation;
                pcopyr.transform.Translate(r_offset.x, r_offset.y, r_offset.z, Space.Self);
                Transform right_target = (Transform)Traverse.Create(PlayerController.Instance.ikController).Field("ikAnimRightFootTarget").GetValue();
                right_target.position = pcopyr.transform.position;
                Destroy(pcopyr);
                PlayerController.Instance.ikController.ForceRightLerpValue(lerped_right_weight);
            }
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
        public bool IsGrabbing() { return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Grabs || EventManager.Instance.IsGrabbing; }

        public bool IsGrounded()
        {
            return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Riding || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Setup || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Manual || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Powerslide || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Pushing || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Braking || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Impact || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Grinding || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.EnterCoping || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.ExitCoping;
        }

        void Log(String text)
        {
            if (debug == true) UnityModManager.Logger.Log(text);
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

        GrabSide grabSide, lastGrabSide;
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
                        if (SettingsManager.Instance.stance == Stance.Regular)
                        {
                            PlayerController.Instance.AnimSetGrabTail(true);
                            result = GrabType.TailGrab;
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
                if (SettingsManager.Instance.stance == Stance.Regular)
                {
                    PlayerController.Instance.AnimSetGrabNose(true);
                    result = GrabType.NoseGrab;
                }
                else
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
            float forward = PlayerController.Instance.GetLeftForwardAxis() + PlayerController.Instance.GetRightForwardAxis();
            float left = Mathf.Abs(PlayerController.Instance.GetLeftToeAxis());
            float right = Mathf.Abs(PlayerController.Instance.GetRightToeAxis());

            return (forward < -0.3f || forward > 0.3f) && left < 0.4f && right < 0.4f;
        }

        private bool CanStaleOrMute()
        {
            bool result = false;
            switch (SettingsManager.Instance.controlType)
            {
                case ControlType.Same:
                    if (SettingsManager.Instance.stance == Stance.Regular)
                    {
                        if (PlayerController.Instance.GetLeftToeAxis() < -0.5f && PlayerController.Instance.GetRightToeAxis() > 0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (PlayerController.Instance.GetLeftToeAxis() > 0.5f && PlayerController.Instance.GetRightToeAxis() < -0.5f)
                    {
                        result = true;
                    }
                    break;
                case ControlType.Swap:
                    if (!PlayerController.Instance.IsSwitch)
                    {
                        if (SettingsManager.Instance.stance == Stance.Regular)
                        {
                            if (PlayerController.Instance.GetLeftToeAxis() < -0.5f && PlayerController.Instance.GetRightToeAxis() > 0.5f)
                            {
                                result = true;
                            }
                        }
                        else if (PlayerController.Instance.GetLeftToeAxis() > 0.5f && PlayerController.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (SettingsManager.Instance.stance == Stance.Regular)
                    {
                        if (PlayerController.Instance.GetLeftToeAxis() > 0.5f && PlayerController.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (PlayerController.Instance.GetLeftToeAxis() < -0.5f && PlayerController.Instance.GetRightToeAxis() > 0.5f)
                    {
                        result = true;
                    }
                    break;
                case ControlType.Simple:
                    if (!PlayerController.Instance.IsSwitch)
                    {
                        if (SettingsManager.Instance.stance == Stance.Regular)
                        {
                            if (PlayerController.Instance.GetLeftToeAxis() < -0.5f && PlayerController.Instance.GetRightToeAxis() > 0.5f)
                            {
                                result = true;
                            }
                        }
                        else if (PlayerController.Instance.GetLeftToeAxis() > 0.5f && PlayerController.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (SettingsManager.Instance.stance == Stance.Regular)
                    {
                        if (PlayerController.Instance.GetLeftToeAxis() > 0.5f && PlayerController.Instance.GetRightToeAxis() < -0.5f)
                        {
                            result = true;
                        }
                    }
                    else if (PlayerController.Instance.GetLeftToeAxis() < -0.5f && PlayerController.Instance.GetRightToeAxis() > 0.5f)
                    {
                        result = true;
                    }
                    break;
            }
            return result;
        }
    }
}
