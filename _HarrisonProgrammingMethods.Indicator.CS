/*
* Confidential of Harrison Programming
* Licensed to Clients of Harrison Programming
* NOT for public distribution

_HarrisonProgrammingMethods
* Contains
	Extension methods
	Instance method
	Static methods

* Using indicator _HarrisonProgrammingMethods to carry methods/functions
  instead of using function (for exporting Properly)

* Notation note
    - In code Functions, start with HP_ (Separate from _HP_ of class function)
      end with _f (to indicate function)
    - In separate function code, start with _HP, end with _f

* Extension methods note:
    - Extension methods may not show up in VS and in PL.Net editor initially after created 
      but will show up eventually in VS, thought may not in PL.Net editor
    - There is no need to specify class HP_ExtensionMethods_Class when using extension method

* Possible Issues

    - May not access bars info in the functions
	- May not access these functions from a strategy
	
*/

using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(false)]
    // inheritage IndicatorObject class
    public class _HarrisonProgrammingMethods : IndicatorObject
    {
        public _HarrisonProgrammingMethods(object _ctx) : base(_ctx)
        {
            // define input default values
        }

        private IPlotObject test1Plot;

		///  Create instances of objects
		protected override void Create()
		{
			// create variable objects, function objects, plot objects etc.
			// In Create() method, create plot before using it
			test1Plot = AddPlot(new PlotAttributes("Test1", EPlotShapes.Line, Color.Red));
		}
		
		/// Setting fixed properties of objects, e.g. MALength
		protected override void StartCalc()
		{
		}
		
		protected override void CalcBar()
		{
            // indicator logic 

            // for testing functions
            double valueToPlot = 1.0;

			test1Plot.Set(valueToPlot);
		}

    }
}

//3 Use namespace PowerLanguage without subnamespace function namespace
namespace PowerLanguage
{
    #region newRegionName001I
    //=====================================================================
    //=====================================================================
    #endregion newRegionName001I

    #region HP_ExtensionMethods
    //=====================================================================
    // Extension Methods
    // Note within namepspace PowerLanguage

    public static class HP_ExtensionMethods
    {
        // OK to use long name HP_ExtensionMethods because we don't need 
        // to type extension methods' class name

        // For EM (Extension Methods), use function name convention: HP_FunctionName_f

        #region newRegionName001
        //---------------------------------------------------------------------
        //---------------------------------------------------------------------
        #endregion newRegionName001


        #region HP_ExtMethodTest001_f
        //---------------------------------------------------------------------
        public static double HP_ExtMethodTest001_f(this ISeries<Double> Price, int Length)
        {
            // Test1, simply = Price
            double fRet = Price[0];

            return fRet;
        }
        //---------------------------------------------------------------------
        #endregion HP_ExtMethodTest001_f

        #region HP_BarYYYYMMDD_HHMM_f
        //---------------------------------------------------------------------
        public static double HP_BarYYYYMMDD_HHMM_f(this ISeries<DateTime> Time, int barsBack)
        {
            // return YYYYMMDD.HHMM as double as bar identifier. previously, know as BDT
            // potential use to specify some Date/Time input as single value, e.g.
            // Test_LE_BDT = 20180515.1230
            return (0.0001 * Convert.ToDouble(Time[barsBack].ToString("yyyyMMddHHmm")));
        }
        //---------------------------------------------------------------------
        #endregion HP_BarYYYYMMDD_HHMM_f

        #region HP_BarMinFrmMidNight_f
        //---------------------------------------------------------------------
        public static double HP_BarMinFrmMidNight_f(this ISeries<DateTime> Time, int barsBack)
        {
            return (60 * Time[barsBack].Hour + Time[barsBack].Minute);
        }
        //---------------------------------------------------------------------
        #endregion HP_BarMinFrmMidNight_f

