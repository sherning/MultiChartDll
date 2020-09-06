using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    public class __SherNing_PriceActionMomentum : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Price Action Momentum
        // Requirements : 1. Scale-in 3 Entries (NextBar Mkt Entries)
        //
        // Type         : Strategy (IOG.Disabled)
        // Version      : v1.0
        // Date         : 06 - Sep - 2020
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
         *        - Import and check key functions
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Inputs
        [Input] public string PamaSettings { get; set; }
        [Input] public int PamaLength { get; set; }
        [Input] public int PamaSmooth { get; set; }
        [Input] public string StrategySettings { get; set; }
        [Input] public int ChannelLength { get; set; }

        public __SherNing_PriceActionMomentum(object _ctx) : base(_ctx) 
        {
            // default settings
            PamaSettings = "Pama Setting";
            PamaLength = 6;
            PamaSmooth = 10;

            StrategySettings = "Strategy Settings";
            ChannelLength = 20;
        }

        #endregion

        #region References
        PriceActionMovingAverageFunction PamaFunction;
        VariableSeries<double>[] Pama;
        protected override void Create()
        {
            PamaFunction = new PriceActionMovingAverageFunction(this);

            Pama = new VariableSeries<double>[4];
            for (int i = 0; i < Pama.Length; i++)
                Pama[i] = new VariableSeries<double>(this);
        }
        #endregion

        #region Fields
        protected override void StartCalc()
        {
            PamaFunction._Length = PamaLength;
            PamaFunction._Smooth = PamaSmooth;
            PamaFunction._PriceType = 1;
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            if (Bars.CurrentBar == 1)
            {
                Pama[1] = PamaFunction.Pama1;
                Pama[2] = PamaFunction.Pama2;
                Pama[3] = PamaFunction.Pama3;
            }

            PamaFunction.Call();

            PlotStrategyInfo();
        }
        #endregion

        #region Algorithm
        private double HighestHigh(int length)
        {
            double ret = 0;
            for (int i = 1; i < length; i++)
                ret = Math.Max(ret, Bars.High[i]);

            return ret;
        }

        private double LowestLow(int length)
        {
            double ret = double.MaxValue;
            for (int i = 1; i < length; i++)
                ret = Math.Min(ret, Bars.Low[i]);

            return ret;
        }
        #endregion

        #region Plots
        private void PlotStrategyInfo()
        {
            // pama plots
            //StrategyInfo.SetPlotValue(1, Pama[1][0]);
            //StrategyInfo.SetPlotValue(2, Pama[2][0]);
            //StrategyInfo.SetPlotValue(3, Pama[3][0]);

            // channel plots
            StrategyInfo.SetPlotValue(11, HighestHigh(ChannelLength));
            StrategyInfo.SetPlotValue(12, LowestLow(ChannelLength));
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