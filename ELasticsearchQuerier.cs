using Elasticsearch.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ES_PS_analyzer
{
    class ELasticsearchQuerier
    {
        private ElasticLowLevelClient Client;
        private string LimboIndex;

        public ELasticsearchQuerier(string Adress, string Index)
        {
            var settings = new ConnectionConfiguration(new Uri(Adress));
            Client = new ElasticLowLevelClient(settings);
            LimboIndex = Index;
        }

        public List<PSDoc> GetLimboCommandLogs(int Count)
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
        }

        public PSDoc GetLastCommand(string HostName)
        {
            var res = Client.Search<StringResponse>("logstash-*", string.Format(@"
{{
    ""from"": 0,
    ""size"": 1,
    ""query"": {{
        ""bool"": {{
            ""should"": [
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

            var searchobj = Newtonsoft.Json.Linq.JObject.Parse(res.Body);
            var Commands = JsonConvert.DeserializeObject<List<PSDoc>>(searchobj["hits"]["hits"].ToString());
            return (Commands.Count > 0 ? Commands[0] : null);
        }

        public void MigrateAndUpdateLog(string NewIndex, string LogId, string RiskLevel)
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
        }
    }

}
