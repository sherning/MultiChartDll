/*
_HMA_Indicator

*/

using System;
using System.Drawing;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(false)]
    public class _SN_I_HMALimit_v100 : IndicatorObject
    {
        [Input] public int Length { get; set; }
        [Input] public int GainLimit { get; set; }
        public _SN_I_HMALimit_v100(object ctx) : base(ctx)
        {
            Length = 6;
            GainLimit = 30;
        }

        private HMA_ mHMA;
        private VariableSeries<Double> vHMA;
        private VariableSeries<Double> vHMA_GainLimit;

        private IPlotObject HMAPlot;

        private double alpha, gain, error, bestGain;
        private Color vHMA_Color;

        protected override void Create()
        {
            mHMA = new HMA_(this);
            vHMA = new VariableSeries<Double>(this);
            vHMA_GainLimit = new VariableSeries<double>(this);

            HMAPlot = AddPlot(new PlotAttributes("HMA_GainLimit", 0, Color.Red,
                                    Color.Empty, 0, 0, true));
        }

        protected override void StartCalc()
        {
            Output.Clear();

            mHMA.price = Bars.Close;
            mHMA.length = Length;
            alpha = 2.0 / (Length + 1.0);
        }

        protected override void CalcBar()
        {
            // Initialize HMA Value
            vHMA.Value = mHMA[0];

            gain = (GainLimit - 1.0) / 100.0;

            vHMA_GainLimit.Value = alpha * (vHMA.Value + gain * (Bars.Close[0] - vHMA_GainLimit[1]))
                     + (1.0 - alpha) * vHMA_GainLimit[1];

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
    }
}