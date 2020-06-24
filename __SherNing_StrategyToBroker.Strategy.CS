using System;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using PowerLanguage.Function;
using PowerLanguage.TradeManager;
using ATCenterProxy.interop;

namespace PowerLanguage.Strategy
{
    //==========================================================================================
    // Name           : Strategy to Broker
    // Description    : Understand what is under the hood
    // Version        : v.1.0
    // Date Created   : 24 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v.1.0
     *  - 
     *  ----------------------------------- Version History -----------------------------------

     *  ---------------------------------------- Notes ----------------------------------------
     *   - Build strategies first before implementing broker orders
     *  ---------------------------------------- Notes ----------------------------------------
     */
    public class __SherNing_StrategyToBroker : SignalObject
    {
        public __SherNing_StrategyToBroker(object _ctx) : base(_ctx) { }
        protected override void Create()
        {
        }

        private MCSymbolInfo SymbolInfo;
        protected override void StartCalc()
        {
            Print("Datafeed: " + Bars.Info.DataFeed);

            // A Symbol Info?
            Print("Symbol Category: " + Bars.Info.ASymbolInfo2.SymbolCategory);
            Print("Symbol Name: " + Bars.Info.ASymbolInfo2.SymbolName);
            Print("Symbol Currency: " + Bars.Info.ASymbolInfo2.SymbolCurrency);
            Print("Symbol Exchange: " + Bars.Info.ASymbolInfo2.SymbolExchange);

            SymbolInfo = new MCSymbolInfo(Bars.Info.ASymbolInfo2, Bars.Info.DataFeed);

            // Symbol info ?
            Print("Symbol Info Symbol: " + SymbolInfo.symbol);
            Print("Symbol Info DataFeed: " + SymbolInfo.data_feed);
        }
        protected override void CalcBar()
        {
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