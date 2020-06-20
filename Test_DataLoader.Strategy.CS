using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;

namespace PowerLanguage.Strategy
{
    public class Test_DataLoader : SignalObject
    {
        public Test_DataLoader(object _ctx) : base(_ctx) { }
        private IOrderMarket buy_order;

        protected override void Create()
        {
        }
        protected override void StartCalc()
        {
            Output.Clear();
        }

        bool isLosded = false;
        protected override void CalcBar()
        {
            if (Bars.CurrentBar > 1)
            {
                isLosded = false;
                InstrumentDataRequest Req = Bars.Request;
                Req.Symbol = "USD.JPY";
                Req.Resolution = new Resolution { Size = 5, Type = EResolution.Minute };
                Req.From = Bars.Time[2];
                Req.To = Bars.TimeValue;

                IDataLoaderResult iRes = DataLoader.BeginLoadData(Req, Result =>
                   {
                       if (Result.IsCompleted)
                       {
                           Output.WriteLine("From {0} To {1} Count Bars is {2}", Result.Request.From,
                                                   Result.Request.To, Result.Data.Length);
                           if (Result.Data.Length > 0)
                           {
                               Output.WriteLine("Time = {0}", Result.Data[0].Time);
                               Output.WriteLine("Price = {0}", Result.Data[0].Close);
                           }

                       }
                       DataLoader.EndLoadData(Result);
                   }

                    , null);

            }

        }
        
    }
}