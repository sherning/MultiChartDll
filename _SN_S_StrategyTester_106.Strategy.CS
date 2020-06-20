/*
_SN_S_StrategyTester_106	20191202
* Use button "Refresh" instead of Ctrl-Left-Mouse
* 
_SN_S_StrategyTester_105	20191129

_SN_S_StrategyTester_104	20191129

_SN_S_StrategyTester_103	20191129

_SN_S_StrategyTester_102	20191128
* Issue with running "ExecControl.Recalculate()" from "Refresh" button
  Need to use Ctrl-Left Mouse Click for now

_SN_S_StrategyTester_100	20191127

* Customized for
  for Sher Ning Teo <sherning@hotmail.com>
  by Harrison, www.HarrisonProgramming.com

* Based on
_HP_ChartToolBar_Strat_106
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ATCenterProxy.interop;
using PowerLanguage.TradeManager;
using PowerLanguage.Function;
using System.Text;

namespace PowerLanguage.Strategy
{
    [SameAsSymbol(true), IOGMode(IOGMode.Disabled)]

    public class _SN_S_StrategyTester_106 : SignalObject
    {
        [Input] public Color RefreshButtonColor { get; set; }
        [Input] public int ContractsDefault { get; set; }
        [Input] public int ContractsIncrement { get; set; }
        [Input] public Color ContractsColor { get; set; }
        [Input] public Color LockButtonColor { get; set; }
        [Input] public string Buy_Text { get; set; }
        [Input] public string SellShort_Text { get; set; }
        [Input] public string Exit_Text { get; set; }
        [Input] public Color Buy_Color { get; set; }
        [Input] public Color SellShort_Color { get; set; }
        [Input] public Color Exit_Color { get; set; }

        public _SN_S_StrategyTester_106(object _ctx) : base(_ctx)
        {
            RefreshButtonColor = Color.Green;

            ContractsDefault = 100000;
            ContractsIncrement = 10000;
            ContractsColor = Color.LightGray;

            LockButtonColor = Color.Red;

            // Will look for these texts/letters
            // Use Upper case here!
            Buy_Text = "B";
            SellShort_Text = "S";
            Exit_Text = "X";

            // Upon refresh, will set the texts/letters to these colors
            Buy_Color = Color.Green;
            SellShort_Color = Color.Red;
            Exit_Color = Color.Yellow;

        }
        private int ToolBarSwitch = 1;

        private bool tool_bar_inited;
        private ToolStripButton btnRefresh, btnLock;
        private NumericUpDown numUpDn_Contracts;
        // start with Locked
        private bool tbLocked;
        private string Rrefresh_Str = " Refresh ";
        private string LockStr = " Lock ";
        bool toTriggerRecalc = false;

        private volatile int myAPos, SPos;

        private int posSize, LE_PosSize, SE_PosSize, E_Qty, X_Qty;
        private bool LBOCDoneOnce;

        private IOrderMarket LE_Mkt_Ord, LX_Mkt_Ord;
        private IOrderMarket SE_Mkt_Ord, SX_Mkt_Ord;

        private double LE_Pvt_P, LE_Lmt_P, LE_Ent_P, LX_Stp_P, LX_Tgt_P;
        private double SE_Pvt_P, SE_Lmt_P, SE_Ent_P, SX_Stp_P, SX_Tgt_P;

        private bool LE_Order, LX_Stp_Order, LX_Tgt_Order, LX_Mkt_Order;
        private bool SE_Order, SX_Stp_Order, SX_Tgt_Order, SX_Mkt_Order;
        private bool X_Mkt_Order;

        private VariableSeries<double> vBarDT;
        private VariableSeries<EMarketPositionSide> SMP;

        // Manual Texts
        // Assume up to 5000 texts
        int txts_Num, idx1;
        string[] txts_Str = new string[5000];
        DateTime[] txts_Time = new DateTime[5000];
        double[] txts_Price = new double[5000];

        //---------------------------------------------------------------------
        // Strategy Specific: Variables

        private bool inTestMode = true;
        //---------------------------------------------------------------------

        protected override void Create()
        {
            base.Create();

            LE_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "LE", EOrderAction.Buy));

            SE_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "SE", EOrderAction.SellShort));

            LX_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "LX", EOrderAction.Sell));

            SX_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "SX", EOrderAction.BuyToCover));

            SMP = new VariableSeries<EMarketPositionSide>(this);

            tbLocked = true;
            posSize = ContractsDefault;
        }

        double ptsPerPips, ptsPerTick, OnePip, OneTick, roundPriceToPts;
        // Assume IB for broker and data
        private string brokerName = "Interactive Brokers";
        private string dataSource = "Interactive Brokers";
        private string SymbolName;
        private string SymbolExchange;
        private ESymbolCategory SymbolCategory;

        protected override void StartCalc()
        {
            var instrument1 = Bars;
            ExecInfo.MaxBarsBack = 1;

            brokerName = Bars.Info.DataFeed;
            dataSource = Bars.Info.DataFeed;
            SymbolName = Bars.Info.Name;
            SymbolExchange = Bars.Info.Exchange;
            SymbolCategory = Bars.Info.Category;

            ptsPerTick = Bars.Info.MinMove / Bars.Info.PriceScale;

            if (SymbolExchange == "IDEALPRO"
                || SymbolExchange == "FOREX")
            {
                ptsPerPips = 10.0 * ptsPerTick;
            }
            else
            {
                ptsPerPips = ptsPerTick;
            }

            OnePip = ptsPerPips;
            OneTick = ptsPerTick;
            roundPriceToPts = ptsPerTick;

            if (ToolBarSwitch == 0 && tool_bar_inited)
            {
                RemoveToolBar_f();
            }

            if (ToolBarSwitch == 1 && !tool_bar_inited)
            {
                initializeToolBar();
                tool_bar_inited = true;

                ChartToolBar.AccessToolBarAsync(tb => {
                    btnRefresh.Enabled = false;
                    numUpDn_Contracts.Enabled = false;
                    btnLock.Text = " Unlock ";
                });

            }

        }

        private bool onCloseTick;

        protected override void CalcBar()
        {

            double close = Bars.Close[0];

            onCloseTick = (Bars.Status == EBarState.Close);

            if (onCloseTick)
            {
                if (tbLocked == false && Bars.CurrentBar == 1)
                {
                    GetManualTexts_f();
                    if (inTestMode) Print_TT("txts_Num = " + txts_Num.ToString());
                }

                if (txts_Num > 0)
                {
                    SMP.Value = CurrentPosition.Side;
                    SPos = StrategyInfo.MarketPosition;

                    X_Mkt_Order = false;
                    LE_Order = false;
                    LX_Mkt_Order = false;
                    SE_Order = false;
                    SX_Mkt_Order = false;

                    if (tbLocked == false)
                    {
                        DefineOrderVars_f();
                    }

                    if (LX_Mkt_Order)
                    {
                        if (SPos > 0)
                        {
                            X_Qty = SPos;
                            // if (inTestMode) Print_TT("SX_Mkt_Ord, X_Qty " + X_Qty.ToString());
                            LX_Mkt_Ord.Send(X_Qty);
                        }
                    }
                    else
                    {
                        if (LE_Order == true)
                        {
                            // if (inTestMode) Print_TT("LE_Mkt_Ord");
                            LE_Mkt_Ord.Send(posSize);
                        }

                    }

                    if (SX_Mkt_Order)
                    {
                        if (SPos < 0)
                        {
                            X_Qty = -SPos;
                            // if (inTestMode) Print_TT("SX_Mkt_Ord, X_Qty " + X_Qty.ToString());
                            SX_Mkt_Ord.Send(X_Qty);
                        }
                    }
                    else
                    {
                        if (SE_Order == true)
                        {
                            // if (inTestMode) Print_TT("SE_Mkt_Ord");
                            SE_Mkt_Ord.Send(posSize);
                        }

                    }

                }
            }

            if (Bars.LastBarOnChart)
            {
                ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
            }

        }

        protected override void StopCalc()
        {
            base.StopCalc();
        }

        protected override void Destroy()
        {
            if (tool_bar_inited)
            {
                RemoveToolBar_f();
            }
        }

        //---------------------------------------------------------------------

        private void RemoveToolBar_f()
        {
            ChartToolBar.AccessToolBar(tb =>
            {
                var itemsToRemove = new List<ToolStripItem>();

                foreach (ToolStripItem item in tb.Items)
                {
                    if (ReferenceEquals(this, item.Tag))
                        itemsToRemove.Add(item);
                }

                foreach (var item in itemsToRemove)
                {
                    tb.Items.Remove(item);
                }
            });
        }

        //---------------------------------------------------------------------

        private void initializeToolBar()
        {
            ChartToolBar.AccessToolBar(tb =>
            {
                btnRefresh = new ToolStripButton
                {
                    Text = Rrefresh_Str,
                    BackColor = RefreshButtonColor,
                    ToolTipText = "Click to Refresh Strategy"
                };
                btnRefresh.Click += (_1, _2) => btnRefresh_Click();
                AddItem2ToolStrip(tb, btnRefresh);

                numUpDn_Contracts = new NumericUpDown
                {
                    Dock = DockStyle.Fill,
                    Maximum = ContractsDefault * 100,
                    Minimum = 0,
                    Increment = ContractsIncrement,
                    DecimalPlaces = 0,
                    Value = ContractsDefault,
                    Text = " Position Size UpDn ",
                    BackColor = ContractsColor,
                    Enabled = true
                };
                numUpDn_Contracts.ValueChanged += numUpDn_ContractsValueChanged;
                AddItem2ToolStrip(tb, new ToolStripControlHost(numUpDn_Contracts));

                AddItem2ToolStrip(tb, new ToolStripSeparator());

                btnLock = new ToolStripButton
                {
                    Text = " Lock ",
                    BackColor = LockButtonColor,
                    ToolTipText = "Click to Lock"
                };
                btnLock.Click += (_1, _2) => btnLock_Click();
                AddItem2ToolStrip(tb, btnLock);

            });
        }

        private void AddItem2ToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }

        private void btnRefresh_Click()
        {
            if (inTestMode) Print_TT("btnRefresh_Click 000");
            try
            {
                toTriggerRecalc = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            if (inTestMode) Print_TT("btnRefresh_Click 999");
        }

        private void numUpDn_ContractsValueChanged(Object sender, EventArgs e)
        {
            posSize = (int)numUpDn_Contracts.Value;

            if (inTestMode) Print_TT("posSize = " + posSize.ToString());
        }

        private void btnLock_Click()
        {
            bool tbLockedBef = tbLocked;

            if (inTestMode) Print_TT("btnLock_Click 000");
            try
            {
                if (tbLockedBef == false)
                {
                    tbLocked = true;
                    if (inTestMode) Print_TT("Unlock to Lock");
                    ChartToolBar.AccessToolBarAsync(tb => {
                        btnRefresh.Enabled = false;
                        numUpDn_Contracts.Enabled = false;
                        btnLock.Text = " Unlock ";
                    });

                }

                if (tbLockedBef == true)
                {
                    tbLocked = false;
                    if (inTestMode) Print_TT("Lock to Unlock");
                    ChartToolBar.AccessToolBarAsync(tb => {
                        btnRefresh.Enabled = true;
                        numUpDn_Contracts.Enabled = true;
                        btnLock.Text = " Lock ";
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            if (inTestMode) Print_TT("btnLock_Click 999");
        }
        //---------------------------------------------------------------------

        protected override void OnRecalcLastBarAfterEvent()
        {
            if (toTriggerRecalc)
            {
                toTriggerRecalc = false;
                ExecControl.Recalculate();
            }

            ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
        }

        private void Print_TT(string printStr)
        {
            // Now time, Bar Time: String
            Output.WriteLine("{0}, {1}: {2}", DateTime.Now.ToString("HH:mm:ss.fff"), Bars.Time[0], printStr);
        }

        private void GetManualTexts_f()
        {
            string textStr;
            bool posSignal;
            ChartPoint thisLoc;

            txts_Num = 0;
            idx1 = 0;

            foreach (ITextObject thisText in DrwText.GetTextObjects(EDrawingSource.Manual))
            {

                textStr = thisText.Text.ToUpper();
                posSignal = false;

                // Set Color according to the input colors
                if (textStr == Buy_Text)
                {
                    thisText.Color = Buy_Color;
                    posSignal = true;
                }
                else
                if (textStr == SellShort_Text)
                {
                    thisText.Color = SellShort_Color;
                    posSignal = true;
                }
                else
                if (textStr == Exit_Text)
                {
                    thisText.Color = Exit_Color;
                    posSignal = true;
                }
                else
                {
                    // do nothing
                    ;
                }

                // retrieve the texts Time and Price
                if (posSignal)
                {
                    txts_Str[idx1] = textStr;

                    thisLoc = thisText.Location;
                    txts_Time[idx1] = thisLoc.Time;
                    txts_Price[idx1] = thisLoc.Price;
                    if (inTestMode) Print_TT("GetManualTexts_f: txts_Str[idx1]" + txts_Str[idx1]);

                    idx1 += 1;
                }

            }
            txts_Num = idx1;

        }

        private void DefineOrderVars_f()
        {
            bool barFound;
            string textStr;

            barFound = false;

            idx1 = 0;

            while (barFound == false && idx1 < txts_Num)
            {
                // if (inTestMode) Print_TT("Bars Time 1, T, 0: " + Bars.Time[1].ToString() + ", " + txts_Time[idx1].ToString() + ", " + Bars.Time[1].ToString());
                // if (Bars.Time[1] < txts_Time[idx1] && Bars.Time[0] >= txts_Time[idx1])
                if (Bars.Time[0] == txts_Time[idx1])
                {
                    barFound = true;
                    textStr = txts_Str[idx1];
                    if (inTestMode) Print_TT("barFound: textStr = " + textStr);
                    if (inTestMode) Print_TT("Exit_Text = " + Exit_Text);

                    // Set Color according to the input colors
                    if (textStr == Buy_Text)
                    {
                        LE_Order = true;
                        if (inTestMode) Print_TT("LE_Order = true");
                    }
                    else
                    if (textStr == SellShort_Text)
                    {
                        SE_Order = true;
                        if (inTestMode) Print_TT("SE_Order = true");
                    }
                    else
                    if (textStr == Exit_Text)
                    {
                        if (inTestMode) Print_TT("Exit_Text");
                        if (inTestMode) Print_TT("SMP[0] " + SMP[0].ToString());
                        if (inTestMode) Print_TT("SMP[0] == EMarketPositionSide.Long " + (SMP[0] == EMarketPositionSide.Long).ToString());

                        X_Mkt_Order = true;
                        if (SMP[0] == EMarketPositionSide.Long)
                        {
                            LX_Mkt_Order = true;
                            if (inTestMode) Print_TT("LX_Mkt_Order");
                        }
                        if (SMP[0] == EMarketPositionSide.Short)
                        {
                            SX_Mkt_Order = true;
                            if (inTestMode) Print_TT("SX_Mkt_Order");
                        }
                    }
                    else
                    {
                        // do nothing
                        if (inTestMode) Print_TT("nothing");
                        ;
                    }
                }

                idx1 += 1;
            }

        }
    }
}

