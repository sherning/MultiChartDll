
using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Indicator
{
    // Remember to uncomment enum to use it.
    public enum ForexSymbols
    { EURJPY, EURUSD, EURCAD, EURAUD, GBPUSD, GBPJPY, GBPCAD, AUDUSD, AUDJPY, AUDCAD, USDJPY, USDCHF, USDCAD }

    public enum SN_Switch { On, Off }
    public class _SN_I_PipCalculator : IndicatorObject
    {
        [Input] public ForexSymbols SymbolTicker { get; set; }
        [Input] public string NoteOnSymbolCatergory { get; set; }
        [Input] public ESymbolCategory SymbolCategory { get; set; }
        [Input] public string NoteOnTestData { get; set; }
        [Input] public SN_Switch TestData { get; set; }

        public _SN_I_PipCalculator (object _ctx) : base(_ctx)
        {
            SymbolTicker = ForexSymbols.EURJPY;
            NoteOnSymbolCatergory = "For IB Forex Data: Use Cash Only";
            SymbolCategory = ESymbolCategory.Cash;
            NoteOnTestData = "Test underlying symbol data";
            TestData = SN_Switch.Off;
        }

        private IPlotObject pipValueInUSD;
        private IPlotObject testData;
        private _SN_F_AddDataStream _SN_DataStream;
        private VariableSeries<double> currencyPrice;

        // Create base symbol price
        private _SN_F_AddDataStream USD_price, CAD_price, JPY_price, AUD_price, CHF_price, EURAUD_price;
        private VariableSeries<bool> sessionInformation;

        private double PipValue;
        protected override void Create()
        {
            // create variable objects, function objects, plot objects etc.
            pipValueInUSD = AddPlot(new PlotAttributes("Pip Value in USD", EPlotShapes.Line, Color.Red));
            testData = AddPlot(new PlotAttributes("test data", EPlotShapes.Line, Color.Red));

            _SN_DataStream = new _SN_F_AddDataStream(this);

            currencyPrice = new VariableSeries<double>(this);

            USD_price = new _SN_F_AddDataStream(this);
            JPY_price = new _SN_F_AddDataStream(this);
            CAD_price = new _SN_F_AddDataStream(this);
            AUD_price = new _SN_F_AddDataStream(this);
            CHF_price = new _SN_F_AddDataStream(this);
            EURAUD_price = new _SN_F_AddDataStream(this);

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
                    _SN_DataStream.ForexSymbol = "EUR.JPY";
                    break;

                case ForexSymbols.EURUSD:
                    _SN_DataStream.ForexSymbol = "EUR.USD";
                    break;

                case ForexSymbols.EURCAD:
                    _SN_DataStream.ForexSymbol = "EUR.CAD";
                    break;

                case ForexSymbols.EURAUD:
                    _SN_DataStream.ForexSymbol = "EUR.AUD";
                    break;

                case ForexSymbols.GBPUSD:
                    _SN_DataStream.ForexSymbol = "GBP.USD";
                    break;

                case ForexSymbols.GBPJPY:
                    _SN_DataStream.ForexSymbol = "GBP.JPY";
                    break;

                case ForexSymbols.GBPCAD:
                    _SN_DataStream.ForexSymbol = "GBP.CAD";
                    break;

                case ForexSymbols.AUDUSD:
                    _SN_DataStream.ForexSymbol = "AUD.USD";
                    break;

                case ForexSymbols.AUDJPY:
                    _SN_DataStream.ForexSymbol = "AUD.JPY";
                    break;

                case ForexSymbols.AUDCAD:
                    _SN_DataStream.ForexSymbol = "AUD.CAD";
                    break;

                case ForexSymbols.USDJPY:
                    _SN_DataStream.ForexSymbol = "USD.JPY";
                    break;

                case ForexSymbols.USDCHF:
                    _SN_DataStream.ForexSymbol = "USD.CHF";
                    break;

                case ForexSymbols.USDCAD:
                    _SN_DataStream.ForexSymbol = "USD.CAD";
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

            USD_price.ForexSymbol = "EUR.USD";
            AUD_price.ForexSymbol = "AUD.USD";
            JPY_price.ForexSymbol = "USD.JPY";
            CHF_price.ForexSymbol = "USD.CHF";
            CAD_price.ForexSymbol = "USD.CAD";
            EURAUD_price.ForexSymbol = "EUR.AUD";

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

            if (sessionInformation[0] == true && TestData == SN_Switch.On)
            {
                testData.Set(currencyPrice.Value);
            }
        }
        private double _SN_PipCalculator_USD()
        {
            string[] symbolParts = _SN_DataStream.ForexSymbol.Split('.');
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