/*
_SN_VolatilityVariable101_f	20180131
* Change inputs/names

_SN_VolatilityVariable100_f     20180113    1050
 * Updated to include current bar data for Vol_Var_Mode = 3 and 4

_SN_VolatilityVariable100_f     20180105    2200
* This is a simple function.
* To use historical bar, create a VariableSeries var to store the data
* Calc on bar closed only
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PowerLanguage
{
	namespace Function
	{
        // Steps for defining a function
        // 1. Define function name, Simple or Series, return type
		public sealed class _SN_VolatilityVariable101_f : FunctionSimple<System.Double>
		{
			public _SN_VolatilityVariable101_f(CStudyControl _master) : base(_master) { }

            public int Vol_Var_Mode { get; set;}

            // mode 1 and 2 length uses this ATR
            public int ATR_Length { get; set; }

            // mode 2 speed uses this ATR speed
            public int ATR_Speed { get; set; }

            public int Mode_3_Days_Back { get; set; }

            public int Mode_4_Weeks_Back { get; set; }

            private const int daysPerWeek = 7;

            // assume 5 min bar
            private const int barInterval = 5;
            // # of 5 min bar per day (24 hr)
            private const int barsPerDay = 288;
            // Set this to likely input value Mode_3_Days_Back = 20, + 1. Reduce resizing
            private const int queCapacity = 21;

            private const double invalidVal = -999.0;
            // for cases 1, 3, 4
            private VariableSeries<double> barRange;

            // for case 2
            private double Speed2Use, Length2Use, LengthXSpeed;
            private VariableSeries<double> VolVar2;

            // for cases 3 and 4
            // For case 3, for each barTime, use a queue to store barRange of trailing Mode_3_Days_Back days
            // For case 4, for each Day of Week/barTime, use a queue to store barRange of trailing Mode_4_Weeks_Back weeks 
            private List<Queue<double>> barRangeQues = new List<Queue<double>>();

            protected override void Create()
			{
                // create variable objects and function objects

                barRange = new VariableSeries<double>(this);

                VolVar2 = new VariableSeries<double>(this);

                // Note daysPerWeek and barsPerDay are constants
                for (int i = 0; i < daysPerWeek; i++)
                {
                    for (int j = 0; j < barsPerDay; j++)
                    {
                        barRangeQues.Add(new Queue<double>(queCapacity));
                    }
                }
            }

            private double fRet;

            protected override void StartCalc()
			{
                // assume invalid Value until proven otherwise
                fRet = invalidVal;

                // for case 2
                Speed2Use = ((double)ATR_Speed) / 100.0;
                Length2Use = 2.0 / (((double)ATR_Length) + 1.0);
                LengthXSpeed = Length2Use * Speed2Use;

                // Clear existing data in StartCalc in case of changing inputs
                int qIdx = 0;
                for (int i = 0; i < daysPerWeek; i++)
                {
                    for (int j = 0; j < barsPerDay; j++)
                    {
                        barRangeQues[qIdx].Clear();
                        qIdx += 1;
                    }
                }
            }

            protected override double CalcBar()
			{
                if (Bars.Status == EBarState.Close)
                {
                    barRange.Value = Bars.High[0] - Bars.Low[0];

                    switch (Vol_Var_Mode)
                    {

                        case 2:
                            fRet = VolVarMode2_f();
                            break;

                        case 3:
                            fRet = VolVarMode3_f();
                            break;

                        case 4:
                            fRet = VolVarMode4_f();
                            break;

                        // combine case 1 and default
                        case 1:
                        default:
                            fRet = VolVarMode1_f();
                            break;
                    }
                }

                return fRet;
			}

            private double VolVarMode1_f()
            {
                double f1Ret;

                if (Bars.CurrentBar >= ATR_Length)
                {
                    f1Ret = barRange.Average(ATR_Length);
                }
                else
                {
                    f1Ret = invalidVal;
                }

                return f1Ret;
            }

            private double VolVarMode2_f()
            {
                double f2Ret;

                VolVar2.Value = (LengthXSpeed * (Bars.High[0] - Bars.Low[0]))
                        + ((1.0 - LengthXSpeed) * VolVar2[1]);

                if (Bars.CurrentBar >= 2)
                {
                    f2Ret = VolVar2[0];
                }
                else
                {
                    f2Ret = invalidVal;
                }

                return f2Ret;
            }

            private double VolVarMode3_f()
            {
                // Calculate the average bar range with the same bar time over an 
                // input() number of days, excluding the current bar

                double f3Ret;

                int barMFMN = HP_barMinFrmMidnight_f();
                // queue 3 index: bar index of the day
                int que3Idx = barMFMN / barInterval;

                // Do this before Avg calc to include current bar data
                // add the latest range for this DayofWeek and bar time of the day
                barRangeQues[que3Idx].Enqueue(barRange[0]);
                // remove trailing range for this bar time
                if (barRangeQues[que3Idx].Count > Mode_3_Days_Back)
                {
                    barRangeQues[que3Idx].Dequeue();
                }

                if (barRangeQues[que3Idx].Count >= Mode_3_Days_Back)
                {
                    f3Ret = barRangeQues[que3Idx].Average();
                }
                else
                {
                    f3Ret = invalidVal;
                }

                return f3Ret;
            }

            private double VolVarMode4_f()
            {
                // Calculate the average bar range with the same time over an 
                // input() number of days, excluding the current bar

                double f4Ret;

                int barMFMN = HP_barMinFrmMidnight_f();
                // queue 4 index include DayofWeek and bar time of the day
                int que4Idx = ((int)(Bars.Time[0].DayOfWeek) * barsPerDay) 
                     + (barMFMN / barInterval);

                // Do this before Avg calc to include current bar data
                // add the latest range for this DayofWeek and bar time of the day
                barRangeQues[que4Idx].Enqueue(barRange[0]);
                while (barRangeQues[que4Idx].Count > Mode_4_Weeks_Back)
                {
                    barRangeQues[que4Idx].Dequeue();
                }

                if (barRangeQues[que4Idx].Count >= Mode_4_Weeks_Back)
                {
                    f4Ret = barRangeQues[que4Idx].Average();
                }
                else
                {
                    f4Ret = invalidVal;
                }

                return f4Ret;
            }

            private int HP_barMinFrmMidnight_f()
            {
                return (60 * Bars.Time[0].Hour + Bars.Time[0].Minute);
            }
        }
    }
}