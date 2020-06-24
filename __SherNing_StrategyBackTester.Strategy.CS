using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PowerLanguage.Strategy
{
    //==========================================================================================
    // Name           : Strategy Tester
    // Description    : Use Text objects to backtest any strategy
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
     *   - Learn and develop my own approach to solving multichart strategies confidently
     *   - Entry Format - LE 100000
     *   - LE, LX, SE, SX for trade orders by default
     *   - Capable of stacking multiple similar orders
     *   - Tool bar support to hide and show text.
     *   - Mouse click support Shift + left click to refresh screen
     *   - Please enable Allow up to X entry orders in Strategy Properties
     *   
     *   v.1.1
     *   - Resolve multiple entry bugs
     *   
     *   v.2.0
     *   - Add new Profit Target PT and Stop Loss SL
     *   - New format for Profit Target which uses Limit Orders
     *   - New format for Stop Loss which uses Stop Orders
     *   - Auto-detect market position
     *   - Note: Limit and Stop orders are send on next bar.
     *   - If exit price is greater or less than the bar itself, use next bar open value
     *   
     *   v.2.1
     *   - Improve entry by using market when touched. 
     *   - Use Bar Magnifier to improve backtesting resolution. (set to 5 min)
     *   - Create Refresh button.
     *   
     *   
     *   
     *  ----------------------------------- Version History -----------------------------------
     

     *  ---------------------------------------- Notes ----------------------------------------
     *   v.0.0
     *   - Signal class name must match script name.
     *   - Apply proper class versions and method versions
     *   - Keep method versions.
     *   - send immediately or compile list of orders first.
     *   - Red bar : Open -> High -> Low -> Close
     *   - Green Bar : Open -> Low -> High -> Close
     *   - Build one using intrabarorder generation
     *   - Build one using market when touched.
     *   - Build on using stop limit order
     *   - Bar Magnifier best resolution is 1 min. Average 170 calculations per bar.
     *   
     *  ---------------------------------------- Notes ----------------------------------------
     */

    [MouseEvents(true), IOGMode(IOGMode.Enabled)]
    [ExitFromOneEntryOnce(ExitFromOneEntryOnce.FillAndKillExitOrder)]
    public class __SherNing_StrategyBackTester : SignalObject // v2.1
    {
        // strategy inputs
        [Input] public string StrategyTester_Settings { get; set; }
        [Input] public string BuyMarketText { get; set; }
        [Input] public string SellShortMarketText { get; set; }
        [Input] public string PriceExitText { get; set; }
        [Input] public string StopExitText { get; set; }
        [Input] public bool ClearNonStrategyText { get; set; }

        // class fields
        private IOrderMarket BuyMktOrder, SellShortMktOrder;
        private IOrderMarket SellLimitOrder, CoverLimitOrder;
        private IOrderMarket SellStopOrder, CoverStopOrder;
        private bool IsBuyMkt, IsSellShortMkt, IsSellLimit, IsCoverLimit, IsSellStop, IsCoverStop;
        private int BuyMktContracts, SellShortMktContracts, PriceExitContracts;
        private double PriceExitPrice, StopExitPrice;
        private List<BarInfo> ListOfOrderBars;
        private VariableSeries<int> MarketPosition;
        private const int NumOfOrderTypes = 6;

        public __SherNing_StrategyBackTester (object _ctx) : base(_ctx)
        {
            StrategyTester_Settings = "--Strategy Tester Settings Menu--";

            // can be changed by user.
            BuyMarketText = "LE";
            SellShortMarketText = "SE";
            PriceExitText = "PT";
            StopExitText = "SL";

            // remove non strategy text
            ClearNonStrategyText = true;
        }
        protected override void Create()
        {
            // Entry orders.
            BuyMktOrder = OrderCreator.MarketThisBar(
                new SOrderParameters(Contracts.UserSpecified, BuyMarketText, EOrderAction.Buy));

            SellShortMktOrder = OrderCreator.MarketThisBar(
               new SOrderParameters(Contracts.UserSpecified, SellShortMarketText, EOrderAction.SellShort));

            // Exit once from one entry set to false.
            SellLimitOrder = OrderCreator.MarketNextBar(new SOrderParameters(
              Contracts.UserSpecified,
              EOrderAction.Sell,
              OrderExit.Total));

            // Market if touched.
            CoverLimitOrder = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified,
                EOrderAction.BuyToCover,
                OrderExit.Total));

            // Exit from all, by using default.
            SellStopOrder = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.Default,
                EOrderAction.Sell));

            CoverStopOrder = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.Default,
                EOrderAction.BuyToCover));

            ListOfOrderBars = new List<BarInfo>();

            // keep track of market position
            MarketPosition = new VariableSeries<int>(this);
        }

        protected override void StartCalc()
        {
            // toolstrip startcalc
            ToolStripStartCalc();

            // set max bars back
            ExecInfo.MaxBarsBack = 1;

            // clear list
            ListOfOrderBars.Clear();

            // clear cache
            IsBuyMkt = IsSellShortMkt = IsSellLimit = false;
            IsCoverLimit = IsSellStop = IsCoverStop = false;
            BuyMktContracts = SellShortMktContracts = PriceExitContracts = 0;
            PriceExitPrice = StopExitPrice = 0;

            // Collect text data, before calculation begins.
            LoadOrderInfo();

            // load files and print if necessary

        }
        protected override void CalcBar()
        {
            // prev bar close, generate order for current bar
            if (Bars.Status == EBarState.Close)
            {
                // this should only be called once, for each bar duration.
                GetStrategyOrders();
            }

            // send orders current intrabar.
            SendStrategyOrders();
        }

        #region Strategy Logic
        private void SendStrategyOrders()
        {
            int mp = MarketPosition[0];

            // in case there is a stack of orders.
            for (int i = 0; i < NumOfOrderTypes; i++)
            {
                if (IsBuyMkt && BuyMktContracts > 0)
                {
                    BuyMktOrder.Send("LE_Mkt", BuyMktContracts);
                    IsBuyMkt = false;
                    BuyMktContracts = 0;
                }
                else if (IsSellShortMkt && SellShortMktContracts > 0)
                {
                    SellShortMktOrder.Send("SE_Mkt", SellShortMktContracts);
                    IsSellShortMkt = false;
                    SellShortMktContracts = 0;
                }
                else if (IsSellLimit && PriceExitContracts > 0 && Bars.Close[0] >= PriceExitPrice)
                {
                    // check if market position is non-zero positive int.
                    if (mp <= 0) return;

                    if (PriceExitContracts > mp)
                        SellLimitOrder.Send("LX_Lmt", mp);
                    else
                        SellLimitOrder.Send("LX_Lmt", PriceExitContracts);

                    IsSellLimit = false;
                    PriceExitContracts = 0;
                }
                else if (IsCoverLimit && PriceExitContracts > 0 && Bars.Close[0] <= PriceExitPrice)
                {
                    // check if market position is not zero and not a positive value
                    if (mp >= 0) return;

                    // MP can be negative
                    int contracts = Math.Abs(mp);

                    if (PriceExitContracts > contracts)
                        CoverLimitOrder.Send("SX_Lmt", contracts);
                    else
                        CoverLimitOrder.Send("SX_Lmt", PriceExitContracts);

                    IsCoverLimit = false;
                    PriceExitContracts = 0;
                }
                else if (IsSellStop && StopExitPrice != 0 && Bars.Close[0] <= StopExitPrice)
                {
                    SellStopOrder.Send("LX_Stp");
                    IsSellStop = false;
                    StopExitPrice = 0;
                }
                else if (IsCoverStop && StopExitPrice != 0 && Bars.Close[0] >= StopExitPrice)
                {
                    CoverStopOrder.Send("SX_Stp");
                    IsCoverStop = false;
                    StopExitPrice = 0;
                }
            }
        }
        private void GetStrategyOrders()
        {
            // check if there are any orders
            if (ListOfOrderBars.Count == 0) return;

            // reverse the list for optimization
            ListOfOrderBars.Sort();

            // marketposition
            MarketPosition.Value = StrategyInfo.MarketPosition;

            // iterate the list
            foreach (BarInfo bar in ListOfOrderBars)
            {
                // skip orders that are not in the list.
                if (Bars.TimeValue != bar.BarTime) continue;

                // buy mkt order
                if (bar.OrderType == BuyMarketText)
                {
                    IsBuyMkt = true;
                    BuyMktContracts = bar.Contracts;
                }

                // sell short mkt order
                else if (bar.OrderType == SellShortMarketText)
                {
                    IsSellShortMkt = true;
                    SellShortMktContracts = bar.Contracts;
                }

                // evaluate marketposition.
                if (MarketPosition[0] > 0)
                {
                    if (bar.OrderType == PriceExitText)
                    {
                        IsSellLimit = true;
                        PriceExitContracts = bar.Contracts;
                        PriceExitPrice = bar.Price;
                    }

                    if (bar.OrderType == StopExitText)
                    {
                        IsSellStop = true;
                        StopExitPrice = bar.Price;
                    }
                }

                // MP can be 0
                else if (MarketPosition[0] < 0)
                {
                    if (bar.OrderType == PriceExitText)
                    {
                        IsCoverLimit = true;
                        PriceExitContracts = bar.Contracts;
                        PriceExitPrice = bar.Price;
                    }

                    if (bar.OrderType == StopExitText)
                    {
                        IsCoverStop = true;
                        StopExitPrice = bar.Price;
                    }
                }
            }
        }
        private void LoadOrderInfo()
        {
            string orderType;
            int contracts;
            double price;
            DateTime barTime;
            BarInfo barData;

            // add open tick instead of closing tick
            int barSize = (int)Bars.Info.Resolution.Size;

            foreach (ITextObject text in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
            {
                string[] separator = { " " };
                string[] temp = text.Text
                    .ToUpper()
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                // cache order type.
                orderType = temp[0];
                barTime = text.Location.Time;

                // cannot use switch statement.
                if (orderType == BuyMarketText)
                {
                    bool result = int.TryParse(temp[1], out contracts);

                    // bitwise operator to evaluate both conditions.
                    if (result == false | contracts <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        // cache data
                        barData = new BarInfo(orderType, contracts, barTime);
                        ListOfOrderBars.Add(barData);

                        // show orders that are valid
                        text.Color = Color.LimeGreen;
                        text.VStyle = ETextStyleV.Below;
                        text.HStyle = ETextStyleH.Right;
                    }
                }
                else if (orderType == SellShortMarketText)
                {
                    bool result = int.TryParse(temp[1], out contracts);
                    if (result == false | contracts <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        // cache data
                        barData = new BarInfo(orderType, contracts, barTime);
                        ListOfOrderBars.Add(barData);

                        text.Color = Color.Red;
                        text.VStyle = ETextStyleV.Above;
                        text.HStyle = ETextStyleH.Right;
                    }

                }
                else if (orderType == PriceExitText)
                {
                    // parse contracts
                    bool result = int.TryParse(temp[1], out contracts);
                    if (result == false | contracts <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // parse price 
                    result = double.TryParse(temp[2], out price);
                    if (result == false | price <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // send on closing tick
                    barData = new BarInfo(orderType, contracts, barTime.AddMinutes(-barSize), price);
                    ListOfOrderBars.Add(barData);

                    text.Color = Color.Cyan;
                    text.VStyle = ETextStyleV.Above;
                    text.HStyle = ETextStyleH.Right;
                }
                else if (orderType == StopExitText)
                {
                    // parse price, no contracts for stoploss.
                    bool result = double.TryParse(temp[1], out price);
                    if (result == false | price <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // send on closing tick
                    barData = new BarInfo(orderType, barTime.AddMinutes(-barSize), price);
                    ListOfOrderBars.Add(barData);

                    text.Color = Color.Yellow;
                    text.VStyle = ETextStyleV.Above;
                    text.HStyle = ETextStyleH.Right;
                }
                else
                {
                    // none of the above.
                    if (ClearNonStrategyText)
                        text.Delete();
                    else
                        text.Color = Color.White;
                }
            }
        }
        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            if (arg.keys == Keys.Shift && arg.buttons == MouseButtons.Left)
            {
                ExecControl.Recalculate();
            }
        }
        protected override void StopCalc()
        {
            Output.Clear();
        }
        #endregion

        #region Bar Data Container
        class BarInfo : IComparable<BarInfo>
        {
            // unable to use Tuples in C# 4.0, there a class is required.
            public string OrderType { get; set; }
            public int Contracts { get; set; }
            public DateTime BarTime { get; set; }
            public double Price { get; set; }
            public BarInfo(string order, DateTime time, double price)
            {
                OrderType = order;
                BarTime = time;
                Price = price;
            }
            public BarInfo(string order, int contracts, DateTime time, double price = 0)
            {
                OrderType = order;
                Contracts = contracts;
                BarTime = time;
                Price = price;
            }

            public int CompareTo(BarInfo other)
            {
                if (other.BarTime > this.BarTime)
                {
                    return -1;
                }
                else if (other.BarTime < this.BarTime)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        #endregion

        #region ToolStrip Framework
        private bool IsToolStripCreated;
        private void ToolStripStartCalc()
        {
            // place this method in startcalc.
            if (IsToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(ToolStripFunction);
                IsToolStripCreated = true;
            }

            // reset variables here
            IsDisplayText = false;
        }


        private ToolStripButton DisplayTextsBtn;
        private void ToolStripCreate(ToolStrip tb)
        {
            // Create Toolstrip objects here and in order of display.

            ToolStripLabel label1 = new ToolStripLabel();
            label1.Text = "Display Text: ";
            AddNewItemToToolStrip(tb, label1);

            DisplayTextsBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, DisplayTextsBtn);
        }

        private bool IsDisplayText;
        private void ToolStripFunction(ToolStrip tb)
        {
            // main toolstrip adjustments
            tb.AutoSize = false;
            tb.Height = 80;
            tb.TabIndex = 0;

            // initialize tool strip objects
            ToolStripCreate(tb);

            // Write functionality here.
            DisplayTextsBtn.Text = "Show";
            DisplayTextsBtn.BackColor = Color.LimeGreen;
            DisplayTextsBtn.ForeColor = Color.White;
            DisplayTextsBtn.Click += delegate
            {
                if (IsDisplayText == true)
                {
                    DisplayTextsBtn.Text = "Show";
                    DisplayTextsBtn.BackColor = Color.LimeGreen;
                    DisplayTextsBtn.ForeColor = Color.White;
                    IsDisplayText = false;

                    string orderType;
                    foreach (ITextObject text in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
                    {
                        string[] separator = { " " };
                        string[] temp = text.Text
                            .ToUpper()
                            .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                        // cache order type.
                        orderType = temp[0];

                        // cannot use switch statement.
                        if (orderType == BuyMarketText)
                        {
                            text.Color = Color.LimeGreen;
                            text.VStyle = ETextStyleV.Below;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == SellShortMarketText)
                        {
                            text.Color = Color.Red;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == PriceExitText)
                        {
                            text.Color = Color.Cyan;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == StopExitText)
                        {
                            text.Color = Color.Yellow;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else
                        {
                            text.Color = Color.White;
                        }
                    }
                }

                // if false, hide text
                else
                {
                    DisplayTextsBtn.Text = "Hide";
                    DisplayTextsBtn.BackColor = Color.Red;
                    DisplayTextsBtn.ForeColor = Color.White;
                    IsDisplayText = true;

                    foreach (ITextObject textObj in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
                    {
                        textObj.Color = Color.Transparent;
                    }
                }

                // this will call OnRecalcLastBarAfterEvent.
                ExecControl.RecalcLastBarAfter(TimeSpan.FromMilliseconds(100));
            };


        }

        protected override void OnRecalcLastBarAfterEvent()
        {
            // no need to recalculate startcalc
            this.CalcBar();
        }
        private void AddNewItemToToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }

        protected override void Destroy()
        {
            // place this method in Destroy()
            if (IsToolStripCreated)
                ChartToolBar.AccessToolBar(RemoveToolBar);
        }

        private void RemoveToolBar(ToolStrip tb)
        {
            List<ToolStripItem> items = new List<ToolStripItem>();
            foreach (ToolStripItem item in tb.Items)
            {
                if (ReferenceEquals(this, item.Tag)) items.Add(item);
            }

            // Clear all tool bar items
            foreach (var item in items) tb.Items.Remove(item);
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

            // Develop using methods, to know who is the caller.
            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }

    #region Version History
    [MouseEvents(true)]
    [ExitFromOneEntryOnce(ExitFromOneEntryOnce.FillAndKillExitOrder)]
    public class __SNs_StrategyTester_v20 : SignalObject // v2.0
    {
        // strategy inputs
        [Input] public string StrategyTester_Settings { get; set; }
        [Input] public string BuyMarketText { get; set; }
        [Input] public string SellShortMarketText { get; set; }
        [Input] public string PriceExitText { get; set; }
        [Input] public string StopExitText { get; set; }
        [Input] public bool ClearNonStrategyText { get; set; }

        // class fields
        private IOrderMarket BuyMktOrder, SellShortMktOrder;
        private IOrderPriced SellLimitOrder, CoverLimitOrder;
        private IOrderPriced SellStopOrder, CoverStopOrder;
        private bool IsBuyMkt, IsSellShortMkt, IsSellLimit, IsCoverLimit, IsSellStop, IsCoverStop;
        private int BuyMktContracts, SellShortMktContracts, PriceExitContracts;
        private double PriceExitPrice, StopExitPrice;
        private List<BarInfo> ListOfOrderBars;
        private VariableSeries<int> MarketPosition;
        private const int NumOfOrderTypes = 6;

        public __SNs_StrategyTester_v20(object _ctx) : base(_ctx)
        {
            StrategyTester_Settings = "--Strategy Tester Settings Menu--";

            // can be changed by user.
            BuyMarketText = "LE";
            SellShortMarketText = "SE";
            PriceExitText = "PT";
            StopExitText = "SL";

            // remove non strategy text
            ClearNonStrategyText = true;
        }
        protected override void Create()
        {
            // market entry orders
            BuyMktOrder = OrderCreator.MarketThisBar(
                new SOrderParameters(Contracts.UserSpecified, BuyMarketText, EOrderAction.Buy));

            SellShortMktOrder = OrderCreator.MarketThisBar(
               new SOrderParameters(Contracts.UserSpecified, SellShortMarketText, EOrderAction.SellShort));

            // limit exit orders
            SellLimitOrder = OrderCreator.Limit(new SOrderParameters(
              Contracts.UserSpecified,
              EOrderAction.Sell,
              OrderExit.Total));

            CoverLimitOrder = OrderCreator.Limit(new SOrderParameters(
                Contracts.UserSpecified,
                EOrderAction.BuyToCover,
                OrderExit.Total));

            // stop orders - close all open positions
            SellStopOrder = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default,
                EOrderAction.Sell));

            CoverStopOrder = OrderCreator.Stop(new SOrderParameters(
                Contracts.Default,
                EOrderAction.BuyToCover));

            ListOfOrderBars = new List<BarInfo>();

            // keep track of market position
            MarketPosition = new VariableSeries<int>(this);
        }

        protected override void StartCalc()
        {
            // toolstrip startcalc
            ToolStripStartCalc();

            // set max bars back
            ExecInfo.MaxBarsBack = 1;

            // clear list
            ListOfOrderBars.Clear();

            // clear cache
            IsBuyMkt = IsSellShortMkt = IsSellLimit = false;
            IsCoverLimit = IsSellStop = IsCoverStop = false;
            BuyMktContracts = SellShortMktContracts = PriceExitContracts = 0;
            PriceExitPrice = StopExitPrice = 0;

            // Collect text data, before calculation begins.
            LoadOrderInfo();

            // load files and print if necessary

        }
        protected override void CalcBar()
        {
            // not necessary for historical orders.
            if (Bars.Status == EBarState.Close)
            {
                GetStrategyOrders();
                SendStrategyOrders();
            }
        }

        #region Strategy Logic
        private void SendStrategyOrders()
        {
            int mp = MarketPosition[0];

            for (int i = 0; i < NumOfOrderTypes; i++)
            {
                if (IsBuyMkt && BuyMktContracts > 0)
                {
                    BuyMktOrder.Send("LE_Mkt", BuyMktContracts);
                    IsBuyMkt = false;
                    BuyMktContracts = 0;
                }
                else if (IsSellShortMkt && SellShortMktContracts > 0)
                {
                    SellShortMktOrder.Send("SE_Mkt", SellShortMktContracts);
                    IsSellShortMkt = false;
                    SellShortMktContracts = 0;
                }
                else if (IsSellLimit && PriceExitContracts > 0 && PriceExitPrice != 0)
                {
                    if (PriceExitContracts > mp)
                        SellLimitOrder.Send("LX_Lmt", PriceExitPrice, mp);
                    else
                        SellLimitOrder.Send("LX_Lmt", PriceExitPrice, PriceExitContracts);

                    IsSellLimit = false;
                    PriceExitContracts = 0;
                }
                else if (IsCoverLimit && PriceExitContracts > 0 && PriceExitPrice != 0)
                {
                    if (PriceExitContracts > Math.Abs(mp))
                        CoverLimitOrder.Send("SX_Lmt", PriceExitPrice, Math.Abs(mp));
                    else
                        CoverLimitOrder.Send("SX_Lmt", PriceExitPrice, PriceExitContracts);

                    IsCoverLimit = false;
                    PriceExitContracts = 0;
                }
                else if (IsSellStop && StopExitPrice != 0)
                {
                    SellStopOrder.Send("LX_Stp", StopExitPrice);
                    IsSellStop = false;
                    StopExitPrice = 0;
                }
                else if (IsCoverStop && StopExitPrice != 0)
                {
                    CoverStopOrder.Send("SX_Stp", StopExitPrice);
                    IsCoverStop = false;
                    StopExitPrice = 0;
                }
            }
        }
        private void GetStrategyOrders()
        {
            // check if there are any orders
            if (ListOfOrderBars.Count == 0) return;

            // reverse the list for optimization
            ListOfOrderBars.Reverse<BarInfo>();

            // marketposition
            MarketPosition.Value = StrategyInfo.MarketPosition;

            // iterate the list
            foreach (BarInfo bar in ListOfOrderBars)
            {
                // skip orders that are not in the list.
                if (Bars.TimeValue != bar.BarTime) continue;

                // buy mkt order
                if (bar.OrderType == BuyMarketText)
                {
                    IsBuyMkt = true;
                    BuyMktContracts += bar.Contracts;
                }

                // sell short mkt order
                else if (bar.OrderType == SellShortMarketText)
                {
                    IsSellShortMkt = true;
                    SellShortMktContracts += bar.Contracts;
                }

                // evaluate marketposition.
                if (MarketPosition[0] > 0)
                {
                    if (bar.OrderType == PriceExitText)
                    {
                        IsSellLimit = true;
                        PriceExitContracts += bar.Contracts;
                        PriceExitPrice = bar.Price;
                    }

                    if (bar.OrderType == StopExitText)
                    {
                        IsSellStop = true;
                        StopExitPrice = bar.Price;
                    }
                }

                // MP can be 0
                else if (MarketPosition[0] < 0)
                {
                    if (bar.OrderType == PriceExitText)
                    {
                        IsCoverLimit = true;
                        PriceExitContracts += bar.Contracts;
                        PriceExitPrice = bar.Price;
                    }

                    if (bar.OrderType == StopExitText)
                    {
                        IsCoverStop = true;
                        StopExitPrice = bar.Price;
                    }
                }
            }
        }
        private void LoadOrderInfo()
        {
            string orderType;
            int contracts;
            double price;
            DateTime barTime;
            BarInfo barData;

            foreach (ITextObject text in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
            {
                string[] separator = { " " };
                string[] temp = text.Text
                    .ToUpper()
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                // cache order type.
                orderType = temp[0];
                barTime = text.Location.Time;

                // cannot use switch statement.
                if (orderType == BuyMarketText)
                {
                    bool result = int.TryParse(temp[1], out contracts);

                    // bitwise operator to evaluate both conditions.
                    if (result == false | contracts <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        // cache data
                        barData = new BarInfo(orderType, contracts, barTime);
                        ListOfOrderBars.Add(barData);

                        // show orders that are valid
                        text.Color = Color.LimeGreen;
                        text.VStyle = ETextStyleV.Below;
                        text.HStyle = ETextStyleH.Right;
                    }
                }
                else if (orderType == SellShortMarketText)
                {
                    bool result = int.TryParse(temp[1], out contracts);
                    if (result == false | contracts <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        // cache data
                        barData = new BarInfo(orderType, contracts, barTime);
                        ListOfOrderBars.Add(barData);

                        text.Color = Color.Red;
                        text.VStyle = ETextStyleV.Above;
                        text.HStyle = ETextStyleH.Right;
                    }

                }
                else if (orderType == PriceExitText)
                {
                    // parse contracts
                    bool result = int.TryParse(temp[1], out contracts);
                    if (result == false | contracts <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // parse price 
                    result = double.TryParse(temp[2], out price);
                    if (result == false | price <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    barData = new BarInfo(orderType, contracts, barTime, price);
                    ListOfOrderBars.Add(barData);

                    text.Color = Color.Cyan;
                    text.VStyle = ETextStyleV.Above;
                    text.HStyle = ETextStyleH.Right;
                }
                else if (orderType == StopExitText)
                {
                    // parse price, no contracts for stoploss.
                    bool result = double.TryParse(temp[1], out price);
                    if (result == false | price <= 0)
                    {
                        MessageBox.Show("Please correct: " + text.Text,
                            Name,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    barData = new BarInfo(orderType, barTime, price);
                    ListOfOrderBars.Add(barData);

                    text.Color = Color.Yellow;
                    text.VStyle = ETextStyleV.Above;
                    text.HStyle = ETextStyleH.Right;
                }
                else
                {
                    // none of the above.
                    if (ClearNonStrategyText)
                        text.Delete();
                    else
                        text.Color = Color.White;
                }
            }
        }
        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            if (arg.keys == Keys.Shift && arg.buttons == MouseButtons.Left)
            {

                ExecControl.Recalculate();
            }
        }
        protected override void StopCalc()
        {
            Output.Clear();
        }
        #endregion

        #region Bar Data Container
        class BarInfo
        {
            // unable to use Tuples in C# 4.0, there a class is required.
            public string OrderType { get; set; }
            public int Contracts { get; set; }
            public DateTime BarTime { get; set; }
            public double Price { get; set; }
            public BarInfo(string order, DateTime time, double price)
            {
                OrderType = order;
                BarTime = time;
                Price = price;
            }
            public BarInfo(string order, int contracts, DateTime time, double price = 0)
            {
                OrderType = order;
                Contracts = contracts;
                BarTime = time;
                Price = price;
            }
        }
        #endregion

        #region ToolStrip Framework
        private bool IsToolStripCreated;
        private void ToolStripStartCalc()
        {
            // place this method in startcalc.
            if (IsToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(ToolStripFunction);
                IsToolStripCreated = true;
            }

            // reset variables here
            IsDisplayText = false;
        }


        private ToolStripButton DisplayTexts;
        private ToolStripLabel DisplayLabel;
        private void ToolStripCreate(ToolStrip tb)
        {
            // Create Toolstrip objects here and in order of display.

            DisplayLabel = new ToolStripLabel();
            DisplayLabel.Text = "Display Text";
            AddNewItemToToolStrip(tb, DisplayLabel);

            DisplayTexts = new ToolStripButton();
            AddNewItemToToolStrip(tb, DisplayTexts);
        }

        private bool IsDisplayText;
        private void ToolStripFunction(ToolStrip tb)
        {
            // main toolstrip adjustments
            tb.AutoSize = false;
            tb.Height = 80;
            tb.TabIndex = 0;

            // initialize tool strip objects
            ToolStripCreate(tb);

            // Write functionality here.
            DisplayTexts.Text = "Show";
            DisplayTexts.BackColor = Color.LimeGreen;
            DisplayTexts.ForeColor = Color.White;
            DisplayTexts.Click += delegate
            {
                if (IsDisplayText == true)
                {
                    DisplayTexts.Text = "Show";
                    DisplayTexts.BackColor = Color.LimeGreen;
                    DisplayTexts.ForeColor = Color.White;
                    IsDisplayText = false;

                    string orderType;
                    foreach (ITextObject text in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
                    {
                        string[] separator = { " " };
                        string[] temp = text.Text
                            .ToUpper()
                            .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                        // cache order type.
                        orderType = temp[0];

                        // cannot use switch statement.
                        if (orderType == BuyMarketText)
                        {
                            text.Color = Color.LimeGreen;
                            text.VStyle = ETextStyleV.Below;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == SellShortMarketText)
                        {
                            text.Color = Color.Red;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == PriceExitText)
                        {
                            text.Color = Color.Cyan;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == StopExitText)
                        {
                            text.Color = Color.Yellow;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else
                        {
                            text.Color = Color.White;
                        }
                    }
                }

                // if false, hide text
                else
                {
                    DisplayTexts.Text = "Hide";
                    DisplayTexts.BackColor = Color.Red;
                    DisplayTexts.ForeColor = Color.White;
                    IsDisplayText = true;

                    foreach (ITextObject textObj in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
                    {
                        textObj.Color = Color.Transparent;
                    }
                }

                // this will call OnRecalcLastBarAfterEvent.
                ExecControl.RecalcLastBarAfter(TimeSpan.FromMilliseconds(100));
            };
        }

        protected override void OnRecalcLastBarAfterEvent()
        {
            // no need to recalculate startcalc
            this.CalcBar();
        }
        private void AddNewItemToToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }

        protected override void Destroy()
        {
            // place this method in Destroy()
            if (IsToolStripCreated)
                ChartToolBar.AccessToolBar(RemoveToolBar);
        }

        private void RemoveToolBar(ToolStrip tb)
        {
            List<ToolStripItem> items = new List<ToolStripItem>();
            foreach (ToolStripItem item in tb.Items)
            {
                if (ReferenceEquals(this, item.Tag)) items.Add(item);
            }

            // Clear all tool bar items
            foreach (var item in items) tb.Items.Remove(item);
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

            // Develop using methods, to know who is the caller.
            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
    [MouseEvents(true)]
    public class __SNs_StrategyTester_v11 : SignalObject // v1.1
    {
        // strategy inputs
        [Input] public string StrategyTester_Settings { get; set; }
        [Input] public string BuyOrderText { get; set; }
        [Input] public string SellOrderText { get; set; }
        [Input] public string SellShortOrderText { get; set; }
        [Input] public string BuyToCoverOrderText { get; set; }

        // class fields
        private IOrderMarket BuyMktOrder, SellMktOrder, SellShortMktOrder, BuyCoverMktOrder;
        private List<BarData> ListOfOrderBars;
        private int[] ContractsTracker, PositionSignal;
        private int TotalLongContracts, TotalShortContracts;

        public __SNs_StrategyTester_v11(object _ctx) : base(_ctx)
        {
            StrategyTester_Settings = "--Strategy Tester Settings Menu--";

            // can be changed by user.
            BuyOrderText = "LE";
            SellOrderText = "LX";
            SellShortOrderText = "SE";
            BuyToCoverOrderText = "SX";

        }
        protected override void Create()
        {
            BuyMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.UserSpecified, BuyOrderText, EOrderAction.Buy));

            SellMktOrder = OrderCreator.MarketNextBar(new SOrderParameters(
                    Contracts.UserSpecified,
                    SellOrderText,
                    EOrderAction.Sell,
                    OrderExit.FromEntryTotal(BuyMktOrder)));

            SellShortMktOrder = OrderCreator.MarketNextBar(
               new SOrderParameters(Contracts.UserSpecified, SellShortOrderText, EOrderAction.SellShort));

            BuyCoverMktOrder = OrderCreator.MarketNextBar(new SOrderParameters(
                Contracts.UserSpecified,
                BuyToCoverOrderText,
                EOrderAction.BuyToCover,
                OrderExit.FromEntryTotal(SellShortMktOrder)));

            ListOfOrderBars = new List<BarData>();

            // 4 order types. avoid [0]
            ContractsTracker = new int[5];
            PositionSignal = new int[5];
        }

        protected override void StartCalc()
        {
            // toolstrip startcalc
            ToolStripStartCalc();

            // set max bars back
            ExecInfo.MaxBarsBack = 1;

            // reset data.
            ListOfOrderBars.Clear();
            TotalLongContracts = TotalShortContracts = 0;
            for (int i = 0; i < 5; i++)
            {
                PositionSignal[i] = 0;
                ContractsTracker[i] = 0;
            }

            // Collect text data, before calculation begins.
            GetAllOrdersFromChart();

            // load files and print if necessary

        }
        protected override void CalcBar()
        {
            // not necessary for historical orders.
            if (Bars.Status == EBarState.Close)
            {
                GetStrategyOrders();
                GenerateStrategyOrders();
            }
        }

        #region Strategy Logic
        private void GenerateStrategyOrders()
        {
            for (int i = 1; i < PositionSignal.Length; i++)
            {
                // flat or long.
                if (TotalLongContracts > 0)
                {
                    // if there is a buy order
                    if (PositionSignal[1] == 1)
                    {
                        BuyMktOrder.Send(ContractsTracker[1]);
                        TotalLongContracts += ContractsTracker[1];
                        TotalShortContracts = 0;
                        PositionSignal[1] = 0;
                    }

                    // market position must be net long.
                    else if (PositionSignal[2] == 1 && TotalLongContracts > 0)
                    {
                        if (ContractsTracker[2] >= TotalLongContracts)
                        {
                            SellMktOrder.SendFromEntry(TotalLongContracts, BuyOrderText);
                            TotalLongContracts = 0;
                        }
                        else
                        {
                            SellMktOrder.SendFromEntry(ContractsTracker[2], BuyOrderText);
                            TotalLongContracts -= ContractsTracker[2];
                        }

                        PositionSignal[2] = 0;
                    }

                    // regardless, it will close and reverse.
                    else if (PositionSignal[3] == 1)
                    {
                        SellShortMktOrder.Send(ContractsTracker[3]);
                        TotalLongContracts = 0;
                        TotalShortContracts = ContractsTracker[3];
                        PositionSignal[3] = 0;
                    }
                }
                else if (TotalShortContracts > 0)
                {
                    // regardless, it will close and reverse.
                    if (PositionSignal[3] == 1)
                    {
                        SellShortMktOrder.Send(ContractsTracker[3]);
                        TotalShortContracts += ContractsTracker[3];
                        PositionSignal[3] = 0;
                    }

                    // market position must be net short.
                    else if (PositionSignal[4] == 1 && TotalShortContracts > 0)
                    {
                        if (ContractsTracker[4] >= TotalShortContracts)
                        {
                            BuyCoverMktOrder.SendFromEntry(TotalShortContracts, SellShortOrderText);
                            TotalShortContracts = 0;
                        }
                        else
                        {
                            BuyCoverMktOrder.SendFromEntry(ContractsTracker[4], SellShortOrderText);
                            TotalShortContracts -= ContractsTracker[4];
                        }

                        PositionSignal[4] = 0;
                    }

                    // if there is a buy reverse order
                    else if (PositionSignal[1] == 1)
                    {
                        BuyMktOrder.Send(ContractsTracker[1]);
                        TotalLongContracts = ContractsTracker[1];
                        TotalShortContracts = 0;
                        PositionSignal[1] = 0;
                    }
                }
                // position is flat.
                else
                {
                    if (PositionSignal[1] == 1)
                    {
                        BuyMktOrder.Send(ContractsTracker[1]);
                        TotalLongContracts = ContractsTracker[1];
                        TotalShortContracts = 0;
                        PositionSignal[1] = 0;
                    }
                    else if (PositionSignal[3] == 1)
                    {
                        SellShortMktOrder.Send(ContractsTracker[3]);
                        TotalLongContracts = 0;
                        TotalShortContracts = ContractsTracker[3];
                        PositionSignal[3] = 0;
                    }
                }
            }

            // reset contract tracker.
            for (int i = 1; i < ContractsTracker.Length; i++)
                ContractsTracker[i] = 0;
        }
        private void GetStrategyOrders()
        {
            // check if there are any orders.
            if (ListOfOrderBars.Count == 0) return;

            // Last in first out, reverse the list.
            foreach (BarData bar in ListOfOrderBars.Reverse<BarData>())
            {
                if (Bars.Time[0] == bar.BarTime)
                {
                    // buy order
                    if (bar.OrderType == BuyOrderText)
                    {
                        PositionSignal[1] = 1;
                        ContractsTracker[1] += bar.Contracts;
                    }

                    // sell order
                    else if (bar.OrderType == SellOrderText)
                    {
                        PositionSignal[2] = 1;
                        ContractsTracker[2] += bar.Contracts;
                    }

                    // sell short order
                    else if (bar.OrderType == SellShortOrderText)
                    {
                        PositionSignal[3] = 1;
                        ContractsTracker[3] += bar.Contracts;
                    }

                    // buy to cover order
                    else if (bar.OrderType == BuyToCoverOrderText)
                    {
                        PositionSignal[4] = 1;
                        ContractsTracker[4] += bar.Contracts;
                    }
                }
            }
        }
        private void GetAllOrdersFromChart()
        {
            string orderType;
            int contracts;
            ChartPoint chartLoc;

            foreach (ITextObject text in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
            {
                string[] separator = { " " };
                string[] temp = text.Text
                    .ToUpper()
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                // always catch exception first, before proceeding.
                if (temp.Length != 2)
                {
                    MessageBox.Show("Please check if you have input the correct data" +
                       "in this format: BuyText XXX");

                    // most likely no contracts detected.
                    return;
                }
                else
                {
                    // split the string to get order and contracts.
                    orderType = temp[0];
                    contracts = int.Parse(temp[1]);
                }

                // get the location of the order bar.
                chartLoc = text.Location;
                int barNumber = (int)chartLoc.BarNumber;
                DateTime barTime = chartLoc.Time;

                // number of bars back from lastbar on chart.
                //int actualBarNumber = (int)chartLoc.BarNumber + ExecInfo.MaxBarsBack;
                //int barsAgo = Bars.FullSymbolData.Count - actualBarNumber;
                //double barHigh = Bars.FullSymbolData.High[barsAgo];
                //double barLow = Bars.FullSymbolData.Low[barsAgo];
                //double halfRange = (barHigh - barLow) * 0.5;

                // create and add orders to list of bardata.
                BarData barsData = new BarData(orderType, contracts, barTime);
                ListOfOrderBars.Add(barsData);

                // re-arrange and re-color order text.
                if (orderType == BuyOrderText)
                {
                    text.Color = Color.LimeGreen;
                    text.VStyle = ETextStyleV.Below;
                    text.HStyle = ETextStyleH.Right;
                }
                else if (orderType == SellOrderText)
                {
                    text.Color = Color.DarkGreen;
                    text.VStyle = ETextStyleV.Above;
                    text.HStyle = ETextStyleH.Right;
                }
                else if (orderType == SellShortOrderText)
                {
                    text.Color = Color.Red;
                    text.VStyle = ETextStyleV.Above;
                    text.HStyle = ETextStyleH.Right;
                }
                else if (orderType == BuyToCoverOrderText)
                {
                    text.Color = Color.DarkRed;
                    text.VStyle = ETextStyleV.Below;
                    text.HStyle = ETextStyleH.Right;
                }
                else
                {
                    text.Color = Color.White;
                    text.Text = "Invalid";
                }
            }
        }
        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            if (arg.keys == Keys.Shift && arg.buttons == MouseButtons.Left)
            {

                ExecControl.Recalculate();
            }
        }
        #endregion

        protected override void StopCalc()
        {
            Output.Clear();
        }

        #region Bar Data Container
        class BarData
        {
            // unable to use Tuples in C# 4.0, there a class is required.
            public string OrderType { get; set; }
            public int BarsAgo { get; set; }
            public int Contracts { get; set; }
            public DateTime BarTime { get; set; }

            public BarData(string order, int contracts, DateTime time)
            {
                OrderType = order;
                Contracts = contracts;
                BarTime = time;
            }
        }
        #endregion

        #region ToolStrip Framework
        private bool IsToolStripCreated;
        private void ToolStripStartCalc()
        {
            // place this method in startcalc.
            if (IsToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(ToolStripFunction);
                IsToolStripCreated = true;
            }

            // reset variables here
            IsDisplayText = false;
        }


        private ToolStripButton DisplayTexts;
        private ToolStripLabel DisplayLabel;
        private void ToolStripCreate(ToolStrip tb)
        {
            // Create Toolstrip objects here and in order of display.

            DisplayLabel = new ToolStripLabel();
            DisplayLabel.Text = "Display Text";
            AddNewItemToToolStrip(tb, DisplayLabel);

            DisplayTexts = new ToolStripButton();
            AddNewItemToToolStrip(tb, DisplayTexts);
        }

        private bool IsDisplayText;
        private void ToolStripFunction(ToolStrip tb)
        {
            // main toolstrip adjustments
            tb.AutoSize = false;
            tb.Height = 80;
            tb.TabIndex = 0;

            // initialize tool strip objects
            ToolStripCreate(tb);

            // Write functionality here.
            DisplayTexts.Text = "Show";
            DisplayTexts.BackColor = Color.LimeGreen;
            DisplayTexts.ForeColor = Color.White;
            DisplayTexts.Click += delegate
            {
                if (IsDisplayText == true)
                {
                    DisplayTexts.Text = "Show";
                    DisplayTexts.BackColor = Color.LimeGreen;
                    DisplayTexts.ForeColor = Color.White;
                    IsDisplayText = false;
                    string orderType;

                    foreach (ITextObject text in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
                    {
                        string[] separator = { " " };
                        string[] temp = text.Text
                            .ToUpper()
                            .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                        // always catch exception first, before proceeding.
                        if (temp.Length != 2)
                        {
                            MessageBox.Show("Please check if you have input the correct data" +
                               "in this format: BuyText XXX");

                            // most likely no contracts detected.
                            return;
                        }
                        else
                        {
                            // split the string to get order and contracts.
                            orderType = temp[0];
                        }

                        // re-arrange and re-color order text.
                        if (orderType == BuyOrderText)
                        {
                            text.Color = Color.LimeGreen;
                            text.VStyle = ETextStyleV.Below;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == SellOrderText)
                        {
                            text.Color = Color.DarkGreen;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == SellShortOrderText)
                        {
                            text.Color = Color.Red;
                            text.VStyle = ETextStyleV.Above;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else if (orderType == BuyToCoverOrderText)
                        {
                            text.Color = Color.DarkRed;
                            text.VStyle = ETextStyleV.Below;
                            text.HStyle = ETextStyleH.Right;
                        }
                        else
                        {
                            text.Color = Color.White;
                            text.Text = "Invalid";
                        }
                    }

                }

                // if false, hide text
                else
                {
                    DisplayTexts.Text = "Hide";
                    DisplayTexts.BackColor = Color.Red;
                    DisplayTexts.ForeColor = Color.White;
                    IsDisplayText = true;
                    foreach (ITextObject textObj in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
                    {
                        textObj.Color = Color.Transparent;
                    }
                }

                // this will call OnRecalcLastBarAfterEvent.
                ExecControl.RecalcLastBarAfter(TimeSpan.FromMilliseconds(100));
            };
        }

        protected override void OnRecalcLastBarAfterEvent()
        {
            // no need to recalculate startcalc
            CalcBar();
        }
        private void AddNewItemToToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }

        protected override void Destroy()
        {
            // place this method in Destroy()
            if (IsToolStripCreated)
                ChartToolBar.AccessToolBar(RemoveToolBar);
        }

        private void RemoveToolBar(ToolStrip tb)
        {
            List<ToolStripItem> items = new List<ToolStripItem>();
            foreach (ToolStripItem item in tb.Items)
            {
                if (ReferenceEquals(this, item.Tag)) items.Add(item);
            }

            // Clear all tool bar items
            foreach (var item in items) tb.Items.Remove(item);
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

            // Develop using methods, to know who is the caller.
            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
    [MouseEvents(true)]
    public class __SNs_StrategyTester_v10 : SignalObject // v.1.0
    {
        // strategy inputs
        [Input] public string StrategyTester_Settings { get; set; }
        [Input] public double AccountSize { get; set; }
        [Input] public int Risk { get; set; }
        [Input] public int MaximumContractsAllowed { get; set; }
        [Input] public string BuyOrderText { get; set; }
        [Input] public string SellOrderText { get; set; }
        [Input] public string SellShortOrderText { get; set; }
        [Input] public string BuyToCoverOrderText { get; set; }


        // class fields
        private IOrderMarket BuyMktOrder, SellMktOrder, SellShortMktOrder, BuyCoverMktOrder;
        private VariableSeries<EMarketPositionSide> MarketPosition;
        private AutoPositionSizingFX AutoPos;
        private List<BarData> ListOfOrderBars;
        private int[] ContractsTracker, PositionSignal;


        public __SNs_StrategyTester_v10(object _ctx) : base(_ctx)
        {
            StrategyTester_Settings = "--Strategy Tester Settings Menu--";

            // Default account size $100,000. risk 2%. Max Contract 10 million.
            AccountSize = 100000;
            Risk = 2;
            MaximumContractsAllowed = 10000000;

            // can be changed by user.
            BuyOrderText = "LE";
            SellOrderText = "LX";
            SellShortOrderText = "SE";
            BuyToCoverOrderText = "SX";

        }
        protected override void Create()
        {
            BuyMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.UserSpecified, "LE", EOrderAction.Buy));

            SellMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.UserSpecified, "LX", EOrderAction.Sell));

            SellShortMktOrder = OrderCreator.MarketNextBar(
               new SOrderParameters(Contracts.UserSpecified, "SE", EOrderAction.SellShort));

            BuyCoverMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.UserSpecified, "SX", EOrderAction.BuyToCover));

            MarketPosition = new VariableSeries<EMarketPositionSide>(this);

            AutoPos = new AutoPositionSizingFX(this);

            ListOfOrderBars = new List<BarData>();
        }

        protected override void StartCalc()
        {
            // set max bars back
            ExecInfo.MaxBarsBack = 1;

            // autoposition sizing properties
            AutoPos._AccountSize = AccountSize;
            AutoPos._Risk = Risk;
            AutoPos._MinimumContracts = 20000;
            AutoPos._MaximumContracts = MaximumContractsAllowed;
            AutoPos._StepSize = 20000;

            // 4 order types. avoid [0]
            ContractsTracker = new int[5];
            PositionSignal = new int[5];
        }
        protected override void CalcBar()
        {
            // load files and print onto chart if necessary.
            if (Bars.CurrentBar == 1)
            {

            }

            // don't have to wait till LastBarOnChart to get txt objs
            if (Bars.CurrentBar == 2)
            {
                // Collect text data.
                GetAllOrdersFromChart();
            }

            // not necessary for historical orders.
            if (Bars.Status == EBarState.Close)
            {
                GetStrategyOrders();
                GenerateStrategyOrders();
            }
        }

        private void GenerateStrategyOrders()
        {
            // check to see how many orders are there.
            int numOfOrders = 0;
            for (int i = 0; i < 5; i++)
                numOfOrders += PositionSignal[i];

            // retrieves the number of contracts +ve net long -ve net short
            int strategyMP = StrategyInfo.MarketPosition;
            int idx = 0;

            while (idx < numOfOrders)
            {
                // current position is long.
                if (strategyMP > 0)
                {
                    // sequence order -> buy -> sell -> sellshort 
                    // check if there are buy orders
                    if (PositionSignal[1] > 0)
                    {
                        BuyMktOrder.Send(ContractsTracker[1]);
                        PositionSignal[1] = 0;
                        ContractsTracker[1] = 0;
                        idx++;
                        continue;
                    }

                    // check if there are sell orders.
                    else if (PositionSignal[2] > 0)
                    {
                        // selling more than current position 
                        if (ContractsTracker[2] > strategyMP)
                            SellShortMktOrder.Send(strategyMP);
                        else
                            SellMktOrder.Send(ContractsTracker[2]);

                        PositionSignal[2] = 0;
                        ContractsTracker[2] = 0;
                        idx++;
                        continue;
                    }

                    // check if there are reverse sellshort
                    else if (PositionSignal[3] > 0)
                    {
                        SellShortMktOrder.Send(ContractsTracker[3]);

                        PositionSignal[3] = 0;
                        ContractsTracker[3] = 0;
                        idx++;
                        continue;
                    }

                    else
                    {
                        // order rejected
                        idx++;
                        continue;
                    }
                }
                else if (strategyMP < 0)
                {
                    // sequence order -> sellshort -> buytocover -> buy
                    // check if there are buy orders

                    // check if there are sellshort
                    if (PositionSignal[3] > 0)
                    {
                        SellShortMktOrder.Send(ContractsTracker[3]);

                        PositionSignal[3] = 0;
                        ContractsTracker[3] = 0;
                        idx++;
                        continue;
                    }

                    // check for buy to cover
                    else if (PositionSignal[4] > 0)
                    {
                        // selling more than current position 
                        if (ContractsTracker[4] > strategyMP)
                            BuyCoverMktOrder.Send(strategyMP);
                        else
                            BuyCoverMktOrder.Send(ContractsTracker[4]);

                        PositionSignal[4] = 0;
                        ContractsTracker[4] = 0;
                        idx++;
                        continue;
                    }

                    // check for reverse buy orders
                    else if (PositionSignal[1] > 0)
                    {
                        BuyMktOrder.Send(ContractsTracker[1]);
                        PositionSignal[1] = 0;
                        ContractsTracker[1] = 0;
                        idx++;
                        continue;
                    }

                    else
                    {
                        // order rejected
                        idx++;
                        continue;
                    }
                }
                else // Market Position flat
                {
                    // order sequence buy or sellshort
                    // buy
                    if (PositionSignal[1] > 0)
                    {
                        BuyMktOrder.Send(ContractsTracker[1]);
                        PositionSignal[1] = 0;
                        ContractsTracker[1] = 0;
                        idx++;
                        continue;
                    }

                    // sell short
                    else if (PositionSignal[3] > 0)
                    {
                        SellShortMktOrder.Send(ContractsTracker[3]);

                        PositionSignal[3] = 0;
                        ContractsTracker[3] = 0;
                        idx++;
                        continue;
                    }

                    else
                    {
                        // order rejected
                        idx++;
                        continue;
                    }
                }
            }
        }

        private void GetStrategyOrders()
        {
            // check if there are any orders.
            if (ListOfOrderBars.Count == 0) return;

            // Last in first out, reverse the list.
            foreach (BarData bar in ListOfOrderBars.Reverse<BarData>())
            {
                if (Bars.Time[0] == bar.BarTime)
                {
                    // buy order
                    if (bar.OrderType == BuyOrderText)
                    {
                        PositionSignal[1] = 1;
                        ContractsTracker[1] += bar.Contracts;
                    }

                    // sell order
                    else if (bar.OrderType == SellOrderText)
                    {
                        PositionSignal[2] = 1;
                        ContractsTracker[2] += bar.Contracts;
                    }

                    // sell short order
                    else if (bar.OrderType == SellShortOrderText)
                    {
                        PositionSignal[3] = 1;
                        ContractsTracker[3] = bar.Contracts;
                    }

                    // buy to cover order
                    else if (bar.OrderType == BuyToCoverOrderText)
                    {
                        PositionSignal[4] = 1;
                        ContractsTracker[4] = bar.Contracts;
                    }
                }
            }
        }

        private void GetAllOrdersFromChart()
        {
            string orderType;
            int contracts;
            ChartPoint chartLoc;

            foreach (ITextObject text in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
            {
                string[] separator = { " " };
                string[] temp = text.Text
                    .ToUpper()
                    .Split(separator, StringSplitOptions.RemoveEmptyEntries);

                // split the string to get order and contracts.
                orderType = temp[0];
                contracts = int.Parse(temp[1]);

                Print("Order: " + orderType);
                Print("contracts: " + contracts);

                // get the location of the order bar.
                chartLoc = text.Location;
                int barNumber = (int)chartLoc.BarNumber;
                DateTime barTime = chartLoc.Time;

                // number of bars back from lastbar on chart.
                //int actualBarNumber = (int)chartLoc.BarNumber + ExecInfo.MaxBarsBack;
                //int barsAgo = Bars.FullSymbolData.Count - actualBarNumber;
                //double barHigh = Bars.FullSymbolData.High[barsAgo];
                //double barLow = Bars.FullSymbolData.Low[barsAgo];
                //double halfRange = (barHigh - barLow) * 0.5;

                // create and add orders to list of bardata.
                BarData barsData = new BarData(orderType, contracts, barTime);
                ListOfOrderBars.Add(barsData);

                // re-arrange and re-color order text.
                if (orderType == BuyOrderText)
                {
                    text.Color = Color.LimeGreen;
                }
                else if (orderType == SellOrderText)
                {
                    text.Color = Color.DarkGreen;
                }
                else if (orderType == SellShortOrderText)
                {
                    text.Color = Color.Red;
                }
                else if (orderType == BuyToCoverOrderText)
                {
                    text.Color = Color.DarkRed;
                }
                else
                {
                    text.Color = Color.White;
                    text.Text = "Invalid";
                }
            }
        }

        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            if (arg.keys == Keys.Shift && arg.buttons == MouseButtons.Left)
            {
                ListOfOrderBars.Clear();
                ExecControl.Recalculate();
            }
        }
        protected override void StopCalc()
        {
            Output.Clear();
        }

        class BarData
        {
            public string OrderType { get; set; }
            public int BarsAgo { get; set; }
            public int Contracts { get; set; }
            public DateTime BarTime { get; set; }

            public BarData(string order, int contracts, DateTime time)
            {
                OrderType = order;
                Contracts = contracts;
                BarTime = time;
            }
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            // Develop using methods, to know who is the caller.
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