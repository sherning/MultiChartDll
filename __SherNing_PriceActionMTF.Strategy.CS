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
            Counter = 0;
        }
        private int Counter;
        protected override void CalcBar()
        {
            if (Bars.CurrentBar % 60 == 0 && Counter <= 5)
            {
                BuyOrder.Send(100000);
                Counter++;
            }

            if (Bars.CurrentBar < 1000)
            {
                if (Positions[0] != null)
                {
                    Print("Position[0]: " + Positions[0].OpenTrades.Count);
                    Print("Position[0]: " + Positions[0].ClosedTrades.Count);
                }

                if (TotalTrades > 1 && Positions[1] != null)
                {
                    Print("Position[1]: " + Positions[1].OpenTrades.Count);
                    Print("Position[1]: " + Positions[1].ClosedTrades.Count);
                }
            }
        
            //if (TotalTrades > 2 && Positions[2] != null)
            //{
            //    Print("Position[2]: " + Positions[2].OpenTrades);
            //    Print("Position[2]: " + Positions[2].ClosedTrades);
            //}

            //if (TotalTrades > 3 && Positions[3] != null)
            //{
            //    Print("Position[3]: " + Positions[3].OpenTrades);
            //    Print("Position[3]: " + Positions[3].ClosedTrades);
            //}

            if (Bars.LastBarInSession)
            {
                SellOrder.Send();
                Counter = 0;
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