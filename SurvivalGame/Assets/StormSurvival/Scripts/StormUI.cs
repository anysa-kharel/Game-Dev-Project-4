using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StormSurvival
{
    public sealed class StormUI : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(14, 23, 22, 255);
        private static readonly Color Paper = new Color32(247, 240, 218, 255);
        private static readonly Color Muted = new Color32(178, 197, 184, 255);
        private static readonly Color Gold = new Color32(248, 192, 103, 255);
        private static readonly Color Card = new Color32(16, 29, 27, 246);
        private static readonly Color Border = new Color32(76, 103, 90, 245);
        private static readonly Color Track = new Color32(49, 68, 61, 255);
        private static readonly Color Alert = new Color32(255, 144, 117, 255);
        private static readonly Vector2 TopLeft = new Vector2(0, 1);
        private static readonly Vector2 Top = new Vector2(.5f, 1);
        private static readonly Vector2 TopRight = Vector2.one;
        private static readonly Vector2 BottomLeft = Vector2.zero;
        private static readonly Vector2 Bottom = new Vector2(.5f, 0);
        private static readonly Vector2 Center = new Vector2(.5f, .5f);

        private StormGame game;
        private Font font;
        private Texture2D roundedTexture;
        private Texture2D backdropTexture;
        private Texture2D reticleTexture;
        private RawImage reticle;
        private Sprite roundedSprite;
        private RectTransform hud, menu, interaction, noticeCard;
        private Text objective, supplies, weather, weatherDetail, direction, prompt, notice;
        private Text torchStatus, fuelStatus, menuTitle, menuSummary, primaryText, secondaryText;
        private Button primary, secondary;
        private RectTransform holdTrack;
        private Image holdFill;
        private readonly Meter[] meters = new Meter[6];
        private readonly Image[] slotBorders = new Image[3];
        private readonly Image[] slotKeys = new Image[3];
        private readonly Text[] slotNumbers = new Text[3];
        private int displayedSlot = -1;

        private struct Meter
        {
            public Image Fill;
            public Text Value;
            public Color Color;

            public void Set(float fraction, bool warning)
            {
                int percent = Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100);
                Value.text = percent + "%";
                Value.color = warning ? Alert : Muted;
                Fill.color = warning ? Alert : Color;
                Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
            }
        }

        public void Build(StormGame owner)
        {
            game = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateTextures();

            var root = Layer(transform, "Survival Interface");
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var input = new GameObject("Survival UI input", typeof(EventSystem), typeof(StandaloneInputModule));
                input.transform.SetParent(transform, false);
            }

            BuildHud(root);
            BuildMenu(root);
            RefreshMode();
        }

        private void BuildHud(RectTransform root)
        {
            hud = Layer(root, "Gameplay");

            var mission = CardAt(hud, "Objective", TopLeft, 24, 22, 396, 106);
            Box(mission, "Accent", TopLeft, 16, 17, 20, 2, Gold);
            Label(mission, "Objective heading", "OBJECTIVE", 44, 9, 316, 20, 12, Gold, bold: true);
            objective = Label(mission, "Current objective", "", 16, 35, 364, 65, 17, Paper);

            var bearing = CardAt(hud, "Direction", Top, -100, 22, 200, 36);
            direction = Label(bearing, "Objective direction", "", 8, 0, 184, 36, 13, Paper, TextAnchor.MiddleCenter, true);

            var conditions = CardAt(hud, "Conditions and resources", TopRight, -279, 22, 255, 128);
            weather = Label(conditions, "Weather", "", 15, 10, 225, 21, 13, Gold, bold: true);
            weatherDetail = Label(conditions, "Weather advice", "", 15, 34, 225, 21, 13, Muted);
            Box(conditions, "Divider", TopLeft, 15, 63, 225, 1, Track);
            Label(conditions, "Supplies heading", "SUPPLIES", 15, 73, 225, 19, 11, Muted, bold: true);
            supplies = Label(conditions, "Carried resources", "", 15, 94, 225, 24, 15, Paper, bold: true);

            var vitals = CardAt(hud, "Vitals", BottomLeft, 24, -220, 243, 196);
            Label(vitals, "Vitals heading", "VITALS", 15, 11, 195, 20, 12, Gold, bold: true);
            meters[0] = BuildMeter(vitals, "Health", 43, new Color32(230, 135, 129, 255));
            meters[1] = BuildMeter(vitals, "Warmth", 67, Gold);
            meters[2] = BuildMeter(vitals, "Stamina", 91, new Color32(184, 219, 143, 255));
            meters[3] = BuildMeter(vitals, "Hunger", 115, new Color32(212, 180, 137, 255));
            meters[4] = BuildMeter(vitals, "Thirst", 139, new Color32(128, 201, 225, 255));
            meters[5] = BuildMeter(vitals, "Wetness", 163, new Color32(142, 183, 212, 255));

            var equipment = Place(hud, "Equipment", Bottom, -234, -81, 468, 57);
            torchStatus = BuildSlot(equipment, 0, "Torch", "");
            fuelStatus = BuildSlot(equipment, 1, "Fuel cup", "");
            BuildSlot(equipment, 2, "Pickaxe", "Left click to mine");
            var shortcuts = CardAt(hud, "Shortcuts", Bottom, -94, -116, 188, 25);
            Label(shortcuts, "Craft and pause hotkeys", "Tab  Craft     ·     Esc  Pause", 4, 0, 180, 25,
                12, Paper, TextAnchor.MiddleCenter);

            reticle = Place(hud, "Aim reticle", Center, -15, -15, 30, 30).gameObject.AddComponent<RawImage>();
            reticle.rectTransform.pivot = Center;
            reticle.rectTransform.anchoredPosition = Vector2.zero;
            reticle.texture = reticleTexture;
            reticle.raycastTarget = false;
            var outline = reticle.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Ink.r, Ink.g, Ink.b, .85f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            interaction = CardAt(hud, "Interaction", Center, -257, 40, 514, 60);
            prompt = Label(interaction, "Interaction prompt", "", 14, 3, 486, 51, 16, Paper, TextAnchor.MiddleCenter, true);
            holdTrack = Place(interaction, "Hold progress", TopLeft, 17, 50, 480, 3);
            AddImage(holdTrack, Track);
            holdFill = Fill(holdTrack, "Progress", Gold);
            interaction.gameObject.SetActive(false);

            noticeCard = CardAt(hud, "Notification", Top, -212, 80, 424, 56);
            notice = Label(noticeCard, "Notification text", "", 15, 2, 394, 52, 15, Paper, TextAnchor.MiddleCenter);
            noticeCard.gameObject.SetActive(false);
        }

        private Meter BuildMeter(RectTransform parent, string name, int y, Color color)
        {
            Label(parent, name, name, 15, y - 6, 76, 20, 13, Paper, TextAnchor.MiddleLeft);
            var track = Place(parent, name + " meter", TopLeft, 88, y + 1, 96, 6);
            AddImage(track, Track);
            return new Meter
            {
                Fill = Fill(track, "Fill", color),
                Value = Label(parent, name + " value", "", 190, y - 6, 38, 20, 12, Muted, TextAnchor.MiddleRight),
                Color = color
            };
        }

        private Text BuildSlot(RectTransform parent, int index, string name, string detail)
        {
            var slot = Place(parent, name, TopLeft, index * 159, 0, 150, 57);
            slotBorders[index] = AddImage(slot, Border, true);
            AddImage(Place(slot, "Background", TopLeft, 1, 1, 148, 55), Card, true);
            slotKeys[index] = ImageAt(slot, "Keycap", TopLeft, 9, 13, 30, 30, Track, true);
            slotNumbers[index] = Label(slot, "Key", (index + 1).ToString(), 9, 13, 30, 30,
                15, Paper, TextAnchor.MiddleCenter, true);
            Label(slot, "Equipment name", name, 48, 9, 99, 19, 15, Paper, bold: true);
            return Label(slot, "Equipment status", detail, 48, 31, 98, 19, 11, Muted);
        }

        private void BuildMenu(RectTransform root)
        {
            menu = Layer(root, "Menu");
            var backdrop = Layer(menu, "Backdrop").gameObject.AddComponent<RawImage>();
            backdrop.texture = backdropTexture;
            backdrop.raycastTarget = false;
            var frame = Place(menu, "Menu layout", Center, -640, -360, 1280, 720);

            Box(frame, "Title rule", TopLeft, 80, 137, 66, 3, Gold);
            menuTitle = Label(frame, "Menu title", "", 75, 159, 625, 195, 88, Paper, bold: true);
            menuTitle.lineSpacing = .9f;
            menuSummary = Label(frame, "Menu summary", "", 80, 363, 610, 98, 20, Muted);
            primary = MakeButton(frame, "Primary action", 80, 477, 324, 56, Gold, Ink, out primaryText);
            primary.onClick.AddListener(RunPrimaryAction);
            secondary = MakeButton(frame, "Secondary action", 80, 546, 324, 48, Track, Paper, out secondaryText);
            secondary.onClick.AddListener(() =>
            {
                if (game.Mode == StormMode.Crafting) game.SetMode(StormMode.Playing);
                else game.Restart();
            });

            var guide = CardAt(frame, "Field guide", TopLeft, 765, 82, 435, 556);
            Label(guide, "Controls heading", "Field controls", 25, 19, 385, 39, 25, Paper, bold: true);
            Box(guide, "Controls divider", TopLeft, 25, 63, 385, 1, Track);
            GuideRow(guide, 77, "WASD  /  Mouse", "Move and look");
            GuideRow(guide, 106, "Shift  /  Space  /  C", "Sprint, jump, crouch");
            GuideRow(guide, 135, "1  /  2  /  3", "Equip torch, fuel cup, pickaxe");
            GuideRow(guide, 164, "Left click", "Mine with the pickaxe");
            GuideRow(guide, 193, "E", "Drink water, eat, use equipment");
            GuideRow(guide, 222, "Hold E", "Build a fire or light the beacon");
            GuideRow(guide, 251, "R", "Refill with the equipped fuel cup");
            GuideRow(guide, 280, "Right mouse", "Hold a movable prop");
            GuideRow(guide, 309, "Tab  /  Esc", "Craft or pause");
            Box(guide, "Settings divider", TopLeft, 25, 356, 385, 1, Track);
            Label(guide, "Settings heading", "Settings", 25, 371, 385, 28, 21, Paper, bold: true);
            SliderControl(guide, "Mouse sensitivity", 414, .03f, .25f, game.sensitivity, value => game.sensitivity = value);
            SliderControl(guide, "Audio volume", 479, 0, 1, game.volume, value => game.volume = value);
        }

        private void GuideRow(RectTransform parent, int y, string keys, string action)
        {
            Label(parent, keys, keys, 25, y, 151, 23, 14, Gold, TextAnchor.MiddleLeft, true);
            Label(parent, action, action, 183, y, 227, 23, 14, Paper, TextAnchor.MiddleLeft);
        }

        private void RunPrimaryAction()
        {
            switch (game.Mode)
            {
                case StormMode.Title: game.Begin(); break;
                case StormMode.Paused: game.SetMode(StormMode.Playing); break;
                case StormMode.Crafting: game.CraftTorch(); break;
                case StormMode.Failed: game.Retry(); break;
                case StormMode.Won: game.Restart(); break;
            }
        }

        public void RefreshMode()
        {
            if (menu == null) return;
            hud.gameObject.SetActive(game.Running);
            menu.gameObject.SetActive(!game.Running);
            if (game.Running) return;

            var state = game.Needs;
            primary.interactable = true;
            menuTitle.fontSize = game.Mode == StormMode.Title ? 88 : 72;
            secondary.gameObject.SetActive(game.Mode == StormMode.Crafting ||
                game.Mode == StormMode.Paused || game.Mode == StormMode.Failed);
            secondaryText.text = game.Mode == StormMode.Crafting ? "Back to game" : "Restart game";

            switch (game.Mode)
            {
                case StormMode.Title:
                    menuTitle.text = "The Big\nStorm";
                    menuSummary.text = "Gather supplies, stay warm, and reach the old lookout.\nThe rescue beacon is your way out of the storm.";
                    primaryText.text = "Begin survival";
                    break;
                case StormMode.Paused:
                    menuTitle.text = "Paused";
                    menuSummary.text = "Last checkpoint  ·  " + game.CheckpointName + "\n" + game.Objective;
                    primaryText.text = "Resume game";
                    break;
                case StormMode.Crafting:
                    menuTitle.text = "Crafting";
                    menuSummary.text = state.Wood + " wood   ·   " + state.Stone + " stone   ·   " + state.Cloth + " cloth\n" +
                        (state.HasTorch
                            ? "Rewrap for 1 wood to add up to 2 minutes of fuel. " + TimeText(Mathf.CeilToInt(state.TorchFuel)) + " left."
                            : "Craft for 2 wood and 1 cloth. Hold E at a burning campfire to light your torch.");
                    primaryText.text = state.HasTorch ? "Rewrap torch" : "Craft torch";
                    primary.interactable = state.HasTorch
                        ? state.Wood >= 1 && state.TorchFuel < 239f
                        : state.Wood >= 2 && state.Cloth >= 1;
                    break;
                case StormMode.Failed:
                    menuTitle.text = "Back to camp";
                    menuSummary.text = "Return to " + game.CheckpointName + " with your supplies and opened routes.\nFire restores warmth, and shelter helps you dry.";
                    primaryText.text = "Retry checkpoint";
                    break;
                case StormMode.Won:
                    menuTitle.text = "Beacon lit";
                    menuSummary.text = "Your signal made it through the storm. Rescue is coming.\n" +
                        TimeText((int)game.Elapsed) + " elapsed   ·   " + game.Retries + " retries   ·   " + game.WoodGathered + " wood gathered";
                    primaryText.text = "Play again";
                    break;
            }
            primaryText.color = primary.interactable ? Ink : Muted;
        }

        private void LateUpdate()
        {
            if (game == null || !game.Running) return;
            var state = game.Needs;
            var player = game.Player;
            objective.text = game.Objective;
            supplies.text = "Wood " + state.Wood + "     Stone " + state.Stone + "     Cloth " + state.Cloth;

            meters[0].Set(state.Health / 100f, state.Health < 25);
            meters[1].Set(state.Warmth / 100f, state.Warmth < 20);
            meters[2].Set(state.Stamina / 100f, state.Stamina < 16);
            meters[3].Set(player.Stats.Hunger / 1000f, player.Stats.Hunger < 250);
            meters[4].Set(player.Stats.Thirst / 1000f, player.Stats.Thirst < 250);
            meters[5].Set(state.Wetness / 100f, state.Wetness >= 85);

            int slot = player.Inventory.selected;
            if (displayedSlot != slot)
            {
                displayedSlot = slot;
                for (int i = 0; i < slotBorders.Length; i++)
                {
                    bool selected = i == slot;
                    slotBorders[i].color = selected ? Gold : Border;
                    slotKeys[i].color = selected ? Gold : Track;
                    slotNumbers[i].color = selected ? Ink : Paper;
                }
            }
            torchStatus.text = !state.HasTorch ? "Uncrafted" :
                (state.TorchLit ? "Lit  " : "Unlit  ") + TimeText(Mathf.CeilToInt(state.TorchFuel));
            int fuel = player.FuelCup.HoldingNow;
            fuelStatus.text = fuel == 0 ? "Empty" : fuel + " / 2 fuel";

            Vector3 delta = game.ObjectivePosition - player.transform.position;
            delta.y = 0;
            float angle = Vector3.SignedAngle(player.transform.forward, delta, Vector3.up);
            string bearing = Mathf.Abs(angle) < 20 ? "↑  Ahead" : angle > 0 ? "→  Right" : "←  Left";
            direction.text = bearing + "    ·    " + Mathf.RoundToInt(delta.magnitude) + " m";

            bool hasTarget = !string.IsNullOrEmpty(player.Prompt);
            bool showCrouchHint = !hasTarget && !player.Crouched &&
                StormWorld.ApproachingCrouchBarrier(player.transform.position, player.View.transform.forward);
            reticle.color = hasTarget ? Gold : new Color(Paper.r, Paper.g, Paper.b, .88f);
            reticle.rectTransform.localScale = hasTarget ? Vector3.one * 1.1f : Vector3.one;
            interaction.gameObject.SetActive(hasTarget || showCrouchHint);
            if (hasTarget || showCrouchHint)
            {
                interaction.anchoredPosition = new Vector2(interaction.anchoredPosition.x, showCrouchHint ? -132f : -40f);
                prompt.text = showCrouchHint ? "Hold C to crouch through" : player.Prompt;
                float progress = hasTarget ? player.HoldProgress : 0f;
                holdTrack.gameObject.SetActive(progress > 0);
                holdFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            }

            bool hasNotice = !string.IsNullOrEmpty(game.Notice) && Time.unscaledTime < game.NoticeUntil;
            noticeCard.gameObject.SetActive(hasNotice);
            if (hasNotice) notice.text = game.Notice;
            UpdateWeather();
        }

        private void UpdateWeather()
        {
            weather.color = Gold;
            if (game.Warming)
            {
                weather.text = "BY THE FIRE";
                weatherDetail.text = "Warming up and drying";
            }
            else if (game.Sheltered)
            {
                weather.text = "SHELTERED";
                weatherDetail.text = "Drying out";
            }
            else if (!game.Weather.OnRidge(game.Player.transform.position))
            {
                weather.text = "EXPOSED TO RAIN";
                weatherDetail.text = game.Needs.Warmth < 20 ? "Warmth low. Find a campfire." : "Find cover or light a torch";
                if (game.Needs.Warmth < 20) weather.color = Alert;
            }
            else if (game.Weather.Gusting)
            {
                weather.text = "STRONG GUST";
                weatherDetail.text = "Crouch or use a windbreak";
                weather.color = Alert;
            }
            else
            {
                weather.text = "EXPOSED RIDGE";
                weatherDetail.text = "Next gust in " + Mathf.CeilToInt(game.Weather.UntilGust) + " s";
            }
        }

        private void SliderControl(RectTransform parent, string name, int y, float min, float max, float current,
            UnityAction<float> changed)
        {
            Label(parent, name, name, 25, y, 310, 20, 14, Paper);
            var valueLabel = Label(parent, name + " value", "", 350, y, 60, 20, 13, Muted, TextAnchor.UpperRight);
            var body = Place(parent, name + " slider", TopLeft, 25, y + 25, 385, 20);
            var hitArea = AddImage(body, Color.clear);
            hitArea.raycastTarget = true;
            var rail = Place(body, "Rail", TopLeft, 8, 7, 369, 6);
            AddImage(rail, Track);
            var fill = Fill(rail, "Value", Gold);
            var handles = Place(body, "Handle area", TopLeft, 8, 0, 369, 20);
            var handle = ImageAt(handles, "Handle", TopLeft, -8, 2, 16, 16, Paper, true);
            handle.rectTransform.anchorMin = new Vector2(0, .5f);
            handle.rectTransform.anchorMax = new Vector2(0, .5f);
            handle.rectTransform.pivot = Center;
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.raycastTarget = true;
            var slider = body.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.SetValueWithoutNotify(current);
            valueLabel.text = Mathf.RoundToInt(slider.normalizedValue * 100) + "%";
            slider.onValueChanged.AddListener(value =>
            {
                changed(value);
                valueLabel.text = Mathf.RoundToInt(slider.normalizedValue * 100) + "%";
            });
        }

        private Button MakeButton(RectTransform parent, string name, int x, int y, int w, int h, Color background,
            Color foreground, out Text text)
        {
            var rect = Place(parent, name, TopLeft, x, y, w, h);
            var image = AddImage(rect, Color.white, true);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Paper, .22f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(background, Border, .38f);
            colors.disabledColor = Track;
            button.colors = colors;
            text = Label(rect, "Button label", "", 18, 0, w - 36, h, 17, foreground, TextAnchor.MiddleCenter, true);
            return button;
        }

        private RectTransform CardAt(Transform parent, string name, Vector2 anchor, float x, float y, float w, float h)
        {
            var rect = Place(parent, name, anchor, x, y, w, h);
            AddImage(rect, Border, true);
            AddImage(Place(rect, "Background", TopLeft, 1, 1, w - 2, h - 2), Card, true);
            return rect;
        }

        private Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size,
            Color color, TextAnchor alignment = TextAnchor.UpperLeft, bool bold = false)
        {
            var label = Place(parent, name, TopLeft, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }

        private void Box(Transform parent, string name, Vector2 anchor, float x, float y, float w, float h, Color color)
        {
            ImageAt(parent, name, anchor, x, y, w, h, color);
        }

        private Image ImageAt(Transform parent, string name, Vector2 anchor, float x, float y, float w, float h,
            Color color, bool rounded = false)
        {
            return AddImage(Place(parent, name, anchor, x, y, w, h), color, rounded);
        }

        private Image AddImage(RectTransform rect, Color color, bool rounded = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (rounded)
            {
                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private Image Fill(Transform parent, string name, Color color)
        {
            return AddImage(Layer(parent, name), color);
        }

        private static RectTransform Place(Transform parent, string name, Vector2 anchor, float x, float y, float w, float h)
        {
            var rect = NewRect(parent, name);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = TopLeft;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static RectTransform Layer(Transform parent, string name)
        {
            var rect = NewRect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private void CreateTextures()
        {
            CreateReticleTexture();
            const int size = 32;
            const float radius = 7f;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "HUD corner mask" };
            var roundedPixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x + .5f - size / 2f) - (size / 2f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y + .5f - size / 2f) - (size / 2f - radius));
                    float alpha = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                    roundedPixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            roundedTexture.SetPixels(roundedPixels);
            roundedTexture.Apply(false, true);
            roundedSprite = Sprite.Create(roundedTexture, new Rect(0, 0, size, size), Center, 100,
                0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));

            const int width = 64;
            backdropTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
                { name = "Menu backdrop", wrapMode = TextureWrapMode.Clamp };
            var backdropPixels = new Color[width];
            for (int x = 0; x < width; x++)
            {
                float alpha = Mathf.Lerp(.94f, .35f, Mathf.SmoothStep(0, 1, x / (width - 1f)));
                backdropPixels[x] = new Color(.025f, .05f, .046f, alpha);
            }
            backdropTexture.SetPixels(backdropPixels);
            backdropTexture.Apply(false, true);
        }

        private void CreateReticleTexture()
        {
            const int size = 64;
            reticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Aim reticle mask", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + .5f - size / 2f);
                    float dy = Mathf.Abs(y + .5f - size / 2f);
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Abs(radius - 18.5f) - 1.4f;
                    float horizontal = Mathf.Max(dy - 1.4f, Mathf.Max(21f - dx, dx - 28f));
                    float vertical = Mathf.Max(dx - 1.4f, Mathf.Max(21f - dy, dy - 28f));
                    float mark = Mathf.Min(Mathf.Min(ring, radius - 2.3f), Mathf.Min(horizontal, vertical));
                    byte alpha = (byte)Mathf.RoundToInt(255 * Mathf.Clamp01(.7f - mark));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            reticleTexture.SetPixels32(pixels);
            reticleTexture.Apply(false, true);
        }

        private static string TimeText(int seconds)
        {
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        private void OnDestroy()
        {
            Destroy(roundedSprite);
            Destroy(roundedTexture);
            Destroy(backdropTexture);
            Destroy(reticleTexture);
        }
    }
}
