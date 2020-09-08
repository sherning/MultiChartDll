using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    public class __SherNing_TripleTimeFrame : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Triple Time Frame
        // Requirements : 
        // Type         : Strategy (IOG.Disabled)
        // Version      : v1.0
        // Date         : 08 - Sep - 2020
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
         *  1.0.1 - Requirements
         *  Strategy will be traded on the 60 min chart. 
         *  Strategy will have one stop loss and one profit target of 3 R
         *        
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  1
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Inputs
        [Input] public string PamaSettings { get; set; }
        [Input] public int PamaLength { get; set; }
        [Input] public int PamaSmooth { get; set; }
        public __SherNing_TripleTimeFrame(object _ctx) : base(_ctx)
        {
            // default settings
            PamaSettings = "Pama Setting";
            PamaLength = 6;
            PamaSmooth = 10;

        }
        #endregion

        #region References
        PriceActionMovingAverageFunction Pamaf;
        VariableSeries<double>[] Pama;

        protected override void Create()
        {
            Pamaf = new PriceActionMovingAverageFunction(this);

            Pama = new VariableSeries<double>[4];
            for (int i = 0; i < Pama.Length; i++)
                Pama[i] = new VariableSeries<double>(this);

            Output.Clear();
        }
        #endregion

        #region Fields
        protected override void StartCalc()
        {
            Pamaf._Length = PamaLength;
            Pamaf._Smooth = PamaSmooth;
            Pamaf._PriceType = 1;
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            if (Bars.CurrentBar == 1)
            {
                Pama[1] = Pamaf.Pama1;
                Pama[2] = Pamaf.Pama2;
                Pama[3] = Pamaf.Pama3;
            }

            Pamaf.Call();

            PlotStrategyInfo();
        }
        #endregion

        #region Plots
        private void PlotStrategyInfo()
        {
            // pama plots
            StrategyInfo.SetPlotValue(1, Pama[1][0]);
            StrategyInfo.SetPlotValue(2, Pama[2][0]);
            StrategyInfo.SetPlotValue(3, Pama[3][0]);
        }
        #endregion
        #region Print
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