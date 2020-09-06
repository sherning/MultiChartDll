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
        //                3. Input settings to change width and color
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
        [Input] public string Plot_1_Settings { get; set; }
        [Input] public int Plot_1_Num { get; set; }
        [Input] public int Plot_1_Width { get; set; }
        [Input] public Color Plot_1_Color { get; set; }

        [Input] public string Plot_2_Settings { get; set; }
        [Input] public int Plot_2_Num { get; set; }
        [Input] public int Plot_2_Width { get; set; }
        [Input] public Color Plot_2_Color { get; set; }

        [Input] public string Plot_3_Settings { get; set; }
        [Input] public int Plot_3_Num { get; set; }
        [Input] public int Plot_3_Width { get; set; }
        [Input] public Color Plot_3_Color { get; set; }

        [Input] public string Plot_4_Settings { get; set; }
        [Input] public int Plot_4_Num { get; set; }
        [Input] public int Plot_4_Width { get; set; }
        [Input] public Color Plot_4_Color { get; set; }
        public __SherNing_StrategyPlots(object _ctx) : base(_ctx)
        {
            Plot_1_Settings = "Plot 1 Settings";
            Plot_2_Settings = "Plot 2 Settings";
            Plot_3_Settings = "Plot 3 Settings";
            Plot_4_Settings = "Plot 4 Settings";

            // default plot numbers
            Plot_1_Num = 1;
            Plot_2_Num = 2;
            Plot_3_Num = 3;
            Plot_4_Num = 4;

            // set default colors and width
            Plot_1_Color = Plot_3_Color = Color.DeepPink; // 255,0,128
            Plot_2_Color = Plot_4_Color = Color.FromArgb(128, 128, 255);
            Plot_1_Width = Plot_2_Width = Plot_3_Width = Plot_4_Width = 1;
        }

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
                        // default color 
                        Color.White, Color.White,
                        // default width
                        3, EPlotStyle.Solid, true)));
            }
        }
        #endregion

        #region Fields
        protected override void StartCalc()
        {
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            // indicator logic 

            // plot 1
            if (StrategyInfo.GetPlotValue(Plot_1_Num) != 0)
                PlotList[1].Set(StrategyInfo.GetPlotValue(Plot_1_Num), Plot_1_Color, Plot_1_Width);

            // plot 2
            if (StrategyInfo.GetPlotValue(Plot_2_Num) != 0)
                PlotList[2].Set(StrategyInfo.GetPlotValue(Plot_2_Num), Plot_2_Color, Plot_2_Width);

            // plot 3
            if (StrategyInfo.GetPlotValue(Plot_3_Num) != 0)
                PlotList[3].Set(StrategyInfo.GetPlotValue(Plot_3_Num), Plot_3_Color, Plot_3_Width);

            // plot 4
            if (StrategyInfo.GetPlotValue(Plot_4_Num) != 0)
                PlotList[4].Set(StrategyInfo.GetPlotValue(Plot_4_Num), Plot_4_Color, Plot_4_Width);
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