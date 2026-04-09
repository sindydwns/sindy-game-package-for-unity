using System.IO;
using Sindy.View;
using Sindy.View.Components;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.Test.Editor
{
    /// <summary>
    /// TestViewComponent에서 사용하는 ViewComponent 프리팹을 생성합니다.
    /// Menu: Tools/Sindy/Create ViewComponent Test Prefabs
    /// </summary>
    public static class ViewComponentPrefabCreator
    {
        private const string PrefabOutputPath = "Assets/sindy-game-package-for-unity/Tests/Runtime/ViewComponentTest/Prefabs";

        [MenuItem("Tools/Sindy/Create ViewComponent Test Prefabs")]
        public static void CreateAll()
        {
            if (!Directory.Exists(PrefabOutputPath))
                Directory.CreateDirectory(PrefabOutputPath);

            CreateLabelPrefab();
            CreateButtonPrefab();
            CreateSearchInputPrefab();
            CreateIconPrefab();
            CreateGaugePrefab();
            CreateNoticePrefab();
            CreateToastPrefab();
            CreateCharacterProfilePrefab();
            CreateShopItemSlotPrefab();
            CreateShopPrefab();
            CreateCharacterSlotPrefab();
            CreateCharacterInventoryPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ViewComponentPrefabCreator] 모든 프리팹 생성 완료.");
        }

        // ── 헬퍼 ────────────────────────────────────────────────────────────────

        private static string PrefabPath(string name) => $"{PrefabOutputPath}/{name}.prefab";

        private static GameObject SavePrefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(name));
            Object.DestroyImmediate(go);
            Debug.Log($"  Created: {name}.prefab");
            return prefab;
        }

        private static TMP_Text AddTMPText(GameObject parent, string childName = "Text")
        {
            var child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<TextMeshProUGUI>();
        }

        private static Button AddButton(GameObject go)
        {
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }

        private static Image AddImage(GameObject go)
        {
            return go.AddComponent<Image>();
        }

        private static Image AddFillImage(GameObject parent, string childName = "Fill")
        {
            var child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            var img = child.AddComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            return img;
        }

        private static Toggle AddToggle(GameObject go, string label = "Tab")
        {
            go.AddComponent<Image>();
            var toggle = go.AddComponent<Toggle>();
            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>();
            var check = new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(bg.transform, false);
            var checkImg = check.AddComponent<Image>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkImg;
            return toggle;
        }

        // ── 원자 프리팹 ──────────────────────────────────────────────────────────

        private static void CreateLabelPrefab()
        {
            var go = new GameObject("label", typeof(RectTransform));
            var comp = go.AddComponent<LabelComponent>();
            var text = AddTMPText(go);
            SetSerializedField(comp, "label", text);
            SavePrefab(go, "label");
        }

        private static void CreateButtonPrefab()
        {
            var go = new GameObject("button", typeof(RectTransform));
            var comp = go.AddComponent<ButtonComponent>();
            var btn = AddButton(go);
            SetSerializedField(comp, "button", btn);
            SavePrefab(go, "button");
        }

        private static void CreateSearchInputPrefab()
        {
            // search_input은 LabelModel을 받는 단순 레이블로 사용됨
            var go = new GameObject("search_input", typeof(RectTransform));
            var comp = go.AddComponent<LabelComponent>();
            var text = AddTMPText(go, "Placeholder");
            SetSerializedField(comp, "label", text);
            SavePrefab(go, "search_input");
        }

        private static void CreateIconPrefab()
        {
            var go = new GameObject("icon", typeof(RectTransform));
            var comp = go.AddComponent<IconComponent>();
            var img = AddImage(go);
            SetSerializedField(comp, "image", img);
            SavePrefab(go, "icon");
        }

        private static void CreateGaugePrefab()
        {
            var go = new GameObject("gauge", typeof(RectTransform));
            var comp = go.AddComponent<GaugeComponent>();
            var fill = AddFillImage(go);
            SetSerializedField(comp, "fill", fill);
            SavePrefab(go, "gauge");
        }

        // ── 팝업 프리팹 ──────────────────────────────────────────────────────────

        /// <summary>
        /// notice_popup — Example1(PopupModel+Patch), Example2(NoticeModel) 모두 대응.
        /// ViewComponent.views에 두 모델의 키를 모두 등록한다.
        /// </summary>
        private static void CreateNoticePrefab()
        {
            var root = new GameObject("notice_popup", typeof(RectTransform));
            var view = root.AddComponent<ViewComponent>();

            // Example2: NoticeModel 키
            var titleLabel = MakeChild<LabelComponent>(root, "TitleLabel", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var contentLabel = MakeChild<LabelComponent>(root, "ContentLabel", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var confirmBtn = MakeChild<ButtonComponent>(root, "ConfirmButton", c => SetSerializedField(c, "button", AddButton(c.gameObject)));
            var cancelBtn = MakeChild<ButtonComponent>(root, "CancelButton", c => SetSerializedField(c, "button", AddButton(c.gameObject)));

            // Example1 Patch 키: header.title / body.message / footer.confirm 은
            // ComponentBuilder가 ViewModel에 child로 주입하므로 ViewComponent.views에 등록
            SetViewComponentViews(view, new[]
            {
                ("title",         (SindyComponent)titleLabel),
                ("content",       contentLabel),
                ("confirm",       confirmBtn),
                ("cancel",        cancelBtn),
                ("header.title",  (SindyComponent)MakeChild<LabelComponent>(root, "HeaderTitle",
                                      c => SetSerializedField(c, "label", AddTMPText(c.gameObject)))),
                ("body.message",  (SindyComponent)MakeChild<LabelComponent>(root, "BodyMessage",
                                      c => SetSerializedField(c, "label", AddTMPText(c.gameObject)))),
                ("footer.confirm",(SindyComponent)MakeChild<ButtonComponent>(root, "FooterConfirm",
                                      c => SetSerializedField(c, "button", AddButton(c.gameObject)))),
            });

            SavePrefab(root, "notice_popup");
        }

        /// <summary>
        /// toast_popup — ToastModel: message(LabelModel), timer(TimerModel = PropModel&lt;string&gt;)
        /// </summary>
        private static void CreateToastPrefab()
        {
            var root = new GameObject("toast_popup", typeof(RectTransform));
            var view = root.AddComponent<ViewComponent>();

            var message = MakeChild<LabelComponent>(root, "Message", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var timer = MakeChild<LabelComponent>(root, "Timer", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));

            SetViewComponentViews(view, new[]
            {
                ("message", (SindyComponent)message),
                ("timer",   (SindyComponent)timer),
            });

            SavePrefab(root, "toast_popup");
        }

        /// <summary>
        /// character_profile_popup — CharacterProfileModel
        /// </summary>
        private static void CreateCharacterProfilePrefab()
        {
            var root = new GameObject("character_profile_popup", typeof(RectTransform));
            var view = root.AddComponent<ViewComponent>();

            var icon = MakeChild<IconComponent>(root, "Icon", c => SetSerializedField(c, "image", AddImage(c.gameObject)));
            var name = MakeChild<LabelComponent>(root, "Name", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var level = MakeChild<LabelComponent>(root, "Level", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var hp = MakeChild<GaugeComponent>(root, "Hp", c => SetSerializedField(c, "fill", AddFillImage(c.gameObject)));
            var mp = MakeChild<GaugeComponent>(root, "Mp", c => SetSerializedField(c, "fill", AddFillImage(c.gameObject)));
            var attack = MakeChild<LabelComponent>(root, "Attack", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var defense = MakeChild<LabelComponent>(root, "Defense", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var close = MakeChild<ButtonComponent>(root, "Close", c => SetSerializedField(c, "button", AddButton(c.gameObject)));

            SetViewComponentViews(view, new[]
            {
                ("header.icon",   (SindyComponent)icon),
                ("header.name",   name),
                ("header.level",  level),
                ("stats.hp",      hp),
                ("stats.mp",      mp),
                ("stats.attack",  attack),
                ("stats.defense", defense),
                ("close",         close),
            });

            SavePrefab(root, "character_profile_popup");
        }

        /// <summary>
        /// shop_item_slot — ShopItemModel (ListComponent의 아이템 슬롯)
        /// </summary>
        private static void CreateShopItemSlotPrefab()
        {
            var root = new GameObject("shop_item_slot", typeof(RectTransform));
            var view = root.AddComponent<ViewComponent>();

            var icon = MakeChild<IconComponent>(root, "Icon", c => SetSerializedField(c, "image", AddImage(c.gameObject)));
            var name = MakeChild<LabelComponent>(root, "Name", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var price = MakeChild<LabelComponent>(root, "Price", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var buy = MakeChild<ButtonComponent>(root, "Buy", c => SetSerializedField(c, "button", AddButton(c.gameObject)));

            SetViewComponentViews(view, new[]
            {
                ("icon",  (SindyComponent)icon),
                ("name",  name),
                ("price", price),
                ("buy",   buy),
            });

            SavePrefab(root, "shop_item_slot");
        }

        /// <summary>
        /// shop_popup — ShopModel: category(TabModel), items(ListViewModel), close(ButtonModel)
        /// </summary>
        private static void CreateShopPrefab()
        {
            // shop_item_slot 프리팹을 ListComponent.prefab으로 참조
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("shop_item_slot"));

            var root = new GameObject("shop_popup", typeof(RectTransform));
            var view = root.AddComponent<ViewComponent>();

            // category: TabComponent (탭 2개)
            var categoryGo = new GameObject("Category", typeof(RectTransform));
            categoryGo.transform.SetParent(root.transform, false);
            var category = categoryGo.AddComponent<TabComponent>();
            var tabs = new Toggle[]
            {
                AddToggle(new GameObject("Tab0", typeof(RectTransform)), "Weapon"),
                AddToggle(new GameObject("Tab1", typeof(RectTransform)), "Armor"),
                AddToggle(new GameObject("Tab2", typeof(RectTransform)), "Consumable"),
            };
            foreach (var tab in tabs)
                tab.transform.SetParent(categoryGo.transform, false);
            SetSerializedField(category, "tabs", tabs);

            // items: ListComponent
            var itemsGo = new GameObject("Items", typeof(RectTransform));
            itemsGo.transform.SetParent(root.transform, false);
            var items = itemsGo.AddComponent<ListComponent>();
            var itemContainer = new GameObject("Container", typeof(RectTransform));
            itemContainer.transform.SetParent(itemsGo.transform, false);
            if (slotPrefab != null)
                SetSerializedField(items, "prefab", slotPrefab.GetComponent<SindyComponent>());
            SetSerializedField(items, "container", itemContainer.transform);

            // close: ButtonComponent
            var close = MakeChild<ButtonComponent>(root, "Close", c => SetSerializedField(c, "button", AddButton(c.gameObject)));

            SetViewComponentViews(view, new[]
            {
                ("category", (SindyComponent)category),
                ("items",    (SindyComponent)items),
                ("close",    (SindyComponent)close),
            });

            SavePrefab(root, "shop_popup");
        }

        /// <summary>
        /// character_slot — CharacterSlotModel (ListComponent의 아이템 슬롯)
        /// </summary>
        private static void CreateCharacterSlotPrefab()
        {
            var root = new GameObject("character_slot", typeof(RectTransform));
            var view = root.AddComponent<ViewComponent>();

            var icon = MakeChild<IconComponent>(root, "Icon", c => SetSerializedField(c, "image", AddImage(c.gameObject)));
            var name = MakeChild<LabelComponent>(root, "Name", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var level = MakeChild<LabelComponent>(root, "Level", c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));
            var hp = MakeChild<GaugeComponent>(root, "Hp", c => SetSerializedField(c, "fill", AddFillImage(c.gameObject)));
            var select = MakeChild<ButtonComponent>(root, "Select", c => SetSerializedField(c, "button", AddButton(c.gameObject)));

            SetViewComponentViews(view, new[]
            {
                ("icon",   (SindyComponent)icon),
                ("name",   name),
                ("level",  level),
                ("hp",     hp),
                ("select", select),
            });

            SavePrefab(root, "character_slot");
        }

        /// <summary>
        /// character_inventory_popup — CharacterInventoryModel
        /// filter(InventoryFilterModel), list(ListViewModel), close, selected
        /// </summary>
        private static void CreateCharacterInventoryPrefab()
        {
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("character_slot"));

            var root = new GameObject("character_inventory_popup", typeof(RectTransform));
            var view = root.AddComponent<ViewComponent>();

            // filter: ViewComponent (InventoryFilterModel의 class/grade/search 서브 모델)
            var filterGo = new GameObject("Filter", typeof(RectTransform));
            filterGo.transform.SetParent(root.transform, false);
            var filter = filterGo.AddComponent<ViewComponent>();

            var classTab = MakeChildUnder<TabComponent>(filterGo, "ClassFilter", c =>
            {
                var t = AddToggle(new GameObject("Tab0", typeof(RectTransform)));
                t.transform.SetParent(c.gameObject.transform, false);
                SetSerializedField(c, "tabs", new[] { t });
            });

            var gradeTab = MakeChildUnder<TabComponent>(filterGo, "GradeFilter", c =>
            {
                var t = AddToggle(new GameObject("Tab0", typeof(RectTransform)));
                t.transform.SetParent(c.gameObject.transform, false);
                SetSerializedField(c, "tabs", new[] { t });
            });

            var searchText = MakeChildUnder<LabelComponent>(filterGo, "SearchText",
                c => SetSerializedField(c, "label", AddTMPText(c.gameObject)));

            SetViewComponentViews(filter, new[]
            {
                ("class",  (SindyComponent)classTab),
                ("grade",  (SindyComponent)gradeTab),
                ("search", (SindyComponent)searchText),
            });

            // list: ListComponent
            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(root.transform, false);
            var list = listGo.AddComponent<ListComponent>();
            var listContainer = new GameObject("Container", typeof(RectTransform));
            listContainer.transform.SetParent(listGo.transform, false);
            if (slotPrefab != null)
                SetSerializedField(list, "prefab", slotPrefab.GetComponent<SindyComponent>());
            SetSerializedField(list, "container", listContainer.transform);

            // close
            var close = MakeChild<ButtonComponent>(root, "Close", c => SetSerializedField(c, "button", AddButton(c.gameObject)));

            SetViewComponentViews(view, new[]
            {
                ("filter", (SindyComponent)filter),
                ("list",   (SindyComponent)list),
                ("close",  (SindyComponent)close),
            });

            SavePrefab(root, "character_inventory_popup");
        }

        // ── 유틸리티 ─────────────────────────────────────────────────────────────

        private static T MakeChild<T>(GameObject parent, string name, System.Action<T> setup = null)
            where T : SindyComponent
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var comp = go.AddComponent<T>();
            setup?.Invoke(comp);
            return comp;
        }

        private static T MakeChildUnder<T>(GameObject parent, string name, System.Action<T> setup = null)
            where T : SindyComponent
        {
            return MakeChild<T>(parent, name, setup);
        }

        private static void SetViewComponentViews(ViewComponent view, (string name, SindyComponent component)[] entries)
        {
            var so = new SerializedObject(view);
            var viewsProp = so.FindProperty("views");
            viewsProp.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var element = viewsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = entries[i].name;
                element.FindPropertyRelative("component").objectReferenceValue = entries[i].component;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedField<TComp, TVal>(TComp comp, string fieldName, TVal value)
            where TComp : Component
            where TVal : Object
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[PrefabCreator] Field '{fieldName}' not found on {typeof(TComp).Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedField<TComp>(TComp comp, string fieldName, Toggle[] toggles)
            where TComp : Component
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[PrefabCreator] Field '{fieldName}' not found on {typeof(TComp).Name}");
                return;
            }
            prop.arraySize = toggles.Length;
            for (int i = 0; i < toggles.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = toggles[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
