﻿#region Copyright
//  Copyright 2016  OSIsoft, LLC
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion
using System;
using System.Linq;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.UnitsOfMeasure;
using System.Collections.Generic;

namespace Ex4_Building_An_AF_Hierarchy_Sln
{
    static class Bonus
    {
        public static void Run()
        {
            AFDatabase database = GetOrCreateDatabase("PISRV01", "Ethical Power Company");
            CreateCategories(database);
            CreateEnumerationSets(database);
            CreateTemplates(database);
            CreateElements(database);
            SetAttributeValues(database);
            CreateCityElements(database);
            CreateWeakReferences(database);
        }

        private static AFDatabase GetOrCreateDatabase(string servername, string databasename)
        {
            PISystem assetServer = new PISystems()[servername];
            if (assetServer == null)
                return null;
            AFDatabase database = assetServer.Databases[databasename];
            if (database == null)
                database = assetServer.Databases.Add(databasename);
            return database;
        }

        private static void CreateCategories(AFDatabase database)
        {
            if (database == null) return;
            var items = new List<string>();
            items.Add("Measures Energy");
            items.Add("Shows Status");
            items.Add("Building Info");
            items.Add("Location");
            items.Add("Time - Series Data");
            foreach (var item in items)
            {
                if (!database.ElementCategories.Contains(item))
                    database.ElementCategories.Add(item);
            }
            if (database.IsDirty)
                database.CheckIn();
        }


        private static void CreateEnumerationSets(AFDatabase database)
        {
            if (database == null) return;

            if (!database.EnumerationSets.Contains("Building Type"))
            {
                AFEnumerationSet bTypeEnum = database.EnumerationSets.Add("Building Type");
                bTypeEnum.Add("Residential", 0);
                bTypeEnum.Add("Business", 1);
            }

            if (!database.EnumerationSets.Contains("Meter Status"))
            {
                AFEnumerationSet mStatusEnum = database.EnumerationSets.Add("Meter Status");
                mStatusEnum.Add("Good", 0);
                mStatusEnum.Add("Bad", 1);
            }
            if (database.IsDirty)
                database.CheckIn();
        }

        private static void CreateTemplates(AFDatabase database)
        {
            if (database == null) return;

            UOM uom = database.PISystem.UOMDatabase.UOMs["kilowatt hour"];

            AFCategory mEnergyE = database.ElementCategories["Measures Energy"];
            AFCategory sStatusE = database.ElementCategories["Shows Status"];

            AFCategory bInfoA = database.AttributeCategories["Building Info"];
            AFCategory locationA = database.AttributeCategories["Location"];
            AFCategory tsDataA = database.AttributeCategories["Time-Series Data"];

            AFEnumerationSet bTypeNum = database.EnumerationSets["Building Type"];
            AFEnumerationSet mStatusEnum = database.EnumerationSets["Meter Status"];

            // Create MeterBasic Element Template

            AFElementTemplate meterBasicTemplate;
            if (!database.ElementTemplates.Contains("MeterBasic"))
            {
                meterBasicTemplate = database.ElementTemplates.Add("MeterBasic");
                meterBasicTemplate.Categories.Add(mEnergyE);

                AFAttributeTemplate substationAttrTemp = meterBasicTemplate.AttributeTemplates.Add("Substation");
                substationAttrTemp.Type = typeof(string);

                AFAttributeTemplate usageLimitAttrTemp = meterBasicTemplate.AttributeTemplates.Add("Usage Limit");
                usageLimitAttrTemp.Type = typeof(string);
                usageLimitAttrTemp.DefaultUOM = uom;

                AFAttributeTemplate buildingAttrTemp = meterBasicTemplate.AttributeTemplates.Add("Building");
                buildingAttrTemp.Type = typeof(string);
                buildingAttrTemp.Categories.Add(bInfoA);

                AFAttributeTemplate bTypeAttrTemp = meterBasicTemplate.AttributeTemplates.Add("Building Type");
                bTypeAttrTemp.TypeQualifier = bTypeNum;
                bTypeAttrTemp.Categories.Add(bInfoA);

                AFAttributeTemplate cityAttrTemp = meterBasicTemplate.AttributeTemplates.Add("City");
                cityAttrTemp.Type = typeof(string);
                cityAttrTemp.Categories.Add(locationA);

                AFAttributeTemplate energyUsageAttrTemp = meterBasicTemplate.AttributeTemplates.Add("Energy Usage");
                energyUsageAttrTemp.Type = typeof(double);
                energyUsageAttrTemp.Categories.Add(tsDataA);
                energyUsageAttrTemp.DefaultUOM = uom;
                energyUsageAttrTemp.DataReferencePlugIn = database.PISystem.DataReferencePlugIns["PI Point"];
                energyUsageAttrTemp.ConfigString = @"\\%@\Configuration|PIDataArchiveName%\%Element%.%Attribute%;UOM=kWh";
            }
            else
                meterBasicTemplate = database.ElementTemplates["MeterBasic"];

            // Create MeterAdvanced Element Template

            if (!database.ElementTemplates.Contains("MeterAdvanced"))
            {
                AFElementTemplate meterAdvancedTemplate = database.ElementTemplates.Add("MeterAdvanced");
                meterAdvancedTemplate.BaseTemplate = meterBasicTemplate;

                AFAttributeTemplate statusAttrTemp = meterAdvancedTemplate.AttributeTemplates.Add("Status");
                statusAttrTemp.TypeQualifier = mStatusEnum;
                statusAttrTemp.Categories.Add(tsDataA);
                statusAttrTemp.DataReferencePlugIn = database.PISystem.DataReferencePlugIns["PI Point"];
                statusAttrTemp.ConfigString = @"\\%@\Configuration|PIDataArchiveName%\%Element%.%Attribute%";

                // Create city Element Template

                AFElementTemplate cityTemplate = database.ElementTemplates.Add("City");

                AFAttributeTemplate cityEnergyUsageAttrTemp = cityTemplate.AttributeTemplates.Add("Energy Usage");
                cityEnergyUsageAttrTemp.Type = typeof(double);
                cityEnergyUsageAttrTemp.DefaultUOM = uom;
                cityEnergyUsageAttrTemp.DataReferencePlugIn = database.PISystem.DataReferencePlugIns["PI Point"];
                cityEnergyUsageAttrTemp.ConfigString = @"\\%@\Configuration|PIDataArchiveName%\%Element%.%Attribute%";
            }

            // Do a checkin at the end instead of one-by-one.

            if (database.IsDirty)
                database.CheckIn();
        }

