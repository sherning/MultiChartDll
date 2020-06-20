
//==============================================================================
// Name:          _SN_I_PriceActionLine_v100
// Description:   Price Action Line - Single Timeframe
// Version:       v.1.0.0
// Date Created:  20 - Nov - 2019
//==============================================================================
// Copyright:     2019, Sher Ning Technologies
//             
// License:     
//==============================================================================

//------------------------------------------------------------------------------
// Version 1.0.0
// Hull moving average vs EMA vs Speed moving average vs No Lag Moving average
// EMA == Speed Moving Average if speed = 100
//
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class _SN_I_PriceActionLine_v100 : IndicatorObject
    {
        #region Indicator Inputs
        [Input] public int Length { get; set; }
        [Input] public int Speed { get; set; }
        [Input] public int GainLimit { get; set; }
        [Input] public double LeastError { get; set; }
        [Input] public string DisplaySetting { get; set; }
        [Input] public SN_Switch DisplaySpeedMA { get; set; }
        [Input] public SN_Switch DisplayHMA { get; set; }
        [Input] public SN_Switch DisplayZeroLag { get; set; }
        [Input] public SN_Switch IsDebugMode { get; set; }
        #endregion
        #region Indicator Constructor
        public _SN_I_PriceActionLine_v100(object _ctx) : base(_ctx)
        {
            // Test if logic design can be implemented inside a constructor
            // answer: cannot have logic inside the constructor.

            // Values for readonly const 
            // Answer: Yes you can set readonly values inside the constructor
            // Not a good idea, as you cannot change readonly inputs on the fly.

            Length = 20;
            Speed = 100;
            GainLimit = 30;
            LeastError = 0.0010;
            DisplaySetting = "Display Settings";
            DisplaySpeedMA = SN_Switch.On;
            DisplayHMA = SN_Switch.On;
            DisplayZeroLag = SN_Switch.On;

            // Change to Off in final release
            IsDebugMode = SN_Switch.On;
        }
        #endregion
        #region Indicator Variables
        // Plot Variables
        private IPlotObject speedMAPlot, zeroMAPlot, HMAPlot;
        private VariableSeries<double> speedMASeries, zeroMASeries, HMASeries;
        private HMA_ HMAFunction;

        // Calculation Variables
        private double alpha, speed;

        // Variables for calculating Zero Lag Moving Average
        private double change, bestGain, gain, error;
        private Color zeroLagColor;
        #endregion

        protected override void Create()
        {
            // Create plot objects
            speedMAPlot = AddPlot(new PlotAttributes("Speed MA", EPlotShapes.Line, Color.Red));
            zeroMAPlot = AddPlot(new PlotAttributes("Zero Lag MA", EPlotShapes.Line, Color.Red));
            HMAPlot = AddPlot(new PlotAttributes("HMA", EPlotShapes.Line, Color.Red));

            // Create variable series
            speedMASeries = new VariableSeries<double>(this);
            zeroMASeries = new VariableSeries<double>(this);
            HMASeries = new VariableSeries<double>(this);

            // Create HMA Object
            HMAFunction = new HMA_(this);

        }
        protected override void StartCalc()
        {
            // Clear Output
            Output.Clear();

            // Test Logic inside StartCalc(), Logic Works.
            if (Length < 2)
            {
                this.Length = 2; // Change properties
            }
            if (GainLimit > 50 || GainLimit < 1)
            {
                this.GainLimit = 30;
            }

            alpha = 2.0 / (Length + 1.0);
            speed = Speed / 100.0;
            change = alpha * speed;

            // Assign inputs to HMA
            HMAFunction.length = this.Length;
            HMAFunction.price = Bars.Close;
        }
        protected override void CalcBar()
        {
            #region DebugMode Start
            if (IsDebugMode == SN_Switch.On)
            {
                // Debug mode start
                Output.WriteLine("");
                Output.WriteLine("====================================== SN_IndicatorName : Debug Mode Start ======================================");
                Output.WriteLine("");
            }
            #endregion
            if (Bars.Status == EBarState.Close)
            {
                // reset values 
                bestGain = 0;
                //gain = 0;
                //error = 0;

                if (Bars.CurrentBar == 1)
                {
                    // Need to initialize, as their calculation require [1]
                    zeroMASeries.Value = Bars.CloseValue;
                    speedMASeries.Value = Bars.CloseValue;
                }

                // Speed MA logic
                speedMASeries.Value = (change * Bars.Close[0]) + (1.0 - change) * speedMASeries[1];

                // HMA Logic
                HMASeries.Value = HMAFunction[0];

                // Zero Lag Logic
                for (int i = -GainLimit; i < GainLimit; i++)
                {
                    gain = i / 100.0;
                    zeroMASeries.Value = alpha * (speedMASeries.Value + gain * (Bars.Close[0] - zeroMASeries[1]))
                                       + (1.0 - alpha) * zeroMASeries[1];

                    error = Bars.CloseValue - zeroMASeries.Value;
                    if (Math.Abs(error) < LeastError)
                    {
                        LeastError = Math.Abs(error);
                        bestGain = gain;
                    }
                }

                zeroMASeries.Value = alpha * (speedMASeries.Value + bestGain * (Bars.Close[0] - zeroMASeries[1]))
                                   + (1.0 - alpha) * zeroMASeries[1];

                // Set Color Logic
                if (zeroMASeries[0] > zeroMASeries[1])
                {
                    zeroLagColor = Color.LimeGreen;
                }
                if (zeroMASeries[0] < zeroMASeries[1])
                {
                    zeroLagColor = Color.Red;
                }

                // Set Plot
                if (zeroMASeries[1] > 0 && DisplayZeroLag == SN_Switch.On)
                {
                    zeroMAPlot.Set(1,zeroMASeries[1],zeroLagColor);
                }
                if (speedMASeries[1] > 0 && DisplaySpeedMA == SN_Switch.On)
                {
                    speedMAPlot.Set(1,speedMASeries[1],Color.Cyan);
                }
                if (DisplayHMA == SN_Switch.On)
                {
                    HMAPlot.Set(1,HMASeries[1],Color.Magenta);
                }
            }
            #region DebugMode End
            if (IsDebugMode == SN_Switch.On)
            {
                // Cannot output from constructor?
                Output.WriteLine("= = = = Check Values inside StartCalc() = = = =");
                Output.WriteLine("Length: {0}, GainLimit: {1}, LeastError: {2}", Length, GainLimit, LeastError);
                Output.WriteLine("alpha: {0}, speed: {1}", alpha, speed);
                Output.WriteLine("error: {0}", error);

            }
            if (IsDebugMode == SN_Switch.On)
            {
                // Debug mode end
                Output.WriteLine("");
                Output.WriteLine("====================================== SN_IndicatorName : Debug Mode End =======================================");
                Output.WriteLine("");
            }
            #endregion
        }
    }
}