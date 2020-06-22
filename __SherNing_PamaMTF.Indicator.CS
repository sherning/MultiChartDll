//==============================================================================
// Name           : __SherNing_PamaMTF
// Description    : Price Action Moving Average
// Version        : v.1.0.0
// Date Created   : 29 - May - 2020
// Time Taken     : 
// Remarks        :
//==============================================================================
// Copyright      : 2020, Sher Ning Technologies           
// License        :      
//==============================================================================

/* ------------------------------- Version 1.0.0 -------------------------------
 * Cannot use class to build Pama_MTF, will have a problem when adjusting inputs.
 * When designing MC.Net code, best to stick to methods only.
 * Otherwise, use MC.NET function class.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class __SherNing_PamaMTF : IndicatorObject
    {
        [Input] public int Length { get; set; }
        [Input] public int Smooth { get; set; }
        [Input] public bool CalculateAll { get; set; }
        [Input] public int PlotPama5 { get; set; }
        [Input] public int PlotPama10 { get; set; }
        [Input] public int PlotPama15 { get; set; }
        [Input] public int PlotPama20 { get; set; }
        [Input] public int PlotPama30 { get; set; }
        [Input] public int PlotPama60 { get; set; }
        [Input] public int PlotPama120 { get; set; }
        [Input] public int PlotPama240 { get; set; }
        [Input] public int PlotPamaDay { get; set; }
        [Input] public int PlotPamaWeek { get; set; }
        [Input] public int PlotPamaMth { get; set; }
        public __SherNing_PamaMTF(object _ctx) : base(_ctx)
        {
            Length = 6;
            Smooth = 10;
            CalculateAll = true;

            PlotPama5    = 1;
            PlotPama10   = 1;
            PlotPama15   = 1;
            PlotPama20   = 1;
            PlotPama30   = 1;
            PlotPama60   = 1;
            PlotPama120  = 1;
            PlotPama240  = 1;
            PlotPamaDay  = 1;
            PlotPamaWeek = 0;
            PlotPamaMth  = 0;
        }

        private const int Timeframes = 11;
        private int[] PamaPlotSwitch;
        private List<IPlotObject> PamaPlots;
        private string[] PamaPlotNames;
        private PriceActionMovingAverageMTF PamaMtf;
        private List<VariableSeries<double>> PamaPrice;
        private StandardDeviationCalculator StdDevCal;

        protected override void Create()
        {
            PamaMtf = new PriceActionMovingAverageMTF(this);
            PamaPrice = new List<VariableSeries<double>>();
            StdDevCal = new StandardDeviationCalculator(this);

            PamaPlotNames = new string[Timeframes]
            {
                "5 Min","10 Min","15 Min","20 Min","30 Min",
                "60 Min","120 Min","240 Min",
                "Daily", "Weekly","Monthly"
            };

            PamaPlots = new List<IPlotObject>();

            for (int i = 0; i < Timeframes; i++)
            {
                PamaPrice.Add(new VariableSeries<double>(this));
                PamaPlots.Add(AddPlot(new PlotAttributes(PamaPlotNames[i])));
            }
        }

        protected override void StartCalc()
        {
            PamaPlotSwitch = new int[Timeframes];
            PamaPlotSwitch[0] = PlotPama5;
            PamaPlotSwitch[1] = PlotPama10;
            PamaPlotSwitch[2] = PlotPama15;
            PamaPlotSwitch[3] = PlotPama20;
            PamaPlotSwitch[4] = PlotPama30;
            PamaPlotSwitch[5] = PlotPama60;
            PamaPlotSwitch[6] = PlotPama120;
            PamaPlotSwitch[7] = PlotPama240;
            PamaPlotSwitch[8] = PlotPamaDay;
            PamaPlotSwitch[9] = PlotPamaWeek;
            PamaPlotSwitch[10] = PlotPamaMth;

            PamaMtf.Calc_5    = PlotPama5;
            PamaMtf.Calc_10   = PlotPama10;
            PamaMtf.Calc_15   = PlotPama15;
            PamaMtf.Calc_20   = PlotPama20;
            PamaMtf.Calc_30   = PlotPama30;
            PamaMtf.Calc_60   = PlotPama60;
            PamaMtf.Calc_120  = PlotPama120;
            PamaMtf.Calc_240  = PlotPama240;
            PamaMtf.Calc_Day  = PlotPamaDay;
            PamaMtf.Calc_Week = PlotPamaWeek;
            PamaMtf.Calc_Mth  = PlotPamaMth;

            PamaMtf._Length = Length;
            PamaMtf._Smooth = Smooth;
            PamaMtf._PriceType = 1;

            StdDevCal._Length = Length;
            StdDevCal._PopulationStdDev = true;
            StdDevCal._Price = PamaMtf.Pama_60;
        }

        protected override void CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                PamaMtf.Call();

                PamaPrice[0].Value = PamaMtf.Pama_5.Value;
                PamaPrice[1].Value = PamaMtf.Pama_10.Value;
                PamaPrice[2].Value = PamaMtf.Pama_15.Value;
                PamaPrice[3].Value = PamaMtf.Pama_20.Value;
                PamaPrice[4].Value = PamaMtf.Pama_30.Value;
                PamaPrice[5].Value = PamaMtf.Pama_60.Value;
                PamaPrice[6].Value = PamaMtf.Pama_120.Value;
                PamaPrice[7].Value = PamaMtf.Pama_240.Value;
                PamaPrice[8].Value = PamaMtf.Pama_Day.Value;
                PamaPrice[9].Value = PamaMtf.Pama_Week.Value;
                PamaPrice[10].Value = PamaMtf.Pama_Month.Value;

                for (int i = 0; i < Timeframes; i++)
                {
                    // if not on continue
                    if (PamaPlotSwitch[i] != 1) continue;

                    if (PamaPrice[i][1] > 0)
                        PamaPlots[i].Set(1, PamaPrice[i][1], Color.Yellow);
                }

                for (int i = 0; i < Timeframes; i++)
                {
                    Print("Timeframe: " + i);
                    Print("PamaPrice[0]: " + PamaPrice[i][0]);
                    Print("PamaPrice[1]: " + PamaPrice[i][1]);
                }

            }
            
        }

        protected override void StopCalc()
        {
            Output.Clear();
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