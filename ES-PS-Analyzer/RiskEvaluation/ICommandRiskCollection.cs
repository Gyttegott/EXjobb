using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ES_PS_analyzer.RiskEvaluation
{
    interface ICommandRiskCollection
    {
        double GetBaseRisk(PSInfo Command);

        bool CommandExists(string command);
    }
}
