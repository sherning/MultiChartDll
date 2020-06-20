using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class _SN_I_HMASmooth_v100 : IndicatorObject
    {
        /// <summary>
        /// HMA Length 
        /// </summary>
        private int HMA_Length { get; set; }
        /// <summary>
        /// Smaller the value the smoother the graph
        /// </summary>
        private int Smoothness { get; set; }
        public _SN_I_HMASmooth_v100(object ctx) : base(ctx) { }

        private HMA_ mHMA;
        private VariableSeries<Double> vHMA;
        private VariableSeries<Double> vHMA_GainLimit;

        private IPlotObject HMAPlot;

        private double alpha, gain;
        private Color vHMA_Color;

        protected override void Create()
        {
            // Trackbar default values
            HMA_Length = 6;
            Smoothness = 10;

            mHMA = new HMA_(this);
            vHMA = new VariableSeries<Double>(this);
            vHMA_GainLimit = new VariableSeries<double>(this);

            HMAPlot = AddPlot(new PlotAttributes("HMA_GainLimit", 0, Color.Red,
                                    Color.Empty, 0, 0, true));
        }

        protected override void StartCalc()
        {
            CreateToolStrip();
            mHMA.price = Bars.Close;
            mHMA.length = HMA_Length;
            alpha = 2.0 / (HMA_Length + 1.0);
        }

        protected override void CalcBar()
        {
            // Initialize HMA Value
            vHMA.Value = mHMA[0];


            //-----------------------------------------SECRET SAUCE-----------------------------------------//
            // Increase gain for extra spicy flavor
            gain = (Smoothness - 1.0) / 100.0;

            vHMA_GainLimit.Value = alpha * (vHMA.Value + gain * (Bars.Close[0] - vHMA_GainLimit[1]))
                     + (1.0 - alpha) * vHMA_GainLimit[1];
            //-----------------------------------------SECRET SAUCE-----------------------------------------//


            // Set Plot Color.
            if (vHMA_GainLimit[0] > vHMA_GainLimit[1])
            {
                vHMA_Color = Color.LimeGreen;
            }

            if (vHMA_GainLimit[0] < vHMA_GainLimit[1])
            {
                vHMA_Color = Color.Red;
            }

            // Plot on Chart
            // Width is the thickness of the plot line
            HMAPlot.Set(1, vHMA_GainLimit[1], vHMA_Color);
        }

        private void PrintMessage(string message)
        {
            Output.WriteLine("\t" + message);
        }

        #region ToolBar Configurations

        private bool ToolStripCreated = false;
        private bool btn_1_Locked = false;
        //private ToolStrip NewToolStrip;
        private ToolStripButton btnOne = new ToolStripButton();
        private TrackBar smoothnessTrackBar = new TrackBar();
        private TrackBar lengthTrackBar = new TrackBar();
        private ToolStripControlHost smoothnessControlHost;
        private ToolStripControlHost lengthControlHost;
        private ToolStripLabel displaySmoothnessTrackBar = new ToolStripLabel();
        private ToolStripLabel displayLengthTrackBar = new ToolStripLabel();

        private void CreateToolStrip()
        {
            if (ToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(ToolBarConfig);
            }

            ToolStripCreated = true;
        }

        private void ToolBarConfig(ToolStrip NewToolStrip)
        {
            // Button Configurations
            // if never tag, cannot destroy
            btnOne.Tag = this;
            btnOne.Text = "Unlocked";
            btnOne.Alignment = ToolStripItemAlignment.Left;
            btnOne.BackColor = Color.RoyalBlue;
            btnOne.ForeColor = Color.White;
            btnOne.Click += Btn_1_ChangeButtonDisplay;

            // Track bar configurations - Do you need to tag ?
            smoothnessControlHost = new ToolStripControlHost(smoothnessTrackBar);
            smoothnessControlHost.Tag = this;

            lengthControlHost = new ToolStripControlHost(lengthTrackBar);
            lengthControlHost.Tag = this;

            // Display Track Bar Value on Label
            displaySmoothnessTrackBar.Tag = this;
            displaySmoothnessTrackBar.Text = "Smoothness: " + Smoothness;
            // displaySmoothnessTrackBar.BackColor = Color.MidnightBlue;
            displaySmoothnessTrackBar.ForeColor = Color.White;

            // Display Length Trackbar Label
            displayLengthTrackBar.Tag = this;
            displayLengthTrackBar.Text = "Length: " + HMA_Length;
            // displayLengthTrackBar.BackColor = Color.MidnightBlue;
            displayLengthTrackBar.ForeColor = Color.White;

            // Smoothness Trackbar Configurations
            smoothnessTrackBar.Dock = DockStyle.Fill;
            smoothnessTrackBar.Maximum = 300;
            smoothnessTrackBar.Minimum = 0;
            smoothnessTrackBar.AutoSize = false;
            smoothnessTrackBar.Width = 1000;
            smoothnessTrackBar.SmallChange = 1;
            smoothnessTrackBar.Value = Smoothness;
            smoothnessTrackBar.ValueChanged += (sender, e) =>
            {
                TrackBar tsi = sender as TrackBar;
                if (Smoothness != tsi.Value)
                {
                    Smoothness = tsi.Value;
                    displaySmoothnessTrackBar.Text = "Smoothness: " + smoothnessTrackBar.Value;
                    ExecControl.Recalculate();
                }
            };

            // Length Trackbar Configurations
            lengthTrackBar.Dock = DockStyle.Fill;
            lengthTrackBar.Maximum = 200;
            lengthTrackBar.Minimum = 2;
            lengthTrackBar.AutoSize = false;
            lengthTrackBar.Width = 1000;
            lengthTrackBar.SmallChange = 1;
            lengthTrackBar.Value = HMA_Length;
            //lengthTrackBar.BackColor = Color.RoyalBlue;
            lengthTrackBar.ValueChanged += (sender, e) =>
            {
                TrackBar tsi = sender as TrackBar;
                if (HMA_Length != tsi.Value)
                {
                    HMA_Length = tsi.Value;
                    displayLengthTrackBar.Text = "Length: " + lengthTrackBar.Value;
                    ExecControl.Recalculate();
                }
            };

            // Tool Strip Configurations
            NewToolStrip.Items.Add(btnOne);
            NewToolStrip.Items.Add(displaySmoothnessTrackBar);
            NewToolStrip.Items.Add(smoothnessControlHost);
            NewToolStrip.Items.Add(displayLengthTrackBar);
            NewToolStrip.Items.Add(lengthControlHost);

            NewToolStrip.Dock = DockStyle.Top;
            NewToolStrip.BackColor = Color.DeepSkyBlue;
            NewToolStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            NewToolStrip.AutoSize = true;
        }

        #region Understanding local variables vs member fields
        // Default value for fields is false. For local variable is null.
        private bool btnTest;
        private void TestButtonMethod(object sender, EventArgs e)
        {
            if (btnTest == true)
            {
                MessageBox.Show("btnTest == True, changing it to false now");
                btnTest = false;
                return;
            }

            if (btnTest == false)
            {
                MessageBox.Show("btnTest == false, changing it to true now");
                btnTest = true;
                return;
            }
        }
        #endregion

        private void Btn_1_ChangeButtonDisplay(object sender, EventArgs e)
        {
            bool btnLocked = btn_1_Locked;

            if (btnLocked == true)
            {
                ChartToolBar.AccessToolBarAsync(tb =>
                {
                    // Button 1 Properties if Locked
                    btnOne.Text = "Locked";
                    btnOne.ToolTipText = "Click to unlock me";
                    btnOne.Alignment = ToolStripItemAlignment.Left;

                    // Button function
                    smoothnessControlHost.Enabled = false;
                    lengthControlHost.Enabled = false;

                    // Set Button Color if Locked
                    btnOne.BackColor = Color.MediumVioletRed;
                    btnOne.ForeColor = Color.White;
                });

                // Set button to Unlock
                btn_1_Locked = false;
            }

            if (btnLocked == false)
            {
                ChartToolBar.AccessToolBarAsync(tb =>
                {
                    // Button 1 Properties if Unlocked
                    btnOne.Text = "Unlocked";
                    btnOne.ToolTipText = "Click to lock me";
                    btnOne.Alignment = ToolStripItemAlignment.Left;

                    // Button Function
                    smoothnessControlHost.Enabled = true;
                    lengthControlHost.Enabled = true;

                    // Set Button Color if Unlocked
                    btnOne.BackColor = Color.RoyalBlue;
                    btnOne.ForeColor = Color.White;
                });

                // Set Button to Lock
                btn_1_Locked = true;
            }
        }

        protected override void Destroy()
        {
            // If true, tool bar is on chart
            if (ToolStripCreated == true)
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