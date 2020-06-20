/*
Mov_Avg_Hull_ indicator
* Named as Mov_Avg_Hull_ to avoid possible conflict with MultiCharts future Mov_Avg_Hull indicator

by Harrison, www.HarrisonProgramming.com
*/

using System;
using System.Drawing;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class Mov_Avg_Hull_ : IndicatorObject
    {
        private HMA_ mHMA;

        private VariableSeries<Double> vHMA;

        private IPlotObject Plot1;

        public Mov_Avg_Hull_(object ctx) : base(ctx)
        {
            length = 9;
        }

        private ISeries<double> price { get; set; }

        [Input]
        public int length { get; set; }

        [Input]
        public int displace { get; set; }

        protected override void Create()
        {
            mHMA = new HMA_(this);
            vHMA = new VariableSeries<Double>(this);
            Plot1 =
                AddPlot(new PlotAttributes("HMA", 0, Color.Yellow,
                                           Color.Empty, 0, 0, true));
        }

        protected override void StartCalc()
        {
            price = Bars.Close;
            mHMA.price = price;
            mHMA.length = length;
        }

        protected override void CalcBar()
        {
            vHMA.Value = mHMA[0];

            if ((displace >= 0 || Bars.CurrentBar > Math.Abs(displace))
                && Bars.CurrentBar >= length)
            {
                Plot1.Set(displace, vHMA.Value);

                if (displace <= 0)
                {
                    if (this.CrossesOver(price, vHMA))
                    {
                        Alerts.Alert("Price crossing over HMA");
                    }
                    else
                    {
                        if (this.CrossesUnder(price, vHMA))
                        {
                            Alerts.Alert("Price crossing under HMA");
                        }
                    }
                }
            }
        }
    }
}