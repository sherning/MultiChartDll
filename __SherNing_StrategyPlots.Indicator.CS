using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class __SherNing_StrategyPlots : IndicatorObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Strategy Plots
        // Requirements : 1. GetPlotValues from strategy
        //                2. Plot the values from the strategy on the chart
        //
        // Type         : Indicator
        // Version      : v1.0
        // Date         : 03 - Sep - 2020
        // Time Taken   : 
        // Remarks      : Requirements, Design, Development, Optimization, Maintenance
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      :
        // Client       :
        // Contact      : 
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.0 - Requirements
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Properties
        [Input] public int Plot_1 { get; set; }
        [Input] public int Plot_2 { get; set; }
        [Input] public int Plot_3 { get; set; }
        [Input] public int Plot_4 { get; set; }
        public __SherNing_StrategyPlots(object _ctx) : base(_ctx) { }

        #endregion

        #region References
        const int NumOfPlots = 4;
        List<IPlotObject> PlotList;
        protected override void Create()
        {
            PlotList = new List<IPlotObject>();

            string[] plotNames = new string[]
            {
                "", "Plot 1", "Plot 2", "Plot 3", "Plot 4"
            };

            // avoid using [0]
            for (int i = 0; i <= NumOfPlots; i++)
            {
                PlotList.Add(
                    AddPlot(new PlotAttributes(
                        plotNames[i],
                        EPlotShapes.Line,
                        Color.Yellow, Color.Yellow,
                        3, EPlotStyle.Solid, true)));
            }
        }
        #endregion

        #region Fields
        int[] PlotNumbers;
        protected override void StartCalc()
        {
            PlotNumbers = new int[NumOfPlots + 1];
            PlotNumbers[0] = 0;
            PlotNumbers[1] = Plot_1;
            PlotNumbers[2] = Plot_2;
            PlotNumbers[3] = Plot_3;
            PlotNumbers[4] = Plot_4;
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            // indicator logic 
            for (int i = 1; i <= NumOfPlots; i++)
            {
                if (StrategyInfo.GetPlotValue(i) != 0 && PlotNumbers[i] != 0)
                    PlotList[i].Set(StrategyInfo.GetPlotValue(PlotNumbers[i]));
            }
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
        private void Print(string message, int chartbarNum)
        {
            int maxbars = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar == chartbarNum - maxbars)
                Print(message);
        }
        #endregion
    }
}