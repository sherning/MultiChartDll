using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    #region Strategy - Price Pattern v20

    [IOGMode(IOGMode.Disabled)]
    public class __SherNing_PricePatterns : SignalObject
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

        public __SherNing_PricePatterns(object _ctx) : base(_ctx)
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

        TradeInfo LeEntry;
        VariableSeries<int> CurrentBars;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);
            LeEntry = new TradeInfo(this);
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

    #region Function - Trade Info v20
    public sealed class TradeInfo : FunctionSeries<int>
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

        public TradeInfo(CStudyControl m) : base(m)
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
