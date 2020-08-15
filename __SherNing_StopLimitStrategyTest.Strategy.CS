using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    [IOGMode(IOGMode.Disabled)]
    public class __SherNing_StopLimitStrategyTest : SignalObject
    {
        //=========================================================================================
        // Project Name : Stop Limit Strategy Test
        // Description  : Test Stop Limit Orders
        // Type         : Strategy
        // Version      : v1.0
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
         * 1.0.1 - Test StopLimit as an Entry Order
         * 1.0.2 - Test StopLimit as an Exit Order
         *--------------------------------------- Objectives --------------------------------------
           
         *---------------------------------------- Versions ---------------------------------------
         *---------------------------------------- Versions ---------------------------------------
        /*/
        public __SherNing_StopLimitStrategyTest(object _ctx) : base(_ctx)
        {

        }

        IOrderMarket buyMarketOrder, sellMarketOrder;
        double sellStopPrice, sellLimitPrice;
        FunctionSeriesTest SeriesTest;
        FunctionSimpleTest SimpleTest;

        protected override void Create()
        {
            SeriesTest = new FunctionSeriesTest(this);
            SimpleTest = new FunctionSimpleTest(this);

            buyMarketOrder = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.Default, "EnterLong", EOrderAction.Buy));

            sellMarketOrder = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.Default, "ExitLong", EOrderAction.Sell));
        }
        double OnePip;
        protected override void StartCalc()
        {
            Output.Clear(); // Clear PowerLanguage Editor output tab
            OnePip = (Bars.Info.BigPointValue / Bars.Info.PriceScale) * 10;
        }

        protected override void CalcBar()
        {
            if (Bars.CurrentBar < 30)
            {
                //Print("Series Test[0]: " + SeriesTest[0]);
                //Print("Series Test[1]: " + SeriesTest[1]);
                if (Bars.CurrentBar % 5 == 0)
                {
                    Print("Simple Test:  " + SimpleTest.Value); 
                }
            }
        }

        private void Display(double price, Color color)
        {
            ChartPoint p = new ChartPoint(Bars.Time[0], price);
            ITextObject txt = DrwText.Create(p, "-");
            txt.Color = color;
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
    public sealed class FunctionSeriesTest : FunctionSeries<int>
    {
        // variable series test later
        // series Calcbar will be called once per bar, to store prev bar values
        public FunctionSeriesTest(CStudyControl m) : base(m)
        {

        }

        int Count;
        protected override void StartCalc()
        {
            Count = 0;
        }
        DateTime CurrentTime;
        protected override int CalcBar()
        {
            // update once per bar, no matter how many time it is called.
            if (Bars.Time[0] != CurrentTime)
            {
                CurrentTime = Bars.Time[0];
                Count++;
            }

            return Count;
        }
    }

    public sealed class FunctionSimpleTest : FunctionSimple<int>
    {
        // treat this just as a regular class, which you dont need to 
        // store values for every bar, only when in demand
        // but with access to Powerlanguage
        public FunctionSimpleTest (CStudyControl m) : base(m)
        {

        }
        int Count;
        protected override void StartCalc()
        {
            Count = 0;
        }
        protected override int CalcBar()
        {
            Count++;
            Output.WriteLine("Count: " + Count);
            return Count;
        }
    }

}