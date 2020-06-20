using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Strategy
{
    public class Test_DataLoader_RT_MC85 : SignalObject
    {
        public Test_DataLoader_RT_MC85(object _ctx) : base(_ctx) { }
        private IOrderMarket buy_order;
        private IDataLoaderResult iRes;

        protected override void Create()
        {
        }
        protected override void StartCalc()
        {
            iRes = null;
            Output.Clear();
        }

        bool isLosded = false;
        protected override void CalcBar()
        {
            int CurrentBar = Bars.CurrentBar + ExecInfo.MaxBarsBack;
            if (Bars.CurrentBar == 90)
            {
                isLosded = false;
                InstrumentDataRequest Req = Bars.Request;
                Req.Symbol = "EUR.JPY";
                Req.Category = ESymbolCategory.Cash;
                Req.Resolution = new Resolution { Size = 5, Type = EResolution.Minute };
                Req.Range = DataRequest.CreateBarsBack(DateTime.Now, 100);
                Req.FilterOutOfRangeData = true;
                Output.WriteLine("My Request: 100 To {0}", DateTime.Now);
                Req.Subscribe2RT = true;

                iRes = DataLoader.BeginLoadData(Req, ResultCallback, null);
            }
        }

        void ResultCallback(IDataLoaderResult Result)
        {
            int countBars = Result.Data.Length;
            if (Result.IsCompleted && countBars > 0)
            {
                Output.WriteLine("Result Request: From {0} To {1} Count Bars is {2}", Result.Request.Range.From,
                                                        Result.Request.Range.To, countBars);
                Output.WriteLine("First bar time = {0}, Last bar time = {1}", Result.Data[0].Time,
                                             Result.Data[countBars - 1].Time);
            }

            if (Result.RTData != null)
            {
                Bar rt = Result.RTData.Value;
                Output.WriteLine("RT Bar: o={0}, h={1}, l={2}, c={3}, dv={4}, uv={5} ", rt.Open, rt.High, rt.Low, rt.Close, rt.DownVolume, rt.UpVolume);
            }

            Output.WriteLine("{0}, {1}, {2}, {3}", Result.Data.Length, Result.Data2 == null, Result.RTData == null, Result.RTData2 == null);
        }

        protected override void StopCalc()
        {
            if (iRes != null)
            {
                Output.WriteLine("Unsubscribe");
                DataLoader.EndLoadData(iRes);
            }
            base.StopCalc();
        }
    }
}