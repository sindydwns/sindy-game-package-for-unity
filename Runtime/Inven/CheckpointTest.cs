using UnityEngine;

namespace Sindy.Inven.Test
{
    public class CheckpointTest : MonoBehaviour
    {
        [SerializeField] private InventoryReference testInventory;
        [SerializeField] private Entity testItem1;
        [SerializeField] private Entity testItem2;
        [SerializeField] private CheckpointReference checkpointReference;

        void OnDisable()
        {
            checkpointReference.Value?.Dispose();
        }

        [ContextMenu("Print State")]
        public void PrintState()
        {
            Debug.Log(checkpointReference.Value);
        }

        // [ContextMenu("Subscribe")]
        // public void Subscribe()
        // {
        //     var checkpoint = checkpointReference.Value;
        //     checkpoint.Subscribe(onChange: (mission) =>
        //     {
        //         Debug.Log($"{checkpoint}\n{mission}");
        //     }, onComplete: (checkpoint, complete) =>
        //     {
        //         Debug.Log($"{checkpoint}\ncompleted: {complete}");
        //     });
        // }

        // [ContextMenu("Subscribe first")]
        // public void SubscribeFirst()
        // {
        //     var checkpoint = checkpointReference.Value;
        //     checkpoint.Subscribe(onChange: (mission) =>
        //     {
        //         Debug.Log($"{checkpoint}\n{mission}");
        //     }, onComplete: (checkpoint, complete) =>
        //     {
        //         Debug.Log($"{checkpoint}\ncompleted: {complete}");
        //     }, first: true);
        // }

        // [ContextMenu("Subscribe once")]
        // public void SubscribeOnce()
        // {
        //     var checkpoint = checkpointReference.Value;
        //     checkpoint.Subscribe(onChange: (mission) =>
        //     {
        //         Debug.Log($"{checkpoint}\n{mission}");
        //     }, onComplete: (checkpoint, complete) =>
        //     {
        //         Debug.Log($"{checkpoint}\ncompleted: {complete}");
        //     }, onlyOnce: true);
        // }

        // [ContextMenu("Tracking")]
        // public void Tracking()
        // {
        //     var checkpoint = checkpointReference.Value;
        //     Debug.Log($"Tracking: {checkpoint.Tracking()}");
        // }

        // [ContextMenu("Tracking until complete")]
        // public void TrackingWithCondition()
        // {
        //     var checkpoint = checkpointReference.Value;
        //     Debug.Log($"Tracking until complete: {checkpoint.Tracking(true)}");
        // }

        // [ContextMenu("Untracking")]
        // public void Untracking()
        // {
        //     var checkpoint = checkpointReference.Value;
        //     checkpoint.Untracking();
        //     Debug.Log($"Untracking");
        // }

        [ContextMenu("Dispose")]
        public void Dispose()
        {
            checkpointReference.Value?.Dispose();
        }

        [ContextMenu("Add Item 1")]
        public void AddItem1()
        {
            testInventory.Value.Add(testItem1);
            Debug.Log($"Added {testItem1.nameId} to inventory.");
        }

        [ContextMenu("Remove Item 1")]
        public void RemoveItem1()
        {
            testInventory.Value.Remove(testItem1, 1);
            Debug.Log($"Removed {testItem1.nameId} from inventory.");
        }

        [ContextMenu("Add Item 2")]
        public void AddItem2()
        {
            testInventory.Value.Add(testItem2);
            Debug.Log($"Added {testItem2.nameId} to inventory.");
        }

        [ContextMenu("Remove Item 2")]
        public void RemoveItem2()
        {
            testInventory.Value.Remove(testItem2, 1);
            Debug.Log($"Removed {testItem2.nameId} from inventory.");
        }
    }
}
