
//==============================================================================
// Name:          _SN_I_GetStrategyPlots_v100
// Description:   Use this indicator to plot strategy values
// Version:       v.1.0.0
// Date Created:  20 - Nov - 2019
//==============================================================================
// Copyright:     2019, Sher Ning Technologies
//             
// License:     
//==============================================================================

using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class _SN_I_GetStrategyPlots_v100 : IndicatorObject
    {
        [Input] public int GetPlotNumber_1 { get; set; }
        [Input] public int GetPlotNumber_2 { get; set; }
        [Input] public int GetPlotNumber_3 { get; set; }
        [Input] public int GetPlotNumber_4 { get; set; }

        public _SN_I_GetStrategyPlots_v100 (object _ctx) : base(_ctx)
        {
            GetPlotNumber_1 = 81;
            GetPlotNumber_2 = 82;
            GetPlotNumber_3 = 83;
            GetPlotNumber_4 = 84;
        }

        // Create IPlotObject using For Loop
        private List<IPlotObject> PAL_Line;
        private const int NumOfPlots = 4;
        private string[] plotNames = new string[] { "Line 1", "Line 2", "Line 3", "Line 4" };
        private Color[] plotColors = new Color[] { Color.Lime, Color.Cyan, Color.PaleVioletRed, Color.Magenta };
        private int[] plotNumbers = new int[100];

        protected override void Create()
        {
            // create ONLY variable objects, function objects, plot objects etc.
            PAL_Line = new List<IPlotObject>();

            for (int i = 0; i < NumOfPlots; i++)
            {
                PAL_Line.Add(AddPlot(new PlotAttributes(plotNames[i], EPlotShapes.Line, plotColors[i], Color.Transparent, 1, EPlotStyle.Solid, false)));
            }
        }
        protected override void StartCalc()
        {
            // assign inputs 
            plotNumbers[0] = GetPlotNumber_1;
            plotNumbers[1] = GetPlotNumber_2;
            plotNumbers[2] = GetPlotNumber_3;
            plotNumbers[3] = GetPlotNumber_4;
        }
        protected override void CalcBar()
        {
            // indicator logic 
            for (int i = 0; i < 2; i++)
            {
                PAL_Line[i].Set(StrategyInfo.GetPlotValue(plotNumbers[i]));
            }
        }
    }
}