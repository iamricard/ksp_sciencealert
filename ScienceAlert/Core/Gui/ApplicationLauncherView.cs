﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using ReeperCommon.Containers;
using ReeperCommon.Logging;
using ReeperKSP.AssetBundleLoading;
using strange.extensions.mediation.impl;
using strange.extensions.signal.impl;
using ScienceAlert.Gui;
using UnityEngine;

namespace ScienceAlert.Core.Gui
{
    [MediatedBy(typeof(ApplicationLauncherMediator))]
// ReSharper disable once ClassNeverInstantiated.Global
    public class ApplicationLauncherView : View
    {
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

        [Inject] public IGuiConfiguration GuiConfiguration { get; set; }

        internal readonly Signal<bool> Toggle = new Signal<bool>();
        internal readonly Signal ButtonCreated = new Signal();

        [AssetBundleAsset("assets/sciencealert/ui/sa_button.prefab", "sciencealert.ksp", AssetBundleAssetAttribute.AssetCreationStyle.Instance)]
#pragma warning disable 649 // disable unassigned warning -- injected via reflection
        private Animator _buttonSprite;
#pragma warning restore 649



        private ApplicationLauncherButton _button;


        public enum ButtonAnimationStates
        {
            Unlit,
            Lit,
            Spinning
        }


        private readonly Dictionary<ButtonAnimationStates, int> _buttonAnimations = new Dictionary<ButtonAnimationStates, int>
        {
            { ButtonAnimationStates.Unlit, Animator.StringToHash("button_idle") },
            { ButtonAnimationStates.Lit, Animator.StringToHash("button_lit") },
            { ButtonAnimationStates.Spinning, Animator.StringToHash("button_spin") }
        };



        protected override void OnDestroy()
        {
            ApplicationLauncher.Instance.Do(l => _button.Do(l.RemoveModApplication));

            base.OnDestroy();
        }


        // Defensive coding in case I change the name or add animations in the future
        private void CheckForAnimationMismatch()
        {
            var statesWithoutDefinedAnimation =
                Enum.GetValues(typeof (ButtonAnimationStates))
                    .Cast<ButtonAnimationStates>()
                    .Except(_buttonAnimations.Select(kvp => kvp.Key))
                    .Select(st => st.ToString())
                    .ToArray();

            var animationsWithoutStates = _buttonSprite.runtimeAnimatorController.animationClips
                .ToDictionary(ac => ac.name, ac => Animator.StringToHash(ac.name)) // KVP<name, hash>
                .Where(clip => _buttonAnimations.Values.All(existingHash => existingHash != clip.Value)) // where this animation isn't used at all
                .Select(clip => clip.Key) // name of clip
                .ToArray();

            var entriesWhereHashDoesntMatchAnyClip =
                _buttonAnimations.Where(
                    entry =>
                        _buttonSprite.runtimeAnimatorController.animationClips.Select(
                            clip => Animator.StringToHash(clip.name)).All(clipHash => clipHash != entry.Value))
                    .Select(kvp => kvp.Key.ToString())
                    .ToArray();

            if (statesWithoutDefinedAnimation.Any()) // these would result in "do-nothing" animations; enum values that aren't linked to any animation and seem superfluous or unimplemented
                Log.Warning("No corresponding button animation for following state(s): " +
                            string.Join(",", statesWithoutDefinedAnimation));

            if (animationsWithoutStates.Any()) // these are extra animations that we apparently aren't using -- not necessarily an error, but suspicious
                Log.Debug(typeof (ApplicationLauncherView).Name + " does not use the following animation clip(s): " +
                          string.Join(",", animationsWithoutStates));

            if (entriesWhereHashDoesntMatchAnyClip.Any()) // these are states that have supposedly been linked to an animation id, but the animation the id references doesn't exist so I probably fatfingered an animation name
                Log.Error("The following " + typeof (ButtonAnimationStates).Name + " entries don't have a valid clip: " +
                          string.Join(",", entriesWhereHashDoesntMatchAnyClip));
        }


        public IEnumerator SetupButton()
        {
            Log.TraceMessage();

            while (ApplicationLauncher.Instance == null || !ApplicationLauncher.Ready)
                yield return null;

            CheckForAnimationMismatch();

            _button = ApplicationLauncher.Instance.AddModApplication(
                                                        OnTrue,
                                                        OnFalse,

                                                        () => { },
                                                        () => { },
                                                        () => { },
                                                        () => { },
                                                        ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                                                        _buttonSprite);

            // commented to confirm this is still the cause in 1.1
            //yield return new WaitForEndOfFrame();   // the button won't respect toggle state immediately for some reason,
            //                                        // so a slight delay is necessary while it finishes doing whatever internal setup

            SetAnimationState(ButtonAnimationStates.Unlit);
            SetAnimationState(ButtonAnimationStates.Spinning); // temp to make sure it's working

            ButtonCreated.Dispatch();
        }


        private void OnFalse()
        {
            Toggle.Dispatch(false);
        }


        private void OnTrue()
        {
            Toggle.Dispatch(true);
        }


        public void SetToggleState(bool tf)
        {
            if (tf)
                _button.Do(b => b.SetTrue(false));
            else _button.Do(b => b.SetFalse(false));
        }


        public void SetAnimationState(ButtonAnimationStates anim)
        {
            int animationId = 0;

            if (_buttonAnimations.TryGetValue(anim, out animationId))
                _buttonSprite.Do(spr => spr.Play(animationId));
            else throw new NotImplementedException(anim.ToString());
        }
    }
}
