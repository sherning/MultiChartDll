//==============================================================================
// Name           : __SherNing_PAMA
// Description    : Price Action Moving Average
// Version        : v.1.0.0
// Date Created   : 25 - May - 2020
// Time Taken     : 6 Days
// Remarks        :
//==============================================================================
// Copyright      : 2020, Sher Ning Technologies           
// License        :      
//==============================================================================

/* ------------------------------- Version 1.0.0 -------------------------------
 * Slider did not work well. A Textbox for entry is better.
 * Completed new Price Action Moving Average function.
 * Add all other features, review and clean up code.
 * Check std dev calculations
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [SameAsSymbol(true)]
    public class __SherNing_PriceActionMTF : IndicatorObject
    {
        [Input] public int PriceTypeInput { get; set; }
        [Input] public int DataTypeInput { get; set; }
        public __SherNing_PriceActionMTF(object _ctx) : base(_ctx)
        {
            PriceTypeInput = 1;
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //

            DataTypeInput = 2;
            // --------------------------- Data Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Pama Price                                     //
            //          Option 3: -                                              //
            //          Option 4: -                                              //
            // ----------------------------------------------------------------- //

            // Other non-[Input] properties values
            Length = 6;
            Smoothness = 10;

            // 4 is inclusive of current bar value.
            PivotLength = 4;
        }
        protected override void Create()
        {
            PAMA_Create();
            PAMAPlots_Create();
        }

        protected override void StartCalc()
        {
            PAMA_StartCalc();
            CreateToolStripInStartCalc();
        }
        protected override void CalcBar()
        {
            PAMA_CalcBar();
            SetPamaPlots_CalcBar();
        }

        protected override void Destroy()
        {
            RemoveToolBarInDestroy();
        }
        // ---------------------------------- Indicator Logic ---------------------------------- //


        #region ToolStip Framework

        #region ToolStrip Fields
        private ToolStripButton FiveMinBtn, TenMinBtn, FifteenMinBtn, TwentyMinBtn, ThirtyMinBtn;
        private ToolStripButton SixtyMinBtn, OneTwentyBtn, TwoFortyBtn, DailyBtn, WeeklyBtn, MonthlyBtn;
        private ToolStripButton LengthConfirmBtn, SmoothConfirmBtn, PivotConfirmBtn;
        private ToolStripButton ShowStdDevBtn, ShowPivots;
        private ToolStripLabel LengthLbl, SmoothnessLbl, PivotLbl;
        private ToolStripTextBox LengthTxtBox, SmoothnessTxtBox, PivotTxtBox;
        private bool LengthTxtBoxNonNumberEntered;
        private bool SmoothTxtBoxNonNumberEntered;
        private bool PivotTxtBoxNonNumberEntered;

        private void CreateNewToolStripObjects(ToolStrip tb)
        {
            // ----------------------- Add Items to Toolstrip ----------------------- //

            // Length Text Box
            LengthLbl = new ToolStripLabel();
            LengthLbl.Text = "Length: ";
            AddNewItemToToolStrip(tb, LengthLbl);

            LengthTxtBox = new ToolStripTextBox();
            AddNewItemToToolStrip(tb, LengthTxtBox);

            LengthConfirmBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, LengthConfirmBtn);

            ToolStripSeparator separator = new ToolStripSeparator();
            AddNewItemToToolStrip(tb, separator);

            // Smoothness Text Box
            SmoothnessLbl = new ToolStripLabel();
            SmoothnessLbl.Text = "Smoothness: ";
            AddNewItemToToolStrip(tb, SmoothnessLbl);

            SmoothnessTxtBox = new ToolStripTextBox();
            AddNewItemToToolStrip(tb, SmoothnessTxtBox);

            SmoothConfirmBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, SmoothConfirmBtn);

            ToolStripSeparator separator2 = new ToolStripSeparator();
            AddNewItemToToolStrip(tb, separator2);

            // Pivot Text Box
            PivotLbl = new ToolStripLabel();
            PivotLbl.Text = "Pivot Len: ";
            AddNewItemToToolStrip(tb, PivotLbl);

            PivotTxtBox = new ToolStripTextBox();
            AddNewItemToToolStrip(tb, PivotTxtBox);

            PivotConfirmBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, PivotConfirmBtn);

            ToolStripSeparator separator3 = new ToolStripSeparator();
            AddNewItemToToolStrip(tb, separator3);

            // on off switch for all the 10 timeframes.
            FiveMinBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, FiveMinBtn);

            TenMinBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, TenMinBtn);

            FifteenMinBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, FifteenMinBtn);

            TwentyMinBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, TwentyMinBtn);

            ThirtyMinBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, ThirtyMinBtn);

            SixtyMinBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, SixtyMinBtn);

            OneTwentyBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, OneTwentyBtn);

            TwoFortyBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, TwoFortyBtn);

            DailyBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, DailyBtn);

            WeeklyBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, WeeklyBtn);

            MonthlyBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, MonthlyBtn);

            ShowStdDevBtn = new ToolStripButton();
            AddNewItemToToolStrip(tb, ShowStdDevBtn);

            ShowPivots = new ToolStripButton();
            AddNewItemToToolStrip(tb, ShowPivots);
        }
        private void AddNewItemToToolStrip(ToolStrip tb, ToolStripItem item)
        {
            item.Tag = this;
            tb.Items.Add(item);
        }
        #endregion

        #region ToolStrip StartCalc()
        private bool IsToolStripCreated;
        private void CreateToolStripInStartCalc()
        {
            if (IsToolStripCreated == false)
            {
                ChartToolBar.AccessToolBar(CreateToolStripMain);
                IsToolStripCreated = true;
            }
        }
        #endregion

        #region ToolStrip Functionality
        private void CreateToolStripMain(ToolStrip tb)
        {
            tb.AutoSize = false;
            tb.Height = 80;
            tb.TabIndex = 0;

            // Instaniate all toolstrip objects here.
            CreateNewToolStripObjects(tb);

            // Set toolstrip objects' functionality here
            SetTextBoxConfig();
            SetConfirmationButton();
            SetPamaTFButtons();
            SetStdDevConfig();
            SetPivotConfig();
        }
        private void SetConfirmationButton()
        {
            LengthConfirmBtn.Text = "Enter";
            LengthConfirmBtn.BackColor = Color.LightSeaGreen;
            LengthConfirmBtn.ForeColor = Color.Black;
            LengthConfirmBtn.Click += delegate
            {
                int length = int.Parse(LengthTxtBox.Text);
                bool checkLength = (length >= 1 && length <= 20);

                if (Length != length)
                {
                    if (checkLength == false) return;
                    Length = length;
                    PAMA_StartCalc();
                    ExecControl.Recalculate();
                }
            };

            SmoothConfirmBtn.Text = "Enter";
            SmoothConfirmBtn.BackColor = Color.LightSeaGreen;
            SmoothConfirmBtn.ForeColor = Color.Black;
            SmoothConfirmBtn.Click += delegate
            {
                int smooth = int.Parse(SmoothnessTxtBox.Text);
                bool checkSmooth = (smooth >= 0 && smooth <= 50);

                if (Smoothness != smooth)
                {
                    if (checkSmooth == false) return;
                    Smoothness = smooth;
                    PAMA_StartCalc();
                    ExecControl.Recalculate();
                }
            };

            PivotConfirmBtn.Text = "Enter";
            PivotConfirmBtn.BackColor = Color.LightSeaGreen;
            PivotConfirmBtn.ForeColor = Color.Black;
            PivotConfirmBtn.Click += delegate
            {
                int pivotLength = int.Parse(PivotTxtBox.Text);
                bool checkPivot = (pivotLength >= 3 && pivotLength <= 20);

                if (PivotLength != pivotLength)
                {
                    if (checkPivot == false) return;
                    PivotLength = pivotLength;
                    PAMA_StartCalc();
                    ExecControl.Recalculate();
                }
            };
        }
        private void SetTextBoxConfig()
        {
            #region Length Text Box
            LengthTxtBox.Text = Length.ToString();
            LengthTxtBox.Select();

            // handle non-numeric inputs
            LengthTxtBox.KeyPress += (sender, e) =>
            {
                if (LengthTxtBoxNonNumberEntered == true) e.Handled = true;
            };

            // Check if only numbers are entered into the textbox
            LengthTxtBox.KeyDown += (sender, e) =>
            {
                LengthTxtBoxNonNumberEntered = false;

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
                            LengthTxtBoxNonNumberEntered = true;
                        }
                    }
                }

                //If shift key was pressed, it's not a number.
                if (Control.ModifierKeys == Keys.Shift)
                {
                    LengthTxtBoxNonNumberEntered = true;
                }
            };
            #endregion

            #region Smoothness Text Box
            SmoothnessTxtBox.Text = Smoothness.ToString();
            SmoothnessTxtBox.Select();

            // handle non-numeric inputs
            SmoothnessTxtBox.KeyPress += (sender, e) =>
            {
                if (SmoothTxtBoxNonNumberEntered == true) e.Handled = true;
            };

            // Check if only numbers are entered into the textbox
            SmoothnessTxtBox.KeyDown += (sender, e) =>
            {
                SmoothTxtBoxNonNumberEntered = false;

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
                            SmoothTxtBoxNonNumberEntered = true;
                        }
                    }
                }

                //If shift key was pressed, it's not a number.
                if (Control.ModifierKeys == Keys.Shift)
                {
                    SmoothTxtBoxNonNumberEntered = true;
                }
            };

            #endregion

            #region Pivot Text Box
            PivotTxtBox.Text = PivotLength.ToString();
            //PivotTxtBox.Select();

            // handle non-numeric inputs
            PivotTxtBox.KeyPress += (sender, e) =>
            {
                if (PivotTxtBoxNonNumberEntered == true) e.Handled = true;
            };

            // Check if only numbers are entered into the textbox
            PivotTxtBox.KeyDown += (sender, e) =>
            {
                PivotTxtBoxNonNumberEntered = false;

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
                            PivotTxtBoxNonNumberEntered = true;
                        }
                    }
                }

                //If shift key was pressed, it's not a number.
                if (Control.ModifierKeys == Keys.Shift)
                {
                    PivotTxtBoxNonNumberEntered = true;
                }
            };
            #endregion
        }
        private void SetStdDevConfig()
        {
            ShowStdDevBtn.Text = "Std Dev";
            ShowStdDevBtn.BackColor = Color.Red;
            ShowStdDevBtn.ForeColor = Color.White;
            ShowStdDevBtn.Click += delegate
            {
                if (IsShowStdDev == false)
                {
                    ShowStdDevBtn.BackColor = Color.LimeGreen;
                    ShowStdDevBtn.ForeColor = Color.White;
                    IsShowStdDev = true;
                    ExecControl.Recalculate();
                }
                else
                {
                    ShowStdDevBtn.BackColor = Color.Red;
                    ShowStdDevBtn.ForeColor = Color.White;
                    IsShowStdDev = false;
                    ExecControl.Recalculate();
                }
            };
        }
        private void SetPivotConfig()
        {
            ShowPivots.Text = "Pivots";
            ShowPivots.BackColor = Color.Red;
            ShowPivots.ForeColor = Color.White;
            ShowPivots.Click += delegate
            {
                if (IsShowPivot == false)
                {
                    ShowPivots.BackColor = Color.LimeGreen;
                    ShowPivots.ForeColor = Color.White;
                    IsShowPivot = true;
                    ExecControl.Recalculate();
                }
                else
                {
                    ShowPivots.BackColor = Color.Red;
                    ShowPivots.ForeColor = Color.White;
                    IsShowPivot = false;
                    ExecControl.Recalculate();
                }
            };
        }
        private void SetPamaTFButtons()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // 5 min
            FiveMinBtn.Text = "5 Min";
            if (PAMA_TF_Switch[0] == 1)
            {
                FiveMinBtn.BackColor = Color.LimeGreen;
                FiveMinBtn.ForeColor = Color.White;
            }
            else
            {
                FiveMinBtn.BackColor = Color.DarkGray;
                FiveMinBtn.ForeColor = Color.White;
            }

            FiveMinBtn.Click += delegate
            {
                int tf = 0;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        FiveMinBtn.BackColor = Color.DarkGray;
                        FiveMinBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        FiveMinBtn.BackColor = Color.LimeGreen;
                        FiveMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        FiveMinBtn.BackColor = Color.Red;
                        FiveMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // 10 min
            TenMinBtn.Text = "10 Min";
            if (PAMA_TF_Switch[1] == 1)
            {
                TenMinBtn.BackColor = Color.LimeGreen;
                TenMinBtn.ForeColor = Color.White;
            }
            else
            {
                TenMinBtn.BackColor = Color.DarkGray;
                TenMinBtn.ForeColor = Color.White;
            }

            TenMinBtn.Click += delegate
            {
                int tf = 1;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        TenMinBtn.BackColor = Color.DarkGray;
                        TenMinBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        TenMinBtn.BackColor = Color.LimeGreen;
                        TenMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        TenMinBtn.BackColor = Color.Red;
                        TenMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // 15 min
            FifteenMinBtn.Text = "15 Min";
            if (PAMA_TF_Switch[2] == 1)
            {
                FifteenMinBtn.BackColor = Color.LimeGreen;
                FifteenMinBtn.ForeColor = Color.White;
            }
            else
            {
                FifteenMinBtn.BackColor = Color.DarkGray;
                FifteenMinBtn.ForeColor = Color.White;
            }

            FifteenMinBtn.Click += (sender, e) =>
            {
                int tf = 2;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        FifteenMinBtn.BackColor = Color.DarkGray;
                        FifteenMinBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        FifteenMinBtn.BackColor = Color.LimeGreen;
                        FifteenMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        FifteenMinBtn.BackColor = Color.Red;
                        FifteenMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // 20 min
            TwentyMinBtn.Text = "20 Min";
            if (PAMA_TF_Switch[3] == 1)
            {
                TwentyMinBtn.BackColor = Color.LimeGreen;
                TwentyMinBtn.ForeColor = Color.White;
            }
            else
            {
                TwentyMinBtn.BackColor = Color.DarkGray;
                TwentyMinBtn.ForeColor = Color.White;
            }

            TwentyMinBtn.Click += delegate
            {
                int tf = 3;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        TwentyMinBtn.BackColor = Color.DarkGray;
                        TwentyMinBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        TwentyMinBtn.BackColor = Color.LimeGreen;
                        TwentyMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        TwentyMinBtn.BackColor = Color.Red;
                        TwentyMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // 30 min
            ThirtyMinBtn.Text = "30 Min";
            if (PAMA_TF_Switch[4] == 1)
            {
                ThirtyMinBtn.BackColor = Color.LimeGreen;
                ThirtyMinBtn.ForeColor = Color.White;
            }
            else
            {
                ThirtyMinBtn.BackColor = Color.DarkGray;
                ThirtyMinBtn.ForeColor = Color.White;
            }

            ThirtyMinBtn.Click += delegate
            {
                int tf = 4;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        ThirtyMinBtn.BackColor = Color.DarkGray;
                        ThirtyMinBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        ThirtyMinBtn.BackColor = Color.LimeGreen;
                        ThirtyMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        ThirtyMinBtn.BackColor = Color.Red;
                        ThirtyMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // 60 min
            SixtyMinBtn.Text = "60 Min";
            if (PAMA_TF_Switch[5] == 1)
            {
                SixtyMinBtn.BackColor = Color.LimeGreen;
                SixtyMinBtn.ForeColor = Color.White;
            }
            else
            {
                SixtyMinBtn.BackColor = Color.DarkGray;
                SixtyMinBtn.ForeColor = Color.White;
            }

            SixtyMinBtn.Click += delegate
            {
                int tf = 5;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        SixtyMinBtn.BackColor = Color.DarkGray;
                        SixtyMinBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        SixtyMinBtn.BackColor = Color.LimeGreen;
                        SixtyMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        SixtyMinBtn.BackColor = Color.Red;
                        SixtyMinBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // 120 min
            OneTwentyBtn.Text = "120 Min";
            if (PAMA_TF_Switch[6] == 1)
            {
                OneTwentyBtn.BackColor = Color.LimeGreen;
                OneTwentyBtn.ForeColor = Color.White;
            }
            else
            {
                OneTwentyBtn.BackColor = Color.DarkGray;
                OneTwentyBtn.ForeColor = Color.White;
            }

            OneTwentyBtn.Click += delegate
            {
                int tf = 6;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        OneTwentyBtn.BackColor = Color.DarkGray;
                        OneTwentyBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        OneTwentyBtn.BackColor = Color.LimeGreen;
                        OneTwentyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        OneTwentyBtn.BackColor = Color.Red;
                        OneTwentyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // 240 min
            TwoFortyBtn.Text = "240 Min";
            if (PAMA_TF_Switch[7] == 1)
            {
                TwoFortyBtn.BackColor = Color.LimeGreen;
                TwoFortyBtn.ForeColor = Color.White;
            }
            else
            {
                TwoFortyBtn.BackColor = Color.DarkGray;
                TwoFortyBtn.ForeColor = Color.White;
            }

            TwoFortyBtn.Click += delegate
            {
                int tf = 7;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        TwoFortyBtn.BackColor = Color.DarkGray;
                        TwoFortyBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        TwoFortyBtn.BackColor = Color.LimeGreen;
                        TwoFortyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        TwoFortyBtn.BackColor = Color.Red;
                        TwoFortyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // Daily
            DailyBtn.Text = "Daily";
            if (PAMA_TF_Switch[8] == 1)
            {
                DailyBtn.BackColor = Color.LimeGreen;
                DailyBtn.ForeColor = Color.White;
            }
            else
            {
                DailyBtn.BackColor = Color.DarkGray;
                DailyBtn.ForeColor = Color.White;
            }

            DailyBtn.Click += delegate
            {
                int tf = 8;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        DailyBtn.BackColor = Color.DarkGray;
                        DailyBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        DailyBtn.BackColor = Color.LimeGreen;
                        DailyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        DailyBtn.BackColor = Color.Red;
                        DailyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // Weekly
            WeeklyBtn.Text = "Weekly";
            if (PAMA_TF_Switch[9] == 1)
            {
                WeeklyBtn.BackColor = Color.LimeGreen;
                WeeklyBtn.ForeColor = Color.White;
            }
            else
            {
                WeeklyBtn.BackColor = Color.DarkGray;
                WeeklyBtn.ForeColor = Color.White;
            }

            WeeklyBtn.Click += delegate
            {
                int tf = 9;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        WeeklyBtn.BackColor = Color.DarkGray;
                        WeeklyBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        WeeklyBtn.BackColor = Color.LimeGreen;
                        WeeklyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        WeeklyBtn.BackColor = Color.Red;
                        WeeklyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };

            // Monthly
            MonthlyBtn.Text = "Monthly";
            if (PAMA_TF_Switch[10] == 1)
            {
                MonthlyBtn.BackColor = Color.LimeGreen;
                MonthlyBtn.ForeColor = Color.White;
            }
            else
            {
                MonthlyBtn.BackColor = Color.DarkGray;
                MonthlyBtn.ForeColor = Color.White;
            }

            MonthlyBtn.Click += delegate
            {
                int tf = 10;
                switch (PAMA_TF_Switch[tf])
                {
                    case -1:
                        MonthlyBtn.BackColor = Color.DarkGray;
                        MonthlyBtn.ForeColor = Color.White;
                        break;

                    case 0:
                        MonthlyBtn.BackColor = Color.LimeGreen;
                        MonthlyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 1;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;

                    case 1:
                        MonthlyBtn.BackColor = Color.Red;
                        MonthlyBtn.ForeColor = Color.White;
                        PAMA_TF_Switch[tf] = 0;
                        PAMA_StartCalc();
                        ExecControl.Recalculate();
                        break;
                }
            };
        }
        private void SetButtonsToDefault()
        {
            if (FiveMinBtn != null)
            {
                if (PAMA_TF_Switch[0] == 1)
                {
                    FiveMinBtn.BackColor = Color.LimeGreen;
                    FiveMinBtn.ForeColor = Color.White;
                }
                else
                {
                    FiveMinBtn.BackColor = Color.DarkGray;
                    FiveMinBtn.ForeColor = Color.White;
                }
            }

            if (TenMinBtn != null)
            {
                if (PAMA_TF_Switch[1] == 1)
                {
                    TenMinBtn.BackColor = Color.LimeGreen;
                    TenMinBtn.ForeColor = Color.White;
                }
                else
                {
                    TenMinBtn.BackColor = Color.DarkGray;
                    TenMinBtn.ForeColor = Color.White;
                }
            }

            if (FifteenMinBtn != null)
            {
                if (PAMA_TF_Switch[2] == 1)
                {
                    FifteenMinBtn.BackColor = Color.LimeGreen;
                    FifteenMinBtn.ForeColor = Color.White;
                }
                else
                {
                    FifteenMinBtn.BackColor = Color.DarkGray;
                    FifteenMinBtn.ForeColor = Color.White;
                }
            }

            if (TwentyMinBtn != null)
            {
                if (PAMA_TF_Switch[3] == 1)
                {
                    TwentyMinBtn.BackColor = Color.LimeGreen;
                    TwentyMinBtn.ForeColor = Color.White;
                }
                else
                {
                    TwentyMinBtn.BackColor = Color.DarkGray;
                    TwentyMinBtn.ForeColor = Color.White;
                }
            }

            if (ThirtyMinBtn != null)
            {
                if (PAMA_TF_Switch[4] == 1)
                {
                    ThirtyMinBtn.BackColor = Color.LimeGreen;
                    ThirtyMinBtn.ForeColor = Color.White;
                }
                else
                {
                    ThirtyMinBtn.BackColor = Color.DarkGray;
                    ThirtyMinBtn.ForeColor = Color.White;
                }
            }

            if (SixtyMinBtn != null)
            {
                if (PAMA_TF_Switch[5] == 1)
                {
                    SixtyMinBtn.BackColor = Color.LimeGreen;
                    SixtyMinBtn.ForeColor = Color.White;
                }
                else
                {
                    SixtyMinBtn.BackColor = Color.DarkGray;
                    SixtyMinBtn.ForeColor = Color.White;
                }
            }

            if (OneTwentyBtn != null)
            {
                if (PAMA_TF_Switch[6] == 1)
                {
                    OneTwentyBtn.BackColor = Color.LimeGreen;
                    OneTwentyBtn.ForeColor = Color.White;
                }
                else
                {
                    OneTwentyBtn.BackColor = Color.DarkGray;
                    OneTwentyBtn.ForeColor = Color.White;
                }
            }

            if (TwoFortyBtn != null)
            {
                if (PAMA_TF_Switch[7] == 1)
                {
                    TwoFortyBtn.BackColor = Color.LimeGreen;
                    TwoFortyBtn.ForeColor = Color.White;
                }
                else
                {
                    TwoFortyBtn.BackColor = Color.DarkGray;
                    TwoFortyBtn.ForeColor = Color.White;
                }
            }

            if (DailyBtn != null)
            {
                if (PAMA_TF_Switch[8] == 1)
                {
                    DailyBtn.BackColor = Color.LimeGreen;
                    DailyBtn.ForeColor = Color.White;
                }
                else
                {
                    DailyBtn.BackColor = Color.DarkGray;
                    DailyBtn.ForeColor = Color.White;
                }
            }

            if (WeeklyBtn != null)
            {
                if (PAMA_TF_Switch[9] == 1)
                {
                    WeeklyBtn.BackColor = Color.LimeGreen;
                    WeeklyBtn.ForeColor = Color.White;
                }
                else
                {
                    WeeklyBtn.BackColor = Color.DarkGray;
                    WeeklyBtn.ForeColor = Color.White;
                }
            }

            if (MonthlyBtn != null)
            {
                if (PAMA_TF_Switch[10] == 1)
                {
                    MonthlyBtn.BackColor = Color.LimeGreen;
                    MonthlyBtn.ForeColor = Color.White;
                }
                else
                {
                    MonthlyBtn.BackColor = Color.DarkGray;
                    MonthlyBtn.ForeColor = Color.White;
                }
            }
        }


        #endregion

        #region Destroy()
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

        #endregion

        #region PAMA Plots  

        private List<IPlotObject> ListOfPamaPlots;
        private List<IPlotObject> ListOfFirstStdDevUpper, ListOfFirstStdDevLower;
        private List<IPlotObject> ListOfUpperPivotPlots, ListOfLowerPivotPlots;
        private string[] PamaPlotNames;
        private int[] PamaPlotStyle;
        private int[] PamaPlotWidth;
        private void PAMAPlots_Create()
        {
            PamaPlotNames = new string[NumTFs]
            {
                "5 Min","10 Min","15 Min","20 Min","30 Min",
                "60 Min","120 Min","240 Min",
                "Daily", "Weekly","Monthly"
            };

            PamaPlotWidth = new int[NumTFs]
            {
                // 5, 10, 15, 20, 30
                1, 1, 1, 1, 1,

                // 60, 120, 240 For non-line style need to set to 1
                1, 1, 3,

                // daily, weekly, monthly
                6, 6, 6
            };

            PamaPlotStyle = new int[NumTFs]
            {
                // 5, 10, 15, 20, 30
                (int)EPlotStyle.Solid, 0, 0, 0, 0,

                // 60, 120, 240
                (int)EPlotStyle.DashDot, 3, (int)EPlotStyle.Solid,

                // daily, weekly, monthly
                (int)EPlotStyle.Solid, 0, 0
            };

            ListOfPamaPlots = new List<IPlotObject>();
            ListOfFirstStdDevUpper = new List<IPlotObject>();
            ListOfFirstStdDevLower = new List<IPlotObject>();
            ListOfUpperPivotPlots = new List<IPlotObject>();
            ListOfLowerPivotPlots = new List<IPlotObject>();

            for (int i = 0; i < NumTFs; i++)
            {
                ListOfPamaPlots.Add(
                    AddPlot(new PlotAttributes(
                        PamaPlotNames[i],
                        EPlotShapes.Line,
                        Color.White, // default colors
                        Color.White, // default colors
                        PamaPlotWidth[i],
                        PamaPlotStyle[i],
                        true))); // plot marker on chart = true

                ListOfFirstStdDevUpper.Add(
                     AddPlot(new PlotAttributes(
                        PamaPlotNames[i],
                        EPlotShapes.Line,
                        Color.White, // default colors
                        Color.White, // default colors
                        PamaPlotWidth[i],
                        EPlotStyle.Dot,
                        true))); // plot marker on chart = true

                ListOfFirstStdDevLower.Add(
                     AddPlot(new PlotAttributes(
                        PamaPlotNames[i],
                        EPlotShapes.Line,
                        Color.White, // default colors
                        Color.White, // default colors
                        PamaPlotWidth[i],
                        EPlotStyle.Dot,
                        true))); // plot marker on chart = true

                ListOfUpperPivotPlots.Add(
                     AddPlot(new PlotAttributes(
                        PamaPlotNames[i],
                        EPlotShapes.Line,
                        Color.White, // default colors
                        Color.White, // default colors
                        PamaPlotWidth[i],
                        EPlotStyle.DashDotDot,
                        true))); // plot marker on chart = true

                ListOfLowerPivotPlots.Add(
                     AddPlot(new PlotAttributes(
                        PamaPlotNames[i],
                        EPlotShapes.Line,
                        Color.White, // default colors
                        Color.White, // default colors
                        PamaPlotWidth[i],
                        EPlotStyle.DashDotDot,
                        true))); // plot marker on chart = true

            }
        }
        private void SetPamaPlots_CalcBar()
        {
            Color plotColor = Color.White;

            for (int i = 0; i < NumTFs; i++)
            {
                if (PAMA_TF_Switch[i] != 1) continue;

                if (ListOfColorValues[i].Value == GreenColor)
                    plotColor = Color.LimeGreen;

                else if (ListOfColorValues[i].Value == RedColor)
                    plotColor = Color.Red;

                ListOfPamaPlots[i].Set(1, ListOfPamaValues[i][1], plotColor);

                if (IsShowStdDev == true)
                {
                    ListOfFirstStdDevUpper[i].Set(1, ListOfPamaValues[i][1] + ListOfStdDevValues[i][1], plotColor);
                    ListOfFirstStdDevLower[i].Set(1, ListOfPamaValues[i][1] - ListOfStdDevValues[i][1], plotColor);
                }

                if (IsShowPivot == true)
                {
                    ListOfUpperPivotPlots[i].Set(1, ListOfPivotHighValues[i][1], plotColor);
                    ListOfLowerPivotPlots[i].Set(1, ListOfPivotLowValues[i][1], plotColor);
                }
            }
        }
        #endregion

        #region Price Action Moving Average
        private const int GreenColor = 1;
        private const int RedColor = -1;
        private List<HullMovingAverage> ListOfHMAs;
        private List<VariableSeries<double>> ListOfHmaValues;
        private List<VariableSeries<bool>> ListOfIsEndOfBar;
        private List<VariableSeries<double>> ListOfHighPrices;
        private List<VariableSeries<double>> ListOfLowPrices;
        private List<VariableSeries<double>> ListOfOpenPrices;
        private List<VariableSeries<double>> ListOfClosePrices;
        private List<VariableSeries<double>> ListOfCalcPrices;
        private List<VariableSeries<double>> ListOfPamaValues;
        private List<VariableSeries<double>> ListOfPamaValues1BarAgo;
        private List<VariableSeries<int>> ListOfColorValues;
        private List<StandardDeviationCalculator> ListOfStdDevCalculators;
        private List<VariableSeries<double>> ListOfStdDevValues;
        private List<MovingAveragePivots> ListOfPivots;
        private List<VariableSeries<double>> ListOfPivotHighValues, ListOfPivotLowValues;
        private void PAMA_Create()
        {
            ListOfHMAs = new List<HullMovingAverage>();
            ListOfHmaValues = new List<VariableSeries<double>>();
            ListOfIsEndOfBar = new List<VariableSeries<bool>>();
            ListOfHighPrices = new List<VariableSeries<double>>();
            ListOfLowPrices = new List<VariableSeries<double>>();
            ListOfOpenPrices = new List<VariableSeries<double>>();
            ListOfClosePrices = new List<VariableSeries<double>>();
            ListOfCalcPrices = new List<VariableSeries<double>>();
            ListOfPamaValues = new List<VariableSeries<double>>();
            ListOfPamaValues1BarAgo = new List<VariableSeries<double>>();
            ListOfColorValues = new List<VariableSeries<int>>();
            ListOfStdDevCalculators = new List<StandardDeviationCalculator>();
            ListOfStdDevValues = new List<VariableSeries<double>>();
            ListOfPivots = new List<MovingAveragePivots>();
            ListOfPivotHighValues = new List<VariableSeries<double>>();
            ListOfPivotLowValues = new List<VariableSeries<double>>();

            for (int i = 0; i < NumTFs; i++)
            {
                ListOfHMAs.Add(new HullMovingAverage(Length));
                ListOfHmaValues.Add(new VariableSeries<double>(this));
                ListOfIsEndOfBar.Add(new VariableSeries<bool>(this));
                ListOfHighPrices.Add(new VariableSeries<double>(this));
                ListOfLowPrices.Add(new VariableSeries<double>(this));
                ListOfOpenPrices.Add(new VariableSeries<double>(this));
                ListOfClosePrices.Add(new VariableSeries<double>(this));
                ListOfCalcPrices.Add(new VariableSeries<double>(this));
                ListOfPamaValues.Add(new VariableSeries<double>(this));
                ListOfPamaValues1BarAgo.Add(new VariableSeries<double>(this));
                ListOfColorValues.Add(new VariableSeries<int>(this));
                ListOfStdDevCalculators.Add(new StandardDeviationCalculator());
                ListOfStdDevValues.Add(new VariableSeries<double>(this));
                ListOfPivots.Add(new MovingAveragePivots());
                ListOfPivotHighValues.Add(new VariableSeries<double>(this));
                ListOfPivotLowValues.Add(new VariableSeries<double>(this));
            }
        }


        private int Length, Smoothness, PivotLength;
        private double Alpha, Gain;
        private int[] PAMA_TF_Switch;
        private int[] CountNumOfBarsForeachTF;
        private const int NumTFs = 11;
        private bool HasSetDefaultTF, IsShowStdDev, IsShowPivot;
        private int CurrentTimeFrame;
        private void PAMA_StartCalc()
        {
            // Clear output messages
            Output.Clear();

            // Pama variables
            Alpha = 2.0 / (Length + 1.0);
            Gain = Smoothness / 100.0;

            // 11 time frames total, default to 0.
            CountNumOfBarsForeachTF = new int[NumTFs];

            if (IsFXPriceChartEnvironment() == false)
                ExecControl.Abort("This indicator only works for FX regular bars price charts.");

            // If user changes timeframe on chart.
            int currentTF = (int)Bars.Info.Resolution.Size;
            if (CurrentTimeFrame != currentTF)
            {
                HasSetDefaultTF = false;

                // Current Time Frame state is preserved during recalculation.
                CurrentTimeFrame = currentTF;
            }

            // On, off or disable certain TFs depending on current chart resolution size.
            if (HasSetDefaultTF == false)
            {
                SetDefaultTFsForCurrentChart();

                // This is a toolbar feature. Remove when coverting to strategy.
                SetButtonsToDefault();
            }

            for (int i = 0; i < NumTFs; i++)
            {
                ListOfHMAs[i].SetLength(Length);
                ListOfStdDevCalculators[i].SetLength(Length);
                ListOfStdDevCalculators[i].PopulationStdDev = true;
                ListOfPivots[i].SetLength(PivotLength);
            }
        }
        private void PAMA_CalcBar()
        {
            // due to the heavy calculations involved, calculate it only at end of bar.
            if (IsEndOfBar() == false) return;
            for (int timeFrame = 0; timeFrame < NumTFs; timeFrame++)
            {
                // if switch not ON or is DISABLED skip.
                if (PAMA_TF_Switch[timeFrame] != 1) continue;

                ListOfIsEndOfBar[timeFrame].Value = IsTimeFrameOnCloseTick(timeFrame);

                // Update private fields members
                SetPriceDataForHigherTFs(timeFrame);

                // Choose which price types you want PAMA to calculate
                SetPriceTypesForHigherTFs(timeFrame);

                // Calculate PAMA price
                SetPAMAValueForHigherTFs(timeFrame);

                // Set Pama Color
                SetPamaColorForHigherTFs(timeFrame);

                // Calculate Std Dev
                CalculateStdDev(timeFrame);

                // Calculate Pivot Values
                CalculatePivots(timeFrame);

                // End of Bar for each time frame
                if (ListOfIsEndOfBar[timeFrame].Value == true)
                    CountNumOfBarsForeachTF[timeFrame] += 1;
            }

        }
        private void CalculatePivots(int timeframe)
        {
            // Calculate end of bar for each time frame
            if (ListOfIsEndOfBar[timeframe].Value == true)
            {
                // Add
                ListOfPivots[timeframe].AddHigh(ListOfHighPrices[timeframe].Value);
                ListOfPivots[timeframe].AddLow(ListOfLowPrices[timeframe].Value);
                ListOfPivots[timeframe].AddData(ListOfPamaValues[timeframe].Value);

                // Cache the data into variable series in order to use 1 bar back
                ListOfPivotHighValues[timeframe].Value = ListOfPivots[timeframe].PivotHighValue;
                ListOfPivotLowValues[timeframe].Value = ListOfPivots[timeframe].PivotLowValue;
            }
        }
        private void CalculateStdDev(int timeframe)
        {
            // Calculate end of bar for each time frame
            if (ListOfIsEndOfBar[timeframe].Value == true)
            {
                switch (DataTypeInput)
                {
                    case 1: // use close price
                        ListOfStdDevCalculators[timeframe].Add(Bars.CloseValue);
                        break;

                    case 2: // use pama value
                        if (ListOfPamaValues[timeframe].Value != 0.0)
                            ListOfStdDevCalculators[timeframe].Add(ListOfPamaValues[timeframe].Value);
                        break;
                }

                ListOfStdDevValues[timeframe].Value = ListOfStdDevCalculators[timeframe].Value;
            }
        }
        private void SetPamaColorForHigherTFs(int timeframe)
        {
            if (ListOfIsEndOfBar[timeframe][0] == true)
            {
                // should be consider 0 ? zero difference, follow prevailing color.
                if (ListOfPamaValues[timeframe].Value > ListOfPamaValues1BarAgo[timeframe].Value)
                {
                    ListOfColorValues[timeframe].Value = GreenColor;
                }
                else if (ListOfPamaValues[timeframe].Value < ListOfPamaValues1BarAgo[timeframe].Value)
                {
                    ListOfColorValues[timeframe].Value = RedColor;
                }
            }
        }
        private void SetPAMAValueForHigherTFs(int timeframe)
        {
            // EBarStatus == Close for that timeframe.
            if (ListOfIsEndOfBar[timeframe].Value == true)
            {
                ListOfPamaValues1BarAgo[timeframe].Value = ListOfPamaValues[timeframe].Value;

                ListOfHMAs[timeframe].AddData(ListOfCalcPrices[timeframe][0]);

                ListOfHmaValues[timeframe].Value = ListOfHMAs[timeframe].Value;

                if (ListOfCalcPrices[timeframe].Value - ListOfPamaValues1BarAgo[timeframe].Value > 0)
                {
                    ListOfPamaValues[timeframe].Value
                    = Alpha * (ListOfHmaValues[timeframe].Value
                    + (-Gain) * (ListOfHmaValues[timeframe].Value - ListOfPamaValues1BarAgo[timeframe].Value))
                    + (1.0 - Alpha) * ListOfPamaValues1BarAgo[timeframe].Value;
                }
                else if (ListOfCalcPrices[timeframe].Value - ListOfPamaValues1BarAgo[timeframe].Value < 0)
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + Gain * (ListOfHmaValues[timeframe].Value - ListOfPamaValues1BarAgo[timeframe].Value))
                   + (1.0 - Alpha) * ListOfPamaValues1BarAgo[timeframe].Value;
                }
                else
                {
                    ListOfPamaValues[timeframe].Value
                   = Alpha * (ListOfHmaValues[timeframe].Value
                   + 0.0 * (ListOfHmaValues[timeframe].Value - ListOfPamaValues1BarAgo[timeframe].Value))
                   + (1.0 - Alpha) * ListOfPamaValues1BarAgo[timeframe].Value;
                }
            }
        }
        private void SetPriceTypesForHigherTFs(int timeframe)
        {
            // -------------------------- Price Input -------------------------- //
            //          Option 1: Close Price                                    //
            //          Option 2: Range Price (High - Low)                       //
            //          Option 3: HLOC Average Price                             //
            //          Option 4: OC Average Price                               //
            // ----------------------------------------------------------------- //


            // EBarStatus == Close for that timeframe.
            if (ListOfIsEndOfBar[timeframe][0] == true)
            {
                switch (PriceTypeInput)
                {
                    default:
                    case 1: // using Close Price
                        ListOfCalcPrices[timeframe].Value = ListOfClosePrices[timeframe].Value;
                        break;

                    case 2: // use of Range Price
                        ListOfCalcPrices[timeframe].Value
                            = ListOfHighPrices[timeframe].Value - ListOfLowPrices[timeframe].Value;
                        break;

                    case 3: // Use of HLOC Average Price
                        ListOfCalcPrices[timeframe].Value
                            = (ListOfHighPrices[timeframe].Value
                            + ListOfLowPrices[timeframe].Value
                            + ListOfOpenPrices[timeframe].Value
                            + ListOfClosePrices[timeframe].Value) * 0.25;
                        break;

                    case 4: // Use of OC Average Price
                        ListOfCalcPrices[timeframe].Value
                            = (ListOfOpenPrices[timeframe].Value
                            + ListOfClosePrices[timeframe].Value) * 0.5;
                        break;
                }
            }
        }
        private void SetPriceDataForHigherTFs(int timeframe)
        {
            if (CountNumOfBarsForeachTF[timeframe] > 0)
            {
                if (ListOfIsEndOfBar[timeframe][1] == true)
                {
                    ListOfHighPrices[timeframe].Value = Bars.HighValue;
                    ListOfLowPrices[timeframe].Value = Bars.LowValue;
                    ListOfOpenPrices[timeframe].Value = Bars.OpenValue;
                }
                else
                {
                    // Keep Track of Higher TFs High Prices
                    if (Bars.HighValue > ListOfHighPrices[timeframe].Value)
                    {
                        ListOfHighPrices[timeframe].Value = Bars.High[0];
                    }

                    // Keep Track of Higher TFs Low Prices
                    if (Bars.LowValue < ListOfLowPrices[timeframe].Value)
                    {
                        ListOfLowPrices[timeframe].Value = Bars.Low[0];
                    }
                }

                // Keep track of closing price.
                if (ListOfIsEndOfBar[timeframe][0] == true)
                    ListOfClosePrices[timeframe].Value = Bars.CloseValue;
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

            // isCloseTick is the same as getting Bars.Status == EBarState.Close for respective TFs
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
                    bool isFx =
                        (Bars.Info.Category == ESymbolCategory.Cash) || (Bars.Info.Category == ESymbolCategory.Forex);
                    bool isSessionEnd = currTimeStartReg == sessEndTimeReg;
                    bool isFriday = Bars.TimeValue.DayOfWeek == DayOfWeek.Friday;
                    isCloseTick = (isFx && isSessionEnd && isFriday);
                    break;

                case 10: // monthly
                    bool isLastDayOfMonth = GetLastTradingDayOfMonth();

                    // last day of the year is tricky ..
                    if (IsLastDayOfTheYear() == true && GetCurrentTime() == 1630)
                        isSessionEnd = true;
                    else
                        isSessionEnd = currTimeStartReg == sessEndTimeReg;

                    isFx =
                   (Bars.Info.Category == ESymbolCategory.Cash) || (Bars.Info.Category == ESymbolCategory.Forex);

                    isCloseTick = isLastDayOfMonth && isSessionEnd && isFx;
                    break;

                default:
                    isCloseTick = false;
                    break;
            }

            return isCloseTick;
        }
        private void SetDefaultTFsForCurrentChart()
        {
            // ------------------------ Timeframe Index Table ------------------------ //
            // 0 - 5min,   1 - 10min,  2 - 15min, 3 - 20min, 4 - 30min, 5 - 60min,     //
            // 6 - 120min, 7 - 240min, 8 - day,   9 - week,  10- month                 //
            //                                                                         //     
            // ------------------------ Timeframe Index Table ------------------------ //

            // set disabled by default.
            PAMA_TF_Switch = new int[NumTFs];

            // Set to disable first.
            for (int i = 0; i < NumTFs; i++)
                PAMA_TF_Switch[i] = -1;

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
                            PAMA_TF_Switch[i] = 1;
                        else
                            PAMA_TF_Switch[i] = 0;
                    }
                    break;

                case 15:
                    // 5, 10, weekly, monthly disabled
                    for (int i = 2; i <= 8; i++)
                    {
                        if (i == 2 || i == 5 || i == 7)
                            PAMA_TF_Switch[i] = 1;
                        else
                            PAMA_TF_Switch[i] = 0;
                    }
                    break;

                case 30:
                    // 5, 10, 15, weekly, monthly disabled
                    for (int i = 4; i <= 8; i++)
                    {
                        if (i == 4 || i == 5 || i == 7)
                            PAMA_TF_Switch[i] = 1;
                        else
                            PAMA_TF_Switch[i] = 0;
                    }
                    break;

                case 60:
                    // 5, 10, 15, 30, monthly disabled
                    for (int i = 5; i <= 9; i++)
                    {
                        // 60, 240, day
                        if (i == 5 || i == 7 || i == 8)
                            PAMA_TF_Switch[i] = 1;
                        else
                            PAMA_TF_Switch[i] = 0;
                    }
                    break;

                case 240:
                    // index 10 is the last.
                    for (int i = 7; i < 11; i++)
                    {
                        if (i == 7 || i == 8 || i == 9)
                            PAMA_TF_Switch[i] = 1;
                        else
                            PAMA_TF_Switch[i] = 0;
                    }
                    break;

                default:
                    break;
            }

            HasSetDefaultTF = true;
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

        #region Environment Settings
        private bool IsEndOfBar()
        {
            return Bars.Status == EBarState.Close;
        }
        private bool IsLastDayOfTheYear()
        {
            DateTime lastDayOfYear = new DateTime(Bars.TimeValue.Year, 12, 31);
            if (Bars.TimeValue.Date == lastDayOfYear.Date) return true;

            return false;
        }
        private int GetLastFridayOfTheMonth()
        {
            int currentYear = Bars.TimeValue.Year;
            int currentMonth = Bars.TimeValue.Month;
            DateTime lastFriday = new DateTime(currentYear, currentMonth, 1).AddMonths(1).AddDays(-1);

            while (lastFriday.DayOfWeek != DayOfWeek.Friday)
                lastFriday = lastFriday.AddDays(-1);

            return lastFriday.Day;
        }
        private bool GetLastTradingDayOfMonth()
        {
            int currentYear = Bars.TimeValue.Year;
            int currentMonth = Bars.TimeValue.Month;
            int currentDay = Bars.TimeValue.Day;

            int numOfDaysInCurrentMonth =
                DateTime.DaysInMonth(currentYear, currentMonth);

            // if last day of the month
            if (Bars.TimeValue.Day == numOfDaysInCurrentMonth) return true;

            // else check for last trading 
            DateTime lastDayOfMonth = new DateTime(currentYear, currentMonth, numOfDaysInCurrentMonth);

            // if last day of the month is a weekend. 
            if (lastDayOfMonth.DayOfWeek == DayOfWeek.Saturday || lastDayOfMonth.DayOfWeek == DayOfWeek.Sunday)
                if (currentDay == GetLastFridayOfTheMonth())
                    return true;

            return false;
        }
        private int GetCurrentTimeFromSessionOpen()
        {
            // minutes from session open
            int minutesFromSessionOpen = 0;

            // total minutes from Midnight. 1050 / 60 = 17.5 hours | 1020 / 60 = 17 hours
            int sessionStartMinutes = (int)(Bars.Sessions[0].StartTime.TotalMinutes);
            int sessionEndMinutes = (int)(Bars.Sessions[0].EndTime.TotalMinutes);

            // current time in minutes from midnight.
            int currentTimeMinutes = 60 * Bars.TimeValue.Hour + Bars.TimeValue.Minute;

            // number of minutes in a day. 1440 minutes
            int numMinutesDaily = 24 * 60;

            // if no condition applies. Return 0.
            if (currentTimeMinutes >= sessionStartMinutes && currentTimeMinutes < numMinutesDaily)
            {
                minutesFromSessionOpen = currentTimeMinutes - sessionStartMinutes;
            }
            else if (currentTimeMinutes > 0 && currentTimeMinutes <= sessionEndMinutes)
            {
                minutesFromSessionOpen = numMinutesDaily - sessionStartMinutes + currentTimeMinutes;
            }
            else if (currentTimeMinutes == 0)
            {
                minutesFromSessionOpen = numMinutesDaily - sessionStartMinutes;
            }

            // For FX, trading day is 1410 minutes or 23.5 hours daily.
            return minutesFromSessionOpen;
        }
        private int GetCurrentTime()
        {
            // 745pm = 1900 + 45
            int hours = Bars.TimeValue.Hour * 100;
            int minutes = Bars.TimeValue.Minute;
            return hours + minutes;
        }
        private int GetSessionEndTime()
        {
            // Current session endtime, 5pm = 500, 530pm = 530
            int hours = Bars.Sessions[0].EndTime.Hours * 100;
            int minutes = Bars.Sessions[0].EndTime.Minutes;
            return hours + minutes;
        }
        private bool IsFXPriceChartEnvironment()
        {
            // Make sure it is a price chart.
            if (Environment.ApplicationCode != EApplicationCode.Charting) return false;

            // Make sure it is not IB forex chart
            if (Bars.Info.Category != ESymbolCategory.Cash) return false;

            // ensure bars are regular bars
            if (Bars.Info.Resolution.ChartType != ChartType.Regular) return false;

            return true;
        }
        #endregion

        #region Hull Moving Average Calculations
        class HullMovingAverage
        {
            // class properties
            public double Value { get { return CalcHma(); } }

            // class fields
            private int Length;
            private int[] Lengths;
            private double[][] PriceList;
            private int Count, Count3;

            // constructor
            public HullMovingAverage(int length)
            {
                // Use constructor to StartCalc()
                SetLength(length);
            }

            #region Public Methods
            public void AddData(double data)
            {
                // Shift all data to the front of the list
                for (int i = 0; i < Lengths[2] - 1; i++)
                    PriceList[2][i] = PriceList[2][i + 1];

                // Add latest data to end of list
                PriceList[2][Lengths[2] - 1] = data;
                Count++;

                // Remove overflow
                if (Count > Length) Count--;
            }
            public void SetLength(int length)
            {
                // parameter check
                if (length < 1) Length = 1;
                else Length = length;

                if (Length < 1) Length = 1;

                // 3 lengths, avoid [0]
                Lengths = new int[4];

                // Calculate only when length value has changed
                int halvedLength;
                if ((Math.Ceiling((double)(Length / 2)) - (Length / 2)) <= 0.5)
                {
                    halvedLength = (int)Math.Ceiling((double)(Length / 2));
                }
                else
                {
                    halvedLength = (int)Math.Floor((double)(Length / 2));
                }

                int sqrRootLength;
                double sqLength = Math.Sqrt(Length);

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
                Lengths[2] = Length;
                Lengths[3] = sqrRootLength;

                // Reset Count
                Count = Count3 = 0;

                // Avoid [0]
                PriceList = new double[4][];

                //PriceList[0] = null by default
                PriceList[1] = new double[Lengths[1]];
                PriceList[2] = new double[Lengths[2]];
                PriceList[3] = new double[Lengths[3]];
            }
            #endregion

            #region Private Methods
            private double CalcHma()
            {
                // insufficient data
                if (Count != Length) return 0;

                //PriceList1[lastvalue--] = PriceList[lastvalue--]
                for (int i = Lengths[1] - 1; i >= 0; i--)
                    PriceList[1][i] = PriceList[2][i + Lengths[2] - Lengths[1]];

                // calculate 
                double wma1 = 2.0 * WeightedAverage(PriceList[1], Lengths[1]);
                double wma2 = WeightedAverage(PriceList[2], Lengths[2]);

                // shuffle to the front
                for (int i = 0; i < Lengths[3] - 1; i++)
                    PriceList[3][i] = PriceList[3][i + 1];

                // Add to the back of the list
                PriceList[3][Lengths[3] - 1] = wma1 - wma2;
                Count3++;

                // remove overflow
                if (Count3 > Lengths[3]) Count3--;

                if (Count3 == Lengths[3])
                    return WeightedAverage(PriceList[3], Lengths[3]);
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
            #endregion
        }

        #endregion

        #region Standard Deviation Calculator
        class StandardDeviationCalculator
        {
            // class properties
            public bool PopulationStdDev { get; set; }
            public int Count { get; private set; }
            public double Value { get { return CalcStdDev(); } }

            // class fields
            private double[] DataSet;
            private int Length;

            // Constructor to set initial length
            public StandardDeviationCalculator(int length, bool isPopulation)
            {
                PopulationStdDev = isPopulation;
                // ------------ Data Type ------------ //
                // true: Population type calculations  //
                // false: Sample type calculations     //
                // ----------------------------------- //

                // if dataset is small, use population
                SetLength(length);
            }
            public StandardDeviationCalculator()
            {
                Length = 0;
                PopulationStdDev = true;
                SetLength(1);
            }

            // class methods
            public void SetLength(int length)
            {
                // First initialization of array
                if (Length == 0)
                {
                    if (length > 0)
                        Length = length;
                    else
                        Length = 1;

                    DataSet = new double[Length];
                    Count = 0;
                    return;
                }

                // If Length property is not 0
                if (Length > 0)
                {
                    if (length > Length)
                    {
                        // resize
                        double[] tempArr = new double[length];

                        // store existing data to temp
                        for (int i = 0; i < Length; i++)
                            tempArr[i] = DataSet[i];

                        // change Length property
                        Count = Length;
                        Length = length;
                        DataSet = new double[Length];
                        DataSet = tempArr;
                    }
                    else if (length < Length)
                    {
                        // resize
                        double[] tempArr = new double[length];
                        for (int i = 0; i < length; i++)
                            tempArr[i] = DataSet[i];

                        Count = Length = length;
                        DataSet = new double[Length];
                        DataSet = tempArr;
                    }
                    else
                    {
                        // remain status quo if no change in length
                    }
                }
            }
            public void Add(double data)
            {
                // shift all the data back by 1
                for (int i = Length - 1; i > 0; i--)
                    DataSet[i] = DataSet[i - 1];

                // Add newest data to the front
                DataSet[0] = data;

                // Count number of data added.
                Count++;

                if (Count > Length) Count--;
            }
            private double CalcStdDev()
            {
                // if insufficient data
                if (Count < Length) return 0;

                // Calculate mean sum
                double mean = 0;
                for (int i = 0; i < Length; i++)
                    mean += DataSet[i];

                // mean = summation(i=0 to i=Length-1) / Length
                mean /= Length;

                // calculate variance sigma^2
                double variance = 0;
                for (int i = 0; i < Length; i++)
                    variance += Math.Pow(DataSet[i] - mean, 2);

                // Calculate sample variance
                if (PopulationStdDev == false) variance /= (Length - 1);

                // Calculate population variance
                else variance /= Length;

                // calculate std dev = Sqrt(variance)
                return Math.Sqrt(variance);
            }
        }
        #endregion

        #region Bar Structure
        struct BarStructure
        {
            // fields
            private const int BarTypes = 4; //[0]: High, [1]: Low, [2]: Open, [3]: Close
            private double[] BarHighs, BarLows, BarOpens, BarCloses;
            private int[] NumOfBars;
            private int MaxBars;

            #region Setup Functions
            public void SetMaxBars()
            {
                SetMaxBars(1);
            }
            public void SetMaxBars(int maxbars)
            {
                if (maxbars >= 1)
                    MaxBars = maxbars;
                else
                    MaxBars = 1;

                // Initialize containers and reset fields
                BarHighs = new double[MaxBars];
                BarLows = new double[MaxBars];
                BarOpens = new double[MaxBars];
                BarCloses = new double[MaxBars];
                NumOfBars = new int[BarTypes];
            }

            #endregion

            #region Analytical Functions

            public double HighestValue(int datatype)
            {
                double[] data = ExportData(datatype);
                if (data == null) return 0;

                double highestValue = data[0];

                for (int i = 1; i < MaxBars; i++)
                {
                    if (data[i] > highestValue)
                        highestValue = Math.Max(data[i], highestValue);
                }

                return highestValue;
            }

            public double LowestValue(int datatype)
            {
                double[] data = ExportData(datatype);
                if (data == null) return 0;

                double lowestValue = data[0];
                for (int i = 1; i < MaxBars; i++)
                {
                    if (data[i] < lowestValue)
                        lowestValue = Math.Min(data[i], lowestValue);
                }

                return lowestValue;
            }
            #endregion

            #region Add Data Functions
            public void AddHigh(double high)
            {
                if (MaxBars < 1) SetMaxBars(1);

                int barId = 0;

                for (int i = MaxBars - 1; i > 0; i--)
                    BarHighs[i] = BarHighs[i - 1];

                // Cache data and update bar count
                BarHighs[0] = high;
                NumOfBars[barId]++;

                if (NumOfBars[barId] > MaxBars) NumOfBars[barId]--;
            }

            public void AddLow(double low)
            {
                if (MaxBars < 1) SetMaxBars(1);

                int barId = 1;

                for (int i = MaxBars - 1; i > 0; i--)
                    BarLows[i] = BarLows[i - 1];

                // Cache data and update bar count
                BarLows[0] = low;
                NumOfBars[barId]++;

                if (NumOfBars[barId] > MaxBars) NumOfBars[barId]--;
            }

            public void AddOpen(double open)
            {
                if (MaxBars < 1) SetMaxBars(1);

                int barId = 2;

                for (int i = MaxBars - 1; i > 0; i--)
                    BarOpens[i] = BarOpens[i - 1];

                // Cache data and update bar count
                BarOpens[0] = open;
                NumOfBars[barId]++;

                if (NumOfBars[barId] > MaxBars) NumOfBars[barId]--;
            }

            public void AddClose(double close)
            {
                if (MaxBars < 1) SetMaxBars(1);

                int barId = 3;

                for (int i = MaxBars - 1; i > 0; i--)
                    BarCloses[i] = BarCloses[i - 1];

                // Cache data and update bar count
                BarCloses[0] = close;
                NumOfBars[barId]++;

                if (NumOfBars[barId] > MaxBars) NumOfBars[barId]--;
            }
            #endregion

            #region Data Management Functions
            public void ClearData()
            {
                if (BarHighs == null || BarLows == null || BarOpens == null
                    || BarCloses == null || NumOfBars == null) return;

                for (int i = 0; i < MaxBars; i++)
                {
                    BarHighs[i] = 0;
                    BarLows[i] = 0;
                    BarOpens[i] = 0;
                    BarCloses[i] = 0;
                }

                for (int i = 0; i < BarTypes; i++)
                    NumOfBars[i] = 0;
            }
            /// <summary>
            /// 0: High, 1: Low, 2: Open, 3: Close
            /// </summary>
            /// <param name="datatype"></param>
            /// <returns></returns>
            public double[] ExportData(int datatype)
            {
                double[] data;

                switch (datatype)
                {
                    case 0: // High
                        data = BarHighs;
                        break;

                    case 1: // Low
                        data = BarLows;
                        break;

                    case 2: // Open
                        data = BarOpens;
                        break;

                    case 3: // Close
                        data = BarCloses;
                        break;

                    default:
                        data = null;
                        break;
                }

                return data;
            }

            #endregion

            #region Documentation
            public string ViewDocumentation()
            {
                string text = "This structure saves X number of bars data, " +
                    "bartype = 0 (high), bartype = 1 (low), bartype = 2 (open), bartype = 3 (close)";

                return text;
            }
            #endregion
        }
        #endregion

        #region Moving Average Pivots
        class MovingAveragePivots
        {
            // class properties
            public double PivotHighValue { get { return GetPivotHighValue(); } }
            public double PivotLowValue { get { return GetPivotLowValue(); } }

            // class fields, 3 points required to determine a turning point
            private const int DataPoints = 3;
            private BarStructure Bars;
            private double[] DataSet;
            private int Length, Count;
            private double _PivotHighValue, _PivotLowValue;

            // class constructors
            public MovingAveragePivots(int length)
            {
                Length = length;
                Bars.SetMaxBars(Length);
                DataSet = new double[DataPoints];
            }

            public MovingAveragePivots()
            {
                Bars.SetMaxBars();
                DataSet = new double[DataPoints];
            }

            #region Main Functions
            private double GetPivotHighValue()
            {
                if (PivotHigh() == true)
                    _PivotHighValue = Bars.HighestValue(0);

                return _PivotHighValue;
            }

            private double GetPivotLowValue()
            {
                if (PivotLow() == true)
                    _PivotLowValue = Bars.LowestValue(1);

                return _PivotLowValue;
            }

            private bool PivotHigh()
            {
                // Insufficient Data
                if (Count < DataPoints) return false;

                if (DataSet[0] < DataSet[1] && DataSet[1] > DataSet[2])
                    return true;

                return false;
            }

            private bool PivotLow()
            {
                // Insufficient Data
                if (Count < DataPoints) return false;

                if (DataSet[0] > DataSet[1] && DataSet[1] < DataSet[2])
                    return true;

                return false;
            }
            #endregion

            #region Add Data
            public void AddData(double data)
            {
                for (int i = DataPoints - 1; i > 0; i--)
                    DataSet[i] = DataSet[i - 1];

                DataSet[0] = data;
                Count++;

                if (Count > DataPoints) Count--;
            }

            public void AddHigh(double data)
            {
                Bars.AddHigh(data);
            }

            public void AddLow(double data)
            {
                Bars.AddLow(data);
            }
            #endregion

            #region Initalize and Clear Data
            public void SetLength(int length)
            {
                Clear();
                Bars.SetMaxBars(length);
            }
            public void Clear()
            {
                Bars.ClearData();
                Count = 0;
                _PivotHighValue = 0;
                _PivotLowValue = 0;
                for (int i = 0; i < DataPoints; i++)
                    DataSet[i] = 0;
            }
            #endregion

            #region Debug
            public override string ToString()
            {
                //string text = string.Format("\nBars maxbars: " + Bars.MaxBars);
                string text2 = string.Format("Pivot Low: " + PivotLow());
                string text3 = string.Format("\tPivot High: " + PivotHigh());
                string text4 = string.Format("\tPivotLowValue: " + PivotLowValue);
                string text5 = string.Format("\tPivothighValue: " + PivotHighValue);
                return text2 + text3 + text4 + text5;
            }
            #endregion
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

        #region FX Modules
        private double GetTickValue(int ticks)
        {
            // 10 ticks make one pip.
            double tickValue = Bars.Info.MinMove / Bars.Info.PriceScale;
            return tickValue * ticks;
        }
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
        #endregion

        // ---------------------------------- Indicator Logic ---------------------------------- //

    }
}