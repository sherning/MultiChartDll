using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class __SherNing_RelativeMomentum : IndicatorObject
    {
        //==============================================================================
        // Name           : __SherNing_RelativeMomentum
        // Description    : Relative Momentum Indicator
        // Version        : v.1.0.0
        // Date Created   : 25 - July - 2020
        // Time Taken     : 
        // Remarks        :
        //==============================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==============================================================================

        /* ------------------------------- Version 1.0.0 -------------------------------
         * 
         */
        [Input] public int NumOfDataSeries { get; set; }
        [Input] public int CountDays { get; set; }

        public __SherNing_RelativeMomentum(object _ctx) : base(_ctx) 
        {
            NumOfDataSeries = 2;
            CountDays = 6;
        }

        RelativeMomentum MomentumFunction;

        protected override void Create()
        {
            MomentumFunction = new RelativeMomentum(this);
            Output.Clear();
        }
        protected override void StartCalc()
        {
            MomentumFunction.NumOfSeries = NumOfDataSeries;
            MomentumFunction.LengthDays = CountDays;
        }
        protected override void CalcBar()
        {
            MomentumFunction.Call();
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

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

namespace PowerLanguage.Function
{
    public sealed class RelativeMomentum : FunctionSeries<double>
    {
        //==============================================================================
        // Name           : __SherNing_RelativeMomentum
        // Description    : Function for Relative Momentum Indicator
        // Version        : v.1.0.0
        // Date Created   : 25 - July - 2020
        // Time Taken     : 
        // Remarks        :
        //==============================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==============================================================================

        /* ------------------------------- Version 1.0.0 -------------------------------
         * 
         */

        public int NumOfSeries { get; set; }
        public int LengthDays { get; set; }

        public RelativeMomentum(CStudyControl m) : base(m)
        {
            // check that numofseries cannot be 0 or greater than 5
            if (NumOfSeries == 0 || NumOfSeries > 5)
                NumOfSeries = 5;
        }

        List<VariableSeries<double>> PriceSeries;
        VariableSeries<bool> EndOfSession;
        int Count;

        protected override void Create()
        {
            PriceSeries = new List<VariableSeries<double>>();
            EndOfSession = new VariableSeries<bool>(this);

            for (int i = 0; i < NumOfSeries; i++)
                PriceSeries.Add(new VariableSeries<double>(this));
        }
        protected override void StartCalc()
        {
            if (LengthDays == 0)
                LengthDays = 1;

            Count = 0;
        }
        protected override double CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                for (int i = 1; i <= NumOfSeries; i++)
                {
                    if (BarsOfData(i) != null)
                        PriceSeries[i].Value = BarsOfData(i).CloseValue;
                }

                CountDays();
            }
            
            return 0;
        }

        private bool CloseOfFirstBarInNewSession()
        {
            EndOfSession.Value = Bars.LastBarInSession;

            // previous bar is last bar on chart
            if (EndOfSession[0] == false && EndOfSession[1] == true)
                return true;

            return false;
        }

        private void CountDays()
        {
            if (Bars.LastBarInSession)
                Count++;

            if (Count == 6)
            {
                Print("6 days");
                Count = 0;
            }
        }
        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

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
