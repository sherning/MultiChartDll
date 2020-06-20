/*
_SN_S_StrategyTester_204	20500111
* Test and update

_SN_S_StrategyTester_203	20500111
* Save text object data to a restart file
* Duplicate text object on charts after restart

_SN_S_StrategyTester_202	20500110
* Try to use List for textObj data; however, MC.Net has issue with list
  saying them list not valid
* Code commented out for now, so that it would appear to be compiled.

_SN_S_StrategyTester_201	20500109
    Using MouseEvents.Left + Shift = Buy
    Using MouseEvents.Right + Shift = Sell
    Using MouseEvents.Left + Ctrl = Exit.
* Need to set MaxBarsBack to 0 to get the text position right
* Do not retain text after refresh without restart algorithm

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
using System.Globalization;
// for StreamWriter
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ATCenterProxy.interop;
using PowerLanguage.TradeManager;
using PowerLanguage.Function;
using System.Text;

namespace PowerLanguage.Strategy
{
    [MouseEvents(true), SameAsSymbol(true), IOGMode(IOGMode.Disabled), RecoverDrawings(false)]

    public class _SN_S_StrategyTester_204 : SignalObject
    {

        [Input] public Color RefreshButtonColor { get; set; }

        [Input] public int ContractsDefault { get; set; }
        public int ContractsIncrement { get; set; }
        [Input] public Color ContractsColor { get; set; }
        [Input] public Color LockButtonColor { get; set; }
        [Input] public Color SaveButtonColor { get; set; }
        [Input] public string Buy_Text { get; set; }
        [Input] public string SellShort_Text { get; set; }
        [Input] public string Exit_Text { get; set; }
        [Input] public Color Buy_Color { get; set; }
        [Input] public Color SellShort_Color { get; set; }
        [Input] public Color Exit_Color { get; set; }
        [Input] public string restartFile { get; set; }
        [Input] public string StratUniqueID { get; set; }

        public _SN_S_StrategyTester_204(object _ctx) : base(_ctx)
        {
            RefreshButtonColor = Color.Green;

            ContractsDefault = 100000;
            ContractsIncrement = 10000;
            ContractsColor = Color.LightGray;

            LockButtonColor = Color.Red;
            SaveButtonColor = Color.Cyan;
           
             // Will look for these texts/letters
             // Use Upper case here!
            Buy_Text = "B";
            SellShort_Text = "S";
            Exit_Text = "X";

            // Upon refresh, will set the texts/letters to these colors
            Buy_Color = Color.Green;
            SellShort_Color = Color.Red;
            Exit_Color = Color.Yellow;

            restartFile = @"C:\Automat\EUR.USD_15Min_Test1.txt";
        }
        private int ToolBarSwitch = 1;

        private bool tool_bar_inited;
        private ToolStripButton btnRefresh, btnLock, btnSave;
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
        int onChartTxts_Num, idx1;

        // onChart texts, including manual and code generated
        string[] onChartTxts_Str = new string[5000];
        DateTime[] onChartTxts_Time = new DateTime[5000];
        double[] onChartTxts_Price = new double[5000];

        // Code generated texts
        int codeGenTxts_Num = 0;
        DateTime[] codeGenTxts_Time = new DateTime[5000];
        double[] codeGenTxts_Price = new double[5000];
        string[] codeGenTxts_Str = new string[5000];

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
                if (Bars.CurrentBar == 1)
                {
                    ReadNRedrawTextObjectsDataFromFile_f();
                }

                if (tbLocked == false && Bars.CurrentBar == 2)
                {
                    GetCodeNManualTexts_f();
                    if (inTestMode) Print_TT("onChartTxts_Num = " + onChartTxts_Num.ToString());
                }

                if (onChartTxts_Num > 0)
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
            DeleteTechTextObjects_f();
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

                btnSave = new ToolStripButton
                {
                    Text = " Save ",
                    BackColor = SaveButtonColor,
                    ToolTipText = "Click to Save"
                };
                btnSave.Click += (_1, _2) => btnSave_Click();
                AddItem2ToolStrip(tb, btnSave);

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
                DeleteTechTextObjects_f();
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
                        btnSave.Enabled = false;
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
                        btnSave.Enabled = true;
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

        private void btnSave_Click()
        {
            if (inTestMode) Print_TT("btnSave_Click 000");

            try
            {
                SaveTextObjectsDataToFile_f();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (inTestMode) Print_TT("btnSave_Click 999");
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

        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            Color txtColor = Color.Black;
            ChartPoint mousePoint = arg.point;
            string txtStr = "";

            if (arg.keys == Keys.Shift)
            {
                if (arg.buttons == MouseButtons.Left)
                {
                    if (inTestMode) Print_TT("Shift - Left click");
                    // Using MouseEvents.Left + Shift = Buy
                    txtStr = Buy_Text;
                    txtColor = Buy_Color;
                }

                if (arg.buttons == MouseButtons.Right)
                {
                    if (inTestMode) Print_TT("Shift - Right click");
                    // Using MouseEvents.Right + Shift = Sell
                    txtStr = SellShort_Text;
                    txtColor = SellShort_Color;
                }
            }

            if (arg.keys == Keys.Control)
            {
                if (arg.buttons == MouseButtons.Left)
                {
                    if (inTestMode) Print_TT("Ctrl - Left click");
                    // Using MouseEvents.Left + Ctrl = Exit.
                    txtStr = Exit_Text;
                    txtColor = Exit_Color;
                }
            }

            if (txtStr != "")
            {
                if (inTestMode) Print_TT("txtStr: " + txtStr);
                if (inTestMode) Print_TT("txtColor: " + txtColor.ToString());
                if (inTestMode) Print_TT("Mouse location: " + mousePoint.Price.ToString() + " " + mousePoint.Time.ToString());
                // Create text label on chart
                ITextObject txt1 = DrwText.Create(mousePoint, txtStr);
                txt1.Color = txtColor;
                txt1.VStyle = ETextStyleV.Center;
                txt1.HStyle = ETextStyleH.Center;

                // Save Text object data to list
                codeGenTxts_Str[codeGenTxts_Num] = txtStr;
                codeGenTxts_Time[codeGenTxts_Num] = mousePoint.Time;
                codeGenTxts_Price[codeGenTxts_Num] = mousePoint.Price;
                codeGenTxts_Num += 1;

            }
        }

        private void Print_TT(string printStr)
        {
            // Now time, Bar Time: String
            Output.WriteLine("{0}, {1}: {2}", DateTime.Now.ToString("HH:mm:ss.fff"), Bars.Time[0], printStr);
        }

        private void SaveTextObjectsDataToFile_f()
        {
            if (inTestMode) Print_TT("SaveTextObjectsDataToFile_f");

            string textStr;
            ChartPoint thisLoc;
            DateTime thisTime;
            double thisPrice;

            onChartTxts_Num = 0;
            idx1 = 0;

            // Count Signal text objects
            foreach (ITextObject thisText in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
            {
                textStr = thisText.Text.ToUpper();
                if (textStr == Buy_Text || textStr == SellShort_Text || textStr == Exit_Text)
                {
                    idx1 += 1;
                }
            }

            onChartTxts_Num = idx1;

            // Write Signal text objects to restart file
            if (onChartTxts_Num > 0)
            {
                using (StreamWriter sw1 = new StreamWriter(restartFile, append: false))
                {
                    sw1.WriteLine("{0}", onChartTxts_Num);

                    foreach (ITextObject thisText in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
                    {
                        textStr = thisText.Text.ToUpper();
                        if (textStr == Buy_Text || textStr == SellShort_Text || textStr == Exit_Text)
                        {
                            thisLoc = thisText.Location;
                            thisTime = thisLoc.Time;
                            thisPrice = thisLoc.Price;

                            sw1.WriteLine("{0},{1},{2}",
                                thisTime.ToString(),
                                thisPrice.ToString(),
                                textStr
                                );
                        }
                    }

                }
            }
        }
        private void DeleteTechTextObjects_f()
        {
            foreach (ITextObject thisText in DrwText.GetTextObjects(EDrawingSource.CurrentTech))
            {
                thisText.Delete();
            }
        }

        private void ReadNRedrawTextObjectsDataFromFile_f()
        {
            if (inTestMode) Print_TT("ReadNRedrawTextObjectsDataFromFile_f");

            if (File.Exists(restartFile))
            {
                codeGenTxts_Num = 0;

                using (StreamReader sr1 = new StreamReader(restartFile))
                {
                    string inLine = "";

                    if ((inLine = sr1.ReadLine()) != null)
                    {
                        codeGenTxts_Num = Convert.ToInt32(inLine);
                    }

                    for (int i = 0; i < codeGenTxts_Num; i++)
                    {
                        if ((inLine = sr1.ReadLine()) != null)
                        {
                            string[] cols = inLine.Split(',');

                            codeGenTxts_Time[i] 
                                = DateTime.ParseExact(cols[0], "M/d/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                            codeGenTxts_Price[i] = Convert.ToDouble(cols[1]);
                            codeGenTxts_Str[i] = cols[2];
                        }
                    }
                }

                Color txtColor;
                string txtStr;

                for (int i = 0; i < codeGenTxts_Num; i++)
                {
                    txtStr = codeGenTxts_Str[i].ToUpper();

                    if (txtStr == Buy_Text)
                    {
                        txtColor = Buy_Color;
                    }
                    else if (txtStr == SellShort_Text)
                    {
                        txtColor = SellShort_Color;
                    }
                    else if (txtStr == Exit_Text)
                    {
                        txtColor = SellShort_Color;
                    }
                    else
                    {
                        txtStr = "";
                        txtColor = Color.Black;
                    }

                    if (txtStr != "")
                    {
                        // Redraw
                        ITextObject txt1 = DrwText.Create(
                            new ChartPoint(codeGenTxts_Time[i], codeGenTxts_Price[i]),
                            codeGenTxts_Str[i]);
                        txt1.Color = txtColor;
                        txt1.VStyle = ETextStyleV.Center;
                        txt1.HStyle = ETextStyleH.Center;
                    }
                }

            }

        }

        private void GetCodeNManualTexts_f()
        {
            string textStr;
            bool posSignal;
            ChartPoint thisLoc;

            onChartTxts_Num = 0;
            idx1 = 0;

            //  foreach (ITextObject thisText in DrwText.GetTextObjects(EDrawingSource.Manual))
            // include both curent code generated and manual text objects
            foreach (ITextObject thisText in DrwText.GetTextObjects(EDrawingSource.CurrentTechOrManual))
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
                    onChartTxts_Str[idx1] = textStr;

                    thisLoc = thisText.Location;
                    onChartTxts_Time[idx1] = thisLoc.Time;
                    onChartTxts_Price[idx1] = thisLoc.Price;
                    if (inTestMode) Print_TT("GetCodeNManualTexts_f: onChartTxts_Str[idx1]" + onChartTxts_Str[idx1]);

                    idx1 += 1;
                }

            }
            onChartTxts_Num = idx1;

        }

        private void DefineOrderVars_f()
        {
            bool barFound;
            string textStr;

            barFound = false;

            idx1 = 0;

            while (barFound == false && idx1 < onChartTxts_Num)
            {
                // if (inTestMode) Print_TT("Bars Time 1, T, 0: " + Bars.Time[1].ToString() + ", " + onChartTxts_Time[idx1].ToString() + ", " + Bars.Time[1].ToString());
                // if (Bars.Time[1] < onChartTxts_Time[idx1] && Bars.Time[0] >= onChartTxts_Time[idx1])
                if (Bars.Time[0] == onChartTxts_Time[idx1])
                {
                    barFound = true;
                    textStr = onChartTxts_Str[idx1];
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

