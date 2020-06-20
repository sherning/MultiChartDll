using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerLanguage.Strategy
{
    [IOGMode(IOGMode.Enabled)]
    public class _SN_S_ChartToolBar_PlaybackTrading_100 : SignalObject
    {
        public _SN_S_ChartToolBar_PlaybackTrading_100 (object _ctx) : base(_ctx)
        {
        }

        private ToolStripButton _infoPanel;
        private ToolStripLabel _pnlLabel;
        private volatile int _trackedMarketPosition;
        private bool _isToolbarInitiated;

        private IOrderMarket _marketLE;
        private IOrderMarket _marketLX;
        private IOrderMarket _marketSE;
        private IOrderMarket _marketSX;

        protected override void Create()
        {
            _marketLE = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "LE", EOrderAction.Buy));
            _marketLX = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "LX", EOrderAction.Sell, OrderExit.Total));

            _marketSE = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "SE", EOrderAction.SellShort));
            _marketSX = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "SX", EOrderAction.BuyToCover, OrderExit.Total));
        }

        protected override void StartCalc()
        {
            RunOnce(ref _isToolbarInitiated, () =>
            {
                CreateToolStrip();
                MessageBox.Show("Playback Trading is enabled. You can now simulate manual trading in Data Playback mode.",
                this.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        protected override void CalcBar()
        {
            if (Bars.CurrentBarAbsolute() > ExecInfo.MaxBarsBack &&
            Bars.Status == EBarState.Close)
            {
                SetMarketPosition(_trackedMarketPosition);
            }
            UpdatePanelInfo();
        }

        private void CreateToolStrip()
        {
            ChartToolBar.AccessToolBar(tb =>
            {
                var lotNumControl = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 100
                };
                AddItemToToolStrip(tb, new ToolStripControlHost(lotNumControl));

                var tsiBuy = new ToolStripButton
                {
                    Text = "Buy Market",
                    BackColor = Color.DeepSkyBlue,
                    ToolTipText = "Click to send Buy Market or reduce Short position"
                };
                tsiBuy.Click += (obj, args) => _trackedMarketPosition += (int)lotNumControl.Value;
                AddItemToToolStrip(tb, tsiBuy);

                _infoPanel = new ToolStripButton
                {
                    ToolTipText = "Click to Close Position"
                };
                _infoPanel.Click += (obj, args) => _trackedMarketPosition = 0;
                AddItemToToolStrip(tb, _infoPanel);

                var tsiSell = new ToolStripButton
                {
                    Text = "Sell Market",
                    BackColor = Color.LightCoral,
                    ToolTipText = "Click to send Sell Market or reduce Long position"
                };
                tsiSell.Click += (obj, args) => _trackedMarketPosition -= (int)lotNumControl.Value;
                AddItemToToolStrip(tb, tsiSell);

                AddItemToToolStrip(tb, new ToolStripSeparator());

                var tsiPnlLabel = new ToolStripLabel
                {
                    Text = "Profit/Loss:",
                };
                AddItemToToolStrip(tb, tsiPnlLabel);
                AddItemToToolStrip(tb, _pnlLabel = new ToolStripLabel());

                AddItemToToolStrip(tb, new ToolStripSeparator());

                UpdatePanelInfo();
            });
        }

        private void UpdatePanelInfo()
        {
            var mp = 0; // market position
            var opl = 0.0; // open profit and loss
            var tpl = 0.0; // total profit and loss
            if (_isToolbarInitiated)
            {
                mp = CurrentPosition.Value;
                opl = CurrentPosition.OpenProfit;
                tpl = NetProfit + opl;
            }

            ChartToolBar.AccessToolBarAsync(tb =>
            {
                _infoPanel.Enabled = (0 != mp);
                _infoPanel.Text = GetOpenPnlString(mp, opl);
                _infoPanel.BackColor = GetBgColor(opl);

                _pnlLabel.Text = tpl.ToString("C");
                _pnlLabel.ForeColor = GetColor(tpl);
            });
        }

        private void SetMarketPosition(int targetPosition)
        {
            IOrderMarket entryOrder;
            IOrderMarket exitOrder;
            int adjust;
            var pos = CurrentPosition.Value;
            var count = Math.Abs(targetPosition);
            var isLong = targetPosition > 0;
            string dirstr;
            if (isLong)
            {
                entryOrder = _marketLE;
                exitOrder = _marketLX;
                adjust = count - pos;
                dirstr = "Long";
            }
            else
            {
                entryOrder = _marketSE;
                exitOrder = _marketSX;
                adjust = count + pos;
                dirstr = "Short";
            }

            if (adjust == 0)
            {
                //Output.WriteLine("{0}\t Pos: {1}\t Target: {2}\t HOLD", Bars.TimeValue, pos, targetPosition);
            }
            else if (adjust > 0)
            {
                if ((isLong && pos < 0) || (!isLong && pos > 0)) adjust = count;
                Output.WriteLine("{0}\t Pos: {1}\t Target: {2}\t {3} Entry: {4}", Bars.TimeValue, pos, targetPosition, dirstr, adjust);
                dirstr = dirstr[0] + "E" + adjust;
                entryOrder.Send(dirstr, adjust);
            }
            else // adjust < 0
            {
                adjust = -adjust;
                Output.WriteLine("{0}\t Pos: {1}\t Target: {2}\t {3} Exit: {4}", Bars.TimeValue, pos, targetPosition, dirstr, adjust);
                dirstr = dirstr[0] + "X" + adjust;
                exitOrder.Send(dirstr, adjust);
            }
        }

        protected override void Destroy()
        {
            if (_isToolbarInitiated)
            {
                ChartToolBar.AccessToolBar(tb =>
                {
                    var itemsToErase = new List<ToolStripItem>();

                    foreach (ToolStripItem item in tb.Items)
                        if (ReferenceEquals(this, item.Tag))
                            itemsToErase.Add(item);

                    foreach (var item in itemsToErase)
                        tb.Items.Remove(item);
                });
            }
        }

        #region Helpers
        private void RunOnce(ref bool flag, Action action)
        {
            if (!flag)
            {
                action();
                flag = true;
            };
        }

        private void AddItemToToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }

        private static string GetOpenPnlString(int mp, double opl)
        {
            var str = (mp > 0) ? mp + " Long"
            : (mp < 0) ? -mp + " Short"
            : "Flat";

            return String.Format("{0} {1}", str, opl.ToString("C"));
        }

        private static Color GetBgColor(double opl)
        {
            return opl > 0 ? Color.LawnGreen
            : opl < 0 ? Color.OrangeRed
            : Color.FromKnownColor(KnownColor.Control);
        }

        private static Color GetColor(double opl)
        {
            return opl > 0 ? Color.Green
            : opl < 0 ? Color.Red
            : Color.FromKnownColor(KnownColor.Control);
        }
        #endregion

    }

}