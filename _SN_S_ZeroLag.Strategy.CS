using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;

namespace PowerLanguage.Strategy
{
    public class _SN_S_ZeroLag : SignalObject
    {
        public enum SN_Switch { On, Off }
        [Input] public int Length { get; set; }
        [Input] public int GainLimit { get; set; }
        [Input] public int Thresh { get; set; }
        [Input] public double LeastError { get; set; }
        [Input] public SN_Switch IsDebugMode { get; set; }

        public _SN_S_ZeroLag(object _ctx) : base(_ctx)
        {
            Length = 20;
            GainLimit = 50;
            Thresh = 1;
            LeastError = 1000000.0;
            IsDebugMode = SN_Switch.Off;
        }

        private VariableSeries<double> EMA_VariableSeries, EC_VariableSeries;
        private VariableObject<double> alpha_VariableObject;

        private double emaPreviousValue;

        private IOrderMarket buyOrder, sellOrder;
        protected override void Create()
        {
            // create variable objects, function objects, order objects etc.
            EMA_VariableSeries = new VariableSeries<double>(this);
            EC_VariableSeries = new VariableSeries<double>(this);
            alpha_VariableObject = new VariableObject<double>(this);

            // Create buy and sell orders
            buyOrder = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.Buy));
            sellOrder = OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, EOrderAction.SellShort));
        }
        protected override void StartCalc()
        {
            Output.Clear();
            // assign inputs 
        }
        protected override void CalcBar()
        {
            if (Bars.Status == EBarState.Close)
            {
                double alpha = 2.0 / (Length + 1.0);
                double gain, error;
                double bestGain = 0;
                if (Bars.CurrentBar == 1)
                {
                    EMA_VariableSeries.Value = Bars.CloseValue;
                    EC_VariableSeries.Value = Bars.CloseValue;
                }
                else
                {
                    emaPreviousValue = EMA_VariableSeries[1];
                    EMA_VariableSeries.Value = alpha * Bars.CloseValue + (1.0 - alpha) * emaPreviousValue;
                }

                for (int i = -GainLimit; i < GainLimit; i++)
                {
                    gain = i / 10.0;
                    EC_VariableSeries.Value = alpha * EMA_VariableSeries.Value + gain * (Bars.CloseValue - EC_VariableSeries[1])
                                                    + (1.0 - alpha) * EC_VariableSeries[1];

                    error = Bars.CloseValue - EC_VariableSeries[0];

                    if (Math.Abs(error) < LeastError)
                    {
                        LeastError = Math.Abs(error);
                        bestGain = gain;
                    }
                }

                EC_VariableSeries.Value = alpha * (EMA_VariableSeries[0] + bestGain * (Bars.CloseValue - EC_VariableSeries[1]))
                                                    + (1.0 - alpha) * EC_VariableSeries[1];

                StrategyInfo.SetPlotValue(81, EMA_VariableSeries.Value);
                StrategyInfo.SetPlotValue(82, EC_VariableSeries.Value);

                // strategy logic 

                if (EC_VariableSeries[0] > EMA_VariableSeries[0] && (100 * LeastError / Bars.CloseValue) > Thresh)
                {
                    buyOrder.Send();
                }
                if (EC_VariableSeries[0] < EMA_VariableSeries[0] && (100 * LeastError / Bars.CloseValue) > Thresh)
                {
                    sellOrder.Send();
                }
            }
        }
    }
}