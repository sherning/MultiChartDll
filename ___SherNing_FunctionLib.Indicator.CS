using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PowerLanguage.Function;

#region MultiChart Function Library

namespace PowerLanguage.Function
{
    #region Price Action Moving Average 
    //==============================================================================
    // Name           : Price Action Moving Average
    // Description    : Calculates the PAMA for a given length
    // Version        : v.1.0.0
    // Date Created   : 20 - May - 2020
    // Time Taken     : 
    // Remarks        :
    //==============================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==============================================================================

    /* ------------------------------- Version 1.0.0 -------------------------------
     * Since, we are inheriting from FuctionSeries<double> we can use this[1] for the 
     * previous calc bar value.
     */

    public sealed class PriceActionMovingAverage : FunctionSeries<double>
    {
        // class properties
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public ISeries<double> _Price { get; set; }

        public PriceActionMovingAverage(CStudyControl m) : base(m)
        {

        }

        // class fields
        private double Alpha, Gain;
        private HullMovingAverage_v10 Hma;
        private VariableSeries<double> HmaValues;
        protected override void Create()
        {
            Hma = new HullMovingAverage_v10(this);
            HmaValues = new VariableSeries<double>(this);
        }
        protected override void StartCalc()
        {
            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;
            Hma._Length = _Length;
        }
        protected override double CalcBar()
        {
            Hma._AddData(_Price[0]);
            HmaValues.Value = Hma.Value;

            if (HmaValues[0] <= 0) return -1;

            double ret;

            // this[1] only works on FunctionSeries<T> only
            // this[1] refers to CalcBar() return value one bar ago.
            if (HmaValues[0] - this[1] > 0)
            {
                ret
                = Alpha * (HmaValues[0]
                + (-Gain) * (HmaValues[0] - this[1]))
                + (1.0 - Alpha) * this[1];
            }
            else if (HmaValues[0] - this[1] < 0)
            {
                ret
                = Alpha * (HmaValues[0]
                + Gain * (HmaValues[0] - this[1]))
                + (1.0 - Alpha) * this[1];
            }
            else
            {
                ret
               = Alpha * (HmaValues[0]
               + 0.0 * (HmaValues[0] - this[1]))
               + (1.0 - Alpha) * this[1];
            }

            return ret;
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    #endregion

    #region Standard Deviation
    //==========================================================================================
    // Name           : Standard Deviation Calculator
    // Description    : Returns the std dev for a given length over a given set of values
    // Version        : v.1.0
    // Date Created   : 22 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v.1.0
     *  - Standard Version is based on bar closed.
     *  - Public methods if Std Dev is not calculated on every bar.
     *  ----------------------------------- Version History -----------------------------------

     *  ---------------------------------------- Notes ----------------------------------------
     *  - 
     *  ---------------------------------------- Notes ----------------------------------------
     */
    public sealed class StandardDeviationCalculatorManual : FunctionSeries<string>
    {
        // class properties
        public int _Length { get; set; }
        public bool _PopulationStdDev { get; set; }

        // class fields
        private double[] DataSet;
        private int Count;

        // class constructor
        public StandardDeviationCalculatorManual (CStudyControl m) : base(m)
        {

        }
        protected override void StartCalc()
        {
            // ensure that length is at least 1.
            if (_Length < 1) _Length = 1;

            // Data set for manual entry
            DataSet = new double[_Length];

            // reset count
            Count = 0;
        }
        protected override string CalcBar()
        {
            return "Use manual methods";
        }

        public void Add(double data)
        {
            // shift all the data back by 1
            for (int i = _Length - 1; i > 0; i--)
                DataSet[i] = DataSet[i - 1];

            // Add newest data to the front
            DataSet[0] = data;

            // Count number of data added.
            Count++;

            if (Count > _Length) Count--;
        }

        public double Calculate()
        {
            double mean = 0;
            for (int i = 0; i < _Length; i++)
                mean += DataSet[i];

            // mean = total sum / length
            mean /= _Length;

            // calculate variance sigma ^ 2
            double variance = 0;
            for (int i = 0; i < _Length; i++)
                variance += Math.Pow(DataSet[i] - mean, 2);

            // calculate population variance
            if (_PopulationStdDev) variance /= _Length;
            else variance /= (_Length - 1);

            // std dev = sqrt(variance)
            return Math.Sqrt(variance);
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class StandardDeviationCalculator : FunctionSeries<double>
    {
        // class properties
        public int _Length { get; set; }
        public bool _PopulationStdDev { get; set; }
        public ISeries<double> _Price { get; set; }

        // class constructor
        public StandardDeviationCalculator(CStudyControl m) : base(m)
        {

        }
        protected override void StartCalc()
        {
            // ensure that length is at least 1.
            if (_Length < 1) _Length = 1;

            // null reference check
            if (_Price == null) _Price = Bars.Close;
        }
        protected override double CalcBar()
        {
            double mean = 0;
            for (int i = 0; i < _Length; i++)
                mean += _Price[i];

            // mean = total sum / length
            mean /= _Length;

            // calculate variance sigma ^ 2
            double variance = 0;
            for (int i = 0; i < _Length; i++)
                variance += Math.Pow(_Price[i] - mean, 2);

            // calculate population variance
            if (_PopulationStdDev) variance /= _Length;
            else variance /= (_Length - 1);

            // std dev = sqrt(variance)
            return Math.Sqrt(variance);
        }
    }

    #endregion

    #region Moving Average Pivots
    //==========================================================================================
    // Name           : Moving Average Pivots
    // Description    : Returns the pivot value for a given length and set of data
    // Version        : v.1.0
    // Date Created   : 22 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v.1.0
     *  - 
     *  ----------------------------------- Version History -----------------------------------

     *  ---------------------------------------- Notes ----------------------------------------
     *  - 
     *  ---------------------------------------- Notes ----------------------------------------
     */

    public sealed class MovingAveragePivotsManual : FunctionSeries<double>
    {
        // Manual Version :
        // Requires adding of data series, data series high, data series low

        // class properties
        public int _Length { get; set; }

        // get accessors
        public double PivotHighs { get { return GetPivotHigh(); } }
        public double PivotLows { get { return GetPivotLow(); } }

        // class fields, 3 points required to determine a turning point
        private const int DataPoints = 3;
        private double HighestHigh, LowestLow;
        private List<double> ListOfHighs, ListOfLows, DataSet;

        // class constructors
        public MovingAveragePivotsManual (CStudyControl m) : base(m) { }

        protected override void StartCalc()
        {
            // ensure sufficient length
            if (_Length < 1) _Length = 1;

            // instaniate list in startcalc
            DataSet = new List<double>(DataPoints);
            ListOfHighs = new List<double>(_Length);
            ListOfLows = new List<double>(_Length);

            // reset class fields
            HighestHigh = LowestLow = 0;

            // prevent index out of range exception
            for (int i = 0; i < DataPoints; i++)
                DataSet.Add(0);

            for (int i = 0; i < _Length; i++)
            {
                ListOfHighs.Add(0);
                ListOfLows.Add(0);
            }
        }
        protected override double CalcBar()
        {
            return 0;
        }

        #region Public Methods
        public void Add(double data)
        {
            // front load data
            DataSet.Insert(0, data);

            // remove overflow
            if (DataSet.Count > DataPoints)
                DataSet.RemoveAt(DataPoints);
        }
        public void AddHigh(double data)
        {
            // front load data
            ListOfHighs.Insert(0, data);

            // remove overflow, last element is length - 1
            if (ListOfHighs.Count > _Length)
                ListOfHighs.RemoveAt(_Length);
        }
        public void AddLow(double data)
        {
            // front load data
            ListOfLows.Insert(0, data);

            // remove overflow, last element is length - 1
            if (ListOfLows.Count > _Length)
                ListOfLows.RemoveAt(_Length);
        }
        #endregion

        #region Private Methods
        private bool IsPivotHigh()
        {
            // Peak:  Data[0] < Data[1] > Data[2]  
            if (DataSet[0] < DataSet[1] && DataSet[1] > DataSet[2]) return true;

            return false;
        }

        private bool IsPivotLow()
        {
            // Trough: Data[0] > Data[1] < Data[2]
            if (DataSet[0] > DataSet[1] && DataSet[1] < DataSet[2]) return true;

            return false;
        }

        private double GetPivotHigh()
        {
            // if true update highest high value
            if (IsPivotHigh() == true)
            {
                double maxValue = ListOfHighs[0];

                for (int i = 1; i < _Length; i++)
                {
                    if (ListOfHighs[i] > maxValue) 
                        maxValue = Math.Max(maxValue, ListOfHighs[i]);
                }

                HighestHigh = maxValue;
            }

            return HighestHigh;
        }

        private double GetPivotLow()
        {
            // if true update lowest low value
            if (IsPivotLow() == true)
            {
                double minValue = ListOfLows[0];

                for (int i = 1; i < _Length; i++)
                {
                    if (ListOfLows[i] < minValue)
                        minValue = Math.Min(minValue, ListOfLows[i]);
                }

                LowestLow = minValue;
            }

            return LowestLow;
        }
        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
    public sealed class MovingAveragePivot : FunctionSeries<string>
    {
        // class properties
        public ISeries<double> _Data { private get; set; }
        public int _MaxBarsBack { get; set; }

        // return these properties.
        public ISeries<double> _PivotHigh { get { return HighestHigh; } }
        public ISeries<double> _PivotLow { get { return LowestLow; } }


        // class fields
        private VariableSeries<double> HighestHigh, LowestLow;

        public MovingAveragePivot(CStudyControl m) : base(m)
        {

        }
        protected override void Create()
        {
            HighestHigh = new VariableSeries<double>(this);
            LowestLow = new VariableSeries<double>(this);
        }
        protected override void StartCalc()
        {
            // ensure minimum Length
            if (_MaxBarsBack < 1) _MaxBarsBack = 1;
        }
        protected override string CalcBar()
        {
            if (IsPivotHigh()) CalcHighestHigh();
            if (IsPivotLow()) CalcLowestLow();

            // no need for return type and value.
            return string.Empty;
        }

        private bool IsPivotHigh()
        {
            // Peak:  Data[0] < Data[1] > Data[2]  
            if (_Data[0] < _Data[1] && _Data[1] > _Data[2]) return true;

            return false;
        }

        private bool IsPivotLow()
        {
            // Trough: Data[0] > Data[1] < Data[2]
            if (_Data[0] > _Data[1] && _Data[1] < _Data[2]) return true;

            return false;
        }

        private void CalcHighestHigh()
        {
            double maxValue = Bars.High[0];

            for (int i = 1; i < _MaxBarsBack; i++)
            {
                if (Bars.High[i] > maxValue)
                    maxValue = Math.Max(Bars.High[i], maxValue);
            }

            HighestHigh.Value = maxValue;
        }

        private void CalcLowestLow()
        {
            double minValue = Bars.Low[0];
            for (int i = 1; i < _MaxBarsBack; i++)
            {
                if (Bars.Low[i] < minValue)
                    minValue = Math.Min(Bars.Low[i], minValue);
            }

            LowestLow.Value = minValue;
        }

        #region Manual Mode

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }

    #endregion

    #region Multi Time Frame Closing Ticks
    //==========================================================================================
    // Name           : Multi-timeframe Closing Ticks
    // Description    : Function returns true timeframe selected is closing tick
    // Version        : v.1.0
    // Date Created   : 22 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v.1.0
     *  - 
     *  ----------------------------------- Version History -----------------------------------

     *  ---------------------------------------- Notes ----------------------------------------
     *  - Check if function series store the previous calculation info.
     *  ---------------------------------------- Notes ----------------------------------------
     */

    public sealed class MultiTimeFrameClosingTick : FunctionSeries<bool>
    {
        // class properties
        public int? _TimeFrame { get; set; }
        public string _TimeFrameSelected
        {
            get
            {
                if (_TimeFrame == null) return "No Timeframe Selected";
                else return DisplayTimeFrameSize(_TimeFrame.Value);
            }
        }
        public MultiTimeFrameClosingTick(CStudyControl cStudyControl) : base(cStudyControl)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }
        protected override bool CalcBar()
        {
            // calculate only on closing tick
            if (Bars.Status != EBarState.Close) 
                return false;

            return IsTimeFrameOnCloseTick(_TimeFrame.Value);
        }

        private bool IsTimeFrameOnCloseTick(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            bool isCloseTick;
            int currTimeStartMin = GetCurrentTimeFromSessionOpen();
            int currTimeStartReg = GetCurrentTime();
            int sessEndTimeReg = GetSessionEndTime();

            switch (timeframe)
            {
                case 0: // 5 min
                    isCloseTick = (currTimeStartMin % 005) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 1: // 10 min
                    isCloseTick = (currTimeStartMin % 010) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 2: // 15 min
                    isCloseTick = (currTimeStartMin % 015) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 3: // 20 min
                    isCloseTick = (currTimeStartMin % 020) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 4: // 30 min
                    isCloseTick = (currTimeStartMin % 030) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 5: // 60 min
                    isCloseTick = (currTimeStartMin % 060) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 6: // 120 min
                    isCloseTick = (currTimeStartMin % 120) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 7: // 240 min
                    isCloseTick = (currTimeStartMin % 240) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 8: // daily
                    isCloseTick = (currTimeStartReg == sessEndTimeReg);
                    break;

                case 9: // weekly
                    bool isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = GetLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfYear() == true && GetCurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }


            return isCloseTick;
        }
        private bool IsLastDayOfYear()
        {
            DateTime lastDay = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDay.Date) return true;

            return false;
        }
        private bool GetLastTradingDayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            int day = Bars.TimeValue.Day;

            // get the total number of days in this month.
            int totalDaysInMonth = DateTime.DaysInMonth(year, month);

            // if last day of month is current bar
            if (Bars.TimeValue.Day == totalDaysInMonth) return true;

            // else check for last trading day
            DateTime lastDayOfMth = new DateTime(year, month, totalDaysInMonth);

            // if the last day of the month is a weekend
            if (lastDayOfMth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMth.DayOfWeek == DayOfWeek.Sunday)
            {
                // if current day is the last friday of the month.
                if (day == GetLastFridayOfMonth()) return true;
            }

            return false;
        }
        private int GetLastFridayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }
        private int GetSessionEndTime()
        {
            // regular, not in total minutes.
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }
        private int GetCurrentTime()
        {
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }
        private int GetCurrentTimeFromSessionOpen()
        {
            // minutes from open
            int minFromSessOpen = 0;

            // total min from Midnight. e.g. 1050 / 60 = 17.5 hours
            int sessStartMin = (int)Bars.Sessions[0].StartTime.TotalMinutes;
            int sessEndMin = (int)Bars.Sessions[0].EndTime.TotalMinutes;

            // current time in minutes from midnight
            int currTimeMin = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // no. of minutes in a day 
            int totalMinDay = 24 * 60;

            // if no condition applies. Return 0.
            if (currTimeMin >= sessStartMin && currTimeMin < totalMinDay)
            {
                minFromSessOpen = currTimeMin - sessStartMin;
            }
            else if (currTimeMin > 0 && currTimeMin <= sessEndMin)
            {
                minFromSessOpen = totalMinDay - sessStartMin + currTimeMin;
            }
            else if (currTimeMin == 0)
            {
                minFromSessOpen = totalMinDay - sessStartMin;
            }

            return minFromSessOpen;
        }
        private string DisplayTimeFrameSize(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            switch (timeframe)
            {
                case 0:
                    return "5 min";
                case 1:
                    return "10 min";
                case 2:
                    return "15 min";
                case 3:
                    return "20 min";
                case 4:
                    return "30 min";
                case 5:
                    return "60 min";
                case 6:
                    return "120 min";
                case 7:
                    return "240 min";
                case 8:
                    return "Daily";
                case 9:
                    return "Weekly";
                case 10:
                    return "Monthly";
            }

            return "Incorrect timeframe";
        }
    }

    #endregion

    #region Hull Moving Average
    //==============================================================================
    // Name           : Hull Moving Average
    // Description    : Calculates the HMA for a given length
    // Version        : v.1.0.0
    // Date Created   : 20 - May - 2020
    // Time Taken     : 
    // Remarks        :
    //==============================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==============================================================================

    /* ------------------------------- Version 1.1 -------------------------------
     *  v1.0
     *  Insertion method is used. When Value property is called the CalcBar is called 
     *  in tandem. Ebarstate == close will not remedy this situation.
     *  The only solution is to call this function only once per bar.
     *  
     *  v1.1
     *  Use AddData(double method)
     *  
     *  v1.3
     *  Use ISeries<double> instead of List<double>
     */
    public sealed class HullMovingAverage : FunctionSeries<double>
    {
        // class properties
        public int _Length { get; set; }


