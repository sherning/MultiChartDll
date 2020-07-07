using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    //==========================================================================================
    // Name           : Price Action Multitime Frame Standard Deviation
    // Description    : Returns the Std Dev on a separate subchart
    // Version        : v1.0
    // Date Created   : 04 - Jul - 2020
    // Time Taken     : 
    // Remarks        :
    // Reference      : SherNingPamaMTF_Funnel
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  - This indicator is retrospective. Not ideal.
     *  - The lag is too great.
     *  ----------------------------------- Version History -----------------------------------
     */
    [SameAsSymbol(true)]
    public class __SherNing_PamaMTF_Funnel : IndicatorObject // v10
    {
        // inputs
        [Input] public int Length { get; set; }
        [Input] public int Smooth { get; set; }
        [Input] public bool DisplayTF1 { get; set; }
        [Input] public bool DisplayTF2 { get; set; }
        [Input] public bool DisplayTF3 { get; set; }

        // fields
        private List<IPlotObject> TrendPlots;
        private string[] PlotNames;
        private EPlotStyle[] PlotStyles;
        private int[] PlotWidths;
        private Color[] PlotColors;
        public __SherNing_PamaMTF_Funnel(object _ctx) : base(_ctx)
        {
            Length = 6;
            Smooth = 10;

            DisplayTF1 = false;
            DisplayTF2 = false;
            DisplayTF3 = true;
        }

        SherNing_PamaMTF_Funnel Funnel;
        protected override void Create()
        {
            // main function
            Funnel = new SherNing_PamaMTF_Funnel(this);

            // List Of Plots
            TrendPlots = new List<IPlotObject>();
            PlotNames = new string[6]
            {
                "TF1_Upper", "TF1_Lower",
                "TF2_Upper", "TF2_Lower",
                "TF3_Upper", "TF3_Lower"
            };

            PlotStyles = new EPlotStyle[6]
            {
                // TF 1
                EPlotStyle.Solid, EPlotStyle.Solid,

                // TF 2
                EPlotStyle.Dash, EPlotStyle.Dash,

                // TF 3
                EPlotStyle.Solid, EPlotStyle.Solid,
            };

            PlotWidths = new int[6]
            {
                // TF 1
                1,1,

                // TF 2
                0,0,

                // TF 3
                3,3
            };

            PlotColors = new Color[6]
            {
                // TF 1
                Color.Yellow, Color.Yellow,
                
                // TF 2
                Color.Cyan, Color.Cyan,

                // TF 3
                Color.Cyan, Color.Magenta,
            };

            // 3 time frames, upper and lower
            for (int i = 0; i < 6; i++)
            {
                TrendPlots.Add(AddPlot(new PlotAttributes(
                        PlotNames[i],
                        EPlotShapes.Line,
                        PlotColors[i],
                        Color.Black,
                        PlotWidths[i],
                        PlotStyles[i],
                        true)));
            }

        }
        protected override void StartCalc()
        {
            // assign inputs 
            Funnel._Length = Length;
            Funnel._Smooth = 10;
        }
        protected override void CalcBar()
        {
            // indicator logic 
            Funnel.Call();

            if (DisplayTF1)
            {
                // new bottom formed. plot previous trend low
                if (Funnel.TrendState_TF1[0] == 1 && Funnel.TrendState_TF1[1] == -1)
                {
                    // arrows
                    ChartPoint point = new ChartPoint(Funnel.TrendLowBarNum_TF1[0], Funnel.TrendLow_TF1[0]);
                    var arrow = DrwArrow.Create(point, false);
                    arrow.Color = Color.Cyan;

                    // plot
                    int barsBack = Bars.CurrentBar - Funnel.TrendLowBarNum_TF1[0];
                    TrendPlots[0].Set(barsBack, Funnel.TrendLow_TF1[0]);
                }

                // new peak formed. plot previous trend high
                if (Funnel.TrendState_TF1[0] == -1 && Funnel.TrendState_TF1[1] == 1)
                {
                    // arrows
                    ChartPoint point = new ChartPoint(Funnel.TrendHighBarNum_TF1[0], Funnel.TrendHigh_TF1[0]);
                    var arrow = DrwArrow.Create(point, true);
                    arrow.Color = Color.Magenta;

                    // set plot 
                    int barsBack = Bars.CurrentBar - Funnel.TrendHighBarNum_TF1[0];
                    TrendPlots[1].Set(barsBack, Funnel.TrendHigh_TF1[0]);
                }
            }

            if (DisplayTF2)
            {
                // new bottom formed. plot previous trend low
                if (Funnel.TrendState_TF2[0] == 1 && Funnel.TrendState_TF2[1] == -1)
                {
                    // arrows
                    ChartPoint point = new ChartPoint(Funnel.TrendLowBarNum_TF2[0], Funnel.TrendLow_TF2[0]);
                    var arrow = DrwArrow.Create(point, false);
                    arrow.Color = Color.Cyan;

                    // plot
                    int barsBack = Bars.CurrentBar - Funnel.TrendLowBarNum_TF2[0];
                    TrendPlots[2].Set(barsBack, Funnel.TrendLow_TF2[0]);
                }

                // new peak formed. plot previous trend high
                if (Funnel.TrendState_TF2[0] == -1 && Funnel.TrendState_TF2[1] == 1)
                {
                    // arrows
                    ChartPoint point = new ChartPoint(Funnel.TrendHighBarNum_TF2[0], Funnel.TrendHigh_TF2[0]);
                    var arrow = DrwArrow.Create(point, true);
                    arrow.Color = Color.Magenta;

                    // set plot 
                    int barsBack = Bars.CurrentBar - Funnel.TrendHighBarNum_TF2[0];
                    TrendPlots[3].Set(barsBack, Funnel.TrendHigh_TF2[0]);
                }
            }

            if (DisplayTF3)
            {
                // new bottom formed. plot previous trend low
                if (Funnel.TrendState_TF3[0] == 1 && Funnel.TrendState_TF3[1] == -1)
                {
                    // arrows
                    ChartPoint point = new ChartPoint(Funnel.TrendLowBarNum_TF3[0], Funnel.TrendLow_TF3[0]);
                    var arrow = DrwArrow.Create(point, false);
                    arrow.Color = Color.Cyan;

                    // plot
                    int barsBack = Bars.CurrentBar - Funnel.TrendLowBarNum_TF3[0];
                    TrendPlots[4].Set(barsBack, Funnel.TrendLow_TF3[0]);
                }

                // new peak formed. plot previous trend high
                if (Funnel.TrendState_TF3[0] == -1 && Funnel.TrendState_TF3[1] == 1)
                {
                    // arrows
                    ChartPoint point = new ChartPoint(Funnel.TrendHighBarNum_TF3[0], Funnel.TrendHigh_TF3[0]);
                    var arrow = DrwArrow.Create(point, true);
                    arrow.Color = Color.Magenta;

                    // set plot 
                    int barsBack = Bars.CurrentBar - Funnel.TrendHighBarNum_TF3[0];
                    TrendPlots[5].Set(barsBack, Funnel.TrendHigh_TF3[0]);
                }
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
