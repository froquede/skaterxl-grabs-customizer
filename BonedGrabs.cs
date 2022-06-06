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
        float anim_ratio = 1.5f;
        string last_state;
        bool save_last_pos = true;
        bool fake_bail = false;
        bool impact = false;
        bool debug = true;
        bool popped = false;
        Vector3 last_pos;
        Vector3 last_rot;

        private void Start()
        {
            reset();
            Log("Controller started");
        }

        void reset() {
            run = limit + 1;
            PlayerController.Instance.playerSM.StartSM();
            PlayerController.Instance.respawn.DoRespawn();
        }

        public void Update()
        {
            LogState();

            if (Main.settings.BonedGrab && IsGrabbing())
            {
                if (save_last_pos)
                {
                    Log("Saved last pos");
                    last_pos = PlayerController.Instance.boardController.boardControlTransform.localPosition;
                    last_rot = PlayerController.Instance.boardController.boardControlTransform.localRotation.eulerAngles;
                    save_last_pos = false;
                }

                DoGrabOffset();
                run = 0;
                popped = false;
                fake_bail = false;
                impact = false;
            }
            else
            {
                if (run <= limit)
                {
                    if(run == 0)
                    {
                        PlayerController.Instance.ikController.LeftIKWeight(1f);
                        PlayerController.Instance.ikController.RightIKWeight(1f);

                        if (IsInAir()) {
                            PlayerController.Instance.playerSM.OnRespawnSM();
                            EventManager.Instance.EnterAir();
                            PlayerController.Instance.ToggleFlipTrigger(false);
                            PlayerController.Instance.BoardFreezedAfterRespawn = false;
                            PlayerController.Instance.SetTurningMode(InputController.TurningMode.InAir);
                            PlayerController.Instance.boardController.boardControlTransform.localPosition = last_pos;
                        }
                    }

                    if (PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Bailed) PreventBail();

                    if (fake_bail)
                    {
                        if (run > limit / anim_ratio)
                        {
                            if (!popped)
                            {
                                PlayerController.Instance.animationController.ForceAnimation("Pop");
                                popped = true;
                            }
                        }
                        PlayerController.Instance.boardController.boardControlTransform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.localPosition, last_pos, Time.deltaTime / 1000);
                        PlayerController.Instance.boardController.UpdateBoardPosition();
                    }

                    run++;
                }
                else
                {
                    if (run == limit + 1)
                    {
                        Log("Catch, over limit ");
                        if(fake_bail) Catch();
                        save_last_pos = true;
                        run++;
                    }
                }
            }
        }

        void Catch() {
            PlayerController.Instance.boardController.boardControlTransform.localRotation = Quaternion.Euler(0, 0, 0);
            Log(PlayerController.Instance.playerSM.IsInBailStateSM().ToString());
            PlayerController.Instance.boardController.CatchRotation();
            PlayerController.Instance.boardController.UpdateBoardPosition();
            PlayerController.Instance.AnimOllieTransition(false);
            PlayerController.Instance.AnimSetupTransition(false);
            PlayerController.Instance.boardController.ReduceImpactBounce();
            PlayerController.Instance.skaterController.AddCollisionOffset();
            PlayerController.Instance.boardController.ForceBoardPosition();
            EventManager.Instance.OnCatched(true, true);
            PlayerController.Instance.AnimCaught(true);
            PlayerController.Instance.cameraController.NeedToSlowLerpCamera = false;
            PlayerController.Instance.ToggleFlipTrigger(false);
            PlayerController.Instance.animationController.ScaleAnimSpeed(0.05f);
            PlayerController.Instance.SetLeftKneeBendWeightManually(1f);
            PlayerController.Instance.SetRightKneeBendWeightManually(1f);
            PlayerController.Instance.boardController.LeaveFlipMode();
            PlayerController.Instance.boardController.SetCatchForwardRotation();
            SoundManager.Instance.PlayCatchSound();
            MonoBehaviourSingleton<PlayerController>.Instance.SetBoardBackwards();
            MonoBehaviourSingleton<PlayerController>.Instance.CorrectHandIKRotation(MonoBehaviourSingleton<PlayerController>.Instance.GetBoardBackwards());
            MonoBehaviourSingleton<PlayerController>.Instance.boardController.ResetAll();
            PlayerController.Instance.boardController.ResetAll();
            PlayerController.Instance.SetMaxSteeze(0f);
            PlayerController.Instance.AnimCaught(true);
            PlayerController.Instance.CrossFadeAnimation("Extend", 0.15f);
            PlayerController.Instance.OnExtendAnimEnter();
            PlayerController.Instance.AnimRelease(false);
            PlayerController.Instance.OnExtendAnimEnter();
            PlayerController.Instance.SetCatchForwardRotation();

            MonoBehaviourSingleton<PlayerController>.Instance.currentStateEnum = PlayerController.CurrentState.Riding;
            MonoBehaviourSingleton<PlayerController>.Instance.cameraController.NeedToSlowLerpCamera = false;
            MonoBehaviourSingleton<PlayerController>.Instance.AnimSetRollOff(false);
            MonoBehaviourSingleton<EventManager>.Instance.EndTrickCombo(false, false);
            MonoBehaviourSingleton<PlayerController>.Instance.ToggleFlipColliders(false);
            MonoBehaviourSingleton<SoundManager>.Instance.PlayShoeMovementSound();
            MonoBehaviourSingleton<PlayerController>.Instance.ResetBoardCenterOfMass();
            MonoBehaviourSingleton<PlayerController>.Instance.ResetBackTruckCenterOfMass();
            MonoBehaviourSingleton<PlayerController>.Instance.ResetFrontTruckCenterOfMass();
            MonoBehaviourSingleton<PlayerController>.Instance.skaterController.InitializeSkateRotation();

            MonoBehaviourSingleton<PlayerController>.Instance.AnimSetRollOff(false);
            MonoBehaviourSingleton<PlayerController>.Instance.AnimSetNoComply(false);
            MonoBehaviourSingleton<PlayerController>.Instance.skaterController.InitializeSkateRotation();
            MonoBehaviourSingleton<PlayerController>.Instance.skaterController.skaterRigidbody.angularVelocity = Vector3.zero;
        }

        void PreventBail()
        {
            PlayerController.Instance.playerSM.IsInAirStateSM();
            base.StopCoroutine("RespawnRoutine");
            Log("Prevent Bail");
            fake_bail = true;
            PlayerController.Instance.respawn.behaviourPuppet.StopAllCoroutines();
            PlayerController.Instance.respawn.behaviourPuppet.unpinnedMuscleKnockout = false;
            PlayerController.Instance.respawn.behaviourPuppet.SetState(BehaviourPuppet.State.Puppet);
            PlayerController.Instance.respawn.bail.StopAllCoroutines();
            PlayerController.Instance.CancelRespawnInvoke();
            PlayerController.Instance.CancelInvoke("DoBail");
            PlayerController.Instance.currentStateEnum = PlayerController.CurrentState.InAir;
            EventManager.Instance.EnterAir();

            //PlayerController.Instance.OnPop(0f, 0f);

            MonoBehaviourSingleton<PlayerController>.Instance.ikController.ForceUpdateIK();
            MonoBehaviourSingleton<PlayerController>.Instance.InvokeEnableArmPhysics();

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
            PlayerController.Instance.SetKneeBendWeightManually(1f);
            PlayerController.Instance.respawn.puppetMaster.state = PuppetMaster.State.Alive;
            PlayerController.Instance.ResetAllAnimations();
            PlayerController.Instance.AnimGrindTransition(false);
            PlayerController.Instance.AnimOllieTransition(false);
            PlayerController.Instance.AnimSetupTransition(false);
            PlayerController.Instance.RagdollLayerChange(true);
            PlayerController.Instance.SetKneeBendWeightManually(0f);
            PlayerController.Instance.respawn.puppetMaster.pinWeight = 1.75f;
            PlayerController.Instance.respawn.puppetMaster.muscleWeight = 1.75f;
            PlayerController.Instance.respawn.behaviourPuppet.defaults.minMappingWeight = 1f;
            PlayerController.Instance.respawn.behaviourPuppet.masterProps.normalMode = BehaviourPuppet.NormalMode.Unmapped;
            PlayerController.Instance.SetBoardPhysicsMaterial(PlayerController.FrictionType.Default);
            PlayerController.Instance.respawn.bail.bailed = false;
            MonoBehaviourSingleton<PlayerController>.Instance.EnablePuppetMaster(false, true);
            MonoBehaviourSingleton<PlayerController>.Instance.boardController.ResetBoardTargetPosition();
            PlayerController.Instance.AnimGrindTransition(false);
            PlayerController.Instance.AnimOllieTransition(false);
            PlayerController.Instance.AnimSetupTransition(false);
            // respawn();
        }

        private void LogState()
        {
            if (PlayerController.Instance.currentStateEnum.ToString() != last_state)
            {
                Log(PlayerController.Instance.currentStateEnum.ToString());
                last_state = PlayerController.Instance.currentStateEnum.ToString();
            }
        }

        public bool IsInAir()
        {
            return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.InAir;
        }

        public bool IsGrabbing()
        {
            return PlayerController.Instance.currentStateEnum == PlayerController.CurrentState.Grabs || EventManager.Instance.IsGrabbing;
        }

        public void DoGrabOffset()
        {

            PlayerController.Instance.boardController.boardControlTransform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.localPosition, PlayerController.Instance.boardController.boardControlTransform.localPosition + new Vector3(Main.settings.GrabBoardBoned_x, Main.settings.GrabBoardBoned_y, Main.settings.GrabBoardBoned_z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);

            var rotation = PlayerController.Instance.boardController.gameObject.transform.localRotation;
            Vector3 lerpedRotation = Vector3.Lerp(rotation.eulerAngles, rotation.eulerAngles + new Vector3(Main.settings.GrabBoardBoned_rotation_x, Main.settings.GrabBoardBoned_rotation_y, Main.settings.GrabBoardBoned_rotation_z), Time.deltaTime * Main.settings.GrabBoardBoned_speed);
            PlayerController.Instance.boardController.gameObject.transform.localRotation = Quaternion.Euler(lerpedRotation.x, lerpedRotation.y, lerpedRotation.z);

            PlayerController.Instance.CorrectHandIKRotation(PlayerController.Instance.boardController.IsBoardBackwards);

            PlayerController.Instance.ikController.LeftIKWeight(Main.settings.GrabBoardBoned_left_speed);
            PlayerController.Instance.ikController.RightIKWeight(Main.settings.GrabBoardBoned_right_speed);

            PlayerController.Instance.LerpKneeIkWeight();

            /*PlayerController.Instance.SetInAirFootPlacement(Main.settings.GrabBoardBoned_left_knee, Main.settings.GrabBoardBoned_left_knee, false);
            PlayerController.Instance.SetInAirFootPlacement(Main.settings.GrabBoardBoned_right_knee, Main.settings.GrabBoardBoned_right_knee, true);*/
        }

        private void CatchBoth()
        {
            if(run >= limit / anim_ratio)
            {
                if(run == limit / anim_ratio)
                {
                    Log("Board to Master");
                    ExitGrab();
                    PlayerController.Instance.boardController.ResetAll();
                    PlayerController.Instance.boardController.AutoCatchRotation();
                    PlayerController.Instance.boardController.ReduceImpactBounce();
                    PlayerController.Instance.skaterController.AddCollisionOffset();
                    PlayerController.Instance.SetBoardToMaster();

                    PlayerController.Instance.boardController.UpdateBoardPosition();
                    PlayerController.Instance.AnimOllieTransition(false);
                    PlayerController.Instance.AnimSetupTransition(false);
                    PlayerController.Instance.currentStateEnum = PlayerController.CurrentState.Impact;
                    PlayerController.Instance.skaterController.AddCollisionOffset();
                }
            }
            if (run < limit / anim_ratio)
            {
                Log("Catch Animation");
                PlayerController.Instance.ikController.LeftIKWeight(1f);
                PlayerController.Instance.ikController.RightIKWeight(1f);
                PlayerController.Instance.boardController.boardControlTransform.localPosition = Vector3.Lerp(PlayerController.Instance.boardController.boardControlTransform.localPosition, PlayerController.Instance.boardController.boardControlTransform.localPosition + last_pos, Time.deltaTime * Main.settings.GrabBoardBoned_speed);
                PlayerController.Instance.boardController.gameObject.transform.localRotation = Quaternion.Euler(0f, last_rot.y, 0f);
            }            

            PlayerController.Instance.ScalePlayerCollider();
            PlayerController.Instance.comController.UpdateCOM();
        }

        void ExitGrab()
        {
            Log("Exit Grab");
            PlayerController.Instance.SetHandIKWeight(0f, 0f);
            PlayerController.Instance.AnimSetGrabToeside(false);
            PlayerController.Instance.AnimSetGrabHeelside(false);
            PlayerController.Instance.AnimSetGrabNose(false);
            PlayerController.Instance.AnimSetGrabTail(false);
            PlayerController.Instance.AnimSetGrabStale(false);
            PlayerController.Instance.AnimSetGrabMute(false);
            EventManager.Instance.OnCatched(true, true);
            PlayerController.Instance.AnimCaught(true);
            EventManager.Instance.ExitGrab();
        }

        void DoImpact()
        {
            if (impact) return;
            Log("DoImpact");
            Log("Is bail? " + fake_bail);

            //PlayerController.Instance.playerSM.OnStickPressedSM(true);
            PlayerController.Instance.playerSM.OnStickPressedSM(false);

            impact = true;
            PlayerController.Instance.currentStateEnum = PlayerController.CurrentState.Impact;
            PlayerController.Instance.ScalePlayerCollider();
            PlayerController.Instance.AnimSetRollOff(false);
            PlayerController.Instance.ToggleFlipColliders(false);
            SoundManager.Instance.PlayMovementFoleySound(0.3f, false);
            PlayerController.Instance.animationController.SetValue("NollieImpact", PlayerController.Instance.animationController.skaterAnim.GetFloat("Nollie"));
            PlayerController.Instance.animationController.ScaleAnimSpeed(1f);
            float num = Mathf.Clamp(Mathf.Abs(PlayerController.Instance.comController.COMRigidbody.velocity.y), 0f, 17f);
            if (num > 6f)
            {
                num -= 6f;
                num /= 11f;
                float num2 = num * 5.5f;
                float p_speed = Mathf.Clamp(1f + num2, 1f, 1f + num2);
                PlayerController.Instance.animationController.ScaleAnimSpeed(p_speed);
            }
            float impactForce = (Mathf.Clamp(Mathf.Abs(PlayerController.Instance.comController.COMRigidbody.velocity.y), 3.5f, 7.2f) - 3.5f) / 3.7f;
            PlayerController.Instance.animationController.SetValue("Impact", impactForce);
                PlayerController.Instance.skaterController.InitializeSkateRotation();
                PlayerController.Instance.AnimSetNoComply(false);
                PlayerController.Instance.boardController.ResetAll();
                PlayerController.Instance.AnimSetManual(false, PlayerController.Instance.AnimGetManualAxis());
                PlayerController.Instance.AnimSetNoseManual(false, PlayerController.Instance.AnimGetManualAxis());
                PlayerController.Instance.SetTurnMultiplier(1f);
                PlayerController.Instance.SetTurningMode(InputController.TurningMode.Grounded);
                PlayerController.Instance.AnimSetGrinding(false);
                PlayerController.Instance.ResetAnimationsAfterImpact();
                PlayerController.Instance.OnImpact();
                if (!PlayerController.Instance.IsCurrentAnimationPlaying("Impact"))
                {
                    PlayerController.Instance.CrossFadeAnimation("Impact", 0.1f);
                }
                PlayerController.Instance.SetKneeBendWeightManually(0f);
        }

        public void ResetMuscleWeight()
        {
            PlayerController.Instance.respawn.puppetMaster.muscleWeight = 1f;
            PlayerController.Instance.respawn.puppetMaster.muscleSpring = 200f;
            PlayerController.Instance.respawn.puppetMaster.muscleDamper = 100f;
        }

        void FinishGrab()
        {
            Log("Finish Grab");
            PlayerController.Instance.boardController.ReduceImpactBounce();
        }

        void Log(String text) {
            if (debug == true) UnityModManager.Logger.Log(text);
        }
    }
}
