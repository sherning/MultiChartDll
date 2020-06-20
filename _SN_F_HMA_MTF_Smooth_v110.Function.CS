/*
_SN_F_HMA_MTF_Smooth_v110

_SN_HMAnStdDev_162_f 20191217

After temp dev code _SN_PALnStdDev_161_f
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PowerLanguage.Function
{
    public sealed class _SN_F_HMA_MTF_Smooth_v110 : FunctionSimple<System.Double>
    {
        // Data1 only
        public _SN_F_HMA_MTF_Smooth_v110 (CStudyControl _master) : base(_master) { }

        public int HMA_Length { get; set; }
        public int Price_Input { get; set; }

        public int Smoothness { get; set; }
        public int StdDev_Length { get; set; }
        // StdDev_DataType 1 for population, 2 for sample
        public int StdDev_DataType { get; set; }

        public int PivotDuration { get; set; }

        // to remove SettingOriginalOrNew and TickBuffer
        private int SettingOriginalOrNew = 1;
        private int TickBuffer = 1;

        // Number of Calculation Time Frames for this instance + added 30 min
        private const int numOfCalcTFs = 5;

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

        // Smoothness fields
        double alpha, gain;

        // int numbers for each Upper_Active_Color_Name and Lower_Active_Color_Name
        private const int Upper_Active_ClrNum = 1;
        private const int Lower_Active_ClrNum = 2;

        // Sufficient to use VariableObject for vHAMs
        private List<VariableObject<double>> vHMAs = new List<VariableObject<double>>();

        private List<VariableObject<double>> vHMAs_Temp = new List<VariableObject<double>>();
        private List<VariableObject<double>> vHMA1s_Temp = new List<VariableObject<double>>();

        private List<VariableObject<double>> vHMA1s = new List<VariableObject<double>>();
        private List<VariableSeries<int>> vActiveClrNum = new List<VariableSeries<int>>();
        private List<VariableObject<double>> vStDevs = new List<VariableObject<double>>();

        private List<VariableObject<double>> vPvtLowPrices = new List<VariableObject<double>>();
        private List<VariableObject<DateTime>> vPvtLowTimes = new List<VariableObject<DateTime>>();

        private List<List<double>> PriceList = new List<List<double>>();

        private int[] HTF_TFs;
        private int[] HTF_BarCnts;

        private List<VariableSeries<bool>> HTF_OnCloseTicks = new List<VariableSeries<bool>>();

        // >> Need to use arrays if we need price history
        private List<VariableSeries<double>> HTF_Prices = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Opens = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Closes = new List<VariableSeries<double>>();

        private List<VariableSeries<double>> HTF_Highs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_Lows = new List<VariableSeries<double>>();
        // Keep track of the Low
        // private List<VariableSeries<DateTime>> HTF_DateTimeAtLows = new List<VariableSeries<DateTime>>();

        private List<VariableSeries<double>> HTF_OpenCloseAvgs = new List<VariableSeries<double>>();
        private List<VariableSeries<double>> HTF_OHLCAvgs = new List<VariableSeries<double>>();
        private List<List<double>> PriceLists = new List<List<double>>();
        private List<_HMA_List_f> mHMALists = new List<_HMA_List_f>();

        private double OnePip, OneTick;
        private int vCategory, sessEnd_ELTime;
        private double pointBuffer;
        private double[] pointBuffersAll;
        private double[] pointBuffers;

        int[] dataCnts = new int[5];
        // index1 is for TF, index2 is data point number for the time frame
        // Max StdDevLen2U is limited to 201
        int HTFArrSizeMax = 201;
        // Closes_Arr: idx1 = HTF, idx2 is HTF bars back
        double[,] Closes_Arr = new double[5, 201];

        // Lows_Arr: idx1 = HTF, idx2 is HTF bars back
        // Assume PivotDuration <= 20 (Array size 2)
        double[,] Lows_Arr = new double[5, 20];
        DateTime[,] LowTimes_Arr = new DateTime[5, 20];

        public ISeries<double> HMA_X01 { get { return vHMAs[0]; } }
        public ISeries<double> HMA_X04 { get { return vHMAs[1]; } }
        public ISeries<double> HMA_X16 { get { return vHMAs[2]; } }
        public ISeries<double> HMA_X64 { get { return vHMAs[3]; } }

        // public ISeries<int> vActiveClrNum_5 { get { return vActiveClrNum[0]; } }

        public ISeries<double> HMA1_X01 { get { return vHMA1s[0]; } }
        public ISeries<double> HMA1_X04 { get { return vHMA1s[1]; } }
        public ISeries<double> HMA1_X16 { get { return vHMA1s[2]; } }
        public ISeries<double> HMA1_X64 { get { return vHMA1s[3]; } }

        // Check for StdDev > 0 before using the value
        public ISeries<double> StdDev_X01 { get { return vStDevs[0]; } }
        public ISeries<double> StdDev_X04 { get { return vStDevs[1]; } }
        public ISeries<double> StdDev_X16 { get { return vStDevs[2]; } }
        public ISeries<double> StdDev_X64 { get { return vStDevs[3]; } }

        public ISeries<double> PvtLowPrice_X01 { get { return vPvtLowPrices[0]; } }
        public ISeries<double> PvtLowPrice_X04 { get { return vPvtLowPrices[1]; } }
        public ISeries<double> PvtLowPrice_X16 { get { return vPvtLowPrices[2]; } }
        public ISeries<double> PvtLowPrice_X64 { get { return vPvtLowPrices[3]; } }

        public ISeries<DateTime> PvtLowTime_X01 { get { return vPvtLowTimes[0]; } }
        public ISeries<DateTime> PvtLowTime_X04 { get { return vPvtLowTimes[1]; } }
        public ISeries<DateTime> PvtLowTime_X16 { get { return vPvtLowTimes[2]; } }
        public ISeries<DateTime> PvtLowTime_X64 { get { return vPvtLowTimes[3]; } }

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
            HTF_TFs = new int[] { 0, 0, 0, 0, 0 };
            HTF_BarCnts = new int[] { 0, 0, 0, 0, 0 };

            // create variable objects, function objects, plot objects etc.
            for (int j = 0; j < numOfCalcTFs; j++)
            {
                // vHMAs.Add(new VariableSeries<double>(this));
                vHMAs_Temp.Add(new VariableObject<double>(this));
                vHMA1s_Temp.Add(new VariableObject<double>(this));

                vHMAs.Add(new VariableObject<double>(this));
                vHMA1s.Add(new VariableObject<double>(this));
                vActiveClrNum.Add(new VariableSeries<int>(this));
                vStDevs.Add(new VariableObject<double>(this));

                vPvtLowPrices.Add(new VariableObject<double>(this));
                vPvtLowTimes.Add(new VariableObject<DateTime>(this));

                PriceLists.Add(new List<double>());
                mHMALists.Add(new _HMA_List_f(this));
            }

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

        private int HMA_LenM1;

        protected override void StartCalc()
        {
            // trend_Tool_Price_Input1.Price_Input = this.Price_Input;
            alpha = 2.0 / (HMA_Length + 1.0);
            gain = (Smoothness - 1.0) / 100.0;

            OnePip = (10.0 * Bars.Info.MinMove / Bars.Info.PriceScale);
            OneTick = (Bars.Info.MinMove / Bars.Info.PriceScale);

            vCategory = (int)Bars.Info.Category;
            // Need to include 9
            if (vCategory != 0 && vCategory != 2 && vCategory != 9 && vCategory != 12)
                vCategory = 4;

            int MntInterval = (int)Bars.Info.Resolution.Duration().TotalMinutes;

            if (Bars.Info.Resolution.ChartType == ChartType.Regular
                && (MntInterval == 15 || MntInterval == 30 || MntInterval == 60 || MntInterval == 240))
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
                    case 30:
                        HTF_TFs[0] = TF_30M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;

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

                    case 15:
                    default:
                        HTF_TFs[0] = TF_15M;
                        HTF_TFs[1] = TF_60M;
                        HTF_TFs[2] = TF_240M;
                        HTF_TFs[3] = TF_Day;
                        break;
                }

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

                // Output.WriteLine("StartCalc: 900: pointBuffer {0} TickBuffer {1} OneTick {2}", pointBuffer, TickBuffer, OneTick);

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

                    for (int j = 0; j < PivotDuration; j++)
                    {
                        Lows_Arr[idx, j] = 0.0;
                    }

                    mHMALists[idx].Length = HMA_Length;
                    mHMALists[idx].PriceList = PriceLists[idx];
                }

                HMA_LenM1 = HMA_Length - 1;

            }
            // EO: if (OKToRun)
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
                    // Note that the bar end times defined do NOT match with the MC.Net IB EUR.USD chart bar end time
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

                            // Keep track of PvtLowPrice and PvtLowTime in PivotDuration
                            for (int j = PivotDuration - 1; j > 0; j--)
                            {
                                Lows_Arr[idx, j] = Lows_Arr[idx, j - 1];
                                LowTimes_Arr[idx, j] = LowTimes_Arr[idx, j - 1];
                            }

                            // construct OHLC of HTF from Data1
                            HTF_Opens[idx].Value = Bars.Open[0];
                            HTF_Highs[idx].Value = Bars.High[0];
                            HTF_Lows[idx].Value = Bars.Low[0];

                            Lows_Arr[idx, 0] = Bars.Low[0];
                            LowTimes_Arr[idx, 0] = Bars.Time[0];
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

                                // Keep track of Low and Low time for this HTF bar
                                Lows_Arr[idx, 0] = Bars.Low[0];
                                LowTimes_Arr[idx, 0] = Bars.Time[0];
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

                            //------------------------------- HMA Calculations -------------------------------
                            // Assume computation is done on Closed Bar!!!
                            vHMA1s[idx].Value = vHMAs[idx].Value;
                            //vHMA1s_Temp[idx].Value = vHMAs_Temp[idx].Value;

                            if (PriceLists[idx].Count > HMA_LenM1)
                            {
                                PriceLists[idx].RemoveAt(0);
                            }
                            PriceLists[idx].Add(HTF_Prices[idx][0]);

                            if (PriceLists[idx].Count == HMA_Length)
                            {
                                // Note use HMA value only when it is > 0
                                vHMAs_Temp[idx].Value = mHMALists[idx].Value;

                                // vHMAs[idx].Value = mHMALists[idx].Value;
                                vHMAs[idx].Value = alpha * (vHMAs_Temp[idx].Value + gain * (HTF_Closes[idx].Value - vHMA1s[idx].Value))
                                                + (1.0 - alpha) * vHMA1s[idx].Value;
                            }

                            // No pointBuffers for HMA
                            // if (Math.Abs(vHMAs[idx].Value - vHMA1s[idx].Value) < pointBuffers[idx])
                            // vHMAs[idx].Value = vHMA1s[idx].Value;

                            //------------------------------- HMA Plot Color -------------------------------
                            if (vHMAs[idx][0] > vHMA1s[idx].Value)
                                vActiveClrNum[idx].Value = Upper_Active_ClrNum;
                            else if (vHMAs[idx][0] < vHMA1s[idx].Value)
                                vActiveClrNum[idx].Value = Lower_Active_ClrNum;

                            //--- PivotDuration Low Price and Time for suppprt -----------------------------
                            // On Change to Up color from down color
                            // Look for PvtLowPrice and PvtLowTime in the latest PivotDuration HTF bars 
                            if (vActiveClrNum[idx][0] == Upper_Active_ClrNum
                                && vActiveClrNum[idx][1] == Lower_Active_ClrNum)
                            {
                                double PvtLowPrice = Lows_Arr[idx, 0];
                                DateTime PvtLowTime = LowTimes_Arr[idx, 0];
                                for (int j = 1; j < PivotDuration; j++)
                                {
                                    if (Lows_Arr[idx, j] < PvtLowPrice)
                                    {
                                        PvtLowPrice = Lows_Arr[idx, j];
                                        PvtLowTime = LowTimes_Arr[idx, j];
                                    }
                                }

                                // Return the PivotDuration PvtLowPrice and PvtLowTime for this HTF (idx)
                                vPvtLowPrices[idx].Value = PvtLowPrice;
                                vPvtLowTimes[idx].Value = PvtLowTime;
                            }

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
