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

        // ── 색상 팔레트 ─────────────────────────────────────────────────────────

        static readonly Color ColBg         = new(0.15f, 0.15f, 0.18f, 1f);
        static readonly Color ColPanel      = new(0.20f, 0.20f, 0.24f, 1f);
        static readonly Color ColCard       = new(0.24f, 0.24f, 0.28f, 1f);
        static readonly Color ColBtnPrimary = new(0.28f, 0.55f, 0.95f, 1f);
        static readonly Color ColBtnMuted   = new(0.35f, 0.35f, 0.40f, 1f);
        static readonly Color ColAccent     = new(0.40f, 0.75f, 0.45f, 1f);
        static readonly Color ColHp         = new(0.85f, 0.30f, 0.30f, 1f);
        static readonly Color ColMp         = new(0.30f, 0.50f, 0.90f, 1f);
        static readonly Color ColGaugeBg    = new(0.12f, 0.12f, 0.14f, 1f);
        static readonly Color ColTextWhite  = new(0.93f, 0.93f, 0.93f, 1f);
        static readonly Color ColTextMuted  = new(0.60f, 0.60f, 0.65f, 1f);
        static readonly Color ColTextGold   = new(1f, 0.84f, 0.30f, 1f);
        static readonly Color ColTabOn      = new(0.28f, 0.55f, 0.95f, 1f);
        static readonly Color ColTabOff     = new(0.25f, 0.25f, 0.30f, 1f);
        static readonly Color ColIcon       = new(0.30f, 0.30f, 0.35f, 1f);
        static readonly Color ColToast      = new(0.18f, 0.18f, 0.22f, 0.95f);
        static readonly Color ColSearch     = new(0.18f, 0.18f, 0.22f, 1f);

        // ── 메뉴 ────────────────────────────────────────────────────────────────

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

        // ═══════════════════════════════════════════════════════════════════════
        // RectTransform 헬퍼
        // ═══════════════════════════════════════════════════════════════════════

        private static string PrefabPath(string name) => $"{PrefabOutputPath}/{name}.prefab";

        private static GameObject SavePrefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(name));
            Object.DestroyImmediate(go);
            Debug.Log($"  Created: {name}.prefab");
            return prefab;
        }

        private static RectTransform RT(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            return rt != null ? rt : go.AddComponent<RectTransform>();
        }

        private static void SetSize(GameObject go, float w, float h)
        {
            var rt = RT(go);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetStretch(GameObject go)
        {
            var rt = RT(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UI 요소 헬퍼
        // ═══════════════════════════════════════════════════════════════════════

        private static Image AddBg(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TMP_Text AddTMPText(GameObject parent, string childName, float fontSize, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            SetStretch(child);
            var tmp = child.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void ConfigureButton(ButtonComponent comp)
        {
            var go = comp.gameObject;
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            var hold = go.GetComponent<HoldButton>();
            if (hold != null) hold.targetGraphic = img;
        }

        private static ButtonComponent MakeButton(GameObject parent, string name,
            float w, float h, Color bgColor, string label, float fontSize, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            SetSize(go, w, h);
            AddBg(go, bgColor);

            var comp = go.AddComponent<ButtonComponent>();
            ConfigureButton(comp);

            var text = AddTMPText(go, "Label", fontSize, textColor, TextAlignmentOptions.Center);
            text.text = label;
            text.margin = new Vector4(4, 2, 4, 2);

            return comp;
        }

        private static (GaugeComponent comp, Image fill) MakeGauge(GameObject parent, string name,
            float w, float h, Color bgColor, Color fillColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            SetSize(go, w, h);
            AddBg(go, bgColor);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(go.transform, false);
            SetStretch(fillGo);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            var comp = go.AddComponent<GaugeComponent>();
            SetSerializedField(comp, "fill", fillImg);
            return (comp, fillImg);
        }

        private static LabelComponent MakeLabel(GameObject parent, string name,
            float fontSize, Color textColor, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var comp = go.AddComponent<LabelComponent>();
            var text = AddTMPText(go, "Text", fontSize, textColor, align);
            SetSerializedField(comp, "label", text);
            return comp;
        }

        private static IconComponent MakeIcon(GameObject parent, string name, float size, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            SetSize(go, size, size);
            var img = AddBg(go, tint);
            var comp = go.AddComponent<IconComponent>();
            SetSerializedField(comp, "image", img);
            return comp;
        }

        private static Toggle MakeToggle(GameObject parent, string name, float w, float h, string label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            SetSize(go, w, h);

            var bgImg = go.AddComponent<Image>();
            bgImg.color = ColTabOff;

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;

            // checkmark overlay
            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(go.transform, false);
            SetStretch(checkGo);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = ColTabOn;
            toggle.graphic = checkImg;

            // label
            var text = AddTMPText(go, "Label", 13, ColTextWhite, TextAlignmentOptions.Center);
            text.text = label;

            return toggle;
        }

        private static VerticalLayoutGroup AddVLayout(GameObject go, int padL, int padR, int padT, int padB,
            float spacing, TextAnchor align = TextAnchor.UpperCenter)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(padL, padR, padT, padB);
            vlg.spacing = spacing;
            vlg.childAlignment = align;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return vlg;
        }

        private static HorizontalLayoutGroup AddHLayout(GameObject go, int padL, int padR, int padT, int padB,
            float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(padL, padR, padT, padB);
            hlg.spacing = spacing;
            hlg.childAlignment = align;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            return hlg;
        }

        private static LayoutElement AddLE(GameObject go, float prefW = -1, float prefH = -1,
            float flexW = -1, float flexH = -1)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            return le;
        }

        private static ContentSizeFitter AddFitter(GameObject go,
            ContentSizeFitter.FitMode horizontal = ContentSizeFitter.FitMode.Unconstrained,
            ContentSizeFitter.FitMode vertical = ContentSizeFitter.FitMode.PreferredSize)
        {
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = horizontal;
            csf.verticalFit = vertical;
            return csf;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 원자 프리팹
        // ═══════════════════════════════════════════════════════════════════════

        private static void CreateLabelPrefab()
        {
            var go = new GameObject("label", typeof(RectTransform));
            SetSize(go, 200, 28);
            var comp = go.AddComponent<LabelComponent>();
            var text = AddTMPText(go, "Text", 16, ColTextWhite);
            SetSerializedField(comp, "label", text);
            AddLE(go, prefH: 28);
            SavePrefab(go, "label");
        }

        private static void CreateButtonPrefab()
        {
            var go = new GameObject("button", typeof(RectTransform));
            SetSize(go, 120, 36);
            AddBg(go, ColBtnPrimary);
            var comp = go.AddComponent<ButtonComponent>();
            ConfigureButton(comp);
            AddTMPText(go, "Label", 15, ColTextWhite, TextAlignmentOptions.Center).text = "Button";
            AddLE(go, prefH: 36);
            SavePrefab(go, "button");
        }

        private static void CreateSearchInputPrefab()
        {
            var go = new GameObject("search_input", typeof(RectTransform));
            SetSize(go, 240, 32);
            AddBg(go, ColSearch);
            var comp = go.AddComponent<LabelComponent>();
            var text = AddTMPText(go, "Placeholder", 14, ColTextMuted);
            text.text = "Search...";
            text.fontStyle = FontStyles.Italic;
            text.margin = new Vector4(8, 0, 8, 0);
            SetSerializedField(comp, "label", text);
            AddLE(go, prefH: 32);
            SavePrefab(go, "search_input");
        }

        private static void CreateIconPrefab()
        {
            var go = new GameObject("icon", typeof(RectTransform));
            SetSize(go, 48, 48);
            var img = AddBg(go, ColIcon);
            var comp = go.AddComponent<IconComponent>();
            SetSerializedField(comp, "image", img);
            AddLE(go, prefW: 48, prefH: 48);
            SavePrefab(go, "icon");
        }

        private static void CreateGaugePrefab()
        {
            var go = new GameObject("gauge", typeof(RectTransform));
            SetSize(go, 200, 16);
            AddBg(go, ColGaugeBg);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(go.transform, false);
            SetStretch(fillGo);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = ColAccent;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            var comp = go.AddComponent<GaugeComponent>();
            SetSerializedField(comp, "fill", fillImg);
            AddLE(go, prefH: 16);
            SavePrefab(go, "gauge");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 팝업 프리팹
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// notice_popup — Example1(PopupModel+Patch), Example2(NoticeModel) 모두 대응.
        /// </summary>
        private static void CreateNoticePrefab()
        {
            // dim background (stretch to parent)
            var root = new GameObject("notice_popup", typeof(RectTransform));
            SetSize(root, 400, 260);
            AddBg(root, ColBg);
            AddVLayout(root, 20, 20, 20, 20, 12);
            AddFitter(root);

            var view = root.AddComponent<ViewComponent>();

            // ── title ──
            var titleLabel = MakeLabel(root, "TitleLabel", 20, ColTextWhite, TextAlignmentOptions.Center);
            AddLE(titleLabel.gameObject, prefH: 30);

            // ── content ──
            var contentLabel = MakeLabel(root, "ContentLabel", 15, ColTextMuted, TextAlignmentOptions.Center);
            AddLE(contentLabel.gameObject, prefH: 40, flexH: 1);

            // ── buttons row ──
            var btnRow = new GameObject("ButtonRow", typeof(RectTransform));
            btnRow.transform.SetParent(root.transform, false);
            AddHLayout(btnRow, 0, 0, 0, 0, 12, TextAnchor.MiddleCenter);
            AddLE(btnRow, prefH: 40);
            AddFitter(btnRow, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained);

            var confirmBtn = MakeButton(btnRow, "ConfirmButton", 130, 36, ColBtnPrimary, "Confirm", 15, ColTextWhite);
            var cancelBtn = MakeButton(btnRow, "CancelButton", 130, 36, ColBtnMuted, "Cancel", 15, ColTextWhite);

            // ── Example1 Patch용 중복 뷰 ──
            var headerTitle = MakeLabel(root, "HeaderTitle", 20, ColTextWhite, TextAlignmentOptions.Center);
            AddLE(headerTitle.gameObject, prefH: 30);
            headerTitle.gameObject.SetActive(false);

            var bodyMessage = MakeLabel(root, "BodyMessage", 15, ColTextMuted, TextAlignmentOptions.Center);
            AddLE(bodyMessage.gameObject, prefH: 40);
            bodyMessage.gameObject.SetActive(false);

            var footerConfirm = MakeButton(root, "FooterConfirm", 130, 36, ColBtnPrimary, "OK", 15, ColTextWhite);
            footerConfirm.gameObject.SetActive(false);

            SetViewComponentViews(view, new (string, SindyComponent)[]
            {
                ("title",          titleLabel),
                ("content",        contentLabel),
                ("confirm",        confirmBtn),
                ("cancel",         cancelBtn),
                ("header.title",   headerTitle),
                ("body.message",   bodyMessage),
                ("footer.confirm", footerConfirm),
            });

            SavePrefab(root, "notice_popup");
        }

        /// <summary>
        /// toast_popup — ToastModel: message + timer
        /// </summary>
        private static void CreateToastPrefab()
        {
            var root = new GameObject("toast_popup", typeof(RectTransform));
            SetSize(root, 300, 48);
            AddBg(root, ColToast);
            AddHLayout(root, 16, 16, 0, 0, 8, TextAnchor.MiddleCenter);

            var view = root.AddComponent<ViewComponent>();

            var message = MakeLabel(root, "Message", 14, ColTextWhite, TextAlignmentOptions.Center);
            AddLE(message.gameObject, flexW: 1, prefH: 48);

            var timer = MakeLabel(root, "Timer", 12, ColTextMuted, TextAlignmentOptions.Center);
            AddLE(timer.gameObject, prefW: 50, prefH: 48);

            SetViewComponentViews(view, new (string, SindyComponent)[]
            {
                ("message", message),
                ("timer",   timer),
            });

            SavePrefab(root, "toast_popup");
        }

        /// <summary>
        /// character_profile_popup — CharacterProfileModel
        /// </summary>
        private static void CreateCharacterProfilePrefab()
        {
            var root = new GameObject("character_profile_popup", typeof(RectTransform));
            SetSize(root, 360, 420);
            AddBg(root, ColBg);
            AddVLayout(root, 20, 20, 20, 20, 12);

            var view = root.AddComponent<ViewComponent>();

            // ── header row (icon + name/level) ──
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(root.transform, false);
            AddHLayout(header, 0, 0, 0, 0, 12, TextAnchor.MiddleLeft);
            AddLE(header, prefH: 72);

            var icon = MakeIcon(header, "Icon", 64, ColIcon);

            var nameCol = new GameObject("NameCol", typeof(RectTransform));
            nameCol.transform.SetParent(header.transform, false);
            SetSize(nameCol, 200, 64);
            AddVLayout(nameCol, 0, 0, 4, 4, 4, TextAnchor.MiddleLeft);

            var nameLabel = MakeLabel(nameCol, "Name", 20, ColTextWhite);
            AddLE(nameLabel.gameObject, prefH: 28);
            var level = MakeLabel(nameCol, "Level", 14, ColTextGold);
            AddLE(level.gameObject, prefH: 20);

            // ── stats section ──
            var statsSection = new GameObject("Stats", typeof(RectTransform));
            statsSection.transform.SetParent(root.transform, false);
            AddVLayout(statsSection, 0, 0, 0, 0, 8);
            AddLE(statsSection, prefH: 160, flexH: 1);
            AddFitter(statsSection);

            // HP
            var hpRow = new GameObject("HpRow", typeof(RectTransform));
            hpRow.transform.SetParent(statsSection.transform, false);
            AddVLayout(hpRow, 0, 0, 0, 0, 2);
            AddLE(hpRow, prefH: 24);
            var hpLabel = MakeLabel(hpRow, "HpLabel", 12, ColTextMuted);
            hpLabel.gameObject.GetComponentInChildren<TMP_Text>().text = "HP";
            AddLE(hpLabel.gameObject, prefH: 14);
            var (hpGauge, _) = MakeGauge(hpRow, "Hp", 0, 10, ColGaugeBg, ColHp);
            AddLE(hpGauge.gameObject, prefH: 10, flexW: 1);

            // MP
            var mpRow = new GameObject("MpRow", typeof(RectTransform));
            mpRow.transform.SetParent(statsSection.transform, false);
            AddVLayout(mpRow, 0, 0, 0, 0, 2);
            AddLE(mpRow, prefH: 24);
            var mpLabel = MakeLabel(mpRow, "MpLabel", 12, ColTextMuted);
            mpLabel.gameObject.GetComponentInChildren<TMP_Text>().text = "MP";
            AddLE(mpLabel.gameObject, prefH: 14);
            var (mpGauge, _) = MakeGauge(mpRow, "Mp", 0, 10, ColGaugeBg, ColMp);
            AddLE(mpGauge.gameObject, prefH: 10, flexW: 1);

            // ATK / DEF row
            var atkDefRow = new GameObject("AtkDefRow", typeof(RectTransform));
            atkDefRow.transform.SetParent(statsSection.transform, false);
            AddHLayout(atkDefRow, 0, 0, 0, 0, 16, TextAnchor.MiddleLeft);
            AddLE(atkDefRow, prefH: 28);

            var attack = MakeLabel(atkDefRow, "Attack", 15, ColTextWhite);
            AddLE(attack.gameObject, prefW: 140, prefH: 28);
            var defense = MakeLabel(atkDefRow, "Defense", 15, ColTextWhite);
            AddLE(defense.gameObject, prefW: 140, prefH: 28);

            // ── close button ──
            var close = MakeButton(root, "Close", 0, 36, ColBtnMuted, "Close", 15, ColTextWhite);
            AddLE(close.gameObject, prefH: 36, flexW: 1);

            SetViewComponentViews(view, new (string, SindyComponent)[]
            {
                ("header.icon",   icon),
                ("header.name",   nameLabel),
                ("header.level",  level),
                ("stats.hp",      hpGauge),
                ("stats.mp",      mpGauge),
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
            SetSize(root, 0, 64);
            AddBg(root, ColCard);
            AddHLayout(root, 8, 8, 8, 8, 10, TextAnchor.MiddleLeft);

            var view = root.AddComponent<ViewComponent>();

            var icon = MakeIcon(root, "Icon", 48, ColIcon);
            AddLE(icon.gameObject, prefW: 48, prefH: 48);

            var infoCol = new GameObject("Info", typeof(RectTransform));
            infoCol.transform.SetParent(root.transform, false);
            SetSize(infoCol, 120, 48);
            AddVLayout(infoCol, 0, 0, 2, 2, 2, TextAnchor.MiddleLeft);
            AddLE(infoCol, flexW: 1, prefH: 48);

            var nameLabel = MakeLabel(infoCol, "Name", 15, ColTextWhite);
            AddLE(nameLabel.gameObject, prefH: 22);
            var price = MakeLabel(infoCol, "Price", 13, ColTextGold);
            AddLE(price.gameObject, prefH: 18);

            var buy = MakeButton(root, "Buy", 70, 32, ColBtnPrimary, "Buy", 13, ColTextWhite);
            AddLE(buy.gameObject, prefW: 70, prefH: 32);

            SetViewComponentViews(view, new (string, SindyComponent)[]
            {
                ("icon",  icon),
                ("name",  nameLabel),
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
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("shop_item_slot"));

            var root = new GameObject("shop_popup", typeof(RectTransform));
            SetSize(root, 400, 480);
            AddBg(root, ColBg);
            AddVLayout(root, 16, 16, 16, 16, 10);

            var view = root.AddComponent<ViewComponent>();

            // ── title ──
            var titleText = AddTMPText(root, "Title", 20, ColTextWhite, TextAlignmentOptions.Center);
            titleText.text = "Shop";
            AddLE(titleText.gameObject, prefH: 30);

            // ── category tabs ──
            var categoryGo = new GameObject("Category", typeof(RectTransform));
            categoryGo.transform.SetParent(root.transform, false);
            var category = categoryGo.AddComponent<TabComponent>();
            AddHLayout(categoryGo, 0, 0, 0, 0, 4, TextAnchor.MiddleCenter);
            AddLE(categoryGo, prefH: 36);

            var tabs = new[]
            {
                MakeToggle(categoryGo, "Tab0", 100, 32, "Weapon"),
                MakeToggle(categoryGo, "Tab1", 100, 32, "Armor"),
                MakeToggle(categoryGo, "Tab2", 100, 32, "Consum."),
            };
            SetSerializedField(category, "tabs", tabs);

            // ── items list ──
            var itemsGo = new GameObject("Items", typeof(RectTransform));
            itemsGo.transform.SetParent(root.transform, false);
            AddBg(itemsGo, ColPanel);
            var items = itemsGo.AddComponent<ListComponent>();
            AddLE(itemsGo, flexH: 1, prefH: 260);

            var itemContainer = new GameObject("Container", typeof(RectTransform));
            itemContainer.transform.SetParent(itemsGo.transform, false);
            SetStretch(itemContainer);
            AddVLayout(itemContainer, 4, 4, 4, 4, 4);

            if (slotPrefab != null)
                SetSerializedField(items, "prefab", slotPrefab.GetComponent<SindyComponent>());
            SetSerializedField(items, "container", itemContainer.transform);

            // ── close ──
            var close = MakeButton(root, "Close", 0, 36, ColBtnMuted, "Close", 15, ColTextWhite);
            AddLE(close.gameObject, prefH: 36);

            SetViewComponentViews(view, new (string, SindyComponent)[]
            {
                ("category", category),
                ("items",    items),
                ("close",    close),
            });

            SavePrefab(root, "shop_popup");
        }

        /// <summary>
        /// character_slot — CharacterSlotModel (ListComponent의 아이템 슬롯)
        /// </summary>
        private static void CreateCharacterSlotPrefab()
        {
            var root = new GameObject("character_slot", typeof(RectTransform));
            SetSize(root, 0, 72);
            AddBg(root, ColCard);
            AddHLayout(root, 8, 8, 8, 8, 10, TextAnchor.MiddleLeft);

            var view = root.AddComponent<ViewComponent>();

            var icon = MakeIcon(root, "Icon", 56, ColIcon);
            AddLE(icon.gameObject, prefW: 56, prefH: 56);

            var infoCol = new GameObject("Info", typeof(RectTransform));
            infoCol.transform.SetParent(root.transform, false);
            SetSize(infoCol, 150, 56);
            AddVLayout(infoCol, 0, 0, 2, 2, 2, TextAnchor.MiddleLeft);
            AddLE(infoCol, flexW: 1, prefH: 56);

            var nameLabel = MakeLabel(infoCol, "Name", 15, ColTextWhite);
            AddLE(nameLabel.gameObject, prefH: 20);
            var level = MakeLabel(infoCol, "Level", 12, ColTextGold);
            AddLE(level.gameObject, prefH: 16);
            var (hp, _) = MakeGauge(infoCol, "Hp", 0, 8, ColGaugeBg, ColHp);
            AddLE(hp.gameObject, prefH: 8, flexW: 1);

            var select = MakeButton(root, "Select", 70, 32, ColBtnPrimary, "Select", 13, ColTextWhite);
            AddLE(select.gameObject, prefW: 70, prefH: 32);

            SetViewComponentViews(view, new (string, SindyComponent)[]
            {
                ("icon",   icon),
                ("name",   nameLabel),
                ("level",  level),
                ("hp",     hp),
                ("select", select),
            });

            SavePrefab(root, "character_slot");
        }

        /// <summary>
        /// character_inventory_popup — CharacterInventoryModel
        /// </summary>
        private static void CreateCharacterInventoryPrefab()
        {
            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("character_slot"));

            var root = new GameObject("character_inventory_popup", typeof(RectTransform));
            SetSize(root, 420, 520);
            AddBg(root, ColBg);
            AddVLayout(root, 16, 16, 16, 16, 10);

            var view = root.AddComponent<ViewComponent>();

            // ── title ──
            var titleText = AddTMPText(root, "Title", 20, ColTextWhite, TextAlignmentOptions.Center);
            titleText.text = "Inventory";
            AddLE(titleText.gameObject, prefH: 30);

            // ── filter section ──
            var filterGo = new GameObject("Filter", typeof(RectTransform));
            filterGo.transform.SetParent(root.transform, false);
            AddVLayout(filterGo, 0, 0, 0, 0, 6);
            AddLE(filterGo, prefH: 80);
            AddFitter(filterGo);
            var filter = filterGo.AddComponent<ViewComponent>();

            // class tabs
            var classRow = new GameObject("ClassFilter", typeof(RectTransform));
            classRow.transform.SetParent(filterGo.transform, false);
            AddHLayout(classRow, 0, 0, 0, 0, 4, TextAnchor.MiddleLeft);
            AddLE(classRow, prefH: 30);
            var classTab = classRow.AddComponent<TabComponent>();
            var classTabs = new[]
            {
                MakeToggle(classRow, "All",     60, 28, "All"),
                MakeToggle(classRow, "Warrior", 70, 28, "Warrior"),
                MakeToggle(classRow, "Mage",    60, 28, "Mage"),
                MakeToggle(classRow, "Archer",  60, 28, "Archer"),
            };
            SetSerializedField(classTab, "tabs", classTabs);

            // grade tabs
            var gradeRow = new GameObject("GradeFilter", typeof(RectTransform));
            gradeRow.transform.SetParent(filterGo.transform, false);
            AddHLayout(gradeRow, 0, 0, 0, 0, 4, TextAnchor.MiddleLeft);
            AddLE(gradeRow, prefH: 30);
            var gradeTab = gradeRow.AddComponent<TabComponent>();
            var gradeTabs = new[]
            {
                MakeToggle(gradeRow, "All",    50, 28, "All"),
                MakeToggle(gradeRow, "Common", 65, 28, "Common"),
                MakeToggle(gradeRow, "Rare",   50, 28, "Rare"),
                MakeToggle(gradeRow, "Epic",   50, 28, "Epic"),
                MakeToggle(gradeRow, "Legend", 65, 28, "Legend"),
            };
            SetSerializedField(gradeTab, "tabs", gradeTabs);

            // search (placeholder label)
            var searchText = MakeLabel(filterGo, "SearchText", 14, ColTextMuted);
            searchText.gameObject.GetComponentInChildren<TMP_Text>().text = "";
            AddLE(searchText.gameObject, prefH: 0);
            searchText.gameObject.SetActive(false); // hidden, Patch로 search_input 주입

            SetViewComponentViews(filter, new (string, SindyComponent)[]
            {
                ("class",  classTab),
                ("grade",  gradeTab),
                ("search", searchText),
            });

            // ── list ──
            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(root.transform, false);
            AddBg(listGo, ColPanel);
            AddLE(listGo, flexH: 1, prefH: 280);
            var list = listGo.AddComponent<ListComponent>();

            var listContainer = new GameObject("Container", typeof(RectTransform));
            listContainer.transform.SetParent(listGo.transform, false);
            SetStretch(listContainer);
            AddVLayout(listContainer, 4, 4, 4, 4, 4);

            if (slotPrefab != null)
                SetSerializedField(list, "prefab", slotPrefab.GetComponent<SindyComponent>());
            SetSerializedField(list, "container", listContainer.transform);

            // ── close ──
            var close = MakeButton(root, "Close", 0, 36, ColBtnMuted, "Close", 15, ColTextWhite);
            AddLE(close.gameObject, prefH: 36);

            SetViewComponentViews(view, new (string, SindyComponent)[]
            {
                ("filter", filter),
                ("list",   list),
                ("close",  close),
            });

            SavePrefab(root, "character_inventory_popup");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 유틸리티
        // ═══════════════════════════════════════════════════════════════════════

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
