using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;

namespace PowerLanguage.Strategy 
{
	public class Test_LimitOrderEx : SignalObject 
	{
		public Test_LimitOrderEx(object _ctx):base(_ctx){}
		private IOrderPriced buy_order;
		private IOrderPriced sell_order;

		[Input]
		public int HowManyBuy
		{
			get;
			set;
		}

		protected override void Create() 
		{
			buy_order = OrderCreator.Limit( new SOrderParameters( Contracts.UserSpecified, "Buy", EOrderAction.Buy ) );
			sell_order = OrderCreator.Limit( new SOrderParameters( Contracts.Default, "Sell", EOrderAction.Sell ) );
			HowManyBuy = 10;
		}

		protected override void StartCalc() 
		{
		}

		protected override void CalcBar()
		{
			if(StrategyInfo.MarketPosition == 0)
			{
				buy_order.Send( Bars.CloseValue, HowManyBuy );
			}
			else if(Bars.CurrentBar % 2 == 0 && StrategyInfo.MarketPosition != 0)
			{
				sell_order.Send( Bars.CloseValue );
			}
		}
	}
}