using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using R3;
using Sindy.Common;
using Sindy.Reactive;

namespace Sindy.Inven
{
    /// <summary>
    /// Inventory의 Entity가 목표 수량에 도달했는지 추적하는 클래스입니다.
    /// </summary>
    public class Checkpoint : IDisposable, ISerializable<int, Entity>
    {
        private readonly CompositeDisposable disposibles = new();
        public bool IsDisposed { get; private set; } = false;
        protected ReactiveList<MissionTracker> missions = new();
        private ReactiveListCondition<MissionTracker> isComplete;
        public ReadOnlyReactiveProperty<bool> IsComplete => isComplete.All;
        public IEnumerable<ILoadMission> Missions => missions;

        public Checkpoint() => InitConditions();
        public Checkpoint(IEnumerable<Mission> missions)
        {
            foreach (var mission in missions)
            {
                AddMission(mission);
            }
            InitConditions();
        }
        public Checkpoint(IEnumerable<ILoadMission> missions)
        {
            foreach (var loadMission in missions)
            {
                AddMission(loadMission);
            }
            InitConditions();
        }
        private void InitConditions()
        {
            isComplete = new(missions, x => x.IsComplete);
        }

        public Checkpoint AddMission(IEnumerable<Mission> missions)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Checkpoint), "Cannot add mission to a disposed Checkpoint.");
            }
            foreach (var mission in missions)
            {
                AddMission(mission);
            }
            return this;
        }
        public Checkpoint AddMission(Mission mission)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Checkpoint), "Cannot add mission to a disposed Checkpoint.");
            }
            if (mission.inventory == null)
            {
                throw new ArgumentNullException(nameof(mission.inventory), "Inventory cannot be null.");
            }
            if (mission.target == null)
            {
                throw new ArgumentNullException(nameof(mission.target), "Entity cannot be null.");
            }

            var tracker = new MissionTracker(mission);
            missions.Add(tracker);
            return this;
        }
        public Checkpoint AddMission(IEnumerable<ILoadMission> loadMissions)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Checkpoint), "Cannot add mission to a disposed Checkpoint.");
            }
            foreach (var loadMission in loadMissions)
            {
                AddMission(loadMission);
            }
            return this;
        }
        public Checkpoint AddMission(ILoadMission loadMission)
        {
            var mission = loadMission.Mission;
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Checkpoint), "Cannot add mission to a disposed Checkpoint.");
            }
            if (mission.inventory == null)
            {
                throw new ArgumentNullException(nameof(mission.inventory), "Inventory cannot be null.");
            }
            if (mission.target == null)
            {
                throw new ArgumentNullException(nameof(mission.target), "Entity cannot be null.");
            }

            var stack = mission.Inventory.GetEntityStack(mission.target);
            var tracker = new MissionTracker(mission);
            if (mission.syncronized == false)
            {
                tracker.Amount.Value = loadMission.Amount.Value;
            }
            missions.Add(tracker);
            return this;
        }

        public override string ToString()
        {
            var missions = string.Join(" ",
                Missions.Select(x => $"{x.Mission.target.name}:{x.Amount}/{x.Mission.amount}"));
            return $"missions: {this.missions.Count}, complete: {IsComplete}\n{missions}";
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            disposibles.Dispose();
            disposibles.Clear();
            isComplete.Dispose();

            foreach (var tracker in missions)
            {
                tracker.Dispose();
            }

            IsDisposed = true;
        }

        public JContainer Serialize()
        {
            return new JArray(
                missions.Select(t => new JObject
                {
                    { "inv", t.Mission.inventory.id },
                    { "ent", t.Mission.target.id },
                    { "cnt", t.Amount.Value },
                })
            );
        }

        /// <summary>
        /// Deserialize 전에 Checkpoint에 미션들을 추가해두어야 합니다.
        /// 추가된 미션들 중 inventory와 entity가 일치하는 미션이 있을경우
        /// 해당 미션의 현재 진행량을 data에서 불러온 값으로 설정합니다.
        /// </summary>
        public void Deserialize(JContainer data, Dictionary<int, Entity> entities)
        {
            if (data is not JArray arr)
            {
                throw new ArgumentException("Data must be a JArray.", nameof(data));
            }
            var deserializeMissionList = missions.ToList();
            foreach (var item in arr)
            {
                if (item is not JObject obj)
                {
                    throw new ArgumentException("Each item in the array must be a JObject.", nameof(data));
                }
                var invId = obj.Value<int>("inv");
                var entId = obj.Value<int>("ent");
                var cnt = obj.Value<long>("cnt");

                var inven = entities[invId] as InventoryEntity;
                var entity = entities[entId];

                var tracker = deserializeMissionList.FirstOrDefault(tracker =>
                    tracker.Mission.inventory == inven && tracker.Mission.target == entity
                );

                if (tracker != null)
                {
                    tracker.Amount.Value = cnt;
                    deserializeMissionList.Remove(tracker);
                }
            }
        }

        protected class MissionTracker : IDisposable, ILoadMission
        {
            private readonly Mission mission;
            public Mission Mission => mission;
            public ReactiveProperty<long> Amount { get; private set; } = new();
            public ReadOnlyReactiveProperty<bool> IsComplete { get; private set; }
            private readonly IEntityStack stack;
            public long Goal => mission.amount;

            public MissionTracker(Mission mission)
            {
                this.mission = mission;

                stack = mission.Stack;
                Amount.Value = mission.startZero ? 0 : stack.Amount;
                IsComplete = Amount
                    .Select(amount => amount >= Goal)
                    .ToReadOnlyReactiveProperty();
            }

            public void Dispose()
            {
                Amount?.Dispose();
                IsComplete?.Dispose();
            }
        }


        public interface ILoadMission
        {
            Mission Mission { get; }
            ReactiveProperty<long> Amount { get; }
        }
    }
}
