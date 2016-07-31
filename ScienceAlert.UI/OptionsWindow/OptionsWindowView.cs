﻿using System;
using System.Collections.Generic;
using ReeperCommon.Logging;
using strange.extensions.context.api;
using strange.extensions.signal.impl;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

#pragma warning disable 649
#pragma warning disable 169

namespace ScienceAlert.UI.OptionsWindow
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class OptionsWindowView : ManualRegistrationView
    {
        [SerializeField] private OptionsListItemView _optionsPrefab;
        [SerializeField] private RectTransform _list;

        [Inject(ContextKeys.CONTEXT)] public IContext Context { get; set; }

        [HideInInspector, NonSerialized] public readonly Signal CloseButtonClicked = new Signal();

        private readonly HashSet<string> _experimentTitles = new HashSet<string>();


        protected override void Awake()
        {
            if (_optionsPrefab == null)
                throw new InvalidOperationException(
                    "options prefab is not set. Check Unity scene to make sure one is assigned");

            if (!Application.isEditor) _optionsPrefab.gameObject.SetActive(false);

            _optionsPrefab.parentViewForcesInjection = false; // otherwise it will be registered with the context as the same time as this view which we don't want,
                                                              // this specific options entry exists only to be cloned and isn't properly set up to be used

            base.Awake();
        }


        public void AddEntry(OptionDisplayStatus option)
        {
            if (_experimentTitles.Contains(option.ExperimentTitle))
                throw new ArgumentException("An option entry for " + option.ExperimentTitle + " has already been added");

            if (_list == null)
                throw new InvalidOperationException("List RectTransform not set");

            if (_optionsPrefab == null)
                throw new InvalidOperationException("options prefab not set (check AssetBundle in Unity)");

            Log.Verbose("Adding entry: " + option.ExperimentTitle);

            _experimentTitles.Add(option.ExperimentTitle);

            var newEntry = OptionsListItemView.Factory.Create(_optionsPrefab, Context, option);

            newEntry.transform.SetParent(_list, false);
        }


        // UnityAction
        public void OnCloseButtonClicked()
        {
            CloseButtonClicked.Dispatch();
        }
    }
}
