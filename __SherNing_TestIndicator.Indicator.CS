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
    public class __SherNing_TestIndicator : IndicatorObject
    {
        [Input] public int Length { get; set; }
        public __SherNing_TestIndicator(object _ctx) : base(_ctx)
        {
        }

        protected override void Create()
        {
        }

        protected override void StartCalc()
        {

        }

        protected override void CalcBar()
        {
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

