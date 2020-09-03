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
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Properties
        public __SherNing_TurtleTrading(object _ctx) : base(_ctx)
        {

        }
        #endregion

        #region References
        protected override void Create()
        {
            Output.Clear();
        }
        #endregion

        #region Fields
        protected override void StartCalc()
        {
            // assign inputs 
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
        }
        #endregion

        #region Private Methods
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

        #region Plot
        private void PlotStrategy()
        {
            StrategyInfo.SetPlotValue(1, HighestHigh(10));
            StrategyInfo.SetPlotValue(2, LowestLow(10));
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
        private void Print(string message, int chartbarNum)
        {
            int maxbars = ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar == chartbarNum - maxbars)
                Print(message);
        }
        #endregion
    }
}

namespace PowerLanguage.Function
{
    public sealed class TradePlan : FunctionSimple<int>
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Trade Plan Practice
        // Requirements : 1. Entry Order Types (Stop, limit, stop limit orders)
        //                2. 1 to 3 profit exits, and 1 stop loss
        //                3. Send an entry, one at a time up to 3 entries max
        //
        // Type         : Function
        // Version      : v1.0
        // Date         : 30 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Requirements, Architecture, Development, Optimization, Maintenance
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      :
        // Client       :
        // Contact      : 
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.0 - User Interface Requirements and programming architecture
         *  1.0.1 - Entry Structure
         *  1.0.2 - Profit Structure
         *  1.0.3 - StopLoss Structure
         *  1.0.4 - Design a new approach to developing software
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Properties
        public int HowManyEntries { get; set; }
        public int HowManyProfitTargets { get; set; }
        public TradePlan(CStudyControl m, EMarketPositionSide side) : base(m)
        {
            // set position side during object initialization
            PosSide = side;

            // by default, set 3 entries and 3 profit targets
            HowManyEntries = 3;
            HowManyProfitTargets = 3;
        }
        #endregion

        #region Fields
        EMarketPositionSide PosSide;
        EntryStruct[] Entry;
        ProfitStruct[] Profit;
        StopLossStruct StopLoss;
        int[] TickNum;
        protected override void StartCalc()
        {
            // entry - 3 entries, avoid [0]
            Entry = new EntryStruct[HowManyEntries + 1];
            for (int i = 0; i < Entry.Length; i++)
                Entry[i] = new EntryStruct();

            // exit - 3 profit targets, avoid [0]
            Profit = new ProfitStruct[HowManyProfitTargets + 1];
            for (int i = 0; i < Profit.Length; i++)
                Profit[i] = new ProfitStruct();

            // stop loss
            StopLoss = new StopLossStruct();

            // tick count, curr = TickNum[0], prev = TickNum[1]
            TickNum = new int[2];
        }

        #endregion

        #region Main
        protected override int CalcBar()
        {
            // increment tick count
            if (Environment.IOGEnabled) TickNum[0]++;
            else TickNum[0] = Bars.CurrentBar;

            Entry[1].Lock = true;
            Entry[1].Price = Bars.Close[0];

            if (Bars.CurrentBar < 55)
            {
                Print("Entry Price: " + Entry[1].Price);
            }

            // assign prev tick curr value
            TickNum[1] = TickNum[0];
            return 0;
        }
        #endregion

        #region Public Methods
        public void SetMarketOrder(double price, int contracts)
        {

        }
        public void SetLimitOrder(double price, int contracts, int timeInForce = 999)
        {
            int i;

            // check for an empty entry slot, and set entry properties
            for (i = 1; i < Entry.Length; i++)
            {
                if (Entry[i].OrderFilled == false)
                {
                    Entry[i].LimitPrice = Entry[i].Price = price;
                    Entry[i].Quantity = contracts;
                    Entry[i].TimeInForce = timeInForce;
                    Entry[i].OrderType = OrderType.Limit;
                    break;
                }
            }
        }

        public void SetStopOrder(double price, int contracts, int timeInForce = 999)
        {
            int i;

            // check for an empty entry slot, and set entry properties
            for (i = 1; i < Entry.Length; i++)
            {
                if (Entry[i].OrderFilled == false)
                {
                    Entry[i].StopPrice = Entry[i].Price = price;
                    Entry[i].Quantity = contracts;
                    Entry[i].TimeInForce = timeInForce;
                    Entry[i].OrderType = OrderType.Stop;
                    break;
                }
            }
        }

