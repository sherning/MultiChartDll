using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;

namespace PowerLanguage.Strategy
{
    //==========================================================================================
    // Name           : Test Strategy
    // Description    : Run simple tests
    // Version        : v.1.0
    // Date Created   : 10 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *   v.1.0
     *  ----------------------------------- Version History -----------------------------------
     

     *  ---------------------------------------- Notes ----------------------------------------
     *   - Orders are sent collectively at the end of CalcBar, and not at Send()
     *  ---------------------------------------- Notes ----------------------------------------
     */
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_TestStrategy : SignalObject // v.1.0
    {
        // class fields
        private IOrderMarket BuyMktOrder, SellMktOrder;
        private DeveloperTools Tools;

        public __SherNing_TestStrategy(object _ctx) : base(_ctx) { }

        TestFunction Test;

        protected override void Create()
        {
            Test = new TestFunction(this);

            BuyMktOrder = OrderCreator
                .MarketNextBar(new SOrderParameters(Contracts.UserSpecified, EOrderAction.Buy));

            SellMktOrder = OrderCreator
                .MarketNextBar(new SOrderParameters(
                    Contracts.UserSpecified,
                    EOrderAction.Sell,
                    OrderExit.FromEntry(BuyMktOrder)));


            // load developer tools
            Tools = new DeveloperTools(this);

        }
        protected override void StartCalc()
        {

        }
        private int Count;
        protected override void CalcBar()
        {
            Test.Call();

            int mp = StrategyInfo.MarketPosition;
            int currTime = Tools.CurrentTime();

            if (currTime == 0330 && mp == 0)
            {
                BuyMktOrder.Send(100000);
                //Print("MP: " + mp);
                //Print("Current time: " + Tools.CurrentTime());
                Count = 0;
            }

            if (currTime == 0430 && Count < 3)
            {
                BuyMktOrder.Send(100000);
                //PrintStrategyReport();
                Count++;
                //Output.WriteLine("");
            }

            if (mp > 0 && currTime == 0530 && Count == 3)
            {
                SellMktOrder.Send(100000);
            }

        }

        protected override void StopCalc()
        {
            Output.Clear();
        }

        #region Strategy Report
        private bool PrintReport = true;
        private void PrintStrategyReport()
        {
            if (PrintReport == false) return;

            // Various strategy properties
            Print("Market Position: " + StrategyInfo.MarketPosition);
            Print("Bar State: " + Bars.Status);
            Print("Current Bar: " + Bars.CurrentBar);
            Print("Position Side: " + PositionSide);
            Print("Postion OpenLots: " + Positions[0].OpenLots);
            Print("Postion Side: " + Positions[0].Side);
            Print("Current Position: " + CurrentPosition.Value);
            Print("Open Trade Count: " + Positions[0].OpenTrades.Count);

            foreach (IMarketPosition marketPosition in Positions)
            {
                foreach (ITrade trade in marketPosition.OpenTrades)
                {
                    Print("Entry order: " + trade.EntryOrder.Price);
                    Print("Entry Bar Num: " + trade.EntryOrder.BarNumber);
                    Print("Profits: " + trade.Profit);
                    Output.WriteLine("");
                }
            }
            Output.WriteLine("");
        }
        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            Output.WriteLine("Project Name:  {0}   |   Bar Time:  {1}   |   Message:  {2}",
                Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
}

namespace PowerLanguage.Function
{
    public sealed class TestFunction : FunctionSimple<int>
    {
        public TestFunction(CStudyControl m) : base(m)
        {

        }

        VariableSeries<int> Test;

        protected override void Create()
        {
            Test = new VariableSeries<int>(this);
        }
        protected override int CalcBar()
        {
            //TestSuccessful();
            TestFailure();
            return 0;
        }
        private bool DebugMode = false;

        private void TestSuccessful()
        {
            if (Bars.CurrentBar == 83)
            {
                // current bar number is updated on first tick of the bar
                Print("BarState: " + Bars.Status);
                Print("Call me first");
                Test.Value = 1;
                Print("Test[0]: " + Test[0]);
            }

            if (Bars.CurrentBar == 83 && Bars.Status == EBarState.Inside)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
            }

            if (Bars.CurrentBar == 83 && Bars.Status == EBarState.Close)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
            }

            if (Bars.CurrentBar == 84 && Bars.Status == EBarState.Open)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
            }

            if (Bars.CurrentBar == 84 && Bars.Status == EBarState.Inside)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
            }
            if (Bars.CurrentBar == 84 && Bars.Status == EBarState.Close)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
            }
        }
        double Test2;
        private void TestFailure()
        {
            if(Bars.CurrentBar == 84)
            {
                Print("Barstate when mp updates: " + Bars.Status);
                Test.Value = StrategyInfo.MarketPosition;
                Test2 = StrategyInfo.MarketPosition;
                Print("Update Test[0]: " + Test[0]);
                Print("Update Test2: " + Test2);
            }


            if (Bars.CurrentBar == 84 && Bars.Status == EBarState.Open)
            {
                Print("Maxbars: " + ExecInfo.MaxBarsBack);
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
                Print("Update Test2: " + Test2);

            }

            if (Bars.CurrentBar == 84 && Bars.Status == EBarState.Inside)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
                Print("Update Test2: " + Test2);

            }

            if (Bars.CurrentBar == 84 && Bars.Status == EBarState.Close)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
                Print("Update Test2: " + Test2);

            }

            if (Bars.CurrentBar == 85 && Bars.Status == EBarState.Open)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
                Print("Update Test2: " + Test2);

            }

            if (Bars.CurrentBar == 85 && Bars.Status == EBarState.Inside)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
                Print("Update Test2: " + Test2);

            }
            if (Bars.CurrentBar == 85 && Bars.Status == EBarState.Close)
            {
                Print("BarState: " + Bars.Status);
                Print("Test[0]: " + Test[0]);
                Print("Update Test2: " + Test2);

            }
        }


        #region Print Functions
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            Output.WriteLine("Project Name:  {0}   |   Bar Time:  {1}   |   Message:  {2}",
                Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
}


