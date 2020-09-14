using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class __SherNing_DochianChannels : IndicatorObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Dochian Channels
        // Requirements : 
        // Type         : Indicator / Function
        // Version      : v1.0
        // Date         : 14 - Sep - 2020
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
         *  1.0.1 - Develop
         *  Multiple donchian channels with different length settings
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
        [Input] public string IndicatorSettings { get; set; }
        [Input] public int Length1 { get; set; }
        [Input] public bool DisplayPlot1 { get; set; }
        [Input] public int Length2 { get; set; }
        [Input] public bool DisplayPlot2 { get; set; }
        [Input] public int Length3 { get; set; }
        [Input] public bool DisplayPlot3 { get; set; }

        public __SherNing_DochianChannels(object _ctx) : base(_ctx)
        {
            IndicatorSettings = "Indicator Settings";

            Length1 = 10;
            Length2 = 20;
            Length3 = 30;

            DisplayPlot1 = true;
            DisplayPlot2 = true;
            DisplayPlot3 = true;
        }

        #endregion

        #region References
        IPlotObject[] ChartPlots;
        protected override void Create()
        {
            ChartPlots = new IPlotObject[7];

            string[] names = new string[]
            {
                "",
                "Upper 1", "Lower 1",
                "Upper 2", "Lower 2",
                "Upper 3", "Lower 3"
            };

            Color[] colors = new Color[]
            {
                Color.White,
                Color.FromArgb(128, 128, 255), Color.DeepPink,
                Color.FromArgb(128, 128, 255), Color.DeepPink,
                Color.FromArgb(128, 128, 255), Color.DeepPink,
            };

            // [0] not in use
            for (int i = 1; i <= 6; i++)
            {
                ChartPlots[i] = AddPlot(new PlotAttributes(
                   names[i], EPlotShapes.Line,
                   colors[i], Color.Black,
                   3, EPlotStyle.Solid, true));
            }

        }
        #endregion

        #region Fields
        double[] ChannelPrice;
        protected override void StartCalc()
        {
            ChannelPrice = new double[7];
        }
        #endregion

        protected override void CalcBar()
        {
            if (DisplayPlot1)
            {
                // calculate when display is true
                ChannelPrice[1] = HighestHigh(Length1);
                ChannelPrice[2] = LowestLow(Length1);

                // plot
                ChartPlots[1].Set(ChannelPrice[1]);
                ChartPlots[2].Set(ChannelPrice[2]);
            }

            if (DisplayPlot2)
            {
                ChannelPrice[3] = HighestHigh(Length2);
                ChannelPrice[4] = LowestLow(Length2);

                ChartPlots[3].Set(ChannelPrice[3]);
                ChartPlots[4].Set(ChannelPrice[4]);
            }

            if (DisplayPlot3)
            {
                ChannelPrice[5] = HighestHigh(Length3);
                ChannelPrice[6] = LowestLow(Length3);

                ChartPlots[5].Set(ChannelPrice[5]);
                ChartPlots[6].Set(ChannelPrice[6]);
            }
        }

        #region Methods
        private double HighestHigh(int length)
        {
            // prevent zero division exception
            if (length < 1) length = 1;

            double ret = 0;

            // exculde current bar high
            for (int i = 1; i < length; i++)
                ret = Math.Max(ret, Bars.High[i]);

            return ret;
        }

        private double LowestLow(int length)
        {
            // prevent zero division exception
            if (length < 1) length = 1;

            double ret = double.MaxValue;

            // exculde current bar low
            for (int i = 1; i < length; i++)
                ret = Math.Min(ret, Bars.Low[i]);

            return ret;
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