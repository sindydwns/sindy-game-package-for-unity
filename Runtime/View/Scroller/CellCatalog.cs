using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// 셀 키 → prefab 매핑을 담는 ScriptableObject 카탈로그.
    /// 코드 등록(RegisterCell/RegisterGlobalCell)과 달리 등록 타이밍 제약이 없고,
    /// 디자이너가 Inspector에서 매핑을 확인/수정할 수 있으며,
    /// 정적 가변 상태(도메인 리로드 이슈)가 없다.
    /// ScrollerFeatureView의 catalog 필드에 할당해 사용한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Sindy/Scroller/Cell Catalog", fileName = "CellCatalog")]
    public class CellCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public SindyComponent prefab;
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(string key, out SindyComponent prefab)
        {
            foreach (var entry in entries)
            {
                if (entry != null && entry.key == key && entry.prefab != null)
                {
                    prefab = entry.prefab;
                    return true;
                }
            }
            prefab = null;
            return false;
        }
    }
}
