//==============================================================================
// Name           : __SherNing_ProjectArrow
// Description    : Get Bars Info from each arrow object and save to file.
// Version        : v.1.0.0
// Date Created   : 15 - Apr - 2020
// Time Taken     : 
// Remarks        :
//==============================================================================
// Copyright      : 2020, Sher Ning Technologies           
// License        :      
//==============================================================================

/* ------------------------------- Version 1.0.0 -------------------------------
 * 14/4/2020
 * Completed saving and loading arrows to and from file.
 * 
 * 22/4/2020
 * Setup Display Information on Chart.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    [MouseEvents(true), SameAsSymbol(true), RecoverDrawings(false), UpdateOnEveryTick(true)]
    public class __SherNing_ProjectArrow : IndicatorObject
    {
        // Constructor will initialize fields, unless you have non-default values.
        private int NumOfArrows;
        private DateTime[] ArrowsLocation;
        private bool[] ArrowsDirection;

        // Global Counter
        private int Idx;

        // File / Directory Path
        private readonly string DirPath = @"C:\MultiChart_Temp";
        private readonly string FilePath = @"C:\MultiChart_Temp\Arrows.txt";

        // Display Info
        private bool CreateDisplayInfo;
        private ITextObject DisplayStatusInfo;
        private ITextObject DisplayPriceInfo;

        // Pip Info
        private double PointsPerPip;

        public __SherNing_ProjectArrow(object _ctx) : base(_ctx) { }
        protected override void Create()
        {
            ArrowsDirection = new bool[5000];
            ArrowsLocation = new DateTime[5000];
        }
        protected override void StartCalc()
        {
            Output.Clear();
            CreateDisplayInfo = false;
            Idx = 0;

            // Calculate pip on chart.
            if (Bars.Info.Exchange == "FOREX" || Bars.Info.Exchange == "IDEALPRO")
            {
                PointsPerPip = (Bars.Info.MinMove / Bars.Info.PriceScale) * 10;
            }
            else
            {
                PointsPerPip = Bars.Info.MinMove / Bars.Info.PriceScale;
            }
        }
        protected override void CalcBar()
        {
            // Calculate at Bar Close.
            if (Bars.Status == EBarState.Close)
            {
                if (Bars.CurrentBar == 1)
                {
                    // Save list of arrows to file.
                    SaveArrowsToTxt();

                    // Load arrow objects on first bar.
                    GetArrowsFromFile();

                    // Display Chart Info
                }

                // Make sure the number of arrows is not zero.
                if (NumOfArrows > 0)
                {
                    SetArrowsOnChartFromFile();
                }
            }

            if (Bars.LastBarOnChart)
            {
                DisplayChartInfo();

                ExecControl.RecalcLastBarAfter(TimeSpan.FromSeconds(1));
            }

            // Calculate per tick
            UpdateChartInfo();
        }

        protected override void OnMouseEvent(MouseClickArgs arg)
        {
            if ((Keys.Shift == arg.keys) && (MouseButtons.Left == arg.buttons))
            {
                ExecControl.Recalculate();
            }
        }

        #region Display Information on Chart
        private void DisplayChartInfo()
        {
            if (CreateDisplayInfo == false)
            {
                var priceLoc = Environment.HighestScaleValue - (20 * PointsPerPip);

                // Display Status Info
                DisplayStatusInfo = DrwText.Create(
                    new ChartPoint(Bars.Time[0], priceLoc),
                    "Display Status Info:");

                // Display Price Info
                // Use pips to adjust display from chart, this should fix the position on chart.
                DisplayPriceInfo = DrwText.Create(
                    new ChartPoint(Bars.Time[0], priceLoc),
                    "Display Price Info:");

                // Status Info Properties
                DisplayStatusInfo.Locked = true;
                DisplayStatusInfo.AnchorToBars = true;
                DisplayStatusInfo.Border = false;
                DisplayStatusInfo.Size = 11;
                DisplayStatusInfo.SetFont(FontStyle.Bold, true);
                DisplayStatusInfo.FontName = "Arial";

                // To the left of the imaginary line time[0] + 10 hours.
                DisplayStatusInfo.HStyle = ETextStyleH.Right;
                DisplayStatusInfo.VStyle = ETextStyleV.Above;

                // Price Info Properties
                DisplayPriceInfo.Locked = true;

                // false by default. will move together with new bars.
                DisplayPriceInfo.AnchorToBars = true;
                DisplayPriceInfo.Border = false;
                DisplayPriceInfo.HStyle = ETextStyleH.Right;
                DisplayPriceInfo.VStyle = ETextStyleV.Below;
                DisplayPriceInfo.FontName = "Arial";
                DisplayPriceInfo.Size = 11;

                // Create display info on chart only once.
                CreateDisplayInfo = true;
            }
        }
        private void UpdateChartInfo()
        {
            // If Display not created, return.
            if (CreateDisplayInfo != true) return;

            // Update DisplayInfo 
            if (Environment.IsRealTimeCalc == true)
            {
                DisplayStatusInfo.Text = string.Format("Status: Real-Time\n"
                    + "Time: {0}\n", Bars.TimeValue.ToString("d-MM h:mm tt"));

                DisplayPriceInfo.Text = string.Format("Close: {0}\nHigh: {1}\nLow: {2}",
                    Bars.CloseValue, Bars.HighValue, Bars.LowValue.ToString("C5"));

                // Set Display Color Settings
                DisplayStatusInfo.Color = Color.LimeGreen;

                // Update at bar close.
                if (Bars.Status == EBarState.Inside)
                {
                    // Price Location
                    var priceLoc = Environment.HighestScaleValue - (20 * PointsPerPip);
                    //var timeLoc = Environment.RightScreenTime - 
                    //new DateTime(0,0,0,0,(int)(Bars.Info.Resolution.Size * 5),0);

                    //Update Location to follow latest Time[0]
                    DisplayStatusInfo.Location = new ChartPoint(Bars.Time[0].AddMinutes(Bars.Info.Resolution.Size * 3),
                        priceLoc);

                    DisplayPriceInfo.Location = new ChartPoint(Bars.Time[0].AddMinutes(Bars.Info.Resolution.Size * 3),
                        priceLoc);
                }
            }
            else
            {
                // Not Real-time data
                DisplayStatusInfo.Text = string.Format("Status: Historical");
                DisplayStatusInfo.Color = Color.OrangeRed;
            }
        }
        #endregion

        #region Save and Load Arrows from file
        private void SetArrowsOnChartFromFile()
        {
            // Declare and initialize local variables
            bool barFound = false;
            int count = 0;

            // Create arrow variable to change arrow properties.
            IArrowObject arrow;

            // Tested. Do not need to worry of order in which arrows are placed on chart. 
            while (barFound == false && count < NumOfArrows)
            {
                // Remember calcbar loops through each historical bar.
                if (Bars.Time[0] == ArrowsLocation[count])
                {
                    // There should only be one arrow per historical bar
                    // Optimize fast exit while loop
                    barFound = true;

                    if (ArrowsDirection[count] == false)
                    {
                        // Long point from bottom up.
                        ChartPoint thisLoc = new ChartPoint
                        (ArrowsLocation[count], Bars.Low.Lowest(3) - (Bars.Range() * 0.15));

                        arrow = DrwArrow.Create(thisLoc, false);
                        arrow.Text = "LONG";
                        arrow.TextColor = Color.LimeGreen;
                        arrow.Color = Color.LimeGreen;
                        arrow.Locked = true;
                    }
                    else
                    {
                        // Short Arrow point from top down
                        ChartPoint thisLoc = new ChartPoint
                        (ArrowsLocation[count], Bars.High.Highest(3) + (Bars.Range() * 0.15));

                        arrow = DrwArrow.Create(thisLoc, true);
                        arrow.Text = "SHORT";
                        arrow.TextColor = Color.HotPink;
                        arrow.Color = Color.HotPink;
                        arrow.Locked = true;
                    }
                }

                count++;
            }

            // Hide all the maunally created arrows.
            foreach (IArrowObject arrowObj in DrwArrow.GetArrowObjects(EDrawingSource.Manual))
            {
                //Output.WriteLine(arrowObj.ID.ToString());
                arrowObj.Color = Color.Transparent;
            }
        }

        private void GetArrowsFromFile()
        {
            // Local variables
            int count = 0;

            // Check if file exists
            if (File.Exists(FilePath))
            {
                // foreach arrow copy to temp array.
                using (StreamReader reader = new StreamReader(FilePath))
                {
                    // Read title, do nothing.
                    reader.ReadLine();

                    // Get the number of arrows from file.
                    NumOfArrows = int.Parse(reader.ReadLine());

                    while (!reader.EndOfStream)
                    {
                        string[] arrowInfo = reader.ReadLine().Split(' ');

                        // Split Date Time AM/PM
                        ArrowsLocation[count] = DateTime.Parse(String.Format("{0} {1} {2}",
                            arrowInfo[0],
                            arrowInfo[1],
                            arrowInfo[2]));

                        ArrowsDirection[count] = Boolean.Parse(arrowInfo[3]);
                        count++;
                    }
                }

                // Check output from indicator cache
                // Good to have a boolean to on / off debug text.
                for (int i = 0; i < NumOfArrows; i++)
                {
                    //Output.WriteLine(ArrowsLocation[i].ToString());
                    //Output.WriteLine(ArrowsDirection[i].ToString());
                }
            }
        }

        private void SaveArrowsToTxt()
        {
            NumOfArrows = 0;

            // Check if directory exist
            if (!Directory.Exists(DirPath))
            {
                DirectoryInfo newDirectory = new DirectoryInfo(DirPath);
                newDirectory.Create();
            }

            // Count only manual arrows.
            foreach (IArrowObject arrow in DrwArrow.GetArrowObjects(EDrawingSource.Manual))
            {
                // Count number of arrows
                NumOfArrows++;
            }

            // No arrows
            if (NumOfArrows == 0) return;

            // Write to file.
            using (StreamWriter sw = new StreamWriter(FilePath, append: false))
            {
                sw.WriteLine("------------------- List of Arrows -------------------");

                // The number of arrows in file.
                sw.WriteLine(NumOfArrows.ToString());

                // Write only manual arrows to file.
                foreach (IArrowObject arrow in DrwArrow.GetArrowObjects(EDrawingSource.Manual))
                {
                    // save arrow objects' data to file.
                    sw.WriteLine("{0} {1}",
                        arrow.Location.Time.ToString("dd-MMM-yy h:mm:ss tt"),
                        arrow.Direction);
                }
            }
        }

        protected override void Destroy()
        {
            foreach (IArrowObject arrow in DrwArrow.GetArrowObjects(EDrawingSource.AnyTechOrManual))
            {
                arrow.Delete();
            }
        }

        #endregion
    }
}