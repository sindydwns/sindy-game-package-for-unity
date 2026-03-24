namespace Sindy.Inven
{
    public class InventoryEntity : Entity
    {
        public InventoryReference inventory;
        public Inventory Inventory => inventory.Value;
    }
}
