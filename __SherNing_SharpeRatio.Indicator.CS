using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [UpdateOnEveryTick(true)]
    public class __SherNing_SharpeRatio : IndicatorObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Sharpe Ratio
        // Requirements : 
        // Type         : Indicator / Function
        // Version      : v1.0
        // Date         : 13 - Sep - 2020
        // Time Taken   : 
        // Remarks      : 
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      :
        // Client       : Sher Ning
        // Contact      : 
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.2 - Research
         *  a. Run some test on session infor and bar time
         *  b. Update on every tick will only work on real-time, historical is based on close tick
         * 
         *  1.0.1 - Requirements
         *  a. X sessions back sharpe ratio
         *  b. Annualize sharpe ratio
         *  c. if were to long or were to short sharpe ratio
         *  d. PAMA long term sharpe ratio
         *  e. PAMA MTF Sharpe Ratio
         *  f. Test on rader screen
         *  
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  1
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        public __SherNing_SharpeRatio(object _ctx) : base(_ctx) { }
        #region References
        VariableSeries<bool> IsSessionEnd;
        VariableSeries<double> EndOfSessionClosingPrice;
        protected override void Create()
        {
            IsSessionEnd = new VariableSeries<bool>(this);
            EndOfSessionClosingPrice = new VariableSeries<double>(this);

            Output.Clear();
        }
        #endregion

        DateTime CurrentDate;
        Calculator Calc;
        protected override void StartCalc()
        {
            PrintOnce = false;
            Calc = new Calculator(true);
        }

        bool PrintOnce;
        protected override void CalcBar()
        {
            // cache series variables
            IsSessionEnd.Value = Bars.LastBarInSession;

            AnnualizedSharpeRatioLongDirection();
        }

        #region Methods

        private void AnnualizedSharpeRatioLongDirection()
        {
            // reset all data on the first bar of the year
            if (FirstBarOfNewYearClosingTick())
            {
                Calc.ClearData();
                Calc.PrevSessionClose = Bars.Close[1];
            }

            // session closing tick
            if (SessionClosingTick())
            {
                double prevClose = Calc.PrevSessionClose;

                // current sessions returns if long in %
                double currentSessionReturn = ((Bars.Close[0] - prevClose) / prevClose) * 100;

                // add to list
                Calc.DataSet.Add(currentSessionReturn);

                // calculate sharpe ratio
                int length = Calc.DataSet.Count;
                double sharpe = Calc.SharpeRatio(length);
                double mean = Calc.Mean(length);

                // test by hand
                Print("Mean: " + mean);

                Print("Sharpe Ratio: " + sharpe);

                // update previous session close 
                Calc.PrevSessionClose = Bars.Close[0];
            }

            // calculate once at the end of each session
            if (IsSessionEnd[0] == false && IsSessionEnd[1])
            {
                //Print("Closing tick of new session bar");
                //Output.WriteLine("");
            }
        }
        private bool FirstBarOfNewYearClosingTick()
        {
            if (Bars.Time[0].Date == new DateTime(Bars.Time[0].Year, 1, 1) && IsSessionEnd[1])
            {
                return true;
            }

            return false;
        }
        private bool SessionClosingTick()
        {
            // works on both end of bar or intrabar calculation
            if (IsSessionEnd[0] && CurrentDate != Bars.Time[0].Date)
            {
                CurrentDate = Bars.Time[0].Date;
                return true;
            }

            return false;
        }
        #endregion

        #region Math
        class Calculator
        {
            // class properties
            public bool PopulationStdDev { get; set; }
            public double PrevSessionClose { get; set; }
            public List<double> DataSet { get; set; }

            // class constructor
            public Calculator(bool isPopulation)
            {
                PopulationStdDev = isPopulation;
                DataSet = new List<double>();
                PrevSessionClose = 1;
            }
            public void ClearData()
            {
                DataSet.Clear();
                PrevSessionClose = 1;
            }
            public double Mean(int length)
            {
                if (length <= 0) return 0;

                double sum = 0;
                for (int i = 0; i < length; i++)
                    sum += DataSet[i];

                return sum / length;
            }
            public double StdDev(int length)
            {
                // ensure that there is data to calculate
                if (DataSet.Count == 0) return 0;

                // mean = total sum / length
                double mean = Mean(length);

                // calculate variance sigma ^ 2
                double variance = 0;
                for (int i = 0; i < length; i++)
                    variance += Math.Pow(DataSet[i] - mean, 2);

                // calculate population variance
                if (PopulationStdDev) variance /= length;
                else variance /= (length - 1);

                // std dev = sqrt(variance)
                return Math.Sqrt(variance);
            }

            public double SharpeRatio(int length)
            {
                // ensure that there is data to calculate
                if (DataSet.Count == 0 || length == 0) return 0;

                // mean = total sum / length
                double mean = Mean(length);

                // calculate variance sigma ^ 2
                double variance = 0;
                for (int i = 0; i < length; i++)
                    variance += Math.Pow(DataSet[i] - mean, 2);

                // calculate population variance
                if (PopulationStdDev) variance /= length;
                else variance /= (length - 1);

                // std dev = sqrt(variance)
                double stdDev = Math.Sqrt(variance);

                // mean of excess returns / std dev, division by 0 check
                return stdDev != 0 ? mean / stdDev : 0;
            }
        }

        #endregion

        #region Print
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false || Environment.Optimizing) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
}