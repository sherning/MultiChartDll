
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
    public class Test_HistoryOrdersLookUp : SignalObject
    {
        public Test_HistoryOrdersLookUp(object _ctx) : base(_ctx) { }
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

            TradeManager.ProcessEvents();
        }

        protected override void OnRecalcLastBarAfterEvent()
        {
            CalcBar();
            ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
        }

        void Orders_FinishChanging(object sender, EventArgs e)
        {
            TradeManager.IOrders ord = sender as TradeManager.IOrders;
            Output.WriteLine("Data Count = {0}", ord.Items.Length);
            foreach (TradeManager.Order o in ord.Items)
            {
                Output.WriteLine("Order Profile = {0}, Order ID = {1} , ExecPrice = {2}, Generated = {3}, Filled\\Canceled = {4} ",
                  o.Profile, o.ID, o.ExecPrice == null ? 0 : o.ExecPrice, o.GeneratedDT, o.FinalDT == null ? new DateTime() : o.FinalDT);
            }
        }
    }
}


