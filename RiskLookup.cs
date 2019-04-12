using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ES_PS_analyzer
{
    /// <summary>
    /// This class represents configuration of a single command regarding risk levels.
    /// It contains a base risk for the command and additional custom definitions of extra risks based on the commands context.
    /// </summary>
    class Mapping
    {
        // The command used
        public string Command { get; set; }

        // The base risk a command has regardless of context
        public double BaseRisk { get; set; }

        // A list of all criterias for additional risks inside the commands possible contexts
        public List<CustomQuery> RiskQueries { get; set; }
    }

    /// <summary>
    /// This class contains a custom definition of extra risks based on a commands context
    /// </summary>
    class CustomQuery
    {
        //What type of evaluation should be done
        public string Type { get; set; }

        //What value the evaluation should use
        public string Value { get; set; }

        //Where to apply the evaluation
        public string Location { get; set; }

        //How much extra risk a positive evaluation should give
        public double RiskAddition { get; set; }

        /// <summary>
        /// Given a PSInfo class this function returns the part of which evaluation for extra risks should be done
        /// </summary>
        private Func<PSInfo, string[]> GetSource;

        /// <summary>
        /// Given a source string from GetSource calculates if requirements for additional risk is fulfilled
        /// <returns>A boolean indicating if extra risk should be applied</returns>
        /// </summary>
        private Func<string, bool> GetEvaluation;

        /// <summary>
        /// Calculates and returns the additonal risk a given command yields according to the defined custom query this object represents
        /// </summary>
        /// <param name="Log">The specific command and its context</param>
        /// <returns>The amount of additional risk that should be applied to a command</returns>
        public double GetAdditionalRisk(PSInfo Log)
        {
            double res = 0;

            //loop through each source given and apply the query to each, sum up all hits
            foreach(string source in GetSource(Log))
            {
                if (GetEvaluation(source))
                    res += RiskAddition;
            }
            return res;
        }

        /// <summary>
        /// Assigns correct delegate functions for quick risk calculations based on assigned values to the object
        /// <para>Must be run before any risk calculations are performed!</para>
        /// <exception cref="System.Exception">Thrown when properties used for delegate assignment does not match predefined values</exception>
        /// </summary>
        public void ConstructQueryWorkflow()
        {
            //Empty array used for empty sources
            string[] EmptyPara = { };

            //Assign evaluation delegate based on assigned evaluation type
            switch (Type)
            {
                case "regex":
                    GetEvaluation = x => Regex.IsMatch(x, Value);
                    break;
                default:
                    throw new Exception("Mappings configuration error");
            }

            //Assign delegate for retrieving the source for evaluation from a command context
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

    /// <summary>
    /// Used for calculating risks for commands and their contexts.
    /// The configuration for evaluating risks is read from the file "CommandRiskMappings.json"
    /// </summary>
    class RiskLookup
    {
        //Dictionary used for finding configurations for specific commands
        private Dictionary<string, Mapping> RiskDict;

        //The list of all configured risk assesements for commands
        List<Mapping> RiskMappings;

        /// <summary>
        /// Default constructor
        /// <exception cref="System.Exception">Thrown when extra criterias in CommandRiskMappings.json are misconfigured</exception>
        /// </summary>
        public RiskLookup()
        {
            try
            {
                RiskDict = new Dictionary<string, Mapping>();
                //Deserialize configurations in file from json
                RiskMappings = JsonConvert.DeserializeObject<List<Mapping>>(File.ReadAllText("CommandRiskMappings.json"));
                foreach(var mapping in RiskMappings)
                {
                    //Hash all commands for quick configuration access later
                    RiskDict[mapping.Command] = mapping;

                    //Fix missing criterias for additional risk with an empty list
                    if (mapping.RiskQueries == null)
                    {
                        mapping.RiskQueries = new List<CustomQuery>();
                    }
                    else
                    {
                        //If extra criterias are present, configure each object so that it evaluates extra risk based on its values
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

        /// <summary>
        /// Calculates the risk for a command and its context
        /// </summary>
        /// <param name="Command">The command and its context</param>
        /// <returns>A number representing the risk the given command and context pose</returns>
        public double getRisk(PSInfo Command)
        {
            //Return 0 (no risk) if the command is not configured
            Mapping Conf;
            try
            {
                Conf = RiskDict[Command.powershell_command];
            }
            catch
            {
                return 0;
            }

            //Calculate the base risk plus extra risks for all fulfilled criterias
            double risk = Conf.BaseRisk;
            foreach(var extra in Conf.RiskQueries)
            {
                risk += extra.GetAdditionalRisk(Command);
            }

            return risk;
        }

        /// <summary>
        /// Checks if a command has been configured
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A bool indicating if the command has been configured</returns>
        public bool CommandExist(string command)
        {
            return RiskDict.ContainsKey(command);
        }
    }
}
