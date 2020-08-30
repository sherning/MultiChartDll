using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;

namespace PowerLanguage.Strategy
{
    [IOGMode(IOGMode.Enabled)]
    public class __SherNing_StrategyOrderTypes : SignalObject
    {
        #region Documentations
        //=========================================================================================
        // Project Name : Strategy Order Types
        // Description  : Test exit and entry order types
        // Objectives   : Trade Execution Mastery (IOG.Enabled / Disabled)
        // Type         : Function
        // Version      : v6.3
        // Date         : 29 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Test Framework
        //=========================================================================================
        // Copyright    : 2020, Sher Ning Quantitative Technologies           
        // License      : Nil
        // Client       : Nil
        // Details      : sherning@hotmail.com
        //=========================================================================================

        /*/
         *----------------------------------- Version Objectives ----------------------------------
         *  1.0.0 - Design, goals and outline
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Object Properties & Constructor
        [Input] public int EntrySelection { get; set; }
        [Input] public int ExitSelection { get; set; }
        public __SherNing_StrategyOrderTypes(object _ctx) : base(_ctx)
        {
            EntrySelection = 1;
            ExitSelection = 4;
        }
        #endregion

        #region Program Main - CalcBar()
        protected override void CalcBar()
        {
            StrategyTest5();
        }

        private void StrategyTest1()
        {
            // three different entries with two exits each
            // IOG.Enabled
            // exit will be on opening tick, that's when BarNum updates.

            double close = Bars.Close[0];
            int mp = StrategyInfo.MarketPosition;
            bool closingTick = Bars.Status == EBarState.Close;

            // 27528
            if (Bars.CurrentBar == 27528 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (Bars.CurrentBar == 27538 - Maxbars && closingTick)
            {
                LeStp2.Send(close, 100000);
            }

            // 27560 
            if (Bars.CurrentBar == 27560 - Maxbars && closingTick)
            {
                LeStp3.Send(close, 100000);
            }

            // exits
            if (mp > 0)
            {
                // 27554
                if (Bars.CurrentBar == 27554 - Maxbars)
                {
                    LxLmt11.Send(close, 50000);
                }

                // 27564
                if (Bars.CurrentBar == 27567 - Maxbars)
                {
                    LxLmt12.Send(close, 50000);
                }

                // 27662
                if (Bars.CurrentBar == 27662 - Maxbars)
                {
                    LxLmt21.Send(close, 50000);
                }

                // 27697
                if (Bars.CurrentBar == 27697 - Maxbars)
                {
                    LxLmt22.Send(close, 50000);
                }

                // 27633
                if (Bars.CurrentBar == 27633 - Maxbars)
                {
                    LxLmt31.Send(close, 50000);
                }

                if (Bars.CurrentBar == 27732 - Maxbars)
                {
                    LxLmt32.Send(close, 50000);
                }
            }
        }
        private void StrategyTest2()
        {
            // three same entries with two exits
            // fromeach exit, send x contracts

            double close = Bars.Close[0];
            int mp = StrategyInfo.MarketPosition;
            bool closingTick = Bars.Status == EBarState.Close;

            // first entries
            if (Bars.CurrentBar == 27528 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (Bars.CurrentBar == 27530 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (Bars.CurrentBar == 27532 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }



            // exits
            if (mp > 0)
            {
                // Entry 1 first exit
                if (Bars.CurrentBar == 27574 - Maxbars)
                {
                    //LxLmt11.Send(close, 100000);
                    LxLmt11.SendFromEntry(close, 50000, "LeStp1");
                }

                // Entry 1 second exit
                if (Bars.CurrentBar == 27595 - Maxbars)
                {
                    LxLmt12.SendFromEntry(close, 50000, "LeStp1");
                }
            }
        }
        private void StrategyTest3()
        {
            // three same entries with two exits
            // Test FromEachTotal()
            // fills the first entry first. then second ...


            double close = Bars.Close[0];
            int mp = StrategyInfo.MarketPosition;
            bool closingTick = Bars.Status == EBarState.Close;

            // first entries
            if (Bars.CurrentBar == 27528 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (Bars.CurrentBar == 27530 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (Bars.CurrentBar == 27532 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            // exits
            if (mp > 0)
            {
                // Entry 1 first exit
                if (Bars.CurrentBar == 27574 - Maxbars && closingTick)
                {
                    LxLmt11.SendFromEntry(close, 150000, "LeStp1");
                }

                // Entry 1 second exit
                if (Bars.CurrentBar == 27595 - Maxbars && closingTick)
                {
                    LxLmt12.SendFromEntry("CloseAll", close, 150000, "LeStp1");
                }
            }
        }
        private void StrategyTest4()
        {
            // three same entries with two exits
            // Test FromEachTotal()
            // fills the first entry first. then second ...
            // test if we can just Send() or do we need to SendFromEntry()
            // we can use either Send() or SendFromEntry()

            double close = Bars.Close[0];
            int mp = StrategyInfo.MarketPosition;
            bool closingTick = Bars.Status == EBarState.Close;

            // first entries
            if (Bars.CurrentBar == 27528 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (Bars.CurrentBar == 27530 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (Bars.CurrentBar == 27532 - Maxbars && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            // exits
            if (mp > 0)
            {
                // Entry 1 first exit
                if (Bars.CurrentBar == 27574 - Maxbars && closingTick)
                {
                    //LxLmt11.SendFromEntry(close, 150000, "LeStp1");
                    LxLmt11.Send(close, 150000);
                }

                // Entry 1 second exit
                if (Bars.CurrentBar == 27595 - Maxbars && closingTick)
                {
                    //LxLmt12.SendFromEntry("CloseAll", close, 150000, "LeStp1");
                    LxLmt12.Send("CloseAll", close, 150000);
                }
            }
        }
        private void StrategyTest5()
        {
            // three same entries with two exits
            // Test FromEachTotal()
            // fills the first entry first. then second ...
            // test if we can just Send() or do we need to SendFromEntry()
            // we can use either Send() or SendFromEntry()
            // if that is the case ..
            // what is SendFromEntry() for ?
            // and how to update trade info ?

            double close = Bars.Close[0];
            int mp = StrategyInfo.MarketPosition;
            bool closingTick = Bars.Status == EBarState.Close;
            int time = Bars.Time[0].Hour * 100 + Bars.Time[0].Minute;

            // first entries
            if (time == 2100 && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (time == 2200 && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            if (time == 2300 && closingTick)
            {
                LeStp1.Send(close, 100000);
            }

            // exits
            if (mp > 0)
            {
                // Entry 1 first exit
                if (time == 0200 && closingTick)
                {
                    //LxLmt11.SendFromEntry(close, 150000, "LeStp1");
                    LxLmt11.Send(close, 50000);
                }

                // Entry 1 second exit
                if (time == 0400 && closingTick)
                {
                    //LxLmt12.SendFromEntry("CloseAll", close, 150000, "LeStp1");
                    LxLmt12.Send("CloseAll", close, 50000);
                }
            }
        }



        #endregion

        #region Object Initialization - Create()
        protected override void Create()
        {
            switch (EntrySelection)
            {
                case 1:
                    EntryOrdersSet1();
                    break;

                default:
                    break;
            }

            switch (ExitSelection)
            {
                case 1:
                    ExitOrdersSet1();
                    break;

                case 2:
                    ExitOrdersSet2();
                    break;

                case 3:
                    ExitOrdersSet3();
                    break;

                case 4:
                    ExitOrdersSet4();
                    break;

                default:
                    break;
            }
        }
        IOrderPriced LeStp1, LeStp2, LeStp3;
        IOrderPriced LxLmt11, LxLmt21, LxLmt31;
        IOrderPriced LxLmt12, LxLmt22, LxLmt32;
        IOrderPriced SeStp1, SeStp2, SeStp3;
        private void EntryOrdersSet1()
        {
            LeStp1 = OrderCreator.Stop(new SOrderParameters(
                Contracts.UserSpecified, "LeStp1", EOrderAction.Buy));

            LeStp2 = OrderCreator.Stop(new SOrderParameters(
               Contracts.UserSpecified, "LeStp2", EOrderAction.Buy));

            LeStp3 = OrderCreator.Stop(new SOrderParameters(
               Contracts.UserSpecified, "LeStp3", EOrderAction.Buy));
        }
        private void ExitOrdersSet1()
        {
            // assign to entry 1
            LxLmt11 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt11", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp1)));

            LxLmt12 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt12", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp1)));

            // assign to entry 2
            LxLmt21 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt21", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp2)));

            LxLmt22 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt22", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp2)));

            // assign to entry 3
            LxLmt31 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt31", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp3)));

            LxLmt32 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt32", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp3)));
        }
        private void ExitOrdersSet2()
        {
            // assign to entry 1
            LxLmt11 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt11", EOrderAction.Sell, OrderExit.FromEntry(LeStp1)));

            LxLmt12 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt12", EOrderAction.Sell, OrderExit.FromEntry(LeStp1)));
        }
        private void ExitOrdersSet3()
        {
            // test from each total

            // assign to entry 1
            LxLmt11 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt11", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp1)));

            LxLmt12 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt12", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp1)));
        }
        private void ExitOrdersSet4()
        {
            // test from each total

            // assign to entry 1
            //LxLmt11 = OrderCreator.Limit(new SOrderParameters(
            //    Contracts.UserSpecified, "LxLmt11", EOrderAction.Sell, OrderExit.FromEntryTotal(LeStp1)));

            LxLmt11 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt11", EOrderAction.Sell, OrderExit.FromAll));

            LxLmt12 = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified, "LxLmt12", EOrderAction.Sell, OrderExit.FromAll));
        }


        #endregion

        #region Object State - StartCalc()
        int Maxbars;
        protected override void StartCalc()
        {
            Maxbars = ExecInfo.MaxBarsBack;
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
         *  1.0.0 - Requirements and programming architecture
         * ---------------------------------- Version Objectives ----------------------------------
        /*/

        /*/
         *------------------------------------ Versions History -----------------------------------
         *  0.0.0 - Base Version
         *------------------------------------ Versions History -----------------------------------
        /*/

        #endregion

        #region Main
        protected override int CalcBar()
        {
            return 0;
        }
        #endregion

        #region Properties
        #endregion

        #region Fields
        EMarketPositionSide PosSide;
        bool IsEntryOrderFilled;
        int EntryQty, ProfitQty1, ProfitQty2, ProfitQty3;

        // market position is for the entire trade plan

        #endregion

        #region Constructor
        public TradePlan(CStudyControl m, EMarketPositionSide side) : base(m)
        {
            // set position side during object initialization
            PosSide = side;
        }
        #endregion

        #region Public Methods
        public void SetLimitOrder(double price, int contracts, int timeInForce = 999)
        {

        }

        public void SetStopOrder(double price, int contracts, int timeInForce = 999)
        {

        }

        public void SetStopLimitOrder(double stopPrice, double limitPrice, int contracts, int timeInForce = 999)
        {

        }
        #endregion

        #region Private Methods
        private void LongPosManager()
        {

        }
        private void ShortPosManager()
        {

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
