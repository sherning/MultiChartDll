
using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using PowerLanguage.TradeManager;
using ATCenterProxy;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PowerLanguage.Strategy
{
    //==========================================================================================
    // Name           : Trade Manager Test
    // Description    : Managing trade data from trade manager
    // Version        : v.1.0
    // Date Created   : 18 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v.1.0
     *  ----------------------------------- Version History -----------------------------------
     
     *  ---------------------------------------- Notes ----------------------------------------
     *   - Learning to manage real-time data.
     * 
     *  ---------------------------------------- Notes ----------------------------------------
     */
    public class __SherNing_TradeManagerTest : SignalObject
    {
        public __SherNing_TradeManagerTest (object _ctx) : base(_ctx) { }
        protected override void Create()
        {
        }

        protected override void StartCalc()
        {
            Output.Clear();
            ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
        }

        bool subscribed = false;

        protected override void CalcBar()
        {
            // Check order status
            OrderStatus();

            TradeManager.ProcessEvents();
        }

        private void OrderStatus()
        {
            if (!subscribed)
            {
                TradeManager.TradingData.Orders.FinishChanging += new EventHandler(Orders_FinishChanging);
                subscribed = true;
            }

            if (Bars.CurrentBar == 1)
            {
                TradeManager.TradingData.Orders.IntervalFltr.From = DateTime.Now.AddDays(-20);
                TradeManager.TradingData.Orders.ProfileFltr.CurrentValue = "Interactive Brokers";
            }
        }

        protected override void OnRecalcLastBarAfterEvent()
        {
            CalcBar();
            ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
        }

        void Orders_FinishChanging(object sender, EventArgs e)
        {
            IOrders ord = sender as IOrders;
            Output.WriteLine("Data Count = {0}", ord.Items.Length);
            foreach (TradeManager.Order o in ord.Items)
            {
                Output.WriteLine("Order Profile = {0}, Order ID = {1} , ExecPrice = {2}, Generated = {3}, Filled\\Canceled = {4} ",
                  o.Profile, o.ID, o.ExecPrice == null ? 0 : o.ExecPrice, o.GeneratedDT, o.FinalDT == null ? new DateTime() : o.FinalDT);
            }
        }
    }
}


