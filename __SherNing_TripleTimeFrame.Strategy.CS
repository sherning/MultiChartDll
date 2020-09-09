using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    public class __SherNing_TripleTimeFrame : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Triple Time Frame
        // Requirements : 
        // Type         : Strategy (IOG.Disabled)
        // Version      : v1.0
        // Date         : 08 - Sep - 2020
        // Time Taken   : 
        // Remarks      : 
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      :
        // Client       : Sher Ning
        // Contact      : 
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.1 - Requirements
         *  Strategy will be traded on the 60 min chart. 
         *  Strategy will have one stop loss and one profit target of 3 R
         *  Use Strategy properties to set initial capital
         *  Use proper position sizing
         *  1.0.2 - Long Short Strategy with AutoPositionSizing
         *  1.0.3 - Optimization of strategy      
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  1
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Inputs
        [Input] public string PamaSettings { get; set; }
        [Input] public int PamaLength { get; set; }
        [Input] public int PamaSmooth { get; set; }
        [Input] public string StrategySettings { get; set; }
        [Input] public double ProfitFactor { get; set; }
        [Input] public int StopLossLength { get; set; }
        [Input] public double RiskPerTrade { get; set; }
        [Input] public int AtrLength { get; set; }
        public __SherNing_TripleTimeFrame(object _ctx) : base(_ctx)
        {
            // default settings
            PamaSettings = "Pama Setting";
            PamaLength = 4;
            PamaSmooth = 10;

            StrategySettings = "Strategy Settings";
            ProfitFactor = 3;
            StopLossLength = 5;
            RiskPerTrade = 0.5; // in %
            AtrLength = 14;
        }
        #endregion

        #region References
        PriceActionMovingAverageFunction Pamaf;
        VariableSeries<int> MP;
        VariableSeries<double>[] Pama;
        VariableSeries<int>[] PamaDir;
        IOrderPriced LEntry, SEntry;
        IOrderPriced LStopLoss, SStopLoss;
        IOrderPriced LProfit, SProfit;

        protected override void Create()
        {
            Pamaf = new PriceActionMovingAverageFunction(this);

            // pama reference
            Pama = new VariableSeries<double>[4];
            for (int i = 0; i < Pama.Length; i++)
                Pama[i] = new VariableSeries<double>(this);

            // pama direction
            PamaDir = new VariableSeries<int>[4];
            for (int i = 0; i < PamaDir.Length; i++)
                PamaDir[i] = new VariableSeries<int>(this);

            // market position
            MP = new VariableSeries<int>(this);

            // orders
            LEntry = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LE", EOrderAction.Buy));

            SEntry = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "SE", EOrderAction.SellShort));

            LStopLoss = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LX_Stp", EOrderAction.Sell));

            SStopLoss = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "SX_Stp", EOrderAction.BuyToCover));

            LProfit = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LX_Pft", EOrderAction.Sell));

            SProfit = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "SX_Pft", EOrderAction.BuyToCover));

            Output.Clear();
        }
        #endregion

        #region Fields
        List<double> Pama2, Pama3;
        double Risk, EntryPrice, StopLossPrice, ProfitPrice;
        const int MinContracts = 10000;
        const int PipValue = 10;
        int TradeQty;
        protected override void StartCalc()
        {
            Pamaf._Length = PamaLength;
            Pamaf._Smooth = PamaSmooth;
            Pamaf._PriceType = 1;

            Pama2 = new List<double>();
            Pama3 = new List<double>();

            Risk = EntryPrice = StopLossPrice = ProfitPrice = 0;

            TradeQty = 0;
        }
        #endregion

        #region Main
        protected override void CalcBar()
        {
            if (Bars.CurrentBar == 1)
            {
                Pama[1] = Pamaf.Pama1;
                Pama[2] = Pamaf.Pama2;
                Pama[3] = Pamaf.Pama3;
            }

            Pamaf.Call();
            ClosingTickPamaValues();

            MP.Value = StrategyInfo.MarketPosition;

            // ensure sufficient bar infomation
            if (Bars.CurrentBar > 528 - ExecInfo.MaxBarsBack)
            {
                SendLongEntry();
                SendLongExits();
                SendShortEntry();
                SendShortExits();
            }


            //PlotStrategyInfo();
            if (Bars.LastBarOnChart)
            {
            }
        }
        #endregion

        #region Methods
        private bool LongEntryConditions()
        {
            // how many conditions ?
            bool[] conditions = new bool[1 + 8];
            conditions[0] = true;

            // check if daily is green - important
            conditions[1] = PamaDir[3][0] == 1;

            // check if 240 is red, for pullback - important
            conditions[2] = PamaDir[2][0] == -1 || PamaDir[2][1] == -1;

            // check if a bar close above the 240 line
            conditions[3] = Bars.Low[0] > Pama[2][0];

            // check if pama2 > pama3
            conditions[4] = Pama[2][0] > Pama[3][0];

            // check if market position is flat
            conditions[5] = MP[0] == 0;

            // must be green bar
            //conditions[6] = Bars.Open[0] < Bars.Close[0];
            conditions[6] = true;

            // spares
            conditions[7] = true;
            conditions[8] = true;

            // check if all conditions are satisfied
            for (int i = 0; i < conditions.Length; i++)
                if (conditions[i] == false) return false;

            return true;
        }
        private void SendLongEntry()
        {
            if (LongEntryConditions() == false) return;

            // --------------------- all conditions satisfied, send order --------------------- //
            double close = Bars.Close[0];
            double OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // trade info
            EntryPrice = close;
            //StopLossPrice = LowestLow(StopLossLength);
            //Risk = EntryPrice - StopLossPrice;

            // atr test
            Risk = ATR(AtrLength) * 2;
            StopLossPrice = EntryPrice - Risk;

            // check risk
            if (Risk > 50 * OnePip) Risk = 50 * OnePip;

            ProfitPrice = EntryPrice + (ProfitFactor * Risk);
            TradeQty = PositionSizing(Risk);

            LEntry.Send(close, TradeQty);
        }
        private void SendLongExits()
        {
            if (MP[0] > 0)
            {
                LStopLoss.Send(StopLossPrice, TradeQty);
                PlotStrategyOrder(StopLossPrice, Color.Yellow);

                LProfit.Send(ProfitPrice, TradeQty);
                PlotStrategyOrder(ProfitPrice, Color.Cyan);
            }

            // end of long trade
            //else if (MP[0] == 0 && MP[1] > 0)
            //{
            //    // reset trade info
            //    TradeQty = 0;
            //    Risk = EntryPrice = StopLossPrice = ProfitPrice = 0;
            //}
        }
        private bool ShortEntryConditions()
        {
            // optimising 
            // how many conditions ?
            bool[] conditions = new bool[1 + 8];
            conditions[0] = true;

            // check if daily is red
            conditions[1] = PamaDir[3][0] == -1;

            // check if 240 is green, for pullback
            conditions[2] = PamaDir[2][0] == 1 || PamaDir[2][1] == 1;

            // check if a bar high below the 240 line
            conditions[3] = Bars.High[0] < Pama[2][0];

            // check if pama2 > pama3
            conditions[4] = Pama[2][0] < Pama[3][0];

            // check if market position is flat
            conditions[5] = MP[0] == 0;

            // must be red bar
            //conditions[6] = Bars.Open[0] > Bars.Close[0];
            conditions[6] = true;

            // spares
            conditions[7] = true;
            conditions[8] = true;

            // check if all conditions are satisfied
            for (int i = 0; i < conditions.Length; i++)
                if (conditions[i] == false) return false;

            return true;
        }

        private void SendShortEntry()
        {
            if (ShortEntryConditions() == false) return;

            // --------------------- all conditions satisfied, send order --------------------- //
            double close = Bars.Close[0];
            double OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            // trade info
            EntryPrice = close;
            //StopLossPrice = HighestHigh(StopLossLength);
            //Risk = Math.Abs(EntryPrice - StopLossPrice);

            Risk = ATR(AtrLength) * 2;
            StopLossPrice = EntryPrice + Risk;

            if (Risk > 50 * OnePip) Risk = 50 * OnePip;

            ProfitPrice = EntryPrice - (ProfitFactor * Risk);
            TradeQty = PositionSizing(Risk);

            SEntry.Send(close, TradeQty);
        }
        private void SendShortExits()
        {
            if (MP[0] < 0)
            {
                SStopLoss.Send(StopLossPrice, TradeQty);
                PlotStrategyOrder(StopLossPrice, Color.Yellow);

                SProfit.Send(ProfitPrice, TradeQty);
                PlotStrategyOrder(ProfitPrice, Color.Cyan);
            }

            // end of long trade
            //else if (MP[0] == 0 && MP[1] < 0)
            //{
            //    //reset trade info
            //   Risk = EntryPrice = StopLossPrice = ProfitPrice = 0;
            //    TradeQty = 0;
            //}
        }
        private int PositionSizing(double risk)
        {
            // pull working capital
            double workingCapital = InitialCapital + NetProfit;

            // convert risk per trade to percent
            double riskInDollars = (RiskPerTrade / 100) * workingCapital;
            double onePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;

            if (risk <= 0) risk = onePip;

            double riskInPips = risk / onePip;
            double rawTradeQty = riskInDollars / (riskInPips * PipValue);

            // floor removes all the digits after decimal places
            double tradeQty = MinContracts * Math.Floor(rawTradeQty * 100000 / MinContracts);

            // ensure min contract
            if (tradeQty < MinContracts) tradeQty = MinContracts;

            return (int)tradeQty;
        }
        private double LowestLow(int length = 1)
        {
            double ret = double.MaxValue;

            // default length to 1 if left unspecified
            for (int i = 0; i < length; i++)
                ret = Math.Min(ret, Bars.Low[i]);

            return ret;
        }
        private double HighestHigh(int length = 1)
        {
            double ret = 0;

            // default length to 1 if left unspecified
            for (int i = 0; i < length; i++)
                ret = Math.Max(ret, Bars.High[i]);

            return ret;
        }
        private double ATR(int length = 1)
        {
            double sum = 0;

            for (int i = 0; i < length; i++)
                sum += Bars.Range(i);

            return sum / length;
        }
        private void ClosingTickPamaValues()
        {
            // get pama closing tick value on closing tick
            bool closingTick = Bars.Status == EBarState.Close;

            if (Pama[2][0] != Pama[2][1] && closingTick)
            {
                Pama2.Insert(0, Pama[2][0]);

                if (Pama2.Count > 3) Pama2.RemoveAt(3);

                if (Pama2.Count == 3)
                {
                    // these two events are mutually exclusive

                    // [2] > [1] < [0]
                    if (Pama2[2] > Pama2[1] && Pama2[1] < Pama2[0])
                        PamaDir[2].Value = 1;

                    // [2] < [1] > [0]
                    if (Pama2[2] < Pama2[1] && Pama2[1] > Pama2[0])
                        PamaDir[2].Value = -1;
                }
            }

            if (Pama[3][0] != Pama[3][1] && closingTick)
            {
                Pama3.Insert(0, Pama[3][0]);

                if (Pama3.Count > 3) Pama3.RemoveAt(3);

                if (Pama3.Count == 3)
                {
                    // [2] > [1] < [0]
                    if (Pama3[2] > Pama3[1] && Pama3[1] < Pama3[0])
                        PamaDir[3].Value = 1;

                    // [2] < [1] > [0]
                    if (Pama3[2] < Pama3[1] && Pama3[1] > Pama3[0])
                        PamaDir[3].Value = -1;
                }
            }
        }
        #endregion

        #region Plots
        private void PlotStrategyOrder(double price, Color color)
        {
            if (Environment.Optimizing || Bars.Status != EBarState.Close) return;

            ChartPoint p = new ChartPoint(Bars.Time[0], price);
            ITextObject order = DrwText.Create(p, "-");
            order.Color = color;
        }
        private void PlotStrategyInfo()
        {
            // pama plots
            StrategyInfo.SetPlotValue(1, Pama[1][0]);
            StrategyInfo.SetPlotValue(2, Pama[2][0]);
            StrategyInfo.SetPlotValue(3, Pama[3][0]);
        }
        #endregion

        #region Print
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false || Environment.Optimizing) return;

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