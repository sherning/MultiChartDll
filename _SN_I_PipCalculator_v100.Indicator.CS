
//==============================================================================
// Name:          _SN_I_PipCalculator_v100
// Description:   Calculate real-time pip per USD 
// Version:       v.1.0.0
// Date Created:  20 - Nov - 2019
//==============================================================================
// Copyright:     2019, Sher Ning Technologies
//             
// License:     
//==============================================================================

//------------------------------------------------------------------------------
// For use on USD accounts only.
// Calculates the value of Pip, once every session.
// Please Import SN_L_Enums.cs
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    public class _SN_I_PipCalculator_v100 : IndicatorObject
    {
        [Input] public ForexSymbols SymbolTicker { get; set; }
        [Input] public string NoteOnSymbolCatergory { get; set; }
        [Input] public ESymbolCategory SymbolCategory { get; set; }
        [Input] public string NoteOnTestData { get; set; }
        [Input] public SN_Switch TestData { get; set; }

        public _SN_I_PipCalculator_v100 (object _ctx) : base(_ctx)
        {
            SymbolTicker = ForexSymbols.EURJPY;
            NoteOnSymbolCatergory = "For IB Forex Data: Use Cash Only";
            SymbolCategory = ESymbolCategory.Cash;
            NoteOnTestData = "Test underlying symbol data";
            TestData = SN_Switch.Off;
        }

        private IPlotObject pipValueInUSD;
        private IPlotObject testData;
        private _SN_F_AddDataStream_v100 _SN_DataStream;
        private VariableSeries<double> currencyPrice;

        // Create base symbol price
        private _SN_F_AddDataStream_v100 USD_price, CAD_price, JPY_price, AUD_price, CHF_price, EURAUD_price;
        private VariableSeries<bool> sessionInformation;

        private double PipValue;
        protected override void Create()
        {
            // create variable objects, function objects, plot objects etc.
            pipValueInUSD = AddPlot(new PlotAttributes("Pip Value in USD", EPlotShapes.Line, Color.Red));
            testData = AddPlot(new PlotAttributes("test data", EPlotShapes.Line, Color.Red));

            _SN_DataStream = new _SN_F_AddDataStream_v100(this);

            currencyPrice = new VariableSeries<double>(this);

            USD_price = new _SN_F_AddDataStream_v100(this);
            JPY_price = new _SN_F_AddDataStream_v100(this);
            CAD_price = new _SN_F_AddDataStream_v100(this);
            AUD_price = new _SN_F_AddDataStream_v100(this);
            CHF_price = new _SN_F_AddDataStream_v100(this);
            EURAUD_price = new _SN_F_AddDataStream_v100(this);

            sessionInformation = new VariableSeries<bool>(this);
        }
        protected override void StartCalc()
        {
            Output.Clear();
            // Reset PipValue
            PipValue = 0;

            switch (SymbolTicker)
            {
                default:
                case ForexSymbols.EURJPY:
                    _SN_DataStream.BenchmarkTicker = "EUR.JPY";
                    break;

                case ForexSymbols.EURUSD:
                    _SN_DataStream.BenchmarkTicker = "EUR.USD";
                    break;

                case ForexSymbols.EURCAD:
                    _SN_DataStream.BenchmarkTicker = "EUR.CAD";
                    break;

                case ForexSymbols.EURAUD:
                    _SN_DataStream.BenchmarkTicker = "EUR.AUD";
                    break;

                case ForexSymbols.GBPUSD:
                    _SN_DataStream.BenchmarkTicker = "GBP.USD";
                    break;

                case ForexSymbols.GBPJPY:
                    _SN_DataStream.BenchmarkTicker = "GBP.JPY";
                    break;

                case ForexSymbols.GBPCAD:
                    _SN_DataStream.BenchmarkTicker = "GBP.CAD";
                    break;

                case ForexSymbols.AUDUSD:
                    _SN_DataStream.BenchmarkTicker = "AUD.USD";
                    break;

                case ForexSymbols.AUDJPY:
                    _SN_DataStream.BenchmarkTicker = "AUD.JPY";
                    break;

                case ForexSymbols.AUDCAD:
                    _SN_DataStream.BenchmarkTicker = "AUD.CAD";
                    break;

                case ForexSymbols.USDJPY:
                    _SN_DataStream.BenchmarkTicker = "USD.JPY";
                    break;

                case ForexSymbols.USDCHF:
                    _SN_DataStream.BenchmarkTicker = "USD.CHF";
                    break;

                case ForexSymbols.USDCAD:
                    _SN_DataStream.BenchmarkTicker = "USD.CAD";
                    break;
            }
            // assign inputs 
            // IB Forex - Cash 
            _SN_DataStream.SymbolCategory = this.SymbolCategory;

            USD_price.SymbolCategory = this.SymbolCategory;
            AUD_price.SymbolCategory = this.SymbolCategory;
            CAD_price.SymbolCategory = this.SymbolCategory;
            JPY_price.SymbolCategory = this.SymbolCategory;
            CHF_price.SymbolCategory = this.SymbolCategory;
            EURAUD_price.SymbolCategory = this.SymbolCategory;

            USD_price.BenchmarkTicker = "EUR.USD";
            AUD_price.BenchmarkTicker = "AUD.USD";
            JPY_price.BenchmarkTicker = "USD.JPY";
            CHF_price.BenchmarkTicker = "USD.CHF";
            CAD_price.BenchmarkTicker = "USD.CAD";
            EURAUD_price.BenchmarkTicker = "EUR.AUD";

            //_SN_PipCalculator_USD();
        }
        protected override void CalcBar()
        {
            sessionInformation.Value = Bars.LastBarInSession;
            currencyPrice.Value = _SN_DataStream[0];
            //double PipValue = _SN_PipCalculator_USD();
            // indicator logic 
            PipValue = _SN_PipCalculator_USD();
            if (sessionInformation[0] == true && TestData == SN_Switch.Off)
            {
                pipValueInUSD.Set(PipValue);
            }
            
            if(sessionInformation[0] == true && TestData == SN_Switch.On)
            {
                testData.Set(currencyPrice.Value);
            }
        }
        private double _SN_PipCalculator_USD()
        {
            string[] symbolParts = _SN_DataStream.BenchmarkTicker.Split('.');
            string baseSymbol = symbolParts[1];
            double conversionFactor, pipValue, priceScale;

            switch (baseSymbol)
            {
                default:
                    {
                        conversionFactor = 1.0;
                        priceScale = 0.00001;
                        break;
                    }

                case "USD":
                    {
                        conversionFactor = 1.0;
                        priceScale = 0.00001;
                        break;
                    }

                case "JPY":
                    {
                        conversionFactor = (1.0 / JPY_price[0]);
                        priceScale = 0.001;
                        break;
                    }

                case "CHF":
                    {
                        conversionFactor = (1.0 / CHF_price[0]);
                        priceScale = 0.00001;
                        break;
                    }

                case "CAD":
                    {
                        priceScale = 0.00001;
                        conversionFactor = (1.0 / CAD_price[0]);
                        break;
                    }

                case "AUD":
                    {
                        priceScale = 0.00001;
                        conversionFactor = 1;
                        break;
                    }
            }

            // Output.WriteLine("Minmove = {0}, PriceScale = {1}", fubBenchmark.Bars.Info.MinMove, priceScale);
            // pipValue = conversionFactor * (100000 * priceScale);
            pipValue = 10 * priceScale * 100000 * conversionFactor;
            // Output.WriteLine("Conversion Factor = {0}, priceScale = {1}", conversionFactor, priceScale);
            // Output.WriteLine("=======================================================================");
            return pipValue;
        }
    }
}