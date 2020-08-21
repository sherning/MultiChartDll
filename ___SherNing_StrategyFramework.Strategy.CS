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
    public class ___SherNing_StrategyFramework : SignalObject
    {
        //=========================================================================================
        // Project Name : Strategy Framework
        // Description  : 
        // Type         : Strategy
        // Version      : v1.0
        // Date         : 20 - Aug - 2020
        // Time Taken   : 
        // Remarks      : Strategy Framework Template
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
        public ___SherNing_StrategyFramework (object _ctx) : base(_ctx)
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

        #region Send Entry
        private void SendEntry() { }
        #endregion

        #region Send Exits
        private void SendExits() { }
        #endregion

        #region Strategy Conditions
        private void LongEntryConditions() { }
        private void ShortEntryConditions() { }
        #endregion

        #region Debug
        private void Debug()
        {
            // all the print statements in the context of current strategy
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

namespace PowerLanguage.Function
{
    #region Function - _StrategyFramework
    public sealed class _StrategyFramework : FunctionSimple<int>
    {
        #region Documentations
        //=========================================================================================
        // Project Name : _Strategy Framework
        // Description  : 
        // Type         : Function
        // Version      : v1.0
        // Date         : 20 - Aug - 2020
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

        public _StrategyFramework(CStudyControl m) : base(m)
        {

        }
        protected override void Create()
        {
            base.Create();
        }
        protected override void StartCalc()
        {
            base.StartCalc();
        }
        protected override int CalcBar()
        {
            throw new NotImplementedException();
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