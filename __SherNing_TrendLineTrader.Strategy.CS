using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using PowerLanguage.TradeManager;
using ATCenterProxy.interop;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    //==========================================================================================
    // Name           : TrendLineTrader
    // Description    : Using toolbar and trendlines to enter a trade
    // Version        : v.1.0
    // Date Created   : 10 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v.1.0
     *  - Toolbar Support
     *  - Upon restart, trendline remains in situ
     *  - Use strategy properties to set limits to trade.
     *  ----------------------------------- Version History -----------------------------------
     

     *  ---------------------------------------- Notes ----------------------------------------
     *   - Learning to manage real-time data.
     *   - Allow partial profit taking, separate text box
     *   - Always allow send orders
     *   - Do not make changes to code when doing LIVE testing, it will crash MC.
     *   - Trade manager is to manage Broker Orders.
     *  ---------------------------------------- Notes ----------------------------------------
     */

    [IOGMode(IOGMode.Enabled), RecoverDrawings(false), MouseEvents(true)]
    [AllowSendOrdersAlways(true)]
    [ExitFromOneEntryOnce(ExitFromOneEntryOnce.FillAndKillExitOrder)]
    public class __SherNing_TrendLineTrader : SignalObject
    {
        // class inputs
        [Input] public string TrendLineTraderSettings { get; set; }
        [Input] public int EntryLineID { get; set; }
        [Input] public int StopLossLineID { get; set; }
        [Input] public int ProfitLineID { get; set; }
        [Input] public bool LockActiveEntryLine { get; set; }
        [Input] public bool LockActiveStopLossLine { get; set; }
        [Input] public bool LockActiveProfitLine { get; set; }
        [Input] public string TradingAccountSettings { get; set; }
        [Input] public double AccountSize { get; set; }
        [Input] public double RiskPerTrade { get; set; }
        [Input] public int MaxContracts { get; set; }

        // class fields
        private const int FxStandardLot = 100000;
        private IOrderMarket BuyMktOrder, ShortMktOrder, SellMktOrder, CoverMktOrder;
        private IOrderPriced BuyLmtOrder, ShortLmtOrder, SellLmtOrder, CoverLmtOrder;
        private IOrderPriced BuyStpOrder, ShortStpOrder, SellStpOrder, CoverStpOrder;
        private bool ActiveBuyLmt, ActiveSellLmt, ActiveShortLmt, ActiveCoverLmt;
        private bool ActiveBuyStp, ActiveSellStp, ActiveShortStp, ActiveCoverStp;
        private VariableSeries<int> MarketPosition;
        private DeveloperTools Tools;
        private AutoPositionSizingFX AutoPos;
        public __SherNing_TrendLineTrader (object _ctx) : base(_ctx)
        {
            // header
            TrendLineTraderSettings = "Trend Line Trader Settings Menu";

            // Trend line ID
            EntryLineID = 1;
            StopLossLineID = 2;
            ProfitLineID = 3;
            LockActiveEntryLine = true;
            LockActiveStopLossLine = true;
            LockActiveProfitLine = true;

            // trading account settings
            TradingAccountSettings = "Trading Account Settings Menu";
            AccountSize = 100000;
            RiskPerTrade = 1;
            MaxContracts = 1000000;
        }
        protected override void Create()
        {
            // market Orders
            BuyMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.UserSpecified, "Buy_Mkt", EOrderAction.Buy));

            ShortMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.UserSpecified, "Short_Mkt", EOrderAction.SellShort));

            SellMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.Default, "Sell_Mkt", EOrderAction.Sell));

            CoverMktOrder = OrderCreator.MarketNextBar(
                new SOrderParameters(Contracts.Default, "Cover_Mkt", EOrderAction.BuyToCover));

            // limit orders, allow partial exits
            BuyLmtOrder = OrderCreator.Limit(
                new SOrderParameters(Contracts.UserSpecified, "Buy_Lmt", EOrderAction.Buy));

            SellLmtOrder = OrderCreator.Limit(
               new SOrderParameters(Contracts.UserSpecified, "Sell_Lmt", EOrderAction.Sell, OrderExit.Total));

            ShortLmtOrder = OrderCreator.Limit(
               new SOrderParameters(Contracts.UserSpecified, "Short_Lmt", EOrderAction.SellShort));

            CoverLmtOrder = OrderCreator.Limit(
               new SOrderParameters(Contracts.UserSpecified, "Cover_Lmt", EOrderAction.BuyToCover, OrderExit.Total));

            // stop loss orders, exit all
            BuyStpOrder = OrderCreator.Stop(
                new SOrderParameters(Contracts.UserSpecified, "Buy_Stp", EOrderAction.Buy));

            SellStpOrder = OrderCreator.Stop(
            new SOrderParameters(Contracts.Default, "Sell_Stp", EOrderAction.Sell));

            ShortStpOrder = OrderCreator.Stop(
            new SOrderParameters(Contracts.UserSpecified, "Short_Stp", EOrderAction.SellShort));

            CoverStpOrder = OrderCreator.Stop(
            new SOrderParameters(Contracts.Default, "Cover_Stp", EOrderAction.BuyToCover));

            MarketPosition = new VariableSeries<int>(this);

            Tools = new DeveloperTools(this);
            AutoPos = new AutoPositionSizingFX(this);
        }
        protected override void StartCalc()
        {
            // initialize Toolstrip
            ToolStripStartCalc();

            // get manual trend lines from chart
            GetTrendLineFromChart();

            // check if all the lines are available
            AreTrendLinesFound();

            // reset all orders
            ActiveBuyLmt = ActiveSellLmt = ActiveShortLmt = ActiveCoverLmt = false;
            ActiveBuyStp = ActiveSellStp = ActiveShortStp = ActiveCoverStp = false;
            PrevMarketPosition = 0;

            // auto pos properties
            AutoPos._AccountSize = AccountSize;
            AutoPos._Risk = RiskPerTrade;
            AutoPos._StepSize = 20000;
            AutoPos._MinimumContracts = 20000;

            // use strategy properties to control.
            AutoPos._MaximumContracts = MaxContracts;

            // status: ok
        }

        protected override void CalcBar()
        {
            // Calculate only on last bar of chart
            if (Bars.LastBarOnChart)
            {
                // Check status: 
                TrendLineStatus();
                DataStatus();
                StrategyOrderStatus();

                // IOG enabled: tick calculation
                GetTrendValues();

                // Get risk reward
                GetRiskReward();

                // Get contracts for auto
                GetContracts();

                // Get profit and losses
                GetProfitAndLosses();

                // Get market position
                GetMarketPosition();

                // Activation: 
                EntryLineActivation();
                StopLossLineActivation();
                ProfitLineActivation();

                // Send:
                SendStrategyOrders();

                // Call CalcBar() every second, during quiet sessions.
                ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
            }
        }

        #region Trend Line Trading
        private ITrendLineObject TL_Entry, TL_Profit, TL_StopLoss;
        private bool FoundEntryLine, FoundStopLossLine, FoundProfitLine;
        private int PrevMarketPosition;

        // entry will be based on color of trend line
        private Color ColorEntryLine = Color.DarkGray;
        private Color ColorStopLossLine = Color.LightGoldenrodYellow;
        private Color ColorProfitLine = Color.DarkCyan;
        private void GetTrendLineFromChart()
        {
            // reset to default
            FoundEntryLine = FoundStopLossLine = FoundProfitLine = false;

            // obtain trend lines from chart.
            foreach (ITrendLineObject trendLine in
                DrwTrendLine.GetTrendLineObjects(EDrawingSource.CurrentTechOrManual))
            {
                if (trendLine.ID == EntryLineID)
                {
                    TL_Entry = trendLine;
                }
                else if (trendLine.ID == StopLossLineID)
                {
                    TL_StopLoss = trendLine;
                }
                else if (trendLine.ID == ProfitLineID)
                {
                    TL_Profit = trendLine;
                }
            }

            if (TL_Entry != null)
            {
                FoundEntryLine = true;
                TL_Entry.Color = ColorEntryLine;
            }

            if (TL_StopLoss != null)
            {
                FoundStopLossLine = true;
                TL_StopLoss.Color = ColorStopLossLine;
            }

            if (TL_Profit != null)
            {
                FoundProfitLine = true;
                TL_Profit.Color = ColorProfitLine;
            }
        }

        // Check system status
        private void AreTrendLinesFound()
        {
            if (FoundEntryLine == false)
            {
                MessageBox.Show("Entry Line not found", Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecControl.Abort("Please check, and reload strategy");
            }

            if (FoundStopLossLine == false)
            {
                MessageBox.Show("Stop Loss Line not found", Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecControl.Abort("Please check, and reload strategy");
            }

            if (FoundProfitLine == false)
            {
                MessageBox.Show("Profit Line not found", Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecControl.Abort("Please check, and reload strategy");
            }
        }
        private void TrendLineStatus()
        {
            if (TL_Entry == null)
            {
                MessageBox.Show("Entry Line not found", Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecControl.Abort("Please check, and reload strategy");
            }

            if (TL_StopLoss == null)
            {
                MessageBox.Show("Stop Loss Line not found", Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecControl.Abort("Please check, and reload strategy");
            }

            if (TL_Profit == null)
            {
                MessageBox.Show("Profit Line not found", Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExecControl.Abort("Please check, and reload strategy");
            }
        }
        private void DataStatus()
        {
            if (Environment.IsRealTimeCalc)
            {
                BtnStatus.Text = "Online";
                BtnStatus.BackColor = Color.ForestGreen;
                BtnStatus.ForeColor = Color.White;
            }
            else
            {
                BtnStatus.Text = "Offline";
                BtnStatus.BackColor = Color.Red;
                BtnStatus.ForeColor = Color.White;
            }
        }
        private void StrategyOrderStatus()
        {
            // track market position
            int marketPosition = StrategyInfo.MarketPosition;
            MarketPosition.Value = marketPosition;

            // current position is flat
            if (CurrentPosition.Side == EMarketPositionSide.Flat && marketPosition == 0)
            {
                // sell order
                if (PrevMarketPosition > 0)
                {
                    // profit
                    if (ActiveSellLmt == true)
                    {
                        ActiveSellLmt = false;
                        BtnProfitPrice.BackColor = DeactivationColor;
                        BtnProfitPrice.ForeColor = Color.White;
                        TogProfit = true;
                        ProfitPriceConfirm = false;
                    }

                    // stop loss
                    if (ActiveSellStp == true)
                    {
                        ActiveSellStp = false;
                        BtnStopLossPrice.BackColor = DeactivationColor;
                        BtnStopLossPrice.ForeColor = Color.White;
                        TogStopLoss = true;
                        StopLossPriceConfirm = false;
                    }

                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = Color.DarkGray;
                    BtnDropMenu.ForeColor = Color.White;
                }
                else if (PrevMarketPosition < 0)
                {
                    // profit
                    if (ActiveCoverLmt == true)
                    {
                        ActiveCoverLmt = false;
                        BtnProfitPrice.BackColor = DeactivationColor;
                        BtnProfitPrice.ForeColor = Color.White;
                        TogProfit = true;
                        ProfitPriceConfirm = false;
                    }

                    // stoploss
                    if (ActiveCoverStp == true)
                    {
                        ActiveCoverStp = false;
                        BtnStopLossPrice.BackColor = DeactivationColor;
                        BtnStopLossPrice.ForeColor = Color.White;
                        TogStopLoss = true;
                        StopLossPriceConfirm = false;
                    }

                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = Color.DarkGray;
                    BtnDropMenu.ForeColor = Color.White;
                }

                PrevMarketPosition = marketPosition;
            }

            // compare mp with prev mp
            else if (CurrentPosition.Side == EMarketPositionSide.Long && marketPosition > 0)
            {
                // a buy position was filled
                if (marketPosition - PrevMarketPosition > 0)
                {
                    // either or
                    if (ActiveBuyLmt == true)
                    {
                        ActiveBuyLmt = false;
                        BtnEntryPrice.BackColor = Color.DarkGray;
                        BtnEntryPrice.ForeColor = Color.White;
                        EntryPriceConfirm = false;
                    }
                    else if (ActiveBuyStp == true)
                    {
                        ActiveBuyStp = false;
                        BtnEntryPrice.BackColor = Color.DarkGray;
                        BtnEntryPrice.ForeColor = Color.White;
                        EntryPriceConfirm = false;
                    }

                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = Color.DarkGray;
                    BtnDropMenu.ForeColor = Color.White;
                }

                // a sell position was filled
                else if (marketPosition - PrevMarketPosition < 0)
                {
                    // profit
                    if (ActiveSellLmt == true)
                    {
                        ActiveSellLmt = false;
                        BtnProfitPrice.BackColor = DeactivationColor;
                        BtnProfitPrice.ForeColor = Color.White;
                        TogProfit = true;
                        ProfitPriceConfirm = false;
                    }

                    // stop loss
                    if (ActiveSellStp == true)
                    {
                        ActiveSellStp = false;
                        BtnStopLossPrice.BackColor = DeactivationColor;
                        BtnStopLossPrice.ForeColor = Color.White;
                        TogStopLoss = true;
                        StopLossPriceConfirm = false;
                    }

                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = Color.DarkGray;
                    BtnDropMenu.ForeColor = Color.White;
                }

                PrevMarketPosition = marketPosition;
            }

            // handle short position
            else if (CurrentPosition.Side == EMarketPositionSide.Short && marketPosition < 0)
            {
                // a short position was filled -1,0,1
                if (Math.Abs(marketPosition) - Math.Abs(PrevMarketPosition) > 0)
                {
                    // either or
                    if (ActiveShortLmt == true)
                    {
                        ActiveShortLmt = false;
                        BtnEntryPrice.BackColor = Color.DarkGray;
                        BtnEntryPrice.ForeColor = Color.White;
                        EntryPriceConfirm = false;
                    }
                    else if (ActiveShortStp == true)
                    {
                        ActiveShortStp = false;
                        BtnEntryPrice.BackColor = Color.DarkGray;
                        BtnEntryPrice.ForeColor = Color.White;
                        EntryPriceConfirm = false;
                    }

                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = Color.DarkGray;
                    BtnDropMenu.ForeColor = Color.White;
                }
                else if (Math.Abs(marketPosition) - Math.Abs(PrevMarketPosition) < 0)
                {
                    // profit
                    if (ActiveCoverLmt == true)
                    {
                        ActiveCoverLmt = false;
                        BtnProfitPrice.BackColor = DeactivationColor;
                        BtnProfitPrice.ForeColor = Color.White;
                        TogProfit = true;
                        ProfitPriceConfirm = false;
                    }

                    // stop loss
                    if (ActiveCoverStp == true)
                    {
                        ActiveCoverStp = false;
                        BtnStopLossPrice.BackColor = DeactivationColor;
                        BtnStopLossPrice.ForeColor = Color.White;
                        TogStopLoss = true;
                        StopLossPriceConfirm = false;
                    }

                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = Color.DarkGray;
                    BtnDropMenu.ForeColor = Color.White;
                }

                PrevMarketPosition = marketPosition;
            }

            // handle trade orders
            if (ActiveBuyLmt == false || ActiveBuyStp == false || ActiveShortLmt == false || ActiveShortStp == false)
            {
                TL_Entry.Locked = false;
                TL_Entry.ExtRight = false;
                TL_Entry.Color = ColorEntryLine;
            }

            if (ActiveSellStp == false || ActiveCoverStp == false)
            {
                TL_StopLoss.Locked = false;
                TL_StopLoss.ExtRight = false;
                TL_StopLoss.Color = ColorStopLossLine;
            }

            if (ActiveSellLmt == false || ActiveCoverLmt == false)
            {
                TL_Profit.Locked = false;
                TL_Profit.ExtRight = false;
                TL_Profit.Color = ColorProfitLine;
            }
        }

        // Strategy state
        private double EntryPrice, StopLossPrice, ProfitPrice, RiskReward;
        private int TradeContracts, ExitContracts;
        private bool EntryPriceConfirm, StopLossPriceConfirm, ProfitPriceConfirm;

        private void GetTrendValues()
        {
            // get values only on last bar on chart.
            if (Bars.LastBarOnChart == false) return;

            // get trend line values, round to 5 digits
            EntryPrice = Tools.RoundDouble(TL_Entry.PriceValue(Bars.TimeValue));
            StopLossPrice = Tools.RoundDouble(TL_StopLoss.PriceValue(Bars.TimeValue));
            ProfitPrice = Tools.RoundDouble(TL_Profit.PriceValue(Bars.TimeValue));

            // update trend line price display
            BtnEntryPrice.Text = EntryPrice.ToString();
            BtnStopLossPrice.Text = StopLossPrice.ToString();
            BtnProfitPrice.Text = ProfitPrice.ToString();
        }
        private void GetRiskReward()
        {
            // cache risk reward, use RR to determine entry is legit
            double riskReward = 0;

            if (MarketPosition[0] == 0)
            {
                // long
                if (BtnDropMenu.Text == TxtBuyLmt || BtnDropMenu.Text == TxtBuyStp)
                {
                    if (EntryPrice > StopLossPrice && EntryPrice < ProfitPrice)
                        riskReward = Tools.RiskReward(EntryPrice, StopLossPrice, ProfitPrice);
                    else
                        riskReward = 0;
                }

                // short
                else if (BtnDropMenu.Text == TxtShortLmt || BtnDropMenu.Text == TxtShortStp)
                {
                    if (EntryPrice < StopLossPrice && EntryPrice > ProfitPrice)
                        riskReward = Tools.RiskReward(EntryPrice, StopLossPrice, ProfitPrice);
                    else
                        riskReward = 0;
                }

                // flat
                else if (BtnDropMenu.Text == TxtFlat)
                {
                    riskReward = 0;
                }
            }

            // in a trade
            else
            {
                // if stop loss and profit is enabled
                if (StopLossPriceConfirm == true && ProfitPriceConfirm == true)
                {
                    riskReward = Tools.RiskReward(StrategyInfo.AvgEntryPrice, StopLossPrice, ProfitPrice);
                }
            }

            // display risk reward
            RiskReward = riskReward;

            // for aesthetic purpose
            if (RiskReward == 0)
            {
                BtnRiskReward.Text = "0.00";
            }
            else
            {
                BtnRiskReward.Text = RiskReward.ToString();

                // update risk reward color temperature, red to blue
                if (RiskReward < 1.0)
                {
                    BtnRiskReward.BackColor = Color.Red;
                    BtnRiskReward.ForeColor = Color.White;
                }
                else if (RiskReward >= 1.0 && RiskReward < 2.0)
                {
                    BtnRiskReward.BackColor = Color.Purple;
                    BtnRiskReward.ForeColor = Color.White;
                }
                else if (RiskReward >= 2.0 && RiskReward < 3.0)
                {
                    BtnRiskReward.BackColor = Color.DarkBlue;
                    BtnRiskReward.ForeColor = Color.White;
                }
                else if (RiskReward >= 3.0)
                {
                    BtnRiskReward.BackColor = Color.Blue;
                    BtnRiskReward.ForeColor = Color.White;
                }
            }
        }
        private void GetContracts()
        {
            // calculate only on last bar on chart
            if (Bars.LastBarOnChart == false) return;

            // Auto vs Manual 
            int contracts = 0;

            // if auto mode
            if (BtnRiskMode.Text == "Auto")
            {
                // long
                if (BtnDropMenu.Text == TxtBuyLmt ||
                    BtnDropMenu.Text == TxtBuyStp ||
                    BtnDropMenu.Text == TxtBuyMkt)
                {
                    // safety check
                    if (EntryPrice > StopLossPrice && EntryPrice < ProfitPrice)
                    {
                        contracts = AutoPos.Calculate(EntryPrice, StopLossPrice);
                    }
                    else
                        contracts = 0;
                }

                // short
                else
                if (BtnDropMenu.Text == TxtShortLmt ||
                    BtnDropMenu.Text == TxtShortStp ||
                    BtnDropMenu.Text == TxtShortMkt)
                {
                    // safety check
                    if (EntryPrice < StopLossPrice && EntryPrice > ProfitPrice)
                        contracts = AutoPos.Calculate(EntryPrice, StopLossPrice);
                    else
                        contracts = 0;
                }

                // flat
                else if (BtnDropMenu.Text == TxtFlat)
                {
                    contracts = 0;
                }
            }

            else if (BtnRiskMode.Text == "Manual")
            {
                contracts = int.Parse(TbEntry.Text);
            }

            // cache to field for order generation
            TradeContracts = contracts;

            // display contracts
            TbEntry.Text = TradeContracts.ToString();
        }
        private void GetProfitAndLosses()
        {
            // calculate last bar on chart
            if (Bars.LastBarOnChart == false) return;

            // real-time profits and losses vs potential profits
            double profits = 0;
            double pipValue = AutoPos._PipValue;
            double onePip = AutoPos._OnePip;
            double contracts = TradeContracts / FxStandardLot;

            // market position flat
            if (MarketPosition[0] == 0)
            {
                // potential profits
                // long
                if (BtnDropMenu.Text == TxtBuyLmt || BtnDropMenu.Text == TxtBuyStp)
                {
                    // safety check
                    if (EntryPrice > StopLossPrice && EntryPrice < ProfitPrice && TradeContracts > 0)
                        profits = contracts * ((ProfitPrice - EntryPrice) / onePip) * pipValue;
                    else
                        profits = 0;
                }

                // short
                else if (BtnDropMenu.Text == TxtShortLmt || BtnDropMenu.Text == TxtShortStp)
                {
                    // safety check
                    if (EntryPrice < StopLossPrice && EntryPrice > ProfitPrice)
                        profits = contracts * (-(ProfitPrice - EntryPrice) / onePip) * pipValue;
                    else
                        profits = 0;
                }

                // flat
                else if (BtnDropMenu.Text == TxtFlat)
                {
                    profits = 0;
                }

                // update potential profits based on risk reward
                if (RiskReward == 0)
                {
                    BtnPnL.BackColor = Color.Black;
                    BtnPnL.ForeColor = Color.White;
                }
                else if (RiskReward < 1.0)
                {
                    BtnPnL.BackColor = Color.Red;
                    BtnPnL.ForeColor = Color.White;
                }
                else if (RiskReward >= 1.0 && RiskReward < 2.0)
                {
                    BtnPnL.BackColor = Color.Purple;
                    BtnPnL.ForeColor = Color.White;
                }
                else if (RiskReward >= 2.0 && RiskReward < 3.0)
                {
                    BtnPnL.BackColor = Color.DarkBlue;
                    BtnPnL.ForeColor = Color.White;
                }
                else if (RiskReward >= 3.0)
                {
                    BtnPnL.BackColor = Color.Blue;
                    BtnPnL.ForeColor = Color.White;
                }
            }

            // market postion is non-zero
            else
            {
                profits = StrategyInfo.OpenEquity;

                // losses red
                if (profits <= 0)
                {
                    BtnPnL.BackColor = Color.Red;
                    BtnPnL.ForeColor = Color.White;
                }

                // profits green
                else
                {
                    BtnPnL.BackColor = Color.ForestGreen;
                    BtnPnL.ForeColor = Color.White;
                }
            }

            // color of pnl is linked
            BtnPnL.Text = profits.ToString("C2");
        }
        private void GetMarketPosition()
        {
            // track the open position.
            if (ProfitPriceConfirm == false)
            {
                TbExit.Text = CurrentPosition.OpenLots.ToString();
            }
            else
            {
                ExitContracts = int.Parse(TbExit.Text);

                // ensure minimum contracts.
                if (ExitContracts < 20000)
                    ExitContracts = 20000;
            }

        }
        private void EntryLineActivation()
        {
            Color entryColor = BtnDropMenu.BackColor;
            DialogResult results;

            if (EntryPriceConfirm == false)
            {
                ActiveBuyLmt = ActiveBuyStp = false;
                ActiveShortLmt = ActiveShortStp = false;
                return;
            }

            // stoploss check
            if (StopLossPriceConfirm == true)
            {
                // buy limit or stop order
                if (entryColor == BtnBuyLmt.BackColor || entryColor == BtnBuyStp.BackColor)
                {
                    // ensure that entry > stop loss
                    if (EntryPrice <= StopLossPrice)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice <= Stoploss Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            // deactivate stop loss line
                            BtnStopLossPrice.BackColor = DeactivationColor;
                            BtnStopLossPrice.ForeColor = Color.White;
                            TogStopLoss = true;
                            StopLossPriceConfirm = false;
                            return;
                        }
                    }
                }

                // short limit or stop order
                else if (entryColor == BtnShortLmt.BackColor || entryColor == BtnShortStp.BackColor)
                {
                    if (EntryPrice >= StopLossPrice)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice >= Stoploss Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            // deactivate stop loss line
                            BtnStopLossPrice.BackColor = DeactivationColor;
                            BtnStopLossPrice.ForeColor = Color.White;
                            TogStopLoss = true;
                            StopLossPriceConfirm = false;
                            return;
                        }
                    }
                }
            }

            // profit check
            if (ProfitPriceConfirm == true)
            {
                // buy limit or stop order
                if (entryColor == BtnBuyLmt.BackColor || entryColor == BtnBuyStp.BackColor)
                {
                    if (EntryPrice >= ProfitPrice)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice >= Profit Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            BtnProfitPrice.BackColor = DeactivationColor;
                            BtnProfitPrice.ForeColor = Color.White;
                            TogProfit = true;
                            ProfitPriceConfirm = false;
                            return;
                        }
                    }
                }

                // short limit or stop order
                else if (entryColor == BtnShortLmt.BackColor || entryColor == BtnShortStp.BackColor)
                {
                    if (EntryPrice <= ProfitPrice)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice <= Profit Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            BtnProfitPrice.BackColor = DeactivationColor;
                            BtnProfitPrice.ForeColor = Color.White;
                            TogProfit = true;
                            ProfitPriceConfirm = false;
                            return;
                        }
                    }
                }
            }

            // check if order will be place immediately
            if (EntryPriceConfirm == true)
            {
                // buy limit or stop order
                if (entryColor == BtnBuyLmt.BackColor)
                {
                    if (EntryPrice >= Bars.CloseValue)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice >= Closing Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            BtnEntryPrice.BackColor = Color.DarkGray;
                            BtnEntryPrice.ForeColor = Color.White;
                            EntryPriceConfirm = false;
                            TogEntry = true;

                            BtnDropMenu.Text = TxtFlat;
                            BtnDropMenu.BackColor = Color.DarkGray;
                            BtnDropMenu.ForeColor = Color.White;
                            return;
                        }
                    }
                }
                else if (entryColor == BtnBuyStp.BackColor)
                {
                    if (EntryPrice <= Bars.CloseValue)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice <= Closing Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            BtnEntryPrice.BackColor = Color.DarkGray;
                            BtnEntryPrice.ForeColor = Color.White;
                            EntryPriceConfirm = false;
                            TogEntry = true;

                            BtnDropMenu.Text = TxtFlat;
                            BtnDropMenu.BackColor = Color.DarkGray;
                            BtnDropMenu.ForeColor = Color.White;
                            return;
                        }
                    }
                }

                // short limit
                else if (entryColor == BtnShortLmt.BackColor)
                {
                    if (EntryPrice <= Bars.CloseValue)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice <= Closing Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            BtnEntryPrice.BackColor = Color.DarkGray;
                            BtnEntryPrice.ForeColor = Color.White;
                            EntryPriceConfirm = false;
                            TogEntry = true;

                            BtnDropMenu.Text = TxtFlat;
                            BtnDropMenu.BackColor = Color.DarkGray;
                            BtnDropMenu.ForeColor = Color.White;
                            return;
                        }
                    }
                }
                else if (entryColor == BtnShortStp.BackColor)
                {
                    if (EntryPrice >= Bars.CloseValue)
                    {
                        results = MessageBox.Show("Warning ! EntryPrice >= Closing Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            BtnEntryPrice.BackColor = Color.DarkGray;
                            BtnEntryPrice.ForeColor = Color.White;
                            EntryPriceConfirm = false;
                            TogEntry = true;

                            BtnDropMenu.Text = TxtFlat;
                            BtnDropMenu.BackColor = Color.DarkGray;
                            BtnDropMenu.ForeColor = Color.White;
                            return;
                        }
                    }
                }
            }

            // status check: ok

            // buy limit order and order not filled
            if (entryColor == BtnBuyLmt.BackColor && EntryPriceConfirm == true)
            {
                ActiveBuyLmt = true;

                TL_Entry.Color = entryColor;
                if (LockActiveEntryLine == true)
                {
                    TL_Entry.Locked = true;
                    TL_Entry.ExtRight = true;
                }
            }
            else if (entryColor == BtnBuyStp.BackColor && EntryPriceConfirm == true)
            {
                ActiveBuyStp = true;

                TL_Entry.Color = entryColor;
                if (LockActiveEntryLine == true)
                {
                    TL_Entry.Locked = true;
                    TL_Entry.ExtRight = true;
                }
            }
            else if (entryColor == BtnShortLmt.BackColor && EntryPriceConfirm == true)
            {
                ActiveShortLmt = true;

                TL_Entry.Color = entryColor;
                if (LockActiveEntryLine == true)
                {
                    TL_Entry.Locked = true;
                    TL_Entry.ExtRight = true;
                }
            }
            else if (entryColor == BtnShortStp.BackColor && EntryPriceConfirm == true)
            {
                ActiveShortStp = true;

                TL_Entry.Color = entryColor;
                if (LockActiveEntryLine == true)
                {
                    TL_Entry.Locked = true;
                    TL_Entry.ExtRight = true;
                }
            }

            // none of the above
            else
            {

            }

        }
        private void StopLossLineActivation()
        {
            if (StopLossPriceConfirm == true)
            {
                DialogResult results;

                if (EntryPriceConfirm == true)
                {
                    // if current position is long.
                    if (MarketPosition[0] > 0 && CurrentPosition.Side == EMarketPositionSide.Long)
                    {
                        // ensure that entry price is greater than stop loss price.
                        if (EntryPrice > StopLossPrice)
                        {
                            ActiveSellStp = true;
                        }
                        else
                        {
                            results = MessageBox.Show("Warning: Entry price < Stop loss price",
                                Name, MessageBoxButtons.OKCancel);

                            if (results == DialogResult.Cancel)
                            {
                                ActiveSellStp = false;
                                BtnStopLossPrice.BackColor = DeactivationColor;
                                BtnStopLossPrice.ForeColor = Color.White;
                                TogStopLoss = true;
                                StopLossPriceConfirm = false;
                                return;
                            }

                            ActiveSellStp = true;
                        }
                    }
                    else if (MarketPosition[0] < 0 && CurrentPosition.Side == EMarketPositionSide.Short)
                    {
                        if (EntryPrice < StopLossPrice)
                        {
                            ActiveCoverStp = true;
                        }
                        else
                        {
                            results = MessageBox.Show("Warning: Entry price > Stop loss price",
                                Name, MessageBoxButtons.OKCancel);

                            if (results == DialogResult.Cancel)
                            {
                                ActiveCoverStp = false;
                                BtnStopLossPrice.BackColor = DeactivationColor;
                                BtnStopLossPrice.ForeColor = Color.White;
                                TogStopLoss = true;
                                StopLossPriceConfirm = false;
                                return;
                            }

                            ActiveCoverStp = true;
                        }
                    }
                }

                // check for immediate fill
                // if current position is long.
                if (MarketPosition[0] > 0 && CurrentPosition.Side == EMarketPositionSide.Long)
                {
                    // ensure that entry price is greater than stop loss price.
                    if (Bars.CloseValue > StopLossPrice)
                    {
                        ActiveSellStp = true;
                    }
                    else
                    {
                        results = MessageBox.Show("Warning: Close < Stop Loss Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            ActiveSellStp = false;
                            BtnStopLossPrice.BackColor = DeactivationColor;
                            BtnStopLossPrice.ForeColor = Color.White;
                            TogStopLoss = true;
                            StopLossPriceConfirm = false;
                            return;
                        }

                        ActiveSellStp = true;
                    }
                }
                else if (MarketPosition[0] < 0 && CurrentPosition.Side == EMarketPositionSide.Short)
                {
                    if (Bars.CloseValue < StopLossPrice)
                    {
                        ActiveCoverStp = true;
                    }
                    else
                    {
                        results = MessageBox.Show("Warning: Close > Stop Loss Price", Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            ActiveCoverStp = false;
                            BtnStopLossPrice.BackColor = DeactivationColor;
                            BtnStopLossPrice.ForeColor = Color.White;
                            TogStopLoss = true;
                            StopLossPriceConfirm = false;
                            return;
                        }

                        ActiveCoverStp = true;
                    }
                }

                if (LockActiveStopLossLine == true)
                {
                    TL_StopLoss.Locked = true;
                    TL_StopLoss.ExtRight = true;
                }
            }
        }
        private void ProfitLineActivation()
        {
            if (ProfitPriceConfirm == true)
            {
                DialogResult results;

                if (EntryPriceConfirm == true)
                {
                    // if current position is long.
                    if (MarketPosition[0] > 0 && CurrentPosition.Side == EMarketPositionSide.Long)
                    {
                        // ensure that entry price is greater than stop loss price.
                        if (EntryPrice < ProfitPrice)
                        {
                            ActiveSellLmt = true;
                        }
                        else
                        {
                            results = MessageBox.Show("Warning: Entry price > Profit Price",
                                Name, MessageBoxButtons.OKCancel);

                            if (results == DialogResult.Cancel)
                            {
                                ActiveSellLmt = false;
                                BtnProfitPrice.BackColor = DeactivationColor;
                                BtnProfitPrice.ForeColor = Color.White;
                                TogProfit = true;
                                ProfitPriceConfirm = false;
                                return;
                            }

                            ActiveSellLmt = true;
                        }
                    }
                    else if (MarketPosition[0] < 0 && CurrentPosition.Side == EMarketPositionSide.Short)
                    {
                        if (EntryPrice > ProfitPrice)
                        {
                            ActiveCoverLmt = true;
                        }
                        else
                        {
                            results = MessageBox.Show("Warning: Entry price < Profit Price", Name, MessageBoxButtons.OKCancel);

                            if (results == DialogResult.Cancel)
                            {
                                ActiveCoverLmt = false;
                                BtnProfitPrice.BackColor = DeactivationColor;
                                BtnProfitPrice.ForeColor = Color.White;
                                TogProfit = true;
                                ProfitPriceConfirm = false;
                                return;
                            }

                            ActiveCoverLmt = true;
                        }
                    }
                }

                // check for immediate fill

                // if current position is long.
                if (MarketPosition[0] > 0 && CurrentPosition.Side == EMarketPositionSide.Long)
                {
                    // ensure that entry price is greater than stop loss price.
                    if (Bars.CloseValue < ProfitPrice)
                    {
                        ActiveSellLmt = true;
                    }
                    else
                    {
                        results = MessageBox.Show("Warning: Close > Profit Price",
                            Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            ActiveSellLmt = false;
                            BtnProfitPrice.BackColor = DeactivationColor;
                            BtnProfitPrice.ForeColor = Color.White;
                            TogProfit = true;
                            ProfitPriceConfirm = false;
                            return;
                        }

                        ActiveSellLmt = true;
                    }
                }
                else if (MarketPosition[0] < 0 && CurrentPosition.Side == EMarketPositionSide.Short)
                {
                    if (Bars.CloseValue > ProfitPrice)
                    {
                        ActiveCoverLmt = true;
                    }
                    else
                    {
                        results = MessageBox.Show("Warning: Close < Profit Price", Name, MessageBoxButtons.OKCancel);

                        if (results == DialogResult.Cancel)
                        {
                            ActiveCoverLmt = false;
                            BtnProfitPrice.BackColor = DeactivationColor;
                            BtnProfitPrice.ForeColor = Color.White;
                            TogProfit = true;
                            ProfitPriceConfirm = false;
                            return;
                        }

                        ActiveCoverLmt = true;
                    }
                }


                if (LockActiveProfitLine == true)
                {
                    TL_Profit.Locked = true;
                    TL_Profit.ExtRight = true;
                }
            }
        }
        private void SendStrategyOrders()
        {
            // entry orders
            if (ActiveBuyLmt == true)
            {
                BuyLmtOrder.Send(EntryPrice, TradeContracts);
            }
            else if (ActiveBuyStp == true)
            {
                BuyStpOrder.Send(EntryPrice, TradeContracts);
            }
            else if (ActiveShortLmt == true)
            {
                ShortLmtOrder.Send(EntryPrice, TradeContracts);
            }
            else if (ActiveShortStp == true)
            {
                ShortStpOrder.Send(EntryPrice, TradeContracts);
            }

            // stop loss exit orders
            if (ActiveSellStp == true)
            {
                SellStpOrder.Send(StopLossPrice);
            }
            else if (ActiveCoverStp == true)
            {
                CoverStpOrder.Send(StopLossPrice);
            }

            // profit exit orders
            if (ActiveSellLmt == true)
            {
                SellLmtOrder.Send(ProfitPrice, ExitContracts);
            }
            else if (ActiveCoverLmt == true)
            {
                CoverLmtOrder.Send(ProfitPrice, ExitContracts);
            }

        }
        protected override void OnRecalcLastBarAfterEvent()
        {
            this.CalcBar();
        }
        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            if (arg.keys == Keys.Shift && arg.buttons == MouseButtons.Left)
            {
                //ExecControl.Recalculate();
            }
        }
        protected override void StopCalc()
        {
            Output.Clear();
        }
        #endregion

        #region ToolStrip Framework

        #region Toolstrip StartCalc
        // ---------------------------- StartCalc Start ---------------------------- //

        private bool IsToolStripCreated;
        private void ToolStripStartCalc()
        {
            // place this method in startcalc.
            if (IsToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(ToolStripMain);
                IsToolStripCreated = true;
            }
        }

        // ---------------------------- StartCalc End ---------------------------- //
        #endregion

        #region ToolStrip Main Logic
        // ---------------------------- Main Start ---------------------------- //
        private ToolStripButton BtnRiskMode;
        private ToolStripTextBox TbEntry, TbExit;
        private ToolStripDropDownButton BtnDropMenu;
        private ToolStripDropDown DropDownMenu;
        private ToolStripButton BtnBuyMkt, BtnBuyLmt, BtnBuyStp;
        private ToolStripButton BtnShortMkt, BtnShortLmt, BtnShortStp;
        private ToolStripButton BtnFlat, BtnCloseAll;
        private ToolStripButton BtnEntryPrice, BtnStopLossPrice, BtnProfitPrice;
        private ToolStripButton BtnStatus, BtnRiskReward, BtnPnL, BtnTbStatus;
        private ToolStripButton BtnArrangeLines;
        private List<ToolStripItem> ToolBarObjs;
        private void ToolStripCreate(ToolStrip tb)
        {
            // create a list to track functional toolbar objects
            ToolBarObjs = new List<ToolStripItem>();

            // Create Toolstrip objects here and in order of display.
            ToolStripLabel label1 = new ToolStripLabel();
            label1.Text = "Risk Mode: ";
            AddNewItemToToolStrip(tb, label1);

            BtnRiskMode = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnRiskMode);
            ToolBarObjs.Add(BtnRiskMode);

            ToolStripLabel label2 = new ToolStripLabel();
            label2.Text = "Entry Size: ";
            AddNewItemToToolStrip(tb, label2);

            TbEntry = new ToolStripTextBox();
            AddNewItemToToolStrip(tb, TbEntry);
            ToolBarObjs.Add(TbEntry);

            ToolStripLabel label3 = new ToolStripLabel();
            label3.Text = "Exit Size: ";
            AddNewItemToToolStrip(tb, label3);

            TbExit = new ToolStripTextBox();
            AddNewItemToToolStrip(tb, TbExit);
            ToolBarObjs.Add(TbExit);

            ToolStripLabel label4 = new ToolStripLabel();
            label4.Text = "Trade Mode: ";
            AddNewItemToToolStrip(tb, label4);

            BtnDropMenu = new ToolStripDropDownButton();
            DropDownMenu = new ToolStripDropDown();

            // add buttons to dropdown menu
            BtnBuyMkt = new ToolStripButton();
            BtnBuyLmt = new ToolStripButton();
            BtnBuyStp = new ToolStripButton();
            BtnShortMkt = new ToolStripButton();
            BtnShortLmt = new ToolStripButton();
            BtnShortStp = new ToolStripButton();
            BtnFlat = new ToolStripButton();
            BtnCloseAll = new ToolStripButton();

            // create collection of orders
            ToolStripItem[] orders = new ToolStripItem[]
            {
                BtnBuyMkt, BtnBuyLmt, BtnBuyStp,
                BtnShortMkt, BtnShortLmt, BtnShortStp,
                BtnFlat, BtnCloseAll
            };

            // add collection to drop down menu
            DropDownMenu.Items.AddRange(orders);

            // set the dropdown when clicked
            BtnDropMenu.DropDown = DropDownMenu;

            AddNewItemToToolStrip(tb, BtnDropMenu);
            ToolBarObjs.Add(BtnDropMenu);

            // entry price
            ToolStripLabel label5 = new ToolStripLabel();
            label5.Text = "Entry $: ";
            AddNewItemToToolStrip(tb, label5);

            BtnEntryPrice = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnEntryPrice);
            ToolBarObjs.Add(BtnEntryPrice);

            // stop loss price
            ToolStripLabel label6 = new ToolStripLabel();
            label6.Text = "Stop $: ";
            AddNewItemToToolStrip(tb, label6);

            BtnStopLossPrice = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnStopLossPrice);
            ToolBarObjs.Add(BtnStopLossPrice);

            // profit price
            ToolStripLabel label7 = new ToolStripLabel();
            label7.Text = "Profit $: ";
            AddNewItemToToolStrip(tb, label7);

            BtnProfitPrice = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnProfitPrice);
            ToolBarObjs.Add(BtnProfitPrice);

            // real-time
            ToolStripLabel label8 = new ToolStripLabel();
            label8.Text = "Real-Time: ";
            AddNewItemToToolStrip(tb, label8);

            BtnStatus = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnStatus);

            // risk reward
            ToolStripLabel label9 = new ToolStripLabel();
            label9.Text = "RiskRwd: ";
            AddNewItemToToolStrip(tb, label9);

            BtnRiskReward = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnRiskReward);

            // profit and loss
            ToolStripLabel label10 = new ToolStripLabel();
            label10.Text = "PnL: ";
            AddNewItemToToolStrip(tb, label10);

            BtnPnL = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnPnL);

            // status
            ToolStripLabel label11 = new ToolStripLabel();
            label11.Text = "Arrange: ";
            AddNewItemToToolStrip(tb, label11);

            BtnArrangeLines = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnArrangeLines);
            ToolBarObjs.Add(BtnArrangeLines);

            // lock status
            ToolStripLabel label12 = new ToolStripLabel();
            label12.Text = "Status: ";
            AddNewItemToToolStrip(tb, label12);

            BtnTbStatus = new ToolStripButton();
            AddNewItemToToolStrip(tb, BtnTbStatus);


        }

        // toggle
        private bool TogRisk, TogLock, TogEntry, TogStopLoss, TogProfit, TogLines;
        private bool TbEntryNonNum, TbExitNonNum;

        // for aesthetic purpose, to include the space.
        private string TxtBuyMkt = " Buy Mkt  ";
        private string TxtBuyLmt = " Buy Lmt  ";
        private string TxtBuyStp = " Buy Stp   ";
        private string TxtShortMkt = "Short Mkt";
        private string TxtShortLmt = "Short Lmt";
        private string TxtShortStp = "Short Stp ";
        private string TxtFlat = "     Flat     ";
        private string TxtCloseAll = "Close All ";
        private Color StopLossActivationColor = Color.Yellow;
        private Color ProfitActivationColor = Color.Cyan;
        private Color DeactivationColor = Color.DarkGray;
        private void ToolStripMain(ToolStrip tb)
        {
            // main toolstrip adjustments first
            tb.AutoSize = false;
            tb.Height = 100;
            tb.TabIndex = 0;

            // initialize tool strip objects
            ToolStripCreate(tb);

            #region Risk Mode
            BtnRiskMode.Text = "Auto";
            BtnRiskMode.BackColor = Color.Cyan;
            BtnRiskMode.ForeColor = Color.Black;
            TbEntry.Enabled = false;
            TogRisk = true;
            BtnRiskMode.Click += delegate
            {
                if (TogRisk == true)
                {
                    BtnRiskMode.Text = "Auto";
                    BtnRiskMode.BackColor = Color.Cyan;
                    BtnRiskMode.ForeColor = Color.Black;
                    TbEntry.Enabled = false;
                    TogRisk = false;
                }
                else
                {
                    BtnRiskMode.Text = "Manual";
                    BtnRiskMode.BackColor = Color.DarkMagenta;
                    BtnRiskMode.ForeColor = Color.White;
                    TbEntry.Enabled = true;
                    TogRisk = true;
                }
            };

            #endregion

            #region Entry Size

            TbEntry.Text = "100000";
            TbEntry.AutoSize = false;
            TbEntry.Width = 150;
            TbEntry.KeyPress += (sender, e) =>
            {
                if (TbEntryNonNum == true) e.Handled = true;
            };

            // Check if only numbers are entered into the textbox
            TbEntry.KeyDown += (sender, e) =>
            {
                TbEntryNonNum = false;

                // Determine whether the keystroke is a number from the top of the keyboard.
                if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
                {
                    // Determine whether the keystroke is a number from the keypad.
                    if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                    {
                        // Determine whether the keystroke is a backspace.
                        if (e.KeyCode != Keys.Back)
                        {
                            // A non-numerical keystroke was pressed.
                            // Set the flag to true and evaluate in KeyPress event.
                            TbEntryNonNum = true;
                        }
                    }
                }

                //If shift key was pressed, it's not a number.
                if (Control.ModifierKeys == Keys.Shift)
                    TbEntryNonNum = true;
            };
            #endregion

            #region Exit Size
            TbExit.Text = "0";
            TbExit.AutoSize = false;
            TbExit.Width = 150;
            TbExit.KeyPress += (sender, e) =>
            {
                if (TbExitNonNum == true) e.Handled = true;
            };

            // Check if only numbers are entered into the textbox
            TbExit.KeyDown += (sender, e) =>
            {
                TbExitNonNum = false;

                // Determine whether the keystroke is a number from the top of the keyboard.
                if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
                {
                    // Determine whether the keystroke is a number from the keypad.
                    if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                    {
                        // Determine whether the keystroke is a backspace.
                        if (e.KeyCode != Keys.Back)
                        {
                            // A non-numerical keystroke was pressed.
                            // Set the flag to true and evaluate in KeyPress event.
                            TbExitNonNum = true;
                        }
                    }
                }

                //If shift key was pressed, it's not a number.
                if (Control.ModifierKeys == Keys.Shift)
                    TbExitNonNum = true;
            };
            #endregion

            #region Drop Down Menu

            BtnDropMenu.Text = TxtFlat;
            BtnDropMenu.BackColor = Color.DarkGray;
            BtnDropMenu.ForeColor = Color.White;
            BtnDropMenu.DropDownDirection = ToolStripDropDownDirection.BelowRight;
            BtnDropMenu.ShowDropDownArrow = false;

            #endregion

            #region Buy Market
            BtnBuyMkt.Text = TxtBuyMkt;
            BtnBuyMkt.BackColor = Color.SeaGreen;
            BtnBuyMkt.ForeColor = Color.White;
            BtnBuyMkt.Click += delegate
            {
                BtnDropMenu.Text = TxtBuyMkt;
                BtnDropMenu.BackColor = BtnBuyMkt.BackColor;
                BtnDropMenu.ForeColor = BtnBuyMkt.ForeColor;

                // check contracts
                if (TradeContracts < 20000) return;

                // cache results
                DialogResult result;
                string message;

                // check if stop loss > entry price, which will result in immediate fill
                if (StopLossPriceConfirm == true && EntryPrice < StopLossPrice)
                {
                    result = MessageBox.Show("Warning: Stoploss > Entry Price.",
                        Name, MessageBoxButtons.OKCancel);

                    if (result == DialogResult.Cancel)
                    {
                        BtnStopLossPrice.BackColor = DeactivationColor;
                        BtnStopLossPrice.ForeColor = Color.White;
                        TogStopLoss = true;
                        StopLossPriceConfirm = false;
                        return;
                    }
                }

                // check if profit < entry price, which will result in immediate fill
                if (ProfitPriceConfirm == true && ProfitPrice < EntryPrice)
                {
                    result = MessageBox.Show("Warning: Profit < Entry Price.", Name, MessageBoxButtons.OKCancel);
                    if (result == DialogResult.Cancel)
                    {
                        BtnProfitPrice.BackColor = DeactivationColor;
                        BtnProfitPrice.ForeColor = Color.White;
                        TogProfit = true;
                        ProfitPriceConfirm = false;
                        return;
                    }
                }

                // check for marketposition
                if (CurrentPosition.Side == EMarketPositionSide.Short && CurrentPosition.OpenLots > 0)
                {
                    message = string.Format("Are you sure: Reverse Buy {0} contracts @ {1} ?",
                        TradeContracts, Bars.CloseValue);

                    result = MessageBox.Show(message, Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                }
                else
                {
                    message = string.Format("Are you sure: Buy {0} contracts @ {1} ?",
                        TradeContracts, Bars.CloseValue);

                    result = MessageBox.Show(message, Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                }

                // exit
                if (result == DialogResult.No)
                {
                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = BtnFlat.BackColor;
                    BtnDropMenu.ForeColor = BtnFlat.ForeColor;
                    return;
                }

                // else
                BuyMktOrder.Send(TradeContracts);
                message = string.Format("Order sent. Buy Mkt {0} @ {1}", TradeContracts, Bars.CloseValue);
                MessageBox.Show(message, Name, MessageBoxButtons.OK);

                BtnDropMenu.Text = TxtFlat;
                BtnDropMenu.BackColor = BtnFlat.BackColor;
                BtnDropMenu.ForeColor = BtnFlat.ForeColor;
            };

            #endregion

            #region Buy Limit
            BtnBuyLmt.Text = TxtBuyLmt;
            BtnBuyLmt.BackColor = Color.LimeGreen;
            BtnBuyLmt.ForeColor = Color.White;
            BtnBuyLmt.Click += delegate
            {
                BtnDropMenu.Text = TxtBuyLmt;
                BtnDropMenu.BackColor = BtnBuyLmt.BackColor;
                BtnDropMenu.ForeColor = BtnBuyLmt.ForeColor;
            };
            #endregion

            #region Buy Stop
            BtnBuyStp.Text = TxtBuyStp;
            BtnBuyStp.BackColor = Color.DarkGreen;
            BtnBuyStp.ForeColor = Color.White;
            BtnBuyStp.Click += delegate
            {
                BtnDropMenu.Text = TxtBuyStp;
                BtnDropMenu.BackColor = BtnBuyStp.BackColor;
                BtnDropMenu.ForeColor = BtnBuyStp.ForeColor;
            };
            #endregion

            #region Short Mkt
            BtnShortMkt.Text = TxtShortMkt;
            BtnShortMkt.BackColor = Color.IndianRed;
            BtnShortMkt.ForeColor = Color.White;
            BtnShortMkt.Click += delegate
            {
                BtnDropMenu.Text = TxtShortMkt;
                BtnDropMenu.BackColor = BtnShortMkt.BackColor;
                BtnDropMenu.ForeColor = BtnShortMkt.ForeColor;

                // check contracts
                if (TradeContracts < 20000) return;

                // cache results
                DialogResult result;
                string message;

                // check if stop loss > entry price, which will result in immediate fill
                if (StopLossPriceConfirm == true && EntryPrice > StopLossPrice)
                {
                    result = MessageBox.Show("Warning: Stoploss < Entry Price.",
                        Name, MessageBoxButtons.OKCancel);

                    if (result == DialogResult.Cancel)
                    {
                        BtnStopLossPrice.BackColor = DeactivationColor;
                        BtnStopLossPrice.ForeColor = Color.White;
                        TogStopLoss = true;
                        StopLossPriceConfirm = false;
                        return;
                    }
                }

                // check if profit < entry price, which will result in immediate fill
                if (ProfitPriceConfirm == true && ProfitPrice > EntryPrice)
                {
                    result = MessageBox.Show("Warning: Profit > Entry Price.",
                        Name, MessageBoxButtons.OKCancel);

                    if (result == DialogResult.Cancel)
                    {
                        BtnProfitPrice.BackColor = DeactivationColor;
                        BtnProfitPrice.ForeColor = Color.White;
                        TogProfit = true;
                        ProfitPriceConfirm = false;
                        return;
                    }
                }

                // check for marketposition
                if (CurrentPosition.Side == EMarketPositionSide.Long && CurrentPosition.OpenLots > 0)
                {
                    message = string.Format("Are you sure: Reverse Short {0} contracts @ {1} ?",
                        TradeContracts, Bars.CloseValue);

                    result = MessageBox.Show(message, Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                }
                else
                {
                    message = string.Format("Are you sure: Short {0} contracts @ {1} ?",
                        TradeContracts, Bars.CloseValue);

                    result = MessageBox.Show(message, Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                }

                // exit
                if (result == DialogResult.No)
                {
                    BtnDropMenu.Text = TxtFlat;
                    BtnDropMenu.BackColor = BtnFlat.BackColor;
                    BtnDropMenu.ForeColor = BtnFlat.ForeColor;
                    return;
                }

                // else
                ShortMktOrder.Send(TradeContracts);
                message = string.Format("Order sent. Short Mkt {0} @ {1}", TradeContracts, Bars.CloseValue);
                MessageBox.Show(message, Name, MessageBoxButtons.OK);

                BtnDropMenu.Text = TxtFlat;
                BtnDropMenu.BackColor = BtnFlat.BackColor;
                BtnDropMenu.ForeColor = BtnFlat.ForeColor;
            };

            #endregion

            #region Short Limit
            BtnShortLmt.Text = TxtShortLmt;
            BtnShortLmt.BackColor = Color.Red;
            BtnShortLmt.ForeColor = Color.White;
            BtnShortLmt.Click += delegate
            {
                BtnDropMenu.Text = TxtShortLmt;
                BtnDropMenu.BackColor = BtnShortLmt.BackColor;
                BtnDropMenu.ForeColor = BtnShortLmt.ForeColor;
            };
            #endregion

            #region Short Stop
            BtnShortStp.Text = TxtShortStp;
            BtnShortStp.BackColor = Color.DarkRed;
            BtnShortStp.ForeColor = Color.White;
            BtnShortStp.Click += delegate
            {
                BtnDropMenu.Text = TxtShortStp;
                BtnDropMenu.BackColor = BtnShortStp.BackColor;
                BtnDropMenu.ForeColor = BtnShortStp.ForeColor;
            };
            #endregion

            #region Flat Position
            BtnFlat.Text = TxtFlat;
            BtnFlat.BackColor = Color.DarkGray;
            BtnFlat.ForeColor = Color.White;
            BtnFlat.Click += delegate
            {
                BtnDropMenu.Text = TxtFlat;
                BtnDropMenu.BackColor = Color.DarkGray;
                BtnDropMenu.ForeColor = Color.White;
            };
            #endregion

            #region Close all Open Position
            BtnCloseAll.Text = TxtCloseAll;
            BtnCloseAll.BackColor = Color.Yellow;
            BtnCloseAll.ForeColor = Color.Black;
            BtnCloseAll.Click += delegate
            {
                // update the drop down button
                BtnDropMenu.Text = TxtCloseAll;
                BtnDropMenu.BackColor = BtnCloseAll.BackColor;
                BtnDropMenu.ForeColor = BtnCloseAll.ForeColor;

                // cache results
                DialogResult result;
                string message;
                int currPos = CurrentPosition.OpenLots;

                // check if market is long.
                if (CurrentPosition.Side == EMarketPositionSide.Long && currPos > 0)
                {
                    message = string.Format("Are you sure: Close all {0} contracts @ {1} ?", currPos, Bars.CloseValue);
                    result = MessageBox.Show(message, Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        BtnDropMenu.Text = TxtFlat;
                        BtnDropMenu.BackColor = BtnFlat.BackColor;
                        BtnDropMenu.ForeColor = BtnFlat.ForeColor;
                        return;
                    }
                    else
                    {
                        SellMktOrder.Send();
                        message = string.Format("Order sent. Sell Mkt {0} @ {1}", currPos, Bars.CloseValue);
                        MessageBox.Show(message, Name, MessageBoxButtons.OK);
                    }
                }
                else if (CurrentPosition.Side == EMarketPositionSide.Short && currPos > 0)
                {
                    message = string.Format("Are you sure: Close all {0} contracts @ {1} ?", currPos, Bars.CloseValue);
                    result = MessageBox.Show(message, Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        BtnDropMenu.Text = TxtFlat;
                        BtnDropMenu.BackColor = BtnFlat.BackColor;
                        BtnDropMenu.ForeColor = BtnFlat.ForeColor;
                        return;
                    }
                    else
                    {
                        CoverMktOrder.Send();
                        message = string.Format("Order sent. Buy To Cover Mkt {0} @ {1}", currPos, Bars.CloseValue);
                        MessageBox.Show(message, Name, MessageBoxButtons.OK);
                    }
                }

                BtnDropMenu.Text = TxtFlat;
                BtnDropMenu.BackColor = BtnFlat.BackColor;
                BtnDropMenu.ForeColor = BtnFlat.ForeColor;

                // reset entry price button
                BtnEntryPrice.BackColor = Color.DarkGray;
                BtnEntryPrice.ForeColor = Color.White;
                EntryPriceConfirm = false;
                TogEntry = true;

                // reset stop loss price button
                BtnStopLossPrice.BackColor = DeactivationColor;
                BtnStopLossPrice.ForeColor = Color.White;
                TogStopLoss = true;
                StopLossPriceConfirm = false;

                // reset profit price button
                BtnProfitPrice.BackColor = DeactivationColor;
                BtnProfitPrice.ForeColor = Color.White;
                TogProfit = true;
                ProfitPriceConfirm = false;
            };

            #endregion

            #region Entry Price
            BtnEntryPrice.Text = "Offline";
            BtnEntryPrice.BackColor = Color.DarkGray;
            BtnEntryPrice.ForeColor = Color.White;
            EntryPriceConfirm = false;
            TogEntry = true;
            BtnEntryPrice.Click += delegate
            {
                if (TogEntry == true)
                {
                    BtnEntryPrice.BackColor = BtnDropMenu.BackColor;
                    BtnEntryPrice.ForeColor = BtnDropMenu.ForeColor;

                    // check
                    if (BtnDropMenu.Text != TxtFlat)
                        EntryPriceConfirm = true;
                    else
                        EntryPriceConfirm = false;

                    TogEntry = false;
                }
                else
                {
                    BtnEntryPrice.BackColor = Color.DarkGray;
                    BtnEntryPrice.ForeColor = Color.White;
                    EntryPriceConfirm = false;
                    TogEntry = true;
                }

            };
            #endregion

            #region Stop Loss Price
            BtnStopLossPrice.Text = "Offline";
            BtnStopLossPrice.BackColor = Color.DarkGray;
            BtnStopLossPrice.ForeColor = Color.White;
            TogStopLoss = true;
            StopLossPriceConfirm = false;
            BtnStopLossPrice.Click += delegate
            {
                if (TogStopLoss)
                {
                    BtnStopLossPrice.BackColor = StopLossActivationColor;
                    BtnStopLossPrice.ForeColor = Color.Black;
                    TogStopLoss = false;
                    StopLossPriceConfirm = true;
                    TL_StopLoss.Color = StopLossActivationColor;
                }
                else
                {
                    BtnStopLossPrice.BackColor = DeactivationColor;
                    BtnStopLossPrice.ForeColor = Color.White;
                    TogStopLoss = true;
                    StopLossPriceConfirm = false;
                    TL_StopLoss.Color = ColorStopLossLine;
                }
            };
            #endregion

            #region Profit Target
            BtnProfitPrice.Text = "Offline";
            BtnProfitPrice.BackColor = Color.DarkGray;
            BtnProfitPrice.ForeColor = Color.White;
            TogProfit = true;
            ProfitPriceConfirm = false;
            BtnProfitPrice.Click += delegate
            {
                if (TogProfit)
                {
                    BtnProfitPrice.BackColor = ProfitActivationColor;
                    BtnProfitPrice.ForeColor = Color.Black;
                    TogProfit = false;
                    ProfitPriceConfirm = true;
                    TL_Profit.Color = ProfitActivationColor;
                }
                else
                {
                    BtnProfitPrice.BackColor = DeactivationColor;
                    BtnProfitPrice.ForeColor = Color.White;
                    TogProfit = true;
                    ProfitPriceConfirm = false;
                    TL_Profit.Color = ColorProfitLine;
                }
            };
            #endregion

            #region Real - time status
            BtnStatus.Text = "Offline";
            BtnStatus.BackColor = Color.Red;
            BtnStatus.ForeColor = Color.White;
            #endregion

            #region Risk Reward
            BtnRiskReward.Text = "0.00";
            BtnRiskReward.BackColor = Color.DarkGray;
            BtnRiskReward.ForeColor = Color.White;
            #endregion

            #region Profit and Loss
            BtnPnL.Text = "$0.00";
            BtnPnL.BackColor = Color.White;
            BtnPnL.ForeColor = Color.Black;
            #endregion

            #region Arrange lines neatedly
            BtnArrangeLines.Text = "Lines";
            BtnArrangeLines.BackColor = Color.ForestGreen;
            BtnArrangeLines.ForeColor = Color.White;
            TogLines = false;
            BtnArrangeLines.Click += delegate
            {
                DialogResult results;

                // check market position
                if (MarketPosition[0] != 0)
                {
                    results = MessageBox.Show("Currently in a trade. Ok to continue", Name, MessageBoxButtons.OKCancel);
                    if (results == DialogResult.Cancel) return;
                }

                if (EntryPriceConfirm == true)
                {
                    results = MessageBox.Show("Entry Line in Use. Ok to continue.", Name, MessageBoxButtons.OKCancel);
                    if (results == DialogResult.Cancel) return;
                }

                if (StopLossPriceConfirm == true)
                {
                    results = MessageBox.Show("Stop Loss Line in Use. Ok to continue.", Name, MessageBoxButtons.OKCancel);
                    if (results == DialogResult.Cancel) return;
                }

                if (ProfitPriceConfirm == true)
                {
                    results = MessageBox.Show("Profit Line in Use. Ok to continue.", Name, MessageBoxButtons.OKCancel);
                    if (results == DialogResult.Cancel) return;
                }

                // right and left screen time
                DateTime rightScreenTime = Environment.RightScreenTime;
                DateTime leftScreenTime = Environment.LeftScreenTime;

                // top and bottom price
                double highestScreenVal = Environment.HighestScaleValue;
                double lowestScreenVal = Environment.LowestScaleValue;

                // if daily return 1. 5 min return 5
                int barType = (int)Bars.Info.Resolution.Size;

                // time difference across the screen
                TimeSpan timeDiffAcrossScreen = rightScreenTime - leftScreenTime;

                // number of bars across the screen
                int numBarsOnScreen = (int)timeDiffAcrossScreen.TotalMinutes / barType;

                // price diffence across the screen
                double priceDiffAcrossScreen = highestScreenVal - lowestScreenVal;

                // Set the length of the trend lines, rounded up to the nearest bar size.
                DateTime startTime = leftScreenTime
                    .AddMinutes(Math.Round(numBarsOnScreen * 0.8) * barType);

                DateTime endTime = leftScreenTime
                    .AddMinutes(Math.Round(numBarsOnScreen * 1.0) * barType);

                // set the height for each trend line
                double entryHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.4);
                double stopLossHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.2);
                double profitHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.6);

                // Entry start and end chart points
                ChartPoint entryStart = new ChartPoint(startTime, entryHeight);
                ChartPoint entryEnd = new ChartPoint(endTime, entryHeight);

                // Entry start and end chart points
                ChartPoint stopLossStart = new ChartPoint(startTime, stopLossHeight);
                ChartPoint stopLossEnd = new ChartPoint(endTime, stopLossHeight);

                // Entry start and end chart points
                ChartPoint profitStart = new ChartPoint(startTime, profitHeight);
                ChartPoint profitEnd = new ChartPoint(endTime, profitHeight);

                if (TogLines == false)
                {
                    // update the three trend lines.
                    TL_Entry.Begin = entryStart;
                    TL_Entry.End = entryEnd;

                    TL_StopLoss.Begin = stopLossStart;
                    TL_StopLoss.End = stopLossEnd;

                    TL_Profit.Begin = profitStart;
                    TL_Profit.End = profitEnd;

                    BtnArrangeLines.BackColor = Color.ForestGreen;
                    BtnArrangeLines.ForeColor = Color.White;
                    TogLines = true;
                }
                else
                {
                    // update the three trend lines.
                    TL_Entry.Begin = entryStart;
                    TL_Entry.End = entryEnd;

                    TL_StopLoss.Begin = profitStart;
                    TL_StopLoss.End = profitEnd;

                    TL_Profit.Begin = stopLossStart;
                    TL_Profit.End = stopLossEnd;

                    BtnArrangeLines.BackColor = Color.Red;
                    BtnArrangeLines.ForeColor = Color.White;
                    TogLines = false;
                }

            };
            #endregion

            #region Tool bar status - locked
            BtnTbStatus.Text = "Unlocked";
            BtnTbStatus.BackColor = Color.DeepSkyBlue;
            BtnTbStatus.ForeColor = Color.White;
            TogLock = true;
            BtnTbStatus.Click += delegate
            {
                if (TogLock == false)
                {
                    BtnTbStatus.Text = "Unlocked";
                    BtnTbStatus.BackColor = Color.DeepSkyBlue;
                    BtnTbStatus.ForeColor = Color.White;
                    TogLock = true;

                    foreach (ToolStripItem item in ToolBarObjs)
                    {
                        item.Enabled = true;
                    }
                }
                else
                {
                    BtnTbStatus.Text = "Locked";
                    BtnTbStatus.BackColor = Color.Red;
                    BtnTbStatus.ForeColor = Color.White;
                    TogLock = false;

                    foreach (ToolStripItem item in ToolBarObjs)
                    {
                        item.Enabled = false;
                    }
                }
            };
            #endregion
        }
        // ---------------------------- Main End ---------------------------- //
        #endregion

        #region Toolstrip Dispose
        // ---------------------------- No changes required to be made here ---------------------------- //
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
        #endregion

    }
}