        public void SetStopLimitOrder(double stopPrice, double limitPrice, int contracts, int timeInForce = 999)
        {
            int i;

            // check for an empty entry slot, and set entry properties
            for (i = 1; i < Entry.Length; i++)
            {
                if (Entry[i].OrderFilled == false)
                {
                    Entry[i].StopPrice = stopPrice;
                    Entry[i].LimitPrice = limitPrice;
                    Entry[i].Quantity = contracts;
                    Entry[i].TimeInForce = timeInForce;
                    Entry[i].OrderType = OrderType.Stop;
                    break;
                }
            }
        }
        #endregion

        #region Private Methods
        private void LongPositionManager()
        {

        }

        private void LongEntryManager()
        {

        }

        private void LongExitManager()
        {

        }

        private void ShortPositionManager()
        {

        }

        private void ShortEntryManager()
        {

        }

        private void ShortExitManager()
        {

        }
        #endregion

        #region Internal Data Structure
        interface IOrderInfo
        {
            string Name { get; set; }
            Color OrderColor { get; set; }
            double Price { get; set; }
            int Quantity { get; set; }
            bool PrintTextOnce { get; set; }
            bool Send { get; }
            bool CancelOrder { get; set; }
            bool Lock { get; set; }
        }
        enum OrderType
        {
            // use inside TradePlan function only
            Stop, Limit, StopLimit
        }

        struct EntryStruct : IOrderInfo
        {
            // entry prices
            public double Price
            {
                get
                {
                    return Pricef;
                }
                set
                {
                    // lock price
                    if (Lock)
                    {
                        if (EntryPriceLockf == false)
                        {
                            Pricef = value;
                            EntryPriceLockf = true;
                        }
                    }

                    // float price
                    else Pricef = value;
                }
            }
            public double StopPrice
            {
                get
                {
                    return StopPricef;
                }
                set
                {
                    // lock price
                    if (Lock)
                    {
                        if (StopPriceLockf == false)
                        {
                            StopPricef = value;
                            StopPriceLockf = true;
                        }
                    }

                    // float price
                    else StopPricef = value;
                }
            }
            public double LimitPrice
            {
                get
                {
                    return LimitPricef;
                }
                set
                {
                    // lock price
                    if (Lock)
                    {
                        if (LimitPriceLockf == false)
                        {
                            LimitPricef = value;
                            LimitPriceLockf = true;
                        }
                    }

                    // float price
                    else LimitPricef = value;
                }
            }
            public int Quantity
            {
                get
                {
                    return Qtyf;
                }
                set
                {
                    // lock price
                    if (Lock)
                    {
                        if (QtyLockf == false)
                        {
                            Qtyf = value;
                            QtyLockf = true;
                        }
                    }

                    // float price
                    else Qtyf = value;
                }
            }
            public int TimeInForce
            {
                get
                {
                    return TimeInForcef;
                }
                set
                {
                    // lock price
                    if (Lock)
                    {
                        if (TimeInForceLockf == false)
                        {
                            TimeInForcef = value;
                            TimeInForceLockf = true;
                        }
                    }

                    // float price
                    else TimeInForcef = value;
                }
            }


            // entry info
            public string Name { get; set; }
            public Color OrderColor { get; set; }
            public int TickNumber { get; set; }
            public OrderType OrderType { get; set; }


            // entry state
            public bool Send { get; private set; }
            public bool CancelOrder { get; set; }
            public bool Conditions
            {
                set
                {
                    if (value)
                    {
                        OrderFilled = true;
                        Send = true;
                    }
                }
            }

            // entry settings
            public bool Lock { get; set; }
            public bool PrintTextOnce { get; set; }
            public bool OrderFilled { get; private set; }

            // fields (postfix f for fields)
            double Pricef, StopPricef, LimitPricef;
            int Qtyf, TimeInForcef;
            bool EntryPriceLockf, StopPriceLockf, LimitPriceLockf;
            bool TimeInForceLockf, QtyLockf;

            // public methods
            public void Reset()
            {
                Pricef = StopPricef = LimitPricef = 0;
                EntryPriceLockf = StopPriceLockf = LimitPriceLockf = false;
                OrderFilled = Send = false;
            }
        }
        struct ProfitStruct : IOrderInfo
        {
            public double Price { get; set; }
            public int Quantity { get; set; }
            public int TickNumber { get; set; }
            public string Name { get; set; }
            public Color OrderColor { get; set; }
            public bool Send { get; set; }
            public bool CancelOrder { get; set; }
            public bool PrintTextOnce { get; set; }
            public bool Lock { get; set; }
        }
        struct StopLossStruct : IOrderInfo
        {
            public double Price { get; set; }
            public int Quantity { get; set; }
            public int TickNumber { get; set; }
            public string Name { get; set; }
            public Color OrderColor { get; set; }
            public bool Send { get; set; }
            public bool CancelOrder { get; set; }
            public bool PrintTextOnce { get; set; }
            public bool Lock { get; set; }
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
