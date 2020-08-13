using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_StopLimitStrategyTest : SignalObject
    {
        //=========================================================================================
        // Project Name : Stop Limit Strategy Test
        // Description  : Test Stop Limit Orders
        // Type         : Strategy
        // Version      : v1.0
        // Date         : 13 - Aug - 2020
        // Time Taken   : 
        // Remarks      : 
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *--------------------------------------- Objectives --------------------------------------
         * 1.0.1 - Test StopLimit as an Entry Order
         * 1.0.2 - Test StopLimit as an Exit Order
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *---------------------------------------- Versions ---------------------------------------
        /*/
        public __SherNing_StopLimitStrategyTest(object _ctx) : base(_ctx) 
        { 
        
        }

        IOrderStopLimit LeStpLmt, LxStpLmt;
        IOrderMarket LeMkt;

        protected override void Create()
        {
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "Le_Mkt", EOrderAction.Buy));

            LeStpLmt = OrderCreator.StopLimit(new SOrderParameters(
                Contracts.UserSpecified, "Le_StpLmt", EOrderAction.Buy));

            LxStpLmt = OrderCreator.StopLimit(new SOrderParameters(
                Contracts.UserSpecified, "Lx_StpLmt", EOrderAction.Sell));
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