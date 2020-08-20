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
         *  1.0.1 - Planning, clean up old code
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *  
         *---------------------------------------- Versions ---------------------------------------
        /*/

        // strategy inputs
        public __SherNing_TradeInfoMulti (object _ctx) : base(_ctx)
        {
        }

        // reference fields 
        VariableSeries<int> MktPosition;
        VariableSeries<EMarketPositionSide> MktDirection;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LeLmt, SeLmt;
        IOrderPriced LxStp, SxStp;
        IOrderPriced LeStp, SeStp;
        IOrderPriced LePft1, LePft2, LePft3;
        IOrderPriced SePft1, SePft2, SePft3;

        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
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
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
          
        }
        protected override void CalcBar()
        {
            // keep track of market position
            MktPosition.Value = StrategyInfo.MarketPosition;
            MktDirection.Value = CurrentPosition.Side;

            // ensure enought data
            if (Bars.CurrentBar > EnoughData)
            {
                
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

    #endregion
}

namespace PowerLanguage.Function
{
    #region Function - Trade Info Multi v10
    public sealed class TradeInfoMulti : FunctionSimple<int>
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Trade Info Multi Entries
        // Description  : Framework multiple entries in the same direction
        // Type         : Function
        // Version      : v1.0
        // Date         : 20 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Multiple Entry TradeInfo Framework
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *--------------------------------------- Objectives --------------------------------------
         *  1.0.1 - Planning
         *--------------------------------------- Objectives --------------------------------------
        /*/

        #region Version History
        /*/
         *---------------------------------------- Versions ---------------------------------------
         *  
         *---------------------------------------- Versions ---------------------------------------
        /*/
        #endregion

        #endregion

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

        public TradeInfoMulti(CStudyControl m, EMarketPositionSide direction, string entryName)
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
        DateTime CurrentTime;

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

            //Debug(7073, 2, "SE_2");
            return MarketPosition[0];
        }

        #region Entry Order Logic
        public bool OCO(TradeInfoMulti other)
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

                    // stop order filled
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

                // market order entry is true
                if (StopPrice <= 0 && LimitPrice <= 0)
                {
                    if (EntryPrice > 0)
                        SendEntry = true;

                    // market order filled
                    if (MarketPosition[0] > 0)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }
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
                            DisplayPriceLevels(EntryPrice, Color.Red, "SE", ref PrintEntryTxtOnce);

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
                            DisplayPriceLevels(EntryPrice, Color.DarkRed, "SE", ref PrintEntryTxtOnce);


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

                // market order entry is true
                if (StopPrice <= 0 && LimitPrice <= 0)
                {
                    if (EntryPrice > 0)
                        SendEntry = true;

                    // market order filled
                    if (MarketPosition[0] < 0)
                    {
                        IsEntryOrderFilled = true;
                        CancelEntryOrder();
                        return;
                    }
                }
            }
        }

        #endregion

        #region Exit Order Logic
        private void LongExit()
        {
            // version 2.0
            // use market position to determine state of trade

            // local variables
            int mp = MarketPosition[0];
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;
            double profitPrice;

            // use current tick price for intra bar order generation
            if (Environment.IOGEnabled)
                profitPrice = Bars.Close[0];
            else
                profitPrice = Bars.High[0];

            if (IsEntryOrderFilled)
            {
                // stop loss in used
                if (StopLossPrice > 0)
                {
                    // conditions to send stop loss
                    if (mp > 0)
                    {
                        // send stop loss on next tick or bar
                        SendStopLoss = true;

                        // print stop loss
                        if (DisplayStopLoss && closingTick && optimization == false)
                            DisplayPriceLevels(StopLossPrice, Color.Yellow, "SL", ref PrintStopLossTxtOnce);
                    }

                    // stop loss hit
                    else
                    {
                        CancelExitOrder();
                        return;
                    }
                }

                // profit 1 in used
                if (ProfitPrice1 > 0)
                {
                    if (profitPrice < ProfitPrice1)
                    {
                        SendProfit1 = true;

                        if (DisplayProfits && closingTick && optimization == false)
                            DisplayPriceLevels(ProfitPrice1, Color.Cyan, "P1", ref PrintProfit1TxtOnce);
                    }

                    else if (ProfitQty1 == (Contracts - mp))
                    {
                        // reset profit prices
                        ProfitPrice1 = ProfitQty1 = 0;

                        // reset stop and profits flags which triggers Send()
                        SendProfit1 = false;

                        // reset txt flags that plot exit txt
                        PrintProfit1TxtOnce = false;

                        Contracts = mp;
                    }
                }

                // profit 2 in used
                if (ProfitPrice2 > 0)
                {
                    if (profitPrice < ProfitPrice2)
                    {
                        SendProfit2 = true;

                        if (DisplayProfits && closingTick && optimization == false)
                            DisplayPriceLevels(ProfitPrice2, Color.Cyan, "P2", ref PrintProfit2TxtOnce);
                    }

                    else if (ProfitQty2 == (Contracts - mp))
                    {
                        // reset profit prices
                        ProfitPrice2 = ProfitQty2 = 0;

                        // reset stop and profits flags which triggers Send()
                        SendProfit2 = false;

                        // reset txt flags that plot exit txt
                        PrintProfit2TxtOnce = false;

                        Contracts = mp;
                    }
                }

                // profit 3 in used
                if (ProfitPrice3 > 0)
                {
                    // profit 3 hit
                    if (profitPrice < ProfitPrice3)
                    {
                        SendProfit3 = true;

                        if (DisplayProfits && closingTick && optimization == false)
                            DisplayPriceLevels(ProfitPrice3, Color.Cyan, "P3", ref PrintProfit3TxtOnce);
                    }

                    else if (ProfitQty3 == (Contracts - mp))
                    {
                        // reset profit prices
                        ProfitPrice3 = ProfitQty3 = 0;

                        // reset stop and profits flags which triggers Send()
                        SendProfit3 = false;

                        // reset txt flags that plot exit txt
                        PrintProfit3TxtOnce = false;

                        Contracts = mp;
                    }
                }

                // all profits targets hit
                if (mp == 0
                    && SendProfit1 == false
                    && SendProfit2 == false
                    && SendProfit3 == false)
                {
                    CancelExitOrder();
                    return;
                }
            }
        }
        private void ShortExit()
        {
            // version 2.0
            // use market position to determine state of trade

            // local variables
            int mp = MarketPosition[0];
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;
            double profitPrice;

            // use current tick price for intra bar order generation
            if (Environment.IOGEnabled)
                profitPrice = Bars.Close[0];
            else
                profitPrice = Bars.Low[0];

            if (IsEntryOrderFilled)
            {
                // stop loss in used
                if (StopLossPrice > 0)
                {
                    // conditions to send stop loss
                    if (mp < 0)
                    {
                        // send stop loss on next tick or bar
                        SendStopLoss = true;

                        // print stop loss
                        if (DisplayStopLoss && closingTick && optimization == false)
                            DisplayPriceLevels(StopLossPrice, Color.Yellow, "SL", ref PrintStopLossTxtOnce);
                    }

                    // stop loss hit
                    else
                    {
                        CancelExitOrder();
                        return;
                    }
                }

                // profit 1 in used
                if (ProfitPrice1 > 0)
                {
                    if (profitPrice > ProfitPrice1)
                    {
                        SendProfit1 = true;

                        if (DisplayProfits && closingTick && optimization == false)
                            DisplayPriceLevels(ProfitPrice1, Color.Cyan, "P1", ref PrintProfit1TxtOnce);
                    }

                    else if (ProfitQty1 == Math.Abs(Contracts + mp))
                    {
                        // reset profit prices
                        ProfitPrice1 = ProfitQty1 = 0;

                        // reset stop and profits flags which triggers Send()
                        SendProfit1 = false;

                        // reset txt flags that plot exit txt
                        PrintProfit1TxtOnce = false;

                        Contracts = Math.Abs(mp);
                    }
                }

                // profit 2 in used
                if (ProfitPrice2 > 0)
                {
                    if (profitPrice > ProfitPrice2)
                    {
                        SendProfit2 = true;

                        if (DisplayProfits && closingTick && optimization == false)
                            DisplayPriceLevels(ProfitPrice2, Color.Cyan, "P2", ref PrintProfit2TxtOnce);
                    }

                    else if (ProfitQty2 == Math.Abs(Contracts + mp))
                    {
                        // reset profit prices
                        ProfitPrice2 = ProfitQty2 = 0;

                        // reset stop and profits flags which triggers Send()
                        SendProfit2 = false;

                        // reset txt flags that plot exit txt
                        PrintProfit2TxtOnce = false;

                        Contracts = Math.Abs(mp);
                    }
                }

                // profit 3 in used
                if (ProfitPrice3 > 0)
                {
                    // profit 3 hit
                    if (profitPrice > ProfitPrice3)
                    {
                        SendProfit3 = true;

                        if (DisplayProfits && closingTick && optimization == false)
                            DisplayPriceLevels(ProfitPrice3, Color.Cyan, "P3", ref PrintProfit3TxtOnce);
                    }

                    else if (ProfitQty3 == Math.Abs(Contracts + mp))
                    {
                        // reset profit prices
                        ProfitPrice3 = ProfitQty3 = 0;

                        // reset stop and profits flags which triggers Send()
                        SendProfit3 = false;

                        // reset txt flags that plot exit txt
                        PrintProfit3TxtOnce = false;

                        Contracts = Math.Abs(mp);
                    }
                }

                // all profits targets hit
                if (mp == 0
                    && SendProfit1 == false
                    && SendProfit2 == false
                    && SendProfit3 == false)
                {
                    CancelExitOrder();
                    return;
                }
            }
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
        public void SetMarketOrder(double entry, int contracts)
        {
            EntryPrice = entry;
            Contracts = contracts;
            IsEntryOrderFilled = false;
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
        public void SetLimitOrder(double limitPrice, int contracts, int timeInForce = 999)
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
        public void SetStopMarketOrder(double stopPrice, int contracts, int timeInForce = 999)
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
                Print("Chart Bar Number: " + (Bars.CurrentBar + maxBars));
                Debug();
            }
        }
        public void Debug(int start, int bars, string name)
        {
            int maxBars = ExecInfo.MaxBarsBack;
            start -= maxBars;

            if (Bars.CurrentBar >= start
                && Bars.CurrentBar <= start + bars
                && EntryName == name)
            {
                Print("Chart Bar Number: " + (Bars.CurrentBar + maxBars));
                Debug();
            }
        }
        public void Debug()
        {
            // entry info
            Print("Class Name: " + EntryName);
            Print("Bar Status: " + Bars.Status);
            Print("Time in Force: " + TimeInForce);
            Print("Entry Order Filled: " + IsEntryOrderFilled);
            Print("Send Entry Order: " + SendEntry);
            Print("Entry Price: " + EntryPrice);
            Print("Stop Price: " + StopPrice);
            Print("Limit Price: " + LimitPrice);
            Output.WriteLine("");

            // exit info
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
            Output.WriteLine("");

            // postion info
            Print("Contracts: " + Contracts);
            Print("Market Position: " + MarketPosition[0]);
            Print("IOG Enabled: " + Environment.IOGEnabled);
            Output.WriteLine("");

            // bar info
            Print("High: " + Bars.High[0]);
            Print("Low: " + Bars.Low[0]);
            Print("Open: " + Bars.Open[0]);
            Print("Close: " + Bars.Close[0]);
            Output.WriteLine("");
            Output.WriteLine("");
        }
        private void DisplayPriceLevels(double price, Color color, string text, ref bool printOnce)
        {
            DateTime time = Bars.Time[0];

            // check if fields are reference types, otherwise pass by ref
            ChartPoint p = new ChartPoint(time, price);
            ITextObject txt = DrwText.Create(p, "-");
            txt.Color = color;

            // plot this only once
            if (printOnce == false)
            {
                ChartPoint x = new ChartPoint(
                    Bars.FullSymbolData.Time[-1], price + (OnePip * TxtDisplace));

                ITextObject t = DrwText.Create(x, text);
                t.Color = color;
                printOnce = true;
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

        private void Print(string message, int chartbarNum)
        {
            int maxbars = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar == chartbarNum - maxbars)
                Print(message);
        }
        #endregion
    }

    #endregion
}
