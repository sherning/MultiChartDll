/*
_HMA_Indicator

*/

using System;
using System.Drawing;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class _HMA_Indicator : IndicatorObject
    {
        private HMA_ mHMA;
        private VariableSeries<Double> vHMA;
        private Color vHMA_Color;
        private IPlotObject Plot1;

        public _HMA_Indicator(object ctx) : base(ctx)
        {
            Length = 9;
        }

        private ISeries<double> price { get; set; }

        [Input]
        public int Length { get; set; }

        [Input]
        public int displace { get; set; }

        protected override void Create()
        {
            mHMA = new HMA_(this);
            vHMA = new VariableSeries<Double>(this);

            Plot1 = AddPlot(new PlotAttributes("HMA", 0, Color.Red,
                                    Color.Empty, 0, 0, true));
        }

        protected override void StartCalc()
        {
            mHMA.price = Bars.Close;
            mHMA.length = Length;
            price = Bars.Close;
        }

        protected override void CalcBar()
        {
            vHMA.Value = mHMA[0];

            if (((displace >= 0)
                 || Bars.CurrentBar > Math.Abs(displace))
                 && Bars.CurrentBar >= Length)
            {
                // Check out value
                Output.WriteLine(Bars.TimeValue.ToString() + "\t"
                    + (vHMA.Value).ToString("F6"));

                // Set Plot Color.
                if (vHMA[0] > vHMA[1])
                {
                    vHMA_Color = Color.LimeGreen;
                }

                if (vHMA[0] < vHMA[1])
                {
                    vHMA_Color = Color.Red;
                }

                Plot1.Set(1, vHMA[1], vHMA_Color);
            }
        }
    }
}