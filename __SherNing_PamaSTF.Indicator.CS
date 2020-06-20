//==============================================================================
// Name           : __SherNing_PamaSTF
// Description    : Price Action Moving Average Single Timeframe
// Version        : v.1.0.0
// Date Created   : 27 - May - 2020
// Time Taken     : 
// Remarks        :
//==============================================================================
// Copyright      : 2020, Sher Ning Technologies           
// License        :      
//==============================================================================

/* ------------------------------- Version 1.0.0 -------------------------------
 *
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
    public class __SherNing_PamaSTF : IndicatorObject
    {
        [Input] public int LengthInput { get; set; }
        [Input] public int SmoothInput { get; set; }
        [Input] public int PriceTypeInput { get; set; }
        public __SherNing_PamaSTF(object _ctx) : base(_ctx)
        {
            LengthInput = 6;
            SmoothInput = 10;
            PriceTypeInput = 1;
        }

        private IPlotObject PamaPlot;
        private PriceActionMovingAverage Pama;
        private VariableSeries<double> PamaValue;
        protected override void Create()
        {
            PamaPlot = AddPlot(new PlotAttributes("Pama Plot", EPlotShapes.Line, Color.Yellow));
            Pama = new PriceActionMovingAverage(this);
            PamaValue = new VariableSeries<double>(this);
        }

        protected override void StartCalc()
        {
            Pama._Length = LengthInput;
            Pama._Smooth = SmoothInput;
            Pama._Price = Bars.Close;
        }
        protected override void CalcBar()
        {
            PamaValue.Value = Pama.Value;
            PamaPlot.Set(1, PamaValue[1]);
        }

        #region Print Functions
        private bool DebugMode = true;

        /// <summary>
        /// 0: Print, 1: Print on last bar in session, 2: Print on last bar on chart.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="printOptions"></param>
        private void Print(string message, int printOptions)
        {
            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            switch (printOptions)
            {
                default:
                case 0: // Print
                    Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                                 Name,
                                 stackTrace.GetFrame(1).GetMethod().Name,
                                 Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                                 message);
                    break;

                case 1: // Print only on the last bar in session
                    if (Bars.LastBarInSession == false) return;
                    Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                                 Name,
                                 stackTrace.GetFrame(1).GetMethod().Name,
                                 Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                                 message);
                    break;

                case 2: // Print only on the last bar on chart
                    if (Bars.LastBarOnChart == false) return;
                    if (Bars.LastBarInSession == false) return;
                    Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                                 Name,
                                 stackTrace.GetFrame(1).GetMethod().Name,
                                 Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                                 message);
                    break;
            }
        }

        /// <summary>
        /// In Order to Print, Set DebugMode = true.
        /// </summary>
        /// <param name="message"></param>
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