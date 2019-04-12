using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ES_PS_analyzer
{
    class Mapping
    {
        public string Command { get; set; }
        public double BaseRisk { get; set; }
        public List<CustomQuery> RiskQueries { get; set; }
    }

    class CustomQuery
    {
        public string Type { get; set; }

        public string Value { get; set; }

        public string Location { get; set; }

        public double RiskAddition { get; set; }

        private Func<PSInfo, string[]> GetSource;

        private Func<string, bool> GetEvaluation;

        public double GetAdditionalRisk(PSInfo Log)
        {
            double res = 0;
            foreach(string source in GetSource(Log))
            {
                if (GetEvaluation(source))
                    res += RiskAddition;
            }
            return res;
        }

        public void ConstructQueryWorkflow()
        {
            string[] EmptyPara = { };
            switch (Type)
            {
                case "regex":
                    GetEvaluation = x => Regex.IsMatch(x, Value);
                    break;
                default:
                    throw new Exception("Mappings configuration error");
            }

            switch (Location)
            {
                case "parameter":
                    GetSource = x => x.powershell_parameters == null ? EmptyPara : x.powershell_parameters.ToArray();
                    break;
                default:
                    throw new Exception("Mappings configuration error");
            }
        }
    }
    class RiskLookup
    {
        private Dictionary<string, Mapping> RiskDict;
        List<Mapping> RiskMappings;

        public RiskLookup()
        {
            try
            {
                RiskDict = new Dictionary<string, Mapping>();
                RiskMappings = JsonConvert.DeserializeObject<List<Mapping>>(File.ReadAllText("CommandRiskMappings.json"));
                foreach(var mapping in RiskMappings)
                {
                    RiskDict[mapping.Command] = mapping;

                    if (mapping.RiskQueries == null)
                    {
                        mapping.RiskQueries = new List<CustomQuery>();
                    }
                    else
                    {
                        foreach (var extra in mapping.RiskQueries)
                        {
                            extra.ConstructQueryWorkflow();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public double getRisk(PSInfo Command)
        {
            Mapping Conf;
            try
            {
                Conf = RiskDict[Command.powershell_command];
            }
            catch
            {
                return 0;
            }

            double risk = Conf.BaseRisk;
            foreach(var extra in Conf.RiskQueries)
            {
                risk += extra.GetAdditionalRisk(Command);
            }

            return risk;
        }

        public bool CommandExist(string command)
        {
            return RiskDict.ContainsKey(command);
        }
    }
}
