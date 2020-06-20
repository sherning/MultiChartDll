/*
_WAverage_List_f	20191214
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
		public sealed class _WAverage_List_f : FunctionSimple<System.Double>
		{
			public _WAverage_List_f(CStudyControl _master) : base(_master) { }
			public _WAverage_List_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

            public int Length {get; set;}
            // Assumpe calling code provide a PriceList of constant size Length
            public List<double> PriceList {get; set;}

            private int Len2Use, LenM1, k;
            private double CumWt_Inv, WtdSum;
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

                WtdSum = 0.0;
                for (int j = 0; j < Len2Use; j++)
                {
                    k = LenM1 - j;
                    WtdSum = WtdSum + (Len2Use - j) * PriceList[k];
                }

                fRet = WtdSum * CumWt_Inv;

                return fRet;
			}
		}
	}
}
