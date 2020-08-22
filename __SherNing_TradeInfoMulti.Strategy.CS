using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    #region Strategy - Price Pattern v32
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_TradeInfoMulti : SignalObject
    {
        //=========================================================================================
        // Project Name : Trade Info Multiple 
        // Description  : Test Framework for Order Execution
        // Type         : Strategy
        // Version      : v1.0
        // Date         : 20 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Test Framework for TradeInfoMulti class
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *--------------------------------------- Objectives --------------------------------------
         *  1.0.1 - Clean up code
         *  1.0.2 - Plan. Decide to create a new function or update tradeinfo class
         *  1.0.3 - Test - FromEntry() with one entry one exit
         *  1.0.4 - Test - Unique order number
         *  1.0.5 - Master Order Execution
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *  
         *---------------------------------------- Versions ---------------------------------------
        /*/

        // strategy inputs
        public __SherNing_TradeInfoMulti(object _ctx) : base(_ctx)
        {
            // do up entire strategy in strategy before converting to function.
        }

        // reference fields 
        VariableSeries<int> MarketPosition, LastMarketPosition;
        IOrderMarket Lemkt1, Lemkt2;
        IOrderPriced Lxstp1, Lxstp2;
        IOrderPriced Lxpft11, Lxpft12, Lxpft13, Lxpft21, Lxpft22, Lxpft23;

        VariableSeries<int> CurrentBars;

        TradeInfo LongTrade1, LongTrade2;

        protected override void Create()
        {
            LongTrade1 = new TradeInfo(this, EMarketPositionSide.Long, "Le_Trd1");
            LongTrade2 = new TradeInfo(this, EMarketPositionSide.Long, "Le_Trd2");

            // test fields
            Lemkt1 = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "Le1", EOrderAction.Buy));

            Lemkt2 = OrderCreator.MarketNextBar(new SOrderParameters(
               Contracts.UserSpecified, "Le2", EOrderAction.Buy));

            Lxstp1 = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "Lesl1", EOrderAction.Sell));

            Lxstp2 = OrderCreator.Stop(new SOrderParameters(
             Contracts.Default, "Lesl2", EOrderAction.Sell, OrderExit.FromEntry(Lemkt2)));

            Lxpft11 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "Lx11", EOrderAction.Sell));

            Lxpft12 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "Lx12", EOrderAction.Sell));

            Lxpft13 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "Lx13", EOrderAction.Sell, OrderExit.FromEntryTotal(Lemkt1)));

            Lxpft21 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "Lx21", EOrderAction.Sell, OrderExit.FromEntryTotal(Lemkt2)));

            Lxpft22 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "Lx22", EOrderAction.Sell, OrderExit.FromEntryTotal(Lemkt2)));

            Lxpft23 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "Lx23", EOrderAction.Sell, OrderExit.FromEntryTotal(Lemkt2)));

            CurrentBars = new VariableSeries<int>(this);
            MarketPosition = new VariableSeries<int>(this);
            LastMarketPosition = new VariableSeries<int>(this);

            // clear output window
            Output.Clear();
        }

        // value fields 
        double OnePip;
        const int EnoughData = 100;
        protected override void StartCalc()
        {
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
            LongTrade1.DisplayEntry = LongTrade1.DisplayStopLoss = LongTrade1.DisplayProfits = true;
            LongTrade2.DisplayEntry = LongTrade2.DisplayStopLoss = LongTrade2.DisplayProfits = true;
        }

        protected override void CalcBar()
        {
            // ensure enought data
            if (Bars.CurrentBar > EnoughData)
            {
                Test1();
            }
        }

        #region Send Entry
        private void SendEntry() { }
        #endregion

        #region Send Exits
        private void SendExits() { }
        #endregion

        #region Tests
        private void Test1()
        {
            // two entry each with own exit
            int currentTime = Bars.Time[0].Hour * 100 + Bars.Time[0].Minute;
            double close = Bars.Close[0];

            // entry 1
            if (currentTime == 2000
                && Bars.Status == EBarState.Close
                && LongTrade1.IsEntryOrderFilled == false)
            {
                LongTrade1.SetMarketOrder(close, 100000);
                LongTrade1.SetStopLoss(close - (10 * OnePip), true);
                LongTrade1.SetProfitTarget(1, 50000, 1);
                LongTrade1.SetProfitTarget(2, 50000, 2);
            }

            //if (currentTime == 2200
            //    && Bars.Status == EBarState.Close
            //    && LongTrade2.IsEntryOrderFilled == false)
            //{
            //    LongTrade2.SetMarketOrder(close, 100000);
            //    LongTrade2.SetStopLoss(close - (10 * OnePip), true);
            //    LongTrade2.SetProfitTarget(1, 50000, 1);
            //    LongTrade2.SetProfitTarget(2, 50000, 2);
            //}

            LongTrade1.Call();
            //LongTrade2.Call();

            if (LongTrade1.SendEntry) Lemkt1.Send(LongTrade1.Contracts);
            //if (LongTrade2.SendEntry) Lemkt2.Send(LongTrade2.Contracts);

            if (LongTrade1.SendStopLoss) Lxstp1.Send(LongTrade1.StopLossPrice);
            //if (LongTrade2.SendStopLoss) Lxstp2.Send(LongTrade2.StopLossPrice);

            if (LongTrade1.SendProfit1) Lxpft11.Send(LongTrade1.ProfitPrice1, LongTrade1.ProfitQty1);
            if (LongTrade1.SendProfit2) Lxpft12.Send(LongTrade1.ProfitPrice2, LongTrade1.ProfitQty2);

            //if (LongTrade2.SendProfit1) Lxpft21.Send(LongTrade2.ProfitPrice1, LongTrade2.ProfitQty1);
            //if (LongTrade2.SendProfit2) Lxpft22.Send(LongTrade2.ProfitPrice2, LongTrade2.ProfitQty2);
        }
        #endregion

        #region Strategy Conditions
        private void LongEntryConditions() { }
        private void ShortEntryConditions() { }
        #endregion

        #region Debug - Print out information
        private void DisplayPriceLevels(double price, Color color)
        {
            DateTime time = Bars.Time[0];

            // check if fields are reference types, otherwise pass by ref
            ChartPoint p = new ChartPoint(time, price);
            ITextObject txt = DrwText.Create(p, "-");
            txt.Color = color;
        }

        private void CurrentPositionInformation()
        {
            if (Bars.Status == EBarState.Close && MarketPosition[0] > 0)
            {
                if (CurrentPosition.OpenTrades[0] != null)
                {
                    Print("Position 1: " + CurrentPosition.OpenTrades[0].EntryOrder.BarNumber);
                }
            }

            if (Bars.Status == EBarState.Close && MarketPosition[0] >= 200000)
            {
                if (CurrentPosition.OpenTrades[1] != null)
                {
                    Print("Position 2: " + CurrentPosition.OpenTrades[1].EntryOrder.BarNumber);
                }
            }

            if (Bars.Status == EBarState.Close && MarketPosition[0] >= 300000)
            {
                if (CurrentPosition.OpenTrades[2] != null)
                {
                    Print("Position 3: " + CurrentPosition.OpenTrades[2].EntryOrder.BarNumber);
                }
            }
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
        #endregion
    }

    #endregion
}

namespace PowerLanguage.Function
{

}
