/*
_SN_PALwithStdDev_161B_f	20191003
* Special version to test PAL_5 to follow IB timing
  similar to PAL_15 in _SN_PALwithStdDev_161
* Must run on 5 min chart, just for PAL 5, 60, 240 and daily only

_SN_PALnStdDev_161_f 20190831
* Add Monthly
* New algorithm for bar end time for Minutes
* 
_SN_PALnStdDev_160_f 20190826
* PAL and StdDev
* for PAL_15_Chart (Priority)
  Need PAL_15, PAL_60, PAL_240, PAL_Day
  And so on
* Note Monthly not done.  It uses the weekly code for now
* 
_SN_PAL130_f 20181003	2000
* Add 10 min

_SN_PAL120_f    20180529	1400
* For TS Forex, it is Forex, the Category number is 12.
* For IB Forex, it is Cash, the Category number is 9.

_SN_PAL110_f    20180117
PAL changes
    // for 5 min, no changes required
    If absvalue( PAL_5 - PAL_5_1 ) < ( TickBuffer * OneTick ) then 
		    PAL_5 = PAL_5_1 ;

    // changes for 15 min
    If absvalue( PAL_15 - PAL_15_1 ) < ( TickBuffer * OneTick* 3) then 
		    PAL_15 = PAL_15_1 ;

    // changes for 20 min OneTick * 4
    // changes for 30 min OneTick * 6
    // changes for 60 min OneTick * 12
    // changes for 120 min OneTick * 24
    // changes for 240 min OneTick * 48
    // changes for daily OneTick * 100
    One Option to switch between the new settings above and original settings [SettingOriginalOrNew = 1 or 2 ]

_SN_PAL100_f    20180115
* Original PAL Function

After temp dev code _SN_PAL_VMA_FN_Indicator_091
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PowerLanguage.Function
{
    public sealed class _SN_PALnStdDev_161B_f : FunctionSimple<System.Double>
    {
        // Data1 only
        public _SN_PALnStdDev_161B_f(CStudyControl _master) : base(_master) { }

        public int Speed_Input { get; set; }
        public int Length_Input { get; set; }
        public int TickBuffer { get; set; }
        public int Price_Input { get; set; }
        // public int Price_Trend_On { get; set; }
        public int SettingOriginalOrNew { get; set; }

        public int StdDev_Length { get; set; }
        // StdDev_DataType 1 for population, 2 for sample
        public int StdDev_DataType { get; set; }
        public bool isDebugMode { get; set; }

        // Number of Calculation Time Frames for this instance
        private const int numOfCalcTFs = 4;

        // Number of All possible Time Frames
        private const int numOfAllTFs = 11;
        // List index from 0 to 10, total 11
        private const int TF_5M = 0;
        private const int TF_10M = 1;
        private const int TF_15M = 2;
        private const int TF_20M = 3;
        private const int TF_30M = 4;
        private const int TF_60M = 5;
        private const int TF_120M = 6;
        private const int TF_240M = 7;
        private const int TF_Day = 8;
        private const int TF_Week = 9;
        private const int TF_Month = 10;

        // int numbers for each Upper_Active_Color_Name and Lower_Active_Color_Name
        private const int Upper_Active_ClrNum = 1;
        private const int Lower_Active_ClrNum = 2;

        // private List<VariableSeries<double>> vPALs = new List<VariableSeries<double>>();
        // Sufficient to use VariableObject for vPALs
        private List<VariableObject<double>> vPALs = new List<VariableObject<double>>();
        private List<VariableObject<double>> vPAL1s = new List<VariableObject<double>>();
        private List<VariableSeries<int>> vActiveClrNum = new List<VariableSeries<int>>();
        private List<VariableObject<double>> vStDevs = new List<VariableObject<double>>();
        private VariableObject<int> PAL_Width;

        private int[] HTF_TFs;
        private int[] HTF_BarCnts;

        private List<VariableSeries<bool>> HTF_OnCloseTicks = new List<VariableSeries<bool>>();

        // >> Need to use arrays if we need price history
        private List<VariableSeries<double>> HTF_Prices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Opens = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Closes = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Highs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Lows = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_OpenCloseAvgs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_OHLCAvgs = new List<VariableSeries<double>>();

        private double OnePip, OneTick;
        private int vCategory, sessEnd_ELTime;
        private double Speed, Length, LengthXSpeed, pointBuffer;
        private double[] pointBuffersAll;
        private double[] pointBuffers;

        int[] dataCnts = new int[4];
        // index1 is for TF, index2 is data point number for the time frame
        // Max StdDevLen2U is limited to 201
        int HTFArrSizeMax = 201;
        // Closes_Arr: idx1 = HTF, idx2 is HTF bars back
        double[,] Closes_Arr = new double[4, 201];

        public ISeries<double> PAL_X01 { get { return vPALs[0]; } }
        public ISeries<double> PAL_X04 { get { return vPALs[1]; } }
        public ISeries<double> PAL_X16 { get { return vPALs[2]; } }
        public ISeries<double> PAL_X64 { get { return vPALs[3]; } }

        // public ISeries<int> vActiveClrNum_5 { get { return vActiveClrNum[0]; } }

        public ISeries<double> PAL1_X01 { get { return vPAL1s[0]; } }
        public ISeries<double> PAL1_X04 { get { return vPAL1s[1]; } }
        public ISeries<double> PAL1_X16 { get { return vPAL1s[2]; } }
        public ISeries<double> PAL1_X64 { get { return vPAL1s[3]; } }

        // Check for StdDev > 0 before using the value
        public ISeries<double> StdDev_X01 { get { return vStDevs[0]; } }
        public ISeries<double> StdDev_X04 { get { return vStDevs[1]; } }
        public ISeries<double> StdDev_X16 { get { return vStDevs[2]; } }
        public ISeries<double> StdDev_X64 { get { return vStDevs[3]; } }

        string filePath;

        bool OKToRun;
        double StdDevInvLen2U;
        int StdDevLen2U;
        DayOfWeek myDOW;
        DateTime myDate;

        private HP_BarMntFromSessOpen_f mBarMFSO;
        private int barMFSO;

        double closeSum, closeAvg;
        double diff, diffSqSum, diffSqAvg;
        double stdDev;
        bool HTF_OnCloseTick;

        protected override void Create()
        {
            mBarMFSO = new HP_BarMntFromSessOpen_f(this);

            // Define 4 TF's to Calc
            HTF_TFs = new int[] { 0, 0, 0, 0 };
            HTF_BarCnts = new int[] { 0, 0, 0, 0 };

            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfCalcTFs; j++)
            {
                // vPALs.Add(new VariableSeries<double>(this));
                vPALs.Add(new VariableObject<double>(this));
                vPAL1s.Add(new VariableObject<double>(this));
                vActiveClrNum.Add(new VariableSeries<int>(this));
                vStDevs.Add(new VariableObject<double>(this));
            }

            PAL_Width = new VariableObject<int>(this);

            // trend_Tool_Price_Input1 = new _SN_Trend_Tool_Price_Input(this);

            for (int j = 0; j < numOfCalcTFs; j++)
            {
                HTF_OnCloseTicks.Add(new VariableSeries<bool>(this));
                HTF_Prices.Add(new VariableSeries<double>(this));
                HTF_Opens.Add(new VariableSeries<double>(this));
                HTF_Closes.Add(new VariableSeries<double>(this));
                HTF_Highs.Add(new VariableSeries<double>(this));
                HTF_Lows.Add(new VariableSeries<double>(this));
                HTF_OpenCloseAvgs.Add(new VariableSeries<double>(this));
                HTF_OHLCAvgs.Add(new VariableSeries<double>(this));
            }
        }

        protected override void StartCalc()
        {
            // trend_Tool_Price_Input1.Price_Input = this.Price_Input;

            OnePip = (10.0 * Bars.Info.MinMove / Bars.Info.PriceScale);
            OneTick = (Bars.Info.MinMove / Bars.Info.PriceScale);

            vCategory = (int)Bars.Info.Category;
            // Need to include 9
            if (vCategory != 0 && vCategory != 2 && vCategory != 9 && vCategory != 12)
                vCategory = 4;

            int MntInterval = (int)Bars.Info.Resolution.Duration().TotalMinutes;

            if (Bars.Info.Resolution.ChartType == ChartType.Regular
                && (MntInterval == 5 || MntInterval == 15 || MntInterval == 60 || MntInterval == 240))
            {
                OKToRun = true;
            }
            else
            {
                OKToRun = false;
            }

            if (OKToRun)
            {

                // Define the 4 TFs to calc

                switch (MntInterval)
                {
                    case 60:
                        HTF_TFs[0] = TF_60M;
                        HTF_TFs[1] = TF_240M;
                        HTF_TFs[2] = TF_Day;
                        HTF_TFs[3] = TF_Week;
                        break;

                    case 240:
                        HTF_TFs[0] = TF_240M;
                        HTF_TFs[1] = TF_Day;
                        HTF_TFs[2] = TF_Week;
                        HTF_TFs[3] = TF_Month;
                        break;

                    // Special Case: 05, 60, 240, Daily
                    case 05:
                        HTF_TFs[0] = TF_5M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;

                    case 15:
                    default:
                        HTF_TFs[0] = TF_15M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;
                }

                Speed = ((int)Speed_Input) / 100.0;
                Length = 2.0 / (((double)Length_Input) + 1.0);
                LengthXSpeed = Length * Speed;
                pointBuffer = ((double)TickBuffer) * OneTick;

                if (SettingOriginalOrNew == 1)
                {
                    pointBuffersAll = new double[] { OneTick, OneTick, OneTick, OneTick, OneTick,
                    OneTick, OneTick, OneTick, OneTick, OneTick, OneTick };
                }
                else
                {
                    pointBuffersAll = new double[] { OneTick, OneTick * 2, OneTick * 3, OneTick * 4, OneTick * 6,
                    OneTick * 12, OneTick * 24, OneTick * 48, OneTick * 100, OneTick * 100, OneTick * 100 };
                }

                pointBuffers = new double[] { pointBuffersAll[HTF_TFs[0]],
                                        pointBuffersAll[HTF_TFs[1]],
                                        pointBuffersAll[HTF_TFs[2]],
                                        pointBuffersAll[HTF_TFs[3]] };


                sessEnd_ELTime = 100 * (Bars.Sessions[0].EndTime.Hours) + Bars.Sessions[0].EndTime.Minutes;

                // for StdDev
                StdDevLen2U = StdDev_Length;
                if (StdDev_Length > 500)
                {
                    StdDevLen2U = 500;
                }

                StdDevInvLen2U = 1.0 / (double)StdDevLen2U;

                // reset data 
                for (int idx = 0; idx < numOfCalcTFs; idx++)
                {
                    HTF_BarCnts[idx] = 0;
                    // reset closes
                    for (int j = 0; j < StdDevLen2U; j++)
                    {
                        Closes_Arr[idx, j] = 0.0;
                    }
                }
            }
            // EO: if (OKToRun)
            if (isDebugMode == true)
            {
                Output.WriteLine("=========================== Debugging in StartCalc() ===========================");
                Output.WriteLine("OneTick = {0}, OnePip = {1}", OneTick, OnePip);
                Output.WriteLine("vCategory = {0} ", vCategory);
                Output.WriteLine("Minute Interval = {0}", MntInterval);
                Output.WriteLine("Current Chartype: {0}", Bars.Info.Resolution.ChartType);
                Output.WriteLine("Bars.Session[0].EndTime.Hours = {0}, .Minutes = {1}", Bars.Sessions[0].EndTime.Hours, Bars.Sessions[0].EndTime.Minutes);
                Output.WriteLine("IB End time", sessEnd_ELTime);
                Output.WriteLine("StartCalc: 900: pointBuffer {0} TickBuffer {1} OneTick {2}", pointBuffer, TickBuffer, OneTick);

            }
        }

        protected override double CalcBar()
        {
            if (OKToRun && Bars.Status == EBarState.Close)
            {
                // EL Time
                int ELTime = 100 * (Bars.Time[0].Hour) + Bars.Time[0].Minute;
                barMFSO = mBarMFSO[0];


                // Loop through each TF
                for (int idx = 0; idx < numOfCalcTFs; idx++)
                {
                    int idxTF = HTF_TFs[idx];


                    // Define Bar Closed Condition for each TF
                    HTF_OnCloseTick = false;

                    switch (idxTF)
                    {
                        case TF_5M:
                            HTF_OnCloseTick = (barMFSO % 005) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_10M:
                            HTF_OnCloseTick = (barMFSO % 010) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_15M:
                            HTF_OnCloseTick = (barMFSO % 015) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_20M:
                            HTF_OnCloseTick = (barMFSO % 020) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_30M:
                            HTF_OnCloseTick = (barMFSO % 030) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_60M:
                            HTF_OnCloseTick = (barMFSO % 060) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_120M:
                            HTF_OnCloseTick = (barMFSO % 120) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_240M:
                            HTF_OnCloseTick = (barMFSO % 240) == 0 || (ELTime == sessEnd_ELTime);
                            break;

                        case TF_Day:
                            HTF_OnCloseTick = (ELTime == sessEnd_ELTime);
                            break;

                        case TF_Week:
                            HTF_OnCloseTick = (
                                /* 
                                // >> Need to define for other markets (than Forex)
                                 (vCategory == 2 && (ELTime == 1600 || ELTime == sessEnd_ELTime))
                                || (vCategory == 4 && ELTime == sessEnd_ELTime)
                                || (vCategory == 0 && ELTime == sessEnd_ELTime)
                                || 
                                */
                                // End of Week for Forex: end of Session on Friday
                                // May have rare exception
                                ((vCategory == 9 || vCategory == 12) && (ELTime == sessEnd_ELTime && Bars.Time[0].DayOfWeek == DayOfWeek.Friday)));
                            break;

                        case TF_Month:
                            myDOW = Bars.Time[0].DayOfWeek;
                            myDate = Bars.Time[0].Date;
                            HTF_OnCloseTick = (
                                /* 
                                // >> Need to define for other markets (than Forex)
                                 (vCategory == 2 && (ELTime == 1600 || ELTime == sessEnd_ELTime))
                                || (vCategory == 4 && ELTime == sessEnd_ELTime)
                                || (vCategory == 0 && ELTime == sessEnd_ELTime)
                                || 
                                */
                                // End of Months for Forex: end of Session on Friday
                                // May have rare exception
                                ((vCategory == 9 || vCategory == 12) && (ELTime == sessEnd_ELTime && _isLastDayOfMonth_f(myDOW, myDate))));
                            break;

                        default:
                            // default, not defined
                            HTF_OnCloseTick = false;
                            break;

                    }

                    HTF_OnCloseTicks[idx].Value = HTF_OnCloseTick;

                    if (HTF_BarCnts[idx] > 0)
                    {
                        if (HTF_OnCloseTicks[idx][1])
                        {
                            // HTF Opening tick
                            // Shift stack if need to keep track of historical value of HTF OHLC
                            for (int j = StdDevLen2U - 1; j > 0; j--)
                            {
                                Closes_Arr[idx, j] = Closes_Arr[idx, j - 1];
                            }

                            // construct OHLC of HTF from Data1
                            HTF_Opens[idx].Value = Bars.Open[0];
                            HTF_Highs[idx].Value = Bars.High[0];
                            HTF_Lows[idx].Value = Bars.Low[0];
                        }
                        else
                        {
                            // Keep track of High/Low
                            if (Bars.High[0] > HTF_Highs[idx].Value)
                            {
                                HTF_Highs[idx].Value = Bars.High[0];
                            }
                            if (Bars.Low[0] < HTF_Lows[idx].Value)
                            {
                                HTF_Lows[idx].Value = Bars.Low[0];
                            }

                        }

                        HTF_Closes[idx].Value = Bars.Close[0];
                        // the latest close
                        Closes_Arr[idx, 0] = Bars.Close[0];

                        if (HTF_OnCloseTicks[idx][0])
                        {
                            // 0.5 * (Open + Close)
                            HTF_OpenCloseAvgs[idx].Value = 0.5 * (HTF_Opens[idx][0] + HTF_Closes[idx][0]);

                            // 0.25 * (O + H + L + C)
                            HTF_OHLCAvgs[idx].Value = 0.25 * (HTF_Highs[idx][0] + HTF_Lows[idx][0]
                                + HTF_Opens[idx][0] + HTF_Closes[idx][0]);

                            //------------------------------- Speed && Length Calculations -------------------------------
                            switch (Price_Input)
                            {
                                case 2:
                                    HTF_Prices[idx].Value = HTF_OpenCloseAvgs[idx][0];
                                    break;

                                case 3:
                                    HTF_Prices[idx].Value = HTF_OHLCAvgs[idx][0];
                                    break;

                                case 1:
                                default:
                                    // default & case 1 are the same
                                    HTF_Prices[idx].Value = HTF_Closes[idx][0];
                                    break;
                            }

                            //------------------------------- PAL Calculations -------------------------------
                            // Assume computation is done on Closed Bar!!!
                            vPAL1s[idx].Value = vPALs[idx].Value;

                            vPALs[idx].Value = (LengthXSpeed * HTF_Prices[idx][0])
                                + ((1 - LengthXSpeed) * vPAL1s[idx].Value);

                            if (Math.Abs(vPALs[idx].Value - vPAL1s[idx].Value) < pointBuffers[idx])
                                vPALs[idx].Value = vPAL1s[idx].Value;

                            //------------------------------- PAL Plot Color -------------------------------
                            if (vPALs[idx][0] > vPAL1s[idx].Value)
                                vActiveClrNum[idx].Value = Upper_Active_ClrNum;
                            else if (vPALs[idx][0] < vPAL1s[idx].Value)
                                vActiveClrNum[idx].Value = Lower_Active_ClrNum;

                            // Calc StdDev on HTF bar close only
                            closeSum = 0.0;
                            diffSqSum = 0.0;
                            // Use > to prevent the use of the first data point on left side of chart
                            // as it may not be a complete HTF bar
                            if (HTF_BarCnts[idx] > StdDevLen2U)
                            {
                                // Have enough data, start computation
                                // Calc Avg
                                for (int j = 0; j < StdDevLen2U; j++)
                                {
                                    closeSum = closeSum + Closes_Arr[idx, j];
                                }
                                closeAvg = closeSum * StdDevInvLen2U;

                                // Calc Avg of ((Close - CloseAvg)^2)
                                for (int j = 0; j < StdDevLen2U; j++)
                                {
                                    diff = Closes_Arr[idx, j] - closeAvg;
                                    diffSqSum = diffSqSum + diff * diff;
                                }
                                diffSqAvg = diffSqSum * StdDevInvLen2U;

                                // Calc Std Dev
                                vStDevs[idx].Value = Math.Sqrt(diffSqAvg);

                            }

                        }

                    }
                    // EO: if (HTF_BarCnts[idx] > 0)

                    if (HTF_OnCloseTicks[idx][0])
                    {
                        HTF_BarCnts[idx] = HTF_BarCnts[idx] + 1;
                    }
                }

                /*
                if (Price_Trend_On == 1)
                {
                    Value40.Value = trend_Tool_Price_Input1.Value;

                    // LONG PAL && DOWN TREND 
                    if (vActiveClrNum[0].Value == Upper_Active_ClrNum && Value40[0] < 0)
                        PAL_Width.Value = 0;
                    else
                    // SHORT PAL && UP TREND 
                    if (vActiveClrNum[0].Value == Lower_Active_ClrNum && Value40[0] > 0)
                        PAL_Width.Value = 0;
                    else
                    // LONG PAL && UP TREND 
                    if (vActiveClrNum[0].Value == Upper_Active_ClrNum && Value40[0] > 0)
                        PAL_Width.Value = Value40[0];
                    else
                    // SHORT PAL && DOWN TREND 
                    if (vActiveClrNum[0].Value == Lower_Active_ClrNum && Value40[0] < 0)
                        PAL_Width.Value = Math.Abs(Value40[0]);
                }

                if (Price_Trend_On != 1)
                    PAL_Width.Value = 2;
                */
                if (isDebugMode == true)
                {
                    Output.WriteLine("=========================== Debugging in CalcBar() ===========================");
                    Output.WriteLine("EL Time: {0}", ELTime);
                    Output.WriteLine("");
                }
            }
            // EO: if (OKToRun && Bars.Status == EBarState.Close)

            return 1.0;
           
        }

        private int Mod(int x, int y)
        {
            return (x % y);
        }

        private bool HP_SessionStartBarMnt_f()
        {
            bool fRet = false;

            if (Bars.Status == EBarState.Close)
            {
                TimeSpan posOpenTime = Bars.TimeValue.TimeOfDay
                    - TimeSpan.FromMinutes(Bars.Info.Resolution.Size);

                if (posOpenTime == Bars.Sessions[0].StartTime)
                {
                    fRet = true;
                }
            }

            return fRet;
        }

        private bool _isLastDayOfMonth_f(DayOfWeek thisDOW, DateTime thisDate)
        {
            bool isLastDayOfMonth;
            DateTime nextBusinessDay;

            if (thisDOW == DayOfWeek.Friday)
            {
                nextBusinessDay = thisDate.AddDays(3.0);
            }
            else
            {
                nextBusinessDay = thisDate.AddDays(1.0);
            }

            if (thisDate.Month != nextBusinessDay.Month)
            {
                isLastDayOfMonth = true;
            }
            else
            {
                isLastDayOfMonth = false;
            }

            return isLastDayOfMonth;
        }

    }
}
