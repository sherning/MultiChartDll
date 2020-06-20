//==============================================================================
// Name           : __SherNing_TLTrading
// Description    : Trend Line Trading Strategy.
// Version        : v.1.0.0
// Date Created   : 25 - Apr - 2020
// Time Taken     : 
// Remarks        :
//==============================================================================
// Copyright      : 2020, Sher Ning Technologies           
// License        :      
//==============================================================================

/* ------------------------------- Version 1.0.0 -------------------------------
 * 25/4/2020
 * Design Architecture for Trend Line Trading
 */

using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;
using System.Windows.Forms;
using PowerLanguage.TradeManager;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;

namespace PowerLanguage.Strategy
{
    [SameAsSymbol(true), MouseEvents(true), UpdateOnEveryTick(true), IOGMode(IOGMode.Enabled), RecoverDrawings(false)]
    public class __SherNing_TLTrading : SignalObject
    {
        /// <summary>
        /// if true, print all Output Messages.
        /// </summary>
        private bool DebugMode = true;

        #region Inputs, Constructor and Default Settings
        [Input] public string TL_Settings { get; private set; }
        [Input] public int AccountSize { get; set; }
        [Input] public bool RealAccountBalance { get; set; }
        [Input] public double RiskPerTradeInPercentage { get; set; }
        [Input] public int MaximumContractsPerTrade { get; set; }
        [Input] public int MinimumContractsPerTrade { get; set; }
        [Input] public int StepIncrementPer20_000 { get; set; }
        [Input] public bool TradeConfirmationRequired { get; set; }
        [Input] public string ImportantNotes { get; set; }

        public __SherNing_TLTrading(object _ctx) : base(_ctx)
        {
            // Any changes made to Constructor or Create() need to remove and re-insert.
            TL_Settings = "Settings for TL Trader";
            AccountSize = 100000;
            RealAccountBalance = false;
            RiskPerTradeInPercentage = 2;
            MaximumContractsPerTrade = 10000000;
            MinimumContractsPerTrade = 20000;
            StepIncrementPer20_000 = 1;
            TradeConfirmationRequired = true;
            ImportantNotes = "Please remove CommandLine from View -> ToolBar";
        }
        #endregion


        #region Multichart Override Methods
        protected override void Create()
        {
        }
        protected override void StartCalc()
        {
            Output.Clear();
            IsCodeGenTrendLinesCreated = false;
            CheckInputSettings();
            InitializeToolStripInStartCalc();
        }

        protected override void CalcBar()
        {
            CreateCodeGenTrendLines();
            UpdateTradeInformationInCalcBar();
            ManageStrategyOrders();
            ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
        }

        protected override void StopCalc()
        {
            // when i change charts stop calc is called.
            //Print("StopCalc Method Called.");
        }
        protected override void Destroy()
        {
            RemoveToolBarInDestroy();
            //RemoveTrendLineObjects();
            //Print("Destroy method called.");
        }
        protected override void OnRecalcLastBarAfterEvent()
        {
            this.CalcBar();
        }
        #endregion


        // ---------------------------------------- TL Trader Functions ----------------------------------------//

        #region OnChart Trend Line Objects

        // On Chart Trend Line Fields
        private ITrendLineObject OnChartEntryLine;
        private ITrendLineObject OnChartProfitLine;
        private ITrendLineObject OnChartStopLossLine;
        private bool IsCodeGenTrendLinesCreated = false;

        // On Chart Trend Line Methods
        private void CreateCodeGenTrendLines()
        {
            if (IsCodeGenTrendLinesCreated == true) return;
            if (Bars.LastBarOnChart == false) return;

            // right and left screen time
            DateTime rightScreenTime = Environment.RightScreenTime;
            DateTime leftScreenTime = Environment.LeftScreenTime;

            // top and bottom price
            double highestScreenVal = Environment.HighestScaleValue;
            double lowestScreenVal = Environment.LowestScaleValue;

            // if daily return 1. 5 min return 5
            int barType = (int)Bars.Info.Resolution.Size;

            // time difference across the screen
            TimeSpan timeDiffAcrossScreen = rightScreenTime - leftScreenTime;

            // number of bars across the screen
            int numBarsOnScreen = (int)timeDiffAcrossScreen.TotalMinutes / barType;

            // price diffence across the screen
            double priceDiffAcrossScreen = highestScreenVal - lowestScreenVal;

            // Set the length of the trend lines, rounded up to the nearest bar size.
            DateTime startTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 0.8) * barType);

