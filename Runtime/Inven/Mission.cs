using System;

namespace Sindy.Inven
{
    [Serializable]
    public class Mission : TargetEntity
    {
        /// <summary>
        /// 미션 설명용 엔티티
        /// </summary>
        public Entity descriptor;
        /// <summary>
        /// 추적할 인벤토리와의 수량 동기화 여부
        /// </summary>
        public bool syncronized;
        /// <summary>
        /// 미션 시작 시 수량을 0으로 초기화할지 여부. syncronized가 false일 때만 적용됨.
        /// </summary>
        public bool startZero;

        public Inventory Inventory => inventory.Inventory;

        public Mission(
            TargetEntity target,
            bool syncronized = false,
            bool startZero = false) : base(target.inventory, target.target, target.amount)
        {
            this.syncronized = syncronized;
            this.startZero = startZero;
        }

        public Mission(
            InventoryEntity inventory,
            Entity target,
            long amount,
            bool syncronized = false,
            bool startZero = false,
            Entity descriptor = null) : base(inventory, target, amount)
        {
            this.syncronized = syncronized;
            this.startZero = startZero;
            this.descriptor = descriptor;
        }
    }

}
