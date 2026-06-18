using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// 등록한 액션을 다음 Update 프레임에 한 번 실행하는 경량 지연 디스패처.
    ///
    /// 존재 이유:
    /// R3 Subject가 OnNext를 방출하는 도중(예: 버튼 OnClick 핸들러 안)에 그 Subject가 속한
    /// 모델 트리를 Dispose/Destroy하면 방출 중인 구독을 자기 자신이 정리하는 꼴이 되어 재진입 오류가 난다.
    /// 파괴 작업을 <see cref="NextFrame"/>로 미루면 방출 스택을 벗어난 뒤 안전하게 실행된다.
    ///
    /// 이 유틸은 컨트롤러마다 "pendingAction 필드 + Update() 펌프"를 손으로 재구현하던 보일러플레이트를 대체한다.
    /// 화면에 부착할 GameObject가 필요 없고(R3 PlayerLoop 사용), 등록 시점 트리의 수명과 무관하게 실행을 보장한다.
    ///
    /// 사용 예:
    /// <code>
    /// buyButton.OnClick.Subscribe(_ => FrameDispatcher.NextFrame(Reopen));
    /// </code>
    /// </summary>
    public static class FrameDispatcher
    {
        private static readonly Queue<Action> pending = new();
        private static IDisposable loop;

        // SubsystemRegistration에서 재초기화 — "Enter Play Mode (no domain reload)" 옵션에서도
        // 정적 상태가 매 플레이 세션마다 깨끗하게 재구성되도록 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            pending.Clear();
            loop?.Dispose();
            loop = Observable.EveryUpdate().Subscribe(static _ => Flush());
        }

        /// <summary>다음 Update 프레임에 한 번 실행할 액션을 등록한다. null은 무시된다.</summary>
        public static void NextFrame(Action action)
        {
            if (action != null) pending.Enqueue(action);
        }

        private static void Flush()
        {
            // 현재 큐 스냅샷만 실행한다 — 플러시 도중 새로 등록된 액션은 자연히 다음 프레임으로 미뤄진다.
            for (var count = pending.Count; count > 0; count--)
            {
                var action = pending.Dequeue();
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    // 한 액션의 예외가 나머지 플러시를 막지 않도록 격리한다.
                    Debug.LogException(e);
                }
            }
        }
    }
}