        // class constructor
        public HullMovingAverage(CStudyControl m) : base(m) { }
        public HullMovingAverage(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private int[] Lengths;
        private List<double>[] PriceLists;

        protected override void StartCalc()
        {
            // ensure that the minimum length is 1
            if (_Length < 1) _Length = 1;

            // to compute HMA you need 3 length.
            // and to avoid ambiguity dont use Lengths[0]
            Lengths = new int[4];

            // Calculate only when length value has changed
            int halvedLength;
            if ((Math.Ceiling((double)(_Length / 2)) - (_Length / 2)) <= 0.5)
            {
                halvedLength = (int)Math.Ceiling((double)(_Length / 2));
            }
            else
            {
                halvedLength = (int)Math.Floor((double)(_Length / 2));
            }

            int sqrRootLength;
            double sqLength = Math.Sqrt(_Length);

            if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
            {
                sqrRootLength = (int)Math.Ceiling(sqLength);
            }
            else
            {
                sqrRootLength = (int)Math.Floor(sqLength);
            }

            //Lengths[0] = 0 by default
            Lengths[1] = halvedLength;
            Lengths[2] = _Length;
            Lengths[3] = sqrRootLength;

            // create an array of 4 price list.
            PriceLists = new List<double>[4];

            //PriceLists[0] = null;
            PriceLists[1] = new List<double>();
            PriceLists[2] = new List<double>();
            PriceLists[3] = new List<double>();
        }
        public void _AddData(double data)
        {
            // using insert method, i got to take caution.
            PriceLists[1].Insert(0, data);
            PriceLists[2].Insert(0, data);

            // clean up list for unwanted values
            if (PriceLists[1].Count > Lengths[1])
                PriceLists[1].RemoveAt(Lengths[1]);

            if (PriceLists[2].Count > Lengths[2])
                PriceLists[2].RemoveAt(Lengths[2]);
        }
        protected override double CalcBar()
        {
            // only call this once on the last bar tick.
            if (Bars.Status == EBarState.Close)
            {
                // insufficient data
                if (PriceLists[2].Count < _Length) return -1;

                // calculate 
                double wma1 = 2.0 * WeightedAverage(PriceLists[1], Lengths[1]);
                double wma2 = WeightedAverage(PriceLists[2], Lengths[2]);

                // add the calculated data to pricelist 3
                PriceLists[3].Insert(0, wma1 - wma2);

                // check for sufficient data
                if (PriceLists[3].Count > Lengths[3])
                {
                    // clean up list for unwanted values
                    PriceLists[3].RemoveAt(Lengths[3]);

                    // calculate the weighted average and return.
                    return WeightedAverage(PriceLists[3], Lengths[3]);
                }
            }

            return -1;
        }

        private double WeightedAverage(List<double> dataSet, int length)
        {
            // the recent data is more heavily weighted
            if (length < 1) return 0;

            double denominator = 1 / ((length + 1) * length * 0.5);
            double weightedSum = 0;

            // newest data is front loaded.
            for (int i = 0; i < length; i++)
                weightedSum += (length - i) * dataSet[i];

            return weightedSum * denominator;
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class HullMovingAverage_v13 : FunctionSeries<double>
    {
        // class properties
        public int _Length { get; set; }
        public VariableSeries<double> _Price { private get; set; }


        // class constructor
        public HullMovingAverage_v13(CStudyControl m) : base(m) { }
        public HullMovingAverage_v13(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private int[] Lengths;
        private VariableSeries<double>[] PriceLists;
        protected override void Create()
        {
            // create an array of 4 price list.
            PriceLists = new VariableSeries<double>[4];

            //PriceLists[0] = null;
            PriceLists[1] = new VariableSeries<double>(this);
            PriceLists[2] = new VariableSeries<double>(this);
            PriceLists[3] = new VariableSeries<double>(this);
        }
        protected override void StartCalc()
        {
            // ensure that the minimum length is 1
            if (_Length < 1) _Length = 1;

            // to compute HMA you need 3 length.
            // and to avoid ambiguity dont use Lengths[0]
            Lengths = new int[4];

            // Calculate only when length value has changed
            int halvedLength;
            if ((Math.Ceiling((double)(_Length / 2)) - (_Length / 2)) <= 0.5)
            {
                halvedLength = (int)Math.Ceiling((double)(_Length / 2));
            }
            else
            {
                halvedLength = (int)Math.Floor((double)(_Length / 2));
            }

            int sqrRootLength;
            double sqLength = Math.Sqrt(_Length);

            if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
            {
                sqrRootLength = (int)Math.Ceiling(sqLength);
            }
            else
            {
                sqrRootLength = (int)Math.Floor(sqLength);
            }

            //Lengths[0] = 0 by default
            Lengths[1] = halvedLength;
            Lengths[2] = _Length;
            Lengths[3] = sqrRootLength;

            // assign the reference from the incoming source to PriceLists[1] and [2]
            // it has to be in calc bar to received the latest price.
            PriceLists[1] = PriceLists[2] = _Price;
        }

        protected override double CalcBar()
        {
            // only call this once on the last bar tick.
            if (Bars.Status == EBarState.Close)
            {
                // assign the reference from the incoming source to PriceLists[1] and [2]
                // it has to be in calc bar to received the latest price.
                //PriceLists[1].Value = _Price[0];
                //PriceLists[2].Value = _Price[0];

                // calculate 
                double wma1 = 2.0 * PriceLists[1].WAverageCustom(Lengths[1]);
                double wma2 = PriceLists[2].WAverageCustom(Lengths[2]);

                // add the calculated data to pricelist 3
                PriceLists[3].Value = wma1 - wma2;

                // calculate the weighted average and return.
                return PriceLists[3].WAverageCustom(Lengths[3]);
            }

            return -1;
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class HullMovingAverage_v12 : FunctionSeries<double>
    {
        // class properties
        public int _Length { get; set; }

        // class constructor
        public HullMovingAverage_v12(CStudyControl m) : base(m) { }
        public HullMovingAverage_v12(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private int[] Lengths;
        private List<double>[] PriceLists;

        protected override void StartCalc()
        {
            // ensure that the minimum length is 1
            if (_Length < 1) _Length = 1;

            // to compute HMA you need 3 length.
            // and to avoid ambiguity dont use Lengths[0]
            Lengths = new int[4];

            // Calculate only when length value has changed
            int halvedLength;
            if ((Math.Ceiling((double)(_Length / 2)) - (_Length / 2)) <= 0.5)
            {
                halvedLength = (int)Math.Ceiling((double)(_Length / 2));
            }
            else
            {
                halvedLength = (int)Math.Floor((double)(_Length / 2));
            }

            int sqrRootLength;
            double sqLength = Math.Sqrt(_Length);

            if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
            {
                sqrRootLength = (int)Math.Ceiling(sqLength);
            }
            else
            {
                sqrRootLength = (int)Math.Floor(sqLength);
            }

            //Lengths[0] = 0 by default
            Lengths[1] = halvedLength;
            Lengths[2] = _Length;
            Lengths[3] = sqrRootLength;

            // create an array of 4 price list.
            PriceLists = new List<double>[4];

            //PriceLists[0] = null;
            PriceLists[1] = new List<double>();
            PriceLists[2] = new List<double>();
            PriceLists[3] = new List<double>();
        }
        public void _AddData(double data)
        {
            //prove that i called twice
            PriceLists[1].Insert(0, data);
            PriceLists[2].Insert(0, data);

            // clean up list for unwanted values
            if (PriceLists[1].Count > Lengths[1])
                PriceLists[1].RemoveAt(Lengths[1]);

            if (PriceLists[2].Count > Lengths[2])
                PriceLists[2].RemoveAt(Lengths[2]);
        }
        protected override double CalcBar()
        {
            // only call this once on the last bar tick.
            if (Bars.Status == EBarState.Close)
            {
                // insufficient data
                if (PriceLists[2].Count < _Length) return -1;

                // calculate 
                double wma1 = 2.0 * WeightedAverage(PriceLists[1], Lengths[1]);
                double wma2 = WeightedAverage(PriceLists[2], Lengths[2]);

                // add the calculated data to pricelist 3
                PriceLists[3].Insert(0, wma1 - wma2);

                // check for sufficient data
                if (PriceLists[3].Count > Lengths[3])
                {
                    // clean up list for unwanted values
                    PriceLists[3].RemoveAt(Lengths[3]);

                    // calculate the weighted average and return.
                    return WeightedAverage(PriceLists[3], Lengths[3]);
                }
            }

            return -1;
        }

        private double WeightedAverage(List<double> dataSet, int length)
        {
            // the recent data is more heavily weighted
            if (length < 1) return 0;

            double denominator = 1 / ((length + 1) * length * 0.5);
            double weightedSum = 0;

            // newest data is front loaded.
            for (int i = 0; i < length; i++)
                weightedSum += (length - i) * dataSet[i];

            return weightedSum * denominator;
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class HullMovingAverage_v11 : FunctionSeries<double>
    {
        // class properties
        public int _Length { get; set; }

        // class fields
        private int[] Lengths;
        private double[][] PriceLists;
        private int Count, Count3;

        // class constructor
        public HullMovingAverage_v11(CStudyControl m) : base(m) { }

        // class members
        protected override void Create()
        {
            // avoid using [0]
            Lengths = new int[4];
            PriceLists = new double[4][];
        }
        protected override void StartCalc()
        {
            // ensure minimum length before calculation
            if (_Length < 1) _Length = 1;

            // 3 lengths, avoid [0]
            Lengths = new int[4];

            // Calculate only when length value has changed
            int halvedLength;
            if ((Math.Ceiling((double)(_Length / 2)) - (_Length / 2)) <= 0.5)
            {
                halvedLength = (int)Math.Ceiling((double)(_Length / 2));
            }
            else
            {
                halvedLength = (int)Math.Floor((double)(_Length / 2));
            }

            int sqrRootLength;
            double sqLength = Math.Sqrt(_Length);

            if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
            {
                sqrRootLength = (int)Math.Ceiling(sqLength);
            }
            else
            {
                sqrRootLength = (int)Math.Floor(sqLength);
            }

            //Lengths[0] = 0 by default
            Lengths[1] = halvedLength;
            Lengths[2] = _Length;
            Lengths[3] = sqrRootLength;

            // Reset Count
            Count = Count3 = 0;

            // Avoid [0]
            PriceLists = new double[4][];

            //PriceList[0] = null by default
            PriceLists[1] = new double[Lengths[1]];
            PriceLists[2] = new double[Lengths[2]];
            PriceLists[3] = new double[Lengths[3]];
        }
        protected override double CalcBar()
        {
            // insufficient data
            if (Count != _Length) return 0;

            //PriceList1[lastvalue--] = PriceList[lastvalue--]
            for (int i = Lengths[1] - 1; i >= 0; i--)
                PriceLists[1][i] = PriceLists[2][i + Lengths[2] - Lengths[1]];

            // calculate 
            double wma1 = 2.0 * WeightedAverage(PriceLists[1], Lengths[1]);
            double wma2 = WeightedAverage(PriceLists[2], Lengths[2]);

            // shuffle to the front
            for (int i = 0; i < Lengths[3] - 1; i++)
                PriceLists[3][i] = PriceLists[3][i + 1];

            // Add to the back of the list
            PriceLists[3][Lengths[3] - 1] = wma1 - wma2;
            Count3++;

            // remove overflow
            if (Count3 > Lengths[3]) Count3--;

            if (Count3 == Lengths[3])
                return WeightedAverage(PriceLists[3], Lengths[3]);
            else
                return -1;
        }

        private double WeightedAverage(double[] dataSet, int length)
        {
            if (length < 1) return 0;

            double denominator = 1 / ((length + 1) * length * 0.5);
            double weightedSum = 0;

            for (int i = 0; i < length; i++)
                weightedSum += (length - i) * dataSet[length - 1 - i];

            return weightedSum * denominator;
        }

        public void _AddData(double data)
        {
            // Shift all data to the front of the list
            for (int i = 0; i < Lengths[2] - 1; i++)
                PriceLists[2][i] = PriceLists[2][i + 1];

            // Add latest data to end of list
            PriceLists[2][Lengths[2] - 1] = data;
            Count++;

            // Remove overflow
            if (Count > _Length) Count--;
        }
    }
    public sealed class HullMovingAverage_v10 : FunctionSeries<double>
    {
        // class properties
        public int _Length { get; set; }
        public double _Data { get; set; }

        // class constructor
        public HullMovingAverage_v10(CStudyControl m) : base(m) { }
        public HullMovingAverage_v10(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private int[] Lengths;
        private List<double>[] PriceLists;

        protected override void StartCalc()
        {
            // ensure that the minimum length is 1
            if (_Length < 1) _Length = 1;

            // to compute HMA you need 3 length.
            // and to avoid ambiguity dont use Lengths[0]
            Lengths = new int[4];

            // Calculate only when length value has changed
            int halvedLength;
            if ((Math.Ceiling((double)(_Length / 2)) - (_Length / 2)) <= 0.5)
            {
                halvedLength = (int)Math.Ceiling((double)(_Length / 2));
            }
            else
            {
                halvedLength = (int)Math.Floor((double)(_Length / 2));
            }

            int sqrRootLength;
            double sqLength = Math.Sqrt(_Length);

            if ((Math.Ceiling(sqLength) - sqLength) <= 0.5)
            {
                sqrRootLength = (int)Math.Ceiling(sqLength);
            }
            else
            {
                sqrRootLength = (int)Math.Floor(sqLength);
            }

            //Lengths[0] = 0 by default
            Lengths[1] = halvedLength;
            Lengths[2] = _Length;
            Lengths[3] = sqrRootLength;

            // create an array of 4 price list.
            PriceLists = new List<double>[4];

            //PriceLists[0] = null;
            PriceLists[1] = new List<double>();
            PriceLists[2] = new List<double>();
            PriceLists[3] = new List<double>();
        }
        public void _AddData(double data)
        {
            //prove that i called twice
            PriceLists[1].Insert(0, data);
            PriceLists[2].Insert(0, data);

            if (PriceLists[1].Count > Lengths[1])
                PriceLists[1].RemoveAt(Lengths[1]);

            if (PriceLists[2].Count > Lengths[2])
                PriceLists[2].RemoveAt(Lengths[2]);
        }
        protected override double CalcBar()
        {
            // only call this once on the last bar tick.
            if (Bars.Status == EBarState.Close)
            {
                // insufficient data
                if (PriceLists[2].Count < _Length) return -1;

                // calculate 
                double wma1 = 2.0 * WeightedAverage(PriceLists[1], Lengths[1]);
                double wma2 = WeightedAverage(PriceLists[2], Lengths[2]);

                // add the calculated data to pricelist 3
                PriceLists[3].Insert(0, wma1 - wma2);

                // check for sufficient data
                if (PriceLists[3].Count > Lengths[3])
                {
                    PriceLists[3].RemoveAt(Lengths[3]);
                    return WeightedAverage(PriceLists[3], Lengths[3]);
                }
            }

            return -1;
        }

        private double WeightedAverage(List<double> dataSet, int length)
        {
            // the recent data is more heavily weighted
            if (length < 1) return 0;

            double denominator = 1 / ((length + 1) * length * 0.5);
            double weightedSum = 0;

            // newest data is front loaded.
            for (int i = 0; i < length; i++)
                weightedSum += (length - i) * dataSet[i];

            return weightedSum * denominator;
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }


    #endregion

    #region Auto Position Sizing Class
    //==============================================================================
    // Name           : AutoPositionSizingFX
    // Description    : Calculates the max contracts for FX given an entry and exit.
    // Version        : v.1.0.0
    // Date Created   : 20 - May - 2020
    // Time Taken     : 
    // Remarks        :
    //==============================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==============================================================================

    /* ------------------------------- Version 1.0.0 -------------------------------
     */
    public sealed class AutoPositionSizingFX : FunctionSimple<int>
    {
        // class properties, use prefix so easier to identify class properties.
        public double _AccountSize { private get; set; }
        public double _Risk { private get; set; }
        public int _StepSize { private get; set; }
        public int _MinimumContracts { private get; set; }
        public int _MaximumContracts { private get; set; }

        // get properties
        public double _PipValue { get { return PipValue(); } }
        public double _OnePip { get { return GetTickValue(10); } }


        // class constructor
        public AutoPositionSizingFX(CStudyControl _master) : base(_master) { }
        public AutoPositionSizingFX(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private const int StandardFxLot = 100000;
        private string BaseSymbol;
        private double EntryPrice, StopLossPrice;
        public Dictionary<string, double> _DictOfPipValues;

        protected override void Create()
        {
            _DictOfPipValues = new Dictionary<string, double>();
        }

        protected override void StartCalc()
        {
            GetPipValues();
        }

        protected override int CalcBar()
        {
            // not needed to be called on a bar by bar basis.
            return 0;
        }
        private double PipValue()
        {
            // get pip value
            GetPipValues();

            return _DictOfPipValues[BaseSymbol];
        }
        public int Calculate(double entryPrice, double stopLossPrice)
        {
            EntryPrice = entryPrice;
            StopLossPrice = stopLossPrice;

            // Check if entry price or stop loss price is 0
            if (EntryPrice == 0 || StopLossPrice == 0) return 0;

            // Get pip values.
            GetPipValues();

            // return tradesize
            int tradeSize;

            // get fx calculation values
            double onePip = GetTickValue(10);
            double pipValue = _DictOfPipValues[BaseSymbol];

            // calculate price risk
            double priceRisk = Math.Abs(EntryPrice - StopLossPrice);

            // round price risk to 5 decimal places, for fx is good enough.
            priceRisk = Math.Round(priceRisk, 5);

            // check if risk is less than one pip.
            if (priceRisk < onePip) priceRisk = onePip;

            // calculate numerator and denominator values
            double valueRisk = (priceRisk / onePip) * pipValue;
            double dollarRisk = _AccountSize * 0.01 * _Risk;

            // obtain raw trade size
            double rawTradeSize = (dollarRisk / valueRisk) * StandardFxLot;

            // convert to min step
            rawTradeSize = _StepSize * Math.Floor(rawTradeSize / _StepSize);

            // check if less than min value
            if (rawTradeSize < _MinimumContracts) rawTradeSize = 0;
            if (rawTradeSize > _MaximumContracts) rawTradeSize = _MaximumContracts;

            tradeSize = (int)rawTradeSize;

            // reset entry and stoploss price to zero.
            EntryPrice = StopLossPrice = 0;

            return tradeSize;
        }

        #region Private methods
        private void GetPipValues()
        {
            // clear all the data at the end of the session.
            ResetDataEndOfSession();

            string chartSymbol = Bars.Info.RTSymbolName;
            ESymbolCategory category = Bars.Info.Category;

            // current chart base symbols
            string[] _baseSymbol = chartSymbol.Split('.');
            string baseSymbol = _baseSymbol[1];

            // cache base symbol
            BaseSymbol = baseSymbol;

            // list of major forex base symbols
            string[] baseSymbols = { "USD", "JPY", "CHF", "CAD", "AUD", "GBP", "NZD" };

            // check if current symbol is within list of major forex base symbols
            if (baseSymbols.Contains(baseSymbol) == false) return;

            // If we have not calculated the pip value for this base symbol then.
            if (_DictOfPipValues.ContainsKey(baseSymbol) == false)
            {
                double conversionFactor;
                switch (baseSymbol)
                {
                    default:
                        {
                            // return 0
                            conversionFactor = 0.0;
                            break;
                        }

                    case "USD":
                        {
                            conversionFactor = 1.0;
                            break;
                        }

                    case "JPY":
                        {
                            conversionFactor = 1.0 / GetSymbolLiveQuote("USD.JPY", category);
                            break;
                        }

                    case "CHF":
                        {
                            conversionFactor = 1.0 / GetSymbolLiveQuote("USD.CHF", category);
                            break;
                        }

                    case "CAD":
                        {
                            conversionFactor = 1.0 / GetSymbolLiveQuote("USD.CAD", category);
                            break;
                        }

                    case "GBP":
                        {
                            conversionFactor = GetSymbolLiveQuote("GBP.USD", category);
                            break;
                        }


                    case "AUD":
                        {
                            conversionFactor = GetSymbolLiveQuote("AUD.USD", category);
                            break;
                        }

                    case "NZD":
                        {
                            conversionFactor = GetSymbolLiveQuote("NZD.USD", category);
                            break;
                        }
                }

                // calculate the pip value for the current chart base symbol
                double pipValue = GetTickValue(10) * conversionFactor * StandardFxLot;

                // cache the results into Dictionary.
                _DictOfPipValues.Add(baseSymbol, pipValue);
            }
        }
        private void ResetDataEndOfSession()
        {
            // calculate end of session time.
            int sessionEndTime = 100 * (Bars.Sessions[0].EndTime.Hours) + Bars.Sessions[0].EndTime.Minutes;
            int currentSessionTime = 100 * (Bars.Time[0].Hour) + Bars.Time[0].Minute;

            // if current session time = end time, clear data.
            if (currentSessionTime == sessionEndTime) _DictOfPipValues.Clear();
        }
        private double GetTickValue(int ticks)
        {
            // 10 ticks make one pip.
            double tickValue = Bars.Info.MinMove / Bars.Info.PriceScale;
            return tickValue * ticks;
        }
        private double GetSymbolLiveQuote(string symbolName, ESymbolCategory category)
        {
            /*   Context on Threading
             *   The concept of foreground and background thread. 
             *   As BeginLoadData is called on a separate worker/foreground thread
             *   Foreground thread will continue to run, even though the background thread has ended.
             *   To terminate worker thread when main (background) thread has ended, you will need
             *   to set the worker thread property IsBackground = true
             *   You will need the AutoResetEvent class to wait for the results,
             *   before returning the forexData local variable. Otherwise, 
             *   the return statement will be called before the Dataload results.
             *   Resulting in forexData = 0.
             *   
             *   This is different from concurrency. This is multi-threading or
             *   parallelism programming. Concurrency, utilizes async and await to 
             *   prevent blocking or freezing of user interface. For parallelism,
             *   it is best used for single-heavy task operations, which does not involve
             *   frequent passing of data from one thread to another.
             *   
             *   The lock in this case, ReaderWriterLockSlim, is used for synchronization.
             *   In the event that, another application is using the same function. 
             *   It prevents both applications from entering the same function call at the same time.
             *   
             *   Once the results returns completed. There is where we lock.
             *   Note: Autoevent needs BOTH a wait and set as a pair, otherwise, it will be in limbo.
             *   
             *   Another note is callback: Callback is a delegate with a specific signature from MC.NET
             *   It takes in a delegate or method pointer to be executed when inside dataloader. 
             *   Call back is just a fancy name, in reality, it is just a delegate.
             *   As we are dealing with parallel programming, we need to execute our method inside of 
             *   DataLoader, otherwise, we would not know if the operation has completed, or call prematurely
             */
            ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

            double forexData = 0;

            var benchmarkInfo = SymbolStorage
                .GetDataFeeds()
                .SelectMany
                (
                    data => SymbolStorage.GetSymbols(data, category)
                                         .Select(data2 => new { dataFeed = data, symInfo = data2 })
                )
                .Where(data => data.symInfo.SymbolName == symbolName)
                .First();

            InstrumentDataRequest request = Bars.Request;
            request.Range.From = Bars.Time[2];
            request.Range.To = Bars.Time[0];
            request.Category = (ESymbolCategory)benchmarkInfo.symInfo.SymbolCategory;
            request.Symbol = benchmarkInfo.symInfo.SymbolName;
            request.DataFeed = benchmarkInfo.dataFeed;
            request.Exchange = benchmarkInfo.symInfo.SymbolExchange;
            request.RTSymbol = request.Symbol;
            request.RTDataFeed = request.DataFeed;
            request.RTExchange = request.Exchange;

            // without autoEvent, the method will return before results are returned
            AutoResetEvent autoEvent = new AutoResetEvent(false);

            // this is foreground or worker thread.
            IDataLoaderResult results = DataLoader.BeginLoadData(request, result =>
            {
                // check to see if load data is completed
                if (result.IsCompleted)
                {
                    try
                    {
                        // lock to prevent other application from accessing concurrently
                        rwLock.EnterWriteLock();
                        foreach (var quote in result.Data)
                            forexData = quote.Close;
                    }
                    finally
                    {
                        // remember to unlock
                        rwLock.ExitWriteLock();
                    }
                }

                DataLoader.EndLoadData(result);

                // return control to main thread
                autoEvent.Set();
            }
            , null);

            // wait for signal from worker thread to continue
            autoEvent.WaitOne();
            return forexData;
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }

    #endregion

    #region Pama MTF Funnel
    //==========================================================================================
    // Name           : Price Action Multitime Frame Standard Deviation
    // Description    : Returns the Std Dev on a separate subchart
    // Version        : v1.1
    // Date Created   : 04 - Jul - 2020
    // Time Taken     : 
    // Remarks        :
    // Reference      : 
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v00
     *  - Original File
     *  v10
     *  - Create Funnel Function
     *  v11
     *  - Bug fixes, refactorization
     *  - Don't create separate function, e.g. one for funnel, one for std
     *  ----------------------------------- Version History -----------------------------------
     */
    public sealed class SherNing_PamaMTF_Funnel : FunctionSimple<bool> // v11
    {
        // properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // return properties
        public VariableSeries<int> TrendState_TF1 { get; private set; }
        public VariableSeries<int> TrendState_TF2 { get; private set; }
        public VariableSeries<int> TrendState_TF3 { get; private set; }

        public VariableSeries<double> TrendHigh_TF1 { get; private set; }
        public VariableSeries<double> TrendHigh_TF2 { get; private set; }
        public VariableSeries<double> TrendHigh_TF3 { get; private set; }

        public VariableSeries<double> TrendLow_TF1 { get; private set; }
        public VariableSeries<double> TrendLow_TF2 { get; private set; }
        public VariableSeries<double> TrendLow_TF3 { get; private set; }

        public VariableSeries<int> TrendHighBarNum_TF1 { get; private set; }
        public VariableSeries<int> TrendHighBarNum_TF2 { get; private set; }
        public VariableSeries<int> TrendHighBarNum_TF3 { get; private set; }

        public VariableSeries<int> TrendLowBarNum_TF1 { get; private set; }
        public VariableSeries<int> TrendLowBarNum_TF2 { get; private set; }
        public VariableSeries<int> TrendLowBarNum_TF3 { get; private set; }

        public SherNing_PamaMTF_Funnel(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;
        private int[] BarCount, TfSelected;
        private List<VariableSeries<double>> PamaValues, HmaValues;
        private List<VariableSeries<bool>> ClosingTicks;
        private List<HullMovingAverage> HmaFns;
        private List<MultiTimeFrameClosingTick> ClosingTickFns;
        private double[] Highs, Lows, Opens, Closes, Prices;

        // v11 fields
        private const int GreenColor = 1;
        private const int RedColor = -1;
        private List<double[]> PamaValuesList;
        private List<VariableSeries<int>> TrendState, TrendHighBarNum, TrendLowBarNum;
        private List<VariableSeries<double>> TrendHigh, TrendLow;


        protected override void Create()
        {
            // list of calculated values
            PamaValues = new List<VariableSeries<double>>();
            HmaValues = new List<VariableSeries<double>>();
            ClosingTicks = new List<VariableSeries<bool>>();

            // list of custom functions
            ClosingTickFns = new List<MultiTimeFrameClosingTick>();
            HmaFns = new List<HullMovingAverage>();

            // v11 fields
            PamaValuesList = new List<double[]>();
            TrendState = new List<VariableSeries<int>>();
            TrendHigh = new List<VariableSeries<double>>();
            TrendHighBarNum = new List<VariableSeries<int>>();
            TrendLow = new List<VariableSeries<double>>();
            TrendLowBarNum = new List<VariableSeries<int>>();

            for (int i = 0; i < Timeframes; i++)
            {
                PamaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new VariableSeries<bool>(this));
                HmaFns.Add(new HullMovingAverage(this));
                HmaValues.Add(new VariableSeries<double>(this));
                ClosingTickFns.Add(new MultiTimeFrameClosingTick(this));

                // v11 fields, this in vs is to pass state of current object
                PamaValuesList.Add(new double[3]);
                TrendState.Add(new VariableSeries<int>(this));
                TrendHighBarNum.Add(new VariableSeries<int>(this));
                TrendLowBarNum.Add(new VariableSeries<int>(this));
                TrendLow.Add(new VariableSeries<double>(this));
                TrendHigh.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;

            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                HmaFns[i]._Length = _Length;
                ClosingTickFns[i]._TimeFrame = i;
            }

            // initialize list of pivots in event of recalculation
            for (int i = 0; i < Timeframes; i++)
            {
                for (int j = 0; j < 3; j++)
                    PamaValuesList[i][j] = 0;
            }

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (TfSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ClosingTicks[timeframe].Value = ClosingTickFns[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                // set trend state, 1 uptrend, -1 downtrend
                SetTrendState(timeframe);

                // set funnel state, highest high, lowest low values for trend state
                SetFunnelState(timeframe);

                if (ClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetFunnelState(int timeframe)
        {
            // current state can only be 1 or -1, and not both.

            // bottom turning point
            if (TrendState[timeframe][1] == -1 && TrendState[timeframe][0] == 1)
            {
                // reset highest high value
                TrendHigh[timeframe].Value = double.MinValue;
            }

            // peak turning point
            if (TrendState[timeframe][1] == 1 && TrendState[timeframe][0] == -1)
            {
                // reset lowest low value
                TrendLow[timeframe].Value = double.MaxValue;
            }

            // trending up
            if (TrendState[timeframe][0] == 1)
            {
                // track highest high value and bar number
                if (Bars.HighValue > TrendHigh[timeframe][0])
                {
                    TrendHigh[timeframe].Value = Bars.HighValue;
                    TrendHighBarNum[timeframe].Value = Bars.CurrentBar;
                }

                // notes: before assigning to VS[0], it will hold VS[1] value
            }

            // trending down
            if (TrendState[timeframe][0] == -1)
            {
                // track lowest low value and bar number
                if (Bars.LowValue < TrendLow[timeframe][0])
                {
                    TrendLow[timeframe].Value = Bars.LowValue;
                    TrendLowBarNum[timeframe].Value = Bars.CurrentBar;
                }
            }
        }
        private void SetTrendState(int timeframe)
        {
            // if time frame closing tick is true
            if (ClosingTicks[timeframe][0])
            {
                // shift cells forward
                for (int i = 2; i >= 1; i--)
                    PamaValuesList[timeframe][i] = PamaValuesList[timeframe][i - 1];

                // add newest data to index 0
                PamaValuesList[timeframe][0] = PamaValues[timeframe].Value;

                // peak [0] < [1] > [2]
                if (PamaValuesList[timeframe][0] < PamaValuesList[timeframe][1] &&
                    PamaValuesList[timeframe][1] > PamaValuesList[timeframe][2])
                {
                    // trend line is red color
                    TrendState[timeframe].Value = RedColor;
                }
                else // bottom [0] > [1] < [2]
                if (PamaValuesList[timeframe][0] > PamaValuesList[timeframe][1] &&
                    PamaValuesList[timeframe][1] < PamaValuesList[timeframe][2])
                {
                    // trend line is green color
                    TrendState[timeframe].Value = GreenColor;
                }

                // variable series, holds prev end of bar state
            }
        }
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                HmaFns[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                HmaValues[timeframe].Value = HmaFns[timeframe].Value;

                // original
                if (Prices[timeframe] - PamaValues[timeframe][1] > 0)
                {
                    PamaValues[timeframe].Value
                    = Alpha * (HmaValues[timeframe].Value
                    + (-Gain) * (HmaValues[timeframe].Value - PamaValues[timeframe][1]))
                    + (1.0 - Alpha) * PamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - PamaValues[timeframe][1] < 0)
                {
                    PamaValues[timeframe].Value
                   = Alpha * (HmaValues[timeframe].Value
                   + Gain * (HmaValues[timeframe].Value - PamaValues[timeframe][1]))
                   + (1.0 - Alpha) * PamaValues[timeframe][1];
                }
                else
                {
                    PamaValues[timeframe].Value
                   = Alpha * (HmaValues[timeframe].Value
                   + 0.0 * (HmaValues[timeframe].Value - PamaValues[timeframe][1]))
                   + (1.0 - Alpha) * PamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            if (ClosingTicks[timeframe][0] == true)
                Prices[timeframe] = Closes[timeframe];
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            TfSelected = new int[Timeframes];

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                TfSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    TfSelected[0] = 1;
                    TrendState_TF1 = TrendState[0];
                    TrendHigh_TF1 = TrendHigh[0];
                    TrendLow_TF1 = TrendLow[0];
                    TrendHighBarNum_TF1 = TrendHighBarNum[0];
                    TrendLowBarNum_TF1 = TrendLowBarNum[0];

                    TfSelected[5] = 1;
                    TrendState_TF2 = TrendState[5];
                    TrendHigh_TF2 = TrendHigh[5];
                    TrendLow_TF2 = TrendLow[5];
                    TrendHighBarNum_TF2 = TrendHighBarNum[5];
                    TrendLowBarNum_TF2 = TrendLowBarNum[5];

                    TfSelected[7] = 1;
                    TrendState_TF3 = TrendState[7];
                    TrendHigh_TF3 = TrendHigh[7];
                    TrendLow_TF3 = TrendLow[7];
                    TrendHighBarNum_TF3 = TrendHighBarNum[7];
                    TrendLowBarNum_TF3 = TrendLowBarNum[7];
                    break;

                case 15:
                    TfSelected[2] = 1;
                    TrendState_TF1 = TrendState[2];
                    TrendHigh_TF1 = TrendHigh[2];
                    TrendLow_TF1 = TrendLow[2];
                    TrendHighBarNum_TF1 = TrendHighBarNum[2];
                    TrendLowBarNum_TF1 = TrendLowBarNum[2];

                    TfSelected[5] = 1;
                    TrendState_TF2 = TrendState[5];
                    TrendHigh_TF2 = TrendHigh[5];
                    TrendLow_TF2 = TrendLow[5];
                    TrendHighBarNum_TF2 = TrendHighBarNum[5];
                    TrendLowBarNum_TF2 = TrendLowBarNum[5];

                    TfSelected[7] = 1;
                    TrendState_TF3 = TrendState[7];
                    TrendHigh_TF3 = TrendHigh[7];
                    TrendLow_TF3 = TrendLow[7];
                    TrendHighBarNum_TF3 = TrendHighBarNum[7];
                    TrendLowBarNum_TF3 = TrendLowBarNum[7];
                    break;

                case 60:
                    TfSelected[5] = 1;
                    TrendState_TF1 = TrendState[5];
                    TrendHigh_TF1 = TrendHigh[5];
                    TrendLow_TF1 = TrendLow[5];
                    TrendHighBarNum_TF1 = TrendHighBarNum[5];
                    TrendLowBarNum_TF1 = TrendLowBarNum[5];

                    TfSelected[7] = 1;
                    TrendState_TF2 = TrendState[7];
                    TrendHigh_TF2 = TrendHigh[7];
                    TrendLow_TF2 = TrendLow[7];
                    TrendHighBarNum_TF2 = TrendHighBarNum[7];
                    TrendLowBarNum_TF2 = TrendLowBarNum[7];

                    TfSelected[8] = 1;
                    TrendState_TF3 = TrendState[8];
                    TrendHigh_TF3 = TrendHigh[8];
                    TrendLow_TF3 = TrendLow[8];
                    TrendHighBarNum_TF3 = TrendHighBarNum[8];
                    TrendLowBarNum_TF3 = TrendLowBarNum[8];
                    break;
            }
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
    public sealed class SherNing_PamaMTF_Funnelv10 : FunctionSimple<bool> // v10
    {
        // properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // return properties 
        public VariableSeries<int> PivotChng_TF1 { get; private set; }
        public VariableSeries<int> PivotChng_TF2 { get; private set; }
        public VariableSeries<int> PivotChng_TF3 { get; private set; }

        // track pivot high, reference bars back
        public VariableSeries<double> PivotHigh_TF1 { get; private set; }
        public VariableSeries<double> PivotHigh_TF2 { get; private set; }
        public VariableSeries<double> PivotHigh_TF3 { get; private set; }

        public VariableSeries<int> PivotHighBarAgo_TF1 { get; private set; }
        public VariableSeries<int> PivotHighBarAgo_TF2 { get; private set; }
        public VariableSeries<int> PivotHighBarAgo_TF3 { get; private set; }

        public VariableSeries<double> PivotLow_TF1 { get; private set; }
        public VariableSeries<double> PivotLow_TF2 { get; private set; }
        public VariableSeries<double> PivotLow_TF3 { get; private set; }

        public VariableSeries<int> PivotLowBarAgo_TF1 { get; private set; }
        public VariableSeries<int> PivotLowBarAgo_TF2 { get; private set; }
        public VariableSeries<int> PivotLowBarAgo_TF3 { get; private set; }

        public SherNing_PamaMTF_Funnelv10(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private const int GreenColor = 1;
        private const int RedColor = -1;

        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<VariableSeries<double>> ListOfPivotHighs, ListOfPivotLows;
        private List<VariableSeries<int>> ListOfPivotValues, ListOfBarNumber;
        private List<VariableSeries<int>> ListOfHighsBarsAgo, ListOfLowsBarsAgo;
        private List<VariableSeries<int>> ListOfPivotChg;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        // list of arrays to track turning points
        private List<double[]> ListOfPivots;

        protected override void Create()
        {
            // list of calculated values
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfPivotValues = new List<VariableSeries<int>>();
            ListOfPivotChg = new List<VariableSeries<int>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfHmas = new List<HullMovingAverage>();

            // list of arrays
            ListOfPivots = new List<double[]>();
            ListOfPivotHighs = new List<VariableSeries<double>>();
            ListOfPivotLows = new List<VariableSeries<double>>();
            ListOfBarNumber = new List<VariableSeries<int>>();
            ListOfHighsBarsAgo = new List<VariableSeries<int>>();
            ListOfLowsBarsAgo = new List<VariableSeries<int>>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));

                // only 3 points needed to determine pivot
                ListOfPivots.Add(new double[3]);
                ListOfPivotValues.Add(new VariableSeries<int>(this));
                ListOfPivotChg.Add(new VariableSeries<int>(this));
                ListOfPivotHighs.Add(new VariableSeries<double>(this));
                ListOfPivotLows.Add(new VariableSeries<double>(this));
                ListOfBarNumber.Add(new VariableSeries<int>(this));
                ListOfHighsBarsAgo.Add(new VariableSeries<int>(this));
                ListOfLowsBarsAgo.Add(new VariableSeries<int>(this));
            }
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;

            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                ListOfHmas[i]._Length = _Length;
                ClosingTicks[i]._TimeFrame = i;
            }

            // initialize list of pivots in event of recalculation
            for (int i = 0; i < Timeframes; i++)
            {
                for (int j = 0; j < 3; j++)
                    ListOfPivots[i][j] = 0;
            }

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = ClosingTicks[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                // set pivot direction
                SetPamaDirection(timeframe);

                // track actual pivot
                TrackPivotLocation(timeframe);

                // track bar ago for pivot highs and lows
                TrackBarNumber(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void TrackBarNumber(int timeframe)
        {
            // bottom turning point, red to green
            if (ListOfPivotChg[timeframe][0] == 1)
            {
                // list of bar number [0], in case it was updated current bar
                ListOfLowsBarsAgo[timeframe].Value =
                    Bars.CurrentBar - ListOfBarNumber[timeframe][0];
            }
            else // peak turning point, green to red
            if (ListOfPivotChg[timeframe][0] == -1)
            {
                ListOfHighsBarsAgo[timeframe].Value =
                    Bars.CurrentBar - ListOfBarNumber[timeframe][0];
            }
        }
        private void TrackPivotLocation(int timeframe)
        {
            // if green track bar's highest high
            if (ListOfPivotValues[timeframe].Value == 1)
            {
                // reset to max value
                ListOfPivotLows[timeframe].Value = double.MaxValue;

                // requires if statement to track bar number
                if (Bars.HighValue > ListOfPivotHighs[timeframe][1])
                {
                    // update current high
                    ListOfPivotHighs[timeframe].Value = Bars.HighValue;

                    // track bar number
                    ListOfBarNumber[timeframe].Value = Bars.CurrentBar;
                }

                // variable series will follow prev cache value
            }
            // if red track bar's lowest low
            else if (ListOfPivotValues[timeframe].Value == -1)
            {
                // reset to min value
                ListOfPivotHighs[timeframe].Value = double.MinValue;

                // compare current with prev low
                if (Bars.LowValue < ListOfPivotLows[timeframe][1])
                {
                    // update low value
                    ListOfPivotLows[timeframe].Value = Bars.LowValue;

                    // track bar number
                    ListOfBarNumber[timeframe].Value = Bars.CurrentBar;
                }

                // bar number is forward moving, no need to reset
            }
        }
        private void SetPamaDirection(int timeframe)
        {
            // if time frame closing tick is true
            if (ListOfClosingTicks[timeframe][0])
            {
                // shift cells forward
                for (int i = 2; i >= 1; i--)
                    ListOfPivots[timeframe][i] = ListOfPivots[timeframe][i - 1];

                // add newest data to index 0
                ListOfPivots[timeframe][0] = ListOfPamaValues[timeframe].Value;

                // peak [0] < [1] > [2]
                if (ListOfPivots[timeframe][0] < ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] > ListOfPivots[timeframe][2])
                {
                    ListOfPivotValues[timeframe].Value = RedColor;
                    ListOfPivotChg[timeframe].Value = RedColor;
                }
                else // bottom [0] > [1] < [2]
                if (ListOfPivots[timeframe][0] > ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] < ListOfPivots[timeframe][2])
                {
                    ListOfPivotValues[timeframe].Value = GreenColor;
                    ListOfPivotChg[timeframe].Value = GreenColor;
                }

                // *variable series will use prev value if there is no update.
            }
            else
            {
                // there is no pivot change 
                ListOfPivotChg[timeframe].Value = 0;
            }
        }
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            if (ListOfClosingTicks[timeframe][0] == true)
                Prices[timeframe] = Closes[timeframe];
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    IsTimeFrameSelected[0] = 1;
                    PivotChng_TF1 = ListOfPivotChg[0];
                    PivotHigh_TF1 = ListOfPivotHighs[0];
                    PivotLow_TF1 = ListOfPivotLows[0];
                    PivotHighBarAgo_TF1 = ListOfHighsBarsAgo[0];
                    PivotLowBarAgo_TF1 = ListOfLowsBarsAgo[0];

                    IsTimeFrameSelected[5] = 1;
                    PivotChng_TF2 = ListOfPivotChg[5];
                    PivotHigh_TF2 = ListOfPivotHighs[5];
                    PivotLow_TF2 = ListOfPivotLows[5];
                    PivotHighBarAgo_TF2 = ListOfHighsBarsAgo[5];
                    PivotLowBarAgo_TF2 = ListOfLowsBarsAgo[5];

                    IsTimeFrameSelected[7] = 1;
                    PivotChng_TF3 = ListOfPivotChg[7];
                    PivotHigh_TF3 = ListOfPivotHighs[7];
                    PivotLow_TF3 = ListOfPivotLows[7];
                    PivotHighBarAgo_TF3 = ListOfHighsBarsAgo[7];
                    PivotLowBarAgo_TF3 = ListOfLowsBarsAgo[7];

                    break;

                case 15:
                    IsTimeFrameSelected[2] = 1;
                    PivotChng_TF1 = ListOfPivotChg[2];
                    PivotHigh_TF1 = ListOfPivotHighs[2];
                    PivotLow_TF1 = ListOfPivotLows[2];
                    PivotHighBarAgo_TF1 = ListOfHighsBarsAgo[2];
                    PivotLowBarAgo_TF1 = ListOfLowsBarsAgo[2];

                    IsTimeFrameSelected[5] = 1;
                    PivotChng_TF2 = ListOfPivotChg[5];
                    PivotHigh_TF2 = ListOfPivotHighs[5];
                    PivotLow_TF2 = ListOfPivotLows[5];
                    PivotHighBarAgo_TF2 = ListOfHighsBarsAgo[5];
                    PivotLowBarAgo_TF2 = ListOfLowsBarsAgo[5];

                    IsTimeFrameSelected[7] = 1;
                    PivotChng_TF3 = ListOfPivotChg[7];
                    PivotHigh_TF3 = ListOfPivotHighs[7];
                    PivotLow_TF3 = ListOfPivotLows[7];
                    PivotHighBarAgo_TF3 = ListOfHighsBarsAgo[7];
                    PivotLowBarAgo_TF3 = ListOfLowsBarsAgo[7];

                    break;

                case 60:
                    IsTimeFrameSelected[5] = 1;
                    PivotChng_TF1 = ListOfPivotChg[5];
                    PivotHigh_TF1 = ListOfPivotHighs[5];
                    PivotLow_TF1 = ListOfPivotLows[5];
                    PivotHighBarAgo_TF1 = ListOfHighsBarsAgo[5];
                    PivotLowBarAgo_TF1 = ListOfLowsBarsAgo[5];

                    IsTimeFrameSelected[7] = 1;
                    PivotChng_TF2 = ListOfPivotChg[7];
                    PivotHigh_TF2 = ListOfPivotHighs[7];
                    PivotLow_TF2 = ListOfPivotLows[7];
                    PivotHighBarAgo_TF2 = ListOfHighsBarsAgo[7];
                    PivotLowBarAgo_TF2 = ListOfLowsBarsAgo[7];

                    IsTimeFrameSelected[8] = 1;
                    PivotChng_TF3 = ListOfPivotChg[8];
                    PivotHigh_TF3 = ListOfPivotHighs[8];
                    PivotLow_TF3 = ListOfPivotLows[8];
                    PivotHighBarAgo_TF3 = ListOfHighsBarsAgo[8];
                    PivotLowBarAgo_TF3 = ListOfLowsBarsAgo[8];

                    break;
            }
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
    public sealed class SherNing_PamaMTF_Funnelv00 : FunctionSimple<bool> // v00
    {
        // properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        public SherNing_PamaMTF_Funnelv00(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private const int GreenColor = 1;
        private const int RedColor = -1;

        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        // list of arrays to track turning points
        private List<double[]> ListOfPivots;

        protected override void Create()
        {
            // list of calculated values
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfHmas = new List<HullMovingAverage>();

            // list of arrays
            ListOfPivots = new List<double[]>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));

                // only 3 points needed to determine pivot
                ListOfPivots.Add(new double[3]);
            }
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;

            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                ListOfHmas[i]._Length = _Length;
                ClosingTicks[i]._TimeFrame = i;
            }

            // initialize list of pivots in event of recalculation
            for (int i = 0; i < Timeframes; i++)
            {
                for (int j = 0; j < 3; j++)
                    ListOfPivots[i][j] = 0;
            }

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = ClosingTicks[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                // set pivot direction
                SetPamaDirection(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaDirection(int timeframe)
        {
            // if time frame closing tick is true
            if (ListOfClosingTicks[timeframe][0])
            {
                // shift cells forward
                for (int i = 2; i >= 1; i--)
                    ListOfPivots[timeframe][i] = ListOfPivots[timeframe][i - 1];

                // add newest data to index 0
                ListOfPivots[timeframe][0] = ListOfPamaValues[timeframe].Value;

                // peak [0] < [1] > [2]
                if (ListOfPivots[timeframe][0] < ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] > ListOfPivots[timeframe][2])
                {
                }
                else // bottom [0] > [1] < [2]
                if (ListOfPivots[timeframe][0] > ListOfPivots[timeframe][1] &&
                    ListOfPivots[timeframe][1] < ListOfPivots[timeframe][2])
                {
                }

                // *variable series will use prev value if there is no update.
            }
            else
            {
                // there is no pivot change 
            }
        }
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            if (ListOfClosingTicks[timeframe][0] == true)
                Prices[timeframe] = Closes[timeframe];
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    IsTimeFrameSelected[0] = 1;


                    IsTimeFrameSelected[5] = 1;


                    IsTimeFrameSelected[7] = 1;


                    break;

                case 15:
                    IsTimeFrameSelected[2] = 1;

                    IsTimeFrameSelected[5] = 1;

                    IsTimeFrameSelected[7] = 1;

                    break;

                case 60:
                    IsTimeFrameSelected[5] = 1;


                    IsTimeFrameSelected[7] = 1;

                    IsTimeFrameSelected[8] = 1;

                    break;
            }
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
    #endregion

    #region Price Action Multitime Frame Standard Deviation
    //==========================================================================================
    // Name           : Price Action Multitime Frame Standard Deviation
    // Description    : Returns the Std Dev for each timeframe
    // Version        : v1.0
    // Date Created   : 04 - Jul - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *  v10
     *  - Price Charts 5 min, 15 min, 60 min only
     *  
     *  ----------------------------------- Version History -----------------------------------
     */

    public sealed class PamaMTF_StdDev : FunctionSimple<bool> // v10
    {
        // properties. 
        public int _Length { get; set; }
        public int _StdDevLength { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // get accessors
        public VariableSeries<double> StdDev1 { get; private set; }
        public VariableSeries<double> StdDev2 { get; private set; }
        public VariableSeries<double> StdDev3 { get; private set; }

        public PamaMTF_StdDev(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues, ListOfStdDevValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private List<StandardDeviationCalculatorManual> ListOfStdDevs;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            // list of calculated values
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfStdDevValues = new List<VariableSeries<double>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfStdDevs = new List<StandardDeviationCalculatorManual>();
            ListOfHmas = new List<HullMovingAverage>();

            // std dev return properties
            StdDev1 = new VariableSeries<double>(this);
            StdDev2 = new VariableSeries<double>(this);
            StdDev3 = new VariableSeries<double>(this);

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));
                ListOfStdDevs.Add(new StandardDeviationCalculatorManual(this));
                ListOfStdDevValues.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;
            if (_StdDevLength < 1) _StdDevLength = 1;

            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                ListOfHmas[i]._Length = _Length;
                ClosingTicks[i]._TimeFrame = i;
                ListOfStdDevs[i]._Length = _StdDevLength;
                ListOfStdDevs[i]._PopulationStdDev = true;
            }

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = ClosingTicks[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                // calculate std dev values
                SetStdDevValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetStdDevValues(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass pama data 
                ListOfStdDevs[timeframe].Add(ListOfPamaValues[timeframe].Value);

                // store in list
                ListOfStdDevValues[timeframe].Value = ListOfStdDevs[timeframe].Calculate();
            }
        }
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe] = Closes[timeframe];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe]
                            = Highs[timeframe] - Lows[timeframe];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe]
                            = (Highs[timeframe]
                            + Lows[timeframe]
                            + Opens[timeframe]
                            + Closes[timeframe]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe]
                            = (Opens[timeframe]
                            + Closes[timeframe]) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    IsTimeFrameSelected[0] = 1;
                    StdDev1 = ListOfStdDevValues[0];

                    IsTimeFrameSelected[5] = 1;
                    StdDev2 = ListOfStdDevValues[5];

                    IsTimeFrameSelected[7] = 1;
                    StdDev3 = ListOfStdDevValues[7];
                    break;

                case 15:
                    IsTimeFrameSelected[2] = 1;
                    StdDev1 = ListOfStdDevValues[2];

                    IsTimeFrameSelected[5] = 1;
                    StdDev2 = ListOfStdDevValues[5];

                    IsTimeFrameSelected[7] = 1;
                    StdDev3 = ListOfStdDevValues[7];
                    break;

                case 60:
                    IsTimeFrameSelected[5] = 1;
                    StdDev1 = ListOfStdDevValues[5];

                    IsTimeFrameSelected[7] = 1;
                    StdDev2 = ListOfStdDevValues[7];

                    IsTimeFrameSelected[8] = 1;
                    StdDev3 = ListOfStdDevValues[8];
                    break;
            }
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }

    #endregion

    #region Price Action Moving Average MTF
    //==============================================================================
    // Name           : PriceActionMovingAverageMTF
    // Description    : Calculates the HMA for a given length
    // Version        : v.1.0.0
    // Date Created   : 20 - May - 2020
    // Time Taken     : 
    // Remarks        :
    //==============================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==============================================================================

    /* ------------------------------- Version History -------------------------------
     * Issue of delaying results.
     * Update default settings based on chart resolution size.
     * Remember Open-close principle when designing classes
     * 
     * v12 does not work. See notes on variable series.
     * v13 adds the calculate all timeframe feature.
     * v14 bug fix
     * v15 Use new MTF closing tick function
     * v20 add std dev, add moving average pivots
     */
    public sealed class PriceActionMovingAverageMTF : FunctionSimple<bool> // v20
    {
        // class properties. 
        public int _Length { get; set; }
        public int _StdDevLength { get; set; }
        public int _PivotLength { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // display on / off
        public int Calc_5 { get; set; }
        public int Calc_10 { get; set; }
        public int Calc_15 { get; set; }
        public int Calc_20 { get; set; }
        public int Calc_30 { get; set; }
        public int Calc_60 { get; set; }
        public int Calc_120 { get; set; }
        public int Calc_240 { get; set; }
        public int Calc_Day { get; set; }
        public int Calc_Week { get; set; }
        public int Calc_Mth { get; set; }

        // Return all 11 Variableseries<double> MTF values.
        public ISeries<double> Pama_5 { get { return ListOfPamaValues[0]; } }
        public ISeries<double> Pama_10 { get { return ListOfPamaValues[1]; } }
        public ISeries<double> Pama_15 { get { return ListOfPamaValues[2]; } }
        public ISeries<double> Pama_20 { get { return ListOfPamaValues[3]; } }
        public ISeries<double> Pama_30 { get { return ListOfPamaValues[4]; } }
        public ISeries<double> Pama_60 { get { return ListOfPamaValues[5]; } }
        public ISeries<double> Pama_120 { get { return ListOfPamaValues[6]; } }
        public ISeries<double> Pama_240 { get { return ListOfPamaValues[7]; } }
        public ISeries<double> Pama_Day { get { return ListOfPamaValues[8]; } }
        public ISeries<double> Pama_Week { get { return ListOfPamaValues[9]; } }
        public ISeries<double> Pama_Month { get { return ListOfPamaValues[10]; } }

        // return std dev
        public List<VariableSeries<double>> Pama_StdDev  { get { return ListOfStdDevsValues; } }

        // return pama pivots
        public List<VariableSeries<double>> Pama_Pivots { get { return ListOfMaPivotsValues; } }

        public PriceActionMovingAverageMTF(CStudyControl _master) : base(_master)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private List<StandardDeviationCalculatorManual> ListOfStdDevs;
        private List<MovingAveragePivotsManual> ListOfMaPivots;
        private List<VariableSeries<double>> ListOfStdDevsValues, ListOfMaPivotsValues;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfMaPivotsValues = new List<VariableSeries<double>>();
            ListOfStdDevsValues = new List<VariableSeries<double>>();

            // list of custom functions
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            ListOfStdDevs = new List<StandardDeviationCalculatorManual>();
            ListOfMaPivots = new List<MovingAveragePivotsManual>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));
                ListOfStdDevs.Add(new StandardDeviationCalculatorManual(this));
                ListOfMaPivots.Add(new MovingAveragePivotsManual(this));
            }

            // Create one extra plot
            // for each MA pivot there is an upper bound and lower bound value
            for (int i = 0; i <= 2 * Timeframes; i++)
                ListOfMaPivotsValues.Add(new VariableSeries<double>(this));

            // each std dev has two upper bound and two lower bound value
            for (int i = 0; i <= 4 * Timeframes; i++)
                ListOfStdDevsValues.Add(new VariableSeries<double>(this));
        }
        protected override void StartCalc()
        {
            // safety check
            if (_Length < 1) _Length = 1;
            if (_StdDevLength < 1) _StdDevLength = 1;
            if (_PivotLength < 3) _PivotLength = 3;

            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                ListOfHmas[i]._Length = _Length;
                ClosingTicks[i]._TimeFrame = i;
                ListOfStdDevs[i]._Length = _StdDevLength;
                ListOfStdDevs[i]._PopulationStdDev = true;
                ListOfMaPivots[i]._Length = _PivotLength;
            }

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = ClosingTicks[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                // calculate std dev values
                SetStdDevValues(timeframe);

                // set MA pivot values
                SetMovingAveragePivots(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetMovingAveragePivots(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // cache pama value
                double pamaValue = ListOfPamaValues[timeframe].Value;

                // add data
                ListOfMaPivots[timeframe].Add(pamaValue);
                ListOfMaPivots[timeframe].AddHigh(Highs[timeframe]);
                ListOfMaPivots[timeframe].AddLow(Lows[timeframe]);

                // cache data
                double pivotHigh = ListOfMaPivots[timeframe].PivotHighs;
                double pivotLow = ListOfMaPivots[timeframe].PivotLows;

                // assign data
                ListOfMaPivotsValues[(timeframe * 2) + 1].Value = pivotHigh;
                ListOfMaPivotsValues[(timeframe * 2) + 2].Value = pivotLow;
            }
        }
        private void SetStdDevValues(int timeframe)
        {
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass pama data 
                ListOfStdDevs[timeframe].Add(ListOfPamaValues[timeframe].Value);

                // cache 
                double stdDev = ListOfStdDevs[timeframe].Calculate();
                double pamaValue = ListOfPamaValues[timeframe].Value;

                // 1st std dev upper value
                ListOfStdDevsValues[(timeframe * 4) + 1].Value = pamaValue + (1 * stdDev);

                // 1st std dev lower value
                ListOfStdDevsValues[(timeframe * 4) + 2].Value = pamaValue - (1 * stdDev);

                // 2nd std dev upper value
                ListOfStdDevsValues[(timeframe * 4) + 3].Value = pamaValue + (2 * stdDev);

                // 2nd std dev lower value
                ListOfStdDevsValues[(timeframe * 4) + 4].Value = pamaValue - (2 * stdDev);
            }
        }
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe] = Closes[timeframe];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe]
                            = Highs[timeframe] - Lows[timeframe];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe]
                            = (Highs[timeframe]
                            + Lows[timeframe]
                            + Opens[timeframe]
                            + Closes[timeframe]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe]
                            = (Opens[timeframe]
                            + Closes[timeframe]) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // get user input first.
            IsTimeFrameSelected[0] = Calc_5;
            IsTimeFrameSelected[1] = Calc_10;
            IsTimeFrameSelected[2] = Calc_15;
            IsTimeFrameSelected[3] = Calc_20;
            IsTimeFrameSelected[4] = Calc_30;
            IsTimeFrameSelected[5] = Calc_60;
            IsTimeFrameSelected[6] = Calc_120;
            IsTimeFrameSelected[7] = Calc_240;
            IsTimeFrameSelected[8] = Calc_Day;
            IsTimeFrameSelected[9] = Calc_Week;
            IsTimeFrameSelected[10] = Calc_Mth;

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // one day
            if (Bars.Info.Resolution.Type == EResolution.Day && chartTF == 1)
            {
                // disable all calculations from 0 - 7
                for (int i = 0; i < 8; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // one week 
            else if (Bars.Info.Resolution.Type == EResolution.Week && chartTF == 1)
            {
                // disable all calculations from 0 - 8
                for (int i = 0; i < 9; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // one month
            else if (Bars.Info.Resolution.Type == EResolution.Month && chartTF == 1)
            {
                // disable all calculations from 0 - 9
                for (int i = 0; i < 10; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // min chart
            else if (Bars.Info.Resolution.Type == EResolution.Minute)
            {
                // disable calculations if below a certain timeframe.
                switch (chartTF)
                {
                    case 10:
                        // disable 5 min
                        IsTimeFrameSelected[0] = -1;
                        break;

                    case 15:
                        // disable 5, 10
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        break;

                    case 20:
                        // disable 5, 10, 15
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        break;

                    case 30:
                        // disable 5, 10, 15, 20
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        break;

                    case 60:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        break;

                    case 120:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        IsTimeFrameSelected[5] = -1;
                        break;

                    case 240:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        IsTimeFrameSelected[5] = -1;
                        IsTimeFrameSelected[6] = -1;
                        break;
                }

                return;
            }


            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class PriceActionMovingAverageMTFv15 : FunctionSimple<bool> // v15
    {
        // class properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // display on / off
        public int Calc_5 { get; set; }
        public int Calc_10 { get; set; }
        public int Calc_15 { get; set; }
        public int Calc_20 { get; set; }
        public int Calc_30 { get; set; }
        public int Calc_60 { get; set; }
        public int Calc_120 { get; set; }
        public int Calc_240 { get; set; }
        public int Calc_Day { get; set; }
        public int Calc_Week { get; set; }
        public int Calc_Mth { get; set; }

        // Return all 11 Variableseries<double> MTF values.
        public ISeries<double> Pama_5 { get { return ListOfPamaValues[0]; } }
        public ISeries<double> Pama_10 { get { return ListOfPamaValues[1]; } }
        public ISeries<double> Pama_15 { get { return ListOfPamaValues[2]; } }
        public ISeries<double> Pama_20 { get { return ListOfPamaValues[3]; } }
        public ISeries<double> Pama_30 { get { return ListOfPamaValues[4]; } }
        public ISeries<double> Pama_60 { get { return ListOfPamaValues[5]; } }
        public ISeries<double> Pama_120 { get { return ListOfPamaValues[6]; } }
        public ISeries<double> Pama_240 { get { return ListOfPamaValues[7]; } }
        public ISeries<double> Pama_Day { get { return ListOfPamaValues[8]; } }
        public ISeries<double> Pama_Week { get { return ListOfPamaValues[9]; } }
        public ISeries<double> Pama_Month { get { return ListOfPamaValues[10]; } }

        public PriceActionMovingAverageMTFv15 (CStudyControl _master) : base(_master) 
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //
        }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<MultiTimeFrameClosingTick> ClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ClosingTicks = new List<MultiTimeFrameClosingTick>();
            

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ClosingTicks.Add(new MultiTimeFrameClosingTick(this));
            }
        }
        protected override void StartCalc()
        {
            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
            {
                ListOfHmas[i]._Length = _Length;
                ClosingTicks[i]._TimeFrame = i;
            }

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = ClosingTicks[timeframe].Value;

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe] = Closes[timeframe];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe]
                            = Highs[timeframe] - Lows[timeframe];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe]
                            = (Highs[timeframe]
                            + Lows[timeframe]
                            + Opens[timeframe]
                            + Closes[timeframe]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe]
                            = (Opens[timeframe]
                            + Closes[timeframe]) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // get user input first.
            IsTimeFrameSelected[0] = Calc_5;
            IsTimeFrameSelected[1] = Calc_10;
            IsTimeFrameSelected[2] = Calc_15;
            IsTimeFrameSelected[3] = Calc_20;
            IsTimeFrameSelected[4] = Calc_30;
            IsTimeFrameSelected[5] = Calc_60;
            IsTimeFrameSelected[6] = Calc_120;
            IsTimeFrameSelected[7] = Calc_240;
            IsTimeFrameSelected[8] = Calc_Day;
            IsTimeFrameSelected[9] = Calc_Week;
            IsTimeFrameSelected[10] = Calc_Mth;

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // one day
            if (Bars.Info.Resolution.Type == EResolution.Day && chartTF == 1)
            {
                // disable all calculations from 0 - 7
                for (int i = 0; i < 8; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // one week 
            else if (Bars.Info.Resolution.Type == EResolution.Week && chartTF == 1)
            {
                // disable all calculations from 0 - 8
                for (int i = 0; i < 9; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // one month
            else if (Bars.Info.Resolution.Type == EResolution.Month && chartTF == 1)
            {
                // disable all calculations from 0 - 9
                for (int i = 0; i < 10; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // min chart
            else if (Bars.Info.Resolution.Type == EResolution.Minute)
            {
                // disable calculations if below a certain timeframe.
                switch (chartTF)
                {
                    case 10:
                        // disable 5 min
                        IsTimeFrameSelected[0] = -1;
                        break;

                    case 15:
                        // disable 5, 10
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        break;

                    case 20:
                        // disable 5, 10, 15
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        break;

                    case 30:
                        // disable 5, 10, 15, 20
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        break;

                    case 60:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        break;

                    case 120:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        IsTimeFrameSelected[5] = -1;
                        break;

                    case 240:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        IsTimeFrameSelected[5] = -1;
                        IsTimeFrameSelected[6] = -1;
                        break;
                }

                return;
            }


            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class PriceActionMovingAverageMTF_v14 : FunctionSeries<bool> // v14
    {
        // Version 2.0 Update property feature.

        // class properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }
        public int _Timeframes { get { return Timeframes; } }

        // display on / off
        public int Calc_5 { get; set; }
        public int Calc_10 { get; set; }
        public int Calc_15 { get; set; }
        public int Calc_20 { get; set; }
        public int Calc_30 { get; set; }
        public int Calc_60 { get; set; }
        public int Calc_120 { get; set; }
        public int Calc_240 { get; set; }
        public int Calc_Day { get; set; }
        public int Calc_Week { get; set; }
        public int Calc_Mth { get; set; }

        // Return all 11 Variableseries<double> MTF values.
        public ISeries<double> Pama_5 { get { return ListOfPamaValues[0]; } }
        public ISeries<double> Pama_10 { get { return ListOfPamaValues[1]; } }
        public ISeries<double> Pama_15 { get { return ListOfPamaValues[2]; } }
        public ISeries<double> Pama_20 { get { return ListOfPamaValues[3]; } }
        public ISeries<double> Pama_30 { get { return ListOfPamaValues[4]; } }
        public ISeries<double> Pama_60 { get { return ListOfPamaValues[5]; } }
        public ISeries<double> Pama_120 { get { return ListOfPamaValues[6]; } }
        public ISeries<double> Pama_240 { get { return ListOfPamaValues[7]; } }
        public ISeries<double> Pama_Day { get { return ListOfPamaValues[8]; } }
        public ISeries<double> Pama_Week { get { return ListOfPamaValues[9]; } }
        public ISeries<double> Pama_Month { get { return ListOfPamaValues[10]; } }

        public PriceActionMovingAverageMTF_v14 (CStudyControl _master) : base(_master) { }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage>();
            ListOfHmaValues = new List<VariableSeries<double>>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
                ListOfHmas[i]._Length = _Length;

            // disable timeframes according to chart selected.
            SetTFsForCalculationBasedOnInterval();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = IsTimeFrameOnCloseTick(timeframe);

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe] = Closes[timeframe];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe]
                            = Highs[timeframe] - Lows[timeframe];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe]
                            = (Highs[timeframe]
                            + Lows[timeframe]
                            + Opens[timeframe]
                            + Closes[timeframe]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe]
                            = (Opens[timeframe]
                            + Closes[timeframe]) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private bool IsTimeFrameOnCloseTick(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            bool isCloseTick;
            int currTimeStartMin = GetCurrentTimeFromSessionOpen();
            int currTimeStartReg = GetCurrentTime();
            int sessEndTimeReg = GetSessionEndTime();

            switch (timeframe)
            {
                case 0: // 5 min
                    isCloseTick = (currTimeStartMin % 005) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 1: // 10 min
                    isCloseTick = (currTimeStartMin % 010) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 2: // 15 min
                    isCloseTick = (currTimeStartMin % 015) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 3: // 20 min
                    isCloseTick = (currTimeStartMin % 020) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 4: // 30 min
                    isCloseTick = (currTimeStartMin % 030) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 5: // 60 min
                    isCloseTick = (currTimeStartMin % 060) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 6: // 120 min
                    isCloseTick = (currTimeStartMin % 120) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 7: // 240 min
                    isCloseTick = (currTimeStartMin % 240) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 8: // daily
                    isCloseTick = (currTimeStartReg == sessEndTimeReg);
                    break;

                case 9: // weekly
                    bool isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = GetLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfYear() == true && GetCurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }


            return isCloseTick;
        }
        private bool IsLastDayOfYear()
        {
            DateTime lastDay = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDay.Date) return true;

            return false;
        }
        private bool GetLastTradingDayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            int day = Bars.TimeValue.Day;

            // get the total number of days in this month.
            int totalDaysInMonth = DateTime.DaysInMonth(year, month);

            // if last day of month is current bar
            if (Bars.TimeValue.Day == totalDaysInMonth) return true;

            // else check for last trading day
            DateTime lastDayOfMth = new DateTime(year, month, totalDaysInMonth);

            // if the last day of the month is a weekend
            if (lastDayOfMth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMth.DayOfWeek == DayOfWeek.Sunday)
            {
                // if current day is the last friday of the month.
                if (day == GetLastFridayOfMonth()) return true;
            }

            return false;
        }
        private int GetLastFridayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }
        private int GetSessionEndTime()
        {
            // regular, not in total minutes.
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }
        private int GetCurrentTime()
        {
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }
        private int GetCurrentTimeFromSessionOpen()
        {
            // minutes from open
            int minFromSessOpen = 0;

            // total min from Midnight. e.g. 1050 / 60 = 17.5 hours
            int sessStartMin = (int)Bars.Sessions[0].StartTime.TotalMinutes;
            int sessEndMin = (int)Bars.Sessions[0].EndTime.TotalMinutes;

            // current time in minutes from midnight
            int currTimeMin = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // no. of minutes in a day 
            int totalMinDay = 24 * 60;

            // if no condition applies. Return 0.
            if (currTimeMin >= sessStartMin && currTimeMin < totalMinDay)
            {
                minFromSessOpen = currTimeMin - sessStartMin;
            }
            else if (currTimeMin > 0 && currTimeMin <= sessEndMin)
            {
                minFromSessOpen = totalMinDay - sessStartMin + currTimeMin;
            }
            else if (currTimeMin == 0)
            {
                minFromSessOpen = totalMinDay - sessStartMin;
            }

            return minFromSessOpen;
        }
        private void SetTFsForCalculationBasedOnInterval()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // clear all previous values.
            IsTimeFrameSelected = new int[Timeframes];

            // get user input first.
            IsTimeFrameSelected[0] = Calc_5;
            IsTimeFrameSelected[1] = Calc_10;
            IsTimeFrameSelected[2] = Calc_15;
            IsTimeFrameSelected[3] = Calc_20;
            IsTimeFrameSelected[4] = Calc_30;
            IsTimeFrameSelected[5] = Calc_60;
            IsTimeFrameSelected[6] = Calc_120;
            IsTimeFrameSelected[7] = Calc_240;
            IsTimeFrameSelected[8] = Calc_Day;
            IsTimeFrameSelected[9] = Calc_Week;
            IsTimeFrameSelected[10] = Calc_Mth;

            // Get bar resolution.
            int chartTF = (int)Bars.Info.Resolution.Size;

            // one day
            if (Bars.Info.Resolution.Type == EResolution.Day && chartTF == 1)
            {
                // disable all calculations from 0 - 7
                for (int i = 0; i < 8; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // one week 
            else if (Bars.Info.Resolution.Type == EResolution.Week && chartTF == 1)
            {
                // disable all calculations from 0 - 8
                for (int i = 0; i < 9; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // one month
            else if (Bars.Info.Resolution.Type == EResolution.Month && chartTF == 1)
            {
                // disable all calculations from 0 - 9
                for (int i = 0; i < 10; i++)
                    IsTimeFrameSelected[i] = -1;

                return;
            }

            // min chart
            else if (Bars.Info.Resolution.Type == EResolution.Minute)
            {
                // disable calculations if below a certain timeframe.
                switch (chartTF)
                {
                    case 10:
                        // disable 5 min
                        IsTimeFrameSelected[0] = -1;
                        break;

                    case 15:
                        // disable 5, 10
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        break;

                    case 20:
                        // disable 5, 10, 15
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        break;

                    case 30:
                        // disable 5, 10, 15, 20
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        break;

                    case 60:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        break;

                    case 120:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        IsTimeFrameSelected[5] = -1;
                        break;

                    case 240:
                        // disable 5, 10, 15, 20, 30
                        IsTimeFrameSelected[0] = -1;
                        IsTimeFrameSelected[1] = -1;
                        IsTimeFrameSelected[2] = -1;
                        IsTimeFrameSelected[3] = -1;
                        IsTimeFrameSelected[4] = -1;
                        IsTimeFrameSelected[5] = -1;
                        IsTimeFrameSelected[6] = -1;
                        break;
                }

                return;
            }


            // if none of the above cases, disable all calculations.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;
        }
        private string DisplayTimeFrameSize(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            switch (timeframe)
            {
                case 0:
                    return "Timeframe: 5 min";
                case 1:
                    return "Timeframe: 10 min";
                case 2:
                    return "Timeframe: 15 min";
                case 3:
                    return "Timeframe: 20 min";
                case 4:
                    return "Timeframe: 30 min";
                case 5:
                    return "Timeframe: 60 min";
                case 6:
                    return "Timeframe: 120 min";
                case 7:
                    return "Timeframe: 240 min";
                case 8:
                    return "Timeframe: Daily";
                case 9:
                    return "Timeframe: Weekly";
                case 10:
                    return "Timeframe: Monthly";
            }

            return "Incorrect timeframe";
        }


        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class PriceActionMovingAverageMTF_v13 : FunctionSeries<bool>
    {
        // class properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }
        public bool _CalculateAll { get; set; }

        // Return all 11 Variableseries<double> MTF values.
        public ISeries<double> Pama_5 { get { return ListOfPamaValues[0]; } }
        public ISeries<double> Pama_10 { get { return ListOfPamaValues[1]; } }
        public ISeries<double> Pama_15 { get { return ListOfPamaValues[2]; } }
        public ISeries<double> Pama_20 { get { return ListOfPamaValues[3]; } }
        public ISeries<double> Pama_30 { get { return ListOfPamaValues[4]; } }
        public ISeries<double> Pama_60 { get { return ListOfPamaValues[5]; } }
        public ISeries<double> Pama_120 { get { return ListOfPamaValues[6]; } }
        public ISeries<double> Pama_240 { get { return ListOfPamaValues[7]; } }
        public ISeries<double> Pama_Day { get { return ListOfPamaValues[8]; } }
        public ISeries<double> Pama_Week { get { return ListOfPamaValues[9]; } }
        public ISeries<double> Pama_Month { get { return ListOfPamaValues[10]; } }

        public PriceActionMovingAverageMTF_v13(CStudyControl _master) : base(_master)
        {
            // can we use function class constructor to define the default values?
            _CalculateAll = false;
        }
        public PriceActionMovingAverageMTF_v13(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues;
        private List<HullMovingAverage> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private double[] Highs, Lows, Opens, Closes, Prices;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage>();
            ListOfHmaValues = new List<VariableSeries<double>>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];
            Highs = new double[Timeframes];
            Lows = new double[Timeframes];
            Opens = new double[Timeframes];
            Closes = new double[Timeframes];
            Prices = new double[Timeframes];

            // Set Hma lengths for each Hma object
            for (int i = 0; i < Timeframes; i++)
                ListOfHmas[i]._Length = _Length;

            // determine which time frame to calculate for current time interval.
            SetDefaultTFsForCurrentChart();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = IsTimeFrameOnCloseTick(timeframe);

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                ListOfHmas[timeframe]._AddData(Prices[timeframe]);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe] = Closes[timeframe];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe]
                            = Highs[timeframe] - Lows[timeframe];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe]
                            = (Highs[timeframe]
                            + Lows[timeframe]
                            + Opens[timeframe]
                            + Closes[timeframe]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe]
                            = (Opens[timeframe]
                            + Closes[timeframe]) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    Highs[timeframe] = Bars.HighValue;
                    Lows[timeframe] = Bars.LowValue;
                    Opens[timeframe] = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > Highs[timeframe])
                        Highs[timeframe] = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < Lows[timeframe])
                        Lows[timeframe] = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    Closes[timeframe] = Bars.CloseValue;
            }
        }
        private bool IsTimeFrameOnCloseTick(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            bool isCloseTick;
            int currTimeStartMin = GetCurrentTimeFromSessionOpen();
            int currTimeStartReg = GetCurrentTime();
            int sessEndTimeReg = GetSessionEndTime();

            switch (timeframe)
            {
                case 0: // 5 min
                    isCloseTick = (currTimeStartMin % 005) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 1: // 10 min
                    isCloseTick = (currTimeStartMin % 010) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 2: // 15 min
                    isCloseTick = (currTimeStartMin % 015) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 3: // 20 min
                    isCloseTick = (currTimeStartMin % 020) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 4: // 30 min
                    isCloseTick = (currTimeStartMin % 030) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 5: // 60 min
                    isCloseTick = (currTimeStartMin % 060) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 6: // 120 min
                    isCloseTick = (currTimeStartMin % 120) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 7: // 240 min
                    isCloseTick = (currTimeStartMin % 240) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 8: // daily
                    isCloseTick = (currTimeStartReg == sessEndTimeReg);
                    break;

                case 9: // weekly
                    bool isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = GetLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfYear() == true && GetCurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }


            return isCloseTick;
        }
        private bool IsLastDayOfYear()
        {
            DateTime lastDay = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDay.Date) return true;

            return false;
        }
        private bool GetLastTradingDayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            int day = Bars.TimeValue.Day;

            // get the total number of days in this month.
            int totalDaysInMonth = DateTime.DaysInMonth(year, month);

            // if last day of month is current bar
            if (Bars.TimeValue.Day == totalDaysInMonth) return true;

            // else check for last trading day
            DateTime lastDayOfMth = new DateTime(year, month, totalDaysInMonth);

            // if the last day of the month is a weekend
            if (lastDayOfMth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMth.DayOfWeek == DayOfWeek.Sunday)
            {
                // if current day is the last friday of the month.
                if (day == GetLastFridayOfMonth()) return true;
            }

            return false;
        }
        private int GetLastFridayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }
        private int GetSessionEndTime()
        {
            // regular, not in total minutes.
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }
        private int GetCurrentTime()
        {
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }
        private int GetCurrentTimeFromSessionOpen()
        {
            // minutes from open
            int minFromSessOpen = 0;

            // total min from Midnight. e.g. 1050 / 60 = 17.5 hours
            int sessStartMin = (int)Bars.Sessions[0].StartTime.TotalMinutes;
            int sessEndMin = (int)Bars.Sessions[0].EndTime.TotalMinutes;

            // current time in minutes from midnight
            int currTimeMin = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // no. of minutes in a day 
            int totalMinDay = 24 * 60;

            // if no condition applies. Return 0.
            if (currTimeMin >= sessStartMin && currTimeMin < totalMinDay)
            {
                minFromSessOpen = currTimeMin - sessStartMin;
            }
            else if (currTimeMin > 0 && currTimeMin <= sessEndMin)
            {
                minFromSessOpen = totalMinDay - sessStartMin + currTimeMin;
            }
            else if (currTimeMin == 0)
            {
                minFromSessOpen = totalMinDay - sessStartMin;
            }

            return minFromSessOpen;
        }
        private void SetDefaultTFsForCurrentChart()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // set disabled by default.
            IsTimeFrameSelected = new int[Timeframes];

            // by default, we do 5, 15, 30, 60, 240 charts
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if you want to calculate all values and not default values.
            if (_CalculateAll == true)
            {
                switch (chartTF)
                {
                    case 5:
                        for (int i = 0; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 15:
                        // 5, 10, weekly, monthly disabled
                        for (int i = 2; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 30:
                        // 5, 10, 15, weekly, monthly disabled
                        for (int i = 4; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 60:
                        // 5, 10, 15, 30, monthly disabled
                        for (int i = 5; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 240:
                        for (int i = 7; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    default:
                        break;
                }

                return;
            }

            // Set to disable first.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    // weekly, monthly disabled
                    for (int i = 0; i <= 8; i++)
                    {
                        // only these timeframes will be turned on
                        if (i == 0 || i == 5 || i == 7)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 15:
                    // 5, 10, weekly, monthly disabled
                    for (int i = 2; i <= 8; i++)
                    {
                        if (i == 2 || i == 5 || i == 7)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 30:
                    // 5, 10, 15, weekly, monthly disabled
                    for (int i = 4; i <= 8; i++)
                    {
                        if (i == 4 || i == 5 || i == 7)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 60:
                    // 5, 10, 15, 30, monthly disabled
                    for (int i = 5; i <= 9; i++)
                    {
                        // 60, 240, day
                        if (i == 5 || i == 7 || i == 8)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 240:
                    // index 10 is the last.
                    for (int i = 7; i < 11; i++)
                    {
                        if (i == 7 || i == 8 || i == 9)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                default:
                    break;
            }
        }
        private string DisplayTimeFrameSize(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            switch (timeframe)
            {
                case 0:
                    return "Timeframe: 5 min";
                case 1:
                    return "Timeframe: 10 min";
                case 2:
                    return "Timeframe: 15 min";
                case 3:
                    return "Timeframe: 20 min";
                case 4:
                    return "Timeframe: 30 min";
                case 5:
                    return "Timeframe: 60 min";
                case 6:
                    return "Timeframe: 120 min";
                case 7:
                    return "Timeframe: 240 min";
                case 8:
                    return "Timeframe: Daily";
                case 9:
                    return "Timeframe: Weekly";
                case 10:
                    return "Timeframe: Monthly";
            }

            return "Incorrect timeframe";
        }


        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class PriceActionMovingAverageMTF_v12 : FunctionSeries<bool>
    {
        // class properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }
        public bool _CalculateAll { get; set; }

        // Return all 11 Variableseries<double> MTF values.
        public ISeries<double> Pama_5 { get { return ListOfPamaValues[0]; } }
        public ISeries<double> Pama_10 { get { return ListOfPamaValues[1]; } }
        public ISeries<double> Pama_15 { get { return ListOfPamaValues[2]; } }
        public ISeries<double> Pama_20 { get { return ListOfPamaValues[3]; } }
        public ISeries<double> Pama_30 { get { return ListOfPamaValues[4]; } }
        public ISeries<double> Pama_60 { get { return ListOfPamaValues[5]; } }
        public ISeries<double> Pama_120 { get { return ListOfPamaValues[6]; } }
        public ISeries<double> Pama_240 { get { return ListOfPamaValues[7]; } }
        public ISeries<double> Pama_Day { get { return ListOfPamaValues[8]; } }
        public ISeries<double> Pama_Week { get { return ListOfPamaValues[9]; } }
        public ISeries<double> Pama_Month { get { return ListOfPamaValues[10]; } }

        public PriceActionMovingAverageMTF_v12(CStudyControl _master) : base(_master)
        {
            // can we use function class constructor to define the default values?
            _CalculateAll = false;
        }
        public PriceActionMovingAverageMTF_v12(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, IsTimeFrameSelected;
        private List<VariableSeries<double>> ListOfPamaValues, ListOfHmaValues, Prices;
        private List<HullMovingAverage_v13> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<VariableSeries<double>> ListOfHighs, ListOfLows, ListOfOpens, ListOfCloses;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage_v13>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfHighs = new List<VariableSeries<double>>();
            ListOfLows = new List<VariableSeries<double>>();
            ListOfOpens = new List<VariableSeries<double>>();
            ListOfCloses = new List<VariableSeries<double>>();
            Prices = new List<VariableSeries<double>>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage_v13(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                Prices.Add(new VariableSeries<double>(this));
                ListOfHighs.Add(new VariableSeries<double>(this));
                ListOfLows.Add(new VariableSeries<double>(this));
                ListOfOpens.Add(new VariableSeries<double>(this));
                ListOfCloses.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset these data before calculations
            BarCount = new int[Timeframes];

            // determine which time frame to calculate for current time interval.
            SetDefaultTFsForCurrentChart();

            // setup HMA function properties.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                ListOfHmas[timeframe]._Length = _Length;
                ListOfHmas[timeframe]._Price = Prices[timeframe];
            }
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                if (IsTimeFrameSelected[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = IsTimeFrameOnCloseTick(timeframe);

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                //ListOfHmas[timeframe]._AddData(Prices[timeframe]);
                //ListOfHmas[timeframe]._Price = Prices[timeframe];

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (Prices[timeframe][0] - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (Prices[timeframe][0] - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        Prices[timeframe].Value = ListOfCloses[timeframe][0];
                        break;

                    case 2: // use of Range Price
                        Prices[timeframe].Value
                            = ListOfHighs[timeframe][0] - ListOfLows[timeframe][0];
                        break;

                    case 3: // Use of HLOC Average Price
                        Prices[timeframe].Value
                            = (ListOfHighs[timeframe][0]
                            + ListOfLows[timeframe][0]
                            + ListOfOpens[timeframe][0]
                            + ListOfCloses[timeframe][0]) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        Prices[timeframe].Value
                            = (ListOfOpens[timeframe][0]
                            + ListOfCloses[timeframe][0]) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    ListOfHighs[timeframe].Value = Bars.HighValue;
                    ListOfLows[timeframe].Value = Bars.LowValue;
                    ListOfOpens[timeframe].Value = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > ListOfHighs[timeframe][0])
                        ListOfHighs[timeframe].Value = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < ListOfLows[timeframe][0])
                        ListOfLows[timeframe].Value = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    ListOfCloses[timeframe].Value = Bars.CloseValue;
            }
        }
        private bool IsTimeFrameOnCloseTick(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            bool isCloseTick;
            int currTimeStartMin = GetCurrentTimeFromSessionOpen();
            int currTimeStartReg = GetCurrentTime();
            int sessEndTimeReg = GetSessionEndTime();

            switch (timeframe)
            {
                case 0: // 5 min
                    isCloseTick = (currTimeStartMin % 005) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 1: // 10 min
                    isCloseTick = (currTimeStartMin % 010) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 2: // 15 min
                    isCloseTick = (currTimeStartMin % 015) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 3: // 20 min
                    isCloseTick = (currTimeStartMin % 020) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 4: // 30 min
                    isCloseTick = (currTimeStartMin % 030) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 5: // 60 min
                    isCloseTick = (currTimeStartMin % 060) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 6: // 120 min
                    isCloseTick = (currTimeStartMin % 120) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 7: // 240 min
                    isCloseTick = (currTimeStartMin % 240) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 8: // daily
                    isCloseTick = (currTimeStartReg == sessEndTimeReg);
                    break;

                case 9: // weekly
                    bool isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = GetLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfYear() == true && GetCurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }


            return isCloseTick;
        }
        private bool IsLastDayOfYear()
        {
            DateTime lastDay = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDay.Date) return true;

            return false;
        }
        private bool GetLastTradingDayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            int day = Bars.TimeValue.Day;

            // get the total number of days in this month.
            int totalDaysInMonth = DateTime.DaysInMonth(year, month);

            // if last day of month is current bar
            if (Bars.TimeValue.Day == totalDaysInMonth) return true;

            // else check for last trading day
            DateTime lastDayOfMth = new DateTime(year, month, totalDaysInMonth);

            // if the last day of the month is a weekend
            if (lastDayOfMth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMth.DayOfWeek == DayOfWeek.Sunday)
            {
                // if current day is the last friday of the month.
                if (day == GetLastFridayOfMonth()) return true;
            }

            return false;
        }
        private int GetLastFridayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }
        private int GetSessionEndTime()
        {
            // regular, not in total minutes.
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }
        private int GetCurrentTime()
        {
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }
        private int GetCurrentTimeFromSessionOpen()
        {
            // minutes from open
            int minFromSessOpen = 0;

            // total min from Midnight. e.g. 1050 / 60 = 17.5 hours
            int sessStartMin = (int)Bars.Sessions[0].StartTime.TotalMinutes;
            int sessEndMin = (int)Bars.Sessions[0].EndTime.TotalMinutes;

            // current time in minutes from midnight
            int currTimeMin = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // no. of minutes in a day 
            int totalMinDay = 24 * 60;

            // if no condition applies. Return 0.
            if (currTimeMin >= sessStartMin && currTimeMin < totalMinDay)
            {
                minFromSessOpen = currTimeMin - sessStartMin;
            }
            else if (currTimeMin > 0 && currTimeMin <= sessEndMin)
            {
                minFromSessOpen = totalMinDay - sessStartMin + currTimeMin;
            }
            else if (currTimeMin == 0)
            {
                minFromSessOpen = totalMinDay - sessStartMin;
            }

            return minFromSessOpen;
        }
        private void SetDefaultTFsForCurrentChart()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // set disabled by default.
            IsTimeFrameSelected = new int[Timeframes];

            // by default, we do 5, 15, 30, 60, 240 charts
            int chartTF = (int)Bars.Info.Resolution.Size;

            // if you want to calculate all values and not default values.
            if (_CalculateAll == true)
            {
                switch (chartTF)
                {
                    case 5:
                        for (int i = 0; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 15:
                        // 5, 10, weekly, monthly disabled
                        for (int i = 2; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 30:
                        // 5, 10, 15, weekly, monthly disabled
                        for (int i = 4; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 60:
                        // 5, 10, 15, 30, monthly disabled
                        for (int i = 5; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    case 240:
                        for (int i = 7; i < Timeframes; i++)
                            IsTimeFrameSelected[i] = 1;

                        break;

                    default:
                        break;
                }

                return;
            }

            // Set to disable first.
            for (int i = 0; i < Timeframes; i++)
                IsTimeFrameSelected[i] = -1;

            switch (chartTF)
            {
                case 5:
                    // weekly, monthly disabled
                    for (int i = 0; i <= 8; i++)
                    {
                        // only these timeframes will be turned on
                        if (i == 0 || i == 5 || i == 7)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 15:
                    // 5, 10, weekly, monthly disabled
                    for (int i = 2; i <= 8; i++)
                    {
                        if (i == 2 || i == 5 || i == 7)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 30:
                    // 5, 10, 15, weekly, monthly disabled
                    for (int i = 4; i <= 8; i++)
                    {
                        if (i == 4 || i == 5 || i == 7)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 60:
                    // 5, 10, 15, 30, monthly disabled
                    for (int i = 5; i <= 9; i++)
                    {
                        // 60, 240, day
                        if (i == 5 || i == 7 || i == 8)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                case 240:
                    // index 10 is the last.
                    for (int i = 7; i < 11; i++)
                    {
                        if (i == 7 || i == 8 || i == 9)
                            IsTimeFrameSelected[i] = 1;
                        else
                            IsTimeFrameSelected[i] = 0;
                    }
                    break;

                default:
                    break;
            }
        }
        private string DisplayTimeFrameSize(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            switch (timeframe)
            {
                case 0:
                    return "Timeframe: 5 min";
                case 1:
                    return "Timeframe: 10 min";
                case 2:
                    return "Timeframe: 15 min";
                case 3:
                    return "Timeframe: 20 min";
                case 4:
                    return "Timeframe: 30 min";
                case 5:
                    return "Timeframe: 60 min";
                case 6:
                    return "Timeframe: 120 min";
                case 7:
                    return "Timeframe: 240 min";
                case 8:
                    return "Timeframe: Daily";
                case 9:
                    return "Timeframe: Weekly";
                case 10:
                    return "Timeframe: Monthly";
            }

            return "Incorrect timeframe";
        }

        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class PriceActionMovingAverageMTF_v11 : FunctionSeries<bool>
    {
        // class properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }

        // Return all 11 Variableseries<double> MTF values.
        public ISeries<double> Pama_5 { get { return ListOfPamaValues[0]; } }
        public ISeries<double> Pama_10 { get { return ListOfPamaValues[1]; } }
        public ISeries<double> Pama_15 { get { return ListOfPamaValues[2]; } }
        public ISeries<double> Pama_20 { get { return ListOfPamaValues[3]; } }
        public ISeries<double> Pama_30 { get { return ListOfPamaValues[4]; } }
        public ISeries<double> Pama_60 { get { return ListOfPamaValues[5]; } }
        public ISeries<double> Pama_120 { get { return ListOfPamaValues[6]; } }
        public ISeries<double> Pama_240 { get { return ListOfPamaValues[7]; } }
        public ISeries<double> Pama_Day { get { return ListOfPamaValues[8]; } }
        public ISeries<double> Pama_Week { get { return ListOfPamaValues[9]; } }
        public ISeries<double> Pama_Month { get { return ListOfPamaValues[10]; } }

        public PriceActionMovingAverageMTF_v11(CStudyControl _master) : base(_master) { }
        public PriceActionMovingAverageMTF_v11(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, DoCalculationsOn;
        private List<VariableSeries<double>> ListOfPamaValues;
        private List<HullMovingAverage_v10> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<VariableSeries<double>> ListOfHighs, ListOfLows, ListOfOpens, ListOfCloses;
        private List<VariableSeries<double>> ListOfCalcPrices, ListOfHmaValues;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage_v10>();
            ListOfHighs = new List<VariableSeries<double>>();
            ListOfLows = new List<VariableSeries<double>>();
            ListOfOpens = new List<VariableSeries<double>>();
            ListOfCloses = new List<VariableSeries<double>>();
            ListOfCalcPrices = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage_v10(this));
                ListOfHighs.Add(new VariableSeries<double>(this));
                ListOfLows.Add(new VariableSeries<double>(this));
                ListOfOpens.Add(new VariableSeries<double>(this));
                ListOfCloses.Add(new VariableSeries<double>(this));
                ListOfCalcPrices.Add(new VariableSeries<double>(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset bar count before calculation
            BarCount = new int[Timeframes];

            for (int i = 0; i < Timeframes; i++)
                ListOfHmas[i]._Length = _Length;

            // determine which time frame to calculate for current time interval.
            SetDefaultTFsForCurrentChart();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                //if (DoCalculationsOn[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = IsTimeFrameOnCloseTick(timeframe);

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                //ListOfHmas[timeframe]._Data = ListOfCalcPrices[timeframe].Value;

                //ListOfHmas[timeframe].Call();
                ListOfHmas[timeframe]._AddData(ListOfCalcPrices[timeframe].Value);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (ListOfCalcPrices[timeframe].Value - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (ListOfCalcPrices[timeframe].Value - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        ListOfCalcPrices[timeframe].Value = ListOfCloses[timeframe].Value;
                        break;

                    case 2: // use of Range Price
                        ListOfCalcPrices[timeframe].Value
                            = ListOfHighs[timeframe].Value - ListOfLows[timeframe].Value;
                        break;

                    case 3: // Use of HLOC Average Price
                        ListOfCalcPrices[timeframe].Value
                            = (ListOfHighs[timeframe].Value
                            + ListOfLows[timeframe].Value
                            + ListOfOpens[timeframe].Value
                            + ListOfCloses[timeframe].Value) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        ListOfCalcPrices[timeframe].Value
                            = (ListOfOpens[timeframe].Value
                            + ListOfCloses[timeframe].Value) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    ListOfHighs[timeframe].Value = Bars.HighValue;
                    ListOfLows[timeframe].Value = Bars.LowValue;
                    ListOfOpens[timeframe].Value = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > ListOfHighs[timeframe].Value)
                        ListOfHighs[timeframe].Value = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < ListOfLows[timeframe].Value)
                        ListOfLows[timeframe].Value = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    ListOfCloses[timeframe].Value = Bars.CloseValue;
            }
        }
        private bool IsTimeFrameOnCloseTick(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            bool isCloseTick;
            int currTimeStartMin = GetCurrentTimeFromSessionOpen();
            int currTimeStartReg = GetCurrentTime();
            int sessEndTimeReg = GetSessionEndTime();

            switch (timeframe)
            {
                case 0: // 5 min
                    isCloseTick = (currTimeStartMin % 005) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 1: // 10 min
                    isCloseTick = (currTimeStartMin % 010) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 2: // 15 min
                    isCloseTick = (currTimeStartMin % 015) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 3: // 20 min
                    isCloseTick = (currTimeStartMin % 020) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 4: // 30 min
                    isCloseTick = (currTimeStartMin % 030) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 5: // 60 min
                    isCloseTick = (currTimeStartMin % 060) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 6: // 120 min
                    isCloseTick = (currTimeStartMin % 120) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 7: // 240 min
                    isCloseTick = (currTimeStartMin % 240) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 8: // daily
                    isCloseTick = (currTimeStartReg == sessEndTimeReg);
                    break;

                case 9: // weekly
                    bool isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = GetLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfYear() == true && GetCurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }


            return isCloseTick;
        }
        private bool IsLastDayOfYear()
        {
            DateTime lastDay = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDay.Date) return true;

            return false;
        }
        private bool GetLastTradingDayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            int day = Bars.TimeValue.Day;

            // get the total number of days in this month.
            int totalDaysInMonth = DateTime.DaysInMonth(year, month);

            // if last day of month is current bar
            if (Bars.TimeValue.Day == totalDaysInMonth) return true;

            // else check for last trading day
            DateTime lastDayOfMth = new DateTime(year, month, totalDaysInMonth);

            // if the last day of the month is a weekend
            if (lastDayOfMth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMth.DayOfWeek == DayOfWeek.Sunday)
            {
                // if current day is the last friday of the month.
                if (day == GetLastFridayOfMonth()) return true;
            }

            return false;
        }
        private int GetLastFridayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }
        private int GetSessionEndTime()
        {
            // regular, not in total minutes.
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }
        private int GetCurrentTime()
        {
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }
        private int GetCurrentTimeFromSessionOpen()
        {
            // minutes from open
            int minFromSessOpen = 0;

            // total min from Midnight. e.g. 1050 / 60 = 17.5 hours
            int sessStartMin = (int)Bars.Sessions[0].StartTime.TotalMinutes;
            int sessEndMin = (int)Bars.Sessions[0].EndTime.TotalMinutes;

            // current time in minutes from midnight
            int currTimeMin = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // no. of minutes in a day 
            int totalMinDay = 24 * 60;

            // if no condition applies. Return 0.
            if (currTimeMin >= sessStartMin && currTimeMin < totalMinDay)
            {
                minFromSessOpen = currTimeMin - sessStartMin;
            }
            else if (currTimeMin > 0 && currTimeMin <= sessEndMin)
            {
                minFromSessOpen = totalMinDay - sessStartMin + currTimeMin;
            }
            else if (currTimeMin == 0)
            {
                minFromSessOpen = totalMinDay - sessStartMin;
            }

            return minFromSessOpen;
        }
        private void SetDefaultTFsForCurrentChart()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // set disabled by default.
            DoCalculationsOn = new int[Timeframes];

            // Set to disable first.
            for (int i = 0; i < Timeframes; i++)
                DoCalculationsOn[i] = -1;

            // by default, we do 5, 15, 30, 60, 240 charts
            int chartTF = (int)Bars.Info.Resolution.Size;
            switch (chartTF)
            {
                case 5:
                    // weekly, monthly disabled
                    for (int i = 0; i <= 8; i++)
                    {
                        // only these timeframes will be turned on
                        if (i == 0 || i == 5 || i == 7)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 15:
                    // 5, 10, weekly, monthly disabled
                    for (int i = 2; i <= 8; i++)
                    {
                        if (i == 2 || i == 5 || i == 7)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 30:
                    // 5, 10, 15, weekly, monthly disabled
                    for (int i = 4; i <= 8; i++)
                    {
                        if (i == 4 || i == 5 || i == 7)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 60:
                    // 5, 10, 15, 30, monthly disabled
                    for (int i = 5; i <= 9; i++)
                    {
                        // 60, 240, day
                        if (i == 5 || i == 7 || i == 8)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 240:
                    // index 10 is the last.
                    for (int i = 7; i < 11; i++)
                    {
                        if (i == 7 || i == 8 || i == 9)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                default:
                    break;
            }
        }
        private string DisplayTimeFrameSize(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            switch (timeframe)
            {
                case 0:
                    return "Timeframe: 5 min";
                case 1:
                    return "Timeframe: 10 min";
                case 2:
                    return "Timeframe: 15 min";
                case 3:
                    return "Timeframe: 20 min";
                case 4:
                    return "Timeframe: 30 min";
                case 5:
                    return "Timeframe: 60 min";
                case 6:
                    return "Timeframe: 120 min";
                case 7:
                    return "Timeframe: 240 min";
                case 8:
                    return "Timeframe: Daily";
                case 9:
                    return "Timeframe: Weekly";
                case 10:
                    return "Timeframe: Monthly";
            }

            return "Incorrect timeframe";
        }


        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }
    public sealed class PriceActionMovingAverageMTF_v10 : FunctionSeries<bool>
    {
        // class properties. 
        public int _Length { get; set; }
        public int _Smooth { get; set; }
        public int _PriceType { get; set; }

        // Return all 11 Variableseries<double> MTF values.
        public ISeries<double> Pama_5 { get { return ListOfPamaValues[0]; } }
        public ISeries<double> Pama_10 { get { return ListOfPamaValues[1]; } }
        public ISeries<double> Pama_15 { get { return ListOfPamaValues[2]; } }
        public ISeries<double> Pama_20 { get { return ListOfPamaValues[3]; } }
        public ISeries<double> Pama_30 { get { return ListOfPamaValues[4]; } }
        public ISeries<double> Pama_60 { get { return ListOfPamaValues[5]; } }
        public ISeries<double> Pama_120 { get { return ListOfPamaValues[6]; } }
        public ISeries<double> Pama_240 { get { return ListOfPamaValues[7]; } }
        public ISeries<double> Pama_Day { get { return ListOfPamaValues[8]; } }
        public ISeries<double> Pama_Week { get { return ListOfPamaValues[9]; } }
        public ISeries<double> Pama_Month { get { return ListOfPamaValues[10]; } }

        public PriceActionMovingAverageMTF_v10(CStudyControl _master) : base(_master) { }
        public PriceActionMovingAverageMTF_v10(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class fields
        private const int Timeframes = 11;
        private double Alpha, Gain;

        private int[] BarCount, DoCalculationsOn;
        private List<VariableSeries<double>> ListOfPamaValues;
        private List<HullMovingAverage_v11> ListOfHmas;
        private List<VariableSeries<bool>> ListOfClosingTicks;
        private List<VariableSeries<double>> ListOfHighs, ListOfLows, ListOfOpens, ListOfCloses;
        private List<VariableSeries<double>> ListOfCalcPrices, ListOfHmaValues;

        protected override void Create()
        {
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfClosingTicks = new List<VariableSeries<bool>>();
            ListOfHmas = new List<HullMovingAverage_v11>();
            ListOfHighs = new List<VariableSeries<double>>();
            ListOfLows = new List<VariableSeries<double>>();
            ListOfOpens = new List<VariableSeries<double>>();
            ListOfCloses = new List<VariableSeries<double>>();
            ListOfCalcPrices = new List<VariableSeries<double>>();
            ListOfHmaValues = new List<VariableSeries<double>>();

            for (int i = 0; i < Timeframes; i++)
            {
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfClosingTicks.Add(new VariableSeries<bool>(this));
                ListOfHmas.Add(new HullMovingAverage_v11(this));
                ListOfHighs.Add(new VariableSeries<double>(this));
                ListOfLows.Add(new VariableSeries<double>(this));
                ListOfOpens.Add(new VariableSeries<double>(this));
                ListOfCloses.Add(new VariableSeries<double>(this));
                ListOfCalcPrices.Add(new VariableSeries<double>(this));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
            }
        }
        protected override void StartCalc()
        {
            Alpha = 2.0 / (_Length + 1.0);
            Gain = _Smooth / 100.0;

            // reset bar count before calculation
            BarCount = new int[Timeframes];

            for (int i = 0; i < Timeframes; i++)
                ListOfHmas[i]._Length = _Length;

            // determine which time frame to calculate for current time interval.
            SetDefaultTFsForCurrentChart();
        }
        protected override bool CalcBar()
        {
            // ensure that this function is calculated at closing tick.
            if (Bars.Status != EBarState.Close) return false;

            // on each bar, iterate through each timeframe.
            for (int timeframe = 0; timeframe < Timeframes; timeframe++)
            {
                // if timeframe is not selected, dont calculate.
                //if (DoCalculationsOn[timeframe] != 1) continue;

                // check for each individual timeframe for closing ticks
                ListOfClosingTicks[timeframe].Value = IsTimeFrameOnCloseTick(timeframe);

                // setup price data for each individual timeframe
                SetPriceData(timeframe);

                // determine price types to use for price action average calculations
                SetPriceTypes(timeframe);

                // calculate price action moving average values
                SetPamaValues(timeframe);

                if (ListOfClosingTicks[timeframe].Value == true)
                    BarCount[timeframe]++;
            }

            return false;
        }

        #region Helper Methods
        private void SetPamaValues(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe].Value == true)
            {
                // pass the data to HMA for calculations
                //ListOfHmas[timeframe]._Data = ListOfCalcPrices[timeframe].Value;
                ListOfHmas[timeframe]._AddData(ListOfCalcPrices[timeframe].Value);

                // cache the Hma return values into variableseries.
                ListOfHmaValues[timeframe].Value = ListOfHmas[timeframe].Value;

                // original
                if (ListOfCalcPrices[timeframe].Value - ListOfPamaValues[timeframe][1] > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                    + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else if (ListOfCalcPrices[timeframe].Value - ListOfPamaValues[timeframe][1] < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues[timeframe][1]))
                   + (1.0 - Alpha) * ListOfPamaValues[timeframe][1];
                }
            }
        }
        private void SetPriceTypes(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfClosingTicks[timeframe][0] == true)
            {
                switch (_PriceType)
                {
                    default:
                    case 1: // using Close Price
                        ListOfCalcPrices[timeframe].Value = ListOfCloses[timeframe].Value;
                        break;

                    case 2: // use of Range Price
                        ListOfCalcPrices[timeframe].Value
                            = ListOfHighs[timeframe].Value - ListOfLows[timeframe].Value;
                        break;

                    case 3: // Use of HLOC Average Price
                        ListOfCalcPrices[timeframe].Value
                            = (ListOfHighs[timeframe].Value
                            + ListOfLows[timeframe].Value
                            + ListOfOpens[timeframe].Value
                            + ListOfCloses[timeframe].Value) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        ListOfCalcPrices[timeframe].Value
                            = (ListOfOpens[timeframe].Value
                            + ListOfCloses[timeframe].Value) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceData(int timeframe)
        {
            if (BarCount[timeframe] > 0)
            {
                // next regular bar after closing tick for calculated timeframe
                if (ListOfClosingTicks[timeframe][1] == true)
                {
                    ListOfHighs[timeframe].Value = Bars.HighValue;
                    ListOfLows[timeframe].Value = Bars.LowValue;
                    ListOfOpens[timeframe].Value = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > ListOfHighs[timeframe].Value)
                        ListOfHighs[timeframe].Value = Bars.High[0];

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < ListOfLows[timeframe].Value)
                        ListOfLows[timeframe].Value = Bars.Low[0];
                }

                // Keep track of closing price.
                if (ListOfClosingTicks[timeframe][0] == true)
                    ListOfCloses[timeframe].Value = Bars.CloseValue;
            }
        }
        private bool IsTimeFrameOnCloseTick(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            bool isCloseTick;
            int currTimeStartMin = GetCurrentTimeFromSessionOpen();
            int currTimeStartReg = GetCurrentTime();
            int sessEndTimeReg = GetSessionEndTime();

            switch (timeframe)
            {
                case 0: // 5 min
                    isCloseTick = (currTimeStartMin % 005) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 1: // 10 min
                    isCloseTick = (currTimeStartMin % 010) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 2: // 15 min
                    isCloseTick = (currTimeStartMin % 015) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 3: // 20 min
                    isCloseTick = (currTimeStartMin % 020) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 4: // 30 min
                    isCloseTick = (currTimeStartMin % 030) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 5: // 60 min
                    isCloseTick = (currTimeStartMin % 060) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 6: // 120 min
                    isCloseTick = (currTimeStartMin % 120) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 7: // 240 min
                    isCloseTick = (currTimeStartMin % 240) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 8: // daily
                    isCloseTick = (currTimeStartReg == sessEndTimeReg);
                    break;

                case 9: // weekly
                    bool isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = GetLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfYear() == true && GetCurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }


            return isCloseTick;
        }
        private bool IsLastDayOfYear()
        {
            DateTime lastDay = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDay.Date) return true;

            return false;
        }
        private bool GetLastTradingDayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            int day = Bars.TimeValue.Day;

            // get the total number of days in this month.
            int totalDaysInMonth = DateTime.DaysInMonth(year, month);

            // if last day of month is current bar
            if (Bars.TimeValue.Day == totalDaysInMonth) return true;

            // else check for last trading day
            DateTime lastDayOfMth = new DateTime(year, month, totalDaysInMonth);

            // if the last day of the month is a weekend
            if (lastDayOfMth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMth.DayOfWeek == DayOfWeek.Sunday)
            {
                // if current day is the last friday of the month.
                if (day == GetLastFridayOfMonth()) return true;
            }

            return false;
        }
        private int GetLastFridayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }
        private int GetSessionEndTime()
        {
            // regular, not in total minutes.
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }
        private int GetCurrentTime()
        {
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }
        private int GetCurrentTimeFromSessionOpen()
        {
            // minutes from open
            int minFromSessOpen = 0;

            // total min from Midnight. e.g. 1050 / 60 = 17.5 hours
            int sessStartMin = (int)Bars.Sessions[0].StartTime.TotalMinutes;
            int sessEndMin = (int)Bars.Sessions[0].EndTime.TotalMinutes;

            // current time in minutes from midnight
            int currTimeMin = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // no. of minutes in a day 
            int totalMinDay = 24 * 60;

            // if no condition applies. Return 0.
            if (currTimeMin >= sessStartMin && currTimeMin < totalMinDay)
            {
                minFromSessOpen = currTimeMin - sessStartMin;
            }
            else if (currTimeMin > 0 && currTimeMin <= sessEndMin)
            {
                minFromSessOpen = totalMinDay - sessStartMin + currTimeMin;
            }
            else if (currTimeMin == 0)
            {
                minFromSessOpen = totalMinDay - sessStartMin;
            }

            return minFromSessOpen;
        }
        private void SetDefaultTFsForCurrentChart()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // set disabled by default.
            DoCalculationsOn = new int[Timeframes];

            // Set to disable first.
            for (int i = 0; i < Timeframes; i++)
                DoCalculationsOn[i] = -1;

            // by default, we do 5, 15, 30, 60, 240 charts
            int chartTF = (int)Bars.Info.Resolution.Size;
            switch (chartTF)
            {
                case 5:
                    // weekly, monthly disabled
                    for (int i = 0; i <= 8; i++)
                    {
                        // only these timeframes will be turned on
                        if (i == 0 || i == 5 || i == 7)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 15:
                    // 5, 10, weekly, monthly disabled
                    for (int i = 2; i <= 8; i++)
                    {
                        if (i == 2 || i == 5 || i == 7)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 30:
                    // 5, 10, 15, weekly, monthly disabled
                    for (int i = 4; i <= 8; i++)
                    {
                        if (i == 4 || i == 5 || i == 7)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 60:
                    // 5, 10, 15, 30, monthly disabled
                    for (int i = 5; i <= 9; i++)
                    {
                        // 60, 240, day
                        if (i == 5 || i == 7 || i == 8)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                case 240:
                    // index 10 is the last.
                    for (int i = 7; i < 11; i++)
                    {
                        if (i == 7 || i == 8 || i == 9)
                            DoCalculationsOn[i] = 1;
                        else
                            DoCalculationsOn[i] = 0;
                    }
                    break;

                default:
                    break;
            }
        }
        private string DisplayTimeFrameSize(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            switch (timeframe)
            {
                case 0:
                    return "Timeframe: 5 min";
                case 1:
                    return "Timeframe: 10 min";
                case 2:
                    return "Timeframe: 15 min";
                case 3:
                    return "Timeframe: 20 min";
                case 4:
                    return "Timeframe: 30 min";
                case 5:
                    return "Timeframe: 60 min";
                case 6:
                    return "Timeframe: 120 min";
                case 7:
                    return "Timeframe: 240 min";
                case 8:
                    return "Timeframe: Daily";
                case 9:
                    return "Timeframe: Weekly";
                case 10:
                    return "Timeframe: Monthly";
            }

            return "Incorrect timeframe";
        }


        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }

    #endregion

    #region Developer Tools
    //==========================================================================================
    // Name           : Developer Tools
    // Description    : Library of useful developer functions
    // Version        : v.1.0
    // Date Created   : 12 - Jun - 2020
    // Time Taken     : 
    // Remarks        : Use PowerLanguage.Func to have access to Bars.Data
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*   ----------------------------------- Version History -----------------------------------
     *   v.1.0
     *   - HowManyBarsAgo returns the bar number, for a given datetime.
     *   - IsTimeToTrade returns true for a given start and end time.
     *   - Additional public datetime functions to determine closing ticks for different TFs
     *   ----------------------------------- Version History -----------------------------------
     *

     *   ---------------------------------------- Notes ----------------------------------------
     *   v.1.0
     *  
     *   
     *   ---------------------------------------- Notes ----------------------------------------
     */
    public sealed class DeveloperTools : FunctionSimple<string>
    {
        public DeveloperTools(CStudyControl m) : base(m) { }
        protected override string CalcBar()
        {
            return "Developer Tools";
        }

        #region International Time Zone
        public DateTime GetLondonTime()
        {
            DateTime currTime = DateTime.Now;
            TimeZoneInfo britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            return TimeZoneInfo.ConvertTime(currTime, TimeZoneInfo.Local, britishZone);
        }
        public DateTime GetEasternTime()
        {
            DateTime timeUtc = DateTime.UtcNow;
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(timeUtc, easternZone);
        }
        #endregion

        #region Closing Tick for Different TimeFrames

        public int ChartResolutionSize()
        {
            return (int)Bars.Info.Resolution.Size;
        }

        /// <summary>
        /// Returns a boolean for a given timeframe
        /// </summary>
        /// <param name="timeframe"></param>
        /// <returns></returns>
        public bool IsTimeFrameOnCloseTick(int timeframe)
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            bool isCloseTick;
            int currTimeStartMin = GetCurrTimeSinceSessOpenInTotalMin();
            int currTimeStartReg = CurrentTime();
            int sessEndTimeReg = GetSessionEndTime();

            switch (timeframe)
            {
                case 0: // 5 min
                    isCloseTick = (currTimeStartMin % 005) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 1: // 10 min
                    isCloseTick = (currTimeStartMin % 010) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 2: // 15 min
                    isCloseTick = (currTimeStartMin % 015) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 3: // 20 min
                    isCloseTick = (currTimeStartMin % 020) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 4: // 30 min
                    isCloseTick = (currTimeStartMin % 030) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 5: // 60 min
                    isCloseTick = (currTimeStartMin % 060) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 6: // 120 min
                    isCloseTick = (currTimeStartMin % 120) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 7: // 240 min
                    isCloseTick = (currTimeStartMin % 240) == 0 || (currTimeStartReg == sessEndTimeReg);
                    break;

                case 8: // daily
                    isCloseTick = (currTimeStartReg == sessEndTimeReg);
                    break;

                case 9: // weekly
                    bool isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = IsLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfYear() == true && CurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx = Bars.Info.Category == ESymbolCategory.Cash;
                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }


            return isCloseTick;
        }

        /// <summary>
        /// Returns true if current bar date == last day of the year
        /// </summary>
        /// <returns></returns>
        public bool IsLastDayOfYear()
        {
            DateTime lastDay = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDay.Date) return true;

            return false;
        }

        /// <summary>
        /// Returns true if current bar date is last trading day of month
        /// </summary>
        /// <returns></returns>
        public bool IsLastTradingDayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            int day = Bars.TimeValue.Day;

            // get the total number of days in this month.
            int totalDaysInMonth = DateTime.DaysInMonth(year, month);

            // if last day of month is current bar
            if (Bars.TimeValue.Day == totalDaysInMonth) return true;

            // else check for last trading day
            DateTime lastDayOfMth = new DateTime(year, month, totalDaysInMonth);

            // if the last day of the month is a weekend
            if (lastDayOfMth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMth.DayOfWeek == DayOfWeek.Sunday)
            {
                // if current day is the last friday of the month.
                if (day == GetLastFridayOfMonth()) return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the day of the last friday of the month in integer
        /// </summary>
        /// <returns></returns>
        public int GetLastFridayOfMonth()
        {
            int year = Bars.TimeValue.Year;
            int month = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }

        /// <summary>
        /// Returns the session end time in integer format
        /// </summary>
        /// <returns></returns>
        public int GetSessionEndTime()
        {
            // regular, not in total minutes.
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }

        /// <summary>
        /// Returns the current time in integer format. i.e 3pm = 1500
        /// </summary>
        /// <returns></returns>
        public int CurrentTime()
        {
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }

        /// <summary>
        /// Returns the current time since session opened in Total Minutes
        /// </summary>
        /// <returns></returns>
        public int GetCurrTimeSinceSessOpenInTotalMin()
        {
            // minutes from open
            int minFromSessOpen = 0;

            // total min from Midnight. e.g. 1050 / 60 = 17.5 hours
            int sessStartMin = (int)Bars.Sessions[0].StartTime.TotalMinutes;
            int sessEndMin = (int)Bars.Sessions[0].EndTime.TotalMinutes;

            // current time in minutes from midnight
            int currTimeMin = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // no. of minutes in a day 
            int totalMinDay = 24 * 60;

            // if no condition applies. Return 0.
            if (currTimeMin >= sessStartMin && currTimeMin < totalMinDay)
            {
                minFromSessOpen = currTimeMin - sessStartMin;
            }
            else if (currTimeMin > 0 && currTimeMin <= sessEndMin)
            {
                minFromSessOpen = totalMinDay - sessStartMin + currTimeMin;
            }
            else if (currTimeMin == 0)
            {
                minFromSessOpen = totalMinDay - sessStartMin;
            }

            return minFromSessOpen;
        }
        #endregion

        #region IsTimeToTrade

        /// <summary>
        /// Returns true if bar time is within start and end time
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public bool IsTimeToTrade(string startTime, string endTime)
        {
            // note: bar time is the last closing tick.
            TimeSpan start = ConvertStrToTime(startTime);
            TimeSpan end = ConvertStrToTime(endTime);
            TimeSpan barTime = Bars.TimeValue.TimeOfDay;

            // if range is on the same day. 00:00 till 23:59 same day yes.
            if (start < end) return (barTime >= start) && (barTime <= end);

            // if start is before midnight and end is past midnight
            return (barTime > start) || (barTime < end);
        }
        private TimeSpan ConvertStrToTime(string time)
        {
            // parse 24 hours format.
            DateTime dateTime = DateTime
                .ParseExact(time, "HH:mm", CultureInfo.InvariantCulture);

            return dateTime.TimeOfDay;
        }
        #endregion

        #region How many bars ago ? 
        /// <summary>
        /// Returns how many bars back from current bar time to given date time.
        /// </summary>
        /// <param name="barTime"></param>
        /// <returns></returns>
        public int HowManyBarsAgo(DateTime barTime)
        {
            // the bar you are searching cannot be greater than current bar time.
            if (barTime > Bars.TimeValue) return -99;

            // most current
            int right = Bars.FullSymbolData.Count - 1;
            int left = 0;

            return BarsAgo(barTime, right, left);
        }

        // Divide and conquer, recursively
        private int BarsAgo(DateTime bartime, int left, int right)
        {
            // base case, if there is only one element left.
            if (left == right)
            {
                if (Bars.FullSymbolData.Time[left] == bartime)
                {
                    return left;
                }
                else
                {
                    return -11;
                }
            }
            else
            {
                // calculate mid
                int mid = (left + right) / 2;

                // check if goal is achieved
                if (Bars.FullSymbolData.Time[mid] == bartime) return mid;

                if (bartime < Bars.FullSymbolData.Time[mid])
                {
                    return BarsAgo(bartime, left, mid - 1);
                }
                else
                {
                    return BarsAgo(bartime, mid + 1, right);
                }
            }
        }
        #endregion

        #region Round double
        public double RoundDouble(double value)
        {
            // check what pair is it
            string[] symbolName = Bars.Info.Name.Split('.');
            string baseSymbol = symbolName[1];

            // determine number of decimal places based on base currency
            if (baseSymbol == "JPY")
                return Math.Round(value, 3, MidpointRounding.AwayFromZero);

            return Math.Round(value, 5, MidpointRounding.AwayFromZero);
        }
        #endregion

        #region Double to string
        public string DoubleToString(double value)
        {
            // check what pair is it
            string[] symbolName = Bars.Info.Name.Split('.');
            string baseSymbol = symbolName[1];

            // determine number of decimal places based on base currency
            if (baseSymbol == "JPY") return DoubleToString(value, 3);

            return DoubleToString(value, 5);
        }
        public string DoubleToString(double value, int decimalPlaces)
        {
            return value.ToString("N" + decimalPlaces);
        }
        #endregion

        #region Calculate Risk Reward
        public double RiskReward(double entry, double stoploss, double profit)
        {
            double risk = Math.Abs(entry - stoploss);
            double reward = Math.Abs(profit - entry);

            // long
            if (stoploss > entry && profit > entry) return 0;

            // short
            if (stoploss < entry && profit < entry) return 0;

            // else return
            return Math.Round(reward / risk, 2);
        }
        #endregion

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

    }

    #endregion

    #region Test Functions
    public sealed class TestSeries : FunctionSeries<double>
    {
        // class properties
        public List<VariableSeries<double>> GetBarValues { get { return ListOfBarValues; } }
        // class fields

        // MC does this implicitly for you.
        //public override double Value { get { return CalcBar(); } }
        // class constructor
        public TestSeries(CStudyControl _master) : base(_master)
        {
        }

        // abstract class FunctionObjects<T> constructor forces this class to have constructors.
        public TestSeries(CStudyControl _master, int _ds) : base(_master, _ds) { }

        // class members
        private List<VariableSeries<double>> ListOfBarValues;
        protected override void Create()
        {
            ListOfBarValues = new List<VariableSeries<double>>();
            for (int i = 0; i < 4; i++)
                ListOfBarValues.Add(new VariableSeries<double>(this));
        }
        protected override void StartCalc()
        {
        }
        protected override double CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                ListOfBarValues[0].Value = Bars.CloseValue;
                ListOfBarValues[1].Value = Bars.OpenValue;
                ListOfBarValues[2].Value = Bars.HighValue;
                ListOfBarValues[3].Value = Bars.LowValue;
            }
            return 0;
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
    public sealed class TestSimple : FunctionSimple<int>
    {
        public int Length { get; set; }
        public TestSimple(CStudyControl _master) : base(_master) { }
        public TestSimple(CStudyControl _master, int _ds) : base(_master, _ds) { }

        protected override void Create()
        {
        }
        protected override int CalcBar()
        {
            return 0;
        }
    }
    #endregion
}
#endregion

#region Test Framework

namespace PowerLanguage.Indicator
{
    //==========================================================================================
    // Name           : Strategy Tester
    // Description    : Use Text objects to backtest any strategy
    // Version        : v.1.0
    // Date Created   : 10 - Jun - 2020
    // Time Taken     : 
    // Remarks        :
    //==========================================================================================
    // Copyright      : 2020, Sher Ning Technologies           
    // License        :      
    //==========================================================================================

    /*  ----------------------------------- Version History -----------------------------------
     *   v.1.0
     *   - Learn and develop my own approach to solving multichart strategies confidently
     *   
     *  ----------------------------------- Version History -----------------------------------
     

     *  ---------------------------------------- Notes ----------------------------------------
     *  -
     *  
     *   
     *  ---------------------------------------- Notes ----------------------------------------
     */
    public class ___SherNing_FunctionLib : IndicatorObject
    {
        // class properties
        [Input] public int Length { get; set; }

        public ___SherNing_FunctionLib (object _ctx) : base(_ctx)
        {
            Length = 6;
        }

        // class fields
        protected override void Create()
        {
            Output.Clear();
        }
        protected override void StartCalc()
        {
        }
        protected override void CalcBar()
        {
        }

        protected override void StopCalc()
        {
            Output.Clear();
        }

        #region Print Functions
        private bool DebugMode = true;
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            // Print who is calling Print()
            StackTrace stackTrace = new StackTrace();

            Output.WriteLine("Project:  {0}   |   Caller:  {1}   |   Bar Time:  {2}   |   Message:  {3}",
                Name,
                stackTrace.GetFrame(1).GetMethod().Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion

        #region ToolStrip Framework

        #region ToolStrip Initialization
        // ---------------------------- StartCalc Start ---------------------------- //

        private bool IsToolStripCreated;
        private void ToolStripStartCalc()
        {
            // place this method in startcalc.
            if (IsToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(ToolStripMain);
                IsToolStripCreated = true;
            }

            // reset toolstrip variables here
            IsDisplayText = false;
        }
        // ---------------------------- StartCalc End ---------------------------- //
        #endregion

        #region ToolStrip Main Function
        // ---------------------------- Main Start ---------------------------- //
        private ToolStripButton DisplayTexts;
        private void ToolStripCreate(ToolStrip tb)
        {
            // Create Toolstrip objects here and in order of display.

            ToolStripLabel label1 = new ToolStripLabel();
            label1.Text = "Display Text";
            AddNewItemToToolStrip(tb, label1);

            DisplayTexts = new ToolStripButton();
            AddNewItemToToolStrip(tb, DisplayTexts);
        }

        private bool IsDisplayText;
        private void ToolStripMain(ToolStrip tb)
        {
            // main toolstrip adjustments first
            tb.AutoSize = false;
            tb.Height = 100;
            tb.TabIndex = 0;

            // initialize tool strip objects
            ToolStripCreate(tb);

            // Write functionality here.
            DisplayTexts.Text = "Show";
            DisplayTexts.BackColor = Color.LimeGreen;
            DisplayTexts.ForeColor = Color.White;
            DisplayTexts.Click += delegate
            {
                if (IsDisplayText == true)
                {
                    DisplayTexts.Text = "Show";
                    DisplayTexts.BackColor = Color.LimeGreen;
                    DisplayTexts.ForeColor = Color.White;
                    IsDisplayText = false;
                }

                // if false, hide text
                else
                {
                    DisplayTexts.Text = "Hide";
                    DisplayTexts.BackColor = Color.Red;
                    DisplayTexts.ForeColor = Color.White;
                    IsDisplayText = true;
                }
            };
        }
        // ---------------------------- Main End ---------------------------- //
        #endregion

        #region ToolStrip Dispose
        // ---------------------------- No changes required to be made here ---------------------------- //
        private void AddNewItemToToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }
        protected override void Destroy()
        {
            // place this method in Destroy()
            if (IsToolStripCreated)
                ChartToolBar.AccessToolBar(RemoveToolBar);
        }
        private void RemoveToolBar(ToolStrip tb)
        {
            List<ToolStripItem> items = new List<ToolStripItem>();
            foreach (ToolStripItem item in tb.Items)
            {
                if (ReferenceEquals(this, item.Tag)) items.Add(item);
            }

            // Clear all tool bar items
            foreach (var item in items) tb.Items.Remove(item);
        }
        #endregion

        #endregion
    }
}

#endregion

#region Function Notes

/* / -------------------------- Function Notes  -------------------------- /
 * 
 *                  VariableSeries<T> as a Property
 *  You can get and set variable series as a Function class Property
 *  Which you can get from the caller. However, you need to use .Call()
 *  To invoke the Function class to calculate before getting the return
 *  value. Everytime, when you need to reference previous bar's calculations,
 *  it is best practice to use VariableSeries<T>
 *  
 *                  List<T> Insert(index, data) Method
 *  Inserting is adding. Adding at a certain index, rather than replacing.
 *  
 *                  Function return value property 
 *  Getting Function Property Value akins to calling CalcBar() in the function.
 *  So if you call value 3 times, it will call CalcBar three times. 
 *  In the case of hull when you are doing inserting. It will insert the same price
 *  bar three times. To remedy this problem, in the calling method, 
 *  save the return value to a VariableSeries. 
 *  
 *                  Debug Print inside function works
 *  You can debug within the function itself. You can use the default print method.
 *  
 *                  Naming Conventions for Multichart Projects
 *   _SNi : Indicator     _SNs : Strategy         _SNf : Function
 *   Create a class for each. Indicator for plotting. Strategy for sending orders.
 *   Create a class framework for functions. Use region and versions to keep track
 *   of different version. V10, the 1 is changed if the outcome is different. 0, refactor.
 *   For Client projects, put the functions in the same class as the indicator or strategy.
 *   
 *                  File/Project Management
 *   Keep all classes and resources required in one file. A single file is a project.
 *   Store all powerlanguage and custom functions under the same file.
 *   Even if i reuse the file, rename it to follow file name. One file - One project.
 *   For my own files. Keep all the functions here. Every client will have their own Function Lib.
 *   Function lib is where the key calculation logic are stored. Indicator functions and strategy
 *   functions should be separated. Practice and improve my workflow for keeping version history.
 *   Latest version will not have vX.X it will be the original name.
 *   First digit, for new features. Second digit, for refactorization or debugging.
 *   
 *                  Understanding Function Call()
 *   The first time a function is called is Bars.Current > 1 or 2.
 *   The function Call() is implicitly called once before an indicator/strategy CalcBar() is called.
 *   Note: Current Bar is the first bar that Calcbar is called. Bars before current bar are MaxBarBack Bars.
 *   These bars are needed to needed to make the first calculation of Calcbar().
 *   Every single time Function.Value is called. a return CalcBar() is called on the function. BECAREFUL.
 *   If Value is called, then implicit Call() will NOT be called.
 *   Note: double Call(int barsago) == x[barsago].Value They are the same thing.
 *   
 *                  Debuging Functions.
 *   You don't have to remove and re-insert an indicator, to see changes made to the function
 *   if you are just printing the results to Output window. This is the best way to debug.
 *   
 *                  Open - Close Principle for design patterns.
 *   Open for modification, but closed for making changes within the class.
 *   When designing classes. I have to bear in mind the open close principle.
 *   
 *                  ISeries<double> 
 *   is an interface that any member who implements it will have an indexer and value property.
 *   the indexer[int barsago], gives you access to the value of which ever bars back using the 
 *   value property. Variable series implements this interface, it adds set to value.
 *   ISeries<double> is pegged to bar number, you can call it many times, you will get the same value.
 *   So long as you are calling it in the same BarNumber. Unlike Count, which increments every time,
 *   CalcBar() is called. classes that implements ISeries<double> works like a list. 
 *   It is passing the reference the reference of the list by property accessor.
 *   When dealing with price data, best is to use variable series, especially if you are working with
 *   non bar close data.
 *   
 *   Difference between FunctionSimple vs FunctionSeries
 *   There is one noticable difference. Is that FunctionSeries will implicityly Call().
 *   While FunctionSimple will not explicitly Call(). This becomes apparent when you are not using 
 *   the return value from CalcBar, rather when you are getting values from the property.
 *   Another key to FunctionSeries is that it remembers its state. So, you can use This[1] to retrive,
 *   reprevious CalcBar Value.
 *   
 *   Additional Notes on VariableSeries<double>
 *   VariableSeries<> is linked and synced with the current bar.
 *   If values will persist from bar to bar, unless it is updated. In other words, at the end of each
 *   closing tick, Variable Series will take the prev calculated value, or the updated new value.
 *   It is like a List which add or inserts once every end of bar implicityly.
 *
 */
#endregion


