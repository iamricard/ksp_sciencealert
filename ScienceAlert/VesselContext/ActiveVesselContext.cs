﻿using System;
using ReeperCommon.Containers;
using ReeperCommon.Extensions;
using ScienceAlert.Core;
using ScienceAlert.Game;
using ScienceAlert.VesselContext.Experiments;
using ScienceAlert.VesselContext.Experiments.Rules;
using ScienceAlert.VesselContext.Gui;
using strange.extensions.context.api;
using UnityEngine;

namespace ScienceAlert.VesselContext
{
    public class ActiveVesselContext : SignalContext
    {
        public ActiveVesselContext(MonoBehaviour view) : base(view, ContextStartupFlags.MANUAL_LAUNCH)
        {
        }


        protected override void mapBindings()
        {
            base.mapBindings();

            if (FlightGlobals.ActiveVessel.IsNull())
            {
                Log.Error("ActiveVessel context created when no active vessel exists");
                return;
            }



            var sharedConfig = injectionBinder.GetInstance<SharedConfiguration>();

            injectionBinder.Bind<ConfigNode>()
                .To(sharedConfig.ExperimentViewConfig)
                .ToName(VesselContextKeys.ExperimentViewConfig);

            injectionBinder.Bind<ConfigNode>()
                .ToValue(sharedConfig.VesselDebugViewConfig)
                .ToName(VesselContextKeys.VesselDebugViewConfig);

            injectionBinder.Bind<IExperimentRuleFactory>().To<ExperimentRuleFactory>().ToSingleton();

            injectionBinder.Bind<IExperimentRulesetProvider>().To<ExperimentRulesetProvider>().ToSingleton();
            //injectionBinder.Bind<ISensorFactory>().To<ExperimentSensorFactory>().ToSingleton();

            injectionBinder.Bind<SignalSaveGuiSettings>().ToSingleton();
            injectionBinder.Bind<SignalLoadGuiSettings>().ToSingleton();


            // note to self: see how these are NOT cross context? That's because each ContextView
            // has its own GameEventView. This is done to avoid having to do any extra bookkeeping (of
            // removing events we've subscribed to) in the event that a ContextView is destroyed.
            // 
            // If we were to register these to cross context signals, those publishers might keep objects
            // designed for the current active vessel alive and away from the GC even when the rest of the
            // context was destroyed
            injectionBinder.Bind<SignalVesselChanged>().ToSingleton();
            injectionBinder.Bind<SignalVesselModified>().ToSingleton();
            injectionBinder.Bind<SignalActiveVesselModified>().ToSingleton();
            injectionBinder.Bind<SignalVesselDestroyed>().ToSingleton();
            injectionBinder.Bind<SignalActiveVesselDestroyed>().ToSingleton();
            injectionBinder.Bind<SignalGameSceneLoadRequested>().ToSingleton();
            injectionBinder.Bind<SignalApplicationQuit>().ToSingleton();

            injectionBinder.Bind<SignalUpdateExperimentListPopup>().ToSingleton();

            mediationBinder.BindView<ExperimentListView>()
                .ToMediator<ExperimentListMediator>()
                .ToMediator<ExperimentListPopupMediator>();

            var gameFactory = injectionBinder.GetInstance<IGameFactory>();

            injectionBinder
                .Bind<IVessel>()
                .Bind<ICelestialBodyProvider>()
                .Bind<IExperimentSituationProvider>()
                .Bind<IExperimentBiomeProvider>()
                .Bind<IScienceContainerCollectionProvider>()
                .ToValue(gameFactory.Create(FlightGlobals.ActiveVessel));

            SetupCommandBindings();
        }



        private void SetupCommandBindings()
        {
            commandBinder.Bind<SignalStart>()
                .InSequence()
                .To<CommandConfigureGameEvents>()
                .To<CommandCreateRuleTypeBindings>()
                .To<CommandCreateVesselGui>()
                .To<CommandDispatchLoadGuiSettingsSignal>()
                .To<CommandCreateExperimentReportCalculator>()
                .To<CommandCreateExperimentSensors>()
                .Once();

 
            commandBinder.Bind<SignalContextDestruction>()
                .To<CommandDispatchSaveGuiSettingsSignal>()
                .Once();

            commandBinder.Bind<SignalSharedConfigurationSaving>()
                .To<CommandDispatchSaveGuiSettingsSignal>();

            commandBinder.Bind<SignalExperimentSensorStatusChanged>()
                .To<CommandLogSensorStatusUpdate>();
        }


        public override void Launch()
        {
            base.Launch();

            Log.Debug("Launching ActiveVesselContext");

            try
            {
                injectionBinder.GetInstance<SignalStart>().Do(s => s.Dispatch());
            }
            catch (Exception e)
            {
                Log.Error("Error while launching ActiveVesselContext: " + e);
                SignalDestruction(true);
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            throw new NotImplementedException();
        }


        public void SignalDestruction(bool destroyContextGo)
        {
            Log.Verbose("Signaling ActiveVesselContext destruction");



            try
            {
                if (destroyContextGo)
                {
                    DestroyContext();
                    return; // context bootstrapper will issue destruction signal
                }

                injectionBinder.GetInstance<SignalContextDestruction>().Do(s => s.Dispatch());
            }
            catch (Exception e)
            {
                Log.Error("Failed to signal destruction: " + e);
            } 
        }


        public void DestroyContext()
        {
            (contextView as GameObject).If(go => go != null).Do(UnityEngine.Object.Destroy);
        }
    }
}