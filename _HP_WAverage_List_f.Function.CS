/*
_HP_WAverage_List_f	20191214
* WAverage from a fixed sized list from input
* To Test and compare to _WAverage
Simple function
*/

using System;
using System.Drawing;
using System.Linq;
// for List
using System.Collections.Generic;

namespace PowerLanguage
{
    namespace Function
    {
        // Steps for defining a function
        // 1. Define function name, Simple or Series, return type
        public sealed class _HP_WAverage_List_f : FunctionSimple<System.Double>
        {
            public _HP_WAverage_List_f(CStudyControl _master) : base(_master) { }
            public _HP_WAverage_List_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

            public int Length { get; set; }
            public List<double> PriceList { get; set; }

            /// <summary>
            /// A safety precaution in case to ensure that the minumum length used is >= 1
            /// </summary>
            private int Len2Use;
            /// <summary>
            /// Used in the calculation of the sum of WAverage, LengthToUse - 1, see formula below
            /// </summary>
            private int LenM1;
            private int k;
            /// <summary>
            /// function denominator value
            /// </summary>
            private double CumWt_Inv;
            /// <summary>
            /// function numerator value
            /// </summary>
            private double WtdSum;
            /// <summary>
            /// Return function WAverage value (double)
            /// </summary>
            private double fRet;

            protected override void Create()
            {
            }

            protected override void StartCalc()
            {
                // Assume Length is constant
                // make sure minimum Length used is >= 1 to prevent possible 1/0 error
                Len2Use = Length;
                if (Length < 1)
                    Len2Use = 1;

                LenM1 = Len2Use - 1;

                // Calculate ONCE
                // 5 + 4 + 3 + 2 + 1 = 15
                // 5(5 + 1)*0.5 = 15
                // n(n + 1) / 2
                CumWt_Inv = 1.0 / ((Len2Use + 1) * Len2Use * 0.5);
            }

            protected override double CalcBar()
            {
                // Check input data
                /*
                Output.WriteLine("------------------- Date: " + Bars.Time[0].ToString());
                Output.WriteLine("Length: {0}", Length);
                for (int j = 0; j < Len2Use; j++)
                {
                    Output.WriteLine("j = {0}, Price = {1}", j, PriceList[j]);
                }
                */

                //3 j = EL var convention index
                //3 k = List index convention
                // (CurrentPrice * 5) + (CP[1] * 4) ... + (CP[4] * 1)
                WtdSum = 0.0;
                for (int j = 0; j < Len2Use; j++)
                {
                    k = LenM1 - j;
                    WtdSum = WtdSum + (Len2Use - j) * PriceList[k];
                }

                // Function return WAverage
                fRet = WtdSum * CumWt_Inv;

                return fRet;
            }
        }
    }
}