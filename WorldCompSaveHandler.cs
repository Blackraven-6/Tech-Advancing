﻿using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;


namespace TechAdvancing
{
    internal class WorldCompSaveHandler : WorldComponent
    {
        private Dictionary<string, int> ConfigValues = new Dictionary<string, int>();

        internal List<string> GetConfigValueNames => this.ConfigValues.Keys.ToList();

        /// <summary>
        /// Stores all the pawns that joined along with their old Faction
        /// </summary>
        public Dictionary<Pawn, Faction> ColonyPeople = new Dictionary<Pawn, Faction>(); //pawn , ORIGINAL faction

        public bool isInitialized;

        public WorldCompSaveHandler(World world) : base(world) // Rimworld will initialize this on world load!
        {
            this.isInitialized = false;
        }

        public void LoadValuesForUpgrade(Dictionary<string, int> cfgVals, Dictionary<Pawn, Faction> colonyPpl)
        {
            this.ConfigValues = cfgVals;
            this.ColonyPeople = colonyPpl;
        }

        public bool IsValueSaved(string key) { return this.ConfigValues.ContainsKey(key); }
        public void RemoveConfigValue(string key) { this.ConfigValues.Remove(key); }

        public void TA_ExposeData(ConfigTabValueSavedAttribute attrib, ref int value, TA_Expose_Mode mode = TA_Expose_Mode.Load)
        {
            string saveName = attrib.SaveName;
            object defaultValue = attrib.DefaultValue;

            if (mode == TA_Expose_Mode.Save)
            {
                LogOutput.WriteLogMessage(Errorlevel.Debug, "Adding " + saveName + " : " + value + "to save dictionary");
                if (this.ConfigValues.ContainsKey(saveName))
                {
                    this.ConfigValues.Remove(saveName);
                }
                this.ConfigValues.Add(saveName, value);
            }
            else if (mode == TA_Expose_Mode.Load)
            {
                if (this.ConfigValues.TryGetValue(saveName, out int tempval))
                {
                    value = tempval;
                }
                else if (this.ConfigValues.TryGetValue(Enum.GetNames(typeof(TA_Expose_Name)).Contains(saveName) ? ((int)Enum.Parse(typeof(TA_Expose_Name), saveName)).ToString() : saveName, out tempval)) // TODO remove backwards compatability fallback
                {
                    value = tempval;
                    LogOutput.WriteLogMessage(Errorlevel.Information, "Value " + saveName + " was loaded via fallback. (A new save system is in place. But this message shouldnt appear anymore after saving)");
                }
                else
                {
                    LogOutput.WriteLogMessage(Errorlevel.Information, "Value " + saveName + " is not present. Using default value.");

                    if (defaultValue == null)
                        LogOutput.WriteLogMessage(Errorlevel.Error, $"Default value of {saveName} is null!");
                    else
                        value = (int)defaultValue;
                }

                LogOutput.WriteLogMessage(Errorlevel.Debug, "Successfully loaded " + saveName + " : " + value + "from save dictionary.");
            }
        }

        public override void ExposeData()
        {
            TechAdvancing_Config_Tab.worldCompSaveHandler = this;
            base.ExposeData();

            Scribe_Collections.Look(ref this.ConfigValues, "TA_Expose_Numbers", LookMode.Value, LookMode.Value);
            int isPplDictSaved = 1;
            //LogOutput.WriteLogMessage(Errorlevel.Information, "val:" + isPplDictSaved.ToString());
            Scribe_Values.Look(ref isPplDictSaved, "TA_Expose_People_isSaved", -1, true);
            //LogOutput.WriteLogMessage(Errorlevel.Information, "val:" + isPplDictSaved.ToString());
            if (this.ColonyPeople != null)
            {
                this.ColonyPeople.RemoveAll(x => x.Key == null);
            }
            if (isPplDictSaved == 1)
            {
                Scribe_Collections.Look(ref this.ColonyPeople, "TA_Expose_People", LookMode.Reference, LookMode.Reference);
                //LogOutput.WriteLogMessage(Errorlevel.Information, "Read TA_ExposePeople");
            }
            TechAdvancing_Config_Tab.ExposeData(TA_Expose_Mode.Load);
            if (this.ColonyPeople == null)
            {
                this.ColonyPeople = new Dictionary<Pawn, Faction>();
            }
            LogOutput.WriteLogMessage(Errorlevel.Information, "Loading finished.");

            TA_ResearchManager.FlushCfg();
        }
    }

    public enum TA_Expose_Mode
    {
        Save,
        Load
    }

    public enum TA_Expose_Name // TODO Remove soon
    {
        Conditionvalue_A,
        Conditionvalue_B,
        Conditionvalue_B_s,
        baseTechlvlCfg,
        configCheckboxNeedTechColonists,
        configCheckboxDisableCostMultiplicatorCap
    }
}