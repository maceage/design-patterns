using AdapterPattern.Domain.Interfaces;
using AdapterPattern.Domain.PinTypes;

namespace AdapterPattern.Domain.Adapters
{
	public class UkPowerPlugAdapter : IPowerPlugAdapter
	{
		private readonly IUkPowerPlug _ukPowerPlug;

		public UkPowerPlugAdapter(IUkPowerPlug ukPowerPlug)
		{
			_ukPowerPlug = ukPowerPlug;
		}

		public void SupplyPower(Electricity electricity)
		{
			RectangularPin rectangularPin1 = new RectangularPin();
			RectangularPin rectangularPin2 = new RectangularPin();
			RectangularPin rectangularPin3 = new RectangularPin();

			_ukPowerPlug.SupplyPower(electricity, rectangularPin1, rectangularPin2, rectangularPin3);
		}
	}
}