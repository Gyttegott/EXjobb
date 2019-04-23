using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace ES_PS_analyzer.Network
{
    public class JSONNetworkReader
    {
        private BlockingCollection<JObject> IncomingQueue;
        private Network.INetworkHandler ReadingClient;
        private Tools.IErrorLogHandler ErrorLogger;
        private Tools.IFileWriter FileWriter;
        private int ListeningPort;
        private string ListeningAddress;

        private TcpListener Server;

        public JSONNetworkReader(BlockingCollection<JObject> queue, Network.INetworkHandler sender, Tools.IFileWriter writer, Tools.IErrorLogHandler error, string address, int port)
        {
            IncomingQueue = queue;
            ListeningPort = port;
            ListeningAddress = address;
            ReadingClient = sender;
            ErrorLogger = error;
            FileWriter = writer;
        }

        public void ReadData()
        {
            Console.Write("Waiting for a connection... ");
            ReadingClient.Connect(ListeningAddress, ListeningPort);

            //Read data
            var ReadData = ReadingClient.RetrieveData();

            //Convert the read bytes to a string
            var data = Encoding.ASCII.GetString(ReadData);

            //Split the converted string into separate strings, each holding a single JSON log object
            var split = Regex.Split(data, "(?<=\\})\\n(?=\\{)");

            foreach (string log in split)
            {
                //Parse the logs into dynamic objects
                JObject ParsedLog;
                try
                {
                    ParsedLog = JObject.Parse(log);
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    Debug.WriteLine("Log entry failed parsing");
                    string filename = string.Format(@"{0}.txt", Guid.NewGuid());
                    ErrorLogger.LogError(string.Format("Parsing of incoming log failed, log content can be found in \"ParseErrors\\{0}\"", filename));
                    FileWriter.WriteFile(string.Format("ParseErrors\\{0}.txt", filename), Encoding.ASCII.GetBytes(log));
                    continue;
                }
                //If the parsed command has not been configured, drop it, it does not impact the risk curve
                /*try
                {
                    if (!ProgramData.RiskLookupTable.CommandExist((string)ParsedLog["powershell"]["command"]))
                    {
                        //Log the dropped command in a local text file for inspection of later inclusion
                        using (var writer = new StreamWriter("UnseenCommands.txt", true))
                        {
                            var pars = (JArray)ParsedLog["powershell"]["parameters"];
                            writer.WriteLine((string)ParsedLog["powershell"]["command"] + " " + (pars == null ? "" : string.Join(" ", pars)));
                        }
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(ParsedLog["powershell"].ToString());
                }*/

                //Insert the parsed log into the incoming log pool
                IncomingQueue.Add(ParsedLog);
            }

            ReadingClient.Disconnect();
        }
    }

}
