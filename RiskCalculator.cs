using System;
using System.Diagnostics;

namespace ES_PS_analyzer
{
    class RiskCalculator
    {
        //Time variables
        private MathNet.Numerics.Interpolation.CubicSpline TimeInterpolator;
        private double StartHour;
        

        public RiskCalculator(double StartHour, double EndHour)
        {
            // Construct the Akima Cubic Spline Interpolation
            this.StartHour = StartHour;
            double WorkHours = (EndHour < StartHour ? EndHour + 24 - StartHour : EndHour - StartHour);
            double EmptyHours = 24 - WorkHours;
            double[] X = { 0, WorkHours/2, WorkHours, WorkHours + (EmptyHours * .1), WorkHours + (EmptyHours * .5), 24 - (EmptyHours * .1), 24, 26 };
            double[] Y = { 1, 1, 1, 2, 2.5, 3, 1, 1 };
            this.TimeInterpolator = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkimaSorted(X, Y);

            Debug.WriteLine("[DEBUG] RiskCalculator: Setting start hour to  " + StartHour);
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Setting akima point [{0}->{8}, {1}->{9}, {2}->{10}, {3}->{11}, {4}->{12}, {5}->{13}, {6}->{14}, {7}->{15}].", X[0], X[1], X[2], X[3], X[4], X[5], X[6], X[7], Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6], Y[7]));
        }

        private double GetRiskTimeFactor(double HourOfDay)
        {
            double RelativeHour = (HourOfDay + 24 - StartHour) % 24;
            return TimeInterpolator.Interpolate(RelativeHour);
        }

        public double GetRisk(PSInfo CurrentCommand, PSInfo LastCommand = null)
        {
            double BaseLine = 0;
            if(LastCommand != null)
            {
                var TimeDiff = CurrentCommand.timestamp - LastCommand.timestamp;
                BaseLine = GetPreviousRiskContribution(LastCommand.powershell_risk, TimeDiff.TotalHours);
            }

            double CurrentHour = CurrentCommand.timestamp.Hour + ((double)CurrentCommand.timestamp.Minute / 60);
            double CurrentTimeFactor = GetRiskTimeFactor(CurrentHour);
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Time factor for {0} is {1}.", CurrentCommand.timestamp.ToString("o"), CurrentTimeFactor));
            double CommandBaseRisk = ProgramData.RiskLookupTable.getRisk(CurrentCommand);
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Base risk from previos value of {0} at {1} is {2}", BaseLine, LastCommand == null ? "never" : LastCommand.timestamp.ToString("o"), BaseLine));
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Risk for command '{0}' is {1}.", CurrentCommand.powershell_command, CommandBaseRisk));

            return BaseLine + CurrentTimeFactor * CommandBaseRisk;
        }

        private double GetPreviousRiskContribution(double PreviousRisk, double HoursPassed)
        {
            return PreviousRisk / Math.Pow(2, HoursPassed/4);
        }
    }

}
