using System;
using UnityEngine;
using UnityModManagerNet;
using SkaterXL.Core;
using RootMotion.Dynamics;
using RootMotion.FinalIK;

namespace grabs_customizer
{
    class BonedGrabs : MonoBehaviour
    {
        public static BonedGrabs Instance { get; private set; }

        int run;
        int limit = 36;
        string last_state;
        bool save_last_pos = true;
        bool debug = true;
        bool determine_grab = true;
        bool on_button = false;

        GrabType actual_grab;
        Vector3 last_pos;

        GameObject left_leg;
        GameObject right_leg;
        GameObject left_foot;
        GameObject right_foot;

        float last_left_leg;
        float last_right_leg;
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
            determine_grab = true;
            save_last_pos = true;
            anim_forced = false;
            PlayerController.Instance.ikController.LeftIKWeight(1f);
            PlayerController.Instance.ikController.RightIKWeight(1f);
        }

        string[] animNames = new string[] { "Disabled", "Falling", "InAir", "Riding" };

        private void Update()
        {
            if (!Main.settings.BonedGrab) return;

            if (PlayerController.Instance.inputController.player.GetButton("Right Stick Button")) on_button = true;
            else on_button = false;

            if (Main.settings.remove_delay && (PlayerController.Instance.inputController.player.GetButton("RB") || PlayerController.Instance.inputController.player.GetButton("LB")) && !IsGrabbing() && !IsGrounded())
            {
                ForceAir();
            }

            if (!IsGrabbing() && run <= limit)
            {
                if (run == 0)
                {
                    reset_values();
                    last_left_leg = left_leg.transform.rotation.eulerAngles.y;
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
                    PlayerController.Instance.DisableArmPhysics();
                    PlayerController.Instance.CorrectHandIKRotation(PlayerController.Instance.boardController.IsBoardBackwards);
                    actual_grab = DetermineGrab();
                    determine_grab = false;
                }

                DoGrabOffsetPosition();
            }
        }

        public void FixedUpdate()
        {
            if (!Main.settings.BonedGrab) return;

            LogState();

            if (Main.settings.remove_delay && (PlayerController.Instance.inputController.player.GetButton("RB") || PlayerController.Instance.inputController.player.GetButton("LB")) && !IsGrabbing() && !IsGrounded())
            {
                ForceAir();
            }

            if (save_last_pos)
            {
                last_pos = PlayerController.Instance.boardController.boardControlTransform.localPosition;
            }

            if (IsGrabbing())
            {
                run = 0;
                save_last_pos = false;

                DoGrabOffsetRotation();

                if (!anim_forced)
                {
                    if (!on_button && Main.settings.selected_anim_index[(int)actual_grab] > 0)
                    {
                        string animation = animNames[Main.settings.selected_anim_index[(int)actual_grab]];
                        PlayerController.Instance.animationController.CrossFadeAnimation(animation, .166f);
                        PlayerController.Instance.animationController.skaterAnim.enabled = true;
                        PlayerController.Instance.animationController.ikAnim.enabled = true;
                        PlayerController.Instance.animationController.ScaleAnimSpeed(1.25f);
                    }
                    else
                    {
                        if (on_button && Main.settings.selected_anim_index_onbutton[(int)actual_grab] > 0)
                        {
                            string animation = animNames[Main.settings.selected_anim_index_onbutton[(int)actual_grab]];
                            PlayerController.Instance.animationController.CrossFadeAnimation(animation, .166f);
                            PlayerController.Instance.animationController.skaterAnim.enabled = true;
                            PlayerController.Instance.animationController.ikAnim.enabled = true;
                            PlayerController.Instance.animationController.ScaleAnimSpeed(1.25f);
                        }
                        else
                        {
                            PlayerController.Instance.animationController.ForceAnimation("Grabs");
                            PlayerController.Instance.animationController.ScaleAnimSpeed(1f);
                        }
                    }
                    anim_forced = true;
                }
            }
            else
            {
                if (run == limit + 1)
                {
                    Catch();
                }
                run++;
            }
        }

        void ForceAir()
        {
            DoGrabOffsetRotation();
            DoGrabOffsetPosition();
            actual_grab = DetermineGrab();

            PlayerController.Instance.currentStateEnum = PlayerController.CurrentState.Grabs;
            EventManager.Instance.StartGrab(actual_grab);
        }

