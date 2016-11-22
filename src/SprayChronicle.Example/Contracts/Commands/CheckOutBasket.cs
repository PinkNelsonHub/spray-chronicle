namespace SprayChronicle.Example.Contracts.Commands
{
    public sealed class CheckOutBasket
    {
        public readonly string BasketId;

        public readonly string OrderId;

        public CheckOutBasket(string basketId, string orderId)
        {
            BasketId = basketId;
            OrderId = orderId;
        }
    }
}
