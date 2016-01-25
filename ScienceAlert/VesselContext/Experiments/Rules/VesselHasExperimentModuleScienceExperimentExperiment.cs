﻿using ScienceAlert.Game;

namespace ScienceAlert.VesselContext.Experiments.Rules
{
    public class VesselHasExperimentModuleScienceExperimentExperiment : ScienceExperimentModuleTracker, IExperimentRule
    {
        public VesselHasExperimentModuleScienceExperimentExperiment(ScienceExperiment experiment, IVessel vessel) : base(experiment, vessel)
        {
        }

        public bool Passes()
        {
            return ExperimentModules.Count > 0;
        }
    }
}