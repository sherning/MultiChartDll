using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Collections.Generic;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    //==========================================================================================
    // Name           : __SherNing_PriceActionMTF
    // Description    : Price action automated strategy
    // Version        : v1.0
    // Date Created   : 24 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  ----------------------------------- Version History -----------------------------------
     

     *  ---------------------------------------- Notes ----------------------------------------
     *  - 3 timeframes
     *  - Highest Timeframe determines market direction
     *  
     *  ---------------------------------------- Notes ----------------------------------------
     */
    public class __SherNing_PriceActionMTF : SignalObject //v10
    {

        public __SherNing_PriceActionMTF(object _ctx) : base(_ctx) 
        {
        }

        private IOrderMarket BuyOrder;
        private IOrderMarket SellOrder;
        protected override void Create()
        {
            BuyOrder = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
               EOrderAction.Buy));
            SellOrder = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default,
                EOrderAction.Sell,OrderExit.FromAll));
        }

        protected override void StartCalc()
        {
        }
        protected override void CalcBar()
        {
            if (Bars.CurrentBar == 5 || Bars.CurrentBar == 15 || Bars.CurrentBar == 25 || Bars.CurrentBar == 35 )
            {
                BuyOrder.Send(100000);
            }

            if (Bars.LastBarOnChart)
            {
                SellOrder.Send();
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