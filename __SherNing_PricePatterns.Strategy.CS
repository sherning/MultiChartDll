using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    #region Strategy - Price Pattern v31
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_PricePatterns : SignalObject
    {
        //=========================================================================================
        // Project Name : Price Patterns
        // Description  : Test Framework for Order Execution
        // Type         : Strategy
        // Version      : v3.1
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
         *  3.1.1 - Test Limit Entries with Exits - Successful
         *  3.1.2 - Test Market Entries with Exits - Succssful
         *  3.1.3 - Test Stop Entries with Exits - Successful
         *  3.1.4 - Test Stop Limit Entries using 2 class objects
         *  3.1.5 - Test Stop Limit order with additional 2 profit exit orders
         *  3.1.6 - Test Stop Limit Short order
         *  3.1.7 - Test Stop Stop Entries
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *---------------------------------------- Versions ---------------------------------------
        /*/

        // strategy inputs
        [Input] public bool ShowPriceLevels { get; set; }
        public __SherNing_PricePatterns(object _ctx) : base(_ctx)
        {
            ShowPriceLevels = true;
        }

        // reference fields 
        PricePatternStrategy PricePattern;
        VariableSeries<double> Pama1, Pama2, Pama3;
        VariableSeries<int> MktPosition;
        VariableSeries<EMarketPositionSide> MktDirection;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LeLmt, SeLmt;
        IOrderPriced LxStp, SxStp;
        IOrderPriced LeStp, SeStp;
        IOrderStopLimit LxStpLmt, SxStpLmt;
        IOrderPriced LePft1, LePft2, LePft3;
        IOrderPriced SePft1, SePft2, SePft3;

        TradeInfo LeEntry1, LeEntry2, SeEntry;
        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);

            // Trade Info Class
            LeEntry1 = new TradeInfo(this, EMarketPositionSide.Long, "LE_1");
            LeEntry2 = new TradeInfo(this, EMarketPositionSide.Long, "LE_2");
            SeEntry = new TradeInfo(this, EMarketPositionSide.Short, "SE_1");

            CurrentBars = new VariableSeries<int>(this);

            // market entry
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE_Mkt", EOrderAction.Buy));

            SeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "SE_Mkt", EOrderAction.SellShort));

            // limit entry 
            LeLmt = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LE_Lmt", EOrderAction.Buy));

            SeLmt = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "SE_Lmt", EOrderAction.SellShort));

            // stop entry
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LE_Stp", EOrderAction.Buy));

            SeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "SE_Stp", EOrderAction.SellShort));

            // stop limit entry
            LxStpLmt = OrderCreator.StopLimit(new SOrderParameters(
                Contracts.UserSpecified, "LE_StpLmt", EOrderAction.Sell));

            SxStpLmt = OrderCreator.StopLimit(new SOrderParameters(
                Contracts.UserSpecified, "SE_StpLmt", EOrderAction.BuyToCover));


            // stop loss 
            LxStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "LX_Stp", EOrderAction.Sell));

            SxStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "SX_Stp", EOrderAction.BuyToCover));

            // profit exit
            LePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LX_P1", EOrderAction.Sell));

            LePft2 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P2", EOrderAction.Sell));

            LePft3 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P3", EOrderAction.Sell));

            // profit exit
            SePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "SX_P1", EOrderAction.BuyToCover));

            SePft2 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "SX_P2", EOrderAction.BuyToCover));

            SePft3 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "SX_P3", EOrderAction.BuyToCover));

            MktPosition = new VariableSeries<int>(this);
            MktDirection = new VariableSeries<EMarketPositionSide>(this);

            // clear output window
            Output.Clear();
        }

        // value fields 
        double OnePip;
        const int EnoughData = 1200;
        protected override void StartCalc()
        {
            PricePattern._Length = 6;
            PricePattern._Smooth = 10;
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            if (ShowPriceLevels)
            {
                // display price lines
                LeEntry1.DisplayEntry = LeEntry1.DisplayProfits = LeEntry1.DisplayStopLoss = true;
                LeEntry2.DisplayEntry = LeEntry2.DisplayProfits = LeEntry2.DisplayStopLoss = true;
                SeEntry.DisplayEntry = SeEntry.DisplayStopLoss = SeEntry.DisplayProfits = true;
            }
            else
            {
                LeEntry1.DisplayEntry = LeEntry1.DisplayProfits = LeEntry1.DisplayStopLoss = false;
                LeEntry2.DisplayEntry = LeEntry2.DisplayProfits = LeEntry2.DisplayStopLoss = false;
                SeEntry.DisplayEntry = SeEntry.DisplayStopLoss = SeEntry.DisplayProfits = false;
            }
        }
        protected override void CalcBar()
        {
            // call strategy function on closing tick
            if (Bars.Status == EBarState.Close) PricePattern.Call();

            // keep track of market position
            MktPosition.Value = StrategyInfo.MarketPosition;
            MktDirection.Value = CurrentPosition.Side;

            if (Bars.CurrentBar == 1)
            {
                // assign same reference location as function
                Pama1 = PricePattern.Pama1;
                Pama2 = PricePattern.Pama2;
                Pama3 = PricePattern.Pama3;
            }

            // ensure enought data
            if (Bars.CurrentBar > EnoughData)
            {
                //SendStpEntry();
                //SendMktEntry();
                SendStpLmtEntry();
                //SendLmtEntry();
                SendExitOrders();
            }

            //Debug();
        }

        #region Send Order Command
        DateTime CurrentTime;
        private void Debug()
        {
            // debug market position and market direction
            int barNum = 5342;
            // to debug specific bar. use chart number - maxbarsback(50)

            int maxBarsBack = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar >= (barNum - maxBarsBack) && Bars.CurrentBar < (barNum - maxBarsBack + 10))
            {
                DateTime time = Bars.TimeValue;

                if (time != CurrentTime)
                {
                    CurrentTime = time;
                    Output.WriteLine("");
                }

                //Print("Bar Number: " + Bars.CurrentBar);
                //Print("Entry Price: " + LeEntry.EntryPrice);
                //Print("Stop Loss Price: " + LeEntry.StopLossPrice);


                if (Bars.Status == EBarState.Open)
                {
                    Print("Bar Open");
                    //LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Inside)
                {
                    Print("Bar Inside");

                    //LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Close)
                {
                    Print("Bar Close");
                    //LeEntry.Debug();

                }
            }
        }
        private void SendStpLmtEntry()
        {
            // normal strategy framework just use SendEntry()

            // long entry conditions
            if (LeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && LeEntry2.SendEntry == false
                && LeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - limit order
                double limitPrice = Bars.Low[0] - (10 * OnePip);
                LeEntry1.SetLmtEntry(limitPrice, 100000, 100);
                LeEntry1.SetStopLoss(limitPrice - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.High[0] + (10 * OnePip);
                LeEntry2.SetStpEntry(stopPrice, 100000, 100);
                LeEntry2.SetStopLoss(stopPrice - (10 * OnePip), true);
                LeEntry2.SetProfitTarget(1, 30000, 1, true);
                LeEntry2.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry2.SetProfitTarget(3, 30000, 2, true);
            }

            // call function for calculations
            LeEntry1.Call();
            LeEntry2.Call();

            // order cancel order
            LeEntry1.OCO(LeEntry2);

            // if both true then send orders
            if (LeEntry1.SendEntry && LeEntry2.SendEntry)
            {
                LeLmt.Send(LeEntry1.LimitPrice, LeEntry1.Contracts);
                LeStp.Send(LeEntry2.StopPrice, LeEntry2.Contracts);
            }
        }
        private void SendStpLmtEntry2()
        {
            // normal strategy framework just use SendEntry()

            // long entry conditions
            if (LeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && LeEntry2.SendEntry == false
                && LeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - limit order
                double limitPrice = Bars.Low[0] - (2.5 * OnePip);
                LeEntry1.SetLmtEntry(limitPrice, 100000, 100);
                LeEntry1.SetStopLoss(limitPrice - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.High[0] + (10 * OnePip);
                LeEntry2.SetStpEntry(stopPrice, 100000, 100);
                LeEntry2.SetStopLoss(stopPrice - (10 * OnePip), true);
                LeEntry2.SetProfitTarget(1, 30000, 1, true);
                LeEntry2.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry2.SetProfitTarget(3, 30000, 2, true);
            }

            // call function for calculations
            LeEntry1.Call();
            LeEntry2.Call();

            // order cancel order
            LeEntry1.OCO(LeEntry2);

            LeEntry1.Debug(4365, 3);
            LeEntry2.Debug(4365, 3);

            // if both true then send orders
            if (LeEntry1.SendEntry && LeEntry2.SendEntry)
            {
                LeLmt.Send(LeEntry1.LimitPrice, LeEntry1.Contracts);
                LeStp.Send(LeEntry2.StopPrice, LeEntry2.Contracts);
            }
        }
        private void SendStpLmtEntry3()
        {
            // normal strategy framework just use SendEntry()

            // long entry conditions
            if (LeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && LeEntry2.SendEntry == false
                && LeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - limit order
                double limitPrice = Bars.Close[0] - (10 * OnePip);
                LeEntry1.SetLmtEntry(limitPrice, 100000, 100);
                LeEntry1.SetStopLoss(limitPrice - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.Close[0] + (10 * OnePip);
                LeEntry2.SetStpEntry(stopPrice, 100000, 100);
                LeEntry2.SetStopLoss(stopPrice - (10 * OnePip), true);
                LeEntry2.SetProfitTarget(1, 30000, 1, true);
                LeEntry2.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry2.SetProfitTarget(3, 30000, 2, true);
            }

            // call function for calculations
            LeEntry1.Call();
            LeEntry2.Call();

            // LeEntry1.Debug(4365, 5);
            // LeEntry2.Debug(4365, 5);

            // order cancel order
            LeEntry1.OCO(LeEntry2);

            // if both true then send orders
            if (LeEntry1.SendEntry && LeEntry2.SendEntry)
            {
                LeLmt.Send(LeEntry1.LimitPrice, LeEntry1.Contracts);
                LeStp.Send(LeEntry2.StopPrice, LeEntry2.Contracts);
            }
        }
        private void SendExitOrders()
        {
            if (LeEntry1.SendStopLoss)
            {
                LxStp.Send(LeEntry1.StopLossPrice);
            }

            if (LeEntry1.SendProfit1)
            {
                LePft1.Send(LeEntry1.ProfitPrice1, LeEntry1.ProfitQty1);
            }

            if (LeEntry1.SendProfit2)
            {
                LePft2.Send(LeEntry1.ProfitPrice2, LeEntry1.ProfitQty2);
            }

            if (LeEntry1.SendProfit3)
            {
                LePft3.Send(LeEntry1.ProfitPrice3, LeEntry1.ProfitQty3);
            }

            if (LeEntry2.SendStopLoss)
            {
                LxStp.Send(LeEntry2.StopLossPrice);
            }

            if (LeEntry2.SendProfit1)
            {
                LePft1.Send(LeEntry2.ProfitPrice1, LeEntry2.ProfitQty1);
            }

            if (LeEntry2.SendProfit2)
            {
                LePft2.Send(LeEntry2.ProfitPrice2, LeEntry2.ProfitQty2);
            }

            if (LeEntry2.SendProfit3)
            {
                LePft3.Send(LeEntry2.ProfitPrice3, LeEntry2.ProfitQty3);
            }

            if (SeEntry.SendStopLoss)
            {
                SxStp.Send(SeEntry.StopLossPrice);
            }

            if (SeEntry.SendProfit1)
            {
                SePft1.Send(SeEntry.ProfitPrice1, SeEntry.ProfitQty1);
            }

            if (SeEntry.SendProfit2)
            {
                SePft2.Send(SeEntry.ProfitPrice2, SeEntry.ProfitQty2);
            }

            if (SeEntry.SendProfit3)
            {
                SePft3.Send(SeEntry.ProfitPrice3, SeEntry.ProfitQty3);
            }

        }

        #region Send Entry Framework
        private void SendStpEntry()
        {
            // successful
            double entryPrice = Bars.Close[0] + (OnePip * 10);
            double stopPrice = Bars.Close[0] - (OnePip * 10);

            // long conditions
            if (LeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && SeEntry.SendEntry == false
                && SeEntry.IsEntryOrderFilled == false)
            {
                LeEntry1.SetStpEntry(entryPrice, 100000, 100);
                LeEntry1.SetStopLoss(stopPrice - OnePip, true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            LeEntry1.Call();

            if (LeEntry1.SendEntry) LeStp.Send(LeEntry1.StopPrice, LeEntry1.Contracts);

            // short conditions
            entryPrice = Bars.Close[0] - (OnePip * 10);
            stopPrice = Bars.Close[0] + (OnePip * 10);

            if (SeConditions()
                && SeEntry.SendEntry == false
                && SeEntry.IsEntryOrderFilled == false
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false)
            {
                SeEntry.SetStpEntry(entryPrice, 100000, 100);
                SeEntry.SetStopLoss(stopPrice + OnePip, true);
                SeEntry.SetProfitTarget(1, 30000, 1, true);
                SeEntry.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry.SetProfitTarget(3, 30000, 2, true);
            }

            SeEntry.Call();

            if (SeEntry.SendEntry) SeStp.Send(SeEntry.StopPrice, SeEntry.Contracts);
        }
        private void SendLmtEntry()
        {
            // successful
            // for limit order cannot send at close, it will not work
            double lePrice = CalcLowestLow(4) - OnePip;
            double leStop = CalcLowestLow(4) - (OnePip * 10);

            // if conditions are okay. send limit order, what happens if a new condition occurs ?
            if (LeConditions() &&
                LeEntry1.SendEntry == false &&
                LeEntry1.IsEntryOrderFilled == false &&
                SeEntry.IsEntryOrderFilled == false &&
                SeEntry.SendEntry == false)
            {
                // setup entry and exit properties
                LeEntry1.SetLmtEntry(lePrice, 100000, 100);
                LeEntry1.SetStopLoss(leStop - OnePip, true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            // function calcbar will do the settle the logic to decide to send order
            LeEntry1.Call();

            // if entry conditions are right, send order
            if (LeEntry1.SendEntry) LeLmt.Send(LeEntry1.EntryPrice, LeEntry1.Contracts);

            double sePrice = CalcHighestHigh(4) + OnePip;
            double seStop = CalcHighestHigh(4) + (OnePip * 10);

            // conditions check
            if (SeConditions() &&
                !SeEntry.SendEntry &&
                !SeEntry.IsEntryOrderFilled &&
                !LeEntry1.IsEntryOrderFilled &&
                !LeEntry1.SendEntry)
            {
                // setup entry and exit properties
                SeEntry.SetLmtEntry(sePrice, 100000, 100);
                SeEntry.SetStopLoss(seStop + OnePip, true);
                SeEntry.SetProfitTarget(1, 30000, 1, true);
                SeEntry.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry.SetProfitTarget(3, 30000, 2, true);
            }

            SeEntry.Call();

            if (SeEntry.SendEntry) SeLmt.Send(SeEntry.LimitPrice, SeEntry.Contracts);
        }
        private void SendMktEntry()
        {
            // successful
            // send entry orders on closing tick
            if (Bars.Status != EBarState.Close) return;

            // long entry condition are valid
            if (LeConditions() && LeEntry1.IsEntryOrderFilled == false && SeEntry.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcLowestLow(4);

                // set stop loss, separate from entry order, sendentryorder() for entry info
                LeEntry1.SetMktEntry(close, 100000);
                LeEntry1.SetStopLoss(stop - OnePip, false);
                LeEntry1.SetProfitTarget(1, 30000, 1);
                LeEntry1.SetProfitTarget(2, 40000, 1.5);
                LeEntry1.SetProfitTarget(3, 30000, 2);

                // send mkt always returns true, or change to SendEntryOrder()
                LeMkt.Send(LeEntry1.Contracts);
            }

            if (SeConditions() && SeEntry.IsEntryOrderFilled == false && LeEntry1.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcHighestHigh(4);

                SeEntry.SetMktEntry(close, 100000);
                SeEntry.SetStopLoss(stop + OnePip, false);
                SeEntry.SetProfitTarget(1, 30000, 1);
                SeEntry.SetProfitTarget(2, 40000, 1.5);
                SeEntry.SetProfitTarget(3, 30000, 2);

                SeMkt.Send(SeEntry.Contracts);
            }

            //Debug();
        }
        #endregion

        #endregion

        #region Strategy Conditions

        public double CalcLowestLow(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
        }
        public double CalcHighestHigh(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
        }
        private bool LeConditions()
        {
            if (PamaBottom() && BarHighBreak(8))
                return true;

            return false;
        }

        private bool SeConditions()
        {
            return PamaPeak() && BarLowBreak(8) ? true : false;
        }
        private bool PamaBottom()
        {
            if (Pama1[0] > Pama1[1] && Pama1[1] < Pama1[2])
                return true;

            return false;
        }
        private bool PamaPeak()
        {
            if (Pama1[0] < Pama1[1] && Pama1[1] > Pama1[2])
                return true;

            return false;
        }
        private bool BarHighBreak(int bars)
        {
            double high = 0;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                high = Math.Max(high, Bars.High[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] > high;
        }

        private bool BarLowBreak(int bars)
        {
            double low = double.MaxValue;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] < low;
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

    #region Strategy - Price Pattern v30

    public class __SherNing_PricePatterns_v30 : SignalObject
    {
        //=========================================================================================
        // Project Name : Price Patterns
        // Description  : Order Execution Test Framework
        // Type         : Strategy
        // Version      : v3.0
        // Date         : 12 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Protocol for starting and completing a project
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *--------------------------------------- Objectives --------------------------------------
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         * - I am a quant who is chill and relax, making things look simple and effortless
         *---------------------------------------- Versions ---------------------------------------
        /*/

        // strategy inputs
        [Input] public bool ShowPriceLevels { get; set; }
        public __SherNing_PricePatterns_v30(object _ctx) : base(_ctx)
        {
            ShowPriceLevels = true;
        }

        // reference fields 
        PricePatternStrategy PricePattern;
        VariableSeries<double> Pama1, Pama2, Pama3;
        VariableSeries<int> MktPosition;
        VariableSeries<EMarketPositionSide> MktDirection;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LeLmt, SeLmt;
        IOrderPriced LeStp, SeStp;
        IOrderPriced LePft1, LePft2, LePft3;
        IOrderPriced SePft1, SePft2, SePft3;

        TradeInfo_v30 LeEntry, SeEntry;
        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);
            LeEntry = new TradeInfo_v30(this, EMarketPositionSide.Long, "LE_1");
            SeEntry = new TradeInfo_v30(this, EMarketPositionSide.Short, "SE_1");
            CurrentBars = new VariableSeries<int>(this);

            // market entry
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE_Mkt", EOrderAction.Buy));

            SeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "SE_Mkt", EOrderAction.SellShort));

            // limit entry 
            LeLmt = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LE_Lmt", EOrderAction.Buy));

            SeLmt = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "SE_Lmt", EOrderAction.SellShort));

            // stop loss 
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "LX_Stp", EOrderAction.Sell));

            SeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "SX_Stp", EOrderAction.BuyToCover));

            // profit exit
            LePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LX_P1", EOrderAction.Sell));

            LePft2 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P2", EOrderAction.Sell));

            LePft3 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P3", EOrderAction.Sell));

            // profit exit
            SePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "SX_P1", EOrderAction.BuyToCover));

            SePft2 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "SX_P2", EOrderAction.BuyToCover));

            SePft3 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "SX_P3", EOrderAction.BuyToCover));

            MktPosition = new VariableSeries<int>(this);
            MktDirection = new VariableSeries<EMarketPositionSide>(this);

            // clear output window
            Output.Clear();
        }

        // value fields 
        double OnePip;
        const int EnoughData = 1200;
        bool LeLmtInForce, SeLmtInForce;
        protected override void StartCalc()
        {
            PricePattern._Length = 6;
            PricePattern._Smooth = 10;
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
            LeLmtInForce = SeLmtInForce = false;

            if (ShowPriceLevels)
            {
                // display price lines
                LeEntry.DisplayEntry = LeEntry.DisplayProfits = LeEntry.DisplayStopLoss = true;
                SeEntry.DisplayStopLoss = SeEntry.DisplayProfits = true;
            }
            else
            {
                LeEntry.DisplayEntry = LeEntry.DisplayProfits = LeEntry.DisplayStopLoss = false;
                SeEntry.DisplayStopLoss = SeEntry.DisplayProfits = false;
            }
        }

        protected override void CalcBar()
        {
            // call strategy function on closing tick
            if (Bars.Status == EBarState.Close) PricePattern.Call();

            // keep track of market position
            MktPosition.Value = StrategyInfo.MarketPosition;
            MktDirection.Value = CurrentPosition.Side;

            if (Bars.CurrentBar == 1)
            {
                // assign same reference location as function
                Pama1 = PricePattern.Pama1;
                Pama2 = PricePattern.Pama2;
                Pama3 = PricePattern.Pama3;
            }

            // ensure enought data
            if (Bars.CurrentBar > EnoughData)
            {
                SendLmtEntry();
                SendExitOrders();
            }

            //Debug();
        }

        #region Send Order Command
        DateTime CurrentTime;
        private void Debug()
        {
            // debug market position and market direction
            int barNum = 5342;
            // to debug specific bar. use chart number - maxbarsback(50)

            int maxBarsBack = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar >= (barNum - maxBarsBack) && Bars.CurrentBar < (barNum - maxBarsBack + 10))
            {
                DateTime time = Bars.TimeValue;

                if (time != CurrentTime)
                {
                    CurrentTime = time;
                    Output.WriteLine("");
                }

                //Print("Bar Number: " + Bars.CurrentBar);
                //Print("Entry Price: " + LeEntry.EntryPrice);
                //Print("Stop Loss Price: " + LeEntry.StopLossPrice);


                if (Bars.Status == EBarState.Open)
                {
                    Print("Bar Open");
                    //LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Inside)
                {
                    Print("Bar Inside");

                    //LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Close)
                {
                    Print("Bar Close");
                    //LeEntry.Debug();

                }
            }
        }
        private void SendLmtEntry()
        {
            // for limit order cannot send at close, it will not work
            double open = CalcLowestLow(4) - OnePip;
            double stop = CalcLowestLow(4) - (OnePip * 10);

            // if conditions are okay. send limit order, what happens if a new condition occurs ?
            if (LeConditions() && LeEntry.SendEntry == false && LeEntry.IsEntryOrderFilled == false)
            {
                // setup entry and exit properties
                LeEntry.SetLmtEntry(open, 100000, 100);
                LeEntry.SetStopLoss(stop - OnePip, true);
                LeEntry.SetProfitTarget(1, 30000, 1, true);
                LeEntry.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry.SetProfitTarget(3, 30000, 2, true);
            }

            // function calcbar will do the settle the logic to decide to send order
            LeEntry.Call();

            // if entry conditions are right, send order
            if (LeEntry.SendEntry) LeLmt.Send(LeEntry.EntryPrice, LeEntry.Contracts);
        }
        private void SendMktEntry()
        {
            // send entry orders on closing tick
            if (Bars.Status != EBarState.Close) return;

            // long entry condition are valid
            if (LeConditions() && LeEntry.IsEntryOrderFilled == false && SeEntry.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcLowestLow(4);

                // set stop loss, separate from entry order, sendentryorder() for entry info
                LeEntry.SetMktEntry(close, 100000);
                LeEntry.SetStopLoss(stop - OnePip, false);
                LeEntry.SetProfitTarget(1, 30000, 1);
                LeEntry.SetProfitTarget(2, 40000, 1.5);
                LeEntry.SetProfitTarget(3, 30000, 2);

                // send mkt always returns true, or change to SendEntryOrder()
                LeMkt.Send(LeEntry.Contracts);
            }

            if (SeConditions() && SeEntry.IsEntryOrderFilled == false && LeEntry.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcHighestHigh(4);

                SeEntry.SetMktEntry(close, 100000);
                SeEntry.SetStopLoss(stop + OnePip, false);
                SeEntry.SetProfitTarget(1, 30000, 1);
                SeEntry.SetProfitTarget(2, 40000, 1.5);
                SeEntry.SetProfitTarget(3, 30000, 2);

                SeMkt.Send(SeEntry.Contracts);
            }

            //Debug();
        }
        private void SendExitOrders()
        {
            LeEntry.Call();

            if (LeEntry.SendStopLoss)
            {
                LeStp.Send(LeEntry.StopLossPrice);
            }

            if (LeEntry.SendProfit1)
            {
                LePft1.Send(LeEntry.ProfitPrice1, LeEntry.ProfitQty1);
            }

            if (LeEntry.SendProfit2)
            {
                LePft2.Send(LeEntry.ProfitPrice2, LeEntry.ProfitQty2);
            }

            if (LeEntry.SendProfit3)
            {
                LePft3.Send(LeEntry.ProfitPrice3, LeEntry.ProfitQty3);
            }

            // foreach trade info you need to call
            SeEntry.Call();

            if (SeEntry.SendStopLoss)
            {
                SeStp.Send(SeEntry.StopLossPrice);
            }

            if (SeEntry.SendProfit1)
            {
                SePft1.Send(SeEntry.ProfitPrice1, SeEntry.ProfitQty1);
            }

            if (SeEntry.SendProfit2)
            {
                SePft2.Send(SeEntry.ProfitPrice2, SeEntry.ProfitQty2);
            }

            if (SeEntry.SendProfit3)
            {
                SePft3.Send(SeEntry.ProfitPrice3, SeEntry.ProfitQty3);
            }

        }
        #endregion

        #region Strategy Conditions

        public double CalcLowestLow(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
        }
        public double CalcHighestHigh(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
        }
        private bool LeConditions()
        {
            if (PamaBottom() && BarHighBreak(8))
                return true;

            return false;
        }

        private bool SeConditions()
        {
            return PamaPeak() && BarLowBreak(8) ? true : false;
        }
        private bool PamaBottom()
        {
            if (Pama1[0] > Pama1[1] && Pama1[1] < Pama1[2])
                return true;

            return false;
        }
        private bool PamaPeak()
        {
            if (Pama1[0] < Pama1[1] && Pama1[1] > Pama1[2])
                return true;

            return false;
        }
        private bool BarHighBreak(int bars)
        {
            double high = 0;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                high = Math.Max(high, Bars.High[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] > high;
        }

        private bool BarLowBreak(int bars)
        {
            double low = double.MaxValue;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] < low;
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

    #region Strategy - Price Pattern v21

    public class __SherNing_PricePatterns_v21 : SignalObject
    {
        //=========================================================================================
        // Name           : __SherNing_PricePatterns
        // Description    : Price Action Patterns Framework
        // Version        : v2.1
        // Date Created   : 01 - Aug - 2020
        // Time Taken     : 
        // Remarks        : Protocol for starting and completing a project
        //=========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //=========================================================================================

        /*/
         *----------------------------------------- Version ---------------------------------------
         *  - Move most of the logic to TradeInfo 2.1
         *  - Most recent version is the one without the version number
         *----------------------------------------- Version ---------------------------------------
           
         *------------------------------------------ Notes ----------------------------------------
         *------------------------------------------ Notes ----------------------------------------
        /*/

        // strategy inputs
        [Input] public bool ShowPriceLevels { get; set; }
        public __SherNing_PricePatterns_v21(object _ctx) : base(_ctx)
        {
            ShowPriceLevels = true;
        }

        // reference fields 
        PricePatternStrategy PricePattern;
        VariableSeries<double> Pama1, Pama2, Pama3;
        VariableSeries<int> MktPosition;
        VariableSeries<EMarketPositionSide> MktDirection;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LeStp, SeStp;
        IOrderPriced LePft1, LePft2, LePft3;
        IOrderPriced SePft1, SePft2, SePft3;

        TradeInfo_v21 LeEntry, SeEntry;
        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);
            LeEntry = new TradeInfo_v21(this, EMarketPositionSide.Long, "LE_1");
            SeEntry = new TradeInfo_v21(this, EMarketPositionSide.Short, "SE_1");
            CurrentBars = new VariableSeries<int>(this);

            // entry
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE_Mkt", EOrderAction.Buy));

            SeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "SE_Mkt", EOrderAction.SellShort));

            // stop loss 
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "LX_Stp", EOrderAction.Sell));

            SeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "SX_Stp", EOrderAction.BuyToCover));

            // profit exit
            LePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LX_P1", EOrderAction.Sell));

            LePft2 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P2", EOrderAction.Sell));

            LePft3 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P3", EOrderAction.Sell));

            // profit exit
            SePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "SX_P1", EOrderAction.BuyToCover));

            SePft2 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "SX_P2", EOrderAction.BuyToCover));

            SePft3 = OrderCreator.Limit(new SOrderParameters(
               Contracts.UserSpecified, "SX_P3", EOrderAction.BuyToCover));

            MktPosition = new VariableSeries<int>(this);
            MktDirection = new VariableSeries<EMarketPositionSide>(this);

            // clear output window
            Output.Clear();
        }

        // value fields 
        double OnePip;
        const int EnoughData = 1200;
        protected override void StartCalc()
        {
            PricePattern._Length = 6;
            PricePattern._Smooth = 10;
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            if (ShowPriceLevels)
            {
                // display price lines
                LeEntry.DisplayProfits = LeEntry.DisplayStopLoss = true;
                SeEntry.DisplayStopLoss = SeEntry.DisplayProfits = true;
            }
            else
            {
                LeEntry.DisplayProfits = LeEntry.DisplayStopLoss = false;
                SeEntry.DisplayStopLoss = SeEntry.DisplayProfits = false;
            }
        }

        protected override void CalcBar()
        {
            // call strategy function on closing tick
            if (Bars.Status == EBarState.Close) PricePattern.Call();

            // keep track of market position
            MktPosition.Value = StrategyInfo.MarketPosition;
            MktDirection.Value = CurrentPosition.Side;

            if (Bars.CurrentBar == 1)
            {
                // assign same reference location as function
                Pama1 = PricePattern.Pama1;
                Pama2 = PricePattern.Pama2;
                Pama3 = PricePattern.Pama3;
            }

            // ensure enought data
            if (Bars.CurrentBar > EnoughData)
            {
                SendEntryOrders();
                SendExitOrders();
            }

            //Debug();
        }

        #region Send Order Command
        DateTime CurrentTime;
        private void Debug()
        {
            // debug market position and market direction
            int barNum = 5342;
            // to debug specific bar. use chart number - maxbarsback(50)

            int maxBarsBack = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar >= (barNum - maxBarsBack) && Bars.CurrentBar < (barNum - maxBarsBack + 10))
            {
                DateTime time = Bars.TimeValue;

                if (time != CurrentTime)
                {
                    CurrentTime = time;
                    Output.WriteLine("");
                }

                //Print("Bar Number: " + Bars.CurrentBar);
                //Print("Entry Price: " + LeEntry.EntryPrice);
                //Print("Stop Loss Price: " + LeEntry.StopLossPrice);


                if (Bars.Status == EBarState.Open)
                {
                    Print("Bar Open");
                    LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Inside)
                {
                    Print("Bar Inside");

                    LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Close)
                {
                    Print("Bar Close");
                    LeEntry.Debug();

                }
            }
        }
        private void SendEntryOrders()
        {
            // send entry orders on closing tick
            if (Bars.Status != EBarState.Close) return;

            // long entry condition are valid
            if (LeConditions() && LeEntry.SendEntry == false && SeEntry.SendEntry == false)
            {
                double close = Bars.Close[0];
                double stop = CalcLowestLow(4);

                LeEntry.SetEntryAndStopLoss(close, stop - OnePip, 100000);
                LeEntry.CalcNSetProfitPrice(1, 30000, 1);
                LeEntry.CalcNSetProfitPrice(2, 40000, 1.5);
                LeEntry.CalcNSetProfitPrice(3, 30000, 2);
                LeMkt.Send(LeEntry.Contracts);
            }

            if (SeConditions() && SeEntry.SendEntry == false && LeEntry.SendEntry == false)
            {
                double close = Bars.Close[0];
                double stop = CalcHighestHigh(4);

                SeEntry.SetEntryAndStopLoss(close, stop + OnePip, 100000);
                SeEntry.CalcNSetProfitPrice(1, 30000, 1);
                SeEntry.CalcNSetProfitPrice(2, 40000, 1.5);
                SeEntry.CalcNSetProfitPrice(3, 30000, 2);
                SeMkt.Send(SeEntry.Contracts);
            }

            //Debug();
        }
        private void SendExitOrders()
        {
            LeEntry.Call();

            if (LeEntry.SendStopLoss)
            {
                LeStp.Send(LeEntry.StopLossPrice);
            }

            if (LeEntry.SendProfit1)
            {
                LePft1.Send(LeEntry.ProfitPrice1, LeEntry.ProfitQty1);
            }

            if (LeEntry.SendProfit2)
            {
                LePft2.Send(LeEntry.ProfitPrice2, LeEntry.ProfitQty2);
            }

            if (LeEntry.SendProfit3)
            {
                LePft3.Send(LeEntry.ProfitPrice3, LeEntry.ProfitQty3);
            }

            // foreach trade info you need to call
            SeEntry.Call();

            if (SeEntry.SendStopLoss)
            {
                SeStp.Send(SeEntry.StopLossPrice);
            }

            if (SeEntry.SendProfit1)
            {
                SePft1.Send(SeEntry.ProfitPrice1, SeEntry.ProfitQty1);
            }

            if (SeEntry.SendProfit2)
            {
                SePft2.Send(SeEntry.ProfitPrice2, SeEntry.ProfitQty2);
            }

            if (SeEntry.SendProfit3)
            {
                SePft3.Send(SeEntry.ProfitPrice3, SeEntry.ProfitQty3);
            }

        }
        #endregion

        #region Strategy Conditions

        public double CalcLowestLow(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
        }
        public double CalcHighestHigh(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
        }
        private bool LeConditions()
        {
            if (PamaBottom() && BarHighBreak(8))
                return true;

            return false;
        }

        private bool SeConditions()
        {
            return PamaPeak() && BarLowBreak(8) ? true : false;
        }
        private bool PamaBottom()
        {
            if (Pama1[0] > Pama1[1] && Pama1[1] < Pama1[2])
                return true;

            return false;
        }
        private bool PamaPeak()
        {
            if (Pama1[0] < Pama1[1] && Pama1[1] > Pama1[2])
                return true;

            return false;
        }
        private bool BarHighBreak(int bars)
        {
            double high = 0;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                high = Math.Max(high, Bars.High[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] > high;
        }

        private bool BarLowBreak(int bars)
        {
            double low = double.MaxValue;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] < low;
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

    #region Strategy - Price Pattern v20

    public class __SherNing_PricePatterns_v20 : SignalObject
    {
        //=========================================================================================
        // Name           : __SherNing_PricePatterns
        // Description    : Price Action Patterns Framework
        // Version        : v2.0
        // Date Created   : 01 - Aug - 2020
        // Time Taken     : 
        // Remarks        : Protocol for starting and completing a project
        //=========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //=========================================================================================

        /*/
         *----------------------------------------- Version ---------------------------------------
         *  - Need to test with market position and position side
         *  - Need IOG for same bar exit
         *  - Implement proper source control
         *  - Success, attend to special cases.
         *  - multi exits requires strategy settings set to multi exits on same bar
         *----------------------------------------- Version ---------------------------------------
           
         *------------------------------------------ Notes ----------------------------------------
         *  - Design a strategy to have one entry, one stop and 2 profit targets
         *  - Build the Trade Info inside strategy first
         *------------------------------------------ Notes ----------------------------------------
        /*/

        // strategy inputs

        public __SherNing_PricePatterns_v20(object _ctx) : base(_ctx)
        {

        }

        // reference fields 
        PricePatternStrategy PricePattern;
        VariableSeries<double> Pama1, Pama2, Pama3;
        VariableSeries<int> MktPosition;
        VariableSeries<EMarketPositionSide> MktDirection;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LeStp, SeStp;
        IOrderPriced LePft1, LePft2, SePft;

        TradeInfo_v20 LeEntry;
        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);
            LeEntry = new TradeInfo_v20(this);
            CurrentBars = new VariableSeries<int>(this);

            // entry
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE_Mkt", EOrderAction.Buy));

            SeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "SE_Mkt", EOrderAction.SellShort));

            // stop loss 
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "LX_Stp", EOrderAction.Sell));

            SeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "SX_Stp", EOrderAction.BuyToCover));

            // profit exit
            LePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LX_P1", EOrderAction.Sell));

            LePft2 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P2", EOrderAction.Sell));

            SePft = OrderCreator.Limit(new SOrderParameters(
                Contracts.Default, "SX_P1", EOrderAction.BuyToCover));

            MktPosition = new VariableSeries<int>(this);
            MktDirection = new VariableSeries<EMarketPositionSide>(this);

            // clear output window
            Output.Clear();
        }

        // value fields 
        double OnePip;
        protected override void StartCalc()
        {
            PricePattern._Length = 6;
            PricePattern._Smooth = 10;
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
        }

        protected override void CalcBar()
        {
            // call strategy function on closing tick
            if (Bars.Status == EBarState.Close) PricePattern.Call();

            // keep track of market position
            MktPosition.Value = StrategyInfo.MarketPosition;
            MktDirection.Value = CurrentPosition.Side;

            if (Bars.CurrentBar == 1)
            {
                // assign same reference location as function
                Pama1 = PricePattern.Pama1;
                Pama2 = PricePattern.Pama2;
                Pama3 = PricePattern.Pama3;
            }

            SendEntryOrders();
            SendExitOrders();
            //Debug();
        }

        #region Send Order Command
        DateTime CurrentTime;
        private void Debug()
        {
            // debug market position and market direction
            int barNum = 5342;
            // to debug specific bar. use chart number - maxbarsback(50)

            int maxBarsBack = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar >= (barNum - maxBarsBack) && Bars.CurrentBar < (barNum - maxBarsBack + 10))
            {
                DateTime time = Bars.TimeValue;

                if (time != CurrentTime)
                {
                    CurrentTime = time;
                    Output.WriteLine("");
                }

                //Print("Bar Number: " + Bars.CurrentBar);
                //Print("Entry Price: " + LeEntry.EntryPrice);
                //Print("Stop Loss Price: " + LeEntry.StopLossPrice);


                if (Bars.Status == EBarState.Open)
                {
                    Print("Bar Open");
                    LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Inside)
                {
                    Print("Bar Inside");

                    LeEntry.Debug();

                }
                if (Bars.Status == EBarState.Close)
                {
                    Print("Bar Close");
                    LeEntry.Debug();

                }
            }
        }
        private void Reset()
        {

        }
        private void SendEntryOrders()
        {
            // send entry orders on closing tick
            if (Bars.Status != EBarState.Close) return;

            // long entry condition are valid
            if (LeConditions() && LeEntry.SendEntry == false)
            {
                double close = Bars.Close[0];
                double stop = CalcLowestLow(4);

                LeEntry.EntryPrice = close;
                LeEntry.StopLossPrice = stop - OnePip;
                LeEntry.SendStopLoss = true;
                LeEntry.ProfitPrice1 = close + ((close - stop) * 1);
                LeEntry.SendProfit1 = true;
                LeEntry.ProfitPrice2 = close + ((close - stop) * 2);
                LeEntry.SendProfit2 = true;
                LeEntry.Contracts = 100000;
                LeEntry.ProfitQty1 = 50000;
                LeEntry.ProfitQty2 = 50000;
                LeMkt.Send(LeEntry.Contracts);
                LeEntry.SendEntry = true;
            }

            //Debug();
        }
        private void SendExitOrders()
        {
            DateTime time = Bars.Time[0];

            if (LeEntry.SendStopLoss)
            {
                LeStp.Send(LeEntry.StopLossPrice);

                if (Bars.Status == EBarState.Close)
                {
                    ChartPoint p = new ChartPoint(time, LeEntry.StopLossPrice);
                    ITextObject txt = DrwText.Create(p, "-");
                    txt.Color = Color.Yellow;
                }
            }
            if (LeEntry.SendProfit1)
            {
                LePft1.Send(LeEntry.ProfitPrice1, LeEntry.ProfitQty1);
                if (Bars.Status == EBarState.Close)
                {
                    ChartPoint p = new ChartPoint(time, LeEntry.ProfitPrice1);
                    ITextObject txt = DrwText.Create(p, "-");
                    txt.Color = Color.Cyan;
                }
            }
            if (LeEntry.SendProfit2)
            {
                LePft2.Send(LeEntry.ProfitPrice2, LeEntry.ProfitQty2);
                if (Bars.Status == EBarState.Close)
                {
                    ChartPoint p = new ChartPoint(time, LeEntry.ProfitPrice2);
                    ITextObject txt = DrwText.Create(p, "-");
                    txt.Color = Color.Cyan;
                }
            }

            // reset
            //double close = Bars.Close[0];

            if (Bars.Low[0] <= LeEntry.StopLossPrice)
            {
                LeEntry.SendProfit2 = LeEntry.SendProfit1 = LeEntry.SendStopLoss = false;
                LeEntry.StopLossPrice = LeEntry.ProfitPrice1 = LeEntry.ProfitPrice2 = 0;
                LeEntry.ProfitQty1 = LeEntry.ProfitQty2 = 0;
                LeEntry.Contracts = 0;
                LeEntry.EntryPrice = 0;
                LeEntry.SendEntry = false;
            }

            if (Bars.High[0] >= LeEntry.ProfitPrice1 && Bars.High[0] >= LeEntry.ProfitPrice2)
            {
                LeEntry.SendProfit2 = LeEntry.SendProfit1 = LeEntry.SendStopLoss = false;
                LeEntry.StopLossPrice = LeEntry.ProfitPrice1 = LeEntry.ProfitPrice2 = 0;
                LeEntry.ProfitQty1 = LeEntry.ProfitQty2 = 0;
                LeEntry.Contracts = 0;
                LeEntry.EntryPrice = 0;
                LeEntry.SendEntry = false;
            }

            else if (Bars.High[0] >= LeEntry.ProfitPrice1)
            {
                LeEntry.SendProfit1 = false;
                LeEntry.ProfitPrice1 = 0;
                LeEntry.ProfitQty1 = 0;
            }

            else if (Bars.High[0] >= LeEntry.ProfitPrice2)
            {
                LeEntry.SendProfit2 = false;
                LeEntry.ProfitPrice2 = 0;
                LeEntry.ProfitQty2 = 0;
            }
        }
        #endregion

        #region Strategy Conditions

        public double CalcLowestLow(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
        }
        public double CalcHighestHigh(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
        }
        private bool LeConditions()
        {
            if (PamaBottom() && BarHighBreak(4))
                return true;

            return false;
        }
        private bool PamaBottom()
        {
            if (Pama1[0] > Pama1[1] && Pama1[1] < Pama1[2])
                return true;

            return false;
        }
        private bool PamaPeak()
        {
            if (Pama1[0] < Pama1[1] && Pama1[1] > Pama1[2])
                return true;

            return false;
        }
        private bool BarHighBreak(int bars)
        {
            double high = 0;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                high = Math.Max(high, Bars.High[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] > high;
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

    #region Strategy - Price Pattern v10
    public class __SherNing_PricePatterns_v10 : SignalObject
    {
        //==========================================================================================
        // Name           : __SherNing_PricePatterns
        // Description    : Price Action Patterns Framework
        // Version        : v1.0
        // Date Created   : 01 - Aug - 2020
        // Time Taken     : 
        // Remarks        : Protocol for starting and completing a project
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  - Need to test with market position and position side
         *  ----------------------------------- Version History -----------------------------------
         *  
         *  ------------------------------------ Project Notes ------------------------------------
         *  - Framework for testing price patterns
         *  - Exit from one ENTRY once
         *  - Complete on strategy script first before coverting to function script
         *  ------------------------------------ Project Notes ------------------------------------
         */
        [Input] public string StrategySettings { get; set; }
        [Input] public string DisplaySettings { get; set; }
        [Input] public bool ShowOrderLines { get; set; }
        public __SherNing_PricePatterns_v10(object _ctx) : base(_ctx)
        {
            StrategySettings = "Strategy Settings";
            DisplaySettings = "Display Settings";
            ShowOrderLines = true;
        }

        // reference fields 
        PricePatternStrategy PricePattern;
        VariableSeries<double> Pama1, Pama2, Pama3;
        VariableSeries<int> MarketPosition;
        VariableSeries<EMarketPositionSide> PosSide;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LeStp, SeStp;
        IOrderPriced LePft1, LePft2, SePft;
        TradeInfo_v11 LeEntry;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);
            LeEntry = new TradeInfo_v11(this, EMarketPositionSide.Long);

            // entry
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE_Mkt", EOrderAction.Buy));

            SeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "SE_Mkt", EOrderAction.SellShort));

            // stop loss 
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "LX_Stp", EOrderAction.Sell));

            SeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "SX_Stp", EOrderAction.BuyToCover));

            // profit exit
            LePft1 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LX_P1", EOrderAction.Sell));

            LePft2 = OrderCreator.Limit(new SOrderParameters(
            Contracts.UserSpecified, "LX_P2", EOrderAction.Sell));

            SePft = OrderCreator.Limit(new SOrderParameters(
                Contracts.Default, "SX_P1", EOrderAction.BuyToCover));

            MarketPosition = new VariableSeries<int>(this);
            PosSide = new VariableSeries<EMarketPositionSide>(this);

            // clear output window
            Output.Clear();
        }

        // fields 
        protected override void StartCalc()
        {
            PricePattern._Length = 6;
            PricePattern._Smooth = 10;

        }

        protected override void CalcBar()
        {
            // call strategy function
            PricePattern.Call();

            // keep track of market position
            MarketPosition.Value = StrategyInfo.MarketPosition;
            PosSide.Value = CurrentPosition.Side;

            if (Bars.CurrentBar == 1)
            {
                // assign same reference location as function
                Pama1 = PricePattern.Pama1;
                Pama2 = PricePattern.Pama2;
                Pama3 = PricePattern.Pama3;
            }

            if (Bars.Status == EBarState.Close) SendEntryOrders();

            SendExitOrders();

        }

        #region Send Order Command
        private void SendEntryOrders()
        {
            if (LeConditions() && LeEntry.Active == false)
            {
                // end of bar close value
                double close = Bars.Close[0];

                // entry info
                LeEntry.EntryPrice = close;
                LeEntry.Active = true;

                // stop info
                LeEntry.StopLossPrice = LeEntry.CalcLowestLow(4);

                // profit info
                LeEntry.ProfitPrice1 = LeEntry.CalcProfitPrice(1);
                LeEntry.ProfitPrice2 = LeEntry.CalcProfitPrice(2);

                // send entry order
                LeMkt.Send(100000);

                // update contracts 
                LeEntry.Contracts = 100000;
                LeEntry.ProfitQty1 = 50000;
                LeEntry.ProfitQty2 = 50000;
            }
        }

        private void SendExitOrders()
        {
            // track using market position
            if (LeEntry.Active)
            {
                if (LeEntry.StopLossPrice > 0 && LeEntry.Contracts > 0)
                {
                    LeStp.Send(LeEntry.StopLossPrice);
                }

                if (LeEntry.ProfitPrice1 > 0 && LeEntry.ProfitQty1 > 0)
                {
                    LePft1.Send(LeEntry.ProfitPrice1, LeEntry.ProfitQty1);
                }

                if (LeEntry.ProfitPrice2 > 0 && LeEntry.ProfitQty2 > 0)
                {
                    LePft2.Send(LeEntry.ProfitPrice2, LeEntry.ProfitQty2);
                }
            }

            if (LeEntry.StopLossPrice > 0 && Bars.Low[0] <= LeEntry.StopLossPrice)
            {
                LeEntry.StopLossPrice = LeEntry.Contracts = 0;
                LeEntry.ProfitPrice1 = LeEntry.ProfitPrice2 = 0;
                LeEntry.ProfitQty1 = LeEntry.ProfitQty2 = 0;
            }

            if (LeEntry.ProfitPrice1 > 0 && Bars.High[0] >= LeEntry.ProfitPrice1)
            {
                LeEntry.Contracts -= LeEntry.ProfitQty1;
                LeEntry.ProfitPrice1 = LeEntry.ProfitQty1 = 0;
            }

            if (LeEntry.ProfitPrice2 > 0 && Bars.High[0] >= LeEntry.ProfitPrice2)
            {
                LeEntry.Contracts -= LeEntry.ProfitQty2;
                LeEntry.ProfitPrice2 = LeEntry.ProfitQty2 = 0;
            }

            // no more contracts, return false
            if (LeEntry.Contracts == 0)
            {
                LeEntry.Active = false;
                LeEntry.StopLossPrice = LeEntry.Contracts = 0;
                LeEntry.ProfitPrice1 = LeEntry.ProfitPrice2 = 0;
                LeEntry.ProfitQty1 = LeEntry.ProfitQty2 = 0;
            }
        }
        #endregion

        #region Strategy Conditions
        private bool LeConditions()
        {
            if (PamaBottom() && BarHighBreak(4))
                return true;

            return false;
        }
        private bool PamaBottom()
        {
            if (Pama1[0] > Pama1[1] && Pama1[1] < Pama1[2])
                return true;

            return false;
        }
        private bool PamaPeak()
        {
            if (Pama1[0] < Pama1[1] && Pama1[1] > Pama1[2])
                return true;

            return false;
        }
        private bool BarHighBreak(int bars)
        {
            double high = 0;

            // get the ten bar high value, exclude current high
            for (int i = 1; i <= bars; i++)
                high = Math.Max(high, Bars.High[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] > high;
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
    #region Function - Trade Info v41
    public sealed class TradeInfo : FunctionSimple<int>
    {
        #region Documentations
        //=========================================================================================
        // Project Name : TradeInfo
        // Description  : Track limit, stop or market order
        // Type         : Function
        // Version      : v4.1
        // Date         : 15 - Aug - 2020
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
         *  4.1.1 - fix when price gaps above or below exit lines - successful
         *  4.1.2 - IOG stop and limit orders - successful
         *  4.1.3 - stop limit long entry - successful
         *  4.1.4 - stop limit short entry
         *  4.1.5 - Non-IOG trades
         *--------------------------------------- Objectives --------------------------------------
        /*/

        #region Version History
        /*/
                 *---------------------------------------- Versions ---------------------------------------
                 *  4.0.1 - FunctionSeries vs FunctionSimple - done
                 *  4.0.2 - MarketPosition and PositionSide to improve accuracy
                 *  4.0.3 - Improve members' names
                 *  4.0.4 - Fields vs VariableObject
                 *  
                 *  3.2.1 - clean up stop limit order code - done
                 *  3.2.2 - test limit and stop order seperately - done
                 *  
                 *  3.1.1 complete long limit order entry debugging and refactorization - Successful
                 *  3.1.2 complete short limit order entry, updated new IOG Mode - Successful
                 *  3.1.3 add long stop order entry - Successful
                 *  3.1.4 add short stop order entry - Successful
                 *  3.1.5 add stop entry and stop limit entry - will not work
                 *  3.1.6 add double stop entry - will not work
                 *---------------------------------------- Versions ---------------------------------------
                /*/
        #endregion

        #endregion

        // trade properties
        #region class properties
        public string EntryName { get; private set; }
        public EMarketPositionSide TradeDirection { get; private set; }

        // entry and stop properties
        public double EntryPrice { get; set; }
        public double LimitPrice { get; private set; }
        public double StopPrice { get; private set; }
        public bool IsEntryOrderFilled { get; private set; }
        public bool SendEntry { get; private set; }
        public int TimeInForce { get; private set; }
        public int Contracts { get; set; }

        // stop loss properties
        public double StopLossPrice { get; set; }
        public bool SendStopLoss { get; private set; }

        // profit 1 properties
        public double ProfitPrice1 { get; set; }
        public int ProfitQty1 { get; set; }
        public bool SendProfit1 { get; private set; }


        // profit 2 properties
        public double ProfitPrice2 { get; set; }
        public int ProfitQty2 { get; set; }
        public bool SendProfit2 { get; private set; }

        // profit 3 properties
        public double ProfitPrice3 { get; set; }
        public int ProfitQty3 { get; set; }
        public bool SendProfit3 { get; private set; }

        // display properties
        public bool DisplayEntry { private get; set; }
        public bool DisplayStopLoss { private get; set; }
        public bool DisplayProfits { private get; set; }
        #endregion

        public TradeInfo(CStudyControl m, EMarketPositionSide direction, string entryName)
            : base(m)
        {
            TradeDirection = direction;
            EntryName = entryName;
        }

        // fields - reference types
        VariableSeries<int> MarketPosition;
        protected override void Create()
        {
            // track market position
            MarketPosition = new VariableSeries<int>(this);
        }

        // fields - value types
        int TxtDisplace;
        double OnePip;
        bool PrintEntryTxtOnce, PrintStopLossTxtOnce;
        bool PrintProfit1TxtOnce, PrintProfit2TxtOnce, PrintProfit3TxtOnce;
        bool EntryPriceLock, StopLossPriceLock, ProfitPrice1Lock, ProfitPrice2Lock, ProfitPrice3Lock;
        private void CancelOrder()
        {
            // position reset method below fields for easier reference
            CancelEntryOrder();
            CancelExitOrder();
        }
        private void CancelEntryOrder()
        {
            // reset entry data
            EntryPrice = LimitPrice = StopPrice = 0;

            // reset profit flags
            ProfitPrice1Lock = ProfitPrice2Lock = ProfitPrice3Lock = false;

            // reset entry and stop loss flags
            EntryPriceLock = StopLossPriceLock = false;

            // entry text
            PrintEntryTxtOnce = false;

            // reset flag to trigger Send()
            SendEntry = false;

            // reset time in force, max 999 bars only
            TimeInForce = 0;
        }
        private void CancelExitOrder()
        {
            // reset stoploss 
            StopLossPrice = 0;

            // reset profit prices
            ProfitPrice1 = ProfitPrice2 = ProfitPrice3 = 0;

            // reset entry order flag
            IsEntryOrderFilled = false;

            // reset stop and profits flags which triggers Send()
            SendStopLoss = SendProfit1 = SendProfit2 = SendProfit3 = false;

            // reset contracts and exit quantity
            Contracts = ProfitQty1 = ProfitQty2 = ProfitQty3 = 0;

            // reset txt flags that plot exit txt
            PrintStopLossTxtOnce = PrintProfit1TxtOnce = PrintProfit2TxtOnce = PrintProfit3TxtOnce = false;
        }
        protected override void StartCalc()
        {
            // one pip = 10 ticks
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // distance between text and price levels, 1 = 1 pip
            TxtDisplace = 1;

            // reset
            CancelOrder();
        }
        protected override int CalcBar()
        {
            // CStudyControl is passed in here.
            if (StrategyInfo != null)
                MarketPosition.Value = StrategyInfo.MarketPosition;

            // get trade direction
            switch (TradeDirection)
            {
                case EMarketPositionSide.Long:
                    LongEntry();
                    LongExit();
                    break;

                case EMarketPositionSide.Short:
                    ShortEntry();
                    ShortExit();
                    break;

                // Do nothing
                default:
                case EMarketPositionSide.Flat:
                    break;
            }

            return Contracts;
        }

        #region Entry Order Logic
        DateTime CurrentTime;
        public bool OCO(TradeInfo other)
        {
            // returns true when EITHER entry is filled
            if (other.IsEntryOrderFilled)
            {
                CancelOrder();
                return true;
            }

            // if current entry is filled, cancel other entry
            else if (IsEntryOrderFilled)
            {
                other.CancelOrder();
                return true;
            }

            return false;
        }
        private void LongEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry is true, and 
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // limit order filled
                    if (Bars.Low[0] <= EntryPrice && MarketPosition[0] > 0)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.LimeGreen;

                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.LimeGreen;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }

                // stop order entry is true
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // limit order filled
                    if (Bars.High[0] >= EntryPrice && MarketPosition[0] > 0)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkGreen;

                            // plot text on entry line
                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.DarkGreen;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }
            }
        }
        private void ShortEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // short limit order filled, check mkt position if it is filled
                    if (Bars.High[0] >= EntryPrice && MarketPosition[0] < 0)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.Red;

                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.Red;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }

                // stop order entry
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // short limit order filled
                    if (Bars.Low[0] <= EntryPrice && MarketPosition[0] < 0)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkRed;

                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.DarkRed;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }
            }
        }

        #endregion

        #region Exit Order Logic
        private void LongExit()
        {
            // for exit, this must be true, need to account for same bar exit
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // stop loss hit, exit all contracts, check mktpos == flat
                if (price <= StopLossPrice && MarketPosition[0] == 0)
                {
                    CancelOrder();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (PrintStopLossTxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            PrintStopLossTxtOnce = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (MarketPosition[0] == (Contracts - ProfitQty1)
                    && ProfitQty1 <= Contracts
                    && price >= ProfitPrice1)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit1TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            PrintProfit1TxtOnce = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 2 hit
                if (MarketPosition[0] == (Contracts - ProfitQty2)
                    && ProfitQty2 <= Contracts
                    && price >= ProfitPrice2)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit2TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            PrintProfit2TxtOnce = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 3 hit
                if (MarketPosition[0] == (Contracts - ProfitQty3)
                    && ProfitQty3 <= Contracts
                    && price >= ProfitPrice3)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit3TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            PrintProfit3TxtOnce = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                CancelOrder();
                return;
            }
        }
        private void ShortExit()
        {
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // stop loss hit, exit all contracts
                if (price >= StopLossPrice && MarketPosition[0] == 0)
                {
                    CancelOrder();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (PrintStopLossTxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            PrintStopLossTxtOnce = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 1 hit
                if (price <= ProfitPrice1
                    && ProfitQty1 <= Contracts
                    && Math.Abs(MarketPosition[0]) == (Contracts - ProfitQty1))
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit1TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            PrintProfit1TxtOnce = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 2 hit
                if (price <= ProfitPrice2 
                    && ProfitQty2 <= Contracts
                    && Math.Abs(MarketPosition[0]) == (Contracts - ProfitQty2))
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit2TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            PrintProfit2TxtOnce = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 3 hit
                if (price <= ProfitPrice3 
                    && ProfitQty3 <= Contracts
                    && Math.Abs(MarketPosition[0]) == (Contracts - ProfitQty3))
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit3TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            PrintProfit3TxtOnce = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                CancelOrder();
                return;
            }
        }

        #endregion

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }

        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return 0;

            return Math.Abs(EntryPrice - StopLossPrice);
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar), offset by number of pips
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low - (pips * OnePip);
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high + (pips * OnePip);
        }
        #endregion

        #region Set Properties
        public void SetProfitTarget(int target, int quantity, double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity);
        }
        public void SetProfitTarget(int target, int quantity, double riskReward, bool lockPrice)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity, lockPrice);
        }
        public void SetProfitPrice(int target, double price, int quantity)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    ProfitPrice1 = price;
                    ProfitQty1 = quantity;
                    break;

                // profit target 2
                case 2:
                    ProfitPrice2 = price;
                    ProfitQty2 = quantity;
                    break;

                // profit target 3
                case 3:
                    ProfitPrice3 = price;
                    ProfitQty3 = quantity;
                    break;

                default:
                    break;
            }
        }
        public void SetProfitPrice(int target, double price, int quantity, bool lockPrice)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    if (lockPrice && ProfitPrice1Lock == false)
                    {
                        ProfitPrice1 = price;
                        ProfitQty1 = quantity;
                        ProfitPrice1Lock = true;
                    }
                    break;

                // profit target 2
                case 2:
                    if (lockPrice && ProfitPrice2Lock == false)
                    {
                        ProfitPrice2 = price;
                        ProfitQty2 = quantity;
                        ProfitPrice2Lock = true;
                    }
                    break;

                // profit target 3
                case 3:
                    if (lockPrice && ProfitPrice3Lock == false)
                    {
                        ProfitPrice3 = price;
                        ProfitQty3 = quantity;
                        ProfitPrice3Lock = true;
                    }
                    break;

                default:
                    break;
            }
        }
        public void SetMktEntry(double entry, int contracts)
        {
            EntryPrice = entry;
            Contracts = contracts;
            IsEntryOrderFilled = true;
        }
        public void SetStopLoss(double stoploss, bool lockPrice)
        {
            // set stop loss price only once
            if (lockPrice && StopLossPriceLock == false)
            {
                StopLossPrice = stoploss;
                StopLossPriceLock = true;
            }
            else
            {
                StopLossPrice = stoploss;
            }
        }
        public void SetLmtEntry(double limitPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (EntryPriceLock == false)
            {
                LimitPrice = EntryPrice = limitPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                EntryPriceLock = true;
                IsEntryOrderFilled = false;
            }
        }
        public void SetStpEntry(double stopPrice, int contracts, int timeInForce = 999)
        {
            // lock entry price, prevent updating
            if (EntryPriceLock == false)
            {
                StopPrice = EntryPrice = stopPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                EntryPriceLock = true;
                IsEntryOrderFilled = false;
            }
        }
        #endregion

        #region Debug
        public void Debug(int start, int bars)
        {
            int maxBars = ExecInfo.MaxBarsBack;
            start -= maxBars;

            if (Bars.CurrentBar >= start && Bars.CurrentBar <= start + bars)
            {
                Print("Class Name: " + EntryName);
                Print("Chart Bar Number: " + (Bars.CurrentBar + maxBars));
                Print("Bar Status: " + Bars.Status);
                Print("Time in Force: " + TimeInForce);
                Print("Entry Order Filled: " + IsEntryOrderFilled);
                Print("Send Entry Order: " + SendEntry);
                Print("Entry Price: " + EntryPrice);
                Print("Stop Price: " + StopPrice);
                Print("Limit Price: " + LimitPrice);
                Print("Send Stop Loss: " + SendStopLoss);
                Print("Stop Loss Price: " + StopLossPrice);
                Print("Send Profit 1: " + SendProfit1);
                Print("Profit Price 1: " + ProfitPrice1);
                Print("Profit Qty 1: " + ProfitQty1);
                Print("Send Profit 2: " + SendProfit2);
                Print("Profit Price 2: " + ProfitPrice2);
                Print("Profit Qty 2: " + ProfitQty2);
                Print("Send Profit 3: " + SendProfit3);
                Print("Profit Price 3: " + ProfitPrice3);
                Print("Profit Qty 3: " + ProfitQty3);
                Print("Contracts: " + Contracts);
                Print("Market Position: " + MarketPosition[0]);
                Print("High: " + Bars.High[0]);
                Print("Low: " + Bars.Low[0]);
                Print("Open: " + Bars.Open[0]);
                Print("Close: " + Bars.Close[0]);
                Output.WriteLine("");
            }
        }
        public void Debug()
        {
            Print("Class Name: " + EntryName);
            Print("Chart Bar Number: " + (Bars.CurrentBar + ExecInfo.MaxBarsBack));
            Print("Bar Status: " + Bars.Status);
            Print("Time in Force: " + TimeInForce);
            Print("Entry Order Filled: " + IsEntryOrderFilled);
            Print("Send Entry Order: " + SendEntry);
            Print("Entry Price: " + EntryPrice);
            Print("Stop Price: " + StopPrice);
            Print("Limit Price: " + LimitPrice);
            Print("Send Stop Loss: " + SendStopLoss);
            Print("Stop Loss Price: " + StopLossPrice);
            Print("Send Profit 1: " + SendProfit1);
            Print("Profit Price 1: " + ProfitPrice1);
            Print("Profit Qty 1: " + ProfitQty1);
            Print("Send Profit 2: " + SendProfit2);
            Print("Profit Price 2: " + ProfitPrice2);
            Print("Profit Qty 2: " + ProfitQty2);
            Print("Send Profit 3: " + SendProfit3);
            Print("Profit Price 3: " + ProfitPrice3);
            Print("Profit Qty 3: " + ProfitQty3);
            Print("Contracts: " + Contracts);
            Print("High: " + Bars.High[0]);
            Print("Low: " + Bars.Low[0]);
            Print("Open: " + Bars.Open[0]);
            Print("Close: " + Bars.Close[0]);
            Output.WriteLine("");
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

    #region Function - Trade Info v40
    public sealed class TradeInfo_v40 : FunctionSimple<int>
    {
        #region Documentations
        //=========================================================================================
        // Project Name : TradeInfo
        // Description  : Track limit, stop or market order
        // Type         : Function
        // Version      : v4.0
        // Date         : 15 - Aug - 2020
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
         *  4.0.1 - FunctionSeries vs FunctionSimple - done
         *  4.0.2 - MarketPosition and PositionSide to improve accuracy
         *  4.0.3 - Improve members' names
         *  4.0.4 - Fields vs VariableObject
         *--------------------------------------- Objectives --------------------------------------
        /*/

        /*/
         *---------------------------------------- Versions ---------------------------------------
         *  3.2.1 - clean up stop limit order code - done
         *  3.2.2 - test limit and stop order seperately - done
         *  
         *  3.1.1 complete long limit order entry debugging and refactorization - Successful
         *  3.1.2 complete short limit order entry, updated new IOG Mode - Successful
         *  3.1.3 add long stop order entry - Successful
         *  3.1.4 add short stop order entry - Successful
         *  3.1.5 add stop entry and stop limit entry - will not work
         *  3.1.6 add double stop entry - will not work
         *---------------------------------------- Versions ---------------------------------------
        /*/

        /* Side Notes 
         *  FunctionSimple - Used this like a regular class with access to Powerlanguage. Will not
         *                   update when Call() or Value() is not in use.
         *                   
         *  FunctionSimple - Used this when you need to access prev bar's closing tick values for
         *                   calculations. Otherwisem, it is similar to FunctionSimple, except that
         *                   CalcBar() is called at least once, every without external call().
         *                   
         *  VariableSeries - Preserves historical value of the variable for access
         *  
         *  VariableObject - Class fields and VariableObject is 99.99% similar.
         * 
         */
        #endregion

        // trade properties
        #region class properties
        public string EntryName { get; private set; }
        public EMarketPositionSide TradeDirection { get; private set; }

        // entry and stop properties
        public double EntryPrice { get; set; }
        public double LimitPrice { get; private set; }
        public double StopPrice { get; private set; }
        public bool IsEntryOrderFilled { get; private set; }
        public bool SendEntry { get; private set; }
        public int TimeInForce { get; private set; }
        public int Contracts { get; set; }

        // stop loss properties
        public double StopLossPrice { get; set; }
        public bool SendStopLoss { get; private set; }

        // profit 1 properties
        public double ProfitPrice1 { get; set; }
        public int ProfitQty1 { get; set; }
        public bool SendProfit1 { get; private set; }


        // profit 2 properties
        public double ProfitPrice2 { get; set; }
        public int ProfitQty2 { get; set; }
        public bool SendProfit2 { get; private set; }

        // profit 3 properties
        public double ProfitPrice3 { get; set; }
        public int ProfitQty3 { get; set; }
        public bool SendProfit3 { get; private set; }

        // display properties
        public bool DisplayEntry { private get; set; }
        public bool DisplayStopLoss { private get; set; }
        public bool DisplayProfits { private get; set; }
        #endregion

        public TradeInfo_v40(CStudyControl m, EMarketPositionSide direction, string entryName)
            : base(m)
        {
            TradeDirection = direction;
            EntryName = entryName;
        }

        // fields - reference types
        VariableSeries<int> MarketPosition;
        protected override void Create()
        {
            // track market position
            MarketPosition = new VariableSeries<int>(this);
        }

        // fields - value types
        int TxtDisplace;
        double OnePip;
        bool PrintEntryTxtOnce, PrintStopLossTxtOnce;
        bool PrintProfit1TxtOnce, PrintProfit2TxtOnce, PrintProfit3TxtOnce;
        bool EntryPriceLock, StopLossPriceLock, ProfitPrice1Lock, ProfitPrice2Lock, ProfitPrice3Lock;
        private void CancelOrder()
        {
            // position reset method below fields for easier reference
            CancelEntryOrder();
            CancelExitOrder();
        }
        private void CancelEntryOrder()
        {
            // reset entry data
            EntryPrice = LimitPrice = StopPrice = 0;

            // reset profit flags
            ProfitPrice1Lock = ProfitPrice2Lock = ProfitPrice3Lock = false;

            // reset entry and stop loss flags
            EntryPriceLock = StopLossPriceLock = false;

            // entry text
            PrintEntryTxtOnce = false;

            // reset flag to trigger Send()
            SendEntry = false;

            // reset time in force, max 999 bars only
            TimeInForce = 0;
        }
        private void CancelExitOrder()
        {
            // reset stoploss 
            StopLossPrice = 0;

            // reset profit prices
            ProfitPrice1 = ProfitPrice2 = ProfitPrice3 = 0;

            // reset entry order flag
            IsEntryOrderFilled = false;

            // reset stop and profits flags which triggers Send()
            SendStopLoss = SendProfit1 = SendProfit2 = SendProfit3 = false;

            // reset contracts and exit quantity
            Contracts = ProfitQty1 = ProfitQty2 = ProfitQty3 = 0;

            // reset txt flags that plot exit txt
            PrintStopLossTxtOnce = PrintProfit1TxtOnce = PrintProfit2TxtOnce = PrintProfit3TxtOnce = false;
        }
        protected override void StartCalc()
        {
            // one pip = 10 ticks
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // distance between text and price levels, 1 = 1 pip
            TxtDisplace = 1;

            // reset
            CancelOrder();
        }
        protected override int CalcBar()
        {
            // CStudyControl is passed in here.
            if (StrategyInfo != null)
                MarketPosition.Value = StrategyInfo.MarketPosition;

            switch (TradeDirection)
            {
                case EMarketPositionSide.Long:
                    LongEntry();
                    LongExit();
                    break;

                case EMarketPositionSide.Short:
                    ShortEntry();
                    ShortExit();
                    break;

                // Do nothing
                default:
                case EMarketPositionSide.Flat:
                    break;
            }

            return Contracts;
        }

        #region Entry Order Logic
        DateTime CurrentTime;
        public bool OCO(TradeInfo_v40 other)
        {
            // returns true when EITHER entry is filled
            if (other.IsEntryOrderFilled)
            {
                CancelOrder();
                return true;
            }

            // if current entry is filled, cancel other entry
            else if (IsEntryOrderFilled)
            {
                other.CancelOrder();
                return true;
            }

            return false;
        }
        private void LongEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry is true, and 
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // limit order filled
                    if (Bars.Low[0] <= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.LimeGreen;

                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.LimeGreen;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }

                // stop order entry is true
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // limit order filled
                    if (Bars.High[0] >= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkGreen;

                            // plot text on entry line
                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.DarkGreen;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }
            }
        }
        private void ShortEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // short limit order filled
                    if (Bars.High[0] >= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.Red;

                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.Red;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }

                // stop order entry
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // short limit order filled
                    if (Bars.Low[0] <= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkRed;

                            if (PrintEntryTxtOnce == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (TxtDisplace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.DarkRed;
                                PrintEntryTxtOnce = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }
            }
        }

        #endregion

        #region Exit Order Logic
        private void LongExit()
        {
            // for exit, this must be true, need to account for same bar exit
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // stop loss hit, exit all contracts, add GAP logic
                if (price <= StopLossPrice)
                {
                    CancelOrder();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (PrintStopLossTxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            PrintStopLossTxtOnce = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit1TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            PrintProfit1TxtOnce = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit2TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            PrintProfit2TxtOnce = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit3TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            PrintProfit3TxtOnce = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                CancelOrder();
                return;
            }
        }
        private void ShortExit()
        {
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // stop loss hit, exit all contracts
                if (price >= StopLossPrice)
                {
                    CancelOrder();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (PrintStopLossTxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            PrintStopLossTxtOnce = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 1 hit
                if (price <= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit1TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            PrintProfit1TxtOnce = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 2 hit
                if (price <= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit2TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            PrintProfit2TxtOnce = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (Environment.IOGEnabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 3 hit
                if (price <= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (PrintProfit3TxtOnce == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * TxtDisplace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            PrintProfit3TxtOnce = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                CancelOrder();
                return;
            }
        }

        #endregion

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }

        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return 0;

            return Math.Abs(EntryPrice - StopLossPrice);
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar), offset by number of pips
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low - (pips * OnePip);
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high + (pips * OnePip);
        }
        #endregion

        #region Set Properties
        public void SetProfitTarget(int target, int quantity, double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity);
        }
        public void SetProfitTarget(int target, int quantity, double riskReward, bool lockPrice)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity, lockPrice);
        }
        public void SetProfitPrice(int target, double price, int quantity)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    ProfitPrice1 = price;
                    ProfitQty1 = quantity;
                    break;

                // profit target 2
                case 2:
                    ProfitPrice2 = price;
                    ProfitQty2 = quantity;
                    break;

                // profit target 3
                case 3:
                    ProfitPrice3 = price;
                    ProfitQty3 = quantity;
                    break;

                default:
                    break;
            }
        }
        public void SetProfitPrice(int target, double price, int quantity, bool lockPrice)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    if (lockPrice && ProfitPrice1Lock == false)
                    {
                        ProfitPrice1 = price;
                        ProfitQty1 = quantity;
                        ProfitPrice1Lock = true;
                    }
                    break;

                // profit target 2
                case 2:
                    if (lockPrice && ProfitPrice2Lock == false)
                    {
                        ProfitPrice2 = price;
                        ProfitQty2 = quantity;
                        ProfitPrice2Lock = true;
                    }
                    break;

                // profit target 3
                case 3:
                    if (lockPrice && ProfitPrice3Lock == false)
                    {
                        ProfitPrice3 = price;
                        ProfitQty3 = quantity;
                        ProfitPrice3Lock = true;
                    }
                    break;

                default:
                    break;
            }
        }
        public void SetMktEntry(double entry, int contracts)
        {
            EntryPrice = entry;
            Contracts = contracts;
            IsEntryOrderFilled = true;
        }
        public void SetStopLoss(double stoploss, bool lockPrice)
        {
            // set stop loss price only once
            if (lockPrice && StopLossPriceLock == false)
            {
                StopLossPrice = stoploss;
                StopLossPriceLock = true;
            }
            else
            {
                StopLossPrice = stoploss;
            }
        }
        public void SetLmtEntry(double limitPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (EntryPriceLock == false)
            {
                LimitPrice = EntryPrice = limitPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                EntryPriceLock = true;
                IsEntryOrderFilled = false;
            }
        }
        public void SetStpEntry(double stopPrice, int contracts, int timeInForce = 999)
        {
            // lock entry price, prevent updating
            if (EntryPriceLock == false)
            {
                StopPrice = EntryPrice = stopPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                EntryPriceLock = true;
                IsEntryOrderFilled = false;
            }
        }
        #endregion

        #region Debug
        public void Debug(int start, int bars)
        {
            int maxBars = ExecInfo.MaxBarsBack;
            start -= maxBars;

            if (Bars.CurrentBar >= start && Bars.CurrentBar <= start + bars)
            {
                Print("Class Name: " + EntryName);
                Print("Chart Bar Number: " + (Bars.CurrentBar + maxBars));
                Print("Bar Status: " + Bars.Status);
                Print("Time in Force: " + TimeInForce);
                Print("Entry Order Filled: " + IsEntryOrderFilled);
                Print("Send Entry Order: " + SendEntry);
                Print("Entry Price: " + EntryPrice);
                Print("Stop Price: " + StopPrice);
                Print("Limit Price: " + LimitPrice);
                Print("Send Stop Loss: " + SendStopLoss);
                Print("Stop Loss Price: " + StopLossPrice);
                Print("Send Profit 1: " + SendProfit1);
                Print("Profit Price 1: " + ProfitPrice1);
                Print("Profit Qty 1: " + ProfitQty1);
                Print("Send Profit 2: " + SendProfit2);
                Print("Profit Price 2: " + ProfitPrice2);
                Print("Profit Qty 2: " + ProfitQty2);
                Print("Send Profit 3: " + SendProfit3);
                Print("Profit Price 3: " + ProfitPrice3);
                Print("Profit Qty 3: " + ProfitQty3);
                Print("Contracts: " + Contracts);
                Print("Market Position: " + MarketPosition[0]);
                Print("High: " + Bars.High[0]);
                Print("Low: " + Bars.Low[0]);
                Print("Open: " + Bars.Open[0]);
                Print("Close: " + Bars.Close[0]);
                Output.WriteLine("");
            }
        }
        public void Debug()
        {
            Print("Class Name: " + EntryName);
            Print("Chart Bar Number: " + (Bars.CurrentBar + ExecInfo.MaxBarsBack));
            Print("Bar Status: " + Bars.Status);
            Print("Time in Force: " + TimeInForce);
            Print("Entry Order Filled: " + IsEntryOrderFilled);
            Print("Send Entry Order: " + SendEntry);
            Print("Entry Price: " + EntryPrice);
            Print("Stop Price: " + StopPrice);
            Print("Limit Price: " + LimitPrice);
            Print("Send Stop Loss: " + SendStopLoss);
            Print("Stop Loss Price: " + StopLossPrice);
            Print("Send Profit 1: " + SendProfit1);
            Print("Profit Price 1: " + ProfitPrice1);
            Print("Profit Qty 1: " + ProfitQty1);
            Print("Send Profit 2: " + SendProfit2);
            Print("Profit Price 2: " + ProfitPrice2);
            Print("Profit Qty 2: " + ProfitQty2);
            Print("Send Profit 3: " + SendProfit3);
            Print("Profit Price 3: " + ProfitPrice3);
            Print("Profit Qty 3: " + ProfitQty3);
            Print("Contracts: " + Contracts);
            Print("High: " + Bars.High[0]);
            Print("Low: " + Bars.Low[0]);
            Print("Open: " + Bars.Open[0]);
            Print("Close: " + Bars.Close[0]);
            Output.WriteLine("");
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

    #region Function - Trade Info v32
    public sealed class TradeInfo_v32 : FunctionSeries<int>
    {
        #region Documentations
        //=========================================================================================
        // Project Name : TradeInfo
        // Description  : Track limit, stop or market order
        // Type         : Function
        // Version      : v3.2
        // Date         : 14 - Aug - 2020
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
         *  3.2.1 - clean up stop limit order code - done
         *  3.2.2 - test limit and stop order seperately - done
         *--------------------------------------- Objectives --------------------------------------
        /*/

        /*/
         *---------------------------------------- Versions ---------------------------------------
         *  3.1.1 complete long limit order entry debugging and refactorization - Successful
         *  3.1.2 complete short limit order entry, updated new IOG Mode - Successful
         *  3.1.3 add long stop order entry - Successful
         *  3.1.4 add short stop order entry - Successful
         *  3.1.5 add stop entry and stop limit entry - will not work
         *  3.1.6 add double stop entry - will not work
         *---------------------------------------- Versions ---------------------------------------
        /*/
        #endregion

        // trade properties
        #region class properties
        public string EntryName { get; private set; }
        public EMarketPositionSide TradeDirection { get; private set; }
        public IOGMode IOGMode { get; private set; }

        // entry and stop properties
        public double EntryPrice { get; set; }
        public double LimitPrice { get; private set; }
        public double StopPrice { get; private set; }
        public bool IsEntryOrderFilled { get; private set; }
        public bool SendEntry { get; private set; }
        public int TimeInForce { get; private set; }
        public int Contracts { get; set; }

        // stop loss properties
        public double StopLossPrice { get; set; }
        public bool SendStopLoss { get; private set; }

        // profit 1 properties
        public double ProfitPrice1 { get; set; }
        public int ProfitQty1 { get; set; }
        public bool SendProfit1 { get; private set; }


        // profit 2 properties
        public double ProfitPrice2 { get; set; }
        public int ProfitQty2 { get; set; }
        public bool SendProfit2 { get; private set; }

        // profit 3 properties
        public double ProfitPrice3 { get; set; }
        public int ProfitQty3 { get; set; }
        public bool SendProfit3 { get; private set; }

        // display properties
        public bool DisplayEntry { private get; set; }
        public bool DisplayStopLoss { private get; set; }
        public bool DisplayProfits { private get; set; }
        #endregion

        public TradeInfo_v32(CStudyControl m, EMarketPositionSide direction, IOGMode mode, string entryName)
            : base(m)
        {
            TradeDirection = direction;
            EntryName = entryName;
            IOGMode = mode;
        }

        // fields - value types
        int Displace;
        double OnePip;
        bool EntryTxt, StopLossTxt, ProfitTxt1, ProfitTxt2, ProfitTxt3;
        bool SetEntryOnce, SetStopLossOnce, SetProfit1Once, SetProfit2Once, SetProfit3Once;
        private void CancelOrder()
        {
            // position reset method below fields for easier reference
            CancelEntryOrder();
            CancelExitOrder();
        }
        private void CancelEntryOrder()
        {
            // reset entry data
            EntryPrice = LimitPrice = StopPrice = 0;

            // reset profit flags
            SetProfit1Once = SetProfit2Once = SetProfit3Once = false;

            // reset entry and stop loss flags
            SetEntryOnce = SetStopLossOnce = false;

            // entry text
            EntryTxt = false;

            // reset flag to trigger Send()
            SendEntry = false;

            // reset time in force, max 999 bars only
            TimeInForce = 0;
        }
        private void CancelExitOrder()
        {
            // reset stoploss 
            StopLossPrice = 0;

            // reset profit prices
            ProfitPrice1 = ProfitPrice2 = ProfitPrice3 = 0;

            // reset entry order flag
            IsEntryOrderFilled = false;

            // reset stop and profits flags which triggers Send()
            SendStopLoss = SendProfit1 = SendProfit2 = SendProfit3 = false;

            // reset contracts and exit quantity
            Contracts = ProfitQty1 = ProfitQty2 = ProfitQty3 = 0;

            // reset txt flags that plot exit txt
            StopLossTxt = ProfitTxt1 = ProfitTxt2 = ProfitTxt3 = false;
        }
        protected override void StartCalc()
        {
            // one pip = 10 ticks
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // distance between text and price levels, 1 = 1 pip
            Displace = 1;

            // reset
            CancelOrder();
        }
        protected override int CalcBar()
        {
            switch (TradeDirection)
            {
                case EMarketPositionSide.Long:
                    LongEntry();
                    LongExit();
                    break;

                case EMarketPositionSide.Short:
                    ShortEntry();
                    ShortExit();
                    break;

                // Do nothing
                default:
                case EMarketPositionSide.Flat:
                    break;
            }

            return Contracts;
        }

        #region Entry Order Logic
        DateTime CurrentTime;
        public bool OCO(TradeInfo_v32 other)
        {
            // returns true when EITHER entry is filled
            if (other.IsEntryOrderFilled)
            {
                CancelOrder();
                return true;
            }

            // if current entry is filled, cancel other entry
            else if (IsEntryOrderFilled)
            {
                other.CancelOrder();
                return true;
            }

            return false;
        }
        private void LongEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry is true, and 
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // limit order filled
                    if (Bars.Low[0] <= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.LimeGreen;

                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.LimeGreen;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }

                // stop order entry is true
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // limit order filled
                    if (Bars.High[0] >= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkGreen;

                            // plot text on entry line
                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.DarkGreen;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }
            }
        }
        private void ShortEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // short limit order filled
                    if (Bars.High[0] >= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.Red;

                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.Red;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }

                // stop order entry
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // short limit order filled
                    if (Bars.Low[0] <= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkRed;

                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.DarkRed;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) CancelOrder();
                }
            }
        }

        #endregion

        #region Exit Order Logic
        private void LongExit()
        {
            // for exit, this must be true, need to account for same bar exit
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // stop loss hit, exit all contracts
                if (price <= StopLossPrice)
                {
                    CancelOrder();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                CancelOrder();
                return;
            }
        }
        private void ShortExit()
        {
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // stop loss hit, exit all contracts
                if (price >= StopLossPrice)
                {
                    CancelOrder();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 1 hit
                if (price <= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 2 hit
                if (price <= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 3 hit
                if (price <= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                CancelOrder();
                return;
            }
        }

        #endregion

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }

        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return 0;

            return Math.Abs(EntryPrice - StopLossPrice);
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar), offset by number of pips
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low - (pips * OnePip);
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high + (pips * OnePip);
        }
        #endregion

        #region Set Properties
        public void SetProfitTarget(int target, int quantity, double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity);
        }
        public void SetProfitTarget(int target, int quantity, double riskReward, bool once)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity, once);
        }
        public void SetProfitPrice(int target, double price, int quantity)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    ProfitPrice1 = price;
                    ProfitQty1 = quantity;
                    break;

                // profit target 2
                case 2:
                    ProfitPrice2 = price;
                    ProfitQty2 = quantity;
                    break;

                // profit target 3
                case 3:
                    ProfitPrice3 = price;
                    ProfitQty3 = quantity;
                    break;

                default:
                    break;
            }
        }
        public void SetProfitPrice(int target, double price, int quantity, bool once)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    if (once && SetProfit1Once == false)
                    {
                        ProfitPrice1 = price;
                        ProfitQty1 = quantity;
                        SetProfit1Once = true;
                    }
                    break;

                // profit target 2
                case 2:
                    if (once && SetProfit2Once == false)
                    {
                        ProfitPrice2 = price;
                        ProfitQty2 = quantity;
                        SetProfit2Once = true;
                    }
                    break;

                // profit target 3
                case 3:
                    if (once && SetProfit3Once == false)
                    {
                        ProfitPrice3 = price;
                        ProfitQty3 = quantity;
                        SetProfit3Once = true;
                    }
                    break;

                default:
                    break;
            }
        }
        public void SetMktEntry(double entry, int contracts)
        {
            EntryPrice = entry;
            Contracts = contracts;
            IsEntryOrderFilled = true;
        }
        public void SetStopLoss(double stoploss, bool once)
        {
            // set stop loss price only once
            if (once && SetStopLossOnce == false)
            {
                StopLossPrice = stoploss;
                SetStopLossOnce = true;
            }
            else
            {
                StopLossPrice = stoploss;
            }
        }
        public void SetLmtEntry(double limitPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (SetEntryOnce == false)
            {
                LimitPrice = EntryPrice = limitPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                SetEntryOnce = true;
                IsEntryOrderFilled = false;
            }
        }
        public void SetStpEntry(double stopPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (SetEntryOnce == false)
            {
                StopPrice = EntryPrice = stopPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                SetEntryOnce = true;
                IsEntryOrderFilled = false;
            }
        }
        #endregion

        #region Debug
        public void Debug(int start, int bars)
        {
            int maxBars = ExecInfo.MaxBarsBack;
            start -= maxBars;

            if (Bars.CurrentBar >= start && Bars.CurrentBar <= start + bars)
            {
                Print("Class Name: " + EntryName);
                Print("Chart Bar Number: " + (Bars.CurrentBar + maxBars));
                Print("Bar Status: " + Bars.Status);
                Print("Time in Force: " + TimeInForce);
                Print("Entry Order Filled: " + IsEntryOrderFilled);
                Print("Send Entry Order: " + SendEntry);
                Print("Entry Price: " + EntryPrice);
                Print("Stop Price: " + StopPrice);
                Print("Limit Price: " + LimitPrice);
                Print("Send Stop Loss: " + SendStopLoss);
                Print("Stop Loss Price: " + StopLossPrice);
                Print("Send Profit 1: " + SendProfit1);
                Print("Profit Price 1: " + ProfitPrice1);
                Print("Profit Qty 1: " + ProfitQty1);
                Print("Send Profit 2: " + SendProfit2);
                Print("Profit Price 2: " + ProfitPrice2);
                Print("Profit Qty 2: " + ProfitQty2);
                Print("Send Profit 3: " + SendProfit3);
                Print("Profit Price 3: " + ProfitPrice3);
                Print("Profit Qty 3: " + ProfitQty3);
                Print("Contracts: " + Contracts);
                Print("High: " + Bars.High[0]);
                Print("Low: " + Bars.Low[0]);
                Print("Open: " + Bars.Open[0]);
                Print("Close: " + Bars.Close[0]);
                Output.WriteLine("");
            }
        }
        public void Debug()
        {
            Print("Class Name: " + EntryName);
            Print("Chart Bar Number: " + (Bars.CurrentBar + ExecInfo.MaxBarsBack));
            Print("Bar Status: " + Bars.Status);
            Print("Time in Force: " + TimeInForce);
            Print("Entry Order Filled: " + IsEntryOrderFilled);
            Print("Send Entry Order: " + SendEntry);
            Print("Entry Price: " + EntryPrice);
            Print("Stop Price: " + StopPrice);
            Print("Limit Price: " + LimitPrice);
            Print("Send Stop Loss: " + SendStopLoss);
            Print("Stop Loss Price: " + StopLossPrice);
            Print("Send Profit 1: " + SendProfit1);
            Print("Profit Price 1: " + ProfitPrice1);
            Print("Profit Qty 1: " + ProfitQty1);
            Print("Send Profit 2: " + SendProfit2);
            Print("Profit Price 2: " + ProfitPrice2);
            Print("Profit Qty 2: " + ProfitQty2);
            Print("Send Profit 3: " + SendProfit3);
            Print("Profit Price 3: " + ProfitPrice3);
            Print("Profit Qty 3: " + ProfitQty3);
            Print("Contracts: " + Contracts);
            Print("High: " + Bars.High[0]);
            Print("Low: " + Bars.Low[0]);
            Print("Open: " + Bars.Open[0]);
            Print("Close: " + Bars.Close[0]);
            Output.WriteLine("");
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

    #region Function - Trade Info v31
    public sealed class TradeInfo_v31 : FunctionSeries<int>
    {
        //=========================================================================================
        // Project Name : TradeInfo
        // Description  : Track, update and verify order information
        // Type         : Function
        // Version      : v3.1
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
         *  3.1.1 complete long limit order entry debugging and refactorization - Successful
         *  3.1.2 complete short limit order entry, updated new IOG Mode - Successful
         *  3.1.3 add long stop order entry - Successful
         *  3.1.4 add short stop order entry - Successful
         *  3.1.5 add stop entry and stop limit entry - will not work
         *  3.1.6 add double stop entry - will not work
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *---------------------------------------- Versions ---------------------------------------
        /*/

        // trade properties
        public string EntryName { get; private set; }
        public EMarketPositionSide TradeDirection { get; private set; }
        public IOGMode IOGMode { get; private set; }

        // entry and stop properties
        public double EntryPrice { get; set; }
        public double LimitPrice { get; set; }
        public double StopPrice { get; set; }
        public bool IsEntryOrderFilled { get; private set; }
        public bool SendEntry { get; private set; }
        public int TimeInForce { get; private set; }
        public int Contracts { get; set; }

        // stop loss properties
        public double StopLossPrice { get; set; }
        public bool SendStopLoss { get; private set; }

        // profit 1 properties
        public double ProfitPrice1 { get; set; }
        public int ProfitQty1 { get; set; }
        public bool SendProfit1 { get; private set; }


        // profit 2 properties
        public double ProfitPrice2 { get; set; }
        public int ProfitQty2 { get; set; }
        public bool SendProfit2 { get; private set; }

        // profit 3 properties
        public double ProfitPrice3 { get; set; }
        public int ProfitQty3 { get; set; }
        public bool SendProfit3 { get; private set; }

        // display properties
        public bool DisplayEntry { private get; set; }
        public bool DisplayStopLoss { private get; set; }
        public bool DisplayProfits { private get; set; }

        public TradeInfo_v31(CStudyControl m, EMarketPositionSide direction, IOGMode mode, string entryName)
            : base(m)
        {
            TradeDirection = direction;
            EntryName = entryName;
            IOGMode = mode;
        }

        // fields - value types
        int Displace;
        double OnePip;
        bool EntryTxt, StopLossTxt, ProfitTxt1, ProfitTxt2, ProfitTxt3;
        bool SetEntryOnce, SetStopLossOnce, SetProfit1Once, SetProfit2Once, SetProfit3Once;
        private void ResetTrade()
        {
            // position reset method below fields for easier reference
            ResetEntry();
            ResetExit();
        }
        private void ResetEntry()
        {
            // reset entry data
            EntryPrice = LimitPrice = StopPrice = 0;

            // reset profit flags
            SetProfit1Once = SetProfit2Once = SetProfit3Once = false;

            // reset entry and stop loss flags
            SetEntryOnce = SetStopLossOnce = false;

            // entry text
            EntryTxt = false;

            // reset flag to trigger Send()
            SendEntry = false;

            // reset time in force, max 999 bars only
            TimeInForce = 0;
        }
        private void ResetStopLimitEntry()
        {
            // reset entry data
            LimitPrice = StopPrice = 0;

            // reset profit flags
            SetProfit1Once = SetProfit2Once = SetProfit3Once = false;

            // reset entry and stop loss flags
            SetEntryOnce = SetStopLossOnce = false;

            // entry text
            EntryTxt = false;

            // reset flag to trigger Send()
            SendEntry = false;

            // reset time in force, max 999 bars only
            TimeInForce = 0;
        }
        private void ResetExit()
        {
            // reset stoploss 
            StopLossPrice = 0;

            // reset profit prices
            ProfitPrice1 = ProfitPrice2 = ProfitPrice3 = 0;

            // reset entry order flag
            IsEntryOrderFilled = false;

            // reset stop and profits flags which triggers Send()
            SendStopLoss = SendProfit1 = SendProfit2 = SendProfit3 = false;

            // reset contracts and exit quantity
            Contracts = ProfitQty1 = ProfitQty2 = ProfitQty3 = 0;

            // reset txt flags that plot exit txt
            StopLossTxt = ProfitTxt1 = ProfitTxt2 = ProfitTxt3 = false;
        }
        protected override void StartCalc()
        {
            // one pip = 10 ticks
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // distance between text and price levels, 1 = 1 pip
            Displace = 1;

            // reset
            ResetTrade();
        }
        protected override int CalcBar()
        {
            switch (TradeDirection)
            {
                case EMarketPositionSide.Long:
                    LongEntry();
                    //Debug(4365, 4367);
                    LongExit();
                    break;

                case EMarketPositionSide.Short:
                    ShortEntry();
                    ShortExit();
                    break;

                // Do nothing
                default:
                case EMarketPositionSide.Flat:
                    break;
            }

            return Contracts;
        }

        #region Entry Order Logic
        DateTime CurrentTime;
        private void LongEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry is true, and 
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // limit order filled
                    if (Bars.Low[0] <= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        ResetEntry();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.LimeGreen;

                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.LimeGreen;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) ResetTrade();
                }

                // stop order entry is true
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // limit order filled
                    if (Bars.High[0] >= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        ResetEntry();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkGreen;

                            // plot text on entry line
                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LE");
                                text.Color = Color.DarkGreen;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) ResetTrade();
                }

                // stop limit entry is true
                //--------------------------------------------------------//
                // Stop limit will NOT work here. For stop limit to work, //
                // You will need to TWO tradeinfo objects, one for short, //
                // and one for limit.                                     //  
                //--------------------------------------------------------//
                if (StopPrice > 0 && LimitPrice > 0)
                {
                    // limit order filled
                    if (Bars.High[0] >= StopPrice)
                    {
                        IsEntryOrderFilled = true;

                        // determine the entry price
                        EntryPrice = StopPrice;
                        ResetStopLimitEntry();
                        return;
                    }
                    else if (Bars.Low[0] <= LimitPrice)
                    {
                        IsEntryOrderFilled = true;

                        // determine the entry price
                        EntryPrice = LimitPrice;
                        ResetStopLimitEntry();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            // display stop price
                            ChartPoint stpPt = new ChartPoint(Bars.Time[0], StopPrice);
                            ITextObject stpTxt = DrwText.Create(stpPt, "-");
                            stpTxt.Color = Color.DarkGreen;

                            // display limit price
                            ChartPoint lmtPt = new ChartPoint(Bars.Time[0], LimitPrice);
                            ITextObject lmtTxt = DrwText.Create(lmtPt, "-");
                            lmtTxt.Color = Color.LimeGreen;

                            // plot text on entry line
                            if (EntryTxt == false)
                            {
                                // stop text
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], StopPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SP");
                                text.Color = Color.DarkGreen;

                                // limit text
                                ChartPoint pt1 = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], LimitPrice + (Displace * OnePip));
                                ITextObject text1 = DrwText.Create(pt1, "LP");
                                text1.Color = Color.LimeGreen;

                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) ResetTrade();
                }
            }
        }
        private void ShortEntry()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // short limit order filled
                    if (Bars.High[0] >= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        ResetEntry();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.Red;

                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.Red;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) ResetTrade();
                }

                // stop order entry
                if (StopPrice > 0 && LimitPrice <= 0)
                {
                    EntryPrice = StopPrice;

                    // short limit order filled
                    if (Bars.Low[0] <= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        ResetEntry();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.DarkRed;

                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "SE");
                                text.Color = Color.DarkRed;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            // this is equivalent to opening tick, but called only once
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) ResetTrade();
                }
            }
        }

        #endregion

        #region Exit Order Logic
        private void LongExit()
        {
            // for exit, this must be true, need to account for same bar exit
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // stop loss hit, exit all contracts
                if (price <= StopLossPrice)
                {
                    ResetTrade();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // profit 1 hit
                if (price >= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                ResetTrade();
                return;
            }
        }
        private void ShortExit()
        {
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.High[0];

                // stop loss hit, exit all contracts
                if (price >= StopLossPrice)
                {
                    ResetTrade();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 1 hit
                if (price <= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 2 hit
                if (price <= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                double price;

                // use current tick price for intra bar order generation
                if (IOGMode == IOGMode.Enabled)
                    price = Bars.Close[0];
                else
                    price = Bars.Low[0];

                // profit 3 hit
                if (price <= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                ResetTrade();
                return;
            }
        }

        #endregion

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }




        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return 0;

            return Math.Abs(EntryPrice - StopLossPrice);
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar), offset by number of pips
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low - (pips * OnePip);
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high + (pips * OnePip);
        }
        #endregion

        #region Set Properties

        public void SetProfitTarget(int target, int quantity, double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity);
        }
        public void SetProfitTarget(int target, int quantity, double riskReward, bool once)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity, once);
        }
        public void SetProfitPrice(int target, double price, int quantity)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    ProfitPrice1 = price;
                    ProfitQty1 = quantity;
                    break;

                // profit target 2
                case 2:
                    ProfitPrice2 = price;
                    ProfitQty2 = quantity;
                    break;

                // profit target 3
                case 3:
                    ProfitPrice3 = price;
                    ProfitQty3 = quantity;
                    break;

                default:
                    break;
            }
        }
        public void SetProfitPrice(int target, double price, int quantity, bool once)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    if (once && SetProfit1Once == false)
                    {
                        ProfitPrice1 = price;
                        ProfitQty1 = quantity;
                        SetProfit1Once = true;
                    }
                    break;

                // profit target 2
                case 2:
                    if (once && SetProfit2Once == false)
                    {
                        ProfitPrice2 = price;
                        ProfitQty2 = quantity;
                        SetProfit2Once = true;
                    }
                    break;

                // profit target 3
                case 3:
                    if (once && SetProfit3Once == false)
                    {
                        ProfitPrice3 = price;
                        ProfitQty3 = quantity;
                        SetProfit3Once = true;
                    }
                    break;

                default:
                    break;
            }
        }
        public void SetMktEntry(double entry, int contracts)
        {
            EntryPrice = entry;
            Contracts = contracts;
            IsEntryOrderFilled = true;
        }
        public void SetStopLoss(double stoploss, bool once)
        {
            // set stop loss price only once
            if (once && SetStopLossOnce == false)
            {
                StopLossPrice = stoploss;
                SetStopLossOnce = true;
            }
            else
            {
                StopLossPrice = stoploss;
            }
        }
        public void SetLmtEntry(double limitPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (SetEntryOnce == false)
            {
                LimitPrice = EntryPrice = limitPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                SetEntryOnce = true;
                IsEntryOrderFilled = false;
            }
        }
        public void SetStpEntry(double stopPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (SetEntryOnce == false)
            {
                StopPrice = EntryPrice = stopPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                SetEntryOnce = true;
                IsEntryOrderFilled = false;
            }
        }
        public void SetStpLmtEntry(double stopPrice, double limitPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (SetEntryOnce == false)
            {
                StopPrice = stopPrice;
                LimitPrice = limitPrice;
                Contracts = contracts;

                // time in force defaults to 999 bars
                TimeInForce = timeInForce;
                SetEntryOnce = true;
                IsEntryOrderFilled = false;
            }
        }
        #endregion

        #region Debug
        public void Debug(int start, int end)
        {
            int maxBars = ExecInfo.MaxBarsBack;
            start -= maxBars;
            end -= maxBars;

            if (Bars.CurrentBar >= start && Bars.CurrentBar <= end)
            {
                Print("Class Name: " + EntryName);
                Print("Chart Bar Number: " + (Bars.CurrentBar + maxBars));
                Print("Bar Status: " + Bars.Status);
                Print("Time in Force: " + TimeInForce);
                Print("Entry Order Filled: " + IsEntryOrderFilled);
                Print("Send Entry Order: " + SendEntry);
                Print("Entry Price: " + EntryPrice);
                Print("Stop Price: " + StopPrice);
                Print("Limit Price: " + LimitPrice);
                Print("Send Stop Loss: " + SendStopLoss);
                Print("Stop Loss Price: " + StopLossPrice);
                Print("Send Profit 1: " + SendProfit1);
                Print("Profit Price 1: " + ProfitPrice1);
                Print("Profit Qty 1: " + ProfitQty1);
                Print("Send Profit 2: " + SendProfit2);
                Print("Profit Price 2: " + ProfitPrice2);
                Print("Profit Qty 2: " + ProfitQty2);
                Print("Send Profit 3: " + SendProfit3);
                Print("Profit Price 3: " + ProfitPrice3);
                Print("Profit Qty 3: " + ProfitQty3);
                Print("Contracts: " + Contracts);
                Print("High: " + Bars.High[0]);
                Print("Low: " + Bars.Low[0]);
                Print("Open: " + Bars.Open[0]);
                Print("Close: " + Bars.Close[0]);
                Output.WriteLine("");
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

    #region Function - Trade Info v30
    public sealed class TradeInfo_v30 : FunctionSeries<int>
    {
        //=========================================================================================
        // Name           : Trade Info Function
        // Description    : Non-IOG strategies w Mkt Entry with Stop Loss and Max 3 Profit Targets
        // Version        : v3.0
        // Date Created   : 12 - Aug - 2020
        // Time Taken     : 
        // Remarks        : 
        //=========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //=========================================================================================

        /*/
         *----------------------------------------- Version ---------------------------------------
         * - OCO and OSO entry and exit orders
         * - StopLimit Orders, Limit Orders, MarketOrders
         * - Refactor TradeOrder
         * - Rename keep logic components
         *----------------------------------------- Version ---------------------------------------
           
         *------------------------------------------ Notes ----------------------------------------
         * - this is designed for single entry point with multi exits
         *------------------------------------------ Notes ----------------------------------------
        /*/

        // trade properties
        public string EntryName { get; private set; }
        public EMarketPositionSide TradeDirection { get; private set; }

        // entry and stop properties
        public double EntryPrice { get; set; }
        public double LimitPrice { get; set; }
        public double StopPrice { get; set; }
        public bool IsEntryOrderFilled { get; private set; }
        public bool SendEntry { get; private set; }
        public int TimeInForce { get; private set; }
        public int Contracts { get; set; }

        // stop loss properties
        public double StopLossPrice { get; set; }
        public bool SendStopLoss { get; private set; }

        // profit 1 properties
        public double ProfitPrice1 { get; set; }
        public int ProfitQty1 { get; set; }
        public bool SendProfit1 { get; private set; }


        // profit 2 properties
        public double ProfitPrice2 { get; set; }
        public int ProfitQty2 { get; set; }
        public bool SendProfit2 { get; private set; }

        // profit 3 properties
        public double ProfitPrice3 { get; set; }
        public int ProfitQty3 { get; set; }
        public bool SendProfit3 { get; private set; }

        // display properties
        public bool DisplayEntry { private get; set; }
        public bool DisplayStopLoss { private get; set; }
        public bool DisplayProfits { private get; set; }

        public TradeInfo_v30(CStudyControl m, EMarketPositionSide direction, string entryName)
            : base(m)
        {
            TradeDirection = direction;
            EntryName = entryName;
        }

        // fields - value types
        int Displace;
        double OnePip;
        bool EntryTxt, StopLossTxt, ProfitTxt1, ProfitTxt2, ProfitTxt3;
        bool SetEntryOnce, SetStopLossOnce, SetProfit1Once, SetProfit2Once, SetProfit3Once;

        private void ResetTrade()
        {
            // position reset method below fields for easier reference
            ResetEntry();
            ResetExit();
        }

        private void ResetEntry()
        {
            // reset entry data
            EntryPrice = LimitPrice = StopPrice = 0;

            // reset profit flags
            SetProfit1Once = SetProfit2Once = SetProfit3Once = false;

            // reset entry and stop loss flags
            SetEntryOnce = SetStopLossOnce = false;

            // entry text
            EntryTxt = false;

            // reset flag to trigger Send()
            SendEntry = false;

            // reset time in force, max 999 bars only
            TimeInForce = 0;
        }

        private void ResetExit()
        {
            // reset stoploss 
            StopLossPrice = 0;

            // reset profit prices
            ProfitPrice1 = ProfitPrice2 = ProfitPrice3 = 0;

            // reset entry order flag
            IsEntryOrderFilled = false;

            // reset stop and profits flags which triggers Send()
            SendStopLoss = SendProfit1 = SendProfit2 = SendProfit3 = false;

            // reset contracts and exit quantity
            Contracts = ProfitQty1 = ProfitQty2 = ProfitQty3 = 0;

            // reset txt flags that plot exit txt
            StopLossTxt = ProfitTxt1 = ProfitTxt2 = ProfitTxt3 = false;
        }

        protected override void StartCalc()
        {
            // one pip = 10 ticks
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // distance between text and price levels, 1 = 1 pip
            Displace = 1;

            // reset
            ResetTrade();
        }

        protected override int CalcBar()
        {
            switch (TradeDirection)
            {
                case EMarketPositionSide.Long:
                    LeOrderSendOrder();
                    LxOrderSendOrder();
                    break;

                case EMarketPositionSide.Short:
                    SxOrderSendOrder();
                    break;

                // Do nothing
                default:
                case EMarketPositionSide.Flat:
                    break;
            }

            return Contracts;
        }

        #region entry order logic
        DateTime CurrentTime;
        private void LeOrderSendOrder()
        {
            // entry order has not been filled
            if (IsEntryOrderFilled == false)
            {
                bool closingTick = Bars.Status == EBarState.Close;
                bool optimizing = Environment.Optimizing;
                DateTime time = Bars.Time[0];

                // limit order entry is true, and 
                if (LimitPrice > 0 && StopPrice <= 0)
                {
                    EntryPrice = LimitPrice;

                    // limit order filled
                    if (Bars.Low[0] <= EntryPrice)
                    {
                        IsEntryOrderFilled = true;
                        ResetEntry();
                        return;
                    }

                    // else, display the entry order line
                    else
                    {
                        SendEntry = true;

                        // display entry price on closing tick when not optimizing
                        if (DisplayEntry && closingTick && !optimizing)
                        {
                            ChartPoint p = new ChartPoint(Bars.Time[0], EntryPrice);
                            ITextObject txt = DrwText.Create(p, "-");
                            txt.Color = Color.LimeGreen;

                            if (EntryTxt == false)
                            {
                                ChartPoint pt = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], EntryPrice + (Displace * OnePip));
                                ITextObject text = DrwText.Create(pt, "LMT");
                                text.Color = Color.LimeGreen;
                                EntryTxt = true;
                            }
                        }

                        // time in force, once per bar, regardless how many time calcbar is called
                        if (time != CurrentTime)
                        {
                            CurrentTime = time;
                            TimeInForce--;
                        }
                    }

                    // time in force for limit order
                    if (TimeInForce == 1) ResetTrade();
                }
            }

            // debug from 
            Debug(10704, 10708);
        }
        #endregion

        #region exit order logic
        private void LxOrderSendOrder()
        {
            // for exit, this must be true
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                // stop loss hit, exit all contracts
                if (Bars.Low[0] <= StopLossPrice)
                {
                    ResetTrade();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                // profit 1 hit
                if (Bars.High[0] >= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                // profit 1 hit
                if (Bars.High[0] >= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                // profit 1 hit
                if (Bars.High[0] >= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                ResetTrade();
                return;
            }
        }
        private void SxOrderSendOrder()
        {
            if (IsEntryOrderFilled)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                // stop loss hit, exit all contracts
                if (Bars.High[0] >= StopLossPrice)
                {
                    ResetTrade();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                // profit 1 hit
                if (Bars.Low[0] <= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                // profit 1 hit
                if (Bars.Low[0] <= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                // profit 1 hit
                if (Bars.Low[0] <= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                ResetTrade();
                return;
            }
        }

        #endregion

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }




        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return 0;

            return Math.Abs(EntryPrice - StopLossPrice);
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar), offset by number of pips
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low - (pips * OnePip);
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high + (pips * OnePip);
        }
        #endregion

        #region Set Properties

        public void SetProfitTarget(int target, int quantity, double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity);
        }
        public void SetProfitTarget(int target, int quantity, double riskReward, bool once)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitPrice(target, profitPrice, quantity, once);
        }
        public void SetProfitPrice(int target, double price, int quantity)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    ProfitPrice1 = price;
                    ProfitQty1 = quantity;
                    break;

                // profit target 2
                case 2:
                    ProfitPrice2 = price;
                    ProfitQty2 = quantity;
                    break;

                // profit target 3
                case 3:
                    ProfitPrice3 = price;
                    ProfitQty3 = quantity;
                    break;

                default:
                    break;
            }
        }
        public void SetProfitPrice(int target, double price, int quantity, bool once)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    if (once && SetProfit1Once == false)
                    {
                        ProfitPrice1 = price;
                        ProfitQty1 = quantity;
                        SetProfit1Once = true;
                    }
                    break;

                // profit target 2
                case 2:
                    if (once && SetProfit2Once == false)
                    {
                        ProfitPrice2 = price;
                        ProfitQty2 = quantity;
                        SetProfit2Once = true;
                    }
                    break;

                // profit target 3
                case 3:
                    if (once && SetProfit3Once == false)
                    {
                        ProfitPrice3 = price;
                        ProfitQty3 = quantity;
                        SetProfit3Once = true;
                    }
                    break;

                default:
                    break;
            }
        }
        public void SetMktEntry(double entry, int contracts)
        {
            EntryPrice = entry;
            Contracts = contracts;
            IsEntryOrderFilled = true;
        }
        public void SetStopLoss(double stoploss, bool once)
        {
            // set stop loss price only once
            if (once && SetStopLossOnce == false)
            {
                StopLossPrice = stoploss;
                SetStopLossOnce = true;
            }
            else
            {
                StopLossPrice = stoploss;
            }
        }
        public void SetLmtEntry(double limitPrice, int contracts, int timeInForce = 999)
        {
            // do this only once
            if (SetEntryOnce == false)
            {
                LimitPrice = EntryPrice = limitPrice;
                Contracts = contracts;
                TimeInForce = timeInForce;
                SetEntryOnce = true;
                IsEntryOrderFilled = false;
            }
        }

        #endregion

        #region Debug
        public void Debug(int start, int end)
        {
            int maxBars = ExecInfo.MaxBarsBack;
            start -= maxBars;
            end -= maxBars;

            if (Bars.CurrentBar >= start && Bars.CurrentBar <= end)
            {
                Print("Chart Bar Number: " + (Bars.CurrentBar + maxBars));
                Print("Bar Status: " + Bars.Status);
                Print("Time in Force: " + TimeInForce);
                Print("Entry Order Filled: " + IsEntryOrderFilled);
                Print("Send Entry Order: " + SendEntry);
                Print("Entry Price: " + EntryPrice);
                Print("Stop Price: " + StopPrice);
                Print("Limit Price: " + LimitPrice);
                Print("Send Stop Loss: " + SendStopLoss);
                Print("Stop Loss Price: " + StopLossPrice);
                Print("Send Profit 1: " + SendProfit1);
                Print("Profit Price 1: " + ProfitPrice1);
                Print("Profit Qty 1: " + ProfitQty1);
                Print("Send Profit 2: " + SendProfit2);
                Print("Profit Price 2: " + ProfitPrice2);
                Print("Profit Qty 2: " + ProfitQty2);
                Print("Send Profit 3: " + SendProfit3);
                Print("Profit Price 3: " + ProfitPrice3);
                Print("Profit Qty 3: " + ProfitQty3);
                Print("Contracts: " + Contracts);
                Output.WriteLine("");
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

    #region Function - Trade Info v21
    public sealed class TradeInfo_v21 : FunctionSeries<int>
    {
        //=========================================================================================
        // Name           : Trade Info Function
        // Description    : Non / IOG strategies w Mkt Entry with Stop Loss, Max 3 Profit Targets
        // Version        : v2.1
        // Date Created   : 10 - Aug - 2020
        // Time Taken     : 
        // Remarks        : 
        //=========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //=========================================================================================

        /*/
         *----------------------------------------- Version ---------------------------------------
         *  - Contracts as return type
         *  - Purpose as a container to store trade information and display it on chart.
         *  - This is designed for market entry with Stop Loss and Max 3 Profit Targets
         *  - Use this as a template for OSO - order send order
         *  - this example is MarketOSO
         *----------------------------------------- Version ---------------------------------------
           
         *------------------------------------------ Notes ----------------------------------------
         *------------------------------------------ Notes ----------------------------------------
        /*/

        // trade properties
        public string EntryName { get; private set; }
        public EMarketPositionSide TradeDirection { get; private set; }

        // entry and stop properties
        public double EntryPrice { get; set; }
        public bool SendEntry { get; private set; }
        public int Contracts { get; set; }

        // stop loss properties
        public double StopLossPrice { get; set; }
        public bool SendStopLoss { get; private set; }

        // profit 1 properties
        public double ProfitPrice1 { get; set; }
        public int ProfitQty1 { get; set; }
        public bool SendProfit1 { get; private set; }


        // profit 2 properties
        public double ProfitPrice2 { get; set; }
        public int ProfitQty2 { get; set; }
        public bool SendProfit2 { get; private set; }

        // profit 3 properties
        public double ProfitPrice3 { get; set; }
        public int ProfitQty3 { get; set; }
        public bool SendProfit3 { get; private set; }

        // display properties
        public bool DisplayStopLoss { private get; set; }
        public bool DisplayProfits { private get; set; }

        public TradeInfo_v21(CStudyControl m, EMarketPositionSide direction, string entryName)
            : base(m)
        {
            TradeDirection = direction;
            EntryName = entryName;
        }

        // fields - value types
        int Displace;
        double OnePip;
        bool StopLossTxt, ProfitTxt1, ProfitTxt2, ProfitTxt3;
        protected override void StartCalc()
        {
            // one pip = 10 ticks
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // distance between text and price levels, 1 = 1 pip
            Displace = 1;

            // reset
            ResetTrade();
        }

        private void ResetTrade()
        {
            EntryPrice = StopLossPrice = 0;
            ProfitPrice1 = ProfitPrice2 = ProfitPrice3 = 0;
            SendEntry = SendStopLoss = false;
            SendProfit1 = SendProfit2 = SendProfit3 = false;
            Contracts = ProfitQty1 = ProfitQty2 = ProfitQty3 = 0;
            StopLossTxt = ProfitTxt1 = ProfitTxt2 = ProfitTxt3 = false;
        }

        protected override int CalcBar()
        {
            switch (TradeDirection)
            {
                case EMarketPositionSide.Long:
                    LongExit();
                    break;

                case EMarketPositionSide.Short:
                    ShortExit();
                    break;

                // Do nothing
                default:
                case EMarketPositionSide.Flat:
                    break;
            }

            return Contracts;
        }

        #region Function Logic
        private void LongExit()
        {
            if (SendEntry)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                // stop loss hit, exit all contracts
                if (Bars.Low[0] <= StopLossPrice)
                {
                    ResetTrade();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                // profit 1 hit
                if (Bars.High[0] >= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                // profit 1 hit
                if (Bars.High[0] >= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                // profit 1 hit
                if (Bars.High[0] >= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                ResetTrade();
                return;
            }
        }
        private void ShortExit()
        {
            if (SendEntry)
            {
                if (StopLossPrice > 0 && Contracts > 0) SendStopLoss = true;
                else SendStopLoss = false;

                if (ProfitQty1 + ProfitQty2 + ProfitQty3 <= Contracts)
                {
                    if (ProfitPrice1 > 0 && ProfitQty1 > 0) SendProfit1 = true;
                    else SendProfit1 = false;

                    if (ProfitPrice2 > 0 && ProfitQty2 > 0) SendProfit2 = true;
                    else SendProfit2 = false;

                    if (ProfitPrice3 > 0 && ProfitQty3 > 0) SendProfit3 = true;
                    else SendProfit3 = false;
                }
                else
                {
                    SendProfit1 = SendProfit2 = SendProfit3 = false;
                }
            }

            DateTime time = Bars.TimeValue;
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // stop loss in used
            if (SendStopLoss)
            {
                // stop loss hit, exit all contracts
                if (Bars.High[0] >= StopLossPrice)
                {
                    ResetTrade();
                    return;
                }
                else
                {
                    if (DisplayStopLoss && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, StopLossPrice);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Yellow;

                        // plot this only once
                        if (StopLossTxt == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], StopLossPrice + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "SL");
                            t.Color = Color.Yellow;
                            StopLossTxt = true;
                        }
                    }
                }
            }

            // profit 1 in used
            if (SendProfit1)
            {
                // profit 1 hit
                if (Bars.Low[0] <= ProfitPrice1 && ProfitQty1 <= Contracts)
                {
                    ProfitPrice1 = 0;
                    Contracts -= ProfitQty1;
                    ProfitQty1 = 0;
                    SendProfit1 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice1);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt1 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice1 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P1");
                            t.Color = Color.Cyan;
                            ProfitTxt1 = true;
                        }
                    }
                }
            }

            // profit 2 in used
            if (SendProfit2)
            {
                // profit 1 hit
                if (Bars.Low[0] <= ProfitPrice2 && ProfitQty2 <= Contracts)
                {
                    ProfitPrice2 = 0;
                    Contracts -= ProfitQty2;
                    ProfitQty2 = 0;
                    SendProfit2 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice2);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt2 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice2 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P2");
                            t.Color = Color.Cyan;
                            ProfitTxt2 = true;
                        }
                    }
                }
            }

            // profit 3 in used
            if (SendProfit3)
            {
                // profit 1 hit
                if (Bars.Low[0] <= ProfitPrice3 && ProfitQty3 <= Contracts)
                {
                    ProfitPrice3 = 0;
                    Contracts -= ProfitQty3;
                    ProfitQty3 = 0;
                    SendProfit3 = false;
                }
                else
                {
                    if (DisplayProfits && closingTick && optimization == false)
                    {
                        ChartPoint p = new ChartPoint(time, ProfitPrice3);
                        ITextObject txt = DrwText.Create(p, "-");
                        txt.Color = Color.Cyan;

                        // plot this only once
                        if (ProfitTxt3 == false)
                        {
                            ChartPoint x = new ChartPoint(
                                Bars.FullSymbolData.Time[-1], ProfitPrice3 + (OnePip * Displace));

                            ITextObject t = DrwText.Create(x, "P3");
                            t.Color = Color.Cyan;
                            ProfitTxt3 = true;
                        }
                    }
                }
            }

            // all profit target hit - test
            if (Contracts == 0)
            {
                ResetTrade();
                return;
            }
        }

        #endregion

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }

        /// <summary>
        /// Set profit price directly to a specific profit exit, based on risk reward
        /// </summary>
        /// <param name="riskReward"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public void CalcNSetProfitPrice(int target, int quantity, double riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (TradeDirection == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (TradeDirection == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            SetProfitTarget(target, profitPrice, quantity);
        }


        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return 0;

            return Math.Abs(EntryPrice - StopLossPrice);
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar), offset by number of pips
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low - (pips * OnePip);
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars, int pips = 0)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high + (pips * OnePip);
        }
        #endregion

        #region Set Properties
        /// <summary>
        /// Set profit price and exit quantity for each target manually
        /// </summary>
        /// <param name="price"></param>
        /// <param name="quantity"></param>
        /// <param name="target"></param>
        public void SetProfitTarget(int target, double price, int quantity)
        {
            switch (target)
            {
                // profit target 1
                case 1:
                    ProfitPrice1 = price;
                    ProfitQty1 = quantity;
                    break;

                // profit target 2
                case 2:
                    ProfitPrice2 = price;
                    ProfitQty2 = quantity;
                    break;

                // profit target 3
                case 3:
                    ProfitPrice3 = price;
                    ProfitQty3 = quantity;
                    break;

                default:
                    break;
            }
        }
        /// <summary>
        /// Set entry price, stop price and contracts
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="stoploss"></param>
        /// <param name="contracts"></param>
        public void SetEntryAndStopLoss(double entry, double stoploss, int contracts)
        {
            EntryPrice = entry;
            Contracts = contracts;
            StopLossPrice = stoploss;
            SendEntry = true;
        }
        #endregion

        #region Debug
        public void Debug()
        {
            Print("Send Entry: " + SendEntry);
            Print("Entry Price: " + EntryPrice);
            Print("Send Stop Loss: " + SendStopLoss);
            Print("Stop Loss Price: " + StopLossPrice);
            Print("Send Profit 1: " + SendProfit1);
            Print("Profit Price 1: " + ProfitPrice1);
            Print("Profit Qty 1: " + ProfitQty1);
            Print("Send Profit 2: " + SendProfit2);
            Print("Profit Price 2: " + ProfitPrice2);
            Print("Profit Qty 2: " + ProfitQty2);
            Print("Send Profit 3: " + SendProfit3);
            Print("Profit Price 3: " + ProfitPrice3);
            Print("Profit Qty 3: " + ProfitQty3);
            Print("Contracts: " + Contracts);
            Output.WriteLine("");
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

    #region Function - Trade Info v20
    public sealed class TradeInfo_v20 : FunctionSeries<int>
    {
        //=========================================================================================
        // Name           : Trade Info Function
        // Description    : Store entry and exit information in a class
        // Version        : v2.0
        // Date Created   : 10 - Aug - 2020
        // Time Taken     : 
        // Remarks        : 
        //=========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //=========================================================================================

        /*/
         *----------------------------------------- Version ---------------------------------------
         *  - Test if it works with IOG
         *  
         *----------------------------------------- Version ---------------------------------------
           
         *------------------------------------------ Notes ----------------------------------------
         *  - Design a strategy to have one entry, one stop and 3 profit targets
         *------------------------------------------ Notes ----------------------------------------
        /*/

        // entry and stop properties
        public double EntryPrice { get; set; }
        public bool SendEntry { get; set; }

        public int Contracts { get; set; }
        public double StopLossPrice { get; set; }
        public bool SendStopLoss { get; set; }

        // profit 1 properties
        public double ProfitPrice1 { get; set; }
        public int ProfitQty1 { get; set; }
        public bool SendProfit1 { get; set; }


        // profit 2 properties
        public double ProfitPrice2 { get; set; }
        public int ProfitQty2 { get; set; }
        public bool SendProfit2 { get; set; }

        public TradeInfo_v20(CStudyControl m) : base(m)
        {
        }

        // fields - value types
        protected override void StartCalc()
        {
        }

        protected override int CalcBar()
        {
            return Contracts;
        }

        #region Debug
        public void Debug()
        {
            Print("Send Entry: " + SendEntry);
            Print("Entry Price: " + EntryPrice);
            Print("Send Stop Loss: " + SendStopLoss);
            Print("Stop Loss Price: " + StopLossPrice);
            Print("Send Profit 1: " + SendProfit1);
            Print("Profit Price 1: " + ProfitPrice1);
            Print("Profit Qty 1: " + ProfitQty1);
            Print("Send Profit 2: " + SendProfit2);
            Print("Profit Price 2: " + ProfitPrice2);
            Print("Profit Qty 2: " + ProfitQty2);
            Print("Contracts: " + Contracts);
            Output.WriteLine("");
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

    #region Function - Trade Info v11
    public sealed class TradeInfo_v11 : FunctionSeries<int>
    {
        //==========================================================================================
        // Name           : Trade Info
        // Description    : Custom function for trade orders
        // Version        : v1.1
        // Date Created   : 09 - Aug - 2020
        // Time Taken     : 
        // Remarks        : Framework for tracking strategy orders
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  
         *  ----------------------------------- Version History -----------------------------------
         *  
         *  ------------------------------------ Project Notes ------------------------------------
         *  - Be used with IOG mode
         *  ------------------------------------ Project Notes ------------------------------------
         */

        // properties
        public bool Active { get; set; }
        public int Contracts { get; set; }
        public double EntryPrice { get; set; }
        public double StopLossPrice { get; set; }
        public double ProfitPrice1 { get; set; }
        public double ProfitPrice2 { get; set; }
        public int ProfitQty1 { get; set; }
        public int ProfitQty2 { get; set; }

        public EMarketPositionSide Direction { get; private set; }

        public TradeInfo_v11(CStudyControl m, EMarketPositionSide direction) : base(m)
        {
            Direction = direction;
        }

        // fields - value types
        protected override void StartCalc()
        {
        }
        protected override int CalcBar()
        {

            if (Bars.Status == EBarState.Close)
            {
                // display stop loss order, once at the end of bar
                if (StopLossPrice > 0)
                {
                    ChartPoint point = new ChartPoint(Bars.Time[0], StopLossPrice);
                    ITextObject text = DrwText.Create(point, "-");
                    text.Color = Color.Yellow;
                }

                if (ProfitPrice1 > 0)
                {
                    ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice1);
                    ITextObject text = DrwText.Create(point, "-");
                    text.Color = Color.Cyan;
                }

                if (ProfitPrice2 > 0)
                {
                    ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice2);
                    ITextObject text = DrwText.Create(point, "-");
                    text.Color = Color.Cyan;
                }
            }

            return 0;
        }

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(int riskReward)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (Direction == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (Direction == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }

        /// <summary>
        /// Set profit price directly to a specific profit exit
        /// </summary>
        /// <param name="riskReward"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public void CalcProfitPrice(int riskReward, int quantity, int target)
        {
            double risk, profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            risk = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (Direction == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (risk * riskReward);

            // short entry
            else if (Direction == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (risk * riskReward);

        }
        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return 0;

            return Math.Abs(EntryPrice - StopLossPrice);
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
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

    #region Function - Trade Info v10
    public sealed class TradeInfo_v10 : FunctionSeries<bool>
    {
        //==========================================================================================
        // Name           : Trade Info
        // Description    : Track strategy orders
        // Version        : v1.0
        // Date Created   : 04 - Aug - 2020
        // Time Taken     : 
        // Remarks        : Framework for tracking strategy orders
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  
         *  ----------------------------------- Version History -----------------------------------
         *  
         *  ------------------------------------ Project Notes ------------------------------------
         *  - Order information for single entry with a stop loss and 3 X profit target
         *  - Used with intraday order generation
         *  ------------------------------------ Project Notes ------------------------------------
         */

        // properties - order type
        public string EntryName { get; set; }
        /// <summary>
        /// Order direction - Either Long or Short
        /// </summary>
        public EMarketPositionSide Direction { get; set; }
        /// <summary>
        /// 1: Market, 2: Limit, 3: Stop
        /// </summary>
        public int OrderType { get; set; }

        // properties - price control using in built methods
        public double EntryPrice { get; private set; }
        public double StopLossPrice { get; set; }
        public double ProfitPrice1 { get; private set; }
        public double ProfitPrice2 { get; private set; }
        public double ProfitPrice3 { get; private set; }

        // properties - risk control
        public double RiskValue { get; set; }
        public int Contracts { get; private set; }


        // properties - trade control
        public bool Active { get; private set; }
        /// <summary>
        /// profit 1 active
        /// </summary>
        public bool P1 { get; private set; }
        /// <summary>
        /// profit 2 active
        /// </summary>
        public bool P2 { get; private set; }
        /// <summary>
        /// profit 3 active
        /// </summary>
        public bool P3 { get; private set; }
        /// <summary>
        /// stop loss active
        /// </summary>
        public bool StopActive { get; private set; }

        // properties - display controls
        /// <summary>
        /// If true displays the stop loss value on chart
        /// </summary>
        public bool ShowStopLoss { get; set; }
        /// <summary>
        /// If true displays the profit(s) value(s) on chart
        /// </summary>
        public bool ShowProfitTarget { get; set; }

        // constructor
        /// <summary>
        /// direction - long or short. 
        /// ordertype - 1: market, 2: limit, 3: stop
        /// </summary>
        /// <param name="m"></param>
        /// <param name="direction"></param>
        public TradeInfo_v10(CStudyControl m, EMarketPositionSide direction, int entryOrder) : base(m)
        {
            // initialize key context
            Direction = direction;
            OrderType = entryOrder;
        }

        // reference fields
        protected override void Create()
        {
        }

        // value fields
        /// <summary>
        /// One pip == 10 Ticks
        /// </summary>
        public double OnePip { get; private set; }
        bool Profit1Txt, Profit2Txt, Profit3Txt, StopLossTxt;
        int ProfitQty1, ProfitQty2, ProfitQty3;
        protected override void StartCalc()
        {
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // reset fields
            ClearData();
        }

        public void ClearData()
        {
            // reset all fields
            Active = false;
            Contracts = 0;
            RiskValue = StopLossPrice = 0;
            ProfitPrice1 = ProfitPrice2 = ProfitPrice3 = 0;
            ProfitQty1 = ProfitQty2 = ProfitQty3 = 0;
            Profit1Txt = Profit2Txt = Profit3Txt = StopLossTxt = false;
            StopActive = P1 = P2 = P3 = false;
        }

        protected override bool CalcBar()
        {
            // entry setup
            switch (Direction)
            {
                case EMarketPositionSide.Long:
                    LongEntry();
                    break;

                case EMarketPositionSide.Short:
                    ShortEntry();
                    break;

                default:
                    break;
            }

            // returns true if stop loss is still active
            switch (Direction)
            {
                // if direction is long
                case EMarketPositionSide.Long:
                    LongExit();
                    break;

                // if direction is short
                case EMarketPositionSide.Short:
                    ShortExit();
                    break;

                // no direction given
                default:
                    break;
            }

            return false;
        }

        #region Add data
        public void EntryInfo(double entry, int contracts)
        {
            // setup entry price and contracts only once
            if (Active) return;
            EntryPrice = entry;
            Contracts = contracts;
        }

        /// <summary>
        /// use stop info to fix stop loss from entry, 
        /// otherwise use property accessor to set
        /// </summary>
        /// <param name="stop"></param>
        public void StopInfo(double stop)
        {
            if (Active) return;
            StopLossPrice = stop;
        }
        /// <summary>
        /// Sets up the profit target. Target is reference to which Profit Target
        /// </summary>
        /// <param name="price"></param>
        /// <param name="contracts"></param>
        /// <param name="target"></param>
        public void ProfitInfo(double price, int quantity, int target)
        {
            // target has to be either 1,2,3
            switch (target)
            {
                case 1:
                    ProfitPrice1 = price;
                    ProfitQty1 = quantity;
                    break;

                case 2:
                    ProfitPrice2 = price;
                    ProfitQty2 = quantity;
                    break;

                case 3:
                    ProfitPrice3 = price;
                    ProfitQty3 = quantity;
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region Class Methods
        private void Print()
        {
            Print("Entry Price: " + EntryPrice);
            Print("Stop Loss: " + StopLossPrice);
            Print("Profit Price: " + ProfitPrice1);
            Print("Profit Price2: " + ProfitPrice2);
            Output.WriteLine("");
        }
        private void LongEntry()
        {
            // Exit method, and return to caller
            if (Active) return;

            double close = Bars.Close[0];

            switch (OrderType)
            {
                // market order
                case 1:
                    Active = true;
                    break;

                // limit order
                case 2:
                    Active = close < EntryPrice;
                    break;

                // stop order
                case 3:
                    Active = close >= EntryPrice;
                    break;

                default:
                    break;
            }
        }
        private void ShortEntry()
        {
            // Exit method, and return to caller
            if (Active == true) return;

            double close = Bars.Close[0];

            switch (OrderType)
            {
                // market order
                case 1:
                    Active = true;
                    break;

                // limit order
                case 2:
                    Active = close > EntryPrice;
                    break;

                // stop order
                case 3:
                    Active = close <= EntryPrice;
                    break;

                default:
                    break;
            }
        }
        private void LongExit()
        {
            // trade is active. 
            if (Active)
            {
                // plot text objects on closing tick
                bool closingTick = Bars.Status == EBarState.Close;

                // stop loss is used
                if (StopLossPrice > 0 && Contracts > 0)
                {
                    // check if stop loss is NOT hit
                    if (Bars.Low[0] > StopLossPrice)
                    {
                        StopActive = true;

                        // display stop loss order, once at the end of bar
                        if (ShowStopLoss && closingTick)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], StopLossPrice);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Yellow;

                            if (!StopLossTxt)
                            {
                                ChartPoint p = new ChartPoint(
                                   Bars.FullSymbolData.Time[-1], StopLossPrice + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "SL");
                                txt.Color = Color.Yellow;
                                StopLossTxt = true;
                            }
                        }
                    }

                    // if stop loss is used and hit
                    else ClearData();
                }

                // check if profit price 3 is used
                if (ProfitPrice3 > 0 && Contracts >= ProfitQty3)
                {
                    // check if profit price 3 is NOT hit
                    if (Bars.High[0] < ProfitPrice3)
                    {
                        P3 = true;

                        // only function series can use this[0]
                        if (ShowProfitTarget && closingTick)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice3);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Cyan;

                            if (!Profit3Txt)
                            {
                                ChartPoint p = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], ProfitPrice3 + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "P3");
                                txt.Color = Color.Cyan;
                                Profit3Txt = true;
                            }
                        }
                    }
                    // check if other profit prices are being used
                    else
                    {
                        // reduce the quantity from total contracts
                        Contracts -= ProfitQty3;

                        // switch off this profit exit
                        ProfitPrice3 = 0;

                        // profit 3 inactive
                        P3 = false;
                    }

                }

                // check if profit price 2 is in used
                if (ProfitPrice2 > 0 && Contracts >= ProfitQty2)
                {
                    if (Bars.High[0] < ProfitPrice2)
                    {
                        P2 = true;

                        // only function series can use this[0]
                        if (ShowProfitTarget && closingTick)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice2);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Cyan;

                            if (!Profit2Txt)
                            {
                                ChartPoint p = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], ProfitPrice2 + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "P2");
                                txt.Color = Color.Cyan;
                                Profit2Txt = true;
                            }
                        }
                    }
                    // check if other profit prices are being used
                    else
                    {
                        // reduce the quantity from total contracts
                        Contracts -= ProfitQty2;

                        // switch off this profit exit
                        ProfitPrice2 = 0;

                        // profit inactive
                        P2 = false;
                    }
                }

                // check if profit price is in used
                if (ProfitPrice1 > 0 && Contracts >= ProfitQty1)
                {
                    // profit price is still in play
                    if (Bars.High[0] < ProfitPrice1)
                    {
                        P1 = true;

                        // only function series can use this[0]
                        if (ShowProfitTarget && closingTick)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice1);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Cyan;

                            if (!Profit1Txt)
                            {
                                ChartPoint p = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], ProfitPrice1 + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "P1");
                                txt.Color = Color.Cyan;
                                Profit1Txt = true;
                            }
                        }
                    }
                    // check if other profit prices are being used
                    else
                    {
                        // reduce the quantity from total contracts
                        Contracts -= ProfitQty1;

                        // switch off this profit exit
                        ProfitPrice1 = 0;

                        // profit inactive
                        P1 = false;
                    }
                }

                // profit exit(s) all hit
                if (Contracts == 0) ClearData();
            }
        }
        private bool ShortExit()
        {
            // need to update later
            bool ret = false;

            // trade is active. 
            if (Active)
            {
                // stop loss is used
                if (StopLossPrice > 0)
                {
                    // check if stop loss is NOT hit
                    if (Bars.High[0] < StopLossPrice)
                    {
                        ret = true;

                        // display stop loss order
                        if (ShowStopLoss && this[0] == true)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], StopLossPrice);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Yellow;

                            if (!StopLossTxt)
                            {
                                ChartPoint p = new ChartPoint(
                                   Bars.FullSymbolData.Time[-1], StopLossPrice + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "SL");
                                txt.Color = Color.Yellow;
                                StopLossTxt = true;
                            }
                        }
                    }

                    // if stop loss is used and hit
                    else ClearData();
                }

                // check if profit price 3 is used
                if (ProfitPrice3 > 0)
                {
                    // check if profit price 3 is NOT hit
                    if (Bars.Low[0] > ProfitPrice3)
                    {
                        ret = true;

                        // only function series can use this[0]
                        if (ShowProfitTarget && this[0] == true)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice3);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Cyan;

                            if (!Profit3Txt)
                            {
                                ChartPoint p = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], ProfitPrice3 + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "P3");
                                txt.Color = Color.Cyan;
                                Profit3Txt = true;
                            }
                        }
                    }
                    // check if other profit prices are being used
                    else
                    {
                        // other profit prices in play
                        if (ProfitPrice1 > 0 || ProfitPrice2 > 0) ProfitPrice3 = 0;
                        else ClearData();
                    }
                }

                // check if profit price 2 is in used
                if (ProfitPrice2 > 0)
                {
                    if (Bars.Low[0] > ProfitPrice2)
                    {
                        ret = true;

                        // only function series can use this[0]
                        if (ShowProfitTarget && this[0] == true)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice2);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Cyan;

                            if (!Profit2Txt)
                            {
                                ChartPoint p = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], ProfitPrice2 + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "P2");
                                txt.Color = Color.Cyan;
                                Profit2Txt = true;
                            }
                        }
                    }
                    // check if other profit prices are being used
                    else
                    {
                        // other profit prices in play
                        if (ProfitPrice1 > 0 || ProfitPrice3 > 0) ProfitPrice2 = 0;
                        else ClearData();
                    }
                }

                // check if profit price is in used
                if (ProfitPrice1 > 0)
                {
                    // profit price is still in play
                    if (Bars.Low[0] > ProfitPrice1)
                    {
                        ret = true;

                        // only function series can use this[0]
                        if (ShowProfitTarget && this[0] == true)
                        {
                            ChartPoint point = new ChartPoint(Bars.Time[0], ProfitPrice1);
                            ITextObject text = DrwText.Create(point, "-");
                            text.Color = Color.Cyan;

                            if (!Profit1Txt)
                            {
                                ChartPoint p = new ChartPoint(
                                    Bars.FullSymbolData.Time[-1], ProfitPrice1 + (2 * OnePip));
                                ITextObject txt = DrwText.Create(p, "P1");
                                txt.Color = Color.Cyan;
                                Profit1Txt = true;
                            }
                        }
                    }
                    // check if other profit prices are being used
                    else
                    {
                        // other profit prices in play
                        if (ProfitPrice2 > 0 || ProfitPrice3 > 0) ProfitPrice1 = 0;
                        else ClearData();
                    }
                }
            }

            return ret;
        }

        #endregion

        #region Calculate
        /// <summary>
        /// Returns the profit price for the given risk reward, 
        /// requires EntryPrice > 0 and StopLossPrice > 0
        /// </summary>
        /// <param name="riskReward"></param>
        /// <returns></returns>
        public double CalcProfitPrice(int riskReward)
        {
            double profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return profitPrice;

            RiskValue = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (Direction == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (RiskValue * riskReward);

            // short entry
            else if (Direction == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (RiskValue * riskReward);

            // cannot cache profit(local) to ProfitPrice as there are 3 diff targets
            return profitPrice;
        }

        /// <summary>
        /// Set profit price directly to a specific profit exit
        /// </summary>
        /// <param name="riskReward"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public void CalcProfitPrice(int riskReward, int quantity, int target)
        {
            double profitPrice = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return;

            RiskValue = Math.Abs(EntryPrice - StopLossPrice);

            // long entry
            if (Direction == EMarketPositionSide.Long)
                profitPrice = EntryPrice + (RiskValue * riskReward);

            // short entry
            else if (Direction == EMarketPositionSide.Short)
                profitPrice = EntryPrice - (RiskValue * riskReward);

            if (Bars.Time[0] > new DateTime(2020, 05, 20) && Bars.Time[0] < new DateTime(2020, 05, 31))
            {
                Print("Risk: " + RiskValue);
                Print("profit price: " + profitPrice);
            }

            // cache directly into profit price
            ProfitInfo(profitPrice, quantity, target);
        }
        /// <summary>
        /// Returns the risk value, 
        /// requires EntryPrice > 0 and StopLossPrice > 0, otherwise risk = 0
        /// </summary>
        /// <returns></returns>
        public double CalcRiskValue()
        {
            double risk = 0;

            // both entry price and stop loss price are valid
            if (EntryPrice <= 0 && StopLossPrice <= 0) return risk;

            risk = Math.Abs(EntryPrice - StopLossPrice);
            RiskValue = risk;

            return risk;
        }
        /// <summary>
        /// Returns the lowest low for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcLowestLow(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
        }
        /// <summary>
        /// Returns the highest high for the given number of bars 
        /// (include current bar)
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        public double CalcHighestHigh(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
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

    #region Function - Price Pattern Strategy
    public sealed class PricePatternStrategy : FunctionSimple<bool>
    {
        // class properties. 
        /// <summary>
        /// Length of Pama
        /// </summary>
        public int _Length { get; set; }
        /// <summary>
        /// Standard Deviation Length of Pama
        /// </summary>
        public int _StdDevLength { get; set; }
        /// <summary>
        /// Max Bars Back for Pivot Calculations
        /// </summary>
        public int _PivotLength { get; set; }
        /// <summary>
        /// Smoothness required for Pama Calculations
        /// </summary>
        public int _Smooth { get; set; }
        /// <summary>
        /// 1: Close Price, 2: Range, 3: HLOC, 4: OC Avg
        /// </summary>
        public int _PriceType { get; set; }
        /// <summary>
        /// Total calculated timeframes
        /// </summary>
        public int _Timeframes { get { return Timeframes; } }

        // return values
        public VariableSeries<double> Pama1 { get; private set; }
        public VariableSeries<double> Pama2 { get; private set; }
        public VariableSeries<double> Pama3 { get; private set; }

        // return std dev
        public List<VariableSeries<double>> Pama_StdDev { get { return ListOfStdDevsValues; } }

        // return pama pivots
        public List<VariableSeries<double>> Pama_Pivots { get { return ListOfMaPivotsValues; } }

        public PricePatternStrategy(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //
        }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private List<StandardDeviationCalculatorManual> ListOfStdDevs;
        private List<MovingAveragePivotsManual> ListOfMaPivots;
        private List<VariableSeries<double>> ListOfStdDevsValues, ListOfMaPivotsValues;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfMaPivotsValues = new List<VariableSeries<double>>();
            ListOfStdDevsValues = new List<VariableSeries<double>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfStdDevs = new List<StandardDeviationCalculatorManual>();
            ListOfMaPivots = new List<MovingAveragePivotsManual>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));
                ListOfStdDevs.Add(new StandardDeviationCalculatorManual(this));
                ListOfMaPivots.Add(new MovingAveragePivotsManual(this));
            }

            // Create one extra plot
            // for each MA pivot there is an upper bound and lower bound value
            for (int i = 0; i <= 2 * Timeframes; i++)
                ListOfMaPivotsValues.Add(new VariableSeries<double>(this));

            // each std dev has two upper bound and two lower bound value
            for (int i = 0; i <= 4 * Timeframes; i++)
                ListOfStdDevsValues.Add(new VariableSeries<double>(this));
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;
            if (_StdDevLength < 1) _StdDevLength = 1;
            if (_PivotLength < 3) _PivotLength = 3;
            if (_PriceType <= 0) _PriceType = 1;

            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                ListOfHmas[i]._Length = _Length;
                ClosingTicks[i]._TimeFrame = i;
                ListOfStdDevs[i]._Length = _StdDevLength;
                ListOfStdDevs[i]._PopulationStdDev = true;
                ListOfMaPivots[i]._Length = _PivotLength;
            }

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = ClosingTicks[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                // calculate std dev values
                SetStdDevValues(timeframe);

                // set MA pivot values
                SetMovingAveragePivots(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetMovingAveragePivots(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // cache pama value
                double pamaValue = ListOfPamaValues[timeframe].Value;

                // add data
                ListOfMaPivots[timeframe].Add(pamaValue);
                ListOfMaPivots[timeframe].AddHigh(Highs[timeframe]);
                ListOfMaPivots[timeframe].AddLow(Lows[timeframe]);

                // cache data
                double pivotHigh = ListOfMaPivots[timeframe].PivotHighs;
                double pivotLow = ListOfMaPivots[timeframe].PivotLows;

                // assign data
                ListOfMaPivotsValues[(timeframe * 2) + 1].Value = pivotHigh;
                ListOfMaPivotsValues[(timeframe * 2) + 2].Value = pivotLow;
            }
        }
        private void SetStdDevValues(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass pama data 
                ListOfStdDevs[timeframe].Add(ListOfPamaValues[timeframe].Value);

                // cache 
                double stdDev = ListOfStdDevs[timeframe].Calculate();
                double pamaValue = ListOfPamaValues[timeframe].Value;

                // 1st std dev upper value
                ListOfStdDevsValues[(timeframe * 4) + 1].Value = pamaValue + (1 * stdDev);

                // 1st std dev lower value
                ListOfStdDevsValues[(timeframe * 4) + 2].Value = pamaValue - (1 * stdDev);

                // 2nd std dev upper value
                ListOfStdDevsValues[(timeframe * 4) + 3].Value = pamaValue + (2 * stdDev);

                // 2nd std dev lower value
                ListOfStdDevsValues[(timeframe * 4) + 4].Value = pamaValue - (2 * stdDev);
            }
        }
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe] = Closes[timeframe];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe]
                            = Highs[timeframe] - Lows[timeframe];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe]
                            = (Highs[timeframe]
                            + Lows[timeframe]
                            + Opens[timeframe]
                            + Closes[timeframe]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe]
                            = (Opens[timeframe]
                            + Closes[timeframe]) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // set by default to disable calculation
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            // set and assign function return values
            if (chartTF == 5 || chartTF == 15 || chartTF == 60)
            {
                switch (chartTF)
                {
                    // 5, 60, 240 min only
                    case 5:
                        IsTimeFrameSelected[0] = 1;
                        Pama1 = ListOfPamaValues[0];

                        IsTimeFrameSelected[5] = 1;
                        Pama2 = ListOfPamaValues[5];

                        IsTimeFrameSelected[7] = 1;
                        Pama3 = ListOfPamaValues[7];
                        break;

                    // 15, 60, 240 min only
                    case 15:
                        IsTimeFrameSelected[2] = 1;
                        Pama1 = ListOfPamaValues[2];

                        IsTimeFrameSelected[5] = 1;
                        Pama2 = ListOfPamaValues[5];

                        IsTimeFrameSelected[7] = 1;
                        Pama3 = ListOfPamaValues[7];
                        break;

                    // 60, 240 and daily only
                    case 60:
                        IsTimeFrameSelected[5] = 1;
                        Pama1 = ListOfPamaValues[5];

                        IsTimeFrameSelected[7] = 1;
                        Pama2 = ListOfPamaValues[7];

                        IsTimeFrameSelected[8] = 1;
                        Pama3 = ListOfPamaValues[8];
                        break;
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
