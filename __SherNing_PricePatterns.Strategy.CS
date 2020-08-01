using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    public class __SherNing_PricePatterns : SignalObject
    {
        //==========================================================================================
        // Name           : __SherNing_PricePatterns
        // Description    : Price Action Patterns Strategy
        // Version        : v1.0
        // Date Created   : 01 - Aug - 2020
        // Time Taken     : 
        // Remarks        : Protocol for starting and completing a project
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  
         *  ----------------------------------- Version History -----------------------------------
         *  
         *  ------------------------------------ Project Notes ------------------------------------
         *  - Default Profit target of 3 X risk reward
         *  - Default Stop Loss
         *  ------------------------------------ Project Notes ------------------------------------
         */

        public __SherNing_PricePatterns(object _ctx) : base(_ctx)
        { }

        // reference fields 
        PricePatternStrategy PricePattern;
        VariableSeries<double> Pama1, Pama2, Pama3;
        IOrderMarket BuyMkt, ShortMkt;
        IOrderPriced SellStop, CoverStop;
        IOrderPriced SellLmt, CoverLmt;

        protected override void Create()
        {
            PricePattern = new PricePatternStrategy(this);

            // entry
            BuyMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "LE_Mkt", EOrderAction.Buy));

            ShortMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "SE_Mkt", EOrderAction.SellShort));

            // stop loss 
            SellStop = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "LX_Stp", EOrderAction.Sell));

            CoverStop = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "SX_Stp", EOrderAction.BuyToCover));

            // profit exit
            SellLmt = OrderCreator.Limit(new SOrderParameters(
                Contracts.Default, "LX_$$", EOrderAction.Sell));

            CoverLmt = OrderCreator.Limit(new SOrderParameters(
                Contracts.Default, "SX_$$", EOrderAction.BuyToCover));

            // clear output window
            Output.Clear();
        }

        // value fields
        bool LeEntry, SeEntry, LxStop, SxStop, LxPft, SxPft;
        double LeEntryPrice, SeEntryPrice, LxStopPrice, SxStopPrice, LxPftPrice, SxPftPrice;

        protected override void StartCalc()
        {
            PricePattern._Length = 6;
            PricePattern._Smooth = 10;

            // reset 
            LeEntry = SeEntry = LxStop = SxStop = LxPft = SxPft = false;
        }
        protected override void CalcBar()
        {
            // call strategy function
            PricePattern.Call();

            if (Bars.CurrentBar == 1)
            {
                // assign same reference location as function
                Pama1 = PricePattern.Pama1;
                Pama2 = PricePattern.Pama2;
                Pama3 = PricePattern.Pama3;
            }

            // strategy conditions
            CurrentInsideBar();

            // order management
            if (LeEntry && StrategyInfo.MarketPosition == 0)
                BuyMkt.Send(100000);

            if (SeEntry && StrategyInfo.MarketPosition == 0)
                ShortMkt.Send(100000);

            // risk management
            bool longActive = StrategyInfo.MarketPosition > 0;
            bool shortActive = StrategyInfo.MarketPosition < 0;

            // entry is filled, and stop loss has not been calculated
            if (!LxStop && longActive)
            {
                LxStopPrice = Bars.Low[1];
                LxStop = true;
            }

            if (!SxStop && shortActive)
            {
                SxStopPrice = Bars.High[1];
                SxStop = true;
            }

            if (longActive && LxStop)
                SellStop.Send(LxStopPrice);

            if (shortActive && SxStop)
                CoverStop.Send(SxStopPrice);


            // profit target
            double risk;

            if (!LxPft && longActive)
            {
                risk = Bars.Close[0] - Bars.Low[1];
                LxPftPrice = Bars.Close[0] + (3 * risk);
                LxPft = true;
            }

            if (!SxPft && shortActive)
            {
                risk = Bars.High[1] - Bars.Close[0];
                SxPftPrice = Bars.Close[0] - (3 * risk);
                SxPft = true;
            }

            if (longActive && LxPft)
                SellLmt.Send(LxPftPrice);

            if (shortActive && SxPft)
                CoverLmt.Send(SxPftPrice);

            // reset
            if (StrategyInfo.MarketPosition == 0)
                LeEntry = SeEntry = LxStop = SxStop = LxPft = SxPft = false;
        }

        /// <summary>
        /// Strategy Condition - Inside bar
        /// </summary>
        /// <returns></returns>
        private void CurrentInsideBar()
        {
            double prevHigh = Bars.High[1];
            double currHigh = Bars.High[0];
            double prevLow = Bars.Low[1];
            double currLow = Bars.Low[0];
            bool insideBar = false;
            bool upPivot = false;
            bool downPivot = false;

            // current bar is an inside bar
            if (currHigh < prevHigh && currLow > prevLow)
                insideBar = true;

            // pivot condition [0] > [1] < [2]
            if (Pama1[0] > Pama1[1] && Pama1[1] < Pama1[2])
                upPivot = true;

            // pivot condition [0] < [1] > [2]
            if (Pama1[0] < Pama1[1] && Pama1[1] > Pama1[2])
                downPivot = true;

            // if current bar is an inside bar
            if (insideBar)
            {
                if (upPivot)
                {
                    LeEntry = true;
                    return;
                }

                if (downPivot)
                {
                    SeEntry = true;
                    return;
                }
            }

            LeEntry = SeEntry = false;
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

namespace PowerLanguage.Function
{
    /// <summary>
    /// Function for Price pattern strategy
    /// </summary>
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

        public PricePatternStrategy (CStudyControl _master) : base(_master)
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

}
