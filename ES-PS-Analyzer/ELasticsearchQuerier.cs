using Elasticsearch.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ES_PS_analyzer
{
    /// <summary>
    /// Used by the ElasticSearch client for the results of log queries
    /// </summary>
    class PSDoc
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_source")]
        public PSInfo Source {get;set;}
    }

    /// <summary>
    /// Client used for querying the ElasticSearch server(s)
    /// </summary>
    class ELasticsearchQuerier
    {
        //Third party client used for querying
        private ElasticLowLevelClient Client;
        //private string LimboIndex;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="Adress">The address to the ElasticSearch cluster</param>
        public ELasticsearchQuerier(string Adress)
        {
            //Construct the third party client
            var settings = new ConnectionConfiguration(new Uri(Adress));
            Client = new ElasticLowLevelClient(settings);
            //LimboIndex = Index;
        }

        /*public List<PSDoc> GetLimboCommandLogs(int Count)
        {
            string req = string.Format(@"
{{
    ""from"": 0,
    ""size"": {0},
    ""query"": {{
        ""bool"": {{
            ""filter"": [
                {{
                    ""range"": {{
                        ""@timestamp"": {{
                            ""lte"": ""now-1m""
                        }}
                    }}
                }}
            ]
        }}
    }},
    ""sort"": [
        {{
            ""@timestamp"": {{
                ""order"": ""asc""
            }}
        }}
    ]
}}", Count);
            var res = Client.Search<StringResponse>(LimboIndex, req);

            Debug.WriteLineIf(!res.Success, "[DEBUG] Could not query for limbo logs!");

            var searchobj = Newtonsoft.Json.Linq.JObject.Parse(res.Body);
            return JsonConvert.DeserializeObject<List<PSDoc>>(searchobj["hits"]["hits"].ToString());
        }*/

        /// <summary>
        /// Queries ElasticSearch for the last run command by a specific host
        /// </summary>
        /// <param name="HostName">The full name of the host to search for</param>
        /// <returns>The found command, null otherwise</returns>
        public async Task<PSDoc> GetLastCommand(string HostName)
        {
            //Make an async call to block the current thread until a result is given
            var res = await Client.SearchAsync<StringResponse>("logstash-*", string.Format(@"
{{
    ""from"": 0,
    ""size"": 1,
    ""query"": {{
        ""bool"": {{
            ""must"": [
                {{
                    ""match"": {{
                        ""source_name"": ""PowerShell""
                    }}
                }},
                {{
                    ""match"": {{
                        ""computer_name"": ""{0}""
                    }}
                }}
            ]
        }}
    }},
    ""sort"": [
        {{
            ""@timestamp"": {{
                ""order"": ""asc""
            }}
        }}
    ]
}}", HostName));

            Debug.WriteLineIf(!res.Success, "[DEBUG] Could not query for last commands!");

            //Parse the result and return it
            var searchobj = Newtonsoft.Json.Linq.JObject.Parse(res.Body);
            var Commands = JsonConvert.DeserializeObject<List<PSDoc>>(searchobj["hits"]["hits"].ToString());
            return (Commands.Count > 0 ? Commands[0] : null);
        }

        /*public void MigrateAndUpdateLog(string NewIndex, string LogId, string RiskLevel)
        {
            var res = Client.Reindex<StringResponse>(string.Format(@"
{{
    ""size"": 1,
    ""source"": {{
        ""index"": ""{0}"",
        ""query"": {{
            ""match"": {{""_id"": ""{2}""}}
        }}
    }},
    ""dest"": {{
        ""index"": ""{1}""
    }},
    ""script"": {{
        ""source"": ""ctx._source.powershell_risk = {3};"",
        ""lang"": ""painless""
    }}
}}", LimboIndex, NewIndex, LogId, RiskLevel));

            Debug.WriteLineIf(!res.Success, "[DEBUG] Could not migrate log to new index!");

            var res2 = Client.Delete<StringResponse>(LimboIndex, "doc", LogId);

            Debug.WriteLineIf(!res2.Success, "[DEBUG] Could not delete the old log");
        }*/
    }

}
