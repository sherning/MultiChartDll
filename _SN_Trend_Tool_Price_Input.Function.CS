/*
_SN_Trend_Tool_Price_Input      20170802
* Use result only after CurrentBar > Longest MALength = 34

* Programmed 
  for Sher Ning Teo <sherning@hotmail.com>, personal use
  by Harrison, www.HarrisonProgramming.com
///B
To be released as _SN_Trend_Tool_Price_Input
_SN_Trend_Tool_Price_Input_002      20170802

///E
*/


using System;
/// for HMA_
using PowerLanguage.Function;
using System.Collections;
/// for List
using System.Collections.Generic;

namespace PowerLanguage.Function
{
    public sealed class _SN_Trend_Tool_Price_Input : FunctionSimple<int>
    {
        public _SN_Trend_Tool_Price_Input(CStudyControl _master) : base(_master) { }
        public _SN_Trend_Tool_Price_Input(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // keep capital letter to match with original code, though input names normally start with lower
        public int Price_Input { private get; set; }

        /// Number of MA's
        private static int numOfMAs = 5;
        /// various MA lengths used, in List
        private List<int> MALength = new List<int>();
        private List<int> len2Use = new List<int>();
        private List<int> halvedLength = new List<int>();
        private List<double> sqLength = new List<double>();
        private List<int> sqrRootLength = new List<int>();

        /// Could Not use HMA_ class
        /// May Need to create extension method for this
        /// private List<HMA_> wMAs = new List<HMA_>();
        /// private List<VariableSeries<double>> vMA = new List<VariableSeries<double>>();

        /// private ISeries<double> vPrice { get; set; }
        private VariableSeries<double> vPrice;

        /// Use List to keep track of WMA's
        private List<VariableSeries<double>> vWMA1 = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vWMA2 = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vWMA1m2 = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> vWMA3 = new List<VariableSeries<double>>();

        /// MA_SUM need to be VariableObject!
        /// It should be int instead of double
        private VariableObject<int> MA_SUM;

        protected override void Create()
        {
            MALength.AddRange(new[] { 5, 8, 13, 21, 34 });

            for (int j = 0; j < numOfMAs; j++)
            {
                len2Use.Add(1);
                halvedLength.Add(1);
                sqLength.Add(1.0);
                sqrRootLength.Add(1);
            }

            vPrice = new VariableSeries<double>(this);

            for (int j = 0; j < numOfMAs; j++)
            {
                /// wMAs.Add(new HMA_(this));
                /// vMA.Add(new VariableSeries<double>(this));
                vWMA1.Add(new VariableSeries<double>(this));
                vWMA2.Add(new VariableSeries<double>(this));
                vWMA1m2.Add(new VariableSeries<double>(this));
                vWMA3.Add(new VariableSeries<double>(this));
            }

            MA_SUM = new VariableObject<int>(this);
        }

        protected override void StartCalc()
        {
            for (int j = 0; j < numOfMAs; j++)
            {
                // make sure minimum Length used is 1 to prevent possible 1/0 error
                len2Use[j] = MALength[j];
                if (MALength[j] < 1)
                    len2Use[j] = 1;

                // Calculate ONCE for faster computation
                if ((Math.Ceiling((double)(len2Use[j] / 2)) - (len2Use[j] / 2)) <= 0.5)
                {
                    halvedLength[j] = (int)Math.Ceiling((double)(len2Use[j] / 2));
                }
                else
                {
                    halvedLength[j] = (int)Math.Floor((double)(len2Use[j] / 2));
                }

                sqLength[j] = Math.Sqrt((double)len2Use[j]);

                if ((Math.Ceiling(sqLength[j]) - sqLength[j]) <= 0.5)
                {
                    sqrRootLength[j] = (int)Math.Ceiling(sqLength[j]);
                }
                else
                {
                    sqrRootLength[j] = (int)Math.Floor(sqLength[j]);
                }
            }
        }
        /// end of: protected override void StartCalc()

        protected override int CalcBar()
        {

            if (Bars.Status == EBarState.Close)
            {
                switch (Price_Input)
                {
                    case 1:
                        vPrice.Value = Bars.Close[0];
                        break;

                    case 2:
                        vPrice.Value = 0.5 * (Bars.Open[0] + Bars.Close[0]);
                        break;

                    case 3:
                        vPrice.Value = 0.25 * (Bars.Open[0] + Bars.High[0] + Bars.Low[0] + Bars.Close[0]);
                        break;

                    default:
                        vPrice.Value = Bars.Close[0];
                        break;
                }

                MA_SUM.Value = 0;

                for (int j = 0; j < numOfMAs; j++)
                {
                    vWMA1[j].Value = 2.0 * (vPrice.WAverageCustom(halvedLength[j]));

                    vWMA2[j].Value = vPrice.WAverageCustom(len2Use[j]);

                    vWMA1m2[j].Value = vWMA1[j].Value - vWMA2[j].Value;

                    vWMA3[j].Value = vWMA1m2[j].WAverageCustom(sqrRootLength[j]);

                    /// referring the prior bar value for VariableSeries vWMA3
                    if (vWMA3[j][0] > vWMA3[j][1])
                    {
                        MA_SUM.Value += 1;
                    }
                    else if (vWMA3[j][0] < vWMA3[j][1])
                    {
                        MA_SUM.Value -= 1;
                    }
                }
                /// end of: for (int j = 0; j < numOfMAs; j++)

            }
            /// end of: if (Bars.Status == EBarState.Close)

            return MA_SUM.Value;
            /// Need to validate
        }
        /// end of: protected override double CalcBar()
    }
}
