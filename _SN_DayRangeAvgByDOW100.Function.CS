/*
_SN_DayRangeAvgByDOW100	20180115
* DOW = Day of Week

_DayRangeAvgByDayOfWeek	20180104

Note
Check fo dowRangeAvg > 0 for valid data

*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Function
{
    public sealed class _SN_DayRangeAvgByDOW100 : FunctionSeries<System.Double>
    {
        // for Data1 only
        public _SN_DayRangeAvgByDOW100(CStudyControl _master) : base(_master) { }

        // Length of Average
        public int Length { get; set; }
        // Day of Week: Monday
        public DayOfWeek ThisDayOfWeek { get; set; }

        private bool inTestMode = false;

        // This Session Open, High, Low, Close
        private VariableSeries<double> TSO, TSH, TSL, TSC;
        private VariableSeries<bool> LastBarInSess;

        // way of week: High, Low, Range
        Queue<double> dowHighs = new Queue<double>();
        Queue<double> dowLows = new Queue<double>();
        Queue<double> dowRanges = new Queue<double>();

        protected override void Create()
        {
            // create variable objects and function objects
            TSO = new VariableSeries<double>(this);
            TSH = new VariableSeries<double>(this);
            TSL = new VariableSeries<double>(this);
            TSC = new VariableSeries<double>(this);
            LastBarInSess = new VariableSeries<bool>(this);
        }

        double[] dowRangesArr;
        private DayOfWeek dow;
        private int weekCnt;
        double dowHighAvg, dowLowAvg, dowRangeAvg, dowRangeStdDev;

        protected override void StartCalc()
        {
            // Clear prior values if any
            dowHighs.Clear();
            dowLows.Clear();
            dowRanges.Clear();

            dowRangesArr = new double[Length];
            // start with -1 to skip the first week, which may be partial
            weekCnt = -1;
            // start with invalid number until proven otherwise
            dowRangeAvg = -999.0;
        }

        protected override System.Double CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                LastBarInSess.Value = Bars.LastBarInSession;
                dow = Bars.Time[0].DayOfWeek;

                SessHighLowCalc_f();

                if (LastBarInSess[0])
                {
                    DowStatCalc_f(Length, ThisDayOfWeek);
                }
            }

            // must return here, not from within if()
            return dowRangeAvg;
        }

        private void SessHighLowCalc_f()
        {
            if (LastBarInSess[1] == true && LastBarInSess[0] == false)
            {
                // Session High/Low
                TSH.Value = Bars.High[0];
                TSL.Value = Bars.Low[0];
            }
            else
            {
                if (Bars.High[0] > TSH[0])
                {
                    TSH.Value = Bars.High[0];
                }

                if (Bars.Low[0] < TSL[0])
                {
                    TSL.Value = Bars.Low[0];
                }
            }
        }

        private void DowStatCalc_f(int length6, DayOfWeek dayOfWeek6)
        {
            if (inTestMode)
                Output.WriteLine("----DowStatCalc_f 01 -------------------------");

            if (dow == dayOfWeek6)
            {
                weekCnt += 1;

                {
                    dowHighs.Enqueue(TSH[0]);
                    dowLows.Enqueue(TSL[0]);
                    dowRanges.Enqueue(TSH[0] - TSL[0]);
                }

                while (dowRanges.Count > length6)
                {
                    dowHighs.Dequeue();
                    dowLows.Dequeue();
                    dowRanges.Dequeue();
                }

                if (weekCnt >= length6 && dowRanges.Count == length6)
                {
                    dowHighAvg = dowHighs.Average();
                    dowLowAvg = dowLows.Average();
                    dowRangeAvg = dowRanges.Average();
                    dowRangesArr = dowRanges.ToArray();
                    dowRangeStdDev = dowRangesArr.StandardDeviationCustom(length6, 1);

                    if (inTestMode)
                    {
                        Output.WriteLine("length6: " + length6.ToString());
                        Output.WriteLine("dowRanges.Count: " + dowRanges.Count.ToString());
                        Output.WriteLine("dowHighAvg: " + dowHighAvg.ToString());
                        Output.WriteLine("dowLowAvg: " + dowLowAvg.ToString());
                        Output.WriteLine("dowRangeAvg: " + dowRangeAvg.ToString());
                        Output.WriteLine("TSH[0]: " + TSH[0].ToString());
                        Output.WriteLine("TSL[0]: " + TSL[0].ToString());
                    }
                }

            }

        }

    }
}