        private void LateUpdate()
        {
            if (!Main.settings.BonedGrab) return;

            if (!IsGrabbing() && IsInAir() && run == 0)
            {
                EventManager.Instance.ExitGrab();
                PlayerController.Instance.ToggleFlipTrigger(false);
                PlayerController.Instance.BoardFreezedAfterRespawn = false;
                PlayerController.Instance.boardController.ResetAll();
                PlayerController.Instance.boardController.UpdateBoardPosition();
                PlayerController.Instance.comController.UpdateCOM();
                PlayerController.Instance.playerSM.OnRespawnSM();
                PlayerController.Instance.DisableArmPhysics();
                PlayerController.Instance.ResetIKOffsets();
            }
        }

        public static float map01(float value, float min, float max)
        {
            return (value - min) * 1f / (max - min);
        }

        void Catch()
        {
            // PlayerController.Instance.boardController.boardControlTransform.localRotation = Quaternion.Euler(0, 0, 0);
            // Log(PlayerController.Instance.playerSM.IsInBailStateSM().ToString());
            PlayerController.Instance.boardController.CatchRotation();
            PlayerController.Instance.boardController.UpdateBoardPosition();
            PlayerController.Instance.AnimOllieTransition(false);
            PlayerController.Instance.AnimSetupTransition(false);
            PlayerController.Instance.boardController.ReduceImpactBounce();
            PlayerController.Instance.skaterController.AddCollisionOffset();
            EventManager.Instance.OnCatched(true, true);
            SoundManager.Instance.PlayCatchSound();
            PlayerController.Instance.boardController.ResetAll();
            PlayerController.Instance.OnExtendAnimEnter();

            MonoBehaviourSingleton<PlayerController>.Instance.currentStateEnum = PlayerController.CurrentState.Riding;
            MonoBehaviourSingleton<PlayerController>.Instance.cameraController.NeedToSlowLerpCamera = false;
            MonoBehaviourSingleton<EventManager>.Instance.EndTrickCombo(false, false);

            MonoBehaviourSingleton<PlayerController>.Instance.ResetBoardCenterOfMass();
            MonoBehaviourSingleton<PlayerController>.Instance.ResetBackTruckCenterOfMass();
            MonoBehaviourSingleton<PlayerController>.Instance.ResetFrontTruckCenterOfMass();
            MonoBehaviourSingleton<PlayerController>.Instance.skaterController.InitializeSkateRotation();

            PlayerController.Instance.AnimRelease(false);
            MonoBehaviourSingleton<PlayerController>.Instance.AnimSetRollOff(false);
            MonoBehaviourSingleton<PlayerController>.Instance.AnimSetNoComply(false);

            Vector3 force = PlayerController.Instance.skaterController.PredictLanding(PlayerController.Instance.skaterController.skaterRigidbody.velocity);
            PlayerController.Instance.skaterController.skaterRigidbody.AddForce(force, ForceMode.Impulse);

            PlayerController.Instance.boardController.UpdateBoardPosition();
        }

        void PreventBail()
        {
            Log("Prevent Bail");
            base.StopCoroutine("RespawnRoutine");
            PlayerController.Instance.respawn.behaviourPuppet.StopAllCoroutines();
            PlayerController.Instance.respawn.behaviourPuppet.unpinnedMuscleKnockout = false;
            PlayerController.Instance.respawn.bail.StopAllCoroutines();
            PlayerController.Instance.CancelRespawnInvoke();
            PlayerController.Instance.CancelInvoke("DoBail");

            base.CancelInvoke("DoBail");
            MonoBehaviourSingleton<PlayerController>.Instance.CancelInvoke("DoBail");
            base.CancelInvoke("PuppetMasterModeActive");
            base.CancelInvoke("EnableBoardPhysics");
            base.Invoke("EnableBoardPhysics", 0);
            base.CancelInvoke("EndRecentRespawn");
            base.Invoke("EndRecentRespawn", 0);
            base.CancelInvoke("DelayPress");
            base.Invoke("DelayPress", 0);
            base.CancelInvoke("EndRespawning");
            base.Invoke("EndRespawning", 0);

            Transform[] componentsInChildren = MonoBehaviourSingleton<PlayerController>.Instance.ragdollHips.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.layer = LayerUtility.RagdollNoInternalCollision;
            }

