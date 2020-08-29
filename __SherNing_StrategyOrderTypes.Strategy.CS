using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_StrategyOrderTypes : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Strategy Order Types
        // Description  : Test exit and entry order types
        // Objectives   : Trade Execution Mastery (IOG.Enabled / Disabled)
        // Type         : Function
        // Version      : v6.3
        // Date         : 29 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Test Framework
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.0 - Design, goals and outline
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Object Properties & Constructor
        [Input] public int EntrySelection { get; set; }
        [Input] public int ExitSelection { get; set; }
        public __SherNing_StrategyOrderTypes(object _ctx) : base(_ctx)
        {
            EntrySelection = 1;
            ExitSelection = 1;
        }
        #endregion

        #region Program Main - CalcBar()
        protected override void CalcBar()
        {
        }
        #endregion

        #region Object Initialization - Create()
        protected override void Create()
        {
            switch (EntrySelection)
            {
                default:
                    break;
            }

            switch (ExitSelection)
            {
                default:
                    break;
            }
        }
        private void EntryOrdersSet1()
        {

        }
        private void ExitOrdersSet1()
        {

        }
        #endregion

        #region Object State - StartCalc()
        protected override void StartCalc()
        {
        }
        #endregion

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
        private void Print(string message, int chartbarNum)
        {
            int maxbars = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar == chartbarNum - maxbars)
                Print(message);
        }
        #endregion
    }
}