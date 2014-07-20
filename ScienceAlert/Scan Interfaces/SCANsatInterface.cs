﻿/******************************************************************************
                   Science Alert for Kerbal Space Program                    
 ******************************************************************************
    Copyright (C) 2014 Allen Mrazek (amrazek@hotmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *****************************************************************************/
//#define DEBUGSCANSAT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ReeperCommon;


namespace ScienceAlert
{
    delegate bool IsCoveredDelegate(double lat, double lon, int mask);

    internal class SCANsatInterface : DefaultScanInterface
    {
        private const string SCANcontrollerTypeName = "SCANsat.SCANcontroller";
        private const string SCANdataTypeName = "SCANsat.SCANdata";
        private const string SCANcontrollerModuleName = "SCANcontroller";
        private const int SCANdataBiomeFlag = 8;

        MethodInfo isCoveredMethod;
        ScenarioModule controllerInstance;
        object dataInstance;
        MethodInfo getMethod;
        IsCoveredDelegate coveredDelegate; // supposedly is faster than invoke

/******************************************************************************
 *                    Implementation Details
 ******************************************************************************/
        public override bool HaveScanData(double lat, double lon)
        {
            try {
                if (isCoveredMethod != null && dataInstance != null)
                {
#if (DEBUG && DEBUGSCANSAT)
                    bool result = (bool)isCoveredMethod.Invoke(dataInstance, new object[] { lon, lat, 8 });
                    bool real = SCANsat.SCANcontroller.controller.getData(FlightGlobals.currentMainBody).isCovered( lon, lat, SCANsat.SCANdata.SCANtype.Biome);

                    if (real != result)
                        Log.Error("actually have data yet was reported as none!");

                    return result;
#else
                    //return (bool)isCoveredMethod.Invoke(dataInstance, new object[] {lat, lon, 8});
                    return coveredDelegate(lat, lon, 8);
                }
#endif
#if DEBUG && DEBUGSCANSAT
                }
                else Log.Error("isCoveredMethod = {0}, dataInstance = {1}", isCoveredMethod == null ? "null!" : "is fine", dataInstance == null ? "null!" : "is fine");
#endif

            } catch (Exception e)
            {
                Log.Error("SCANsatInterface.HaveScanData failed with: {0}", e);
            }

            return false;
        }



        void Start()
        {
            Log.Verbose("SCANsatInterface Start");

            try
            {
                Type controllerType = AssemblyLoader.loadedAssemblies
                    .SelectMany(loaded => loaded.assembly.GetExportedTypes())
                    .SingleOrDefault(t => t.FullName == SCANcontrollerTypeName);

                Type scanDataType = AssemblyLoader.loadedAssemblies
                    .SelectMany(loaded => loaded.assembly.GetExportedTypes())
                    .SingleOrDefault(t => t.FullName == SCANdataTypeName);
#if DEBUG
                AssemblyLoader.loadedAssemblies.ToList().ForEach(a =>
                {
                    if (a.name == "SCANsat")
                    {
                        Log.Write("Assembly {0}:", a.assembly.FullName);

                        a.assembly.GetExportedTypes().ToList().ForEach(t =>
                        {
                            Log.Write("   exports: {0}", t.FullName);
                        });
                    }
                    else Log.Write("Assembly [brief]: {0}", a.name);
                });
#endif

                if (controllerType == null) throw new Exception("SCANsatInterface: Failed to locate SCANcontroller type!");
                if (scanDataType == null) throw new Exception("SCANsatInterface: Failed to locate SCANdata type!");



#if DEBUG
                Log.Write("SCANcontroller properties:");

                controllerType.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).ToList().ForEach(p =>
                {
                    Log.Write("   property: {0}", p.Name);
                });

                Log.Write("SCANcontroller fields:");

                controllerType.GetFields().ToList().ForEach(f => Log.Write("  field: {0}", f.Name));

                Log.Write("SCANController methods:");

                controllerType.GetMethods().ToList().ForEach(m => Log.Write("  method: {0}", m.Name));
#endif


                PropertyInfo controllerProperty = controllerType.GetProperty("controller", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                Log.Write("controllerProperty = {0}", controllerProperty.ToString());

                if (controllerProperty == null) throw new Exception("SCANsatInterface: Failed to locate SCANcontroller.controller property!");
                Log.Write("controllerType: {0}", controllerType.ToString());


#if DEBUG && DEBUGSCANSAT
                Log.Write("Property value: {0}", SCANsat.SCANcontroller.controller);
                if (SCANsat.SCANcontroller.controller == null) Log.Write("real controller is null!");
                if (HighLogic.CurrentGame == null) Log.Write("CurrentGame is null!");

                HighLogic.CurrentGame.scenarios.ForEach(psm => Log.Write("ScenarioModule: {0}", psm.moduleName));

                HighLogic.CurrentGame.scenarios.ForEach(psm =>
                {
                    if (psm.moduleRef == null) Log.Error("PSM {0} moduleRef is null after passing check!", psm.moduleName);
                });
#endif


                var pcontrollerInstance = HighLogic.CurrentGame.scenarios.Find(psm => psm.moduleName == "SCANcontroller");

                if (pcontrollerInstance == null) throw new Exception("SCANsatInterface: Controller instance is null!");
                if (pcontrollerInstance.moduleRef == null) throw new Exception("SCANsatInterface: moduleRef is null!");

                controllerInstance = pcontrollerInstance.moduleRef;


                // now we can use the instance to get a reference to the SCANdata object we're
                // after. Technically we'll get that by using the controller instance's getData(CelestialBody) method

                getMethod = controllerType.GetMethod("getData", new Type[] { typeof(CelestialBody) });
                if (getMethod == null) throw new Exception("SCANsatInterface: Failed to retrieve get method");
            } catch (Exception e)
            {
                Log.Error("SCANsatInterface initialization failed with following: {0}", e);
                Log.Error("Switching to default scan interface.");

                creator.ScheduleInterfaceChange(Settings.ScanInterface.None);
                return;
            }

            GameEvents.onDominantBodyChange.Add(OnBodyChange);

            OnBodyChange(new GameEvents.FromToAction<CelestialBody, CelestialBody>(null, FlightGlobals.currentMainBody));
        }