        private static void CreateElements(AFDatabase database)
        {
            if (database == null) return;

            // here we create the configuration element
            // we do a small exception creating an attribute in this method.
            AFElement configuration;
            if (!database.Elements.Contains("Configuration"))
            {
                configuration = database.Elements.Add("Configuration");
                AFAttribute name= configuration.Attributes.Add("PIDataArchiveName");
                name.SetValue(new AFValue("PISRV01"));
            }
               

            AFElement meters;
            meters = database.Elements["Meters"];
            if (meters == null)
                meters = database.Elements.Add("Meters");

            AFElementTemplate basic = database.ElementTemplates["MeterBasic"];
            AFElementTemplate advanced = database.ElementTemplates["MeterAdvanced"];

            foreach (int i in Enumerable.Range(1, 12))
            {
                string name = "Meter" + i.ToString("D3");
                if (!meters.Elements.Contains(name))
                {
                    AFElementTemplate eTemp = i <= 8 ? basic : advanced;
                    AFElement e = meters.Elements.Add(name, eTemp);
                }
            }
            if (database.IsDirty)
                database.CheckIn();
        }

        /// <summary>
        /// This methods fills the static attributes for the first meter only.
        /// In real situation, the configuration would come from a third party source e.g. xml or sql query... 
        /// </summary>
        /// <param name="database"></param>
        private static void SetAttributeValues(AFDatabase database)
        {
            if (database == null) return;

            AFElement meter001 = database.Elements["Meters"].Elements["Meter001"];
            meter001.Attributes["Substation"].SetValue(new AFValue("SSA-01"));
            meter001.Attributes["Usage Limit"].SetValue(new AFValue(350));
            meter001.Attributes["Building"].SetValue(new AFValue("The Shard"));

            AFEnumerationValue bTypeValue = database.EnumerationSets["Building Type"]["Residential"];
            meter001.Attributes["Building Type"].SetValue(new AFValue(bTypeValue));
            meter001.Attributes["City"].SetValue(new AFValue("London"));
        }

        private static void CreateCityElements(AFDatabase database)
        {
            if (database == null) return;

            if (!database.Elements.Contains("Geographical Locations"))
            {
                AFElement geoLocations = database.Elements.Add("Geographical Locations");
                AFElementTemplate cityTemplate = database.ElementTemplates["City"];

                geoLocations.Elements.Add("London", cityTemplate);
                geoLocations.Elements.Add("Montreal", cityTemplate);
                geoLocations.Elements.Add("San Francisco", cityTemplate);
            }
            if (database.IsDirty)
                database.CheckIn();
        }

        private static void CreateWeakReferences(AFDatabase database)
        {
            if (database == null) return;

            AFReferenceType weakRefType = database.ReferenceTypes["Weak Reference"];

            AFElement meters = database.Elements["Meters"];

            AFElement london = database.Elements["Geographical Locations"].Elements["London"];
            london.Elements.Add(meters.Elements["Meter001"], weakRefType);
            london.Elements.Add(meters.Elements["Meter002"], weakRefType);
            london.Elements.Add(meters.Elements["Meter003"], weakRefType);
            london.Elements.Add(meters.Elements["Meter004"], weakRefType);

            AFElement sanFrancisco = database.Elements["Geographical Locations"].Elements["San Francisco"];
            sanFrancisco.Elements.Add(meters.Elements["Meter005"], weakRefType);
            sanFrancisco.Elements.Add(meters.Elements["Meter006"], weakRefType);
            sanFrancisco.Elements.Add(meters.Elements["Meter007"], weakRefType);
            sanFrancisco.Elements.Add(meters.Elements["Meter008"], weakRefType);

            AFElement montreal = database.Elements["Geographical Locations"].Elements["Montreal"];
            montreal.Elements.Add(meters.Elements["Meter009"], weakRefType);
            montreal.Elements.Add(meters.Elements["Meter010"], weakRefType);
            montreal.Elements.Add(meters.Elements["Meter011"], weakRefType);
            montreal.Elements.Add(meters.Elements["Meter012"], weakRefType);

            if (database.IsDirty)
                database.CheckIn();
        }
    }
}
