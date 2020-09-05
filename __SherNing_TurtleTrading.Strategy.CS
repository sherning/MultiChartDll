using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    public class __SherNing_TurtleTrading : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Turtle Trading System
        // Requirements : 1. Scale-in 3 Entries (NextBar Mkt Entries)
        //                2. Trailing Exit - Donchian 10 
        //                3. Stop Loss - 2 X ATR
        //                4. Enchance Version. Not original
        //
        // Type         : Strategy (IOG.Disabled)
        // Version      : v1.1
        // Date         : 04 - Sep - 2020
        // Time Taken   : 
        // Remarks      : Requirements, Design, Development, Optimization, Maintenance
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      :
        // Client       :
        // Contact      : 
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.1.1 - Complete Stp Short Side
         *        
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *  
         *  1.0.0 - Requirements
         *  1.0.1 - Design
         *        - MaxBarBacks must be set in strategy properties manually
         *        - Add and test various components required to build Turtle Strategy
         *        - Develop code in the most efficient and effective manners
         *        - Functions are tools in my arsenal, I do not necessarily need to use them
         *        - Good programming is about code reusability
         *  1.0.2 - Develop LE Mkt and LX Stp entries
         *  1.0.3 - Develop LE Stp entries
         *        - IOG.Enabled is used if ENTRY requires intra bar calculation
         *        - Track price and market position
         *        - Test with 200 MA
         *  1.0.4 - Test StpLimit, Stp is the trigger, limit is the entry type      
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Properties
        [Input] public int FastChannelLength { get; set; }
        [Input] public int SlowChannelLength { get; set; }
        [Input] public int AtrLength { get; set; }
        [Input] public int MaxNumOfEntries { get; set; }
        public __SherNing_TurtleTrading(object _ctx) : base(_ctx)
        {
            FastChannelLength = 10;
            SlowChannelLength = 20;
            AtrLength = 14;
            MaxNumOfEntries = 3;
        }
        #endregion

        #region References
        VariableSeries<int> MarketPosition;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LxStp, SxStp;
        IOrderPriced LeStp, SeStp;
        IOrderStopLimit LeStpLmt, SeStpLmt;
        protected override void Create()
        {
            MarketPosition = new VariableSeries<int>(this);

            // strategy orders
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE", EOrderAction.Buy));

            // default is from all
            LxStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LX", EOrderAction.Sell, OrderExit.FromAll));

            // by default order exit from all
            SxStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "SX", EOrderAction.BuyToCover));

            // using stop entries
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LE_Stp", EOrderAction.Buy));

            SeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "SE_Stp", EOrderAction.SellShort));

            // using stop limit orders, stop will be known as the trigger price
            // essentially stop limit is a limit order with trigger price
            LeStpLmt = OrderCreator.StopLimit(new SOrderParameters(
                Contracts.UserSpecified, "LE_StpLmt", EOrderAction.Buy));

            SeStpLmt = OrderCreator.StopLimit(new SOrderParameters(
                Contracts.UserSpecified, "SE_StpLmt", EOrderAction.SellShort));

            // clear during debug
            Output.Clear();
        }
        #endregion

        #region Fields
        int NumOfEntries;
        double RiskValue;
        double PrevLEntryPrice, LEntryPrice, StopLossPrice;
        double PrevSEntryPrice, SEntryPrice;
        bool PrintStopLossText, PrintEntryText;
        double OnePip;
        protected override void StartCalc()
        {
            // assign inputs 
            NumOfEntries = 0;
            RiskValue = StopLossPrice = 0;
            PrevLEntryPrice = 0;
            PrintEntryText = PrintStopLossText = false;

            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
        }
        private void PrintState()
        {
            // framework for print strategy state
            Print("---------------------- Start ----------------------");

            // standard strategy info
            Print("Bar Number: " + (Bars.CurrentBar + ExecInfo.MaxBarsBack));

            // maxbarsback + 1 is when calcbar starts calculation. 
            // if not enough data to reference, an exception will be thrown
            // cannot set maxbarsback programmatically in a strategy
            Print("MaxBarsBack: " + ExecInfo.MaxBarsBack);
            Print("Bar Status: " + Bars.Status);
            Output.WriteLine("");

            // bar info
            Print("High: " + Bars.High[0]);
            Print("Low: " + Bars.Low[0]);
            Print("Open: " + Bars.Open[0]);
            Print("Close: " + Bars.Close[0]);
            Output.WriteLine("");

            // strategy state
            Print("Market Position: " + MarketPosition[0]);
            Print("Num Of Entries: " + NumOfEntries);
            Print("Risk Value: " + RiskValue);
            Print("Prev Entry Price: " + PrevLEntryPrice);
            Print("StopLoss Price: " + StopLossPrice);


            Print("----------------------- End -----------------------");
            Output.WriteLine("");
            Output.WriteLine("");
        }
        private void PrintState(int chartBarNum)
        {
            if (Bars.CurrentBar + ExecInfo.MaxBarsBack == chartBarNum)
                PrintState();
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            // track market position at the start of calculations
            MarketPosition.Value = StrategyInfo.MarketPosition;

            // send long entry
            //if (SendLeMkt()) LeMkt.Send(100000);

            // send stop loss 
            //if (SendLxStp()) LxStp.Send(StopLossPrice, MarketPosition[0]);

            //if (SendLEntryStp()) LeStp.Send(LEntryPrice, 100000);
            if (SendLEntryStp()) LeStpLmt.Send(LEntryPrice, LEntryPrice - (5 * OnePip), 100000);

            if (SendLExitStopLoss()) LxStp.Send(StopLossPrice, MarketPosition[0]);

            //if (SendSEntryStp()) SeStp.Send(SEntryPrice, 100000);
            if (SendSEntryStp()) SeStpLmt.Send(SEntryPrice, SEntryPrice + (5 * OnePip), 100000);

            if (SendSExitStopLoss()) SxStp.Send(StopLossPrice, Math.Abs(MarketPosition[0]));

            PrintState(540);

            PlotStrategy();
        }
        #endregion

        #region Private Methods
        private bool SendLeMkt()
        {
            double channel = HighestHigh(SlowChannelLength);

            // max 3 entries, max contracts set with strategy properties
            bool maxEntries = NumOfEntries < MaxNumOfEntries;

            // if it is the previous entry was flat or short
            bool firstEntry = MarketPosition[1] <= 0 && MarketPosition[0] == 0;

            // entry info
            double close = Bars.Close[0];

            // if end of bar close is above or touching the channel
            if (close >= channel && maxEntries)
            {
                // check if first entry
                if (firstEntry)
                {
                    // initial stop loss value
                    RiskValue = ATR(AtrLength) * 2;
                    StopLossPrice = close - RiskValue;
                    PrevLEntryPrice = close;
                    NumOfEntries++;

                    return true;
                }

                // second and third entry, one risk value away
                if (MarketPosition[0] > 0 && close >= (PrevLEntryPrice + RiskValue))
                {
                    PrevLEntryPrice = close;
                    NumOfEntries++;
                    return true;
                }
            }

            return false;
        }
        private bool SendLEntryStp()
        {
            int mp = MarketPosition[0];

            // if negative return false
            if (mp < 0) return false;

            double channel = HighestHigh(FastChannelLength);
            bool firstEntry = MarketPosition[1] <= 0 && mp == 0;
            bool firstEntryFilled = MarketPosition[1] <= 0 && mp > 0;
            bool newEntry = mp > 0 && mp - MarketPosition[1] > 0;
            bool maCondition = Bars.Low[0] >= MovingAverage(200);

            // first entry price = channel high
            if (firstEntry && maCondition)
            {
                LEntryPrice = channel;
                //PlotStrategyOrderOnChart("LE 1", EntryPrice, Color.DarkGreen, ref PrintEntryText);
                return true;
            }

            // check when the first entry is filled
            if (firstEntryFilled)
            {
                RiskValue = ATR(AtrLength) * 2;
                StopLossPrice = LEntryPrice - RiskValue;
                PrevLEntryPrice = LEntryPrice;
                NumOfEntries++;
            }

            // second entries onwards
            if (NumOfEntries < MaxNumOfEntries && mp > 0 && MarketPosition[1] > 0)
            {
                // new entry detected
                if (newEntry)
                {
                    // update new price
                    PrevLEntryPrice = LEntryPrice;
                    PrintEntryText = false;
                    NumOfEntries++;
                }

                LEntryPrice = PrevLEntryPrice + RiskValue;

                if (NumOfEntries < MaxNumOfEntries && maCondition)
                {
                    // plot order lines on chart
                    PlotStrategyOrderOnChart
                        ("LE " + (NumOfEntries + 1), LEntryPrice, Color.DarkGreen, ref PrintEntryText);
                    return true;
                }
                else
                {
                    return false;
                }

            }

            return false;
        }
        private bool SendSEntryStp()
        {
            int mp = MarketPosition[0];

            // if negative return false
            if (mp > 0) return false;

            double channel = LowestLow(FastChannelLength);
            bool firstEntry = MarketPosition[1] >= 0 && mp == 0;
            bool firstEntryFilled = MarketPosition[1] >= 0 && mp < 0;
            bool newEntry = mp < 0 && mp - MarketPosition[1] < 0;
            bool maCondition = Bars.High[0] <= MovingAverage(200);

            // first entry price = channel high
            if (firstEntry && maCondition)
            {
                SEntryPrice = channel;
                return true;
            }

            // check when the first entry is filled
            if (firstEntryFilled)
            {
                RiskValue = ATR(AtrLength) * 2;
                StopLossPrice = SEntryPrice + RiskValue;
                PrevSEntryPrice = SEntryPrice;
                NumOfEntries++;
            }

            // second entries onwards
            if (NumOfEntries < MaxNumOfEntries && mp < 0 && MarketPosition[1] < 0)
            {
                // new entry detected
                if (newEntry)
                {
                    // update new price
                    PrevSEntryPrice = SEntryPrice;
                    PrintEntryText = false;
                    NumOfEntries++;
                }

                SEntryPrice = PrevSEntryPrice - RiskValue;

                if (NumOfEntries < MaxNumOfEntries && maCondition)
                {
                    // plot order lines on chart
                    PlotStrategyOrderOnChart
                        ("SE " + (NumOfEntries + 1), SEntryPrice, Color.DarkRed, ref PrintEntryText);
                    return true;
                }
                else
                {
                    return false;
                }

            }

            return false;
        }

        private bool SendLExitStopLoss()
        {
            // reset after long exit
            if (MarketPosition[1] > 0 && MarketPosition[0] <= 0)
            {
                // reset data and return false
                NumOfEntries = 0;
                RiskValue = StopLossPrice = 0;
                PrintEntryText = PrintStopLossText = false;
                LEntryPrice = PrevLEntryPrice = 0;

                return false;
            }

            if (StopLossPrice > 0 && MarketPosition[0] > 0)
            {
                double channel = LowestLow(FastChannelLength);

                // if close moves into 1 unit of profits, non-retracing
                StopLossPrice = Math.Max(StopLossPrice, channel);

                // print stop loss line
                PlotStrategyOrderOnChart("SL", StopLossPrice, Color.Yellow, ref PrintStopLossText);

                return true;
            }

            return false;
        }
        private bool SendSExitStopLoss()
        {
            // reset after long exit
            if (MarketPosition[1] < 0 && MarketPosition[0] >= 0)
            {
                // reset data and return false
                NumOfEntries = 0;
                RiskValue = StopLossPrice = 0;
                PrintEntryText = PrintStopLossText = false;
                SEntryPrice = PrevSEntryPrice = 0;

                return false;
            }

            if (StopLossPrice > 0 && MarketPosition[0] < 0)
            {
                double channel = HighestHigh(FastChannelLength);

                // if close moves into 1 unit of profits, non-retracing
                StopLossPrice = Math.Min(StopLossPrice, channel);

                // print stop loss line
                PlotStrategyOrderOnChart("SL", StopLossPrice, Color.Yellow, ref PrintStopLossText);

                return true;
            }

            return false;
        }

        private bool SendLxStp()
        {
            // reset after long exit
            if (MarketPosition[1] > 0 && MarketPosition[0] <= 0)
            {
                // reset data and return false
                NumOfEntries = 0;
                RiskValue = StopLossPrice = 0;
                PrintStopLossText = false;

                return false;
            }

            if (StopLossPrice > 0 && MarketPosition[0] > 0)
            {
                double channel = LowestLow(FastChannelLength);

                // if close moves into 1 unit of profits, non-retracing
                StopLossPrice = Math.Max(StopLossPrice, channel);

                // print stop loss line
                PlotStrategyOrderOnChart("SL", StopLossPrice, Color.Yellow, ref PrintStopLossText);

                return true;
            }

            return false;
        }
        private double MovingAverage(int length)
        {
            // include current close
            double sum = 0;
            for (int i = 0; i < length; i++)
                sum += Bars.Close[i];

            return sum / length;
        }
        private double ATR(int length)
        {
            // deafult ATR calculations includes current bar
            double sum = 0;

            // i is bars back
            for (int i = 0; i < length; i++)
                sum += Bars.Range(i);

            return sum / length;
        }
        private double HighestHigh(int length)
        {
            double ret = 0;

            // exclude current high
            for (int i = 1; i < length; i++)
                ret = Math.Max(ret, Bars.High[i]);

            return ret;
        }
        private double LowestLow(int length)
        {
            double ret = double.MaxValue;

            // exclude current high
            for (int i = 1; i < length; i++)
                ret = Math.Min(ret, Bars.Low[i]);

            return ret;
        }
        #endregion

        #region Plots
        private void PlotStrategyOrderOnChart(string text, double price, Color color, ref bool Once)
        {
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // plot on closing tick, and avoid drwing during optimization
            if (closingTick == false || optimization) return;

            ChartPoint point = new ChartPoint(Bars.Time[0], price);
            ITextObject order = DrwText.Create(point, "-");
            order.Color = color;

            double onePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // print the stop loss text only once
            if (Once == false)
            {
                Once = true;
                point.Price = price + (2 * onePip);

                ITextObject txt = DrwText.Create(point, text);
                txt.Color = color;
            }
        }
        private void PlotStrategy()
        {
            if (Environment.Optimizing) return;

            // good way to test if internal function is working visually
            StrategyInfo.SetPlotValue(1, HighestHigh(SlowChannelLength));
            StrategyInfo.SetPlotValue(11, HighestHigh(FastChannelLength));
            StrategyInfo.SetPlotValue(2, LowestLow(SlowChannelLength));
            StrategyInfo.SetPlotValue(12, LowestLow(FastChannelLength));
            StrategyInfo.SetPlotValue(3, MovingAverage(200));
            StrategyInfo.SetPlotValue(10, ATR(AtrLength));
        }
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

    #region TurtleTrading_v10
    public class __SherNing_TurtleTrading_v10 : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Turtle Trading System
        // Requirements : 1. Scale-in 3 Entries (NextBar Mkt Entries)
        //                2. Trailing Exit - Donchian 10 
        //                3. Stop Loss - 2 X ATR
        //                4. Enchance Version. Not original
        //
        // Type         : Strategy (IOG.Disabled)
        // Version      : v1.0
        // Date         : 03 - Sep - 2020
        // Time Taken   : 
        // Remarks      : Requirements, Design, Development, Optimization, Maintenance
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      :
        // Client       :
        // Contact      : 
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.0 - Requirements
         *  1.0.1 - Design
         *        - MaxBarBacks must be set in strategy properties
         *        - Add and test various components required to build Turtle Strategy
         *        - Develop code in the most efficient and effective manners
         *        - Functions are tools in my arsenal, I do not necessarily need to use them
         *        - Good programming is about code reusability
         *  1.0.2 - Develop LE Mkt and LX Stp entries
         *  1.0.3 - Develop LE Stp entries
         *        - IOG.Enabled is used if ENTRY requires intra bar calculation
         *        - IOG.Enabled is used if you need to track intrabar MP
         *        - Test with 200 MA
         *        
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Properties
        [Input] public int FastChannelLength { get; set; }
        [Input] public int SlowChannelLength { get; set; }
        [Input] public int AtrLength { get; set; }
        [Input] public int MaxNumOfEntries { get; set; }
        public __SherNing_TurtleTrading_v10(object _ctx) : base(_ctx)
        {
            FastChannelLength = 10;
            SlowChannelLength = 20;
            AtrLength = 14;
            MaxNumOfEntries = 3;
        }
        #endregion

        #region References
        VariableSeries<int> MarketPosition;
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LxStp, SxStp;
        IOrderPriced LeStp, SeStp;
        protected override void Create()
        {
            MarketPosition = new VariableSeries<int>(this);

            // strategy orders
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE", EOrderAction.Buy));

            // default is from all
            LxStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LX", EOrderAction.Sell, OrderExit.FromAll));


            // using stop entries
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LE_Stp", EOrderAction.Buy));

            // clear during debug
            Output.Clear();
        }
        #endregion

        #region Fields
        int NumOfEntries;
        double RiskValue;
        double PrevEntryPrice, EntryPrice, StopLossPrice;
        bool PrintStopLossText, PrintEntryText;
        protected override void StartCalc()
        {
            // assign inputs 
            NumOfEntries = 0;
            RiskValue = StopLossPrice = 0;
            PrevEntryPrice = 0;
            PrintEntryText = PrintStopLossText = false;
        }
        private void PrintState()
        {
            // framework for print strategy state
            Print("---------------------- Start ----------------------");

            // standard strategy info
            Print("Bar Number: " + (Bars.CurrentBar + ExecInfo.MaxBarsBack));
            Print("Bar Status: " + Bars.Status);
            Output.WriteLine("");

            // bar info
            Print("High: " + Bars.High[0]);
            Print("Low: " + Bars.Low[0]);
            Print("Open: " + Bars.Open[0]);
            Print("Close: " + Bars.Close[0]);
            Output.WriteLine("");

            // strategy state
            Print("Market Position: " + MarketPosition[0]);
            Print("Num Of Entries: " + NumOfEntries);
            Print("Risk Value: " + RiskValue);
            Print("Prev Entry Price: " + PrevEntryPrice);
            Print("StopLoss Price: " + StopLossPrice);


            Print("----------------------- End -----------------------");
            Output.WriteLine("");
            Output.WriteLine("");
        }
        private void PrintState(int chartBarNum)
        {
            if (Bars.CurrentBar + ExecInfo.MaxBarsBack == chartBarNum)
                PrintState();
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            // track market position at the start of calculations
            MarketPosition.Value = StrategyInfo.MarketPosition;

            // send long entry
            //if (SendLeMkt()) LeMkt.Send(100000);

            // send stop loss 
            //if (SendLxStp()) LxStp.Send(StopLossPrice, MarketPosition[0]);

            if (SendLeStp()) LeStp.Send(EntryPrice, 100000);

            if (SendLxStpForLeStp()) LxStp.Send(StopLossPrice, MarketPosition[0]);

            PrintState(540);

            PlotStrategy();
        }
        #endregion

        #region Private Methods
        private bool SendLeMkt()
        {
            double channel = HighestHigh(SlowChannelLength);

            // max 3 entries, max contracts set with strategy properties
            bool maxEntries = NumOfEntries < MaxNumOfEntries;

            // if it is the previous entry was flat or short
            bool firstEntry = MarketPosition[1] <= 0 && MarketPosition[0] == 0;

            // entry info
            double close = Bars.Close[0];

            // if end of bar close is above or touching the channel
            if (close >= channel && maxEntries)
            {
                // check if first entry
                if (firstEntry)
                {
                    // initial stop loss value
                    RiskValue = ATR(AtrLength) * 2;
                    StopLossPrice = close - RiskValue;
                    PrevEntryPrice = close;
                    NumOfEntries++;

                    return true;
                }

                // second and third entry, one risk value away
                if (MarketPosition[0] > 0 && close >= (PrevEntryPrice + RiskValue))
                {
                    PrevEntryPrice = close;
                    NumOfEntries++;
                    return true;
                }
            }

            return false;
        }
        private bool SendLeStp()
        {
            int mp = MarketPosition[0];

            // if negative return false
            if (mp < 0) return false;

            double channel = HighestHigh(FastChannelLength);
            bool firstEntry = MarketPosition[1] <= 0 && mp == 0;
            bool firstEntryFilled = MarketPosition[1] <= 0 && mp > 0;
            bool newEntry = mp > 0 && mp - MarketPosition[1] > 0;
            bool maCondition = Bars.Low[0] >= MovingAverage(200);

            // first entry price = channel high
            if (firstEntry && maCondition)
            {
                EntryPrice = channel;
                //PlotStrategyOrderOnChart("LE 1", EntryPrice, Color.DarkGreen, ref PrintEntryText);
                return true;
            }

            // check when the first entry is filled
            if (firstEntryFilled)
            {
                RiskValue = ATR(AtrLength) * 2;
                StopLossPrice = EntryPrice - RiskValue;
                PrevEntryPrice = EntryPrice;
                NumOfEntries++;
            }

            // second entries onwards
            if (NumOfEntries < MaxNumOfEntries && mp > 0 && MarketPosition[1] > 0)
            {
                // new entry detected
                if (newEntry)
                {
                    // update new price
                    PrevEntryPrice = EntryPrice;
                    PrintEntryText = false;
                    NumOfEntries++;
                }

                EntryPrice = PrevEntryPrice + RiskValue;

                if (NumOfEntries < MaxNumOfEntries && maCondition)
                {
                    // plot order lines on chart
                    PlotStrategyOrderOnChart
                        ("LE " + (NumOfEntries + 1), EntryPrice, Color.DarkGreen, ref PrintEntryText);
                    return true;
                }
                else
                {
                    return false;
                }

            }

            return false;
        }
        private bool SendLxStpForLeStp()
        {
            // reset after long exit
            if (MarketPosition[1] > 0 && MarketPosition[0] <= 0)
            {
                // reset data and return false
                NumOfEntries = 0;
                RiskValue = StopLossPrice = 0;
                PrintEntryText = PrintStopLossText = false;
                EntryPrice = PrevEntryPrice = 0;

                return false;
            }

            if (StopLossPrice > 0 && MarketPosition[0] > 0)
            {
                double channel = LowestLow(FastChannelLength);

                // if close moves into 1 unit of profits, non-retracing
                StopLossPrice = Math.Max(StopLossPrice, channel);

                // print stop loss line
                PlotStrategyOrderOnChart("SL", StopLossPrice, Color.Yellow, ref PrintStopLossText);

                return true;
            }

            return false;
        }

        private bool SendLxStp()
        {
            // reset after long exit
            if (MarketPosition[1] > 0 && MarketPosition[0] <= 0)
            {
                // reset data and return false
                NumOfEntries = 0;
                RiskValue = StopLossPrice = 0;
                PrintStopLossText = false;

                return false;
            }

            if (StopLossPrice > 0 && MarketPosition[0] > 0)
            {
                double channel = LowestLow(FastChannelLength);

                // if close moves into 1 unit of profits, non-retracing
                StopLossPrice = Math.Max(StopLossPrice, channel);

                // print stop loss line
                PlotStrategyOrderOnChart("SL", StopLossPrice, Color.Yellow, ref PrintStopLossText);

                return true;
            }

            return false;
        }
        private double MovingAverage(int length)
        {
            // include current close
            double sum = 0;
            for (int i = 0; i < length; i++)
                sum += Bars.Close[i];

            return sum / length;
        }
        private double ATR(int length)
        {
            // deafult ATR calculations includes current bar
            double sum = 0;

            // i is bars back
            for (int i = 0; i < length; i++)
                sum += Bars.Range(i);

            return sum / length;
        }
        private double HighestHigh(int length)
        {
            double ret = 0;

            // exclude current high
            for (int i = 1; i < length; i++)
                ret = Math.Max(ret, Bars.High[i]);

            return ret;
        }
        private double LowestLow(int length)
        {
            double ret = double.MaxValue;

            // exclude current high
            for (int i = 1; i < length; i++)
                ret = Math.Min(ret, Bars.Low[i]);

            return ret;
        }
        #endregion

        #region Plots
        private void PlotStrategyOrderOnChart(string text, double price, Color color, ref bool Once)
        {
            bool closingTick = Bars.Status == EBarState.Close;
            bool optimization = Environment.Optimizing;

            // plot on closing tick and NOT during optimization
            if (closingTick == false && optimization == false) return;

            ChartPoint point = new ChartPoint(Bars.Time[0], price);
            ITextObject order = DrwText.Create(point, "-");
            order.Color = color;

            double onePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // print the stop loss text only once
            if (Once == false)
            {
                Once = true;
                point.Price = price + (2 * onePip);

                ITextObject txt = DrwText.Create(point, text);
                txt.Color = color;
            }
        }
        private void PlotStrategy()
        {
            // good way to test if internal function is working visually
            StrategyInfo.SetPlotValue(1, HighestHigh(SlowChannelLength));
            StrategyInfo.SetPlotValue(11, HighestHigh(FastChannelLength));
            StrategyInfo.SetPlotValue(2, LowestLow(SlowChannelLength));
            StrategyInfo.SetPlotValue(12, LowestLow(FastChannelLength));
            StrategyInfo.SetPlotValue(3, MovingAverage(200));
            StrategyInfo.SetPlotValue(10, ATR(AtrLength));
        }
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

    #endregion

}

