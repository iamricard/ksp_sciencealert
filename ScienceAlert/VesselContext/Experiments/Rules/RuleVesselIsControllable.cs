﻿using System;
using ScienceAlert.Game;

namespace ScienceAlert.VesselContext.Experiments.Rules
{
    // ReSharper disable once UnusedMember.Global
    public class RuleVesselIsControllable : IExperimentRule
    {
        private readonly IVessel _vessel;

        public RuleVesselIsControllable(IVessel vessel)
        {
            if (vessel == null) throw new ArgumentNullException("vessel");
            _vessel = vessel;
        }

        public bool Passes()
        {
            return _vessel.IsControllable;
        }
    }
}