using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    public class __SherNing_PriceActionMomentum : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Price Action Momentum
        // Requirements : 1. Scale-in 3 Entries (NextBar Mkt Entries)
        //
        // Type         : Strategy (IOG.Disabled)
        // Version      : v1.0
        // Date         : 06 - Sep - 2020
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
         *  
         *  1.0.1 - Requirements
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Inputs
        public __SherNing_PriceActionMomentum(object _ctx) : base(_ctx) { }

        #endregion

        #region References
        protected override void Create()
        {
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
        }
        #endregion

        #region Algorithm

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
        #endregion
    }
}