        #region HP_HMA_f
        /*
        //--------------------------------------------------------------------
        // Created 2017, Updated, 20180110
        // Status: NOT Feasible\NOT Completed\NOT Tested
        // HMA may not be feasible to be put into an EM, 20180110
        //
        //3 static: Handling of Data2?
        public static double HP_HMA_f(this ISeries<Double> Price, int Length)
        {
            bool DoneOnce = false;
            /// VariableSeries<double> vWMA1, vWMA2, vWMA3;
            int len2Use, halvedLength, sqrRootLength;
            double sqLength, fRet;

            fRet = 0.0;
            if (!DoneOnce)
            {
                DoneOnce = true;

                // make sure minimum Length used is 1 to prevent possible 1/0 error
                len2Use = Length;
                if (Length < 1)
                    len2Use = 1;

                // Calculate ONCE for faster computation
                if ((Math.Ceiling((double)(len2Use / 2)) - (len2Use / 2)) <= 0.5)
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

                //3 ?? How to use VariableSeries in static function
                //3 VariableSeries<double> vWMA1;
                //3 "this" is not valid in a static property
                //3 vWMA1 = new VariableSeries<double>(this);
                //3 Could not create VariableSeries without this
            }


            //3 HOw to do once?
            //3 The name 'Bars' does not exist in the current content
            //3 ?? How to gain access to Bars info in static function
            //3 
            //3 if (Bars.CurrentBar > 1)
            if (DoneOnce)
            {

                vWMA1.Value = 2.0 * PublicFunctions.WAverageCustom(Price, halvedLength);

                vWMA2.Value = Price.WAverageCustom(len2Use);

                vWMA3.Value = vWMA1.Value - vWMA2.Value;

                Ret = vWMA3.WAverageCustom(sqrRootLength);
                PublicFunctions.WAverageCustom

                //  fRet = PublicFunctions.WAverageCustom(2.0 * (PublicFunctions.WAverageCustom(Price, halvedLength)) - PublicFunctions.WAverageCustom(Price, len2Use), sqrRootLength);
            }

            return fRet;
        }
        //---------------------------------------------------------------------
        */
        #endregion HP_HMA_f


        #region HP_ExtMethodTest999_f
        //---------------------------------------------------------------------
        public static double HP_ExtMethodTest999_f(this ISeries<Double> Price, int Length)
        {
            // Test1, simply = Price
            double fRet = Price[0];

            return fRet;
        }
        //---------------------------------------------------------------------
        #endregion HP_ExtMethodTest999_f

    }
    //=====================================================================
    #endregion HP_ExtensionMethods


    // >> Test Instace Method
    #region HP_IM_InstnaceMethods
    // Naming convention HP_FunctionName_f
    //=====================================================================

    #region HP_IM001Int_f
    //---------------------------------------------------------------------
    public sealed class HP_IM001Int_f : FunctionSimple<int>
    {
        // Return CurrentSession number
        // Limitation: For market with 5 sessions/week only
        // Must check for > 0 for using the result
        // Feasible/Completed/Validated 20180111
        // 10
        public HP_IM001Int_f(CStudyControl _master) : base(_master) { }
        public HP_IM001Int_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        private VariableSeries<double> vVar1;
        private int fRet;

        protected override void Create()
        {
            vVar1 = new VariableSeries<double>(this);
        }

        protected override void StartCalc()
        {
        }

        protected override int CalcBar()
        {
            return (fRet);
        }
    }
    //---------------------------------------------------------------------
    #endregion HP_IM001Int_f

    #region HP_IM001Dbl_f
    //---------------------------------------------------------------------
    public sealed class HP_IM001Dbl_f : FunctionSimple<double>
    {
        // Return CurrentSession number
        // Limitation: For market with 5 sessions/week only
        // Must check for > 0 for using the result
        // Feasible/Completed/Validated 20180111
        // 10
        public HP_IM001Dbl_f(CStudyControl _master) : base(_master) { }
        public HP_IM001Dbl_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        private VariableSeries<double> vVar1;
        private double fRet;

        protected override void Create()
        {
            vVar1 = new VariableSeries<double>(this);
        }

        protected override void StartCalc()
        {
        }

        protected override double CalcBar()
        {
            return (fRet);
        }
    }
    //---------------------------------------------------------------------
    #endregion HP_IM001Dlb_f

    #region HP_HMA_f
    //---------------------------------------------------------------------
    public sealed class HP_HMA_f : FunctionSimple<double>
    {
        public HP_HMA_f(CStudyControl _master) : base(_master) { }
        public HP_HMA_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        public ISeries<double> Price { private get; set; }
        public int Length { private get; set; }

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
            len2Use = Length;
            if (Length < 1)
                len2Use = 1;