            PlayerController.Instance.respawn.behaviourPuppet.BoostImmunity(1000f);
            PlayerController.Instance.respawn.puppetMaster.state = PuppetMaster.State.Alive;
            PlayerController.Instance.ResetAllAnimations();
            PlayerController.Instance.AnimGrindTransition(false);
            PlayerController.Instance.AnimOllieTransition(false);
            PlayerController.Instance.AnimGrindTransition(false);
            PlayerController.Instance.AnimSetupTransition(false);
            PlayerController.Instance.respawn.behaviourPuppet.masterProps.normalMode = BehaviourPuppet.NormalMode.Unmapped;
            PlayerController.Instance.respawn.bail.bailed = false;

            PlayerController.Instance.animationController.CrossFadeAnimation("InAir", .5f);

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

        public void DoGrabOffsetRotation()
        {
            if (Main.settings.continuously_detect) actual_grab = DetermineGrab();
            IK();

            Vector3 offset = getCustomRotation(actual_grab);

            var rotation = PlayerController.Instance.boardController.gameObject.transform.localRotation;
            Vector3 lerpedRotation = Vector3.Lerp(rotation.eulerAngles, rotation.eulerAngles + new Vector3(offset.x, offset.y, offset.z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
            PlayerController.Instance.boardController.gameObject.transform.localRotation = Quaternion.Euler(lerpedRotation.x, lerpedRotation.y, lerpedRotation.z);
            PlayerController.Instance.boardController.boardRigidbody.gameObject.transform.localRotation = PlayerController.Instance.boardController.gameObject.transform.localRotation;

            PlayerController.Instance.boardController.UpdateBoardPosition();
            // PlayerController.Instance.ikController.ForceUpdateIK();
            PlayerController.Instance.ikController.LeftIKWeight(getLeftWeight());
            PlayerController.Instance.ikController.RightIKWeight(getRightWeight());

            PlayerController.Instance.LerpKneeIkWeight();

            PlayerController.Instance.ScalePlayerCollider();
            PlayerController.Instance.boardController.SetBoardControllerUpVector(PlayerController.Instance.skaterController.skaterTransform.up);
            PlayerController.Instance.SnapRotation();
            PlayerController.Instance.SetRotationTarget(true);

            Vector3 offset_pos = getCustomPosition(actual_grab);
            PlayerController.Instance.boardController.boardControlTransform.transform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.transform.localPosition, PlayerController.Instance.boardController.boardControlTransform.transform.localPosition + new Vector3(offset_pos.x, offset_pos.y, offset_pos.z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
            PlayerController.Instance.comController.UpdateCOM();
        }

        int getLeftWeight()
        {
            if (!on_button) return Main.settings.left_foot_speed[(int)actual_grab] ? 0 : 1;
            else return Main.settings.left_foot_speed_onbutton[(int)actual_grab] ? 0 : 1;
        }

        int getRightWeight()
        {
            if (!on_button) return Main.settings.right_foot_speed[(int)actual_grab] ? 0 : 1;
            else return Main.settings.right_foot_speed_onbutton[(int)actual_grab] ? 0 : 1;
        }

        void IK()
        {
            GrabSide side = DetermineSide();
            PlayerController.Instance.ikController._finalIk.solver.iterations = 4;
            PlayerController.Instance.ikController._finalIk.solver.spineMapping.iterations = 4;
            PlayerController.Instance.ikController.SetHandIKWeight(side == GrabSide.Left ? 1 : 0, side == GrabSide.Right ? 1 : 0);
        }

        void UpdateKneeTargets()
        {
            float kneeIkLerp = .5f;
            Vector3 vector = (!MonoBehaviourSingleton<PlayerController>.Instance.GetBoardBackwards()) ? MonoBehaviourSingleton<PlayerController>.Instance.boardController.boardTransform.forward : (-MonoBehaviourSingleton<PlayerController>.Instance.boardController.boardTransform.forward);
            vector = Vector3.ProjectOnPlane(vector, MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterTransform.up);
            float kneeIkRotationOffset = Vector3.SignedAngle(MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterTransform.forward, vector, MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterTransform.up);
            Vector3 vector2 = Vector3.Lerp(MonoBehaviourSingleton<PlayerController>.Instance.skaterController.leftKneeGuide.up, Quaternion.AngleAxis(kneeIkRotationOffset, MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterTransform.up) * MonoBehaviourSingleton<PlayerController>.Instance.skaterController.leftKneeGuide.up, kneeIkLerp);
            Vector3 vector3 = Vector3.Lerp(MonoBehaviourSingleton<PlayerController>.Instance.skaterController.rightKneeGuide.up, Quaternion.AngleAxis(kneeIkRotationOffset, MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterTransform.up) * MonoBehaviourSingleton<PlayerController>.Instance.skaterController.rightKneeGuide.up, kneeIkLerp);
            vector2 = Vector3.ProjectOnPlane(vector2, MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterTransform.up);
            vector3 = Vector3.ProjectOnPlane(vector3, MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterTransform.up);
            Vector3 position = MonoBehaviourSingleton<PlayerController>.Instance.skaterController.leftKneeGuide.position + vector2 * 0.5f;
            Vector3 position2 = MonoBehaviourSingleton<PlayerController>.Instance.skaterController.rightKneeGuide.position + vector3 * 0.5f;
            MonoBehaviourSingleton<PlayerController>.Instance.skaterController.leftKneeTarget.position = position;
            MonoBehaviourSingleton<PlayerController>.Instance.skaterController.rightKneeTarget.position = position2;
        }

        public Vector3 getCustomRotation(GrabType grab)
        {
            return !on_button ? Main.settings.rotation_offset[(int)grab] : Main.settings.rotation_offset_onbutton[(int)grab];
        }

        public Vector3 getCustomPosition(GrabType grab)
        {
            return !on_button ? Main.settings.position_offset[(int)grab] : Main.settings.position_offset_onbutton[(int)grab];
        }

        public void DoGrabOffsetPosition()
        {
            Vector3 offset = getCustomPosition(actual_grab);
            PlayerController.Instance.boardController.boardControlTransform.transform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.transform.localPosition, PlayerController.Instance.boardController.boardControlTransform.transform.localPosition + new Vector3(offset.x, offset.y, offset.z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
        }

        public void ResetWeights()
        {
        }

        public bool IsInAir() { return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.InAir; }
        public bool IsGrabbing() { return (PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Grabs) || EventManager.Instance.IsGrabbing; }

        public bool IsGrounded()
        {
            return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Riding || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Setup || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Manual || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Powerslide || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Pushing || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Braking || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Impact || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Grinding || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.EnterCoping || PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.ExitCoping;
        }

        void Log(String text)
        {
            if (debug == true) UnityModManager.Logger.Log(text);
        }

        private string[] grabNames = new string[] { "Nose Grab", "Indy Grab", "Tail Grab", "Melon Grab", "Mute Grab", "Stalefish" };

        private enum GrabSide
        {
            Left,
            Right,
            Both
        }

        private GrabSide grab;

        private GrabType DetermineGrab()
        {
            GrabType result = GrabType.Indy;
            GrabSide grabSide = DetermineSide();

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
                                result = GrabType.NoseGrab;
                            }
                            else if (PlayerController.Instance.GetRightForwardAxis() > 0.3f || PlayerController.Instance.GetLeftForwardAxis() > 0.3f)
                            {
                                result = GrabType.TailGrab;
                            }
                        }
                        else
                        {
                            result = GrabType.NoseGrab;
                        }
                    }
                    else if (this.CanStaleOrMute())
                    {
                        if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                        {
                            result = GrabType.Stalefish;
                        }
                        else
                        {
                            result = GrabType.Mute;
                        }
                    }
                    else if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                    {
                        result = GrabType.Indy;
                    }
                    else
                    {
                        result = GrabType.Melon;
                    }
                }
            }
            else if (this.CanGrabNoseOrTail())
            {
                if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                {
                    result = GrabType.NoseGrab;
                }
                else if (PlayerController.Instance.GetLeftForwardAxis() < -0.3f)
                {
                    result = GrabType.NoseGrab;
                }
                else if (PlayerController.Instance.GetLeftForwardAxis() > 0.3f)
                {
                    result = GrabType.TailGrab;
                }
            }
            else if (this.CanStaleOrMute())
            {
                if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
                {
                    result = GrabType.Mute;
                }
                else
                {
                    result = GrabType.Stalefish;
                }
            }
            else if (MonoBehaviourSingleton<SettingsManager>.Instance.stance == Stance.Regular)
            {
                result = GrabType.Melon;
            }
            else
            {
                result = GrabType.Indy;
            }

            Log(result.ToString());

            return result;
        }

        GrabSide DetermineSide()
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
