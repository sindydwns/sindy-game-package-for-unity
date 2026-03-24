namespace Sindy.Inven
{
    public interface IReadOnlyEntityStack
    {
        public Entity Entity { get; }
        public long Amount { get; }
    }
}
