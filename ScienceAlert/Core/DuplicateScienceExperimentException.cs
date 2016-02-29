﻿using System;

namespace ScienceAlert.Core
{
    public class DuplicateScienceExperimentException : Exception
    {
        public DuplicateScienceExperimentException(ScienceExperiment exp)
            : base("A binding for " + exp.id + " already exists!")
        {
            
        }
    }
}
