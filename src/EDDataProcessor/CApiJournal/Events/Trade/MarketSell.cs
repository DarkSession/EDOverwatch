namespace EDDataProcessor.CApiJournal.Events.Trade
{
    internal class MarketSell : EjectCargo
    {
        public MarketSell(string type, int count) :
            base(type, count)
        {
        }
    }
}