            DateTime endTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 1.0) * barType);

            // set the height for each trend line
            double entryHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.4);
            double stopLossHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.2);
            double profitHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.6);

            // Entry start and end chart points
            ChartPoint entryStart = new ChartPoint(startTime, entryHeight);
            ChartPoint entryEnd = new ChartPoint(endTime, entryHeight);

            // Entry start and end chart points
            ChartPoint stopLossStart = new ChartPoint(startTime, stopLossHeight);
            ChartPoint stopLossEnd = new ChartPoint(endTime, stopLossHeight);

            // Entry start and end chart points
            ChartPoint profitStart = new ChartPoint(startTime, profitHeight);
            ChartPoint profitEnd = new ChartPoint(endTime, profitHeight);

            // Create the three trend lines.
            if (OnChartEntryLine == null)
                OnChartEntryLine = DrwTrendLine.Create(entryStart, entryEnd);

            if (OnChartStopLossLine == null)
                OnChartStopLossLine = DrwTrendLine.Create(stopLossStart, stopLossEnd);

            if (OnChartProfitLine == null)
                OnChartProfitLine = DrwTrendLine.Create(profitStart, profitEnd);

            // Set initial trend line properties
            OnChartEntryLine.Color = Color.White;
            OnChartStopLossLine.Color = Color.Yellow;
            OnChartProfitLine.Color = Color.Cyan;

            IsCodeGenTrendLinesCreated = true;
            // ---------------------------------------- Debug ---------------------------------------- //
        }
        private void SetToShortPosition()
        {
            // right and left screen time
            DateTime rightScreenTime = Environment.RightScreenTime;
            DateTime leftScreenTime = Environment.LeftScreenTime;

            // top and bottom price
            double highestScreenVal = Environment.HighestScaleValue;
            double lowestScreenVal = Environment.LowestScaleValue;

            // if daily return 1. 5 min return 5
            int barType = (int)Bars.Info.Resolution.Size;

            // time difference across the screen
            TimeSpan timeDiffAcrossScreen = rightScreenTime - leftScreenTime;

            // number of bars across the screen
            int numBarsOnScreen = (int)timeDiffAcrossScreen.TotalMinutes / barType;

            // price diffence across the screen
            double priceDiffAcrossScreen = highestScreenVal - lowestScreenVal;

            // Set the length of the trend lines, rounded up to the nearest bar size.
            DateTime startTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 0.8) * barType);

            DateTime endTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 1.0) * barType);

            // set the height for each trend line
            double entryHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.4);
            double stopLossHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.6);
            double profitHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.2);

            // Entry start and end chart points
            ChartPoint entryStart = new ChartPoint(startTime, entryHeight);
            ChartPoint entryEnd = new ChartPoint(endTime, entryHeight);

            // Entry start and end chart points
            ChartPoint stopLossStart = new ChartPoint(startTime, stopLossHeight);
            ChartPoint stopLossEnd = new ChartPoint(endTime, stopLossHeight);

            // Entry start and end chart points
            ChartPoint profitStart = new ChartPoint(startTime, profitHeight);
            ChartPoint profitEnd = new ChartPoint(endTime, profitHeight);

            // Create the three trend lines.
            if (OnChartEntryLine != null)
            {
                OnChartEntryLine.Begin = entryStart;
                OnChartEntryLine.End = entryEnd;

                if (EntryPriceTL_Button.Text == "Offline") OnChartEntryLine.Color = Color.White;
                else OnChartEntryLine.Color = DisplayOrderButton.BackColor;
            }

            if (OnChartStopLossLine != null)
            {
                OnChartStopLossLine.Begin = stopLossStart;
                OnChartStopLossLine.End = stopLossEnd;
                OnChartStopLossLine.Color = Color.Yellow;
            }

            if (OnChartProfitLine != null)
            {
                OnChartProfitLine.Begin = profitStart;
                OnChartProfitLine.End = profitEnd;
                OnChartProfitLine.Color = Color.Cyan;

            }
        }
        private void SetToLongPosition()
        {
            // right and left screen time
            DateTime rightScreenTime = Environment.RightScreenTime;
            DateTime leftScreenTime = Environment.LeftScreenTime;

            // top and bottom price
            double highestScreenVal = Environment.HighestScaleValue;
            double lowestScreenVal = Environment.LowestScaleValue;

            // if daily return 1. 5 min return 5
            int barType = (int)Bars.Info.Resolution.Size;

            // time difference across the screen
            TimeSpan timeDiffAcrossScreen = rightScreenTime - leftScreenTime;

            // number of bars across the screen
            int numBarsOnScreen = (int)timeDiffAcrossScreen.TotalMinutes / barType;

            // price diffence across the screen
            double priceDiffAcrossScreen = highestScreenVal - lowestScreenVal;

            // Set the length of the trend lines, rounded up to the nearest bar size.
            DateTime startTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 0.8) * barType);

            DateTime endTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 1.0) * barType);

            // set the height for each trend line
            double entryHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.4);
            double stopLossHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.2);
            double profitHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.6);

            // Entry start and end chart points
            ChartPoint entryStart = new ChartPoint(startTime, entryHeight);
            ChartPoint entryEnd = new ChartPoint(endTime, entryHeight);

            // Entry start and end chart points
            ChartPoint stopLossStart = new ChartPoint(startTime, stopLossHeight);
            ChartPoint stopLossEnd = new ChartPoint(endTime, stopLossHeight);

            // Entry start and end chart points
            ChartPoint profitStart = new ChartPoint(startTime, profitHeight);
            ChartPoint profitEnd = new ChartPoint(endTime, profitHeight);

            // Create the three trend lines.
            if (OnChartEntryLine != null)
            {
                OnChartEntryLine.Begin = entryStart;
                OnChartEntryLine.End = entryEnd;
                if (EntryPriceTL_Button.Text == "Offline") OnChartEntryLine.Color = Color.White;
                else OnChartEntryLine.Color = DisplayOrderButton.BackColor;
            }

            if (OnChartStopLossLine != null)
            {
                OnChartStopLossLine.Begin = stopLossStart;
                OnChartStopLossLine.End = stopLossEnd;
                OnChartStopLossLine.Color = Color.Yellow;
            }

            if (OnChartProfitLine != null)
            {
                OnChartProfitLine.Begin = profitStart;
                OnChartProfitLine.End = profitEnd;
                OnChartProfitLine.Color = Color.Cyan;

            }
        }
        private void SetToDefaultPosition()
        {
            // right and left screen time
            DateTime rightScreenTime = Environment.RightScreenTime;
            DateTime leftScreenTime = Environment.LeftScreenTime;

            // top and bottom price
            double highestScreenVal = Environment.HighestScaleValue;
            double lowestScreenVal = Environment.LowestScaleValue;

            // if daily return 1. 5 min return 5
            int barType = (int)Bars.Info.Resolution.Size;

            // time difference across the screen
            TimeSpan timeDiffAcrossScreen = rightScreenTime - leftScreenTime;

            // number of bars across the screen
            int numBarsOnScreen = (int)timeDiffAcrossScreen.TotalMinutes / barType;

            // price diffence across the screen
            double priceDiffAcrossScreen = highestScreenVal - lowestScreenVal;

            // Set the length of the trend lines, rounded up to the nearest bar size.
            DateTime startTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 0.8) * barType);

            DateTime endTime = leftScreenTime
                .AddMinutes(Math.Round(numBarsOnScreen * 1.0) * barType);

            // set the height for each trend line
            double entryHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.4);
            double stopLossHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.2);
            double profitHeight = lowestScreenVal + (priceDiffAcrossScreen * 0.6);

            // Entry start and end chart points
            ChartPoint entryStart = new ChartPoint(startTime, entryHeight);
            ChartPoint entryEnd = new ChartPoint(endTime, entryHeight);

            // Entry start and end chart points
            ChartPoint stopLossStart = new ChartPoint(startTime, stopLossHeight);
            ChartPoint stopLossEnd = new ChartPoint(endTime, stopLossHeight);

            // Entry start and end chart points
            ChartPoint profitStart = new ChartPoint(startTime, profitHeight);
            ChartPoint profitEnd = new ChartPoint(endTime, profitHeight);

            // Create the three trend lines.
            if (OnChartEntryLine != null)
            {
                OnChartEntryLine.Begin = entryStart;
                OnChartEntryLine.End = entryEnd;
                OnChartEntryLine.Color = Color.Gray;
            }

            if (OnChartStopLossLine != null)
            {
                OnChartStopLossLine.Begin = stopLossStart;
                OnChartStopLossLine.End = stopLossEnd;
                OnChartStopLossLine.Color = Color.Yellow;
            }

            if (OnChartProfitLine != null)
            {
                OnChartProfitLine.Begin = profitStart;
                OnChartProfitLine.End = profitEnd;
                OnChartProfitLine.Color = Color.Cyan;

            }
        }
        private void RemoveTrendLineObjects()
        {
            if (OnChartEntryLine != null) OnChartEntryLine.Delete();
            if (OnChartStopLossLine != null) OnChartStopLossLine.Delete();
            if (OnChartProfitLine != null) OnChartProfitLine.Delete();
        }

        #endregion


        #region Create Toolstrip

        #region Tool Strip Fields
        // ------------------------------- Fields ------------------------------- //
        // Create flag to track buy sell or flat
        private int TradeMode;
        private bool IsToolStripCreated = false;

        // Create Drop down button and drop down menu
        private ToolStripDropDownButton DisplayOrderButton;
        private ToolStripDropDown OrderDropDownMenu;

        // Create Buy Buttons for drop down menu
        private ToolStripButton BuyLimitButton, BuyStopButton, BuyMarketButton;

        // Create Sell Buttons for drop down menu
        private ToolStripButton SellLimitButton, SellStopButton, SellMarketButton;

        // Create Flat Button
        private ToolStripButton FlatButton, CloseAllPositionButton;

        // Risk Management Items
        private ToolStripButton AutoPositionButton;
        private ToolStripLabel RiskLabel;
        private ToolStripControlHost SliderPanel;
        private TrackBar RiskSlider;
        private ToolStripTextBox ContractTextBox;
        private ToolStripLabel EnterContractsLabel;

        // Risk Management Fields
        private bool IsAutoPosOn = false;
        private int SliderLastValue;
        private double SliderValue;
        private bool NonNumberEntered = false;

        // Create StopLoss TL Button
        private ToolStripButton EntryPriceTL_Button, StopLossTL_Button, ProfitTL_Button;
        private bool IsEntryPriceButtonClicked, IsStopLossTL_Button_Clicked, IsProfitTL_Button_Clicked;

        // Display Trade Information
        private ToolStripButton DisplayStatusLabel, RiskRewardLabel, ProfitAndLossLabel;

        // Lock ToolBar
        private ToolStripButton LockToolBarButton;
        private bool IsToolBarLocked = false;

        // update stoploss value, close value and profit value.
        private bool IsDisplayEntryPriceInfo, IsDisplayStopLossInfo, IsDisplayProfitInfo;

        // Track list of toolstrips item to lock and unlock
        private List<ToolStripItem> ListOfToolBarObjects;

        // ------------------------------- Fields ------------------------------- //
        #endregion

        #region ToolStrip Objects Creation
        // ------------------------------- ToolStrip Creation ------------------------------- //
        // InstantiateAllToolStripObjects
        private void InitializeToolStripInStartCalc()
        {
            // make sure toolstrip has not been created.
            if (IsToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(InitializedState);
                IsToolStripCreated = true;
            }
        }
        private void CreateNewInstancesOfToolStripItems()
        {
            // to prevent cross function null exception.
            OrderDropDownMenu = new ToolStripDropDown();
            DisplayOrderButton = new ToolStripDropDownButton();
            BuyLimitButton = new ToolStripButton();
            BuyStopButton = new ToolStripButton();
            BuyMarketButton = new ToolStripButton();
            SellLimitButton = new ToolStripButton();
            SellStopButton = new ToolStripButton();
            SellMarketButton = new ToolStripButton();
            FlatButton = new ToolStripButton();
            CloseAllPositionButton = new ToolStripButton();
            AutoPositionButton = new ToolStripButton();
            RiskSlider = new TrackBar();
            SliderPanel = new ToolStripControlHost(RiskSlider);
            RiskLabel = new ToolStripLabel();
            EnterContractsLabel = new ToolStripLabel();
            ContractTextBox = new ToolStripTextBox();
            StopLossTL_Button = new ToolStripButton();
            ProfitTL_Button = new ToolStripButton();
            EntryPriceTL_Button = new ToolStripButton();
            DisplayStatusLabel = new ToolStripButton();
            RiskRewardLabel = new ToolStripButton();
            ProfitAndLossLabel = new ToolStripButton();
            LockToolBarButton = new ToolStripButton();
            ListOfToolBarObjects = new List<ToolStripItem>();
        }
        private void InitializedState(ToolStrip tb)
        {
            // Building tool bar must FOLLOW the order sequentially.

            // Set Toolstrip Properties
            tb.AutoSize = false;
            tb.Height = 80;
            tb.TabIndex = 0;

            // Create tool bar features here, in order of tool bar display
            CreateNewInstancesOfToolStripItems();

            // Create each module
            CreateRiskManagementModule(tb);
            CreateTradeOrdersDropDownMenu(tb);
            CreateEntryPriceButton(tb);
            CreateStopLossButton(tb);
            CreateProfitButton(tb);
            CreateTradeInformationLabels(tb);
            CreateLockToolBarButton(tb);
        }
        // ------------------------------- ToolStrip Creation ------------------------------- //
        #endregion

        #region Drop Down Module
        private void CreateTradeOrdersDropDownMenu(ToolStrip tb)
        {
            // Create a label
            ToolStripLabel label = new ToolStripLabel();
            label.Text = "Trade Mode: ";
            AddNewItemToToolStrip(tb, label);

            // Set Dropdown Menu Properties
            DisplayOrderButton.Text = "     Flat     ";
            DisplayOrderButton.AutoSize = true;
            DisplayOrderButton.BackColor = Color.DarkSlateGray;
            DisplayOrderButton.ForeColor = Color.White;
            DisplayOrderButton.DropDown = OrderDropDownMenu;
            DisplayOrderButton.DropDownDirection = ToolStripDropDownDirection.BelowRight;
            DisplayOrderButton.ShowDropDownArrow = false;

            // Create Buy Orders
            CreateBuyLimitButton();
            CreateBuyStopButton();
            CreateBuyMarketButton();

            // Create Sell Orders
            CreateSellLimitButton();
            CreateSellStopButton();
            CreateSellMarketButton();

            // Create Flat Button and ExitAll Button
            CreateFlatButton();
            CreateCloseAllOpenPosition();

            // Add all the buttons to dropdown menu
            ToolStripItem[] orderCollections = new ToolStripItem[]
            {
                BuyLimitButton, BuyStopButton, BuyMarketButton,
                SellLimitButton, SellStopButton, SellMarketButton,
                FlatButton, CloseAllPositionButton
            };

            OrderDropDownMenu.Items.AddRange(orderCollections);
            AddNewItemToFunctionalObjectList(DisplayOrderButton);
            AddNewItemToToolStrip(tb, DisplayOrderButton);

            // Create a separator for the next button
            ToolStripSeparator separator = new ToolStripSeparator();
            AddNewItemToToolStrip(tb, separator);
        }

        #region Drop Down Module - Buy Buttons
        private void CreateBuyLimitButton()
        {
            // Create Buy Limit Button
            BuyLimitButton.Text = "Buy Limit";
            BuyLimitButton.BackColor = Color.LimeGreen;
            BuyLimitButton.Click += delegate
            {
                int prevTradeMode = TradeMode;
                if (prevTradeMode <= 0) SetToLongPosition();

                DisplayOrderButton.Text = "Buy Limit";
                DisplayOrderButton.BackColor = Color.LimeGreen;
                TradeMode = 1;
            };
        }
        private void CreateBuyStopButton()
        {
            // Create Buy Stop Button
            BuyStopButton.Text = "Buy Stop";
            BuyStopButton.BackColor = Color.DarkGreen;
            BuyStopButton.ForeColor = Color.White;
            BuyStopButton.Click += delegate
            {
                int prevTradeMode = TradeMode;
                if (prevTradeMode <= 0) SetToLongPosition();

                DisplayOrderButton.Text = "Buy Stop";
                DisplayOrderButton.BackColor = Color.DarkGreen;
                DisplayOrderButton.ForeColor = Color.White;
                TradeMode = 2;
            };
        }
        private void CreateBuyMarketButton()
        {
            BuyMarketButton.Text = "Buy Mkt ";
            BuyMarketButton.BackColor = Color.SeaGreen;
            BuyMarketButton.ForeColor = Color.White;

            // Button functionality
            BuyMarketButton.Click += delegate
            {
                DisplayOrderButton.Text = "Buy Mkt ";
                DisplayOrderButton.BackColor = Color.SeaGreen;
                DisplayOrderButton.ForeColor = Color.White;

                // must evaluate both conditions, prevent short-circuit evaluation.
                if (BuyOrderCheck() == false | IsMinimumNumOfContracts() == false) 
                    return;

                DialogResult results = MessageBox.Show(
                    string.Format("Warning! Orders will be executed immediately." +
                    "\nBuy Market {0} contracts @ {1}? ", Bars.CloseValue, ContractTextBox.Text),
                    Name.ToString(), MessageBoxButtons.OKCancel);

                if (results == DialogResult.OK)
                {
                    MessageBox.Show("Buy Market Order Executed");
                    DisplayOrderButton.Text = "     Flat     ";
                    DisplayOrderButton.BackColor = Color.DarkSlateGray;
                    DisplayOrderButton.ForeColor = Color.White;
                    SetToDefaultPosition();
                }
                else
                {
                    DisplayOrderButton.Text = "     Flat     ";
                    DisplayOrderButton.BackColor = Color.DarkSlateGray;
                    DisplayOrderButton.ForeColor = Color.White;
                }
            };
        }
        #endregion

        #region Drop Down Module - Sell Buttons
        private void CreateSellLimitButton()
        {
            // Create Sell Limit Button
            SellLimitButton.Text = "Sell Limit";
            SellLimitButton.BackColor = Color.Red;
            SellLimitButton.ForeColor = Color.White;
            SellLimitButton.Click += delegate
            {
                int prevTradeMode = TradeMode;
                if (prevTradeMode >= 0) SetToShortPosition();

                DisplayOrderButton.Text = "Sell Limit";
                DisplayOrderButton.BackColor = Color.Red;
                DisplayOrderButton.ForeColor = Color.White;
                TradeMode = -1;
            };
        }
        private void CreateSellStopButton()
        {
            SellStopButton.Text = "Sell Stop";
            SellStopButton.BackColor = Color.DarkRed;
            SellStopButton.ForeColor = Color.White;
            SellStopButton.Click += delegate
            {
                int prevTradeMode = TradeMode;
                if (prevTradeMode >= 0) SetToShortPosition();

                DisplayOrderButton.Text = "Sell Stop";
                DisplayOrderButton.BackColor = Color.DarkRed;
                DisplayOrderButton.ForeColor = Color.White;
                TradeMode = -2;
            };
        }
        private void CreateSellMarketButton()
        {
            SellMarketButton.Text = "Sell Mkt ";
            SellMarketButton.BackColor = Color.IndianRed;
            SellMarketButton.ForeColor = Color.White;
            SellMarketButton.Click += delegate
            {
                DisplayOrderButton.Text = "Sell Mkt ";
                DisplayOrderButton.BackColor = Color.IndianRed;
                DisplayOrderButton.ForeColor = Color.White;

                // must evaluate both conditions, prevent short-circuit evaluation.
                if (SellOrderCheck() == false | IsMinimumNumOfContracts() == false) return;

                DialogResult results = MessageBox.Show(
                    string.Format("Warning! Orders will be executed immediately." +
                    "\nSell Market {0} contracts @ {1}? ", Bars.CloseValue, ContractTextBox.Text),
                    Name.ToString(), MessageBoxButtons.OKCancel);

                if (results == DialogResult.OK)
                {
                    MessageBox.Show("Sell Market Order Executed");
                    DisplayOrderButton.Text = "     Flat     ";
                    DisplayOrderButton.BackColor = Color.DarkSlateGray;
                    DisplayOrderButton.ForeColor = Color.White;
                    SetToDefaultPosition();
                }
                else
                {
                    DisplayOrderButton.Text = "     Flat     ";
                    DisplayOrderButton.BackColor = Color.DarkSlateGray;
                    DisplayOrderButton.ForeColor = Color.White;
                }
            };
        }
        #endregion

        #region Drop Down Module - Misc Buttons
        private void CreateFlatButton()
        {
            FlatButton.Text = "     Flat    ";
            FlatButton.BackColor = Color.DarkSlateGray;
            FlatButton.ForeColor = Color.White;
            FlatButton.Click += delegate
            {
                TradeMode = 0;
                DisplayOrderButton.Text = "     Flat     ";
                DisplayOrderButton.BackColor = Color.DarkSlateGray;
                DisplayOrderButton.ForeColor = Color.White;
            };
        }
        private void CreateCloseAllOpenPosition()
        {
            CloseAllPositionButton.Text = "  Exit All  ";
            CloseAllPositionButton.BackColor = Color.Yellow;
            CloseAllPositionButton.ForeColor = Color.Black;
            CloseAllPositionButton.Click += new EventHandler(delegate
            {
                DialogResult result = MessageBox.Show("Warning! You are about to close all open positions.",
                    Name.ToString(), MessageBoxButtons.OKCancel);

                if (result == DialogResult.OK)
                {
                    MessageBox.Show("Closed all open positions");
                    DisplayOrderButton.Text = "     Flat     ";
                    DisplayOrderButton.BackColor = Color.DarkSlateGray;
                    DisplayOrderButton.ForeColor = Color.White;
                }
                else
                {
                    DisplayOrderButton.Text = "     Flat     ";
                    DisplayOrderButton.BackColor = Color.DarkSlateGray;
                    DisplayOrderButton.ForeColor = Color.White;
                }
            });
        }
        #endregion

        #endregion

        #region Risk Management Module
        private bool IsMinimumNumOfContracts()
        {
            // ensure that the minimum contract to trade for FX is 20_000
            int contracts = int.Parse(ContractTextBox.Text);
            if (contracts < 20000)
            {
                MessageBox.Show("Please set a contract size of 20,000 or greater");
                return false;
            }

            return true;
        }
        private void CreateRiskManagementModule(ToolStrip tb)
        {
            // setup items
            ToolStripLabel label = new ToolStripLabel();
            label.Text = "Risk Mode: ";
            AddNewItemToToolStrip(tb, label);

            // setup autoposition button
            AutoPositionButton.Text = "Auto";
            AutoPositionButton.BackColor = Color.Turquoise;
            AutoPositionButton.ForeColor = Color.Black;

            AddNewItemToFunctionalObjectList(AutoPositionButton);
            AddNewItemToToolStrip(tb, AutoPositionButton);

            // add items to main tool strip
            AddNewItemToFunctionalObjectList(SliderPanel);
            AddNewItemToToolStrip(tb, RiskLabel);
            AddNewItemToToolStrip(tb, SliderPanel);

            // Create text box
            CreateContractBox(tb);

            // start in auto mode (before any buttons pressed.)
            RiskLabel.Text = "Risk: 2.0 %";
            ContractTextBox.Enabled = false;
            ContractTextBox.Text = "100000";

            RiskSlider.Dock = DockStyle.Fill;
            SliderLastValue = 1;
            RiskSlider.Width = 200;
            RiskSlider.Maximum = 10;
            RiskSlider.Minimum = 1;
            RiskSlider.SmallChange = 1;
            RiskSlider.Value = 10;
            RiskSlider.ValueChanged += (sender, e) =>
            {
                TrackBar trackBar = sender as TrackBar;
                double sliderValue = trackBar.Value * (RiskPerTradeInPercentage / RiskSlider.Maximum);
                SliderValue = sliderValue;
                SliderLastValue = trackBar.Value;

                // if slider value is a whole number
                if ((sliderValue - Math.Truncate(sliderValue)) == 0.0)
                {
                    RiskLabel.Text = string.Format("Risk: {0}.0 %", sliderValue);
                }
                else
                {
                    RiskLabel.Text = string.Format("Risk: {0} %", sliderValue);
                }
            };

            // automode disable all Entry market orders.
            BuyMarketButton.Enabled = false;
            SellMarketButton.Enabled = false;

            AutoPositionButton.Click += ToogleRiskOrContractMode;

            // Create a Separator at the end
            ToolStripSeparator separator = new ToolStripSeparator();
            AddNewItemToToolStrip(tb, separator);
        }
        private void ToogleRiskOrContractMode(object obj, EventArgs arg)
        {
            if (IsAutoPosOn == true)
            {
                IsAutoPosOn = false;

                AutoPositionButton.Text = "Auto";
                AutoPositionButton.BackColor = Color.Turquoise;
                AutoPositionButton.ForeColor = Color.Black;

                // change contract text box properties
                ContractTextBox.Enabled = false;
                RiskSlider.Enabled = true;

                // disable buy market orders
                BuyMarketButton.Enabled = false;
                SellMarketButton.Enabled = false;

                // Change risk / contract slider settings
                RiskSlider.Maximum = 10;
                RiskSlider.Minimum = 1;
                RiskSlider.Value = SliderLastValue;
                RiskSlider.SmallChange = 1;

                // if slider value is a whole number
                if ((SliderValue - Math.Truncate(SliderValue)) == 0.0)
                {
                    RiskLabel.Text = string.Format("Risk: {0}.0 %", SliderValue);
                }
                else
                {
                    RiskLabel.Text = string.Format("Risk: {0} %", SliderValue);
                }

                RiskSlider.ValueChanged += (sender, e) =>
                {
                    TrackBar trackBar = sender as TrackBar;
                    double sliderValue = trackBar.Value * (RiskPerTradeInPercentage / RiskSlider.Maximum);
                    SliderValue = sliderValue;
                    SliderLastValue = trackBar.Value;

                    // if slider value is a whole number
                    if ((sliderValue - Math.Truncate(sliderValue)) == 0.0)
                    {
                        RiskLabel.Text = string.Format("Risk: {0}.0 %", sliderValue);
                    }
                    else
                    {
                        RiskLabel.Text = string.Format("Risk: {0} %", sliderValue);
                    }
                };
            }
            else
            {
                // Enter Manual Mode
                IsAutoPosOn = true;

                AutoPositionButton.Text = "Manual";
                AutoPositionButton.BackColor = Color.DarkMagenta;
                AutoPositionButton.ForeColor = Color.White;

                // change contract text box properties
                ContractTextBox.Enabled = true;
                RiskSlider.Enabled = false;

                // Set focus on Contract Box
                ContractTextBox.Select();

                // Enable buy market orders
                BuyMarketButton.Enabled = true;
                SellMarketButton.Enabled = true;
            }
        }
        private void CreateContractBox(ToolStrip tb)
        {
            // Create a label
            EnterContractsLabel.Text = "Contracts: ";
            AddNewItemToToolStrip(tb, EnterContractsLabel);

            ContractTextBox.Text = "100000";
            ContractTextBox.AcceptsReturn = false;
            ContractTextBox.AcceptsTab = false;
            ContractTextBox.AutoSize = false;
            ContractTextBox.Width = 200;
            ContractTextBox.ShortcutsEnabled = false;
            ContractTextBox.HideSelection = true;
            ContractTextBox.Select();

            ContractTextBox.KeyPress += (sender, e) =>
            {
                if (NonNumberEntered == true) e.Handled = true;
            };

            // Check if only numbers are entered into the textbox
            ContractTextBox.KeyDown += (sender, e) =>
            {
                NonNumberEntered = false;

                // Determine whether the keystroke is a number from the top of the keyboard.
                if (e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9)
                {
                    // Determine whether the keystroke is a number from the keypad.
                    if (e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9)
                    {
                        // Determine whether the keystroke is a backspace.
                        if (e.KeyCode != Keys.Back)
                        {
                            // A non-numerical keystroke was pressed.
                            // Set the flag to true and evaluate in KeyPress event.
                            NonNumberEntered = true;
                        }
                    }
                }

                //If shift key was pressed, it's not a number.
                if (Control.ModifierKeys == Keys.Shift)
                {
                    NonNumberEntered = true;
                }
            };

            AddNewItemToFunctionalObjectList(ContractTextBox);
            AddNewItemToToolStrip(tb, ContractTextBox);
        }
        #endregion

        #region Trend Lines Activation Module
        private void CreateStopLossButton(ToolStrip tb)
        {
            // Create a label
            ToolStripLabel label = new ToolStripLabel();
            label.Text = "Stop Loss: ";
            AddNewItemToToolStrip(tb, label);

            // Create and set the properties
            StopLossTL_Button.Text = "Offline";
            StopLossTL_Button.BackColor = Color.DarkGray;
            StopLossTL_Button.ForeColor = Color.White;
            StopLossTL_Button.Click += delegate
            {
                if (IsStopLossTL_Button_Clicked == false)
                {
                    StopLossTL_Button.BackColor = Color.Yellow;
                    StopLossTL_Button.ForeColor = Color.Black;
                    IsStopLossTL_Button_Clicked = true;
                    OnChartStopLossLine.ExtRight = true;
                    IsDisplayStopLossInfo = true;
                    OnChartStopLossLine.Locked = true;
                }
                else
                {
                    StopLossTL_Button.Text = "Offline";
                    StopLossTL_Button.BackColor = Color.DarkGray;
                    StopLossTL_Button.ForeColor = Color.White;
                    IsStopLossTL_Button_Clicked = false;
                    OnChartStopLossLine.ExtRight = false;
                    IsDisplayStopLossInfo = false;
                    OnChartStopLossLine.Locked = false;
                }
            };
            AddNewItemToFunctionalObjectList(StopLossTL_Button);
            AddNewItemToToolStrip(tb, StopLossTL_Button);

            // Create a Separator
            ToolStripSeparator separator = new ToolStripSeparator();
            AddNewItemToToolStrip(tb, separator);
        }
        private void CreateProfitButton(ToolStrip tb)
        {
            // Create a label
            ToolStripLabel label = new ToolStripLabel();
            label.Text = "Profit Target: ";
            AddNewItemToToolStrip(tb, label);

            // Create and set the properties
            ProfitTL_Button.Text = "Offline";
            ProfitTL_Button.BackColor = Color.DarkGray;
            ProfitTL_Button.ForeColor = Color.White;
            ProfitTL_Button.Click += delegate
            {
                if (IsProfitTL_Button_Clicked == false)
                {
                    ProfitTL_Button.BackColor = Color.Cyan;
                    ProfitTL_Button.ForeColor = Color.Black;
                    IsProfitTL_Button_Clicked = true;
                    OnChartProfitLine.ExtRight = true;
                    IsDisplayProfitInfo = true;
                    OnChartProfitLine.Locked = true;
                }
                else
                {
                    ProfitTL_Button.Text = "Offline";
                    ProfitTL_Button.BackColor = Color.DarkGray;
                    ProfitTL_Button.ForeColor = Color.White;
                    IsProfitTL_Button_Clicked = false;
                    OnChartProfitLine.ExtRight = false;
                    IsDisplayProfitInfo = false;
                    OnChartProfitLine.Locked = false;
                }
            };
            AddNewItemToFunctionalObjectList(ProfitTL_Button);
            AddNewItemToToolStrip(tb, ProfitTL_Button);

            // Create a Separator
            ToolStripSeparator separator = new ToolStripSeparator();
            AddNewItemToToolStrip(tb, separator);
        }
        private void CreateEntryPriceButton(ToolStrip tb)
        {
            // Create a label
            ToolStripLabel label = new ToolStripLabel();
            label.Text = "Entry Price: ";
            AddNewItemToToolStrip(tb, label);

            // Create a price label
            IsDisplayEntryPriceInfo = false;
            EntryPriceTL_Button.Text = "Offline";
            EntryPriceTL_Button.BackColor = Color.DarkGray;
            EntryPriceTL_Button.ForeColor = Color.White;
            EntryPriceTL_Button.Click += delegate
            {
                if (IsEntryPriceButtonClicked == false)
                {
                    IsDisplayEntryPriceInfo = true;
                    EntryPriceTL_Button.BackColor = OnChartEntryLine.Color;
                    EntryPriceTL_Button.ForeColor = Color.Black;
                    ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
                    IsEntryPriceButtonClicked = true;
                    OnChartEntryLine.ExtRight = true;
                    OnChartEntryLine.Locked = true;
                }
                else
                {
                    IsDisplayEntryPriceInfo = false;
                    EntryPriceTL_Button.Text = "Offline";
                    EntryPriceTL_Button.BackColor = Color.DarkGray;
                    EntryPriceTL_Button.ForeColor = Color.White;
                    OnChartEntryLine.Color = Color.White;
                    IsEntryPriceButtonClicked = false;
                    OnChartEntryLine.ExtRight = false;
                    OnChartEntryLine.Locked = false;
                }
            };

            AddNewItemToFunctionalObjectList(EntryPriceTL_Button);
            AddNewItemToToolStrip(tb, EntryPriceTL_Button);
        }

        #endregion

        #region Update Trendline and Trade Info + LockToolBar
        private void CreateTradeInformationLabels(ToolStrip tb)
        {
            ToolStripLabel label2 = new ToolStripLabel();
            label2.Text = "Real-Time: ";
            AddNewItemToToolStrip(tb, label2);

            AddNewItemToToolStrip(tb, DisplayStatusLabel);

            if (Environment.IsRealTimeCalc == true)
            {
                DisplayStatusLabel.Text = "Real-Time";
                DisplayStatusLabel.BackColor = Color.LimeGreen;
                DisplayStatusLabel.ForeColor = Color.Black;
            }
            else
            {
                DisplayStatusLabel.Text = "Offline";
                DisplayStatusLabel.BackColor = Color.Red;
                DisplayStatusLabel.ForeColor = Color.White;
            }

            ToolStripLabel label3 = new ToolStripLabel();
            label3.Text = "Risk-Reward: ";
            AddNewItemToToolStrip(tb, label3);

            // add risk reward label to toolstrip.
            RiskRewardLabel.Text = "0.00";
            AddNewItemToToolStrip(tb, RiskRewardLabel);

            ToolStripLabel label4 = new ToolStripLabel();
            label4.Text = "P&L: ";
            AddNewItemToToolStrip(tb, label4);

            // add profit and loss label to toolstrip
            ProfitAndLossLabel.Text = "$0.00";
            ProfitAndLossLabel.BackColor = Color.DarkGray;
            ProfitAndLossLabel.ForeColor = Color.White;
            AddNewItemToToolStrip(tb, ProfitAndLossLabel);
        }
        private void CreateLockToolBarButton(ToolStrip tb)
        {
            // Create a label
            ToolStripLabel label = new ToolStripLabel();
            label.Text = "ToolBar Status: ";
            AddNewItemToToolStrip(tb, label);

            LockToolBarButton.Text = "Unlocked";
            LockToolBarButton.BackColor = Color.DeepSkyBlue;
            LockToolBarButton.ForeColor = Color.White;
            LockToolBarButton.Click += delegate
            {
                if (IsToolBarLocked == false)
                {
                    LockToolBarButton.Text = "Locked";
                    LockToolBarButton.BackColor = Color.IndianRed;
                    LockToolBarButton.ForeColor = Color.White;

                    foreach (ToolStripItem item in ListOfToolBarObjects) item.Enabled = false;

                    IsToolBarLocked = true;
                }
                else
                {
                    LockToolBarButton.Text = "Unlocked";
                    LockToolBarButton.BackColor = Color.DeepSkyBlue;
                    LockToolBarButton.ForeColor = Color.White;

                    foreach (ToolStripItem item in ListOfToolBarObjects) item.Enabled = true;

                    IsToolBarLocked = false;
                }
            };

            AddNewItemToToolStrip(tb, LockToolBarButton);
        }
        private double TrendLineEntryPrice()
        {
            if (OnChartEntryLine == null || Bars.LastBarOnChart == false) return 0;
            return OnChartEntryLine.PriceValue(Bars.TimeValue);
        }
        private double TrendLineStopLossPrice()
        {
            if (OnChartStopLossLine == null || Bars.LastBarOnChart == false) return 0;
            return OnChartStopLossLine.PriceValue(Bars.TimeValue);
        }
        private double TrendLineProfitPrice()
        {
            if (OnChartProfitLine == null || Bars.LastBarOnChart == false) return 0;
            return OnChartProfitLine.PriceValue(Bars.TimeValue);
        }
        private int AutoPositionContracts()
        {
            double entryPrice = TrendLineEntryPrice();
            double stopLossPrice = TrendLineStopLossPrice();

            if (entryPrice <= 0 || stopLossPrice <= 0 || Bars.LastBarOnChart == false) return 0;

            int contracts;
            if (AutoPositionButton.Text == "Auto")
            {
                contracts = AutoPositionSizing(AccountSize, SliderValue,
                    entryPrice, stopLossPrice, 20000, MaximumContractsPerTrade, 20000, 0);

                // update the contract text box only in auto mode.
                ContractTextBox.Text = contracts.ToString();
            }
            else
            {
                contracts = int.Parse(ContractTextBox.Text);
            }

            return contracts;
        }
        private double PotentialProfits(int contracts, double profitValue)
        {
            // calculate potential profits for a standard lot size of 100_000 FX
            int standardLotSize = 100000;
            double onePip = GetTickValue(10);
            return PipValue * (contracts / standardLotSize) * (profitValue / onePip);
        }
        private void UpdateTradeInformationInCalcBar()
        {
            // must be last bar on chart.
            if (Bars.LastBarOnChart == false) return;
            if (OnChartEntryLine == null || OnChartStopLossLine == null || OnChartProfitLine == null) return;

            double entryPrice = TrendLineEntryPrice();
            double stopLossPrice = TrendLineStopLossPrice();
            double profitPrice = TrendLineProfitPrice();
            int contracts = AutoPositionContracts();

            double riskValue = 0;
            double profitsValue = 0;

            #region Display Trend Line Info on Tool Strip
            // Display Entry Price Info
            if (IsDisplayEntryPriceInfo == true)
            {
                EntryPriceTL_Button.Text = DisplayDouble(entryPrice);

                if (DisplayOrderButton.Text == "     Flat     ")
                {
                    OnChartEntryLine.Color = Color.White;
                }
                else
                {
                    OnChartEntryLine.Color = DisplayOrderButton.BackColor;
                }

                EntryPriceTL_Button.BackColor = DisplayOrderButton.BackColor;
                EntryPriceTL_Button.ForeColor = DisplayOrderButton.ForeColor;
            }

            // Display Stop Loss Price Info
            if (IsDisplayStopLossInfo == true)
                StopLossTL_Button.Text = DisplayDouble(stopLossPrice);

            // Display Profit Price Info
            if (IsDisplayProfitInfo == true)
                ProfitTL_Button.Text = DisplayDouble(profitPrice);
            #endregion

            // for long trades.
            if (DisplayOrderButton.Text == "Buy Limit" || DisplayOrderButton.Text == "Buy Stop")
            {
                riskValue = entryPrice - stopLossPrice;
                profitsValue = profitPrice - entryPrice;
            }

            // for short trades
            if (DisplayOrderButton.Text == "Sell Limit" || DisplayOrderButton.Text == "Sell Stop")
            {
                riskValue = stopLossPrice - entryPrice;
                profitsValue = entryPrice - profitPrice;
            }

            #region Profit Potential
            // Display Profit Potential on ToolStrip
            if (profitsValue > 0)
            {
                double potentialProfits = PotentialProfits(contracts, profitsValue);
                ProfitAndLossLabel.Text = string.Format("$ " + DisplayDouble(potentialProfits, 2));
                ProfitAndLossLabel.BackColor = Color.LightSkyBlue;
                ProfitAndLossLabel.ForeColor = Color.Black;
            }
            else
            {
                ProfitAndLossLabel.Text = "$0.00";
                ProfitAndLossLabel.BackColor = Color.White;
                ProfitAndLossLabel.ForeColor = Color.Black;
            }

            #endregion

            #region Risk Reward Ratio
            // Display Risk Reward Ratio on ToolStrip
            if (riskValue > 0 && profitsValue > 0)
            {
                double riskRewardRatio = profitsValue / riskValue;
                RiskRewardLabel.Text = DisplayDouble(riskRewardRatio, 2);

                if (riskRewardRatio < 1.0)
                {
                    RiskRewardLabel.BackColor = Color.Red;
                    RiskRewardLabel.ForeColor = Color.White;
                }
                else if (riskRewardRatio >= 1.0 && riskRewardRatio < 2.0)
                {
                    RiskRewardLabel.BackColor = Color.Turquoise;
                    RiskRewardLabel.ForeColor = Color.Black;
                }
                else
                {
                    RiskRewardLabel.BackColor = Color.Cyan;
                    RiskRewardLabel.ForeColor = Color.Black;
                }
            }
            else
            {
                RiskRewardLabel.Text = "0.00";
                RiskRewardLabel.BackColor = Color.DarkGray;
                RiskRewardLabel.ForeColor = Color.White;
            }

            #endregion
        }
        #endregion

        #region Other Misc ToolStip Functions
        private void AddNewItemToToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }
        private void AddNewItemToFunctionalObjectList(ToolStripItem item)
        {
            // This function is to for locking and unlocking functional buttons.
            ListOfToolBarObjects.Add(item);
        }
        private void RemoveToolBarInDestroy()
        {
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

        private void ResetAllButtons()
        {
            // Trade Order Button Reset
            DisplayOrderButton.Text = "     Flat     ";
            DisplayOrderButton.BackColor = Color.DarkSlateGray;
            DisplayOrderButton.ForeColor = Color.White;

            // Stoploss Activation Button Reset
            StopLossTL_Button.Text = "Offline";
            StopLossTL_Button.BackColor = Color.DarkGray;
            StopLossTL_Button.ForeColor = Color.White;
            IsStopLossTL_Button_Clicked = false;
            OnChartStopLossLine.ExtRight = false;
            IsDisplayStopLossInfo = false;
            OnChartStopLossLine.Locked = false;

            // Profit Activation Button Reset
            ProfitTL_Button.Text = "Offline";
            ProfitTL_Button.BackColor = Color.DarkGray;
            ProfitTL_Button.ForeColor = Color.White;
            IsProfitTL_Button_Clicked = false;
            OnChartProfitLine.ExtRight = false;
            IsDisplayProfitInfo = false;
            OnChartProfitLine.Locked = false;

            // Entry Activation Button Reset
            IsDisplayEntryPriceInfo = false;
            EntryPriceTL_Button.Text = "Offline";
            EntryPriceTL_Button.BackColor = Color.DarkGray;
            EntryPriceTL_Button.ForeColor = Color.White;
            OnChartEntryLine.Color = Color.White;
            IsEntryPriceButtonClicked = false;
            OnChartEntryLine.ExtRight = false;
            OnChartEntryLine.Locked = false;

        }
        #endregion


        #region Strategy Generate Orders

        // not sure if strategy orders are sent on every tick, or every bar or only once.
        private double EntryPrice;
        private void ManageStrategyOrders()
        {
            if (Bars.LastBarOnChart == false || Environment.IsRealTimeCalc == false) return;
            SendEntryOrder();
            SendStopLossOrders();
            SendProfitOrder();
        }
        private void SendStopLossOrders()
        {
            // if = 0, means there is no entry price, no need to send any orders
            if (EntryPrice == 0) return;

            double stopLossPrice;
            if (StopLossTL_Button.Text == "Offline") return;
            else stopLossPrice = double.Parse(StopLossTL_Button.Text);

            string tradeDirection;

            if (EntryPrice - stopLossPrice > 0)
                tradeDirection = "Long";
            else
                tradeDirection = "Short";

            string message = string.Format("Stop loss price {0} reached. Closing all {1} positions", 
                stopLossPrice,
                tradeDirection);

            if (Bars.Close[0] == stopLossPrice) MessageBox.Show(message, Name.ToString());

            // Reset all the lines and buttons.
            EntryPrice = 0;
            SetToDefaultPosition();
            ResetAllButtons();
        }
        private void SendProfitOrder()
        {
            // if = 0, means there is no entry price, no need to send any orders
            if (EntryPrice == 0) return;

            double profitPrice;
            if (ProfitTL_Button.Text == "Offline") return;
            else profitPrice = double.Parse(ProfitTL_Button.Text);

            string tradeDirection;

            if (EntryPrice - profitPrice > 0)
                tradeDirection = "Short";
            else
                tradeDirection = "Long";

            string message = string.Format("Profit price {0} reached. Closing all {1} positions",
                profitPrice,
                tradeDirection);

            if (Bars.Close[0] == profitPrice) MessageBox.Show(message, Name.ToString());

            // reset all to default settings
            EntryPrice = 0;
            SetToDefaultPosition();
            ResetAllButtons();
        }
        private void SendEntryOrder()
        {
            if (Bars.LastBarOnChart == false) return;

            double trendlineEntryPrice = OnChartEntryLine.PriceValue(Bars.TimeValue);

            // if current close does not touch trendline return.
            if (Bars.Close[0] != trendlineEntryPrice) return;

            // Check which order is it.
            switch (DisplayOrderButton.Text)
            {
                case "Buy Limit":
                case "Buy Stop":
                    if (BuyOrderCheck() && IsTradeOrderConfirmed())
                    {
                        EntryPrice = Bars.Close[0];

                        // if order is filled, 
                        DisplayOrderButton.Text = "     Flat     ";
                        DisplayOrderButton.BackColor = Color.DarkSlateGray;
                        DisplayOrderButton.ForeColor = Color.White;
                        OnChartEntryLine.Color = Color.White;
                    }
                    break;

                case "Sell Limit":
                case "Sell Stop":
                    if (SellOrderCheck() && IsTradeOrderConfirmed())
                    {
                        EntryPrice = Bars.Close[0];

                        // if order is filled, 
                        DisplayOrderButton.Text = "     Flat     ";
                        DisplayOrderButton.BackColor = Color.DarkSlateGray;
                        DisplayOrderButton.ForeColor = Color.White;
                        OnChartEntryLine.Color = Color.White;
                    }
                    break;

                default:
                    EntryPrice = 0;
                    break;
            }
        }
        private bool BuyOrderCheck()
        {
            string entryPriceLabel = EntryPriceTL_Button.Text;
            string stopLossLabel = StopLossTL_Button.Text;
            string profitLabel = ProfitTL_Button.Text;

            // Get price information.
            double entryPrice, stopLossPrice, profitPrice;

            // if entry button is not activated.
            if (entryPriceLabel == "Offline") return false;
            else entryPrice = double.Parse(entryPriceLabel);

            // Get the stoploss price and profit price if they are activated.
            if (stopLossLabel != "Offline") stopLossPrice = double.Parse(stopLossLabel);
            else stopLossPrice = double.MinValue;

            if (profitLabel != "Offline") profitPrice = double.Parse(profitLabel);
            else profitPrice = double.MaxValue;

            // if stoplossprice < entryprice < profitprice then ...
            if (stopLossPrice < entryPrice && entryPrice < profitPrice) return true;

            return false;
        }
        private bool SellOrderCheck()
        {
            string entryPriceLabel = EntryPriceTL_Button.Text;
            string stopLossLabel = StopLossTL_Button.Text;
            string profitLabel = ProfitTL_Button.Text;

            // Get price information.
            double entryPrice, stopLossPrice, profitPrice;

            // if entry button is not activated.
            if (entryPriceLabel == "Offline") return false;
            else entryPrice = double.Parse(entryPriceLabel);

            // Get the stoploss price and profit price if they are activated.
            if (stopLossLabel != "Offline") stopLossPrice = double.Parse(stopLossLabel);
            else stopLossPrice = double.MaxValue;

            if (profitLabel != "Offline") profitPrice = double.Parse(profitLabel);
            else profitPrice = double.MinValue;

            // if stoplossprice > entryprice > profitprice then ...
            if (stopLossPrice > entryPrice && entryPrice > profitPrice) return true;

            return false;
        }
        private bool IsTradeOrderConfirmed()
        {
            // display message box when user selects true.
            if (TradeConfirmationRequired == false) return true;

            string orderType = DisplayOrderButton.Text;
            string entryPrice = EntryPriceTL_Button.Text;
            string contracts = ContractTextBox.Text;
            string strategyName = Name.ToString();
            string timeNow = DateTime.Now.ToString();

            if (entryPrice == "Offline") return false;

            string orderMessage = string.Format("{0} with {1} contracts at ${2} ?",
                orderType,
                contracts,
                entryPrice);

            DialogResult results = MessageBox.Show(orderMessage, strategyName, MessageBoxButtons.OKCancel);

            if (results == DialogResult.OK)
            {
                MessageBox.Show(orderMessage + "\nOrder Sent at " + timeNow);
                return true;
            }
            else
            {
                MessageBox.Show("Order Cancelled.");
                return false;
            }
        }
        #endregion


        #region Environment and Account Settings
        private void CheckInputSettings()
        {
            if (MinimumContractsPerTrade < 20000)
            {
                MinimumContractsPerTrade = 20000;
            }

            if (StepIncrementPer20_000 < 1)
            {
                StepIncrementPer20_000 = 1;
            }
        }

        #endregion


        #region Pip Calculator & Auto Position Sizing
        private double PipValue;
        private int AutoPositionSizing(int accountSize, double risk, double entryPrice,
            double stopLossPrice, int stepSize, int maxContracts, int minContracts, int fixContract)
        {
            int tradeSize;

            // for forex use 100_000 for standard lot size.
            int fxStandardLotSize = 100000;

            if (fixContract == 0)
            {
                // get pip value for standard lot
                double pipValue = PipCalculator(fxStandardLotSize);

                // one pip size
                double onePip = GetTickValue(10);

                // calculate price risk.
                double priceRisk = Math.Abs(entryPrice - stopLossPrice);

                // check if risk is less than one pip.
                if (priceRisk < onePip) priceRisk = onePip;

                // calculate numerator and denominator values
                double valueRisk = (priceRisk / onePip) * pipValue;
                double dollarRisk = accountSize * 0.01 * risk;

                // obtain raw trade size
                double rawTradeSize = (dollarRisk / valueRisk) * fxStandardLotSize;

                // convert to min step
                rawTradeSize = stepSize * Math.Floor(rawTradeSize / stepSize);

                // check if less than min value
                if (rawTradeSize < minContracts) rawTradeSize = 0;
                if (rawTradeSize > maxContracts) rawTradeSize = maxContracts;

                tradeSize = (int)rawTradeSize;
            }
            else
            {
                // if contract size has value use fix contracts.
                tradeSize = fixContract;
            }

            return tradeSize;
        }
        private double PipCalculator(int contracts)
        {
            int sessionEndTime = 100 * (Bars.Sessions[0].EndTime.Hours) + Bars.Sessions[0].EndTime.Minutes;
            int currentSessionTime = 100 * (Bars.Time[0].Hour) + Bars.Time[0].Minute;

            // Compute only once a day, or if pip value has not been calculated before
            if (currentSessionTime == sessionEndTime || PipValue == 0)
            {
                // Get current chart symbol.
                string[] _currentBaseSymbol = Bars.Info.Name.Split('.');
                string baseSymbol = _currentBaseSymbol[1];

                // define base symbols in collection
                string[] baseSymbols = { "USD", "JPY", "CHF", "CAD", "AUD", "GBP" };

                // check if current base symbol is in collection
                if (baseSymbols.Contains(baseSymbol) == false)
                {
                    MessageBox.Show("The chart symbol you have chosen is currently not available." +
                        "\nPlease contact sherning@hotmail.com");
                    return 0;
                }

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
                            conversionFactor = 1.0 / GetBenchmark("USD.JPY", ESymbolCategory.Cash);
                            break;
                        }

                    case "CHF":
                        {
                            conversionFactor = 1.0 / GetBenchmark("USD.CHF", ESymbolCategory.Cash);
                            break;
                        }

                    case "CAD":
                        {
                            conversionFactor = 1.0 / GetBenchmark("USD.CAD", ESymbolCategory.Cash);
                            break;
                        }

                    case "GBP":
                        {
                            conversionFactor = GetBenchmark("GBP.USD", ESymbolCategory.Cash);
                            break;
                        }


                    case "AUD":
                        {
                            conversionFactor = GetBenchmark("AUD.USD", ESymbolCategory.Cash);
                            break;
                        }
                }

                // store pip value
                PipValue = GetTickValue(10) * conversionFactor * contracts;
            }

            return PipValue;
        }
        private double GetTickValue(int ticks)
        {
            // 10 ticks make one pip.
            double tickValue = Bars.Info.MinMove / Bars.Info.PriceScale;
            return tickValue * ticks;
        }
        private double GetBenchmark(string symbolName, ESymbolCategory category)
        {
            ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

            // since only one data is used. we dont need dictionary.
            Dictionary<string, double> forexData = new Dictionary<string, double>();

            string keyOne = string.Format("{0:MM/dd/yyyy}", Bars.Time[0]);
            string keyTwo = string.Format("{0:MM/dd/yyyy}", Bars.Time[1]);
            bool hasKey;

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
            request.Range.To = Bars.Time[0] + TimeSpan.FromDays(365);
            request.Category = (ESymbolCategory)benchmarkInfo.symInfo.SymbolCategory;
            request.Symbol = benchmarkInfo.symInfo.SymbolName;
            request.DataFeed = benchmarkInfo.dataFeed;
            request.Exchange = benchmarkInfo.symInfo.SymbolExchange;
            request.RTSymbol = request.Symbol;
            request.RTDataFeed = request.DataFeed;
            request.RTExchange = request.Exchange;

            AutoResetEvent autoEvent = new AutoResetEvent(false);

            IDataLoaderResult results = DataLoader.BeginLoadData(request, result =>
            {
                if (result.IsCompleted)
                {
                    try
                    {
                        rwLock.EnterWriteLock();
                        foreach (var quote in result.Data)
                        {
                            string k = string.Format("{0:MM/dd/yyyy}", quote.Time);
                            forexData[k] = quote.Close;
                        }
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }
                }

                DataLoader.EndLoadData(result);
                autoEvent.Set();
            }
            , null);

            autoEvent.WaitOne();

            try
            {
                rwLock.EnterReadLock();
                hasKey = forexData.ContainsKey(keyOne);
                bool hasKey2 = forexData.ContainsKey(keyTwo);

                return hasKey
                    ? forexData[keyOne]
                    : (hasKey2 ? forexData[keyTwo] : 0.0);
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }
        #endregion


        #region Backup Trend Line Objects

        #endregion


        #region Help Methods
        private string DisplayDouble(double dbValue)
        {
            // check what pair is it
            string[] symbolName = Bars.Info.Name.Split('.');
            string baseSymbol = symbolName[1];

            // determine number of decimal places based on base currency
            if (baseSymbol == "JPY") return DisplayDouble(dbValue, 3);

            return DisplayDouble(dbValue, 5);
        }
        private string DisplayDouble(double dbValue, int decimalPlaces)
        {
            return dbValue.ToString("N" + decimalPlaces);
        }
        private void Print(string message)
        {
            // print messages if debug mode == true.
            if (DebugMode == false) return;

            Output.WriteLine("Project Name:  {0}   |   Bar Time:  {1}   |   Message:  {2}",
                Name,
                Bars.TimeValue.ToString("dd-MM-yy HH:mm:ss tt"),
                message);
        }
        #endregion
    }
}