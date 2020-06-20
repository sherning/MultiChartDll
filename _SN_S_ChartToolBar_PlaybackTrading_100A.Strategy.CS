using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PowerLanguage.Strategy
{
    public class _SN_S_ChartToolBar_PlaybackTrading_100A : SignalObject
    {
        public _SN_S_ChartToolBar_PlaybackTrading_100A(object _ctx) : base(_ctx) { }

        private IOrderMarket MarketLE;
        private IOrderMarket MarketLX;
        private IOrderMarket MarketSE;
        private IOrderMarket MarketSX;

        private bool _isToolbarInitiated;

        protected override void Create()
        {
            MarketLE = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "LE", EOrderAction.Buy));
            MarketLX = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "LX", EOrderAction.Sell, OrderExit.Total));
            MarketSE = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "SE", EOrderAction.SellShort));
            MarketSX = OrderCreator.MarketThisBar(new SOrderParameters(Contracts.UserSpecified, "SX", EOrderAction.BuyToCover, OrderExit.Total));
        }
        protected override void StartCalc()
        {
            _isToolbarInitiated = RunOnce(_isToolbarInitiated, () =>
            {
                // Use DialogResults to save the return state of the buttons
                DialogResult dialogResult = MessageBox.Show("Playback Trading is enabled. You can now simulate manual trading in Data Playback Mode. " +
                    "Click Yes to enable ToolStrip.",this.Name,MessageBoxButtons.YesNo,MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    Destroy();
                    CreateToolStrip();
                }
                if (dialogResult == DialogResult.No)
                {
                    Destroy();
                }
            });

            MessageBox.Show("Toolbar Initiated? " + _isToolbarInitiated);
        }
        protected override void CalcBar()
        {
            // test
        }

        #region Helper
        private void CreateToolStrip()
        {
            ChartToolBar.AccessToolBar(ToolBarConfigurations);
        }

        private void ToolBarConfigurations(ToolStrip NewToolStrip)
        {
            NumericUpDown lotNumControl = new NumericUpDown();
            lotNumControl.Minimum = 1;
            lotNumControl.Maximum = 100;
            lotNumControl.Tag = this;
            NewToolStrip.Items.Add(new ToolStripControlHost(lotNumControl));

            ToolStripButton tsiBuy = new ToolStripButton
            {
                Text = "Buy Market",
                BackColor = Color.DeepSkyBlue,
                ToolTipText = "Click to send Buy Market or reduce Short Position"
            };
        }
        private bool RunOnce(bool flag, Action action)
        {
            if (flag == true) 
                return false;

            action();
            return true;
        }

        protected override void Destroy()
        {
            // If true, tool bar is on chart
            if (_isToolbarInitiated == true)
            {
                ChartToolBar.AccessToolBar(toolStrip =>
                {
                    // Create a list of items to erase
                    var ListOfItemsToErase = new List<ToolStripItem>();

                    // Add the items on the ToolBar to a new list
                    foreach (ToolStripItem item in toolStrip.Items)
                    {
                        if (ReferenceEquals(this, item.Tag))
                        {
                            ListOfItemsToErase.Add(item);
                        }
                    }

                    // Iterate the list of items and remove them
                    foreach (var item in ListOfItemsToErase)
                    {
                        toolStrip.Items.Remove(item);
                    }
                });
            }
        }
        #endregion
    }
}