            // Calculate ONCE for faster computation
            if ((Math.Ceiling((double)(len2Use / 2)) - (len2Use / 2)) <= 0.5)
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
            vWMA1.Value = 2.0 * (Price.WAverageCustom(halvedLength));

            vWMA2.Value = Price.WAverageCustom(len2Use);

            vWMA3.Value = vWMA1.Value - vWMA2.Value;

            double fRet = vWMA3.WAverageCustom(sqrRootLength);

            return (fRet);
        }
    }
    //---------------------------------------------------------------------
    #endregion HMA_

    #region HP_BarMntFromSessOpen_f
    //---------------------------------------------------------------------
    public sealed class HP_BarMntFromSessOpen_f : FunctionSimple<int>
    {
        // Return CurrentSession number
        // Limitation: For market with 5 sessions/week only
        // Must check for > 0 for using the result
        // Feasible/Completed/Validated 20180111
        // 10
        public HP_BarMntFromSessOpen_f(CStudyControl _master) : base(_master) { }
        public HP_BarMntFromSessOpen_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        const int MFMNAt2400 = 1440;
        private int sess1StartMFMN, sess1EndMFMN;

        private int MFMN, MFSO, fRet;

        protected override void Create()
        {
        }

        protected override void StartCalc()
        {
            sess1StartMFMN = (int)(Bars.Sessions[0].StartTime.TotalMinutes);
            sess1EndMFMN = (int)(Bars.Sessions[0].EndTime.TotalMinutes);
        }

        protected override int CalcBar()
        {
            // Minutes From MidNight
            MFMN = 60 * Bars.Time[0].Hour + Bars.Time[0].Minute;

            if (MFMN >= sess1StartMFMN && MFMN < MFMNAt2400)
            {
                // Minutes From Session Open
                MFSO = MFMN - sess1StartMFMN;
            }
            else if (MFMN == 0)
            {
                // which is 2400
                MFSO = MFMNAt2400 - sess1StartMFMN;
            }
            else if (MFMN > 0 && MFMN <= sess1EndMFMN)
            {
                MFSO = MFMNAt2400 - sess1StartMFMN + MFMN;
            }

            // separate var as not all path return MFSO
            fRet = MFSO;

            return (fRet);
        }
    }
    //---------------------------------------------------------------------
    #endregion HP_BarMntFromSessOpen_f

    #region HP_CurrentSession_f
    //---------------------------------------------------------------------
    public sealed class HP_CurrentSession_f : FunctionSimple<int>
    {
        // Return CurrentSession number
        // Limitation: For market with 5 sessions/week only
        // Must check for > 0 for using the result
        // Feasible/Completed/Validated 20180111
        // 10
        public HP_CurrentSession_f(CStudyControl _master) : base(_master) { }
        public HP_CurrentSession_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        private VariableSeries<bool> LastBarInSess;
        DayOfWeek dow;
        private int idow, fRet;

        protected override void Create()
        {
            LastBarInSess = new VariableSeries<bool>(this);
        }

        protected override void StartCalc()
        {
        }

        protected override int CalcBar()
        {
            LastBarInSess.Value = Bars.LastBarInSession;

            if (LastBarInSess[1] == true && LastBarInSess[0] == false)
            {
                dow = Bars.Time[0].DayOfWeek;
                // Note idow = 0 for Sunday, 1 for Monday, 5 for Fri
                idow = (int)dow;
                //3 Output.WriteLine("dow {0}, idow {1} ", dow, idow);
                fRet = 0;
                // First bar in a new Session
                if (dow >= DayOfWeek.Monday && dow <= DayOfWeek.Friday)
                {
                    if (dow != Bars.Time[1].DayOfWeek)
                    {
                        // like day session market,stock
                        fRet = idow;
                    }
                    else
                    {
                        // like 24 hours market, session start in evening
                        fRet = idow + 1;
                    }
                }
                else
                {
                    // Sunday, Session 1 start
                    fRet = 1;
                }
            }

            return (fRet);
        }
    }
    //---------------------------------------------------------------------
    #endregion HP_CurrentSession_f

    #region HP_IM999Int_f
    //---------------------------------------------------------------------
    public sealed class HP_IM999Int_f : FunctionSimple<int>
    {
        // Return CurrentSession number
        // Limitation: For market with 5 sessions/week only
        // Must check for > 0 for using the result
        // Feasible/Completed/Validated 20180111
        // 10
        public HP_IM999Int_f(CStudyControl _master) : base(_master) { }
        public HP_IM999Int_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        private VariableSeries<double> vVar1;
        private int fRet;

        protected override void Create()
        {
            vVar1 = new VariableSeries<double>(this);
        }

        protected override void StartCalc()
        {
        }

        protected override int CalcBar()
        {
            return (fRet);
        }
    }
    //---------------------------------------------------------------------
    #endregion HP_IM999Int_f

    #region HP_IM999Dbl_f
    //---------------------------------------------------------------------
    public sealed class HP_IM999Dbl_f : FunctionSimple<double>
    {
        // Return CurrentSession number
        // Limitation: For market with 5 sessions/week only
        // Must check for > 0 for using the result
        // Feasible/Completed/Validated 20180111
        // 10
        public HP_IM999Dbl_f(CStudyControl _master) : base(_master) { }
        public HP_IM999Dbl_f(CStudyControl _master, int _ds) : base(_master, _ds) { }

        private VariableSeries<double> vVar1;
        private double fRet;

        protected override void Create()
        {
            vVar1 = new VariableSeries<double>(this);
        }

        protected override void StartCalc()
        {
        }

        protected override double CalcBar()
        {
            return (fRet);
        }
    }
    //---------------------------------------------------------------------
    #endregion HP_IM999Dlb_f

    //=====================================================================
    #endregion HP_IM_InstnaceMethods


    #region HPSM (Static Methods)
    //=====================================================================
    // Static Methods
    //
    // Naming convention FunctionName_f
    public static class HPSM
    {
        // Use short name HPSM to make it easier to type,
        // but still HPSM instead of HP to make it longer to separating names
        // THis is how PublicFunctions are created

        // Naming convention FunctionName_f
        // Usage: HPSM.FunctionName_f

        #region newRegionNameHPSM001
        //---------------------------------------------------------------------
        public static double StaticMethodTest001_f()
        {
            return 1.0;
        }

        //---------------------------------------------------------------------
        #endregion newRegionNameHPSM001

        #region NumDecimals_f
        //---------------------------------------------------------------------
        public static int NumDecimals_f(double ptsPerTick)
        {
            //3 MC.Net version require input ptsPerTick, while EL version does not
            double smallNum = 0.000000001;
            int numDec;
            double tmp1Dbl;
           
            // Max value
            numDec = 8;

            for (int cntDown = 8; cntDown >= 0; cntDown--)
            {            
                tmp1Dbl = ptsPerTick * Math.Pow(10, cntDown);        
                if ( (tmp1Dbl - Math.Truncate(tmp1Dbl)) < smallNum)
                    numDec = cntDown;
            }

            return numDec;
        }
        //---------------------------------------------------------------------
        #endregion NumDecimals_f

        #region RoundPriceTo_f
        //---------------------------------------------------------------------
        public static double RoundPriceTo_f(double price6, double roundPriceTo6)
        {
            return (roundPriceTo6 * Math.Round(price6 / roundPriceTo6, 0));
        }
        //---------------------------------------------------------------------
        #endregion RoundPriceTo_f

        #region TFTo10_f
        //---------------------------------------------------------------------
        public static int TFTo10_f(bool condTF)
        {
            int cond12;

            if (condTF)
            {
                cond12 = 1;
            }
            else
            {
                cond12 = 0;
            }

            return cond12;
        }
        //---------------------------------------------------------------------
        #endregion TFTo10_f

        #region newRegionNameHPSM999
        //---------------------------------------------------------------------
        public static double StaticMethodTest999_f()
        {
            return 1.0;
        }
        //---------------------------------------------------------------------
        #endregion newRegionNameHPSM999
    }

    //=====================================================================
    #endregion HPSM (Static Methods)
}
//3 EO: namespace PowerLanguage