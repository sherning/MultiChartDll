using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class __SherNing_BarDemographics : IndicatorObject
    {
        // To visualize the percentage of a normalized bar.
        public __SherNing_BarDemographics(object _ctx) : base(_ctx) { }

        private List<IPlotObject> ListOfLines;
        private string[] LineNames;
        private Color[] LineColors;
        private double[] PlotValues;

        private IPlotObject HighToBody, Body, BodyToLow;
        private Color BarColor;
        protected override void Create()
        {
            ListOfLines = new List<IPlotObject>();
            LineNames = new string[] { "25", "50", "75" };
            LineColors = new Color[] { Color.Cyan, Color.Yellow, Color.Cyan };
            PlotValues = new double[] { 25, 50, 75 };

            for (int i = 0; i < 3; i++)
            {
                ListOfLines.Add(AddPlot(new PlotAttributes(LineNames[i], EPlotShapes.Line, LineColors[i])));
            }


            HighToBody = AddPlot(new PlotAttributes(
                "High To Body", 
                EPlotShapes.Histogram, 
                Color.DarkGray,
                Color.White,
                3,
                EPlotStyle.Solid,
                false));

            Body = AddPlot(new PlotAttributes(
                "Body", 
                EPlotShapes.Histogram, 
                Color.White, 
                Color.White, 
                5, 
                EPlotStyle.Solid, 
                false));

            BodyToLow = AddPlot(new PlotAttributes(
                "Body to Low", 
                EPlotShapes.Histogram, 
                Color.DarkGray, 
                Color.White, 
                7, 
                EPlotStyle.Solid, 
                false));
        }
        protected override void StartCalc()
        {
        }
        protected override void CalcBar()
        {
            if (Bars.Status != EBarState.Close) return;
            double barRange = Bars.High[0] - Bars.Low[0];
            double highToBody, body, bodyToLow;

            if(Bars.Close[0] - Bars.Open[0] > 0)
            {
                highToBody = (Bars.High[0] - Bars.Close[0]) / barRange;
                body = (Bars.Close[0] - Bars.Open[0]) / barRange;
                bodyToLow = (Bars.Open[0] - Bars.Low[0]) / barRange;
                BarColor = Color.LimeGreen;
            }
            // RedBar
            else
            {
                highToBody = (Bars.High[0] - Bars.Open[0]) / barRange;
                body = (Bars.Open[0] - Bars.Close[0]) / barRange;
                bodyToLow = (Bars.Close[0] - Bars.Low[0]) / barRange;
                BarColor = Color.Red;
            }

            BodyToLow.Set(bodyToLow * 100, Color.Black);
            Body.Set((bodyToLow + body) * 100, BarColor);
            HighToBody.Set((bodyToLow + body + highToBody) * 100, Color.Black);

            for (int i = 0; i < 3; i++)
            {
                ListOfLines[i].Set(PlotValues[i]);
            }
        }
    }
}