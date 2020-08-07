using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_PriceActionPatterns : SignalObject
    {
        //==========================================================================================
        // Name           : Price Action Patterns
        // Description    : Strategy
        // Version        : v1.0
        // Date Created   : 05 - Aug - 2020
        // Time Taken     : 
        // Remarks        : OCO Test
        //==========================================================================================
        // Copyright      : 2020, Sher Ning Technologies           
        // License        :      
        //==========================================================================================

        /*  ----------------------------------- Version History -----------------------------------
         *  
         *  ----------------------------------- Version History -----------------------------------
         *  
         *  ------------------------------------ Project Notes ------------------------------------
         *  - OCO orders without IOG
         *  - Track Stop and Limit orders
         *  - revise single entry multi exit
         *  - revise multi entry multi exit
         *  ------------------------------------ Project Notes ------------------------------------
         */

        // properties

        public __SherNing_PriceActionPatterns(object _ctx) : base(_ctx)
        {

        }

        // reference fields
        IOrderMarket LeMkt, SeMkt;
        IOrderPriced LePft, SePft, LeStp, SeStp;
        VariableSeries<EMarketPositionSide> PosSide;
        VariableSeries<int> MktPos;

        protected override void Create()
        {
            // entry orders
            LeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "Le Mkt", EOrderAction.Buy));

            SeMkt = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified, "Se Mkt", EOrderAction.SellShort));

            // oco orders
            LeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "Lx Stp", EOrderAction.Sell));

            SeStp = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default, "Sx Stp", EOrderAction.BuyToCover));

            LePft = OrderCreator.Limit(new SOrderParameters(
                Contracts.Default, "Lx Pft", EOrderAction.Sell));

            SePft = OrderCreator.Limit(new SOrderParameters(
                Contracts.Default, "Sx Pft", EOrderAction.BuyToCover));

            // reference variables
            PosSide = new VariableSeries<EMarketPositionSide>(this);
            MktPos = new VariableSeries<int>(this);

            // clear output when debugging in script
            Output.Clear();
        }

        // fields - value types 

        // each strategy order requires these information
        bool LeActive;
        double LePftPrice, LeStpPrice;
        protected override void StartCalc()
        {
            ClearData();
        }

        private void ClearData()
        {
            // reset data in the event strategy recalculates
            LeActive = false;
            LePftPrice = LeStpPrice = 0;
        }
        protected override void CalcBar()
        {
            // without IOG, calculate on close tick by default

            // cache strategy status, to determine if OCO filled
            MktPos.Value = StrategyInfo.MarketPosition;
            PosSide.Value = CurrentPosition.Side;

            // send order on closing tick
            if (Bars.Status == EBarState.Close) SendEntryOrder();

            SendExitOrders();
        }

        private void SendEntryOrder()
        {
            double close = Bars.Close[0];

            // send Le Entry order once
            if (TenBarHighBreak() && LeActive == false)
            {
                LeMkt.Send(100000);
                LeActive = true;

                // calculate stop price
                LeStpPrice = CalcLeStop();

                // profit price = current price + (3 X Risk)
                LePftPrice = close + (Math.Abs(close - LeStpPrice) * 2);
            }
        }
        private void SendExitOrders()
        {
            double close = Bars.Close[0];

            if (LeActive)
            {
                // check if stop price > 0 and close is above close
                if (LeStpPrice > 0 && close > LeStpPrice)
                {
                    // send stop order
                    LeStp.Send(LeStpPrice);

                    // mark stop order on chart
                    ChartPoint point = new ChartPoint(Bars.Time[0], LeStpPrice);
                    ITextObject marker = DrwText.Create(point, "-");
                    marker.Color = Color.White;
                }
                else ClearData();

                // check if profit price > 0 and close is less than close 
                if (LePftPrice > 0 && close < LePftPrice)
                {
                    // send profit order
                    LePft.Send(LePftPrice);

                    // mark profit order on chart
                    ChartPoint point = new ChartPoint(Bars.Time[0], LePftPrice);
                    ITextObject marker = DrwText.Create(point, "-");
                    marker.Color = Color.Yellow;
                }
                else ClearData();
            }
        }
        private double CalcLeStop()
        {
            return LowestLow(10);
        }

        private bool TenBarHighBreak()
        {
            double high = 0;

            // get the ten bar high value, exlude current high
            for (int i = 1; i <= 10; i++)
                high = Math.Max(high, Bars.High[i]);

            // if current close > 10 bar high, return true
            return Bars.Close[0] > high;
        }



        private double HighestHigh(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double high = 0;

            // get the ten bar high value, include current high
            for (int i = 0; i < bars; i++)
                high = Math.Max(high, Bars.High[i]);

            return high;
        }

        private double LowestLow(int bars)
        {
            // bars has to be 1 or greater
            if (bars <= 0) return 0;

            double low = double.MaxValue;

            // get the ten bar high value
            for (int i = 0; i < bars; i++)
                low = Math.Min(low, Bars.Low[i]);

            return low;
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