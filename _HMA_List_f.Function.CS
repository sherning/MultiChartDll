/*
_HMA_List function
* Use List data to calc HMA
* Use _WAverage_List_f
* After HMA_ function and _WAverage_List_f_Test1 indicator

* Assume constant Length >= 1
* Use function value only when function value > 0

by Harrison, www.HarrisonProgramming.com
*/

using System;
// for List
using System.Collections.Generic;

namespace PowerLanguage.Function
{
    /// <summary>
    /// Hull Moving average Explained
    /// Using a series of 10 numbers from '0' to '9'.
    /// The mid point or average value is 4.5
    /// Hull, take the recent half value and take the mid point or average value which is 7.
    /// Finally, to remove lag, we take the midpoint 7 and add the difference of the two average (7 - 4.5 = 2.5)
    /// which equates to (7 + 2.5 = 9.5) which is slightly overcompensated. -> this helps offset lagging effects
    /// 
    /// SMA is good for smoothing, but is very laggy. Combining 2 techniques provides superior smoothing and offset lag.
    /// </summary>
    public sealed class _HMA_List_f : FunctionSimple<double>
    {
        public _HMA_List_f(CStudyControl _master) : base(_master) { }
        public _HMA_List_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        public int Length { private get; set; }
        public List<double> PriceList { get; set; }

        private double dWMA1, dWMA2;
        private int LenToUse, halvedLength, sqrRootLength;
        private double sqLength;

        // private dy default
        int Length1, Length2, Length3;
        int Len1M1, Len2M1, Len3M1;
        int delLen1Len2;

        _HP_WAverage_List_f mWMA1, mWMA2, mWMA3;
        List<double> PriceList1;
        List<double> PriceList3;

        protected override void Create()
        {
            mWMA1 = new _HP_WAverage_List_f(this);
            mWMA2 = new _HP_WAverage_List_f(this);
            mWMA3 = new _HP_WAverage_List_f(this);

            PriceList3 = new List<double>();
        }

        bool onOpenTick;
        int currentBarPT;
        int lastPrice3Idx;

        protected override void StartCalc()
        {
            // make sure minimum Length used is 1 to prevent possible 1/0 error
            LenToUse = Length;
            if (Length < 1)
                LenToUse = 1;

            // Calculate ONCE for faster computation
            if ((Math.Ceiling((double) (LenToUse / 2)) - (LenToUse / 2)) <= 0.5)
            {
                halvedLength = (int)Math.Ceiling((double)(LenToUse / 2));
            }
            else
            {
                halvedLength = (int)Math.Floor((double)(LenToUse / 2));
            }

            sqLength = Math.Sqrt((double)LenToUse);

            if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
            {
                sqrRootLength = (int)Math.Ceiling(sqLength);
            }
            else
            {
                sqrRootLength = (int)Math.Floor(sqLength);
            }

            Length1 = halvedLength;
            Length2 = LenToUse;
            Length3 = sqrRootLength;

            Len1M1 = Length1 - 1;
            Len2M1 = Length2 - 1;
            Len3M1 = Length3 - 1;

            delLen1Len2 = Length2 - Length1;

            // Fix the size
            // Pass length into list constructor to set the number of elements that the new list can store.
            PriceList1 = new List<double>(Length1);
            for (int j=0; j< Length1; j++)
            {
                PriceList1.Add(0.0);
            }

            // clear value from any prior calc
            PriceList3.Clear();

            // - - - - - - - - Set WMA properties - - - - - - - - //
            mWMA1.Length = Length1;
            mWMA1.PriceList = PriceList1;

            mWMA2.Length = Length2;
            // use the input PriceList for this one, the list Count is Length2
            mWMA2.PriceList = PriceList;

            mWMA3.Length = Length3;
            mWMA3.PriceList = PriceList3;

            // - - - - - - - - Set WMA properties - - - - - - - - //

            onOpenTick = false;
            currentBarPT = 0;
            lastPrice3Idx = 0;
        }

        double fRet = 0;

        protected override double CalcBar()
        {
            onOpenTick = (Bars.CurrentBar > currentBarPT);
            currentBarPT = Bars.CurrentBar;

            // Note Len2M1 is a bigger of Len1M1 and Len2M1
            if (PriceList.Count > Len2M1)
            {
                int j2;
                // Copy the last Length1 elements of PriceList to PriceList1
                for (int j1 = Len1M1; j1 >= 0 ; j1--)
                {
                    j2 = j1 + delLen1Len2;
                    PriceList1[j1] = PriceList[j2];
                }

                dWMA1 = 2.0 * mWMA1.Value;

                dWMA2 = mWMA2.Value;
            }

            if (onOpenTick)
            {
                if (PriceList3.Count > Len3M1)
                {
                    PriceList3.RemoveAt(0);
                }
                PriceList3.Add(0.0);
                lastPrice3Idx = PriceList3.Count - 1;
            }
            PriceList3[lastPrice3Idx] = dWMA1 - dWMA2;

            if (PriceList3.Count == Length3)
            {
                // Calc and plot only when the list has Length3 elements
                fRet = mWMA3.Value;
            }
            else
            {
                // indicate that the result are not valided
                fRet = -1.0;
            }

            // Check for fRet <> -1.0 before using the data!!!

            return (fRet);
        }
    }
}
