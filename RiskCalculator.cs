using System;
using System.Diagnostics;

namespace ES_PS_analyzer
{
    /// <summary>
    /// Main risk calculator used for calculating risks for commands. Unlike RiskLookup class it takes things like previous commands and other factors into consideration.
    /// </summary>
    class RiskCalculator
    {
        //Time variables used for calculating extra factors for time of command execution
        private MathNet.Numerics.Interpolation.CubicSpline TimeInterpolator;
        private double StartHour;


        /// <summary>
        /// Constructs a new RiskCalculator
        /// </summary>
        /// <param name="StartHour">The starting time of the workday given in hours of range (0, 24]</param>
        /// <param name="EndHour">The ending time of the workday given in hours of range (0, 24]</param>
        public RiskCalculator(double StartHour, double EndHour)
        {
            //Force parameters to correct range
            StartHour = StartHour % 24;
            EndHour = EndHour % 24;

            // Construct the Akima Cubic Spline Interpolation for range (0, 24] of hours since work began
            this.StartHour = StartHour;
            double WorkHours = (EndHour < StartHour ? EndHour + 24 - StartHour : EndHour - StartHour);
            double EmptyHours = 24 - WorkHours;
            double[] X = { 0, WorkHours/2, WorkHours, WorkHours + (EmptyHours * .1), WorkHours + (EmptyHours * .5), 24 - (EmptyHours * .1), 24, 26 };
            double[] Y = { 1, 1, 1, 2, 2.5, 3, 1, 1 };
            this.TimeInterpolator = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkimaSorted(X, Y);

            Debug.WriteLine("[DEBUG] RiskCalculator: Setting start hour to  " + StartHour);
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Setting akima point [{0}->{8}, {1}->{9}, {2}->{10}, {3}->{11}, {4}->{12}, {5}->{13}, {6}->{14}, {7}->{15}].", X[0], X[1], X[2], X[3], X[4], X[5], X[6], X[7], Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6], Y[7]));
        }

        /// <summary>
        /// Calculates the factor of which command risks should be multiplied based on the time of day it was executed
        /// </summary>
        /// <param name="HourOfDay">The time of the day given in hours of range (0, 24]</param>
        /// <returns>The factor to multiply risks with</returns>
        private double GetRiskTimeFactor(double HourOfDay)
        {
            //Remake the given hour to a relative hour indicating hours passed since work began
            //must be done since the Akima interpolation is constructed accordingly
            double RelativeHour = (HourOfDay + 24 - StartHour) % 24;
            return TimeInterpolator.Interpolate(RelativeHour);
        }

        /// <summary>
        /// Calculates the final risk value of a command given its context and the context of the previously run command.
        /// </summary>
        /// <param name="CurrentCommand">The command to calculate the risk for</param>
        /// <param name="LastCommand">The command run before the current one</param>
        /// <returns>A number representing the final risk of the executed command</returns>
        public double GetRisk(PSInfo CurrentCommand, PSInfo LastCommand = null)
        {
            //set the inital risk to 0
            double BaseLine = 0;
            //Calculate the time difference between the current command and the previous one, then calculate the risk the previous command brings to the equation
            if(LastCommand != null)
            {
                var TimeDiff = CurrentCommand.timestamp - LastCommand.timestamp;
                BaseLine = GetPreviousRiskContribution(LastCommand.powershell_risk, TimeDiff.TotalHours);
            }

            //Calculate the factor for the time of day the current command was executed
            double CurrentHour = CurrentCommand.timestamp.Hour + ((double)CurrentCommand.timestamp.Minute / 60);
            double CurrentTimeFactor = GetRiskTimeFactor(CurrentHour);
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Time factor for {0} is {1}.", CurrentCommand.timestamp.ToString("o"), CurrentTimeFactor));
            //Look up what risk the current command and its context has independently
            double CommandBaseRisk = ProgramData.RiskLookupTable.getRisk(CurrentCommand);
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Base risk from previos value of {0} at {1} is {2}", BaseLine, LastCommand == null ? "never" : LastCommand.timestamp.ToString("o"), BaseLine));
            Debug.WriteLine(string.Format("[DEBUG] RiskCalculator: Risk for command '{0}' is {1}.", CurrentCommand.powershell_command, CommandBaseRisk));

            //Calculate the complete command risk
            return BaseLine + CurrentTimeFactor * CommandBaseRisk;
        }

        /// <summary>
        /// Calculates the risk the previously executed command that should be passed on based on its risk and the time elapsed since its execution
        /// </summary>
        /// <param name="PreviousRisk">The previous command risk</param>
        /// <param name="HoursPassed">The numer of hours passed since the previous execution</param>
        /// <returns></returns>
        private double GetPreviousRiskContribution(double PreviousRisk, double HoursPassed)
        {
            return PreviousRisk / Math.Pow(2, HoursPassed/4);
        }
    }

}
