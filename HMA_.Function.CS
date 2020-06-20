/*
HMA_ function
* Named as HMA_ to avoid possible conflict with MultiCharts future HMA function 
* Assume constant Length >= 1
* Use function value only after Bars.CurrentBar > Length

* Based on EasyLanguage code
    jtHMA - Hull Moving Average Function, by Atavachron, May 2005

by Harrison, www.HarrisonProgramming.com
*/

using System;

namespace PowerLanguage.Function
{
    public sealed class HMA_ : FunctionSimple<double>
    {
        public HMA_(CStudyControl _master) : base(_master) { }
        public HMA_(CStudyControl _master, int _ds) : base(_master, _ds) { }

        public ISeries<double> price { private get; set; }
        public int length { private get; set; }

        private VariableSeries<double> vWMA1, vWMA2, vWMA3;
        private int len2Use, halvedLength, sqrRootLength;
        private double sqLength;

        protected override void Create()
        {
            vWMA1 = new VariableSeries<double>(this);
            vWMA2 = new VariableSeries<double>(this);
            vWMA3 = new VariableSeries<double>(this);
        }

        protected override void StartCalc()
        {
            // make sure minimum Length used is 1 to prevent possible 1/0 error
            len2Use = length;
            if (length < 1)
                len2Use = 1;

            // Calculate ONCE for faster computation
            if ((Math.Ceiling((double) (len2Use / 2)) - (len2Use / 2)) <= 0.5)
            {
                halvedLength = (int)Math.Ceiling((double)(len2Use / 2));
            }
            else
            {
                halvedLength = (int)Math.Floor((double)(len2Use / 2));
            }

            sqLength = Math.Sqrt((double)len2Use);

            if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
            {
                sqrRootLength = (int)Math.Ceiling(sqLength);
            }
            else
            {
                sqrRootLength = (int)Math.Floor(sqLength);
            }
        }

        protected override double CalcBar()
        {
            vWMA1.Value = 2.0 * (price.WAverageCustom(halvedLength));

            vWMA2.Value = price.WAverageCustom(len2Use);

            vWMA3.Value = vWMA1.Value - vWMA2.Value;

            double fRet = vWMA3.WAverageCustom(sqrRootLength);

            return (fRet);
        }
    }
}