        void OnDestroy()
        {
            Log.Verbose("SCANsatInterface OnDestroy");
            GameEvents.onDominantBodyChange.Remove(OnBodyChange);
        }



        public void OnBodyChange(GameEvents.FromToAction<CelestialBody, CelestialBody> bodies)
        {
            Log.Verbose("SCANsatInterface.OnBodyChange, from {0} to {1}", bodies.from != null ? bodies.from.GetName() : "<none>", bodies.to != null ? bodies.to.GetName() : "<none>");

            dataInstance = getMethod.Invoke(controllerInstance, new object[] {bodies.to});

#if DEBUG && DEBUGSCANSAT
            var real = SCANsat.SCANcontroller.controller.getData(bodies.to);

            if (real == null)
            {
                Log.Error("SCANsatInterface.OnBodyChange: SCANsat data is null!");
            }
            else if (real != dataInstance) Log.Error("real != data instance from scansat");
#endif


            if (dataInstance == null)
            {
                Log.Error("SCANsatInterface: failed to get SCANdata on body change!");
                isCoveredMethod = null;
                return;
            }
            else
            {
                isCoveredMethod = dataInstance.GetType().GetMethod("isCovered", BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

                
                if (isCoveredMethod == null)
                {
                    Log.Error("SCANsatInterface: failed to get SCANdata.isCovered! Interface failure");
                    creator.ScheduleInterfaceChange(Settings.ScanInterface.None);
                }
                else
                {
                    Log.Debug("SCANsatInterface: successfully retrieved SCANdata.isCovered.");
                    coveredDelegate = (IsCoveredDelegate)Delegate.CreateDelegate(typeof(IsCoveredDelegate), isCoveredMethod); 

                }
            }
        }


        public static bool IsAvailable()
        {
            // it's possible, if SCANsat is removed, for a ScenarioModule of the appropriate name
            // to exist yet it will fail to load. Rely on finding the type instead

            Type controllerType = AssemblyLoader.loadedAssemblies
                .SelectMany(loaded => loaded.assembly.GetExportedTypes())
                .SingleOrDefault(t => t.FullName == SCANcontrollerTypeName);

            if (controllerType == null)
            {
                Log.Debug("SCANsatInterface.IsAvailable: Failed to find controller type. SCANsat interface will not be available.");
                return false;
            }

            // we'll have to trigger the static SCANcontroller.controller property
            // and leave ScenarioModule creation to it
            try
            {

                controllerType.GetProperty("controller", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetValue(null, null);

#if DEBUG
                //Log.Write("Dumping current list of ScenarioModules after using property to create SCANcontroller instance");
                //HighLogic.CurrentGame.scenarios.ForEach(psm => Log.Write("ScenarioModule: {0}", psm.moduleName));
                ////ScenarioRunner.GetLoadedModules().ForEach(sm => Log.Write("ScenarioModule {0} is loaded.", sm.snapshot.moduleName));
                //ScenarioRunner.GetUpdatedProtoModules().ForEach(psm => Log.Write("Update scenario module: {0}", psm.moduleName));
                //Log.Write("done");
#endif
                    
                // confirm it did what we wanted:
                return HighLogic.CurrentGame.scenarios.Any(psm => psm.moduleName == SCANcontrollerModuleName) && controllerType != null;
            }
            catch (Exception e)
            {
                Log.Error("SCANsatInterface.IsAvailable: exception occurred while trying to access controller property! {0}", e);
                return false;
            }
           
        }



        public static bool ScenarioReady()
        {
            return HighLogic.CurrentGame.scenarios.Any(psm => psm.moduleName == SCANcontrollerModuleName && psm.moduleRef != null);
        }
    }
}