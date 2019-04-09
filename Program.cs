using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Runtime.Caching;
using System.Diagnostics;
using System.Collections;

namespace ES_PS_analyzer
{
    class PSDoc
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_source")]
        public PSInfo Source {get;set;}
    }

    class PSInfo
    {
        public string powershell_main_command { get; set; }

        public string powershell_script_name { get; set; }

        public string powershell_command { get; set; }

        public string powershell_host_application { get; set; }

        public List<string> powershell_parameters { get; set; }

        [JsonProperty("@timestamp")]
        public DateTime timestamp { get; set; }

        public double powershell_risk { get; set; }

        public string computer_name { get; set; }

    }

    class Program
    {

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            // Construct object containing the program settings
            ProgramSettings Configuration;
            try
            {
                Console.WriteLine("Loading program settings...");
                Configuration = new ProgramSettings();
            }
            catch
            {
                Console.WriteLine("Program settings could not be loaded!");
                return;
            }
            Console.WriteLine("Program settings successfully loaded!");



            var StartHour = DateTime.ParseExact(Configuration.getSetting("WorkHoursStart"), "H:mm", null, System.Globalization.DateTimeStyles.None);
            var EndHour = DateTime.ParseExact(Configuration.getSetting("WorkHoursEnd"), "H:mm", null, System.Globalization.DateTimeStyles.None);
            var RiskCalculator = new RiskCalculator(StartHour.Hour + (StartHour.Minute / 60), EndHour.Hour + (EndHour.Minute / 60));

            var CommandCache = new MemoryCache("PowerShellCommands");


            // Set up a connection object to ElasticSearch
            var ESClient = new ELasticsearchQuerier(Configuration.getSetting("ConnectionUrl"), Configuration.getSetting("PreprocessIndex"));



            PSDoc LastRunCommand = null;
            Dictionary<string, bool> PreviousBatchLogs = new Dictionary<string, bool>();
            
            while (true)
            {
                var LimboCommands = ESClient.GetLimboCommandLogs(10);
                Debug.WriteLine("[DEBUG] Queried new logs, found " + LimboCommands.Count());
                bool GotAllLogs = LimboCommands.Count() < 10 ? true : false;

                //Filter out any previously processed logs since Elasticsearch may not have had time to delete them yet
                LimboCommands.RemoveAll(x => PreviousBatchLogs.TryGetValue(x.Id, out var DontCareAtAll));

                //sort found logs such that hosts are grouped and in chronological order with oldest command first
                foreach (var Command in LimboCommands.OrderBy(x => x.Source.computer_name).ThenBy(y => y.Source.timestamp))
                {
                    //Add the command to dictionary of processed commands
                    PreviousBatchLogs[Command.Id] = true;

                    Debug.WriteLine(string.Format("[DEBUG]Processing log for {0} from {1}.", Command.Source.computer_name, Command.Source.timestamp.ToString("o")));
                    //Check if saved last command is present or found in cache, otherwise fetch it from ElasticSearch
                    if (LastRunCommand == null || Command.Source.computer_name != LastRunCommand.Source.computer_name)
                    {
                        if (LastRunCommand != null)
                        {
                            //Switching host so switch cached last commands as well
                            Debug.WriteLine("[DEBUG] Processing new host, looking for last run command in cache.");
                            CommandCache[LastRunCommand.Source.computer_name] = LastRunCommand;
                        }
                        LastRunCommand = (PSDoc)CommandCache[Command.Source.computer_name];
                        if(LastRunCommand == null)
                        {
                            //Nothing in cache, fetch from Elasticsearch instead
                            Debug.WriteLine("[DEBUG] Last run command for computer was not found in cache.");
                            LastRunCommand = ESClient.GetLastCommand(Command.Source.computer_name);
                        }
                            
                    }
                    Debug.WriteLineIf(Command.Source.computer_name == LastRunCommand.Source.computer_name, "[DEBUG] Same host as previous log, keeping last run command.");

                    //Calculate the risk level for the command
                    var RiskLevel = RiskCalculator.GetRisk(Command.Source, LastRunCommand.Source);
                    Command.Source.powershell_risk = RiskLevel;

                    //Update the log in elasticsearch, put it in the correct index and update the last run command variable
                    ESClient.MigrateAndUpdateLog(string.Format("logstash-{0}.{1}.{2}", Command.Source.timestamp.Year, Command.Source.timestamp.Month.ToString("00"), Command.Source.timestamp.Day.ToString("00")), Command.Id, RiskLevel.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                    LastRunCommand = Command;
                }

                //sleep for 10 sec to wait for more logs
                if (GotAllLogs)
                {
                    //Reset the dictionary of previously processed commands since Elasticsearch now gets time to catch up
                    PreviousBatchLogs = new Dictionary<string, bool>();

                    Debug.WriteLine("[DEBUG] No more logs, entering sleep.");
                    System.Threading.Thread.Sleep(10000);
                }
            }


        }

    }

}
