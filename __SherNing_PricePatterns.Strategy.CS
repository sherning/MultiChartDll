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
    public class __SherNing_PricePatterns : SignalObject
    {
        //=========================================================================================
        // Project Name : Price Patterns
        // Description  : Test Framework for Order Execution
        // Type         : Strategy
        // Version      : v3.2
        // Date         : 17 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Template for OrderInfo Class
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *--------------------------------------- Objectives --------------------------------------
         *  3.2.1 - Clean up Code
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *  3.1.1 - Test Limit Entries with Exits - Successful
         *  3.1.2 - Test Market Entries with Exits - Succssful
         *  3.1.3 - Test Stop Entries with Exits - Successful
         *  3.1.4 - Test Stop Limit Entries using 2 class objects - done
         *  3.1.5 - Test Stop Limit order with additional 2 profit exit orders - done
         *  3.1.6 - Test Stop Limit Short order - successful
         *  3.1.7 - Test Stop Stop Entries
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
        IOrderPriced LePft1, LePft2, LePft3;
        IOrderPriced SePft1, SePft2, SePft3;

        TradeInfoSingle LeEntry1, LeEntry2, SeEntry1, SeEntry2;
        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);

            // Trade Info Class
            LeEntry1 = new TradeInfoSingle(this, EMarketPositionSide.Long, "LE_1");
            LeEntry2 = new TradeInfoSingle(this, EMarketPositionSide.Long, "LE_2");
            SeEntry1 = new TradeInfoSingle(this, EMarketPositionSide.Short, "SE_1");
            SeEntry2 = new TradeInfoSingle(this, EMarketPositionSide.Short, "SE_2");

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
                SeEntry1.DisplayEntry = SeEntry1.DisplayStopLoss = SeEntry1.DisplayProfits = true;
                SeEntry2.DisplayEntry = SeEntry2.DisplayStopLoss = SeEntry2.DisplayProfits = true;
            }
            else
            {
                LeEntry1.DisplayEntry = LeEntry1.DisplayProfits = LeEntry1.DisplayStopLoss = false;
                LeEntry2.DisplayEntry = LeEntry2.DisplayProfits = LeEntry2.DisplayStopLoss = false;
                SeEntry1.DisplayEntry = SeEntry1.DisplayStopLoss = SeEntry1.DisplayProfits = false;
                SeEntry2.DisplayEntry = SeEntry2.DisplayStopLoss = SeEntry2.DisplayProfits = false;
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
                //SendStpLmtEntry();
                //SendStpStpEntry();
                //SendLmtEntry();
                TestFramework1();
                SendExitOrders();
            }
        }
        #region Test Framework
        private void TestFramework1()
        {
            // successful
            if (Bars.Status != EBarState.Close) return;

            // long entry condition are valid
            if (Bars.CurrentBar % 101 == 0
                && LeEntry1.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = close - (15 * OnePip);

                // set stop loss, separate from entry order, sendentryorder() for entry info
                LeEntry1.SetMarketOrder(close, 100000);
                LeEntry1.SetStopLoss(stop - OnePip, false);
                LeEntry1.SetProfitTarget(1, 30000, 1);
                LeEntry1.SetProfitTarget(2, 40000, 1.5);
                LeEntry1.SetProfitTarget(3, 30000, 2);
            }

            LeEntry1.Call();

            if (LeEntry1.SendEntry)
                LeMkt.Send(LeEntry1.Contracts);

            if (Bars.CurrentBar % 151 == 0
                && SeEntry1.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = close + (15 * OnePip);

                SeEntry1.SetMarketOrder(close, 100000);
                SeEntry1.SetStopLoss(stop + OnePip, false);
                SeEntry1.SetProfitTarget(1, 30000, 1);
                SeEntry1.SetProfitTarget(2, 40000, 1.5);
                SeEntry1.SetProfitTarget(3, 30000, 2);
            }

            SeEntry1.Call();

            if (SeEntry1.SendEntry)
                SeMkt.Send(SeEntry1.Contracts);
        }
        #endregion

        #region Send Order Command
        DateTime CurrentTime;
        private void SendStpEntry()
        {
            // successful
            double entryPrice = Bars.Close[0] + (OnePip * 10);
            double stopPrice = Bars.Close[0] - (OnePip * 10);

            // long conditions
            if (LeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false)
            {
                LeEntry1.SetStopMarketOrder(entryPrice, 100000, 100);
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
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false)
            {
                SeEntry1.SetStopMarketOrder(entryPrice, 100000, 100);
                SeEntry1.SetStopLoss(stopPrice + OnePip, true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            SeEntry1.Call();

            if (SeEntry1.SendEntry) SeStp.Send(SeEntry1.StopPrice, SeEntry1.Contracts);
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
                SeEntry1.IsEntryOrderFilled == false &&
                SeEntry1.SendEntry == false)
            {
                // setup entry and exit properties
                LeEntry1.SetLimitOrder(lePrice, 100000, 100);
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
                !SeEntry1.SendEntry &&
                !SeEntry1.IsEntryOrderFilled &&
                !LeEntry1.IsEntryOrderFilled &&
                !LeEntry1.SendEntry)
            {
                // setup entry and exit properties
                SeEntry1.SetLimitOrder(sePrice, 100000, 100);
                SeEntry1.SetStopLoss(seStop + OnePip, true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            SeEntry1.Call();

            if (SeEntry1.SendEntry) SeLmt.Send(SeEntry1.LimitPrice, SeEntry1.Contracts);
        }
        private void SendMktEntry()
        {
            // successful
            // send entry orders on closing tick
            if (Bars.Status != EBarState.Close) return;

            // long entry condition are valid
            if (LeConditions() && LeEntry1.IsEntryOrderFilled == false && SeEntry1.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcLowestLow(4);

                // set stop loss, separate from entry order, sendentryorder() for entry info
                LeEntry1.SetMarketOrder(close, 100000);
                LeEntry1.SetStopLoss(stop - OnePip, false);
                LeEntry1.SetProfitTarget(1, 30000, 1);
                LeEntry1.SetProfitTarget(2, 40000, 1.5);
                LeEntry1.SetProfitTarget(3, 30000, 2);

                // send mkt always returns true, or change to SendEntryOrder()
            }

            LeEntry1.Call();

            if (LeEntry1.SendEntry)
                LeMkt.Send(LeEntry1.Contracts);

            if (SeConditions() && SeEntry1.IsEntryOrderFilled == false && LeEntry1.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcHighestHigh(4);

                SeEntry1.SetMarketOrder(close, 100000);
                SeEntry1.SetStopLoss(stop + OnePip, false);
                SeEntry1.SetProfitTarget(1, 30000, 1);
                SeEntry1.SetProfitTarget(2, 40000, 1.5);
                SeEntry1.SetProfitTarget(3, 30000, 2);
            }

            SeEntry1.Call();

            if (SeEntry1.SendEntry)
                SeMkt.Send(SeEntry1.Contracts);
        }
        private void SendStpStpEntry()
        {
            // normal strategy framework just use SendEntry()

            // long entry conditions
            if (LeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                )
            {
                // entry 1 - short stop order
                double stopPrice1 = Bars.Low[0] - (10 * OnePip);
                SeEntry1.SetStopMarketOrder(stopPrice1, 100000, 100);
                SeEntry1.SetStopLoss(stopPrice1 + (10 * OnePip), true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice2 = Bars.High[0] + (10 * OnePip);
                LeEntry1.SetStopMarketOrder(stopPrice2, 100000, 100);
                LeEntry1.SetStopLoss(stopPrice2 - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            // call function for calculations
            LeEntry1.Call();
            SeEntry1.Call();

            // order cancel order
            LeEntry1.OCO(SeEntry1);

            // if both true then send orders
            if (LeEntry1.SendEntry && SeEntry1.SendEntry)
            {
                SeStp.Send(SeEntry1.StopPrice, SeEntry1.Contracts);
                LeStp.Send(LeEntry1.StopPrice, LeEntry1.Contracts);
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
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                && SeEntry2.SendEntry == false
                && SeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - limit order
                double limitPrice = Bars.Low[0] - (10 * OnePip);
                LeEntry1.SetLimitOrder(limitPrice, 100000, 100);
                LeEntry1.SetStopLoss(limitPrice - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.High[0] + (10 * OnePip);
                LeEntry2.SetStopMarketOrder(stopPrice, 100000, 100);
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

            // short entry conditions
            if (SeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && LeEntry2.SendEntry == false
                && LeEntry2.IsEntryOrderFilled == false
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                && SeEntry2.SendEntry == false
                && SeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - limit order
                double limitPrice = Bars.High[0] + (10 * OnePip);
                SeEntry1.SetLimitOrder(limitPrice, 100000, 100);
                SeEntry1.SetStopLoss(limitPrice + (10 * OnePip), true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.Low[0] - (10 * OnePip);
                SeEntry2.SetStopMarketOrder(stopPrice, 100000, 100);
                SeEntry2.SetStopLoss(stopPrice + (10 * OnePip), true);
                SeEntry2.SetProfitTarget(1, 30000, 1, true);
                SeEntry2.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry2.SetProfitTarget(3, 30000, 2, true);
            }

            // call function for calculations
            SeEntry1.Call();
            SeEntry2.Call();

            // order cancel order
            SeEntry1.OCO(SeEntry2);

            // if both true then send orders
            if (SeEntry1.SendEntry && SeEntry2.SendEntry)
            {
                SeLmt.Send(SeEntry1.LimitPrice, SeEntry1.Contracts);
                SeStp.Send(SeEntry2.StopPrice, SeEntry2.Contracts);
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

            if (SeEntry1.SendStopLoss)
            {
                SxStp.Send(SeEntry1.StopLossPrice);
            }

            if (SeEntry1.SendProfit1)
            {
                SePft1.Send(SeEntry1.ProfitPrice1, SeEntry1.ProfitQty1);
            }

            if (SeEntry1.SendProfit2)
            {
                SePft2.Send(SeEntry1.ProfitPrice2, SeEntry1.ProfitQty2);
            }

            if (SeEntry1.SendProfit3)
            {
                SePft3.Send(SeEntry1.ProfitPrice3, SeEntry1.ProfitQty3);
            }

            if (SeEntry2.SendStopLoss)
            {
                SxStp.Send(SeEntry2.StopLossPrice);
            }

            if (SeEntry2.SendProfit1)
            {
                SePft1.Send(SeEntry2.ProfitPrice1, SeEntry2.ProfitQty1);
            }

            if (SeEntry2.SendProfit2)
            {
                SePft2.Send(SeEntry2.ProfitPrice2, SeEntry2.ProfitQty2);
            }

            if (SeEntry2.SendProfit3)
            {
                SePft3.Send(SeEntry2.ProfitPrice3, SeEntry2.ProfitQty3);
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

    #region Strategy - Price Pattern v31
    public class __SherNing_PricePatterns_v31 : SignalObject
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
         *  3.1.4 - Test Stop Limit Entries using 2 class objects - done
         *  3.1.5 - Test Stop Limit order with additional 2 profit exit orders - done
         *  3.1.6 - Test Stop Limit Short order - successful
         *  3.1.7 - Test Stop Stop Entries
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *---------------------------------------- Versions ---------------------------------------
        /*/

        // strategy inputs
        [Input] public bool ShowPriceLevels { get; set; }
        public __SherNing_PricePatterns_v31(object _ctx) : base(_ctx)
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

        TradeInfoSingle LeEntry1, LeEntry2, SeEntry1, SeEntry2;
        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);

            // Trade Info Class
            LeEntry1 = new TradeInfoSingle(this, EMarketPositionSide.Long, "LE_1");
            LeEntry2 = new TradeInfoSingle(this, EMarketPositionSide.Long, "LE_2");
            SeEntry1 = new TradeInfoSingle(this, EMarketPositionSide.Short, "SE_1");
            SeEntry2 = new TradeInfoSingle(this, EMarketPositionSide.Short, "SE_2");

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
                SeEntry1.DisplayEntry = SeEntry1.DisplayStopLoss = SeEntry1.DisplayProfits = true;
                SeEntry2.DisplayEntry = SeEntry2.DisplayStopLoss = SeEntry2.DisplayProfits = true;
            }
            else
            {
                LeEntry1.DisplayEntry = LeEntry1.DisplayProfits = LeEntry1.DisplayStopLoss = false;
                LeEntry2.DisplayEntry = LeEntry2.DisplayProfits = LeEntry2.DisplayStopLoss = false;
                SeEntry1.DisplayEntry = SeEntry1.DisplayStopLoss = SeEntry1.DisplayProfits = false;
                SeEntry2.DisplayEntry = SeEntry2.DisplayStopLoss = SeEntry2.DisplayProfits = false;
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
                SendMktEntry();
                //SendStpLmtEntry();
                //SendStpStpEntry();
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
        private void SendStpStpEntry()
        {
            // normal strategy framework just use SendEntry()

            // long entry conditions
            if (LeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                //&& LeEntry2.SendEntry == false
                //&& LeEntry2.IsEntryOrderFilled == false
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                //&& SeEntry2.SendEntry == false
                //&& SeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - short stop order
                double stopPrice1 = Bars.Low[0] - (10 * OnePip);
                SeEntry1.SetStopMarketOrder(stopPrice1, 100000, 100);
                SeEntry1.SetStopLoss(stopPrice1 + (10 * OnePip), true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice2 = Bars.High[0] + (10 * OnePip);
                LeEntry1.SetStopMarketOrder(stopPrice2, 100000, 100);
                LeEntry1.SetStopLoss(stopPrice2 - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            // call function for calculations
            LeEntry1.Call();
            SeEntry1.Call();

            // order cancel order
            LeEntry1.OCO(SeEntry1);

            // if both true then send orders
            if (LeEntry1.SendEntry && SeEntry1.SendEntry)
            {
                SeStp.Send(SeEntry1.StopPrice, SeEntry1.Contracts);
                LeStp.Send(LeEntry1.StopPrice, LeEntry1.Contracts);
            }

            //// short entry conditions
            //if (SeConditions()
            //    && LeEntry1.SendEntry == false
            //    && LeEntry1.IsEntryOrderFilled == false
            //    && LeEntry2.SendEntry == false
            //    && LeEntry2.IsEntryOrderFilled == false
            //    && SeEntry1.SendEntry == false
            //    && SeEntry1.IsEntryOrderFilled == false
            //    && SeEntry2.SendEntry == false
            //    && SeEntry2.IsEntryOrderFilled == false
            //    )
            //{
            //    // entry 1 - limit order
            //    double limitPrice = Bars.High[0] + (10 * OnePip);
            //    SeEntry1.SetLmtEntry(limitPrice, 100000, 100);
            //    SeEntry1.SetStopLoss(limitPrice + (10 * OnePip), true);
            //    SeEntry1.SetProfitTarget(1, 30000, 1, true);
            //    SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
            //    SeEntry1.SetProfitTarget(3, 30000, 2, true);

            //    // entry 2 - stop order
            //    double stopPrice = Bars.Low[0] - (10 * OnePip);
            //    SeEntry2.SetStpEntry(stopPrice, 100000, 100);
            //    SeEntry2.SetStopLoss(stopPrice + (10 * OnePip), true);
            //    SeEntry2.SetProfitTarget(1, 30000, 1, true);
            //    SeEntry2.SetProfitTarget(2, 40000, 1.5, true);
            //    SeEntry2.SetProfitTarget(3, 30000, 2, true);
            //}

            //// call function for calculations
            //SeEntry1.Call();
            //SeEntry2.Call();

            //// order cancel order
            //SeEntry1.OCO(SeEntry2);

            //// if both true then send orders
            //if (SeEntry1.SendEntry && SeEntry2.SendEntry)
            //{
            //    SeLmt.Send(SeEntry1.LimitPrice, SeEntry1.Contracts);
            //    SeStp.Send(SeEntry2.StopPrice, SeEntry2.Contracts);
            //}
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
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                && SeEntry2.SendEntry == false
                && SeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - limit order
                double limitPrice = Bars.Low[0] - (10 * OnePip);
                LeEntry1.SetLimitOrder(limitPrice, 100000, 100);
                LeEntry1.SetStopLoss(limitPrice - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.High[0] + (10 * OnePip);
                LeEntry2.SetStopMarketOrder(stopPrice, 100000, 100);
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

            // short entry conditions
            if (SeConditions()
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false
                && LeEntry2.SendEntry == false
                && LeEntry2.IsEntryOrderFilled == false
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                && SeEntry2.SendEntry == false
                && SeEntry2.IsEntryOrderFilled == false
                )
            {
                // entry 1 - limit order
                double limitPrice = Bars.High[0] + (10 * OnePip);
                SeEntry1.SetLimitOrder(limitPrice, 100000, 100);
                SeEntry1.SetStopLoss(limitPrice + (10 * OnePip), true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.Low[0] - (10 * OnePip);
                SeEntry2.SetStopMarketOrder(stopPrice, 100000, 100);
                SeEntry2.SetStopLoss(stopPrice + (10 * OnePip), true);
                SeEntry2.SetProfitTarget(1, 30000, 1, true);
                SeEntry2.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry2.SetProfitTarget(3, 30000, 2, true);
            }

            // call function for calculations
            SeEntry1.Call();
            SeEntry2.Call();

            // order cancel order
            SeEntry1.OCO(SeEntry2);

            // if both true then send orders
            if (SeEntry1.SendEntry && SeEntry2.SendEntry)
            {
                SeLmt.Send(SeEntry1.LimitPrice, SeEntry1.Contracts);
                SeStp.Send(SeEntry2.StopPrice, SeEntry2.Contracts);
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
                LeEntry1.SetLimitOrder(limitPrice, 100000, 100);
                LeEntry1.SetStopLoss(limitPrice - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.High[0] + (10 * OnePip);
                LeEntry2.SetStopMarketOrder(stopPrice, 100000, 100);
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
                LeEntry1.SetLimitOrder(limitPrice, 100000, 100);
                LeEntry1.SetStopLoss(limitPrice - (10 * OnePip), true);
                LeEntry1.SetProfitTarget(1, 30000, 1, true);
                LeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                LeEntry1.SetProfitTarget(3, 30000, 2, true);

                // entry 2 - stop order
                double stopPrice = Bars.Close[0] + (10 * OnePip);
                LeEntry2.SetStopMarketOrder(stopPrice, 100000, 100);
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

            if (SeEntry1.SendStopLoss)
            {
                SxStp.Send(SeEntry1.StopLossPrice);
            }

            if (SeEntry1.SendProfit1)
            {
                SePft1.Send(SeEntry1.ProfitPrice1, SeEntry1.ProfitQty1);
            }

            if (SeEntry1.SendProfit2)
            {
                SePft2.Send(SeEntry1.ProfitPrice2, SeEntry1.ProfitQty2);
            }

            if (SeEntry1.SendProfit3)
            {
                SePft3.Send(SeEntry1.ProfitPrice3, SeEntry1.ProfitQty3);
            }

            if (SeEntry2.SendStopLoss)
            {
                SxStp.Send(SeEntry2.StopLossPrice);
            }

            if (SeEntry2.SendProfit1)
            {
                SePft1.Send(SeEntry2.ProfitPrice1, SeEntry2.ProfitQty1);
            }

            if (SeEntry2.SendProfit2)
            {
                SePft2.Send(SeEntry2.ProfitPrice2, SeEntry2.ProfitQty2);
            }

            if (SeEntry2.SendProfit3)
            {
                SePft3.Send(SeEntry2.ProfitPrice3, SeEntry2.ProfitQty3);
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
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false)
            {
                LeEntry1.SetStopMarketOrder(entryPrice, 100000, 100);
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
                && SeEntry1.SendEntry == false
                && SeEntry1.IsEntryOrderFilled == false
                && LeEntry1.SendEntry == false
                && LeEntry1.IsEntryOrderFilled == false)
            {
                SeEntry1.SetStopMarketOrder(entryPrice, 100000, 100);
                SeEntry1.SetStopLoss(stopPrice + OnePip, true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            SeEntry1.Call();

            if (SeEntry1.SendEntry) SeStp.Send(SeEntry1.StopPrice, SeEntry1.Contracts);
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
                SeEntry1.IsEntryOrderFilled == false &&
                SeEntry1.SendEntry == false)
            {
                // setup entry and exit properties
                LeEntry1.SetLimitOrder(lePrice, 100000, 100);
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
                !SeEntry1.SendEntry &&
                !SeEntry1.IsEntryOrderFilled &&
                !LeEntry1.IsEntryOrderFilled &&
                !LeEntry1.SendEntry)
            {
                // setup entry and exit properties
                SeEntry1.SetLimitOrder(sePrice, 100000, 100);
                SeEntry1.SetStopLoss(seStop + OnePip, true);
                SeEntry1.SetProfitTarget(1, 30000, 1, true);
                SeEntry1.SetProfitTarget(2, 40000, 1.5, true);
                SeEntry1.SetProfitTarget(3, 30000, 2, true);
            }

            SeEntry1.Call();

            if (SeEntry1.SendEntry) SeLmt.Send(SeEntry1.LimitPrice, SeEntry1.Contracts);
        }
        private void SendMktEntry()
        {
            // successful
            // send entry orders on closing tick
            if (Bars.Status != EBarState.Close) return;

            // long entry condition are valid
            if (LeConditions() && LeEntry1.IsEntryOrderFilled == false && SeEntry1.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcLowestLow(4);

                // set stop loss, separate from entry order, sendentryorder() for entry info
                LeEntry1.SetMarketOrder(close, 100000);
                LeEntry1.SetStopLoss(stop - OnePip, false);
                LeEntry1.SetProfitTarget(1, 30000, 1);
                LeEntry1.SetProfitTarget(2, 40000, 1.5);
                LeEntry1.SetProfitTarget(3, 30000, 2);

                // send mkt always returns true, or change to SendEntryOrder()
            }

            LeEntry1.Call();

            if (LeEntry1.SendEntry)
                LeMkt.Send(LeEntry1.Contracts);

            if (SeConditions() && SeEntry1.IsEntryOrderFilled == false && LeEntry1.IsEntryOrderFilled == false)
            {
                double close = Bars.Close[0];
                double stop = CalcHighestHigh(4);

                SeEntry1.SetMarketOrder(close, 100000);
                SeEntry1.SetStopLoss(stop + OnePip, false);
                SeEntry1.SetProfitTarget(1, 30000, 1);
                SeEntry1.SetProfitTarget(2, 40000, 1.5);
                SeEntry1.SetProfitTarget(3, 30000, 2);
            }

            SeEntry1.Call();

            if (SeEntry1.SendEntry)
                SeMkt.Send(SeEntry1.Contracts);
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
