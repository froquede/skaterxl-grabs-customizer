using System;
using UnityEngine;
using UnityModManagerNet;
using SkaterXL.Core;
using RootMotion.Dynamics;

namespace grabs_customizer
{
    class BonedGrabs : MonoBehaviour
    {
        public static BonedGrabs Instance { get; private set; }

        int run;
        int limit = 36;
        string last_state;
        bool save_last_pos = true;
        bool debug = false;
        bool determine_grab = true;
        GrabType actual_grab;
        Vector3 last_pos;

        private void Start()
        {
            reset();
            Log("Controller started");
        }

        void reset()
        {
            run = limit + 1;
            PlayerController.Instance.playerSM.StartSM();
            PlayerController.Instance.respawn.DoRespawn();
        }

        private void Update()
        {
            if (!Main.settings.BonedGrab) return;
            if (!IsGrabbing() && run <= limit)
            {
                if (run == 0)
                {
                    PlayerController.Instance.animationController.ForceAnimation("Catch");
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

            if (save_last_pos)
            {
                last_pos = PlayerController.Instance.boardController.boardControlTransform.localPosition;
            }

            if (IsGrabbing())
            {
                run = 0;
                save_last_pos = false;

                DoGrabOffsetRotation();
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

        private void LateUpdate()
        {
            if (!Main.settings.BonedGrab) return;

            if (!IsGrabbing() && IsInAir() && run == 0)
            {
                PlayerController.Instance.ToggleFlipTrigger(false);
                PlayerController.Instance.BoardFreezedAfterRespawn = false;
                PlayerController.Instance.boardController.ResetAll();
                PlayerController.Instance.boardController.UpdateBoardPosition();
                PlayerController.Instance.comController.UpdateCOM();
                PlayerController.Instance.playerSM.OnRespawnSM();
                PlayerController.Instance.DisableArmPhysics();
                PlayerController.Instance.ResetIKOffsets();
                determine_grab = true;
                save_last_pos = true;
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
            PlayerController.Instance.currentStateEnum = PlayerController.CurrentState.InAir;
            EventManager.Instance.EnterAir();

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

            Vector3 offset = getCustomRotation(actual_grab);

            var rotation = PlayerController.Instance.boardController.gameObject.transform.localRotation;
            Vector3 lerpedRotation = Vector3.Lerp(rotation.eulerAngles, rotation.eulerAngles + new Vector3(offset.x, offset.y, offset.z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
            PlayerController.Instance.boardController.gameObject.transform.localRotation = Quaternion.Euler(lerpedRotation.x, lerpedRotation.y, lerpedRotation.z);
            PlayerController.Instance.boardController.boardRigidbody.gameObject.transform.localRotation = PlayerController.Instance.boardController.gameObject.transform.localRotation;

            PlayerController.Instance.boardController.UpdateBoardPosition();
            // PlayerController.Instance.ikController.ForceUpdateIK();
            PlayerController.Instance.ikController.LeftIKWeight(Main.settings.left_foot_speed[(int)actual_grab] ? 0f : 1f);
            PlayerController.Instance.ikController.RightIKWeight(Main.settings.right_foot_speed[(int)actual_grab] ? 0f : 1f);

            PlayerController.Instance.LerpKneeIkWeight();

            PlayerController.Instance.ScalePlayerCollider();
            PlayerController.Instance.boardController.SetBoardControllerUpVector(PlayerController.Instance.skaterController.skaterTransform.up);
            PlayerController.Instance.SnapRotation();
            PlayerController.Instance.SetRotationTarget(true);

            Vector3 offset_pos = getCustomPosition(actual_grab);
            PlayerController.Instance.boardController.boardControlTransform.transform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.transform.localPosition, PlayerController.Instance.boardController.boardControlTransform.transform.localPosition + new Vector3(offset_pos.x, offset_pos.y, offset_pos.z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
        }

        public Vector3 getCustomRotation(GrabType grab)
        {
            return Main.settings.rotation_offset[(int)grab];
        }

        public Vector3 getCustomPosition(GrabType grab)
        {
            return Main.settings.position_offset[(int)grab];
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
