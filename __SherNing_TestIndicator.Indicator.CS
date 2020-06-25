//==============================================================================
// Name           : __SherNing_TestIndicator
// Description    : Test Notes for Various Multichart Application
// Version        : v.1.0.0
// Date Created   : 22 - May - 2020
// Time Taken     : 
// Remarks        :
//==============================================================================
// Copyright      : 2020, Sher Ning Technologies           
// License        :      
//==============================================================================

/* ------------------------------- Version 1.0.0 -------------------------------
 * In MC.Net, we have to design algorithm based on the fact that CalcBar() is 
 * calculated once every tick or bar close.
 * 
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using PowerLanguage.Function;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true), MouseEvents(true), UpdateOnEveryTick(true), SkipIdenticalTicks(true)]
    public class __SherNing_TestIndicator : IndicatorObject
    {
        [Input] public int Length { get; set; }
        public __SherNing_TestIndicator(object _ctx) : base(_ctx)
        {
            Length = 6;
        }
        private IPlotObject Plot10, Plot11, Plot12;
        private IPlotObjectStr StatusLine;
        private DeveloperTools Tools;
        protected override void Create()
        {
            Plot10 = AddPlot(new PlotAttributes("v10"));
            Plot11 = AddPlot(new PlotAttributes("v11"));
            Plot12 = AddPlot(new PlotAttributes("v12"));

            StatusLine = AddPlot(new StringPlotAttributes("Clock", Color.LimeGreen, Color.White));
            Tools = new DeveloperTools(this);
        }

        protected override void StartCalc()
        {
        }

        protected override void CalcBar()
        {
            if (Bars.LastBarOnChart != true) return;

            DateTime prevTime = Bars.StatusLine.Time;
            DateTime easternTime = Tools.GetEasternTime();
            TimeSpan timeDiff = easternTime - prevTime;

            string statusTxt = string.Format("{0} ({1}s)",
                prevTime.ToString("HH:mm:ss"),
                Math.Round(timeDiff.TotalSeconds,
                1));


            if (timeDiff > TimeSpan.FromSeconds(30))
            {
                StatusLine.Set(statusTxt, Color.Red);
            }
            else if (timeDiff > TimeSpan.FromSeconds(15))
            {
                StatusLine.Set(statusTxt, Color.Orange);
            }
            else
            {
                StatusLine.Set(statusTxt, Color.LimeGreen);
            }

            ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
        }

        protected override void OnRecalcLastBarAfterEvent()
        {
            DateTime easternTime = Tools.GetEasternTime();

            if (easternTime.DayOfWeek != DayOfWeek.Saturday && easternTime.DayOfWeek != DayOfWeek.Sunday)
            {
                CalcBar();
            }
        }

        protected override void StopCalc()
        {

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