// Lügen — TableView.cs
// A proper Canvas-based UI replacing DebugGameUI's IMGUI. Builds the entire
// scene programmatically on Awake, so you don't have to wire anything in
// the Unity Editor. Just attach this MonoBehaviour to a GameObject and
// press Play.
//
// Layout (rough):
//   ┌──────────────────────────────────────────────────────────┐
//   │           HUD  (Floor / ♥ / 💰 / Target / Mod)            │
//   ├──────────────────────────────────────────────────────────┤
//   │  Bot 2     Bot 1 (boss-seat tells)     Bot 3              │
//   │                                                          │
//   │              Pile      Draw                              │
//   │        (played pile cards)  (deck back)                   │
//   │                                                          │
//   │     ╔══════════ HAND ═══════════╗                         │
//   │     ║  [10]  [Q]  [K]  [A]  [J]  ║   [Log panel right]    │
//   │     ╚════════════════════════════╝                       │
//   │   [Play 2] [Clear]   |  [Call LIAR] [Let it go]           │
//   └──────────────────────────────────────────────────────────┘
//
// Optional sprite hooks (see [Header] fields below). Drag sprites into
// the Inspector to replace the placeholder colored rectangles. If a slot
// is empty, the placeholder is used.

#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using Lugen.Affixes;
using Lugen.AI;
using Lugen.Cards;
using Lugen.Characters;
using Lugen.Consumables;
using Lugen.Core;
using Lugen.Floor;
using Lugen.Jokers;
using Lugen.Relics;
using Lugen.Round;
using Lugen.Run;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Lugen.UnityHooks
{
    public class TableView : MonoBehaviour
    {
        // ---------------- Sprite hooks (optional) ----------------
        [Header("Optional sprites — leave empty for placeholder colors")]
        public Sprite tableFeltSprite;
        public Sprite cardBackSprite;
        public Sprite cardFrontSprite;
        public Sprite buttonSprite;

        [Header("Colors")]
        public Color tableTopColor = new Color(0.05f, 0.30f, 0.18f);
        public Color tableBottomColor = new Color(0.02f, 0.10f, 0.07f);
        public Color panelColor = new Color(0.10f, 0.13f, 0.18f, 0.85f);
        public Color brandYellow = new Color(0.92f, 0.70f, 0.04f);
        public Color liarRed = new Color(0.88f, 0.18f, 0.30f);

        // ---------------- Engine state ----------------
        private enum Phase { Intro, CharSelect, Round, RoundEnd, FloorEnd, BossRelic, Fork, Shop, Reward, Event, Cleanse, Treasure, RunEnd }
        private Phase phase = Phase.Intro;
        private RunManager run;
        private RoundController controller;
        private readonly HashSet<string> selectedCardIds = new HashSet<string>();

        private float botTurnDelay = 0.6f;
        private float botActionTimer;
        private float challengeWindowSec = 5f;
        private float challengeTimer;
        private bool challengeWindowOpen;
        private int currentChallengerSeat = -1;
        private float botChallengeDecideTimer;

        private List<string> uiLog = new List<string>();
        private ForkOptions currentFork;
        private Lugen.Shop.Shop currentShop;
        private List<string> rewardJokerOffers, bossRelicOffers, treasureRelicOffers;
        private string lastEventTitle, lastEventText, lastEventResult;
        private int? lastWinnerIdx;
        private int lastRoundGoldGained;

        // ---------------- UI references (built in Awake) ----------------
        private Canvas canvas;
        private RectTransform tableRoot;
        private Text hudText;
        private RectTransform handArea;
        private RectTransform pileArea;
        private RectTransform drawPileArea;
        private RectTransform[] opponentSlots = new RectTransform[3];
        private Text[] opponentTexts = new Text[3];
        private RectTransform[] opponentCardRows = new RectTransform[3];
        private Text logText;
        private ScrollRect logScroll;
        private RectTransform actionsBar;
        private Text actionPrompt;
        private Button playButton, clearButton, callLiarButton, letItGoButton;
        private Text playButtonText;
        private GameObject challengeBar;
        private Text challengeText;

        // Modal panels
        private GameObject introPanel, charSelectPanel, roundEndPanel, floorEndPanel;
        private GameObject bossRelicPanel, forkPanel, shopPanel, rewardPanel, eventPanel;
        private GameObject cleansePanel, treasurePanel, runEndPanel;
        private Text modalTitle, modalBody;
        private RectTransform modalButtonRow;
        private GameObject genericModal;

        // ---------------- Lifecycle ----------------

        private void Awake()
        {
            BuildUI();
            ShowPhase(Phase.Intro);
        }

        private void Update()
        {
            if (phase != Phase.Round) return;
            if (controller?.State == null) return;
            var s = controller.State;
            if (s.gameOver) { GoToRoundEnd(); return; }

            if (!challengeWindowOpen && s.currentTurn != 0)
            {
                botActionTimer -= Time.deltaTime;
                if (botActionTimer <= 0f)
                {
                    PumpBotTurn();
                    botActionTimer = botTurnDelay;
                }
            }
            if (challengeWindowOpen)
            {
                if (currentChallengerSeat == 0)
                {
                    challengeTimer -= Time.deltaTime;
                    UpdateChallengeText();
                    if (challengeTimer <= 0f) CloseChallenge();
                }
                else
                {
                    botChallengeDecideTimer -= Time.deltaTime;
                    UpdateChallengeText();
                    if (botChallengeDecideTimer <= 0f) BotChallengeDecide();
                }
            }

            SyncRoundUI();
        }

        // ---------------- BuildUI ----------------

        private void BuildUI()
        {
            EnsureEventSystem();

            // Canvas
            var canvasGO = new GameObject("Lugen Canvas");
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            tableRoot = (RectTransform)canvas.transform;

            // ---- Felt background ----
            var bg = MakeImage("TableFelt", tableRoot, panelColor);
            bg.transform.SetAsFirstSibling();
            var bgRT = (RectTransform)bg.transform;
            Stretch(bgRT);
            // Use sprite if provided, else draw a vertical gradient via two stacked images.
            var bgImg = bg.GetComponent<Image>();
            if (tableFeltSprite != null) { bgImg.sprite = tableFeltSprite; bgImg.color = Color.white; }
            else
            {
                bgImg.color = tableBottomColor;
                var top = MakeImage("FeltGradient", tableRoot, tableTopColor);
                top.transform.SetSiblingIndex(1);
                var topRT = (RectTransform)top.transform;
                topRT.anchorMin = new Vector2(0, 0.4f);
                topRT.anchorMax = new Vector2(1, 1);
                topRT.offsetMin = Vector2.zero; topRT.offsetMax = Vector2.zero;
            }

            // ---- HUD bar (top) ----
            var hud = MakePanel("HUD", tableRoot);
            var hudRT = (RectTransform)hud.transform;
            hudRT.anchorMin = new Vector2(0, 1);
            hudRT.anchorMax = new Vector2(1, 1);
            hudRT.pivot = new Vector2(0.5f, 1);
            hudRT.sizeDelta = new Vector2(0, 56);
            hudRT.anchoredPosition = new Vector2(0, 0);
            hudText = MakeText(hud.transform, "Lügen", 22, FontStyle.Bold);
            hudText.alignment = TextAnchor.MiddleCenter;
            var hudTextRT = (RectTransform)hudText.transform;
            Stretch(hudTextRT);

            // ---- Opponent slots (top half) ----
            // Three slots: left, top-center, right.
            float opTop = -80f;
            float opPadX = 60f;
            opponentSlots[0] = MakeOpponentSlot(0, "BotLeft", new Vector2(0, 1), new Vector2(opPadX, opTop), TextAnchor.UpperLeft).rect;
            opponentTexts[0] = ((RectTransform)opponentSlots[0].parent).GetComponentInChildren<Text>();
            opponentCardRows[0] = opponentSlots[0];
            opponentSlots[1] = MakeOpponentSlot(1, "BotCenter", new Vector2(0.5f, 1), new Vector2(0, opTop), TextAnchor.UpperCenter).rect;
            opponentTexts[1] = ((RectTransform)opponentSlots[1].parent).GetComponentInChildren<Text>();
            opponentCardRows[1] = opponentSlots[1];
            opponentSlots[2] = MakeOpponentSlot(2, "BotRight", new Vector2(1, 1), new Vector2(-opPadX, opTop), TextAnchor.UpperRight).rect;
            opponentTexts[2] = ((RectTransform)opponentSlots[2].parent).GetComponentInChildren<Text>();
            opponentCardRows[2] = opponentSlots[2];

            // ---- Pile + draw pile (center) ----
            var pileLabel = MakeText(tableRoot, "Played pile", 14);
            pileLabel.alignment = TextAnchor.MiddleCenter;
            var pileLabelRT = (RectTransform)pileLabel.transform;
            pileLabelRT.anchorMin = pileLabelRT.anchorMax = new Vector2(0.45f, 0.55f);
            pileLabelRT.sizeDelta = new Vector2(160, 20);
            pileLabelRT.anchoredPosition = new Vector2(0, 70);

            var pile = new GameObject("Pile");
            pile.transform.SetParent(tableRoot, false);
            pileArea = pile.AddComponent<RectTransform>();
            pileArea.anchorMin = pileArea.anchorMax = new Vector2(0.45f, 0.55f);
            pileArea.sizeDelta = new Vector2(140, 110);

            var drawLabel = MakeText(tableRoot, "Draw", 14);
            drawLabel.alignment = TextAnchor.MiddleCenter;
            var drawLabelRT = (RectTransform)drawLabel.transform;
            drawLabelRT.anchorMin = drawLabelRT.anchorMax = new Vector2(0.55f, 0.55f);
            drawLabelRT.sizeDelta = new Vector2(120, 20);
            drawLabelRT.anchoredPosition = new Vector2(0, 70);

            var draw = new GameObject("DrawPile");
            draw.transform.SetParent(tableRoot, false);
            drawPileArea = draw.AddComponent<RectTransform>();
            drawPileArea.anchorMin = drawPileArea.anchorMax = new Vector2(0.55f, 0.55f);
            drawPileArea.sizeDelta = new Vector2(80, 110);

            // ---- Hand (bottom) ----
            var handHolder = new GameObject("HandHolder");
            handHolder.transform.SetParent(tableRoot, false);
            var hhRT = handHolder.AddComponent<RectTransform>();
            hhRT.anchorMin = new Vector2(0.5f, 0);
            hhRT.anchorMax = new Vector2(0.5f, 0);
            hhRT.pivot = new Vector2(0.5f, 0);
            hhRT.sizeDelta = new Vector2(900, 130);
            hhRT.anchoredPosition = new Vector2(0, 80);
            handArea = hhRT;
            var hLayout = handHolder.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // ---- Action bar (just above hand) ----
            actionsBar = MakePanel("Actions", tableRoot).GetComponent<RectTransform>();
            actionsBar.anchorMin = new Vector2(0.5f, 0); actionsBar.anchorMax = new Vector2(0.5f, 0);
            actionsBar.pivot = new Vector2(0.5f, 0);
            actionsBar.sizeDelta = new Vector2(900, 70);
            actionsBar.anchoredPosition = new Vector2(0, 8);

            actionPrompt = MakeText(actionsBar.transform, "", 16, FontStyle.Italic);
            actionPrompt.alignment = TextAnchor.MiddleCenter;
            var apRT = (RectTransform)actionPrompt.transform;
            apRT.anchorMin = new Vector2(0, 1); apRT.anchorMax = new Vector2(1, 1); apRT.pivot = new Vector2(0.5f, 1);
            apRT.sizeDelta = new Vector2(0, 22);
            apRT.anchoredPosition = new Vector2(0, -2);

            playButton = MakeButton(actionsBar, "Play", brandYellow, Color.black, () => DoHumanPlay());
            playButtonText = playButton.GetComponentInChildren<Text>();
            var pbRT = (RectTransform)playButton.transform;
            pbRT.anchorMin = pbRT.anchorMax = new Vector2(0.3f, 0); pbRT.pivot = new Vector2(0.5f, 0);
            pbRT.sizeDelta = new Vector2(180, 36);
            pbRT.anchoredPosition = new Vector2(0, 6);

            clearButton = MakeButton(actionsBar, "Clear", new Color(0.4f, 0.4f, 0.45f), Color.white, () => selectedCardIds.Clear());
            var cbRT = (RectTransform)clearButton.transform;
            cbRT.anchorMin = cbRT.anchorMax = new Vector2(0.5f, 0); cbRT.pivot = new Vector2(0.5f, 0);
            cbRT.sizeDelta = new Vector2(120, 36);
            cbRT.anchoredPosition = new Vector2(0, 6);

            callLiarButton = MakeButton(actionsBar, "Call LIAR", liarRed, Color.white, () => DoCallLiar());
            var clRT = (RectTransform)callLiarButton.transform;
            clRT.anchorMin = clRT.anchorMax = new Vector2(0.7f, 0); clRT.pivot = new Vector2(0.5f, 0);
            clRT.sizeDelta = new Vector2(140, 36);
            clRT.anchoredPosition = new Vector2(0, 6);
            callLiarButton.gameObject.SetActive(false);

            letItGoButton = MakeButton(actionsBar, "Let it go", new Color(0.4f, 0.4f, 0.45f), Color.white, () => CloseChallenge());
            var lgRT = (RectTransform)letItGoButton.transform;
            lgRT.anchorMin = lgRT.anchorMax = new Vector2(0.85f, 0); lgRT.pivot = new Vector2(0.5f, 0);
            lgRT.sizeDelta = new Vector2(140, 36);
            lgRT.anchoredPosition = new Vector2(0, 6);
            letItGoButton.gameObject.SetActive(false);

            // ---- Challenge banner (over the pile area) ----
            challengeBar = MakePanel("ChallengeBar", tableRoot);
            var cbarRT = (RectTransform)challengeBar.transform;
            cbarRT.anchorMin = new Vector2(0.5f, 0.5f); cbarRT.anchorMax = new Vector2(0.5f, 0.5f);
            cbarRT.pivot = new Vector2(0.5f, 0.5f);
            cbarRT.sizeDelta = new Vector2(620, 40);
            cbarRT.anchoredPosition = new Vector2(0, -90);
            challengeBar.GetComponent<Image>().color = new Color(0.92f, 0.70f, 0.04f, 0.95f);
            challengeText = MakeText(challengeBar.transform, "Challenge…", 16, FontStyle.Bold);
            challengeText.alignment = TextAnchor.MiddleCenter;
            challengeText.color = Color.black;
            Stretch((RectTransform)challengeText.transform);
            challengeBar.SetActive(false);

            // ---- Log panel (right side) ----
            var log = MakePanel("LogPanel", tableRoot);
            var logRT = (RectTransform)log.transform;
            logRT.anchorMin = new Vector2(1, 0); logRT.anchorMax = new Vector2(1, 1);
            logRT.pivot = new Vector2(1, 0.5f);
            logRT.sizeDelta = new Vector2(360, -100);
            logRT.anchoredPosition = new Vector2(-10, 0);
            // Title
            var logTitle = MakeText(log.transform, "Log", 16, FontStyle.Bold);
            var ltRT = (RectTransform)logTitle.transform;
            ltRT.anchorMin = new Vector2(0, 1); ltRT.anchorMax = new Vector2(1, 1);
            ltRT.pivot = new Vector2(0.5f, 1);
            ltRT.sizeDelta = new Vector2(0, 24);
            ltRT.anchoredPosition = new Vector2(0, -4);
            // Scroll body
            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(log.transform, false);
            var scRT = scroll.AddComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0, 0); scRT.anchorMax = new Vector2(1, 1);
            scRT.offsetMin = new Vector2(8, 8); scRT.offsetMax = new Vector2(-8, -28);
            logScroll = scroll.AddComponent<ScrollRect>();
            scroll.AddComponent<Image>().color = new Color(0, 0, 0, 0.30f);
            scroll.AddComponent<Mask>().showMaskGraphic = true;
            var content = new GameObject("Content");
            content.transform.SetParent(scroll.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);
            logScroll.content = contentRT;
            logScroll.vertical = true;
            logScroll.horizontal = false;
            logText = content.AddComponent<Text>();
            logText.font = CardView.LegacyFont();
            logText.fontSize = 12;
            logText.alignment = TextAnchor.UpperLeft;
            logText.color = new Color(0.9f, 0.9f, 0.9f);
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            logText.verticalOverflow = VerticalWrapMode.Overflow;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ---- Modal container ----
            genericModal = MakePanel("Modal", tableRoot);
            var mRT = (RectTransform)genericModal.transform;
            Stretch(mRT);
            genericModal.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            // Inner box
            var inner = MakePanel("Inner", genericModal.transform);
            var innRT = (RectTransform)inner.transform;
            innRT.anchorMin = new Vector2(0.5f, 0.5f); innRT.anchorMax = new Vector2(0.5f, 0.5f);
            innRT.pivot = new Vector2(0.5f, 0.5f);
            innRT.sizeDelta = new Vector2(800, 500);
            modalTitle = MakeText(inner.transform, "Title", 28, FontStyle.Bold);
            var mtRT = (RectTransform)modalTitle.transform;
            mtRT.anchorMin = new Vector2(0, 1); mtRT.anchorMax = new Vector2(1, 1);
            mtRT.pivot = new Vector2(0.5f, 1);
            mtRT.sizeDelta = new Vector2(-40, 40);
            mtRT.anchoredPosition = new Vector2(0, -16);
            modalTitle.alignment = TextAnchor.MiddleCenter;
            modalBody = MakeText(inner.transform, "", 16);
            var mbRT = (RectTransform)modalBody.transform;
            mbRT.anchorMin = new Vector2(0, 0); mbRT.anchorMax = new Vector2(1, 1);
            mbRT.offsetMin = new Vector2(20, 80); mbRT.offsetMax = new Vector2(-20, -64);
            modalBody.alignment = TextAnchor.UpperCenter;
            modalBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            modalBody.verticalOverflow = VerticalWrapMode.Overflow;
            // Button row
            var br = new GameObject("ButtonRow");
            br.transform.SetParent(inner.transform, false);
            modalButtonRow = br.AddComponent<RectTransform>();
            modalButtonRow.anchorMin = new Vector2(0, 0); modalButtonRow.anchorMax = new Vector2(1, 0);
            modalButtonRow.pivot = new Vector2(0.5f, 0);
            modalButtonRow.sizeDelta = new Vector2(-40, 60);
            modalButtonRow.anchoredPosition = new Vector2(0, 16);
            var brLayout = br.AddComponent<HorizontalLayoutGroup>();
            brLayout.spacing = 12;
            brLayout.childAlignment = TextAnchor.MiddleCenter;
            brLayout.childForceExpandWidth = false;
            brLayout.childForceExpandHeight = false;

            genericModal.SetActive(false);
        }

        private (RectTransform rect, Text label) MakeOpponentSlot(int idx, string name, Vector2 anchor, Vector2 pos, TextAnchor align)
        {
            var slot = new GameObject(name);
            slot.transform.SetParent(tableRoot, false);
            var slotRT = slot.AddComponent<RectTransform>();
            slotRT.anchorMin = slotRT.anchorMax = anchor;
            slotRT.pivot = new Vector2(anchor.x, 1);
            slotRT.sizeDelta = new Vector2(380, 130);
            slotRT.anchoredPosition = pos;

            var label = MakeText(slot.transform, $"Bot {idx + 1}", 14);
            label.alignment = align;
            var labelRT = (RectTransform)label.transform;
            labelRT.anchorMin = new Vector2(0, 1); labelRT.anchorMax = new Vector2(1, 1);
            labelRT.pivot = new Vector2(0.5f, 1);
            labelRT.sizeDelta = new Vector2(0, 22);

            // Card row.
            var row = new GameObject("CardRow");
            row.transform.SetParent(slot.transform, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 0); rowRT.anchorMax = new Vector2(1, 1);
            rowRT.offsetMin = new Vector2(0, 0); rowRT.offsetMax = new Vector2(0, -24);
            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = -30; // overlap the back faces
            hl.childAlignment = align == TextAnchor.UpperLeft
                ? TextAnchor.UpperLeft
                : align == TextAnchor.UpperRight ? TextAnchor.UpperRight : TextAnchor.UpperCenter;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;

            return (rowRT, label);
        }

        // ---------------- Phase routing ----------------

        private void ShowPhase(Phase newPhase)
        {
            phase = newPhase;
            bool inGame = (newPhase == Phase.Round);
            actionsBar.gameObject.SetActive(inGame);

            if (newPhase == Phase.Intro) ShowIntroModal();
            else if (newPhase == Phase.CharSelect) ShowCharSelectModal();
            else if (newPhase == Phase.Round) HideModal();
            else if (newPhase == Phase.RoundEnd) ShowRoundEndModal();
            else if (newPhase == Phase.FloorEnd) ShowFloorEndModal();
            else if (newPhase == Phase.BossRelic) ShowBossRelicModal();
            else if (newPhase == Phase.Fork) ShowForkModal();
            else if (newPhase == Phase.Shop) ShowShopModal();
            else if (newPhase == Phase.Reward) ShowRewardModal();
            else if (newPhase == Phase.Event) ShowEventModal();
            else if (newPhase == Phase.Cleanse) ShowCleanseModal();
            else if (newPhase == Phase.Treasure) ShowTreasureModal();
            else if (newPhase == Phase.RunEnd) ShowRunEndModal();
        }

        private void HideModal() { genericModal.SetActive(false); }

        private void OpenModal(string title, string body)
        {
            modalTitle.text = title;
            modalBody.text = body;
            ClearModalButtons();
            genericModal.SetActive(true);
        }

        private void ClearModalButtons()
        {
            for (int i = modalButtonRow.childCount - 1; i >= 0; i--)
                Destroy(modalButtonRow.GetChild(i).gameObject);
        }

        private void AddModalButton(string label, System.Action onClick, Color? bgColor = null)
        {
            var btn = MakeButton(modalButtonRow, label, bgColor ?? brandYellow,
                bgColor.HasValue ? Color.white : Color.black,
                () => onClick?.Invoke());
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 44;
        }

        // ---------------- Phase: Intro ----------------
        private void ShowIntroModal()
        {
            OpenModal("Lügen", "A roguelike bluff card game.\n\nPick a character, fight 9 floors,\nbuild your run deck, beat Lugen.");
            AddModalButton("Start", () => ShowPhase(Phase.CharSelect));
        }

        // ---------------- Phase: CharSelect ----------------
        private Vector2 charSelectScroll;
        private void ShowCharSelectModal()
        {
            OpenModal("Pick a character", "");
            // Reuse modalBody as a list — replace text with a list of buttons.
            modalBody.text = "";
            ClearModalButtons();
            // Make a vertical list of character buttons inside modalBody area.
            // Simpler: reset button row to vertical layout.
            DestroyImmediate(modalButtonRow.GetComponent<HorizontalLayoutGroup>());
            var v = modalButtonRow.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            modalButtonRow.sizeDelta = new Vector2(-40, 360);
            modalButtonRow.anchorMin = new Vector2(0, 0); modalButtonRow.anchorMax = new Vector2(1, 1);
            modalButtonRow.offsetMin = new Vector2(20, 60); modalButtonRow.offsetMax = new Vector2(-20, -60);

            foreach (var ch in CharacterCatalog.All.Values)
            {
                var btn = MakeButton(modalButtonRow, $"{ch.name} — {ch.passive}", new Color(0.18f, 0.22f, 0.30f), Color.white, () =>
                {
                    // Restore horizontal layout for future modals.
                    DestroyImmediate(modalButtonRow.GetComponent<VerticalLayoutGroup>());
                    var h = modalButtonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                    h.spacing = 12;
                    h.childAlignment = TextAnchor.MiddleCenter;
                    h.childForceExpandWidth = false;
                    h.childForceExpandHeight = false;
                    modalButtonRow.anchorMin = new Vector2(0, 0); modalButtonRow.anchorMax = new Vector2(1, 0);
                    modalButtonRow.pivot = new Vector2(0.5f, 0);
                    modalButtonRow.sizeDelta = new Vector2(-40, 60);
                    modalButtonRow.anchoredPosition = new Vector2(0, 16);
                    StartRun(ch.id);
                });
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 44;
            }
        }

        // ---------------- Phase: Round (HUD + table) ----------------

        private List<CardView> handViews = new List<CardView>();
        private List<CardView> pileViews = new List<CardView>();
        private List<CardView> drawViews = new List<CardView>();
        private List<List<CardView>> opponentViews = new List<List<CardView>>();

        private void SyncRoundUI()
        {
            var s = controller.State;
            var rs = run.State;

            // HUD
            string mod = string.IsNullOrEmpty(rs.currentFloorModifier) ? "" : $"  ·  MOD: {rs.currentFloorModifier}";
            hudText.text = $"Floor {rs.currentFloor}/{Constants.TOTAL_FLOORS}    " +
                           $"<color=#e85a5a>♥ {rs.hearts}</color>    " +
                           $"<color=#e8c845>{rs.gold}g</color>    " +
                           $"Target: <b>{s.targetRank.ToShort()}</b>    " +
                           $"Wins: {string.Join("/", rs.roundsWon)}{mod}";
            hudText.supportRichText = true;

            // Hand
            RebuildList(handViews, handArea, s.hands[0], faceDown: false, onClick: OnHandCardClicked);

            // Pile
            RebuildList(pileViews, pileArea, s.pile.Select(p => p.card).ToList(), faceDown: true, onClick: null, overlap: true);

            // Draw pile (just show 1 back + count text)
            RebuildList(drawViews, drawPileArea,
                s.drawPile.Count > 0 ? new List<Card> { s.drawPile[s.drawPile.Count - 1] } : new List<Card>(),
                faceDown: true, onClick: null);
            if (drawViews.Count > 0)
            {
                var t = drawViews[0].GetComponentInChildren<Text>();
                if (t != null) t.text = $"×{s.drawPile.Count}";
            }

            // Opponents (seats 1, 2, 3 → slots 0, 1, 2)
            while (opponentViews.Count < 3) opponentViews.Add(new List<CardView>());
            for (int i = 1; i < s.NumPlayers && i <= 3; i++)
            {
                int slot = i - 1;
                opponentTexts[slot].text = BotLabel(i);
                RebuildList(opponentViews[slot], opponentCardRows[slot], s.hands[i], faceDown: true, onClick: null, overlap: true);
            }

            // Action prompt + buttons
            UpdateActionsBar();
        }

        private string BotLabel(int seat)
        {
            var s = controller.State;
            string pers = run.State.botPersonalities[seat] ?? "?";
            string status = s.eliminated[seat] ? "eliminated" : s.finished[seat] ? "done" : $"{s.hands[seat].Count} cards";
            string arrow = (s.currentTurn == seat && !challengeWindowOpen) ? "▶ " : "";
            return $"{arrow}<b>seat {seat}</b> ({pers}) — {status}";
        }

        // Rebuild a list of CardView children to match `cards` exactly.
        private void RebuildList(List<CardView> views, RectTransform parent, List<Card> cards, bool faceDown, System.Action<CardView> onClick, bool overlap = false)
        {
            // Strip extras.
            while (views.Count > cards.Count)
            {
                Destroy(views[views.Count - 1].gameObject);
                views.RemoveAt(views.Count - 1);
            }
            // Add missing.
            while (views.Count < cards.Count)
            {
                var v = CardView.Create(parent, cards[views.Count], faceDown, onClick);
                views.Add(v);
            }
            // Sync data.
            for (int i = 0; i < cards.Count; i++)
            {
                views[i].cardData = cards[i];
                views[i].SetFaceDown(faceDown);
                if (!faceDown)
                {
                    bool sel = selectedCardIds.Contains(cards[i].id);
                    views[i].SetSelected(sel);
                }
            }
        }

        private void OnHandCardClicked(CardView v)
        {
            if (controller.State.currentTurn != 0 || challengeWindowOpen) return;
            if (selectedCardIds.Contains(v.cardData.id)) selectedCardIds.Remove(v.cardData.id);
            else selectedCardIds.Add(v.cardData.id);
        }

        private void UpdateActionsBar()
        {
            var s = controller.State;
            bool myTurn = (s.currentTurn == 0) && !challengeWindowOpen && !s.gameOver;
            actionPrompt.text = myTurn
                ? $"Your turn — select 1–3, play as <b>{s.targetRank.ToShort()}</b>"
                : (challengeWindowOpen
                    ? (currentChallengerSeat == 0 ? "Will you call LIAR?" : $"Seat {currentChallengerSeat} is deciding…")
                    : $"Seat {s.currentTurn} is playing…");
            playButton.gameObject.SetActive(myTurn);
            clearButton.gameObject.SetActive(myTurn);
            playButtonText.text = $"Play {selectedCardIds.Count}";
            playButton.interactable = myTurn && selectedCardIds.Count >= 1 && selectedCardIds.Count <= 3;
            callLiarButton.gameObject.SetActive(challengeWindowOpen && currentChallengerSeat == 0);
            letItGoButton.gameObject.SetActive(challengeWindowOpen && currentChallengerSeat == 0);
            challengeBar.SetActive(challengeWindowOpen);
        }

        private void UpdateChallengeText()
        {
            var s = controller.State;
            if (s.lastPlay == null) { challengeText.text = ""; return; }
            string head = currentChallengerSeat == 0
                ? $"Your call — {challengeTimer:F1}s   |   "
                : $"Seat {currentChallengerSeat} thinking…   |   ";
            challengeText.text = head +
                $"seat {s.lastPlay.playerIdx} played {s.lastPlay.count} as {s.lastPlay.claim.ToShort()}";
        }

        // ---------------- Run / round logic (delegates to engine) ----------------

        private void StartRun(string charId)
        {
            run = new RunManager();
            run.StartRun(charId);
            controller = new RoundController(run.State);
            controller.Logs.OnLog += AppendLog;
            controller.Logs.OnPrivatePeek += AppendLog;
            RollFloorModifier();
            controller.StartRound();
            AppendLog($"=== Run started: {CharacterCatalog.Get(charId)?.name}, Floor 1 ===");
            ShowPhase(Phase.Round);
            botActionTimer = botTurnDelay;
        }

        private void RollFloorModifier()
        {
            run.State.currentFloorModifier = null;
            int f = run.State.currentFloor;
            if (f >= 4 && f != 6 && f != 9 && f != 3)
            {
                if (!string.IsNullOrEmpty(run.State.preRolledNextFloorMod))
                {
                    run.State.currentFloorModifier = run.State.preRolledNextFloorMod;
                    run.State.preRolledNextFloorMod = null;
                }
                else
                {
                    var pool = FloorModifiers.EligibleForFloor(f);
                    if (pool.Count > 0) run.State.currentFloorModifier = Rng.Pick(pool);
                }
                if (run.State.currentFloorModifier != null)
                    AppendLog($"Floor modifier: {FloorModifiers.All[run.State.currentFloorModifier].name}.");
            }
        }

        private void DoHumanPlay()
        {
            var s = controller.State;
            if (s.currentTurn != 0) return;
            if (selectedCardIds.Count < 1 || selectedCardIds.Count > 3) { AppendLog("Pick 1–3 cards."); return; }
            var ok = TurnResolver.TryPlay(s, 0, selectedCardIds.ToList(), 1, 3, controller.Logs);
            if (!ok) { AppendLog("Play rejected."); return; }
            selectedCardIds.Clear();
            if (s.hands[0].Count == 0) { JackCurse.MarkFinished(s, 0); AppendLog("You emptied your hand!"); }
            OpenChallenge(0);
        }

        private void DoCallLiar() => DoCallLiarFor(0);

        private void DoCallLiarFor(int challenger)
        {
            var outcome = controller.CallLiar(challenger);
            challengeWindowOpen = false; currentChallengerSeat = -1;
            AppendLog(outcome.truthTold
                ? $"Truth told. Seat {challenger} takes the pile and is skipped."
                : $"LIAR! Seat {controller.State.lastPlay.playerIdx} takes the pile.");
            ResolvePostChallenge(outcome);
        }

        private void BotChallengeDecide()
        {
            var s = controller.State;
            int bot = currentChallengerSeat;
            var pers = PersonalityCatalog.Get(run.State.botPersonalities[bot]);
            int auditorEveryN = 1 + Rng.Range(0, 5);
            bool willCall = BotBrain.ShouldCallLiar(s, bot, pers, auditorEveryN);
            if (willCall) { AppendLog($"Seat {bot} ({pers?.name}) calls LIAR!"); DoCallLiarFor(bot); }
            else { AppendLog($"Seat {bot} passes."); CloseChallenge(); }
        }

        private void CloseChallenge()
        {
            challengeWindowOpen = false; currentChallengerSeat = -1;
            if (controller?.State != null) controller.State.challengeOpen = false;
            AdvanceAfterPlay();
        }

        private void OpenChallenge(int playerJustPlayed)
        {
            int challenger = TurnRotation.FindNextActive(controller.State, playerJustPlayed);
            if (challenger < 0)
            {
                if (controller?.State != null) controller.State.challengeOpen = false;
                AdvanceAfterPlay();
                return;
            }
            currentChallengerSeat = challenger;
            challengeWindowOpen = true;
            challengeTimer = challengeWindowSec;
            botChallengeDecideTimer = 0.7f;
            controller.OpenChallengeWindow(playerJustPlayed);
        }

        private void PumpBotTurn()
        {
            var s = controller.State;
            int bot = s.currentTurn;
            if (s.eliminated[bot] || s.finished[bot] || s.outOfTurns[bot] || s.hands[bot].Count == 0)
            {
                if (s.hands[bot].Count == 0) JackCurse.MarkFinished(s, bot);
                controller.AdvanceTurn();
                return;
            }
            int pileBefore = s.pile.Count;
            controller.RunBotTurn(bot);
            if (s.pile.Count <= pileBefore) return;
            OpenChallenge(bot);
        }

        private void AdvanceAfterPlay()
        {
            controller.AdvanceTurn();
            CheckRoundEnd();
        }

        private void ResolvePostChallenge(LiarOutcome outcome)
        {
            var s = controller.State;
            int picker = outcome.pickerUpper;
            if (JackCurse.CheckCurse(s, picker, controller.JackLimitFor(picker)))
            {
                AppendLog($"Jack curse: seat {picker} eliminated.");
                if (picker == 0) run.State.hearts = Mathf.Max(0, run.State.hearts - 1);
            }
            for (int p = 0; p < s.NumPlayers; p++)
                if (!s.finished[p] && !s.eliminated[p] && s.hands[p].Count == 0)
                    { JackCurse.MarkFinished(s, p); AppendLog($"Seat {p} emptied their hand."); }
            CheckRoundEnd();
            if (!s.gameOver)
            {
                int next = TurnRotation.FindNextActive(s, picker);
                if (next < 0) s.gameOver = true; else s.currentTurn = next;
            }
        }

        private void CheckRoundEnd()
        {
            var s = controller.State;
            if (JackCurse.ShouldEndRound(s))
            {
                s.gameOver = true;
                AppendLog($"=== Round over. Placements: {string.Join(", ", s.placements)} ===");
            }
        }

        // ---------------- Phase: Round End ----------------
        private void GoToRoundEnd()
        {
            var s = controller.State;
            lastWinnerIdx = s.placements.Count > 0 ? s.placements[0] : (int?)null;
            lastRoundGoldGained = 0;
            if (s.placements.Count > 0 && s.placements[0] == 0)
                lastRoundGoldGained = GoldEconomy.AddGold(run.State, Constants.GOLD_PLACE_1);
            else if (s.placements.Count > 1 && s.placements[1] == 0)
                lastRoundGoldGained = GoldEconomy.AddGold(run.State, Constants.GOLD_PLACE_2);
            if (lastWinnerIdx.HasValue) run.State.roundsWon[lastWinnerIdx.Value]++;
            ShowPhase(Phase.RoundEnd);
        }

        private void ShowRoundEndModal()
        {
            var rs = run.State;
            string winnerStr = lastWinnerIdx.HasValue ? $"seat {lastWinnerIdx.Value}" : "(no winner — all eliminated)";
            string gainStr = lastRoundGoldGained > 0 ? $"+{lastRoundGoldGained}g" : "0g";
            int floorWinner = -1;
            for (int p = 0; p < rs.roundsWon.Length; p++)
                if (rs.roundsWon[p] >= Constants.ROUNDS_TO_WIN_FLOOR) floorWinner = p;
            OpenModal("Round over",
                $"Winner: {winnerStr}\n" +
                $"Floor wins: {string.Join(" / ", rs.roundsWon)}\n" +
                $"Gold: {rs.gold}g  ({gainStr} this round)\n" +
                $"Hearts: {rs.hearts}");
            if (floorWinner >= 0)
                AddModalButton("Continue", () => GoToFloorEnd(floorWinner));
            else
                AddModalButton("Next round", NextRound);
        }

        private void NextRound()
        {
            selectedCardIds.Clear();
            controller.StartRound();
            ShowPhase(Phase.Round);
            botActionTimer = botTurnDelay;
        }

        // ---------------- Phase: Floor End ----------------
        private void GoToFloorEnd(int floorWinner)
        {
            var rs = run.State;
            bool humanWon = floorWinner == 0;
            if (humanWon)
            {
                int floorBonus = GoldEconomy.AddGold(rs, Constants.GOLD_PER_FLOOR_WIN);
                AppendLog($"=== Floor {rs.currentFloor} cleared. +{floorBonus}g ===");
            }
            else
            {
                rs.hearts = Mathf.Max(0, rs.hearts - 1);
                AppendLog($"=== Lost floor {rs.currentFloor}. -1 Heart. ({rs.hearts} left) ===");
            }
            ShowPhase(Phase.FloorEnd);
        }

        private void ShowFloorEndModal()
        {
            var rs = run.State;
            OpenModal("Floor end", $"Floor {rs.currentFloor} resolved.\nHearts: {rs.hearts}    Gold: {rs.gold}g");
            if (rs.hearts <= 0) { AddModalButton("Run over", () => ShowPhase(Phase.RunEnd)); return; }
            bool humanWonFloor = rs.roundsWon.Length > 0 && rs.roundsWon[0] >= Constants.ROUNDS_TO_WIN_FLOOR;
            if (humanWonFloor && rs.currentFloor == Constants.TOTAL_FLOORS)
            { AddModalButton("Victory!", () => ShowPhase(Phase.RunEnd)); return; }
            if (humanWonFloor)
            {
                if (run.IsBossFloor(rs.currentFloor)) AddModalButton("Pick a relic", () => ShowPhase(Phase.BossRelic));
                else AddModalButton("Continue", () => ShowPhase(Phase.Fork));
            }
            else
            {
                AddModalButton("Retry floor", () => { rs.roundsWon = new int[Constants.NUM_PLAYERS]; NextRound(); });
            }
        }

        // ---------------- Phase: Boss relic ----------------
        private void ShowBossRelicModal()
        {
            string bossId = run.State.botPersonalities[1];
            if (string.IsNullOrEmpty(bossId) || !RelicCatalog.BossPool.ContainsKey(bossId))
            { AdvanceToNextFloor(); return; }
            var pool = RelicCatalog.BossPool[bossId].Where(r => !run.State.relics.Contains(r)).ToList();
            Rng.ShuffleInPlace(pool);
            bossRelicOffers = pool.Take(2).ToList();
            string body = string.Join("\n\n", bossRelicOffers.Select(rid =>
            {
                var d = RelicCatalog.All[rid];
                return $"<b>{d.name}</b> — {d.desc}";
            }));
            OpenModal("Boss relic", body);
            foreach (var rid in bossRelicOffers)
            {
                var d = RelicCatalog.All[rid];
                AddModalButton($"Take {d.name}", () =>
                {
                    run.State.relics.Add(rid); AppendLog($"Relic: {d.name}.");
                    AdvanceToNextFloor();
                });
            }
            AddModalButton("Skip", () => AdvanceToNextFloor(), new Color(0.3f, 0.3f, 0.36f));
        }

        // ---------------- Phase: Fork ----------------
        private void ShowForkModal()
        {
            currentFork = ForkNode.RollFork(run.State.currentFloor, false);
            OpenModal("Fork — pick a node", "");
            foreach (var opt in currentFork.options)
                AddModalButton(opt.ToString(), () => EnterForkNode(opt));
        }

        private void EnterForkNode(ForkNodeType node)
        {
            switch (node)
            {
                case ForkNodeType.Shop: currentShop = new Lugen.Shop.Shop(run.State); ShowPhase(Phase.Shop); break;
                case ForkNodeType.Reward:
                    var pool = JokerCatalog.All.Values
                        .Where(j => !run.State.jokers.Has(j.id) || (j.stackable && run.State.jokers.Stack(j.id) < j.maxStack))
                        .ToList();
                    Rng.ShuffleInPlace(pool);
                    rewardJokerOffers = pool.Take(2).Select(j => j.id).ToList();
                    ShowPhase(Phase.Reward); break;
                case ForkNodeType.Event: RollRandomEvent(); ShowPhase(Phase.Event); break;
                case ForkNodeType.Cleanse: ShowPhase(Phase.Cleanse); break;
                case ForkNodeType.Treasure:
                    var tpool = RelicCatalog.TreasurePool.Where(r => !run.State.relics.Contains(r) && RelicCatalog.All[r].unlock == null).ToList();
                    Rng.ShuffleInPlace(tpool);
                    treasureRelicOffers = tpool.Take(2).ToList();
                    ShowPhase(Phase.Treasure); break;
            }
        }

        // ---------------- Phase: Shop ----------------
        private void ShowShopModal()
        {
            string body = $"Gold: {run.State.gold}g\n\n";
            body += "<b>Consumables:</b>\n";
            foreach (var id in currentShop.Inventory)
                if (ConsumableCatalog.All.TryGetValue(id, out var c)) body += $"  {c.name} — {c.price}g — {c.desc}\n";
            body += "\n<b>Jokers:</b>\n";
            foreach (var id in currentShop.JokerOffers)
                if (JokerCatalog.All.TryGetValue(id, out var j)) body += $"  {j.name} [{j.rarity}] — {j.price}g\n";
            if (currentShop.RelicOffers.Count > 0) {
                body += "\n<b>Relic:</b>\n";
                foreach (var id in currentShop.RelicOffers)
                    if (RelicCatalog.All.TryGetValue(id, out var r)) body += $"  {r.name} — {r.price}g\n";
            }
            OpenModal("Shop", body);
            // Build buttons for buying.
            foreach (var id in currentShop.Inventory)
                if (ConsumableCatalog.All.TryGetValue(id, out var c))
                    AddModalButton($"Buy {c.name} ({c.price}g)", () => { var err = currentShop.BuyConsumable(id); AppendLog(err ?? $"Bought {c.name}."); ShowShopModal(); });
            foreach (var id in currentShop.JokerOffers)
                if (JokerCatalog.All.TryGetValue(id, out var j))
                    AddModalButton($"Buy {j.name} ({j.price}g)", () => { var err = currentShop.BuyJoker(id); AppendLog(err ?? $"Bought {j.name}."); ShowShopModal(); });
            foreach (var id in currentShop.RelicOffers)
                if (RelicCatalog.All.TryGetValue(id, out var r))
                    AddModalButton($"Buy {r.name} ({r.price}g)", () => { var err = currentShop.BuyRelic(id); AppendLog(err ?? $"Bought {r.name}."); ShowShopModal(); });
            AddModalButton("Leave shop", AdvanceToNextFloor, new Color(0.3f, 0.3f, 0.36f));
        }

        // ---------------- Phase: Reward ----------------
        private void ShowRewardModal()
        {
            string body = "Pick a joker, or skip for 75g.\n\n";
            foreach (var id in rewardJokerOffers)
                if (JokerCatalog.All.TryGetValue(id, out var j)) body += $"<b>{j.name}</b> [{j.rarity}] — {j.desc}\n\n";
            OpenModal("Reward", body);
            foreach (var id in rewardJokerOffers)
                if (JokerCatalog.All.TryGetValue(id, out var j))
                    AddModalButton($"Take {j.name}", () => { run.State.jokers.TryEquip(j); AppendLog($"Joker equipped: {j.name}."); AdvanceToNextFloor(); });
            AddModalButton($"Skip — +{Constants.REWARD_NODE_GOLD}g", () => { int g = GoldEconomy.AddGold(run.State, Constants.REWARD_NODE_GOLD); AppendLog($"+{g}g"); AdvanceToNextFloor(); }, new Color(0.3f, 0.3f, 0.36f));
        }

        // ---------------- Phase: Event ----------------
        private void RollRandomEvent()
        {
            var pool = new System.Action[]
            {
                () => { lastEventTitle = "Found Coins"; lastEventText = "You spot coins on the floor."; int g = GoldEconomy.AddGold(run.State, 30); lastEventResult = $"+{g}g"; },
                () => { lastEventTitle = "Generous Drunk"; lastEventText = "A patron tips you."; int g = GoldEconomy.AddGold(run.State, 50); lastEventResult = $"+{g}g"; },
                () => { lastEventTitle = "Pickpocket"; lastEventText = "Someone bumps into you."; int loss = Mathf.Min(run.State.gold, 20); run.State.gold -= loss; lastEventResult = $"-{loss}g"; },
                () => { lastEventTitle = "Lucky Find"; lastEventText = "A charm clings to one of your cards."; var cands = run.State.runDeck.Where(c => c.affix != Affix.Steel && c.rank != Rank.Jack).ToList(); if (cands.Count > 0) { var card = Rng.Pick(cands); var old = card.affix; card.affix = Rng.Pick(AffixExtensions.AllRandomable); lastEventResult = $"{card.rank.ToShort()}: {old.ToShort()} → {card.affix.ToShort()}"; } else lastEventResult = "(no eligible card)"; },
                () => { lastEventTitle = "Shrine of Hearts"; lastEventText = "Donate 100g for a Heart shard."; if (run.State.gold >= 100) { run.State.gold -= 100; run.State.heartShards++; if (run.State.heartShards >= Constants.HEART_SHARDS_REQUIRED) { run.State.hearts++; run.State.heartShards = 0; lastEventResult = "+1 Heart!"; } else lastEventResult = $"+1 shard ({run.State.heartShards}/{Constants.HEART_SHARDS_REQUIRED})"; } else lastEventResult = "Not enough gold."; },
            };
            pool[Rng.Range(0, pool.Length)]();
            AppendLog($"Event: {lastEventTitle} — {lastEventResult}");
        }

        private void ShowEventModal()
        {
            OpenModal(lastEventTitle, lastEventText + "\n\n<b>Result:</b> " + lastEventResult);
            AddModalButton("Continue", AdvanceToNextFloor);
        }

        // ---------------- Phase: Cleanse ----------------
        private void ShowCleanseModal()
        {
            var cursed = run.State.runDeck.Where(c => c.affix == Affix.Cursed).ToList();
            string body = cursed.Count > 0
                ? "Cursed cards in your run deck:\n" + string.Join("\n", cursed.Select(c => $"  {c.rank.ToShort()} (Cursed)"))
                : "No Cursed cards. Strip an affix from one of your cards instead.";
            OpenModal("Cleanse", body);
            foreach (var c in cursed)
                AddModalButton($"Remove {c.rank.ToShort()}", () => { run.State.runDeck.Remove(c); AppendLog($"Removed Cursed {c.rank.ToShort()}."); AdvanceToNextFloor(); });
            foreach (var c in run.State.runDeck.Where(x => x.affix != Affix.None && x.affix != Affix.Cursed).Take(6))
                AddModalButton($"Strip {c.affix.ToShort()} from {c.rank.ToShort()}", () => { var old = c.affix; c.affix = Affix.None; AppendLog($"Stripped {old.ToShort()}."); AdvanceToNextFloor(); });
            AddModalButton("Skip", AdvanceToNextFloor, new Color(0.3f, 0.3f, 0.36f));
        }

        // ---------------- Phase: Treasure ----------------
        private void ShowTreasureModal()
        {
            string body = "Pick a treasure relic:\n\n";
            foreach (var rid in treasureRelicOffers)
                if (RelicCatalog.All.TryGetValue(rid, out var d)) body += $"<b>{d.name}</b> — {d.desc}\n\n";
            OpenModal("Treasure", body);
            foreach (var rid in treasureRelicOffers)
                if (RelicCatalog.All.TryGetValue(rid, out var d))
                    AddModalButton($"Take {d.name}", () => { run.State.relics.Add(rid); AppendLog($"Relic: {d.name}."); AdvanceToNextFloor(); });
            AddModalButton("Skip", AdvanceToNextFloor, new Color(0.3f, 0.3f, 0.36f));
        }

        // ---------------- Phase: Run End ----------------
        private void ShowRunEndModal()
        {
            var rs = run.State;
            bool victory = rs.hearts > 0 && rs.currentFloor >= Constants.TOTAL_FLOORS && rs.roundsWon[0] >= Constants.ROUNDS_TO_WIN_FLOOR;
            OpenModal(victory ? "Victory!" : "Run over",
                victory
                    ? $"You cleared all {Constants.TOTAL_FLOORS} floors with {rs.hearts} ♥ remaining.\nFinal gold: {rs.gold}g"
                    : $"You ran out of Hearts on Floor {rs.currentFloor}.\nFinal gold: {rs.gold}g");
            AddModalButton("New run", () => { run = null; controller = null; uiLog.Clear(); ShowPhase(Phase.CharSelect); });
        }

        // ---------------- Floor advance ----------------
        private void AdvanceToNextFloor()
        {
            run.AdvanceFloor();
            RollFloorModifier();
            if (run.State.currentFloor > Constants.TOTAL_FLOORS) { ShowPhase(Phase.RunEnd); return; }
            controller = new RoundController(run.State);
            controller.Logs.OnLog += AppendLog;
            controller.Logs.OnPrivatePeek += AppendLog;
            controller.StartRound();
            AppendLog($"=== Floor {run.State.currentFloor} starts. Target: {controller.State.targetRank.ToShort()} ===");
            ShowPhase(Phase.Round);
            botActionTimer = botTurnDelay;
            selectedCardIds.Clear();
        }

        // ---------------- Helpers ----------------
        private void AppendLog(string msg)
        {
            uiLog.Add(msg);
            if (uiLog.Count > 200) uiLog.RemoveAt(0);
            if (logText != null) logText.text = string.Join("\n", uiLog);
            if (logScroll != null) logScroll.verticalNormalizedPosition = 0f; // bottom
        }

        // EventSystem creation that works under both old and new Input System.
        // Unity 6 defaults to the new Input System; if we add StandaloneInputModule
        // there it silently fails (the old module reads UnityEngine.Input which
        // is disabled), and no clicks register. Try InputSystemUIInputModule
        // (from com.unity.inputsystem) via reflection first, and only fall back
        // to StandaloneInputModule if the new package isn't installed.
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();

            var newModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModuleType != null)
            {
                es.AddComponent(newModuleType);
                return;
            }
            es.AddComponent<StandaloneInputModule>();
        }

        // ---- UI factory helpers ----
        private GameObject MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private GameObject MakePanel(string name, Transform parent)
        {
            var go = MakeImage(name, parent, panelColor);
            return go;
        }

        private Text MakeText(Transform parent, string content, int size, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 24);
            var t = go.AddComponent<Text>();
            t.font = CardView.LegacyFont();
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        private Button MakeButton(RectTransform parent, string label, Color bg, Color fg, System.Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140, 36);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bg;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.20f);
            colors.disabledColor = new Color(bg.r * 0.5f, bg.g * 0.5f, bg.b * 0.5f, 0.6f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            // Text
            var txt = MakeText(go.transform, label, 16, FontStyle.Bold);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = fg;
            Stretch((RectTransform)txt.transform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
