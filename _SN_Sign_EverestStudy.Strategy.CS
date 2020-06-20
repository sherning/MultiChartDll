/*
_Everest_205     20180829    2030
* Add Yellow plot for stop
* Round to ForexStepSize

_Everest_205     20180822    1600
* Review

_Everest_204     20180806    1600
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PowerLanguage.Function;
using PowerLanguage.TradeManager;
using ATCenterProxy.interop;


namespace PowerLanguage.Strategy
{
    // [IOGMode(IOGMode.Enabled)]
    [IOGMode(IOGMode.Disabled)]

    public class _Everest_205 : SignalObject
    {
        #region inputs

        // PasalCase for Inputs

        [Input]
        public string ToolBarInputs { get; set; }
        [Input]
        public int LongTradesSwitch { get; set; }
        [Input]
        public int ShortTradesSwitch { get; set; }
        [Input]
        public int TradeStartTime1 { get; set; }
        [Input]
        public int TradeEndTime1 { get; set; }
        [Input]
        public int TradeStartTime2 { get; set; }
        [Input]
        public int TradeEndTime2 { get; set; }
        [Input]
        public double RiskPerTrade { get; set; }
        [Input]
        public double PipValueInUSD { get; set; }

        [Input]
        public string FunctionInputs { get; set; }
        /*
        [Input]
        public int Avg_Day_Mode { get; set; }
        */
        public int ATR_Speed { get; set; }
        [Input]
        public int ATR_Length { get; set; }
        [Input]
        public int PA_Speed { get; set; }
        [Input]
        public int PA_Length { get; set; }
        [Input]
        public int TickBuffer { get; set; }
        [Input]
        public int PriceInput { get; set; }
        [Input]
        public double RangeMultiple { get; set; }
        [Input]
        public double DistanceMultiple { get; set; }
        [Input]
        public double NewStepMultiple { get; set; }
        [Input]
        public int PivotLength { get; set; }

        [Input]
        public string StrategyControlInputs { get; set; }
        [Input]
        public double PivotDifference { get; set; }
        [Input]
        public int EODExitTime { get; set; }
        [Input]
        public int EOWExitTime { get; set; }

        [Input]
        public string RiskManagementInputs { get; set; }
        [Input]
        public int APModeInput { get; set; }
        [Input]
        public int FixContractsInput { get; set; }
        [Input]
        public int AccountSizeInput { get; set; }
        [Input]
        public int ForexStepSize { get; set; }
        [Input]
        public int MinContractsSize { get; set; }
        [Input]
        public int MaxContractsSize { get; set; }

        [Input]
        public string ExitStrategyInputs { get; set; }
        [Input]
        public int StopLossPivotLength { get; set; }
        [Input]
        public int ChannelLength { get; set; }

        // for next 4 inputs, use double instead of int, to allow specify 0.1 Pip
        [Input]
        public double StopLossBuffer { get; set; }
        [Input]
        public double MinRiskRequired { get; set; }
        [Input]
        public double OptimalRisk { get; set; }
        [Input]
        public double MaxRiskAllowed { get; set; }

        [Input]
        public double ProfitTarget_1 { get; set; }
        [Input]
        public double ProfitTarget_2 { get; set; }

        [Input]
        public double AccFactor { get; set; }
        [Input]
        public double AccLimit { get; set; }

        /*
                [Input]
                public int LiveTradeSwitch { get; set; }
                [Input]
                public string IB_AccountName { get; set; }

                [Input]
                public string ToolBarSettings { get; set; }
                [Input]
                public int TradingToolBarOn { get; set; }
                [Input]
                public int TTB_EntrySwitch { get; set; }
                [Input]
                public double TTB_Risk { get; set; }
                [Input]
                public int TTB_TradeTimeOn { get; set; }
                [Input]
                public int TTB_TimeExitSwitch { get; set; }

                [Input]
                public bool ConfirmationRequired { get; set; }
                [Input]
                public int ConfirmTimeOutSeconds { get; set; }

                [Input]
                public Color LE_Color { get; set; }
                [Input]
                public Color LX_Color { get; set; }
                [Input]
                public Color SE_Color { get; set; }
                [Input]
                public Color SX_Color { get; set; }
                [Input]
                public Color FlatColor { get; set; }
                [Input]
                public Color PosMatchColor { get; set; }
                [Input]
                public Color PosMisMatchColor { get; set; }
        */

        public _Everest_205(object _ctx) : base(_ctx)
        {

            ToolBarInputs = "ToolBar Inputs";
            LongTradesSwitch = 1;
            ShortTradesSwitch = 1;
            //Trade_Time_On = 1: trading tool bar to control on/off
            // Negative number on TradeStartTime1 or TradeEndTime1 to turn it off
            TradeStartTime1 = 0;
            TradeEndTime1 = 2400;
            // Negative number on TradeStartTime1 or TradeEndTime1 to turn it off
            TradeStartTime2 = -1;
            TradeEndTime2 = -1;
            // Keep RiskPerTrade here with ToolBar Inputs for future use with ToolBar
            // in percent, 0.3 means 0.3 percent
            RiskPerTrade = 0.3;
            // Pip Value in USD, P/L per Pip in a 100,000 trade
            PipValueInUSD = 10.0;

            FunctionInputs = "Function Inputs";
            ATR_Length = 34;
            ATR_Speed = 120;
            // Note: PAC ATR_Length = Mode_2_ATR_Length_Input
            PA_Speed = 120;
            PA_Length = 4;
            TickBuffer = 1;
            PriceInput = 1;
            RangeMultiple = 1.0;
            DistanceMultiple = 0.1;
            NewStepMultiple = 1.0;
            PivotLength = 4;

            StrategyControlInputs = "Strategy Control Inputs";
            // PivotDifference values are in PIPs
            PivotDifference = 1.0;
            // End Of Dau Exit Time, Negative number to turn it off
            EODExitTime = -1;
            // End Of Week Exit Time, Negative number to turn it off
            EOWExitTime = -1;

            RiskManagementInputs = "Risk Management Inputs";
            APModeInput = 2;
            // for APModeInput = 1
            FixContractsInput = 100000;
            // for APModeInput = 2
            // if no Real_Balance data, use FixContractsInput
            AccountSizeInput = 100000;
            ForexStepSize = 20000;
            MinContractsSize = 20000;
            MaxContractsSize = 10000000;

            ExitStrategyInputs = "Exit Strategy Inputs";
            StopLossPivotLength = 4;
            ChannelLength = 10;

            // StopLossBuffer(1) // in PIPs
            StopLossBuffer = 1.0;
            // MinRiskRequired(5) //in PIPs
            MinRiskRequired = 5.0;
            // OptimalRisk(10)  // in PIPs
            OptimalRisk = 10.0;
            // MaxRiskAllowed(15) // in PIPs
            MaxRiskAllowed = 15.0;

            // { Profit Target 1 and 2 same number for single exit}
            // in Risk Reward, always 50 percent
            ProfitTarget_1 = 3.0;
            //  in Risk Reward, takes remaining (if 1 is On, 50 percent, if 1 is Off, 2 take 100 percent)
            ProfitTarget_2 = 10.0;
            // negative number to turn off
            // If ProfitTarget_2 is off, just let trailing stop take

            // Parabolic
            AccFactor = 0.01;
            AccLimit = 0.18;
            /*
            LiveTradeSwitch = 0;
            IB_AccountName = "";

            ToolBarSettings = "Tool Bar Settings";
            // set to 0 for optimization
            TradingToolBarOn = 0;

            // TTB = Trading ToolBar (default settings)
            // TTB_EntrySwitch: Entry Switch, 0 Off, 1 LongOnly, 2 ShortOnly, 3 Long and Short
            // This version: Long Only, Short not done
            TTB_EntrySwitch = 3;
            // TTB_Risk: 0.5 to 2.0, typically
            TTB_Risk = 1.0;
            // TTB_TradeTime_On: 1/0 for On/Off, to use TradeStartTime1 and TradeEndTime1
            TTB_TradeTimeOn = 1;
            // TTB_TimeExitSwitch 0 Off, 1 End of Day Exit (determined by EODExitTime)
            TTB_TimeExitSwitch = 1;

            // more ToolBar_Settings
            ConfirmationRequired = true;
            ConfirmTimeOutSeconds = 5;
            LE_Color = Color.LimeGreen;
            LX_Color = Color.Green;
            SE_Color = Color.Pink;
            SX_Color = Color.Red;
            FlatColor = Color.LightGray;
            PosMatchColor = Color.LightGreen;
            PosMisMatchColor = Color.Red;
            */

        }
        // Set these weekly or monthly, can program to get these automatically as needed
        private double USDCAD_Price = 1.27;
        private double USDCHF_Price = 1.00;
        private double USDJPY_Price = 113.73;
        private int VolVarMode = 2;
        private int AvgDayLength = 2;
        private int Mode_3_DaysBack = 1;
        private int Mode_4_WeeksBack = 1;

        private int LiveTradeSwitch = 0;
        private string IB_AccountName = "";

        private string ToolBarSettings = "Tool Bar Settings";
        // set to 0 for optimization
        private int TradingToolBarOn = 0;

        // TTB = Trading ToolBar (default settings)
        // TTB_EntrySwitch: Entry Switch, 0 Off, 1 LongOnly, 2 ShortOnly, 3 Long and Short
        // This version: Long Only, Short not done
        private int TTB_EntrySwitch = 3;
        // TTB_Risk: 0.5 to 2.0, typically
        private double TTB_Risk = 1.0;
        // TTB_TradeTime_On: 1/0 for On/Off, to use TradeStartTime1 and TradeEndTime1
        private int TTB_TradeTimeOn = 1;
        // TTB_TimeExitSwitch 0 Off, 1 End of Day Exit (determined by EODExitTime)
        private int TTB_TimeExitSwitch = 1;

        // more ToolBar_Settings
        private bool ConfirmationRequired = true;
        private int ConfirmTimeOutSeconds = 5;
        private Color LE_Color = Color.LimeGreen;
        private Color LX_Color = Color.Green;
        private Color SE_Color = Color.Pink;
        private Color SX_Color = Color.Red;
        private Color FlatColor = Color.LightGray;
        private Color PosMatchColor = Color.LightGreen;
        private Color PosMisMatchColor = Color.Red;


        #endregion inputs

        #region variables
        // camelCase for variables, except for special notations and abbreviations
        private int historical_Swt = 1;
        private int Stp_Swt = 1;
        private int Tgt_Swt = 1;
        private int brokerSameAsDatafeed_Swt = 1;

        // 1/0 for Yes/No for Long trades
        // 1/0 for Yes/No for Short trades		
        private int longTrades = 1;
        private int shortTrades = 1;
        private int timeExitSwitch;

        private double dMinContractsSize, dMaxContractsSize;

        // private int sess1StartTimeEL, sess1EndTimeEL;
        private TimeSpan sess1StartTime, sess1EndTime;
        // Use VariableSeries for vBarDT vCurrSess, SMPmay, need to refer to [1] for these vars
        private VariableSeries<bool> lastBarInSess;
        private VariableSeries<double> vBarDT;
        private VariableSeries<int> vELTime;
        private HP_BarMntFromSessOpen_f mBarMFSO;
        private int barMFSO;
        private HP_CurrentSession_f mCurrSess;
        // Use vCurrSess or currSess depend on if we need to refer to [1]
        private VariableSeries<int> vCurrSess;
        private int currSess;
        private VariableSeries<EMarketPositionSide> SMP;

        private VariableSeries<bool> LE_Order;
        private VariableSeries<bool> SE_Order;
        private bool LX_Stp_Order, LX_Tgt_Order, LX_Mkt_Order;
        private bool SX_Stp_Order, SX_Tgt_Order, SX_Mkt_Order;

        private double LE_Pvt_P, LE_Lmt_P, LE_Ent_P, LX_Stp0_P, LX_Stp_P,
            LX_Tgt_P, LX_Tgt1_P, LX_Tgt2_P, LE_riskValue, LE_riskRewardLevel;
        private double SE_Pvt_P, SE_Lmt_P, SE_Ent_P, SX_Stp0_P, SX_Stp_P,
            SX_Tgt_P, SX_Tgt1_P, SX_Tgt2_P, SE_riskValue, SE_riskRewardLevel;

        private int LE_Ord_Qty, LX_Ord_Qty, LX_Tgt1_Ord_Qty, LX_Tgt2_Ord_Qty;
        private int SE_Ord_Qty, SX_Ord_Qty, SX_Tgt1_Ord_Qty, SX_Tgt2_Ord_Qty;

        private VariableObject<int> LE_BarsSince;
        private VariableObject<int> SE_BarsSince;
        private VariableObject<double> LE_HiCloseSinEnt, LE_HiHighSinEnt;
        private VariableSeries<double> LE_Hi_PAL_5_SinEnt;
        private VariableObject<int> LX_TrlStpActive;
        private VariableObject<double> SE_LoCloseSinEnt, SE_LoLowSinEnt;

        private IOrderMarket LE_Mkt_Ord, LX_Mkt_Ord;
        private IOrderMarket SE_Mkt_Ord, SX_Mkt_Ord;
        private IOrderPriced LE_Stp_Ord, LX_Stp_Ord, LX_Tgt_Ord, LX_Tgt1_Ord, LX_Tgt2_Ord;
        private IOrderPriced SE_Stp_Ord, SX_Stp_Ord, SX_Tgt_Ord, SX_Tgt1_Ord, SX_Tgt2_Ord;

        private int stratStartBN, stratStartBN_D2, stratStartBN_D3;
        private int numDec, vCategory;
        private volatile int APos, SPos;
        private int MinMovesPerPips;
        private double ptsPerPips, ptsPerTick, onePip, oneTick, BAS_Pts, roundPriceToPts;
        private double pivotDiffPts, stopLossBufferPts, minRiskRequiredPts, optimalRiskPts, maxRiskAllowedPts;
        private double stopPips, targetPips;
        private double StpPts, StpAmt, TgtPts, TgtAmt;
        private double LX_Stp_Pips, LX_Tgt_Pips, LX_StpPts, LX_TrlStpPts, LX_TgtPts;
        private double SX_Stp_Pips, SX_Tgt_Pips, SX_StpPts, SX_TrlStpPts, SX_TgtPts;
        private int posSize, E_Qty, X_Qty;

        private bool OnCloseTick, haveEnoughData, OKToEnter, LBOCDoneOnce;

        // Set default broker an datafeed
        private string brokerName = "Interactive Brokers";
        private string dataSource = "Interactive Brokers";
        private string SymbolName;
        private string SymbolExchange;
        private ESymbolCategory SymbolCategory;

        private IOrderMarket rLEm, rLXm;
        private IOrderPriced rLEl, rLEs, rLXl, rLXs;
        private double rLE_BDT1, rLE_P, rLX_BDT1, rLX_P;
        private int rLE_Qty;
        private string rLE_BDT1Key, rLE_PKey, rLX_BDT1Key, rLX_PKey;
        private IOrderMarket rSEm, rSXm;
        private IOrderPriced rSEl, rSEs, rSXl, rSXs;
        private double rSE_BDT1, rSE_P, rSX_BDT1, rSX_P;
        private int rSE_Qty;
        private string rSE_BDT1Key, rSE_PKey, rSX_BDT1Key, rSX_PKey;
        private bool GVOK;

        private MCSymbolInfo symInfo;
        private ITradingProfile tradingProfile;
        private string tradeProfileName = "Interactive Brokers";
        private string tradeSymbol;
        private bool connectedTF;


        private bool toolBarInited;
        private ToolStripLabel btnAutoStatus;
        private ToolStripButton btnLE, btnSE, btnLX, btnSX;
        private TrackBar tbarexitQty;
        private NumericUpDown numUpDn_LE_PosSiz, numUpDn_SE_PosSiz;

        private ToolStripTextBox txtbxQty;
        private ToolStripButton btnCnfm;
        private ToolStripButton btnTestModify;
        private ToolStripButton btnTestCancel;
        private ToolStripButton btnClosePos;
        private ToolStripButton btnQty, btnSPos, btnMatch, btnAPos;

        private System.Windows.Forms.Timer frmTmr1;

        private string LEStr = " LE ";
        private string SEStr = " SE ";
        private string LXStr = " LX ";
        private string SXStr = " SX ";

        private string cnfmStr = "Cnfm";
        private string matchStr = "Match";
        private string misMatchStr = "Mismatch";
        private string SPosFlatStr = "SPos Flat";
        private string APosFlatStr = "APos Flat";

        private string NAStr = "N/A";
        private DateTime LE_CnfmTimeOut;
        private DateTime SE_CnfmTimeOut;
        private DateTime LX_CnfmTimeOut;
        private DateTime SX_CnfmTimeOut;



        // in Test Mode: Will print and send additional data for plotting in separate indicators
        private bool inTestMode = true;
        private int test_E_Swt = 0;

        private double RunLoc1, RunLoc2, RunLoc3, RunLoc4, RunLoc5;

        // Strategy Specific: Variables

        // Set internally
        // PAC and PAL settings in sync = Same inputs;
        // Note: Make sure to match strategy and indicator input values
        private const int PA_PAC_ATR_Type = 2;
        // *** Critical: Make sure to match input values for PAL_SettingOriginalOrNew b/w indicators/strategy
        private const int PAL_SettingOriginalOrNew = 1;

        // Enum-like
        private const int APMode_FixContract = 1;
        private const int APMode_RealAcctBalance = 2;
        private const int APMode_AcctSizeInput = 3;

        // Vol_Var
        private _SN_VolatilityVariable101_f mVol_Var_5;
        private VariableSeries<double> Vol_Var_5;

        // Avg_Day (DayRangeAvgByDOW)
        // 1 for Monday, ..., 5 for Friday
        private _SN_DayRangeAvgByDOW100 wDayRangeAvg1;
        private _SN_DayRangeAvgByDOW100 wDayRangeAvg2;
        private _SN_DayRangeAvgByDOW100 wDayRangeAvg3;
        private _SN_DayRangeAvgByDOW100 wDayRangeAvg4;
        private _SN_DayRangeAvgByDOW100 wDayRangeAvg5;
        private VariableSeries<double> Avg_Day_Range;

        // PAL
        // PAL has only total of 8 TFs, there is no 10 Min
        private const int numOfTFs_PAL = 8;
        private List<VariableSeries<double>> vPALs = new List<VariableSeries<double>>();
        private VariableSeries<double> PAL_5;
        private VariableSeries<bool> isPALPivotLow;
        private VariableSeries<bool> isPALPvt_L;
        private VariableObject<double> upPALPivotValue, upPALPivotValue_1, upPALPivotValue_2;
        private VariableObject<double> upPALPivotPrice, upPALPivotPrice_1, upPALPivotPrice_2;
        private _SN_Trend_Tool_Price_Input trendToolPriceInput1;
        // Update to _SN_PAL120_f
        private _SN_PAL120_f mPAL;

        // PAC
        private const int numOfTFs_PAC = 9;
        private _SN_PAC_203_f PAC203;
        private VariableSeries<double> vPAC_LowerVal;
        private VariableSeries<double> vPAC_UpperVal;
        private VariableSeries<bool> vPAC_LowerAct;
        private VariableSeries<bool> vPAC_UpperAct;

        private VariableSeries<int> vhaveValidPAC_5;
        private VariableSeries<double> vPAC_5_LowerVal;
        private VariableSeries<double> vPAC_5_UpperVal;
        private VariableSeries<bool> vPAC_5_LowerAct;
        private VariableSeries<bool> vPAC_5_UpperAct;

        private VariableSeries<int> vhaveValidPAC_10;
        private VariableSeries<double> vPAC_10_LowerVal;
        private VariableSeries<double> vPAC_10_UpperVal;
        private VariableSeries<bool> vPAC_10_LowerAct;
        private VariableSeries<bool> vPAC_10_UpperAct;

        private VariableSeries<int> vhaveValidPAC_15;
        private VariableSeries<double> vPAC_15_LowerVal;
        private VariableSeries<double> vPAC_15_UpperVal;
        private VariableSeries<bool> vPAC_15_LowerAct;
        private VariableSeries<bool> vPAC_15_UpperAct;

        private VariableSeries<int> vhaveValidPAC_20;
        private VariableSeries<double> vPAC_20_LowerVal;
        private VariableSeries<double> vPAC_20_UpperVal;
        private VariableSeries<bool> vPAC_20_LowerAct;
        private VariableSeries<bool> vPAC_20_UpperAct;

        private VariableSeries<int> vhaveValidPAC_30;
        private VariableSeries<double> vPAC_30_LowerVal;
        private VariableSeries<double> vPAC_30_UpperVal;
        private VariableSeries<bool> vPAC_30_LowerAct;
        private VariableSeries<bool> vPAC_30_UpperAct;

        private VariableSeries<int> vhaveValidPAC_60;
        private VariableSeries<double> vPAC_60_LowerVal;
        private VariableSeries<double> vPAC_60_UpperVal;
        private VariableSeries<bool> vPAC_60_LowerAct;
        private VariableSeries<bool> vPAC_60_UpperAct;

        private VariableSeries<int> vhaveValidPAC_120;
        private VariableSeries<double> vPAC_120_LowerVal;
        private VariableSeries<double> vPAC_120_UpperVal;
        private VariableSeries<bool> vPAC_120_LowerAct;
        private VariableSeries<bool> vPAC_120_UpperAct;

        private VariableSeries<int> vhaveValidPAC_240;
        private VariableSeries<double> vPAC_240_LowerVal;
        private VariableSeries<double> vPAC_240_UpperVal;
        private VariableSeries<bool> vPAC_240_LowerAct;
        private VariableSeries<bool> vPAC_240_UpperAct;

        private VariableSeries<int> vhaveValidPAC_Day;
        private VariableSeries<double> vPAC_Day_LowerVal;
        private VariableSeries<double> vPAC_Day_UpperVal;
        private VariableSeries<bool> vPAC_Day_LowerAct;
        private VariableSeries<bool> vPAC_Day_UpperAct;

        // _L for Long, _S for _Short
        private VariableSeries<bool> LE_Specific, SE_Specific;
        private VariableSeries<bool> isTradeTime, isUpTrend, isRiskSmall_L, isRiskSmall_S;
        private VariableSeries<bool> isNewLowerStep, isNewHigherStep, isStepBig;

        private VariableSeries<double> vATR1_5, vATR2_5, vPAC_Mid, vPAC_SprHalf, vPAC_Displace;

        private VariableObject<int> LX_TrlStp_Active, parabolicCounter, gradientCounter;
        private VariableObject<double> accFactorValue, gradientValue;

        private VariableSeries<double> LX_TrlStp;

        private VariableSeries<double> SX_TrlStp_3;
        private VariableObject<bool> trailing_Stop_RRL_Act;
        private VariableSeries<double> L_TSL, S_TSL;
        private VariableObject<double> EP, L_SLP, S_SLP, L_PFT, S_PFT;
        private double L_RR, S_RR;
        // Gradient Bar Index after a jump
        private VariableObject<int> gradBarIdx;
        private double lengthXSpeed, pointBuffer, ATR2Use;

        private int auto_Profit_Target_On = 1;
        private int display_Profit_On = 1;
        private int display_Trailing_On = 1;
        private int display_InitialStop_On = 1;
        private int display_PAC_On = 1;
        private double LX_PACLow_P, SX_PACHigh_P;

        private double plotStopDot, plotStopDash, plotStopBefTrl;

        #endregion variables

        protected override void Create()
        {
            base.Create();

            lastBarInSess = new VariableSeries<bool>(this);
            vBarDT = new VariableSeries<double>(this);
            vELTime = new VariableSeries<int>(this);
            mBarMFSO = new HP_BarMntFromSessOpen_f(this);
            mCurrSess = new HP_CurrentSession_f(this);
            vCurrSess = new VariableSeries<int>(this);
            SMP = new VariableSeries<EMarketPositionSide>(this);

            LE_BarsSince = new VariableObject<int>(this);
            SE_BarsSince = new VariableObject<int>(this);

            LE_HiCloseSinEnt = new VariableObject<double>(this);
            LE_HiHighSinEnt = new VariableObject<double>(this);
            LE_Hi_PAL_5_SinEnt = new VariableSeries<double>(this);
            LX_TrlStpActive = new VariableObject<int>(this);

            SE_LoCloseSinEnt = new VariableObject<double>(this);
            SE_LoLowSinEnt = new VariableObject<double>(this);

            LE_Order = new VariableSeries<bool>(this);
            SE_Order = new VariableSeries<bool>(this);

            LE_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "LE_m", EOrderAction.Buy));

            SE_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "SE_m", EOrderAction.SellShort));

            LE_Stp_Ord = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                "LE_s", EOrderAction.Buy));

            SE_Stp_Ord = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                "SE_s", EOrderAction.SellShort));

            LX_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "LX_Mkt", EOrderAction.Sell));

            SX_Mkt_Ord = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                "SX_Mkt", EOrderAction.BuyToCover));

            LX_Stp_Ord = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                "LX_Stp", EOrderAction.Sell));

            LX_Tgt_Ord = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                "LX_Tgt", EOrderAction.Sell));

            LX_Tgt1_Ord = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                "LX_Tgt1", EOrderAction.Sell, OrderExit.Total));

            LX_Tgt2_Ord = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                "LX_Tgt2", EOrderAction.Sell, OrderExit.Total));

            SX_Stp_Ord = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                "SX_Stp", EOrderAction.BuyToCover));

            SX_Tgt_Ord = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                "SX_Tgt", EOrderAction.BuyToCover));

            SX_Tgt1_Ord = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                "SX_Tgt1", EOrderAction.BuyToCover, OrderExit.Total));

            SX_Tgt2_Ord = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                "SX_Tgt2", EOrderAction.BuyToCover, OrderExit.Total));

            rLEm = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                    "rLEm", EOrderAction.Buy));
            rLEl = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                    "rLEl", EOrderAction.Buy));
            rLEs = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                    "rLEs", EOrderAction.Buy));
            rLXm = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                    "rLXm", EOrderAction.Sell));
            rLXl = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                    "rLXl", EOrderAction.Sell));
            rLXs = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                    "rLXs", EOrderAction.Sell));

            rSEm = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                    "rSEm", EOrderAction.SellShort));
            rSEl = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                    "rSEl", EOrderAction.SellShort));
            rSEs = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                    "rSEs", EOrderAction.SellShort));
            rSXm = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.UserSpecified,
                    "rSXm", EOrderAction.BuyToCover));
            rSXl = OrderCreator.Limit(new SOrderParameters(Contracts.UserSpecified,
                    "rSXl", EOrderAction.BuyToCover));
            rSXs = OrderCreator.Stop(new SOrderParameters(Contracts.UserSpecified,
                    "rSXs", EOrderAction.BuyToCover));

            rLE_BDT1 = 0.0;
            rLE_P = 0.0;
            rLE_Qty = 0;
            rLX_BDT1 = 0.0;
            rLX_P = 0.0;

            rSE_BDT1 = 0.0;
            rSE_P = 0.0;
            rSE_Qty = 0;
            rSX_BDT1 = 0.0;
            rSX_P = 0.0;


            // 0 fo Off, 1 for Long only, 2 for Short only, 3 for Long and Short
            if (TTB_EntrySwitch == 1 || TTB_EntrySwitch == 3)
            {
                longTrades = 1;
            }
            else
            {
                longTrades = 0;
            }

            if (TTB_EntrySwitch == 2 || TTB_EntrySwitch == 3)
            {
                shortTrades = 1;
            }
            else
            {
                shortTrades = 0;
            }

            // take it from input, by-pass TTB_EntrySwitch
            longTrades = LongTradesSwitch;
            shortTrades = ShortTradesSwitch;

            timeExitSwitch = TTB_TimeExitSwitch;

            // Avg_Day (DayRangeAvgByDOW)
            wDayRangeAvg1 = new _SN_DayRangeAvgByDOW100(this);
            wDayRangeAvg2 = new _SN_DayRangeAvgByDOW100(this);
            wDayRangeAvg3 = new _SN_DayRangeAvgByDOW100(this);
            wDayRangeAvg4 = new _SN_DayRangeAvgByDOW100(this);
            wDayRangeAvg5 = new _SN_DayRangeAvgByDOW100(this);

            // Vol_Var
            mVol_Var_5 = new _SN_VolatilityVariable101_f(this);
            Vol_Var_5 = new VariableSeries<double>(this);
            Avg_Day_Range = new VariableSeries<double>(this);

            // PAL
            for (int j = 0; j < numOfTFs_PAL; j++)
            {
                vPALs.Add(new VariableSeries<double>(this));
            }
            PAL_5 = new VariableSeries<double>(this);
            isPALPivotLow = new VariableSeries<bool>(this);
            isPALPvt_L = new VariableSeries<bool>(this);
            upPALPivotValue = new VariableObject<double>(this);
            upPALPivotValue_1 = new VariableObject<double>(this);
            upPALPivotValue_2 = new VariableObject<double>(this);
            upPALPivotPrice = new VariableObject<double>(this);
            upPALPivotPrice_1 = new VariableObject<double>(this);
            upPALPivotPrice_2 = new VariableObject<double>(this);
            trendToolPriceInput1 = new _SN_Trend_Tool_Price_Input(this);
            mPAL = new _SN_PAL120_f(this);

            // PAC
            PAC203 = new _SN_PAC_203_f(this);

            vPAC_LowerVal = new VariableSeries<double>(this);
            vPAC_UpperVal = new VariableSeries<double>(this);
            vPAC_LowerAct = new VariableSeries<bool>(this);
            vPAC_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_5 = new VariableSeries<int>(this);
            vPAC_5_LowerVal = new VariableSeries<double>(this);
            vPAC_5_UpperVal = new VariableSeries<double>(this);
            vPAC_5_LowerAct = new VariableSeries<bool>(this);
            vPAC_5_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_10 = new VariableSeries<int>(this);
            vPAC_10_LowerVal = new VariableSeries<double>(this);
            vPAC_10_UpperVal = new VariableSeries<double>(this);
            vPAC_10_LowerAct = new VariableSeries<bool>(this);
            vPAC_10_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_15 = new VariableSeries<int>(this);
            vPAC_15_LowerVal = new VariableSeries<double>(this);
            vPAC_15_UpperVal = new VariableSeries<double>(this);
            vPAC_15_LowerAct = new VariableSeries<bool>(this);
            vPAC_15_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_20 = new VariableSeries<int>(this);
            vPAC_20_LowerVal = new VariableSeries<double>(this);
            vPAC_20_UpperVal = new VariableSeries<double>(this);
            vPAC_20_LowerAct = new VariableSeries<bool>(this);
            vPAC_20_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_30 = new VariableSeries<int>(this);
            vPAC_30_LowerVal = new VariableSeries<double>(this);
            vPAC_30_UpperVal = new VariableSeries<double>(this);
            vPAC_30_LowerAct = new VariableSeries<bool>(this);
            vPAC_30_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_60 = new VariableSeries<int>(this);
            vPAC_60_LowerVal = new VariableSeries<double>(this);
            vPAC_60_UpperVal = new VariableSeries<double>(this);
            vPAC_60_LowerAct = new VariableSeries<bool>(this);
            vPAC_60_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_120 = new VariableSeries<int>(this);
            vPAC_120_LowerVal = new VariableSeries<double>(this);
            vPAC_120_UpperVal = new VariableSeries<double>(this);
            vPAC_120_LowerAct = new VariableSeries<bool>(this);
            vPAC_120_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_240 = new VariableSeries<int>(this);
            vPAC_240_LowerVal = new VariableSeries<double>(this);
            vPAC_240_UpperVal = new VariableSeries<double>(this);
            vPAC_240_LowerAct = new VariableSeries<bool>(this);
            vPAC_240_UpperAct = new VariableSeries<bool>(this);

            vhaveValidPAC_Day = new VariableSeries<int>(this);
            vPAC_Day_LowerVal = new VariableSeries<double>(this);
            vPAC_Day_UpperVal = new VariableSeries<double>(this);
            vPAC_Day_LowerAct = new VariableSeries<bool>(this);
            vPAC_Day_UpperAct = new VariableSeries<bool>(this);

            LE_Specific = new VariableSeries<bool>(this);
            SE_Specific = new VariableSeries<bool>(this);

            isTradeTime = new VariableSeries<bool>(this);
            isUpTrend = new VariableSeries<bool>(this);
            isRiskSmall_L = new VariableSeries<bool>(this);
            isRiskSmall_S = new VariableSeries<bool>(this);
            isNewLowerStep = new VariableSeries<bool>(this);
            isNewHigherStep = new VariableSeries<bool>(this);
            isStepBig = new VariableSeries<bool>(this);

            vATR1_5 = new VariableSeries<double>(this);
            vATR2_5 = new VariableSeries<double>(this);

            vPAC_Mid = new VariableSeries<double>(this);
            vPAC_SprHalf = new VariableSeries<double>(this);

            LX_TrlStp_Active = new VariableObject<int>(this);
            parabolicCounter = new VariableObject<int>(this);
            gradientCounter = new VariableObject<int>(this);
            accFactorValue = new VariableObject<double>(this);
            gradientValue = new VariableObject<double>(this);

            LX_TrlStp = new VariableSeries<double>(this);

            SX_TrlStp_3 = new VariableSeries<double>(this);

            vPAC_Displace = new VariableSeries<double>(this);
            LE_HiCloseSinEnt = new VariableObject<double>(this);
            SE_LoCloseSinEnt = new VariableObject<double>(this);

            L_TSL = new VariableSeries<double>(this);
            S_TSL = new VariableSeries<double>(this);

            trailing_Stop_RRL_Act = new VariableObject<bool>(this);

            EP = new VariableObject<double>(this);
            L_SLP = new VariableObject<double>(this);
            S_SLP = new VariableObject<double>(this);
            L_PFT = new VariableObject<double>(this);
            S_PFT = new VariableObject<double>(this);

        }

        protected override void StartCalc()
        {

            var instrument1 = Bars;
            ExecInfo.MaxBarsBack = 300;

            stratStartBN = 50;
            stratStartBN_D2 = 50;
            stratStartBN_D3 = 50;

            dataSource = Bars.Info.DataFeed;
            if (brokerSameAsDatafeed_Swt == 1) brokerName = Bars.Info.DataFeed;

            SymbolName = Bars.Info.Name;
            SymbolExchange = Bars.Info.Exchange;
            SymbolCategory = Bars.Info.Category;

            vCategory = (int)Bars.Info.Category;
            Output.WriteLine("vCategory: {0}", Bars.Info.Category.ToString());
            Output.WriteLine("vCategory: {0}", vCategory);
            ptsPerTick = Bars.Info.MinMove / Bars.Info.PriceScale;
            Output.WriteLine("SymbolExchange: " + SymbolExchange);

            if (SymbolCategory == ESymbolCategory.Cash
                || SymbolCategory == ESymbolCategory.Forex
                || SymbolExchange == "IDEALPRO"
                || SymbolExchange == "FOREX")
            {
                ptsPerPips = 10.0 * ptsPerTick;
            }
            else
            {
                ptsPerPips = ptsPerTick;
            }

            MinMovesPerPips = 1;

            onePip = ptsPerPips;
            oneTick = ptsPerTick;
            roundPriceToPts = ptsPerTick;

            // StopPips = ;
            targetPips = 0;

            pivotDiffPts = PivotDifference * onePip;
            stopLossBufferPts = StopLossBuffer * onePip;
            minRiskRequiredPts = MinRiskRequired * onePip;
            optimalRiskPts = OptimalRisk * onePip;
            maxRiskAllowedPts = MaxRiskAllowed * onePip;

            // make sure >= 1 pips
            StpPts = Math.Max(1, stopPips) * ptsPerPips;
            LX_TgtPts = Math.Max(1, targetPips) * ptsPerPips;
            SX_TgtPts = Math.Max(1, targetPips) * ptsPerPips;

            if (TradingToolBarOn == 0 && toolBarInited)
            {
                RemoveToolBar_f();
            }
            if (TradingToolBarOn == 1 && !toolBarInited)
            {
                initializeToolBar();
                toolBarInited = true;
            }

            Output.WriteLine("======================================");
            Output.WriteLine("Profile defined by the strategy: {0}", Profile);

            Output.WriteLine("Bars.Info.ASymbolInfo2.SymbolCategory: {0}", Bars.Info.ASymbolInfo2.SymbolCategory.ToString());
            Output.WriteLine("Bars.Info.ASymbolInfo2.SymbolExchange: {0}", Bars.Info.ASymbolInfo2.SymbolExchange.ToString());

            symInfo = new MCSymbolInfo(Bars.Info.ASymbolInfo2, Bars.Info.DataFeed);
            tradeSymbol = symInfo.symbol.SymbolName;

            tradingProfile = null;

            if (LiveTradeSwitch == 1)
            {
                tradingProfile = getTradingProfile();
                tradingProfile.Connection += TradingProfile_Connection;
            }

            Output.WriteLine("Symbol: {0}", symInfo.symbol.SymbolName);

            PipCalculatorUSD_f();

            if (TradingToolBarOn == 1)
            {
                // Use Timer only when TradingToolBar_On is On
                frmTmr1 = new System.Windows.Forms.Timer();
                frmTmr1.Tick += new EventHandler(frmTmr1_Tick);
                frmTmr1.Interval = 2000;
                frmTmr1.Start();
            }
            LBOCDoneOnce = false;




            // Convert to double once for use later
            dMinContractsSize = (double)MinContractsSize;
            dMaxContractsSize = (double)MaxContractsSize;

            var BNList1 = new List<int> { stratStartBN };
            // This is the minimum stratStartBN
            stratStartBN = BNList1.Max();

            // Avg_Day (DayRangeAvgByDOW)
            wDayRangeAvg1.Length = AvgDayLength;
            wDayRangeAvg1.ThisDayOfWeek = DayOfWeek.Monday;
            wDayRangeAvg2.Length = AvgDayLength;
            wDayRangeAvg2.ThisDayOfWeek = DayOfWeek.Tuesday;
            wDayRangeAvg3.Length = AvgDayLength;
            wDayRangeAvg3.ThisDayOfWeek = DayOfWeek.Wednesday;
            wDayRangeAvg4.Length = AvgDayLength;
            wDayRangeAvg4.ThisDayOfWeek = DayOfWeek.Thursday;
            wDayRangeAvg5.Length = AvgDayLength;
            wDayRangeAvg5.ThisDayOfWeek = DayOfWeek.Friday;

            // Vol_Var
            mVol_Var_5.Vol_Var_Mode = this.VolVarMode;
            mVol_Var_5.ATR_Speed = this.ATR_Speed;
            mVol_Var_5.ATR_Length = this.ATR_Length;
            mVol_Var_5.Mode_3_Days_Back = this.Mode_3_DaysBack;
            mVol_Var_5.Mode_4_Weeks_Back = this.Mode_4_WeeksBack;

            // PAL
            mPAL.Speed_Input = this.PA_Speed;
            mPAL.Length_Input = this.PA_Length;
            mPAL.TickBuffer = this.TickBuffer;
            mPAL.Price_Input = this.PriceInput;
            mPAL.SettingOriginalOrNew = PAL_SettingOriginalOrNew;

            trendToolPriceInput1.Price_Input = this.PriceInput;

            // *** Critical: Make sure that the indicators input values match those of strategy input values ****
            // PAC_ATR_Type use PA_PAC_ATR_Type, regardless of what Vol_Var_Mode is
            PAC203.ATR_Type = PA_PAC_ATR_Type;
            // PAC_ATR_Length use Vol_Var_Mode's Mode_2_ATR_Length_Input
            PAC203.ATR_Length = ATR_Length;
            PAC203.Speed = PA_Speed;
            PAC203.Length = PA_Length;
            PAC203.TickBuffer = this.TickBuffer;
            PAC203.PriceInput = this.PriceInput;
            PAC203.RangeMultiple = this.RangeMultiple;
            PAC203.DistanceMultiple = this.DistanceMultiple;
            PAC203.NewStepMultiple = this.NewStepMultiple;
            PAC203.PivotLength = this.PivotLength;
            PAC203.MinMovesPerPips = this.MinMovesPerPips;
            Output.WriteLine("====== print to make sure PAC input match: Strategy =======");
            Output.WriteLine("PAC203.ATR_Type = " + PAC203.ATR_Type.ToString());
            Output.WriteLine("PAC203.ATR_Length = " + PAC203.ATR_Length.ToString());
            Output.WriteLine("PAC203.Speed = " + PAC203.Speed.ToString());
            Output.WriteLine("PAC203.Length = " + PAC203.Length.ToString());
            Output.WriteLine("PAC203.TickBuffer = " + PAC203.TickBuffer.ToString());
            Output.WriteLine("PAC203.PriceInput = " + PAC203.PriceInput.ToString());
            Output.WriteLine("PAC203.RangeMultiple = " + PAC203.RangeMultiple.ToString());
            Output.WriteLine("PAC203.DistanceMultiple = " + PAC203.DistanceMultiple.ToString());
            Output.WriteLine("PAC203.NewStepMultiple = " + PAC203.NewStepMultiple.ToString());
            Output.WriteLine("PAC203.PivotLength = " + PAC203.PivotLength.ToString());
            Output.WriteLine("PAC203.MinMovesPerPips = " + PAC203.MinMovesPerPips.ToString());

            // Cannot comment out these not to be used in strategy
            // because the function need them to run
            PAC203.haveValidPAC_5 = vhaveValidPAC_5;
            PAC203.PAC_5_UpperVal = vPAC_5_UpperVal;
            PAC203.PAC_5_LowerVal = vPAC_5_LowerVal;
            PAC203.PAC_5_UpperAct = vPAC_5_UpperAct;
            PAC203.PAC_5_LowerAct = vPAC_5_LowerAct;
            PAC203.haveValidPAC_10 = vhaveValidPAC_10;
            PAC203.PAC_10_UpperVal = vPAC_10_UpperVal;
            PAC203.PAC_10_LowerVal = vPAC_10_LowerVal;

            PAC203.PAC_10_UpperAct = vPAC_10_UpperAct;
            PAC203.PAC_10_LowerAct = vPAC_10_LowerAct;

            PAC203.haveValidPAC_15 = vhaveValidPAC_15;
            PAC203.PAC_15_UpperVal = vPAC_15_UpperVal;
            PAC203.PAC_15_LowerVal = vPAC_15_LowerVal;
            PAC203.PAC_15_UpperAct = vPAC_15_UpperAct;
            PAC203.PAC_15_LowerAct = vPAC_15_LowerAct;

            PAC203.haveValidPAC_20 = vhaveValidPAC_20;
            PAC203.PAC_20_UpperVal = vPAC_20_UpperVal;
            PAC203.PAC_20_LowerVal = vPAC_20_LowerVal;
            PAC203.PAC_20_UpperAct = vPAC_20_UpperAct;
            PAC203.PAC_20_LowerAct = vPAC_20_LowerAct;

            PAC203.haveValidPAC_30 = vhaveValidPAC_30;
            PAC203.PAC_30_UpperVal = vPAC_30_UpperVal;
            PAC203.PAC_30_LowerVal = vPAC_30_LowerVal;
            PAC203.PAC_30_UpperAct = vPAC_30_UpperAct;
            PAC203.PAC_30_LowerAct = vPAC_30_LowerAct;

            PAC203.haveValidPAC_60 = vhaveValidPAC_60;
            PAC203.PAC_60_UpperVal = vPAC_60_UpperVal;
            PAC203.PAC_60_LowerVal = vPAC_60_LowerVal;
            PAC203.PAC_60_UpperAct = vPAC_60_UpperAct;
            PAC203.PAC_60_LowerAct = vPAC_60_LowerAct;

            PAC203.haveValidPAC_120 = vhaveValidPAC_120;
            PAC203.PAC_120_UpperVal = vPAC_120_UpperVal;
            PAC203.PAC_120_LowerVal = vPAC_120_LowerVal;
            PAC203.PAC_120_UpperAct = vPAC_120_UpperAct;
            PAC203.PAC_120_LowerAct = vPAC_120_LowerAct;

            PAC203.haveValidPAC_240 = vhaveValidPAC_240;
            PAC203.PAC_240_UpperVal = vPAC_240_UpperVal;
            PAC203.PAC_240_LowerVal = vPAC_240_LowerVal;
            PAC203.PAC_240_UpperAct = vPAC_240_UpperAct;
            PAC203.PAC_240_LowerAct = vPAC_240_LowerAct;

            PAC203.haveValidPAC_Day = vhaveValidPAC_Day;
            PAC203.PAC_Day_UpperVal = vPAC_Day_UpperVal;
            PAC203.PAC_Day_LowerVal = vPAC_Day_LowerVal;
            PAC203.PAC_Day_UpperAct = vPAC_Day_UpperAct;
            PAC203.PAC_Day_LowerAct = vPAC_Day_LowerAct;
        }

        protected override void CalcBar()
        {
            // Test run location, set to 0 until proven otherwise by setting them to 1 to see
            // if a location run
            RunLoc1 = 0;
            RunLoc2 = 0;
            RunLoc3 = 0;
            RunLoc4 = 0;
            RunLoc5 = 0;

            OnCloseTick = (Bars.Status == EBarState.Close);
            vCurrSess.Value = mCurrSess[0];
            currSess = vCurrSess[0];
            vBarDT.Value = Bars.Time.HP_BarYYYYMMDD_HHMM_f(0);
            vELTime.Value = 100 * Bars.Time[0].Hour + Bars.Time[0].Minute;
            // Output.WriteLine("vBarDT = {0}", vBarDT.Value);
            barMFSO = mBarMFSO[0];

            SMP.Value = CurrentPosition.Side;
            SPos = StrategyInfo.MarketPosition;

            LE_Order.Value = false;
            LX_Stp_Order = false;
            LX_Tgt_Order = false;
            LX_Mkt_Order = false;

            SE_Order.Value = false;
            SX_Stp_Order = false;
            SX_Tgt_Order = false;
            SX_Mkt_Order = false;

            haveEnoughData = (Bars.CurrentBar > stratStartBN);


            if (OnCloseTick)
            {
                // Avg_Day (DayRangeAvgByDOW)
                Avg_Day_Range.Value = Avg_Day_Range_f((currSess));

                Vol_Var_5.Value = mVol_Var_5.Value;

                mPAL.Call();
                PAL_5.Value = mPAL.PAL_5[0];
                vPALs[0].Value = mPAL.PAL_5[0];
                vPALs[1].Value = mPAL.PAL_15[0];
                vPALs[2].Value = mPAL.PAL_20[0];
                vPALs[3].Value = mPAL.PAL_30[0];
                vPALs[4].Value = mPAL.PAL_60[0];
                vPALs[5].Value = mPAL.PAL_120[0];
                vPALs[6].Value = mPAL.PAL_240[0];
                vPALs[7].Value = mPAL.PAL_Day[0];

                PAC203.Call();

                isUpTrend.Value = isUpTrend_f();
                isTradeTime.Value = isTradeTime_f();
                isRiskSmall_L.Value = isRiskSmall_L_f();
                isPALPvt_L.Value = isPALPvt_L_f();
            }

            if (haveEnoughData && OnCloseTick)
            {
                if (historical_Swt == 1 && OnCloseTick && Environment.IsRealTimeCalc == false)
                {
                    HP_rHistorical_f();
                }

                ExitMktStd_f();

                EntryStd_f();


                // TSL and Tgt
                LX_StpTgt_f();
                SX_StpTgt_f();

                LE_Ord_Qty = SN_AutoPositionSize_f(LE_riskValue);
                SE_Ord_Qty = SN_AutoPositionSize_f(SE_riskValue);


                // LX_Mkt, LE, LX_Stp, LX_Tgt Ord

                if (SMP[0] == EMarketPositionSide.Long)
                {
                    LX_Ord_Qty = SPos;
                }
                else
                {
                    LX_Ord_Qty = LE_Ord_Qty;

                    /*
                    // { Profit Target 1 and 2 same number for single exit}
                    // in Risk Reward, always 50 percent
                    ProfitTarget_1 = 3.0;
                    //  in Risk Reward, takes remaining (if 1 is On, 50 percent, if 1 is Off, 2 take 100 percent)
                    ProfitTarget_2 = 10.0;
                    // negative number to turn off
                    // If ProfitTarget_2 is off, just let trailing stop take
                    */
                    if (ProfitTarget_1 <= 0)
                    {
                        LX_Tgt1_Ord_Qty = 0;

                        if (ProfitTarget_2 > 0)
                        {
                            LX_Tgt2_Ord_Qty = LX_Ord_Qty;
                        }
                        else
                        {
                            LX_Tgt2_Ord_Qty = 0;
                        }
                    }
                    else
                    {
                        LX_Tgt1_Ord_Qty = (int)(0.5 * (double)LX_Ord_Qty);
                        LX_Tgt2_Ord_Qty = LX_Ord_Qty - LX_Tgt1_Ord_Qty;
                    }
                }

                if (SMP[0] == EMarketPositionSide.Short)
                {
                    SX_Ord_Qty = -SPos;
                }
                else
                {
                    SX_Ord_Qty = SE_Ord_Qty;
                }

                if (LX_Mkt_Order ||
                    // if Price is at or beyond stop price, exit a market
                    Bars.Close[0] <= LX_Stp_P)
                {
                    if (SMP[0] == EMarketPositionSide.Long)
                        LX_Mkt_Ord.Send(LX_Ord_Qty);
                }
                else
                {
                    if (LE_Order[0] == true)
                    {
                        LE_Mkt_Ord.Send(LE_Ord_Qty);
                    }

                    if (LX_Stp_Order == true && LX_Stp_P > 0)
                    {
                        LX_Stp_Ord.Send(LX_Stp_P, LX_Ord_Qty);
                    }

                    if (LX_Tgt_Order == true && LX_Tgt1_P > 0 && LX_Tgt1_Ord_Qty > 0)
                    {
                        LX_Tgt1_Ord.Send(LX_Tgt1_P, LX_Tgt1_Ord_Qty);
                    }

                    if (LX_Tgt_Order == true && LX_Tgt2_P > 0 && LX_Tgt2_Ord_Qty > 0)
                    {
                        LX_Tgt2_Ord.Send(LX_Tgt2_P, LX_Tgt2_Ord_Qty);
                    }

                }

                if (SX_Mkt_Order ||
                    // if Price is at or beyond stop price, exit a market
                    Bars.Close[0] >= SX_Stp_P)
                {
                    if (SMP[0] == EMarketPositionSide.Short)
                        SX_Mkt_Ord.Send(SX_Ord_Qty);
                }
                else
                {
                    if (SE_Order[0] == true)
                    {
                        SE_Mkt_Ord.Send(SE_Ord_Qty);
                    }

                    if (SX_Stp_Order == true && SX_Stp_P > 0)
                    {
                        SX_Stp_Ord.Send(SX_Stp_P, SX_Ord_Qty);
                    }

                    if (SX_Tgt_Order == true && SX_Tgt_P > 0)
                    {
                        SX_Tgt_Ord.Send(SX_Tgt_P, SX_Ord_Qty);
                    }
                }

                SendDataToPlotSpecific_f();


            }

            if (LBOCDoneOnce == false && Bars.LastBarOnChart)
            {
                LBOCDoneOnce = true;
                if (!haveEnoughData)
                {
                    MessageBox.Show("Not enough historical data. Please load more data");
                }
            }
        }

        protected override void StopCalc()
        {
            if (TradingToolBarOn == 1 && frmTmr1 != null)
            {
                frmTmr1.Stop();
                frmTmr1.Dispose();
            }

            base.StopCalc();
        }

        protected override void Destroy()
        {
            tradingProfile = null;

            if (toolBarInited)
            {
                RemoveToolBar_f();
            }
        }

        #region strategyStandardFunctions
        // Harrison Programming Standard strategy framework

        private void RemoveToolBar_f()
        {
            ChartToolBar.AccessToolBar(tb =>
            {
                var itemsToRemove = new List<ToolStripItem>();

                foreach (ToolStripItem item in tb.Items)
                {
                    if (ReferenceEquals(this, item.Tag))
                        itemsToRemove.Add(item);
                }

                foreach (var item in itemsToRemove)
                {
                    tb.Items.Remove(item);
                }
            });
        }

        // Harrison Programming Standard strategy framework


        private void HP_rHistorical_f()
        {

            if (rLE_BDT1 > 0 && rLE_P > 0
                && vBarDT[0] >= rLE_BDT1 && vBarDT[1] < rLE_BDT1)
            {
                if (rLE_P > Bars.Close[0])
                {
                    rLEs.Send(rLE_P, rLE_Qty);
                }
                {
                    rLEl.Send(rLE_P, rLE_Qty);
                }

                Output.WriteLine("rLE_BDT1 = " + rLE_BDT1.ToString());
            }

            if (rLX_BDT1 > 0 && rLX_P > 0
                && vBarDT[0] >= rLX_BDT1 && vBarDT[1] < rLX_BDT1)
            {
                if (rLX_P < Bars.Close[0])
                {
                    rLXs.Send(rLX_P, SPos);
                }
                {
                    rLXl.Send(rLX_P, SPos);
                }

                Output.WriteLine("rLX_BDT1 = " + rLX_BDT1.ToString());
            }

            if (rSE_BDT1 > 0 && rSE_P > 0
                && vBarDT[0] >= rSE_BDT1 && vBarDT[1] < rSE_BDT1)
            {
                if (rSE_P < Bars.Close[0])
                {
                    rSEs.Send(rSE_P, rSE_Qty);
                }
                {
                    rSEl.Send(rSE_P, rSE_Qty);
                }

                Output.WriteLine("rSE_BDT1 = " + rSE_BDT1.ToString());
            }

            if (rSX_BDT1 > 0 && rSX_P > 0
                && vBarDT[0] >= rSX_BDT1 && vBarDT[1] < rSX_BDT1)
            {
                if (rSX_P > Bars.Close[0])
                {
                    rSXs.Send(rSX_P, SPos);
                }
                {
                    rSXl.Send(rSX_P, SPos);
                }

                Output.WriteLine("rSX_BDT1 = " + rSX_BDT1.ToString());
            }
        }

        private void ExitMktStd_f()
        {

            if (longTrades == 1)
            {
                // EODExitTime (end of Day)
                // does not consider special early close
                if (TTB_TimeExitSwitch == 1 &&
                    // Exit at a time only
                    EODExitTime >= 0 && vELTime[0] >= EODExitTime && vELTime[1] < EODExitTime)
                {
                    // Sell("LX_EOD") next bar at market;
                    LX_Mkt_Order = true;
                }

                // EOWExitTime (end of Week)
                // does not consider early market close on Fri
                // does not consider market close on entire Fri
                if (Bars.Time[0].DayOfWeek == DayOfWeek.Friday
                    && EOWExitTime >= 0 && vELTime[0] >= EOWExitTime && vELTime[1] < EOWExitTime)
                {
                    // Sell("LX_EOD") next bar at market;
                    LX_Mkt_Order = true;
                }

            }

            if (shortTrades == 1)
            {
                // EODExitTime (end of Day)
                // does not consider special early close
                if (TTB_TimeExitSwitch == 1
                     && EODExitTime >= 0
                     && vELTime[0] >= EODExitTime)
                {
                    // BuyToCover("SX_EOD") next bar at market;
                    SX_Mkt_Order = true;
                }

                // EOWExitTime (end of Week)
                // does not consider early market close on Fri
                // does not consider market close on entire Fri
                if (Bars.Time[0].DayOfWeek == DayOfWeek.Friday
                    && EOWExitTime >= 0 && vELTime[0] >= EODExitTime)
                {
                    // Sell("LX_EOD") next bar at market;
                    SX_Mkt_Order = true;
                }
            }


            ExitMktSpecific_f();
        }

        private void EntryStd_f()
        {
            LE_Specific.Value = LE_Specific_f();

            if (
                (LX_Mkt_Order == false
                && longTrades == 1
                && SMP[0] != EMarketPositionSide.Long
                && OKToEnterStd_f()
                && LE_Specific[0]
                && test_E_Swt == 0)
            )
            {
                LE_Pvt_P = HPSM.RoundPriceTo_f(Bars.Close[0], roundPriceToPts);
                LE_Lmt_P = LE_Pvt_P;
                LE_Order.Value = true;
                // LE_OrderType = OrderType_Market;
                // Buy ("LE1") EntryLotSize shares next bar at LE_Pvt_P stop;
            }

            if (
                (SX_Mkt_Order == false
                && shortTrades == 1 && SMP[0] != EMarketPositionSide.Short
                && OKToEnter
                && SE_Specific_f()
                && test_E_Swt == 0)
            )
            {
                SE_Pvt_P = HPSM.RoundPriceTo_f(Bars.Close[0], roundPriceToPts);
                SE_Lmt_P = SE_Pvt_P;
                // SE_OrderType = OrderType_Market;
                // SellShort ("SE1") EntryLotSize shares next bar at SE_Pvt_P stop;
                SE_Order.Value = true;
            }

        }

        private void BarsSinceEntry_f()
        {
            // track of own bars since entry, as EL BarsSinceEntry may have issue in real time
            // >> Shall we convert to Extension Method of SMP?

            if (SMP[0] == EMarketPositionSide.Long)
            {
                if (SMP[1] != EMarketPositionSide.Long)
                    LE_BarsSince.Value = 0;
                else
                    LE_BarsSince.Value = LE_BarsSince[0] + 1;
            }

            if (SMP[0] == EMarketPositionSide.Short)
            {
                if (SMP[1] != EMarketPositionSide.Short)
                    SE_BarsSince.Value = 0;
                else
                    SE_BarsSince.Value = SE_BarsSince[0] + 1;
            }
        }



        private void SendDataToPlotStd_f()
        {
            // Reserve 1 to 50 to 800 to 999 for specific strategy
            // for Standard Strategy: Plot numbers 051 to 799

            // RunLoc#
            StrategyInfo.SetPlotValue(051, RunLoc1);
            StrategyInfo.SetPlotValue(052, RunLoc2);
            StrategyInfo.SetPlotValue(053, RunLoc3);
            StrategyInfo.SetPlotValue(054, RunLoc4);
            StrategyInfo.SetPlotValue(055, RunLoc5);

            // for Standard Strategy: Plot numbers 001 to 799
            StrategyInfo.SetPlotValue(061, (double)currSess);
            StrategyInfo.SetPlotValue(062, vBarDT[0]);
            StrategyInfo.SetPlotValue(063, (double)vELTime[0]);
            StrategyInfo.SetPlotValue(064, (double)barMFSO);
            StrategyInfo.SetPlotValue(065, (double)SMP[0]);
            StrategyInfo.SetPlotValue(066, SPos);

            StrategyInfo.SetPlotValue(111, LE_Pvt_P);
            StrategyInfo.SetPlotValue(112, LE_Lmt_P);
            StrategyInfo.SetPlotValue(113, LE_Ent_P);
            StrategyInfo.SetPlotValue(114, LE_Ord_Qty);
            StrategyInfo.SetPlotValue(115, LX_Stp0_P);
            StrategyInfo.SetPlotValue(116, LX_Stp_P);
            StrategyInfo.SetPlotValue(117, LX_Tgt_P);
            StrategyInfo.SetPlotValue(118, LX_Ord_Qty);

            StrategyInfo.SetPlotValue(131, 1 * HPSM.TFTo10_f(LE_Order[0]));
            // if (LE_Order[0]) Output.WriteLine("Have LE_Order for {0} at {1}", LE_Ord_Qty, Bars.Time[0]);
            StrategyInfo.SetPlotValue(132, 2 * HPSM.TFTo10_f(LX_Stp_Order));
            StrategyInfo.SetPlotValue(133, 3 * HPSM.TFTo10_f(LX_Tgt_Order));
            StrategyInfo.SetPlotValue(134, 4 * HPSM.TFTo10_f(LX_Mkt_Order));

            StrategyInfo.SetPlotValue(141, 1.5 * HPSM.TFTo10_f(SE_Order[0]));
            if (SE_Order[0]) Output.WriteLine("Have SE_Order for {0} at {1}", SE_Ord_Qty, Bars.Time[0]);
            StrategyInfo.SetPlotValue(142, 2.5 * HPSM.TFTo10_f(SX_Stp_Order));
            StrategyInfo.SetPlotValue(143, 3.5 * HPSM.TFTo10_f(SX_Tgt_Order));
            StrategyInfo.SetPlotValue(144, 4.5 * HPSM.TFTo10_f(SX_Mkt_Order));

            StrategyInfo.SetPlotValue(211, SE_Pvt_P);
            StrategyInfo.SetPlotValue(212, SE_Lmt_P);
            StrategyInfo.SetPlotValue(213, SE_Ent_P);
            StrategyInfo.SetPlotValue(214, SE_Ord_Qty);
            StrategyInfo.SetPlotValue(215, SX_Stp0_P);
            StrategyInfo.SetPlotValue(216, SX_Stp_P);
            StrategyInfo.SetPlotValue(217, SX_Tgt_P);
            StrategyInfo.SetPlotValue(218, SX_Ord_Qty);
        }


        private void frmTmr1_Tick(Object sender, EventArgs arg)
        // protected override void OnRecalcLastBarAfterEvent()
        {
            if (TradingToolBarOn == 1)
            {

                if (LiveTradeSwitch == 1)
                {
                    checkConnectionStatus();
                    TradeManager.ProcessEvents();
                }

                MP_Update_f();
                CnfmTimeOutReset();
            }
        }


        private void initializeToolBar()
        {
            ChartToolBar.AccessToolBar(tb =>
            {
                btnAutoStatus = new ToolStripLabel
                {
                    Text = "0 0 0",
                    // BackColor = Color.Red,
                    ForeColor = Color.Red,
                    ToolTipText = "Show Auto Status"
                };
                AddItem2ToolStrip(tb, btnAutoStatus);

                AddItem2ToolStrip(tb, new ToolStripSeparator());
                btnLE = new ToolStripButton
                {
                    Text = LEStr,
                    BackColor = LE_Color,
                    ToolTipText = "Click for Long Entry"
                };
                btnLE.Click += (_1, _2) => btnLE_Click();
                AddItem2ToolStrip(tb, btnLE);

                numUpDn_LE_PosSiz = new NumericUpDown
                {
                    Dock = DockStyle.Fill,
                    Maximum = MaxContractsSize,
                    Minimum = 0,
                    Increment = ForexStepSize,
                    DecimalPlaces = 0,
                    Value = posSize,
                    Text = " Position Size UpDn ",
                    BackColor = Color.LightGray,
                    Enabled = true
                };
                AddItem2ToolStrip(tb, new ToolStripControlHost(numUpDn_LE_PosSiz));

                AddItem2ToolStrip(tb, new ToolStripSeparator());

                btnSE = new ToolStripButton
                {
                    Text = " SE ",
                    BackColor = SE_Color,
                    ToolTipText = "Click for Short Entry"
                };
                btnSE.Click += (_1, _2) => btnSE_Click();
                AddItem2ToolStrip(tb, btnSE);

                AddItem2ToolStrip(tb, new ToolStripSeparator());

                numUpDn_SE_PosSiz = new NumericUpDown
                {
                    Dock = DockStyle.Fill,
                    Maximum = MaxContractsSize,
                    Minimum = 0,
                    Increment = ForexStepSize,
                    DecimalPlaces = 0,
                    Value = posSize,
                    Text = " Position Size UpDn ",
                    BackColor = Color.LightGray,
                    Enabled = true
                };
                AddItem2ToolStrip(tb, new ToolStripControlHost(numUpDn_SE_PosSiz));

                AddItem2ToolStrip(tb, new ToolStripSeparator());

                btnLX = new ToolStripButton
                {
                    Text = " LX ",
                    BackColor = LX_Color,
                    ToolTipText = "Click for Long Exit"
                };
                btnLX.Click += (_1, _2) => btnLX_Click();
                AddItem2ToolStrip(tb, btnLX);

                AddItem2ToolStrip(tb, new ToolStripSeparator());

                btnSX = new ToolStripButton
                {
                    Text = " SX ",
                    BackColor = SX_Color,
                    ToolTipText = "Click for Short Exit"
                };
                btnSX.Click += (_1, _2) => btnSX_Click();
                AddItem2ToolStrip(tb, btnSX);

                AddItem2ToolStrip(tb, new ToolStripSeparator());

                // Reserved for Adding these feature
                btnSPos = new ToolStripButton
                {
                    // Click to get the last price
                    Text = "SPos",
                    BackColor = Color.LightGray,
                    // ToolTipText = "Click to set to last, then modify"
                    ToolTipText = "Strategy Position (SPos)"
                };
                AddItem2ToolStrip(tb, btnSPos);

                btnMatch = new ToolStripButton
                {
                    // Click to get the last price
                    Text = "Match TBD",
                    // ToolTipText = "Click to set to last, then modify"
                    ToolTipText = "SPos/APos match or not"
                };
                AddItem2ToolStrip(tb, btnMatch);

                btnAPos = new ToolStripButton
                {
                    // Click to get the last price
                    Text = "APos",
                    // ToolTipText = "Click to set to last, then modify"
                    ToolTipText = "Actual Position (APos)"
                };
                AddItem2ToolStrip(tb, btnAPos);

                AddItem2ToolStrip(tb, new ToolStripSeparator());

            });
        }

        private void AddItem2ToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }

        private void TradingProfile_Connection(ETM_ConnectionChanged connState)
        {
            checkConnectionStatus();
        }

        private void checkConnectionStatus()
        {
            if (tradingProfile.ConnectionState == ETM_ConnectionChanged.eTM_CC_Connected)
            {
                if (!connectedTF)
                    showConnected();
            }
            else if (tradingProfile.ConnectionState == ETM_ConnectionChanged.eTM_CC_Disconnected)
            {
                if (connectedTF)
                    showDisconnected();
            }
        }

        private void showConnected()
        {
            connectedTF = true;

            if (TradingToolBarOn == 1)
            {
                ChartToolBar.AccessToolBarAsync(tb =>
                {
                    btnAutoStatus.Text = "1 1 1";
                    btnAutoStatus.ForeColor = Color.DarkCyan;
                    btnLE.Enabled = true;
                    btnSE.Enabled = true;
                    btnLX.Enabled = true;
                    btnSX.Enabled = true;
                    btnTestModify.Enabled = true;
                    btnTestCancel.Enabled = true;
                    btnClosePos.Enabled = true;
                });
            }
        }

        private void showDisconnected()
        {
            connectedTF = false;
            if (TradingToolBarOn == 1)
            {
                ChartToolBar.AccessToolBarAsync(tb =>
                {
                    btnAutoStatus.Text = "0 0 0";
                    btnAutoStatus.ForeColor = Color.Red;
                    btnLE.Enabled = false;
                    btnSE.Enabled = false;
                    btnLX.Enabled = false;
                    btnSX.Enabled = false;
                    btnTestModify.Enabled = false;
                    btnTestCancel.Enabled = false;
                    btnClosePos.Enabled = false;
                });
            }
        }

        private void btnLE_Click()
        {
            try
            {
                Output.WriteLine("ConfirmationRequired: " + ConfirmationRequired.ToString());
                Output.WriteLine("btnLE.Text: " + btnLE.Text);

                if ((ConfirmationRequired == true && btnLE.Text == cnfmStr)
                    || (ConfirmationRequired == false && btnLE.Text == LEStr))
                {
                    btnLE.Text = LEStr;
                    if (CurrentPosition.Side != EMarketPositionSide.Long)
                    {
                        posSize = SN_AutoPositionSize_f(LE_riskValue);

                        LE_Mkt_Ord.Send("LE", posSize);
                        Output.WriteLine("LE " + posSize.ToString());

                        rLE_BDT1 = HP_BarDT_f(1);
                        rLE_P = Bars.Close[0];
                        rLE_Qty = posSize;
                    }
                }

                if (ConfirmationRequired && btnLE.Text == LEStr)
                {
                    ChartToolBar.AccessToolBarAsync(tb =>
                    {
                        btnLE.Text = cnfmStr;
                        LE_CnfmTimeOut = DateTime.Now.AddSeconds(ConfirmTimeOutSeconds);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSE_Click()
        {
            try
            {
                Output.WriteLine("ConfirmationRequired: " + ConfirmationRequired.ToString());
                Output.WriteLine("btnSE.Text: " + btnSE.Text);

                if ((ConfirmationRequired == true && btnSE.Text == cnfmStr)
                    || (ConfirmationRequired == false && btnSE.Text == SEStr))
                {
                    btnSE.Text = SEStr;
                    if (CurrentPosition.Side != EMarketPositionSide.Short)
                    {
                        // make allowance for bid/ask spread at market entry
                        posSize = SN_AutoPositionSize_f(SE_riskValue);

                        SE_Mkt_Ord.Send("SE", posSize);
                        Output.WriteLine("SE " + posSize.ToString());

                        rSE_BDT1 = HP_BarDT_f(1);
                        rSE_P = Bars.Close[0];
                        rSE_Qty = posSize;
                    }
                }

                if (ConfirmationRequired && btnSE.Text == SEStr)
                {
                    ChartToolBar.AccessToolBarAsync(tb =>
                    {
                        btnSE.Text = cnfmStr;
                        SE_CnfmTimeOut = DateTime.Now.AddSeconds(ConfirmTimeOutSeconds);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // private void numUpDn_LE_PosSizValueChanged()
        private void numUpDn_LE_PosSizValueChanged(Object sender, EventArgs e)
        {
            posSize = (int)numUpDn_LE_PosSiz.Value;
            StringBuilder posSizeSb = new StringBuilder("posSize: ");
            posSizeSb.Append(numUpDn_LE_PosSiz.Value.ToString());
            Output.WriteLine(posSizeSb.ToString());
        }

        private void numUpDn_SE_PosSizValueChanged(Object sender, EventArgs e)
        {
            posSize = (int)numUpDn_SE_PosSiz.Value;
            StringBuilder posSizeSb = new StringBuilder("posSize: ");
            posSizeSb.Append(numUpDn_SE_PosSiz.Value.ToString());
            Output.WriteLine(posSizeSb.ToString());
        }

        private void btnLX_Click()
        {
            try
            {
                Output.WriteLine("ConfirmationRequired: " + ConfirmationRequired.ToString());
                Output.WriteLine("btnLX.Text: " + btnLX.Text);

                if ((ConfirmationRequired == true && btnLX.Text == cnfmStr)
                    || (ConfirmationRequired == false && btnLX.Text == LXStr))
                {
                    btnLX.Text = LXStr;
                    SPos = StrategyInfo.MarketPosition;
                    if (SPos > 0)
                    {
                        X_Qty = SPos;
                        LX_Mkt_Ord.Send(X_Qty);
                        Output.WriteLine("LX " + X_Qty.ToString());

                        rLX_BDT1 = HP_BarDT_f(1);
                        rLX_P = Bars.Close[0];
                    }
                }

                if (ConfirmationRequired && btnLX.Text == LXStr)
                {
                    ChartToolBar.AccessToolBarAsync(tb =>
                    {
                        btnLX.Text = cnfmStr;
                        LX_CnfmTimeOut = DateTime.Now.AddSeconds(ConfirmTimeOutSeconds);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSX_Click()
        {
            try
            {
                Output.WriteLine("ConfirmationRequired: " + ConfirmationRequired.ToString());
                Output.WriteLine("btnSX.Text: " + btnSX.Text);

                if ((ConfirmationRequired == true && btnSX.Text == cnfmStr)
                    || (ConfirmationRequired == false && btnSX.Text == SXStr))
                {
                    btnSX.Text = SXStr;
                    SPos = StrategyInfo.MarketPosition;
                    if (SPos < 0)
                    {
                        X_Qty = -SPos;
                        SX_Mkt_Ord.Send(X_Qty);
                        Output.WriteLine("SX " + X_Qty.ToString());

                        rSX_BDT1 = HP_BarDT_f(1);
                        rSX_P = Bars.Close[0];
                    }
                }

                if (ConfirmationRequired && btnSX.Text == SXStr)
                {
                    ChartToolBar.AccessToolBarAsync(tb =>
                    {
                        btnSX.Text = cnfmStr;
                        SX_CnfmTimeOut = DateTime.Now.AddSeconds(ConfirmTimeOutSeconds);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CnfmTimeOutReset()
        {
            ChartToolBar.AccessToolBarAsync(tb =>
            {
                if (btnLE.Text == cnfmStr && DateTime.Now > LE_CnfmTimeOut)
                {
                    btnLE.Text = LEStr;
                }

                if (btnSE.Text == cnfmStr && DateTime.Now > SE_CnfmTimeOut)
                {
                    btnSE.Text = SEStr;
                }

                if (btnLX.Text == cnfmStr && DateTime.Now > LX_CnfmTimeOut)
                {
                    btnLX.Text = LXStr;
                }

                if (btnSX.Text == cnfmStr && DateTime.Now > SX_CnfmTimeOut)
                {
                    btnSX.Text = SXStr;
                }
            });

        }

        private void MP_Update_f()
        {
            SPos = StrategyInfo.MarketPosition;

            try
            {
                ChartToolBar.AccessToolBarAsync(tb =>
                {
                    btnSPos.Text = SPos.ToString();


                    if (SPos > 0)
                    {
                        if (btnSPos.BackColor != LE_Color)
                        {
                            btnSPos.BackColor = LE_Color;
                            Output.WriteLine("btnSPos.BackColor 2 " + btnSPos.BackColor.ToString());
                        }
                    }
                    else if (SPos < 0)
                    {
                        if (btnSPos.BackColor != SE_Color)
                        {
                            btnSPos.BackColor = SE_Color;
                            Output.WriteLine("btnSPos.BackColor 3 " + btnSPos.BackColor.ToString());
                        }
                    }
                    else
                    {
                        if (btnSPos.Text != SPosFlatStr)
                        {
                            btnSPos.Text = SPosFlatStr;
                            btnSPos.BackColor = FlatColor;
                        }
                    }

                    if (APos > 0)
                    {
                        if (btnAPos.BackColor != LE_Color)
                        {
                            btnAPos.BackColor = LE_Color;
                        }
                    }
                    else if (APos < 0)
                    {
                        if (btnAPos.BackColor != SE_Color)
                        {
                            btnAPos.BackColor = SE_Color;
                        }
                    }
                    else
                    {
                        if (btnAPos.Text != APosFlatStr)
                        {
                            btnAPos.Text = APosFlatStr;
                            btnAPos.BackColor = FlatColor;
                        }
                    }

                    if (APos != SPos)
                    {
                        if (btnMatch.Text != misMatchStr)
                        {
                            btnMatch.Text = misMatchStr;
                            btnMatch.BackColor = PosMisMatchColor;
                        }
                    }
                    else if (APos == SPos)
                    {
                        if (btnMatch.Text != matchStr)
                        {
                            btnMatch.Text = matchStr;
                            btnMatch.BackColor = PosMatchColor;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void setPosPanelInfo_f(int amp, double openPnL)
        {
            APos = amp;
            if (TradingToolBarOn == 1)
            {
                ChartToolBar.AccessToolBarAsync(tb =>
                {
                    btnClosePos.Enabled = (amp != 0);
                    btnClosePos.Text = posInfoToStr_f(amp, openPnL);
                    btnClosePos.BackColor =
                        ((openPnL >= 0) ? Color.LawnGreen : Color.OrangeRed);
                });
            }
        }

        private static string posInfoToStr_f(int amp, double openPnL)
        {
            string ampStr;
            if (amp > 0)
                ampStr = amp.ToString() + " Long";
            else if (amp < 0)
                ampStr = (-amp).ToString() + " Short";
            else
                ampStr = "Flat";
            return string.Format("{0} {1}", ampStr, openPnL.ToString("C"));
        }

        private ITradingProfile getTradingProfile()
        {
            if (tradingProfile == null)
            {
                foreach (var tp in TradeManager.TradingProfiles)
                {
                    if (tp.Name == Profile)
                    {
                        Output.WriteLine("Profile: {0}", tp.Name);
                        if (tp.ConnectionState != ETM_ConnectionChanged.eTM_CC_Connected)
                            ExecControl.Abort("Profile {0} is not connected!", tp.Name);

                        tradingProfile = tp;

                        var acct = tradingProfile.Accounts[0];

                        // if specified, look for it; otherwise use above
                        if (IB_AccountName != "")
                        {
                            bool foundValidAcct = false;
                            Output.WriteLine("Numbers of accounts: {0}", tradingProfile.Accounts.Length);
                            for (int j = 0; j < tradingProfile.Accounts.Length; j++)
                            {
                                acct = tradingProfile.Accounts[j];
                                Output.WriteLine("Acct # & Name: {0} {1}", j, acct.ToString());
                                if (acct.ToString() == IB_AccountName)
                                {
                                    foundValidAcct = true;
                                    Output.WriteLine("Acct # & Name: {0} {1}", j, acct.ToString());
                                    break;
                                }
                            }
                            if (foundValidAcct == false)
                            {
                                ExecControl.Abort("Did not find matching account: {0}. "
                                    + " Make sure NOT to incldue double quotes for string input value!", IB_AccountName);
                            }
                        }

                        Output.WriteLine("Acct selected: {0}", acct.ToString());
                        tradingProfile.CurrentAccount = acct;
                        tradingProfile.CurrentSymbol = symInfo;

                        var poss = TradeManager.TradingData.Positions;
                        poss.AccountFltr.CurrentValue = acct;
                        poss.SymbolFltr.CurrentValue = symInfo.symbol.SymbolName;
                        poss.ProfileFltr.CurrentValue = Profile;

                        poss.Added += positions_AllChanged;
                        poss.Changed += positions_AllChanged;
                        poss.Deleted += positions_AllChanged;

                        break;
                    }
                }

            }

            return tradingProfile;
        }

        private void positions_AllChanged(Position[] _items)
        {
            var poss2 = TradeManager.TradingData.Positions.Items;

            if (poss2.Length == 0)
                setPosPanelInfo_f(0, 0);
            else
                setPosPanelInfo_f(poss2[0].Value, poss2[0].OpenPL);

            btnAPos.Text = poss2[0].Value.ToString();

            // display_E_OrderState_f();
        }

        // HarrisonProgramming supporting functions
        /*
        // Comment block
        */

        private double HP_AccountBalance_f(string accountName6)
        {
            double acctbal = 0.0;

            if (tradingProfile.ConnectionState == ETM_ConnectionChanged.eTM_CC_Connected)
            {
                var accounts = TradeManager.TradingData.Accounts.Items;
                foreach (Account account in accounts)
                {
                    if (account.Name == accountName6)
                    {
                        acctbal = (double)account.Balance;
                        break;
                    }
                }
            }
            else
            {
                // indicate no real balance of no connection
                acctbal = -999.0;
            }

            return acctbal;
        }


        private double HP_BarDT_f(int barsBack)
        {
            return (0.0001 * Convert.ToDouble(Bars.Time[barsBack].ToString("yyyyMMddHHmm")));
        }

        private double PipCalculatorUSD_f()
        {
            /*
            string[] symParts = new string[2];
			string sepStr = ".";
			
            if (SymbolName.Contains(sepStr))
            {
                // Assume IB Forex symbol format of ABC.XYZ
                symParts = SymbolName.Split('.');
            }
            {
                // for TS
                symParts[0] = SymbolName.Substring(0, 3);
                symParts[1] = SymbolName.Substring(3, 3);
                Output.WriteLine("symParts[0] : " + symParts[0]);
                Output.WriteLine("symParts[1] : " + symParts[1]);
            }
			*/
            string[] symParts = SymbolName.Split('.');

            string baseSym = symParts[1];
            double convertfactor = 1.0;

            switch (baseSym)
            {
                case "CAD":
                    if (USDCAD_Price > 0) convertfactor = (1.0 / USDCAD_Price);
                    break;

                case "CHF":
                    if (USDCHF_Price > 0) convertfactor = (1.0 / USDCHF_Price);
                    break;

                case "JPY":
                    if (USDJPY_Price > 0) convertfactor = (1.0 / USDJPY_Price);
                    break;

                default:
                    convertfactor = 1.0;
                    break;
            }
            double PipValue = convertfactor * (100000 * ptsPerPips);

            return PipValue;
        }

        private double HP_RoundPriceTo_f(double price6, double roundPriceTo6)
        {
            return (roundPriceTo6 * Math.Round(price6 / roundPriceTo6, 0));
        }


        #endregion strategyStandardFunctions

        #region strategySpecificFunctions

        private void ExitMktSpecific_f()
        {
            ;
        }

        private bool OKToEnterStd_f()
        {
            bool OKToEnter6;

            // Not to enter on the last bar in the session, as it would cause order rejection
            OKToEnter6 = (Bars.Time[0].TimeOfDay != sess1EndTime);

            return OKToEnter6;
        }

        private bool OK1 = false;

        private bool LE_Specific_f()
        {
            // Long Entry condition for this Specific strategy
            bool LE_Cond;

            LE_Cond = (
                isPALPivotLow[0]
                && isUpTrend[0]
                && isTradeTime[0]
                && isRiskSmall_L[0]
                && isPALPvt_L[0]
                    );

            return LE_Cond;
        }

        private bool SE_Specific_f()
        {
            // Short Entry condition for this Specific strategy
            bool SE_Cond;

            SE_Cond = (
                false
                    );

            return SE_Cond;
        }

        private int checkPosSize;

        private int SN_AutoPositionSize_f(double stpPts6)
        {
            // Auto_Position_Sizing_Function

            double accountSize6, tradeSizeDbl6;
            int tradeSize6;

            if (APModeInput == APMode_RealAcctBalance || APModeInput == APMode_AcctSizeInput)
            {
                if (APModeInput == APMode_RealAcctBalance && LiveTradeSwitch == 1)
                {
                    // "Real-time Account Balance"
                    accountSize6 = HP_AccountBalance_f(IB_AccountName);
                    // if no Real_Balance data, use AP_Mode_2_User_Input
                    if (accountSize6 == -999)
                        accountSize6 = AccountSizeInput;
                }
                else
                // if (APModeInput == AP_Mode_User_Input)
                {
                    accountSize6 = AccountSizeInput;
                }

                // Auto Position Sizing function
                double stopPts6 = Math.Abs(stpPts6);
                if (stopPts6 < onePip) stopPts6 = onePip;
                // double PipValue = PipCalculatorUSD_f();
                double PipValue = PipValueInUSD;

                // Instrument Type = 1 for Forex PIP Calculation , = 2 for Futures, = 3 for Equities
                // For NOW, just program for Forex

                // HP_PipCalculatorUSD_f is based on 100,000
                // rawTradeSize
                tradeSizeDbl6 = (int)(100000.0 * (accountSize6 * 0.01 * RiskPerTrade) /
                        ((stopPts6 / onePip) * PipValue));

                // If (rawTradeSize < ForexStepSize ) use minimum step 
                // autoTradeSize
                tradeSizeDbl6 = (double)ForexStepSize
                    * (Math.Floor(tradeSizeDbl6 / (double)ForexStepSize));

                tradeSizeDbl6 = TTB_Risk * tradeSizeDbl6;

                // Min
                if (tradeSizeDbl6 < dMinContractsSize)
                    // no trade executed
                    tradeSizeDbl6 = 0;

                // Max
                if (tradeSizeDbl6 > dMaxContractsSize)
                    tradeSizeDbl6 = dMaxContractsSize;

                // this may overflow if tradeSizeDbl6 is larger, move the calc to later
                tradeSize6 = (int)tradeSizeDbl6;
            }
            else
            // if (APModeInput == AP_Mode_Fix_Contract)
            {
                // fixTradeSize
                tradeSize6 = FixContractsInput;
            }

            checkPosSize = tradeSize6;

            return tradeSize6;
        }

        private double Avg_Day_Range_f(int currSess6)
        {
            // Avg_Day (DayRangeAvgByDOW)
            double ADR6;

            switch (currSess6)
            {
                case 2:
                    ADR6 = wDayRangeAvg2.Value;
                    break;

                case 3:
                    ADR6 = wDayRangeAvg3.Value;
                    break;

                case 4:
                    ADR6 = wDayRangeAvg4.Value;
                    break;

                case 5:
                    ADR6 = wDayRangeAvg5.Value;
                    break;

                case 1:
                default:
                    ADR6 = wDayRangeAvg1.Value;
                    break;
            }

            return ADR6;
        }

        private bool isTradeTime_f()
        {
            // default to true for TTB_TradeTime_On != 1
            bool isTradeTime6 = true;

            if (TTB_TradeTimeOn == 1)
            {
                if (((TradeEndTime1 > TradeStartTime1 && TradeStartTime1 >= 0 && TradeEndTime1 >= 0)
                    && (vELTime[0] >= TradeStartTime1 && vELTime[0] < TradeEndTime1))
                    || ((TradeEndTime2 > TradeStartTime2 && TradeStartTime2 >= 0 && TradeEndTime2 >= 0)
                      && (vELTime[0] >= TradeStartTime2 && vELTime[0] < TradeEndTime2)))
                {
                    isTradeTime6 = true;
                }
                else
                {
                    isTradeTime6 = false;
                }
            }

            return isTradeTime6;
        }

        private bool isUpTrend_f()
        {
            int PAL_60_Dir, PAL_240_Dir, PAC_10_Dir;
            bool isUpTrend6;

            if (Bars.Close[0] > vPALs[4][0])
            {
                PAL_60_Dir = 1;
            }
            else
            {
                PAL_60_Dir = 0;
            }

            if (Bars.Close[0] > vPALs[6][0])
            {
                PAL_240_Dir = 1;
            }
            else
            {
                PAL_240_Dir = 0;
            }

            /*
            If PAC_10 is Green then PAC_10_Dir = 1
            If PAC_10 is Red then PAC_10_Dir = -1
            */
            if (vPAC_10_LowerAct[0] == true)
            {
                PAC_10_Dir = 1;
            }
            else if (vPAC_10_UpperAct[0] == true)
            {
                PAC_10_Dir = -1;
            }
            else
            {
                PAC_10_Dir = 0;
            }

            if (PAL_60_Dir == 1 && PAL_240_Dir == 1 && PAC_10_Dir == 1)
            {
                isUpTrend6 = true;
            }
            else
            {
                isUpTrend6 = false;
            }

            return isUpTrend6;
        }

        // Need separate isRiskSmall_L_f and isRiskSmall_S_f functions
        private bool isRiskSmall_L_f()
        {
            // this also define LE_riskValue besides isRiskSmall_L

            bool isRiskSmall;

            // do this computation only when not in long, so as not to change stop value
            if (SMP[0] != EMarketPositionSide.Long)
            {
                double stopLossPrice1, entryPrice1, riskValue1;
                double stopLossPrice, riskValue;

                stopLossPrice1 = Bars.Low.Lowest(StopLossPivotLength) - stopLossBufferPts;
                entryPrice1 = Bars.Close[0];
                // Add for local use
                riskValue1 = entryPrice1 - stopLossPrice1;

                if (riskValue1 < minRiskRequiredPts)
                {
                    riskValue = Math.Min(riskValue1, optimalRiskPts);

                    stopLossPrice = (entryPrice1 - riskValue);

                    isRiskSmall = true;
                }
                else if (riskValue1 >= minRiskRequiredPts && riskValue1 < maxRiskAllowedPts)
                {
                    // No change
                    riskValue = riskValue1;
                    stopLossPrice = stopLossPrice1;
                    isRiskSmall = true;
                }
                else
                // if (riskValue1 >= maxRiskAllowedPts )
                {
                    // Still need to define vars riskValue and stopLossPrice
                    riskValue = riskValue1;
                    stopLossPrice = stopLossPrice1 - stopLossBufferPts;

                    isRiskSmall = false;
                }

                // initial stop price take it from here
                LX_Stp0_P = stopLossPrice;
                // >> SN: This is to prevent possible divided-by-zero error
                riskValue = Math.Max(riskValue, stopLossBufferPts);
                LE_riskValue = riskValue;

                /*
                if (vBarDT[0] == 20180802.2030 )
                {
                    Output.WriteLine("stopLossPrice1 = {0}", stopLossPrice1);
                    Output.WriteLine("entryPrice1 = {0}", entryPrice1);
                    Output.WriteLine("riskValue1 = {0}", riskValue1);
                    Output.WriteLine("riskValue = {0}", riskValue);
                    Output.WriteLine("stopLossPrice = {0}", stopLossPrice);
                    Output.WriteLine("LX_Stp0_P = {0}", LX_Stp0_P);
                }
                */
            }
            else
            {
                isRiskSmall = false;
            }

            return isRiskSmall;
        }

        private bool isPALPvt_L_f()
        {
            // Use isPALPvt_L instead of isLongPivot for faster programming with _L and _S
            bool isPALPvt_L6 = false;
            isPALPivotLow.Value = (PAL_5[0] > PAL_5[1] && PAL_5[1] < PAL_5[2]);

            if (isPALPivotLow[0])
            {
                upPALPivotValue_2.Value = upPALPivotValue_1[0];
                upPALPivotValue_1.Value = upPALPivotValue[0];
                upPALPivotValue.Value = PAL_5[0];

                upPALPivotPrice_2.Value = upPALPivotPrice_1[0];
                upPALPivotPrice_1.Value = upPALPivotPrice[0];
                upPALPivotPrice.Value = Bars.Low.Lowest(PivotLength);
            }

            if (isPALPivotLow[0] == true
                && upPALPivotValue[0] > upPALPivotValue_1[0] + pivotDiffPts
                && upPALPivotValue[0] > upPALPivotValue_1[0] + pivotDiffPts
                )
            {
                isPALPvt_L6 = true;
            }

            return isPALPvt_L6;
        }

        private bool LX_PAC_BrokeThru;
        private bool SX_PAC_BrokeThru;
        private double LX_PAC_Stp_1, LX_PAC_Stp_2, LX_PAC_Stp;
        private double SX_PAC_Stp_1, SX_PAC_Stp_2, SX_PAC_Stp;


        private void StpTgt_EntryBar_f()
        {
            if (SMP[1] != EMarketPositionSide.Long && SPos > 0)
            {
                LX_Stp_Order = true;
                LX_Stp_P = LX_Stp0_P;
                LX_Tgt_Order = true;
            }
            else if (SMP[1] != EMarketPositionSide.Short && SPos < 0)
            {
                SX_Stp_Order = true;
                SX_Stp_P = SX_Stp0_P;
                SX_Tgt_Order = true;
            }
        }

        double LE_EntryRR;
        private double LX_TrlStp_0, LX_TrlStp_1, LX_TrlStp_2;

        private void LX_StpTgt_f()
        {
            // negative to indicate not to plot, until proven otherwise
            plotStopDot = -1;
            plotStopDash = -1;
            // use -1 to indicate not active
            plotStopBefTrl = -1;

            // reset trailing, end of trade
            if (LX_TrlStpActive[0] == 1 && SMP[0] != EMarketPositionSide.Long)
            {
                LX_TrlStpActive.Value = 0;
            }

            if (SMP[0] == EMarketPositionSide.Long)
            {
                if (SMP[1] != EMarketPositionSide.Long)
                {
                    // entry bar
                    // Highest Close Since Long Entry
                    LE_HiCloseSinEnt.Value = Bars.Close[0];
                    LE_HiHighSinEnt.Value = Bars.High[0];
                    LE_Hi_PAL_5_SinEnt.Value = PAL_5[0];

                    // reset these on initial LE
                    parabolicCounter.Value = 1;
                    gradientCounter.Value = 0;
                }

                if (Bars.Close[0] > LE_HiCloseSinEnt[0])
                {
                    LE_HiCloseSinEnt.Value = Bars.Close[0];
                }

                if (Bars.High[0] > LE_HiHighSinEnt[0])
                {
                    LE_HiHighSinEnt.Value = Bars.High[0];
                }

                if (PAL_5[0] > LE_Hi_PAL_5_SinEnt[0])
                {
                    LE_Hi_PAL_5_SinEnt.Value = PAL_5[0];
                }

                LE_riskRewardLevel = (LE_HiHighSinEnt[0] - StrategyInfo.AvgEntryPrice) / LE_riskValue;

                LX_Tgt1_P = StrategyInfo.AvgEntryPrice + (ProfitTarget_1 * LE_riskValue);
                LX_Tgt2_P = StrategyInfo.AvgEntryPrice + (ProfitTarget_2 * LE_riskValue);

                // trailingStopLoss – start of trailing stop loss
                LX_TrlStp_1 = LX_Stp0_P;
                plotStopBefTrl = LX_Stp0_P;

                if (LE_riskRewardLevel >= 1 && SMP[0] == EMarketPositionSide.Long)
                {
                    LX_TrlStpActive.Value = 1;
                }

                if (LX_TrlStpActive[0] == 1)
                {
                    plotStopBefTrl = -1;
                    // use -1 to indicate not active

                    if (LE_Hi_PAL_5_SinEnt[0] > LE_Hi_PAL_5_SinEnt[1])
                    {
                        /*
	                    extremePoint = highestValueSinceEntry
	                    parabolicCounter = parabolicCounter + 1

	                    if accFactorValue < AccLimit then
		                    accFactorValue = (parabolicCounter * mathfloor(riskRewardLevel) * AccFactor)
	                    else
		                    accFactorValue = AccLimit
	
                        LX_trailingStopLoss2 = LX_trailingStopLoss[1] + 
                        ( accFactorValue * (extremePoint – LX_trailingStopLoss[1]) )

                        gradientCounter = 0 // reset gradient counter to 0 or 1 ??
                        plotText(“.”) // size 3
                        ?? Plot, or send data to indicator to plot Stop value, if not trade, black or transparent
                         */
                        double extremePoint = LE_Hi_PAL_5_SinEnt[0];
                        parabolicCounter.Value = parabolicCounter[0] + 1;

                        if (accFactorValue[0] < AccLimit)
                        {
                            accFactorValue.Value = (parabolicCounter[0] * Math.Floor(LE_riskRewardLevel) * AccFactor);
                        }
                        else
                        {
                            accFactorValue.Value = AccLimit;
                        }

                        LX_TrlStp_2 = LX_TrlStp[1]
                            + (accFactorValue[0] * (extremePoint - LX_TrlStp[1]));

                        // reset gradient counter to 0 or 1 ??
                        gradientCounter.Value = 0;
                        // ?? Plot, or send data to indicator to plot Stop value, if not trade, black or transparent
                        // plotText(“.”) // size 3
                        plotStopDot = LX_TrlStp_2;
                        Output.WriteLine("DateTime = {0}, plotStopDot = {1}", Bars.Time[0].ToString(), plotStopDot);
                    }
                    else // if (LE_Hi_PAL_5_SinEnt[0] <= LE_Hi_PAL_5_SinEnt[1])
                    {
                        /*
	                    gradientCounter = gradientCounter + 1
	                    gradientValue = 
                            math.floor(riskRewardLevel) * oneTick * gradientCounter

	                    LX_trailingStopLoss2 = LX_trailingStopLoss[1] + gradientValue
	                    plotText(“-”) // Size 2

                         // reset parabolicCounter
	                    parabolicCounter = 1;
                        */
                        gradientCounter.Value = gradientCounter[0] + 1;

                        gradientValue.Value =
                           Math.Floor(LE_riskRewardLevel) * oneTick * 2 /* gradientCounter[0] */;

                        LX_TrlStp_2 = LX_TrlStp[1] + gradientValue[0];

                        // plotText(“-”) // Size 2
                        plotStopDash = LX_TrlStp_2;

                        // reset parabolicCounter
                        parabolicCounter.Value = 1;
                    }
                }

                // Combine
                LX_TrlStp.Value = Math.Max(LX_TrlStp_1, LX_TrlStp_2);

                // Prevent trailing stop loss from reversing
                // Non-retracing
                if (LX_TrlStp[0] < LX_TrlStp[1] && LX_TrlStp[1] > 0)
                {
                    LX_TrlStp.Value = LX_TrlStp[1];
                }

                LX_Stp_Order = true;

                LX_Stp_P = LX_TrlStp[0];
                LX_Stp_P = HP_RoundPriceTo_f(LX_Stp_P, roundPriceToPts);

                LX_Tgt_Order = true;

            }
            else
            {
                // reset to 0
                LX_TrlStp_1 = 0;
                LX_TrlStp_2 = 0;
                // Important to set LX_TrlStp_3 just before entry
                LX_TrlStp.Value = LX_Stp0_P;
                LX_Stp_Order = true;
                LX_Stp_P = LX_TrlStp[0];
            }
        }

        double SE_EntryRR;
        private double SX_TrlStp_0, SX_TrlStp_1, SX_TrlStp_2;

        private void SX_StpTgt_f()
        {
            // Not completed!

            if (SMP[0] == EMarketPositionSide.Short)
            {
                if (SMP[1] != EMarketPositionSide.Short)
                {
                    // entry bar
                    // Highest Close Since Long Entry
                    SE_LoCloseSinEnt.Value = Bars.Close[0];
                }

                if (Bars.Close[0] < SE_LoCloseSinEnt[0])
                {
                    SE_LoCloseSinEnt.Value = Bars.Close[0];
                }

                SX_TrlStp_1 = SX_Stp0_P;
                double SE_EntryRRL = Math.Abs(SE_LoCloseSinEnt[0] - SX_Stp0_P);


                // Combine
                SX_TrlStp_3.Value = Math.Min(SX_TrlStp_1, SX_TrlStp_2);

                // Prevent trailing stop loss from reversing
                // Non-retracing
                if (SX_TrlStp_3[0] > SX_TrlStp_3[1] && SX_TrlStp_3[1] > 0)
                {
                    SX_TrlStp_3.Value = SX_TrlStp_3[1];
                }

                SX_Stp_Order = true;
                // V205
                // SX_Stp_P = SX_TrlStp_3[0] + BAS_Pts;
                // V206
                SX_Stp_P = SX_TrlStp_3[0];
                SX_Stp_P = HP_RoundPriceTo_f(SX_Stp_P, roundPriceToPts);

                SX_Tgt_Order = true;

                /*
                // After a certain risk reward Level, 
                // exit at marlet if PAC channel is broken through
                if PAC_TrendExit_On = 1
                And PAC_LowerColor[1] = Green
                And PAC_LowerColor<> Green
                And SE_EntryRRL > PAC_TrendExitRRL then
                    Sell ALL Contracts at Market
                */
            }
            else if (SE_Order[0] == true)
            {
                SX_TrlStp_3.Value = SX_Stp0_P + BAS_Pts;
            }
        }

        //----------------------------------------------------------------------
        int isUpTrend_Cnt, isNewLowerStep_Cnt, isRiskSmall_L_Cnt, isStepBigNot_Cnt,
            isExtended_L_Not_Cnt, isConsolidationNot_Cnt, isTradeTime_Cnt;

        private void SendDataToPlotSpecific_f()
        {
            // Send Data To Plot [for the] Specific Strategy 

            // Reserve 1 to 50 for strategy indicator plot
            // send sPos for setting plots to BackgroundColor if SPos = 0
            StrategyInfo.SetPlotValue(7, (double)SPos);

            if (inTestMode || SMP[0] != EMarketPositionSide.Flat)
            {
                if (SMP[0] == EMarketPositionSide.Long)
                {
                    if (display_Profit_On == 1)
                        StrategyInfo.SetPlotValue(1, LX_Tgt_P);

                    if (display_Trailing_On == 1 && LX_Stp_P > 0)
                        StrategyInfo.SetPlotValue(2, LX_Stp_P);
                }
                if (SMP[0] == EMarketPositionSide.Short)
                {
                    if (display_Profit_On == 1)
                        StrategyInfo.SetPlotValue(1, SX_Tgt_P);

                    if (display_Trailing_On == 1 && SX_Stp_P > 0)
                        StrategyInfo.SetPlotValue(2, SX_Stp_P);
                }
                if (display_InitialStop_On == 1)
                {
                    StrategyInfo.SetPlotValue(3, LX_Stp0_P);
                    StrategyInfo.SetPlotValue(4, SX_Stp0_P);
                }
            }
            else // Not to plot if flat by setting them to 0
            {

            }

            //=================================================================
            // How to use SendDataToPlotSpecific_f for testing
            // Send value to an indicator starting from plotIndex =  801 to 999
            // StrategyInfo.SetPlotValue(plotIndex, myValue1);
            // For non-price related plots, use indicator __HP_StrategyInfoPlotValue_Subgraph
            // For price related plots, use indicator __HP_StrategyInfoPlotValue_SameAsSymbol

            //-----------------------------------------------------------------
            // Test count each entry condtion for testing
            // plot the counts. If any one count = 0 or very low, review that condition
            if (inTestMode)
            {
                if (Bars.CurrentBar == 1)
                {
                    isUpTrend_Cnt = 0;
                    isNewLowerStep_Cnt = 0;
                    isRiskSmall_L_Cnt = 0;
                    isStepBigNot_Cnt = 0;
                    isExtended_L_Not_Cnt = 0;
                    isConsolidationNot_Cnt = 0;
                    isTradeTime_Cnt = 0;
                }

                if (isUpTrend[0]) isUpTrend_Cnt = isUpTrend_Cnt + 1;
                if (isNewLowerStep[0]) isNewLowerStep_Cnt = isNewLowerStep_Cnt + 1;
                if (isRiskSmall_L[0]) isRiskSmall_L_Cnt = isRiskSmall_L_Cnt + 1;
                if (isStepBig[0] == false) isStepBigNot_Cnt = isStepBigNot_Cnt + 1;
                if (isTradeTime[0]) isTradeTime_Cnt = isTradeTime_Cnt + 1;

                // plotIndex = 801 to 810 for Entry condition count                
                StrategyInfo.SetPlotValue(801, isUpTrend_Cnt);
                StrategyInfo.SetPlotValue(802, isNewLowerStep_Cnt); // small count
                StrategyInfo.SetPlotValue(803, isRiskSmall_L_Cnt);
                StrategyInfo.SetPlotValue(804, isStepBigNot_Cnt);
                StrategyInfo.SetPlotValue(805, isExtended_L_Not_Cnt);
                StrategyInfo.SetPlotValue(806, isConsolidationNot_Cnt);
                StrategyInfo.SetPlotValue(807, isTradeTime_Cnt);

                //-----------------------------------------------------------------
                // Test each LE subcondition
                // plotIndex = 810 to 820 for test True/false of each LE subcondition
                // LE_Specific = Combined LE Condition for this specific strategy
                StrategyInfo.SetPlotValue(810, 6 * HPSM.TFTo10_f(LE_Specific[0]));
                StrategyInfo.SetPlotValue(811, 1 * HPSM.TFTo10_f(isPALPivotLow[0]));
                StrategyInfo.SetPlotValue(812, 2 * HPSM.TFTo10_f(isUpTrend[0]));
                StrategyInfo.SetPlotValue(813, 3 * HPSM.TFTo10_f(isTradeTime[0]));
                StrategyInfo.SetPlotValue(814, 4 * HPSM.TFTo10_f(isRiskSmall_L[0]));
                StrategyInfo.SetPlotValue(815, 5 * HPSM.TFTo10_f(isPALPvt_L[0]));
                // Sum of LE subcondition. When SumSubLECond = 5, have LE
                int SumSubLECond = 0;
                if (isPALPivotLow[0]) SumSubLECond += 1;
                if (isUpTrend[0]) SumSubLECond += 1;
                if (isTradeTime[0]) SumSubLECond += 1;
                if (isRiskSmall_L[0]) SumSubLECond += 1;
                if (isPALPvt_L[0]) SumSubLECond += 1;
                StrategyInfo.SetPlotValue(816, SumSubLECond);

                //-----------------------------------------------------------------
                // Test LE_Ord_Qty (LE_Order Quantity)
                StrategyInfo.SetPlotValue(821, LE_Ord_Qty);
                StrategyInfo.SetPlotValue(822, checkPosSize);

                //-----------------------------------------------------------------
                // Test LX_Stp0_P, LX_TrlStp_1,  LX_TrlStp_2, LX_Stp_P
                StrategyInfo.SetPlotValue(830, LX_Stp0_P);  // White
                StrategyInfo.SetPlotValue(831, LX_TrlStp_1);    // Yellow
                StrategyInfo.SetPlotValue(832, LX_TrlStp_2);    // Magenta
                StrategyInfo.SetPlotValue(834, LX_Stp_P);   // Red

                //-----------------------------------------------------------------
                // Test Dot and Dash, 
                StrategyInfo.SetPlotValue(835, plotStopDot);    // Cyan
                StrategyInfo.SetPlotValue(836, plotStopDash);   // Magenta
                // initial stop before trailing
                StrategyInfo.SetPlotValue(837, plotStopBefTrl);    // Yellow

                //-----------------------------------------------------------------
                // Test isUpTrend_f
                StrategyInfo.SetPlotValue(841, 4 * HPSM.TFTo10_f(isUpTrend[0]));
                StrategyInfo.SetPlotValue(842, 3 * HPSM.TFTo10_f(Bars.Close[0] > vPALs[4][0]));
                StrategyInfo.SetPlotValue(843, 2 * HPSM.TFTo10_f(Bars.Close[0] > vPALs[4][0]));
                StrategyInfo.SetPlotValue(844, 1 * HPSM.TFTo10_f(vPAC_LowerAct[0]));


                //-----------------------------------------------------------------
            }
        }

        #endregion strategySpecificFunctions

    }

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

}
