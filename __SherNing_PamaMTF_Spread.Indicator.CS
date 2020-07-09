using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class __SherNing_PamaMTF_Spread : IndicatorObject
    {
        //==========================================================================================
        // Name           : Price Action Moving Average Spread
        // Description    : Returns the Std Dev of two different pama spreads
        // Version        : v1.0
        // Date Created   : 09 - Jul - 2020
        // Time Taken     : 
        // Remarks        :
        // Reference      : 
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  v10
         *  - 
         *  ----------------------------------- Version History -----------------------------------
         */
        public __SherNing_PamaMTF_Spread(object _ctx) : base(_ctx) { }
        protected override void Create()
        {
        }
        protected override void StartCalc()
        {
        }
        protected override void CalcBar()
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