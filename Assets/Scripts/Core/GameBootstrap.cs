using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameBootstrap : MonoBehaviour
{
    [Header("Ссылки для сцены PlayTest")]
    [SerializeField] private ResourcePanel panel;
    [SerializeField] private EventWindow eventWindow;

    // Можно не указывать в Inspector, найдётся сам по тегу Player.
    [SerializeField] private Transform player;

    [Header("Подсказка взаимодействия")]
    // Можно не заполнять: если поле пустое, подсказка создастся в коде
    // внутри первого Canvas (ничего в сцене настраивать не нужно).
    [SerializeField] private GameObject interactPrompt;

    private TextMeshProUGUI interactPromptText;

    [Header("Пузырь над игроком (арт-пак)")]
    // Если выключить, останется только текстовая подсказка внизу экрана.
    [SerializeField] private bool useWorldPrompt = true;
    [SerializeField] private float worldPromptGap = 0.1f;
    [SerializeField] private float worldPromptScale = 1f;

    [Header("Пул событий сцены")]
    [SerializeField] private List<GameEvent> allEvents = new List<GameEvent>();

    private GameState state;
    private EventManager eventManager;
    private DayCycleManager dayCycle;

    // Активный bootstrap нужен, чтобы другие скрипты (например, двери)
    // могли узнать, есть ли в текущей комнате неразрешённое событие.
    private static GameBootstrap active;

    // Мировая подсказка-пузырь: создаётся в коде и висит над игроком.
    private const int WorldPromptSortingOrder = 500;
    private Transform worldPrompt;
    private SpriteRenderer worldPromptBubble;
    private SpriteRenderer worldPromptIcon;
    private SpriteRenderer playerRenderer;
    private string worldPromptKey;
    private float worldPromptShownAt;
    private Sprite promptBubbleIdle;
    private Sprite promptBubbleHot;
    private Sprite promptDoorIdle;
    private Sprite promptDoorHot;
    private readonly Dictionary<string, Sprite> eventIconCache = new Dictionary<string, Sprite>();

    public static bool IsEventWindowOpen()
    {
        return active != null && active.eventWindow != null && active.eventWindow.IsOpen;
    }

    public static bool HasPendingEventInCurrentRoom()
    {
        if (active == null || active.dayCycle == null) return false;
        if (active.eventWindow != null && active.eventWindow.IsOpen) return false;
        return active.dayCycle.GetPendingEventForRoom(GameSession.CurrentRoomId) != null;
    }

    public static bool IsPlayerInDoorZone()
    {
        return DoorTrigger.HasDoorInRange;
    }

    void Awake()
    {
        active = this;

        // Если подсказка назначена в Inspector — по умолчанию она скрыта.
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void OnDestroy()
    {
        if (active == this)
            active = null;
    }

    void Start()
    {
        ResolvePlayer();
        EnsureInteractPrompt();
        EnsureWorldPrompt();

        // Если меню запросило продолжение сохранённой игры.
        // Если файла нет или он не смог прочитаться — начинаем новую игру.
        if (GameSession.ConsumeContinueRequest())
        {
            if (LoadGame()) return;
            Debug.LogWarning("[GameBootstrap] Сохранение не найдено — начинаем новую игру.");
        }

        StartNewGame();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f5Key.wasPressedThisFrame)
            SaveNow(true);

        // Быстрый отладочный просмотр: F6 прокручивает время на 1 час.
        if (keyboard.f6Key.wasPressedThisFrame)
        {
            float nextHour = GameClock.Hour + 1f;
            if (nextHour >= 24f) nextHour = GameClock.DayStartHour;
            GameClock.SetHour(nextHour);
            Debug.Log($"[GameClock] Отладочное время: {GameClock.Hour:00}:00");
        }

        // Единая обработка E: дверь -> переход, событие в комнате -> окно события,
        // иначе — ничего. Вся логика собрана в TryInteract().
        if (keyboard.eKey.wasPressedThisFrame)
            TryInteract();

        UpdateInteractPrompt();
        UpdateWorldPrompt();
    }

    // Автосохранение при выходе из игры.
    void OnApplicationQuit()
    {
        SaveNow(false);
    }

    void ResolvePlayer()
    {
        if (player != null) return;

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
        else Debug.LogWarning("[GameBootstrap] Объект с тегом Player не найден — игрок не будет сохранён.");
    }

    // Точка появления коменданта.
    // Координаты заданы пользователем из сцены PlayTest.
    static readonly Vector3 OfficeSpawnPosition = new Vector3(-3.8553f, 2.02f, 0f);

    void TeleportPlayer(Vector3 position, string roomId)
    {
        ResolvePlayer();
        if (player == null) return;

        var p = new Vector3(position.x, position.y, player.position.z);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = new Vector2(p.x, p.y);
        }
        player.position = p;

        if (!string.IsNullOrEmpty(roomId))
            GameSession.SetCurrentRoom(roomId);
    }

    void TeleportPlayerToOffice()
    {
        TeleportPlayer(OfficeSpawnPosition, "office1");
    }

    // Создаёт/настраивает служебные переходы между этажами.
    // На 1 этаже в сцене не было двери обратно на 2 этаж — из-за этого
    // игрок «застревал» на 1 этаже. Дверь создаём в коде, чтобы не править
    // scene YAML вручную.
    void EnsureFloorTransitions()
    {
        var doorToFloor1 = FindDoorByName("Door_To_Floor1");
        if (doorToFloor1 == null) return;

        // Точка появления на 2 этаже (рядом с дверью вниз, но в проходимом месте).
        var spawnFloor2 = GetOrCreateSpawn("Spawn_From_Floor1", new Vector3(-1.2f, 1.0f, 0f));

        // Дверь/триггер на 1 этаже для подъёма на 2 этаж.
        // Ставим рядом со Spawn_To_Floor, где игрок появляется на 1 этаже.
        var spawnFloor1 = GameObject.Find("Spawn_To_Floor");
        Vector3 floor1DoorPos = spawnFloor1 != null
            ? spawnFloor1.transform.position + new Vector3(0f, -0.8f, 0f)
            : new Vector3(-1.2f, -32.75f, 0f);

        var doorToFloor2 = FindDoorByName("Door_To_Floor2");
        if (doorToFloor2 == null)
        {
            var go = new GameObject("Door_To_Floor2");
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.6f, 0.8f);
            doorToFloor2 = go.AddComponent<DoorTrigger>();
            go.transform.position = floor1DoorPos;
        }
        doorToFloor2.Configure(spawnFloor2, "Второй Этаж");

        // На всякий случай убеждаемся, что дверь вниз ведёт в понятную комнату.
        // (destinationName "Первый Этаж" обрабатывается в DoorTrigger.ResolveRoomId)
    }

    static DoorTrigger FindDoorByName(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<DoorTrigger>() : null;
    }

    static Transform GetOrCreateSpawn(string name, Vector3 position)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        go.transform.position = position;
        return go.transform;
    }

    // Создаёт подсказку «E — разобраться», если она не назначена в Inspector.
    // Так фича работает без ручной сборки UI в сцене.
    void EnsureInteractPrompt()
    {
        if (interactPrompt != null)
        {
            interactPromptText = interactPrompt.GetComponent<TextMeshProUGUI>();
            interactPrompt.SetActive(false);
            return;
        }

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GameBootstrap] Canvas не найден — подсказка E не будет создана.");
            return;
        }

        var go = new GameObject("InteractPrompt", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.layer = canvas.gameObject.layer;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = "E — разобраться";
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.95f, 0.82f, 1f);
        text.outlineColor = new Color(0.16f, 0.09f, 0.05f, 1f);
        text.outlineWidth = 0.2f;
        text.raycastTarget = false;
        interactPromptText = text;

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 80f);
        rt.sizeDelta = new Vector2(700f, 70f);

        if (text.font == null)
            Debug.LogWarning("[GameBootstrap] У подсказки нет шрифта TMP — проверь TMP Essential Resources.");

        interactPrompt = go;
        go.SetActive(false);
    }

    // Показывает подсказку для E: у двери — переход, в комнате с событием —
    // «разобраться». Если взаимодействовать не с чем, подсказка скрыта.
    void UpdateInteractPrompt()
    {
        if (interactPrompt == null) return;
        if (interactPromptText == null)
            interactPromptText = interactPrompt.GetComponent<TextMeshProUGUI>();
        if (interactPromptText == null) return;

        string prompt = null;

        if (state != null && dayCycle != null && eventWindow != null
            && !dayCycle.IsOver && !eventWindow.IsOpen)
        {
            if (IsPlayerInDoorZone())
                prompt = DoorTrigger.GetPromptText();
            else if (dayCycle.GetPendingEventForRoom(GameSession.CurrentRoomId) != null)
                prompt = "E — разобраться";
        }

        bool show = !string.IsNullOrEmpty(prompt);
        if (show && interactPromptText.text != prompt)
            interactPromptText.text = prompt;

        if (interactPrompt.activeSelf != show)
            interactPrompt.SetActive(show);
    }

    // ------------------------------------------------------------------
    // Пузырь над игроком (world-space)
    // ------------------------------------------------------------------

    // Создаёт пузырь в коде и вешает его над игроком.
    // Ничего в сцене собирать не нужно: спрайты берутся из Assets/Resources/UI.
    void EnsureWorldPrompt()
    {
        if (!useWorldPrompt || player == null) return;
        if (worldPrompt != null) return;

        promptBubbleIdle = Resources.Load<Sprite>("UI/HUD/bubble_exclam");
        promptBubbleHot = Resources.Load<Sprite>("UI/HUD/bubble_exclam_selected");
        promptDoorIdle = Resources.Load<Sprite>("UI/HUD/bubble_door");
        promptDoorHot = Resources.Load<Sprite>("UI/HUD/bubble_door_selected");

        if (promptBubbleIdle == null || promptBubbleHot == null
            || promptDoorIdle == null || promptDoorHot == null)
        {
            Debug.LogWarning("[GameBootstrap] Не найдены PNG в Assets/Resources/UI/HUD — пузырь над игроком отключён.");
            return;
        }

        var root = new GameObject("InteractBubble");
        root.transform.SetParent(player, false);
        root.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        worldPrompt = root.transform;

        playerRenderer = player.GetComponent<SpriteRenderer>();

        var bubble = new GameObject("Bubble");
        bubble.transform.SetParent(root.transform, false);
        bubble.transform.localScale = Vector3.one * worldPromptScale;
        worldPromptBubble = bubble.AddComponent<SpriteRenderer>();
        worldPromptBubble.sortingOrder = WorldPromptSortingOrder;

        var icon = new GameObject("EventIcon");
        icon.transform.SetParent(bubble.transform, false);
        icon.transform.localScale = Vector3.one * 0.8f;
        icon.transform.localPosition = new Vector3(0f, 0.09f, 0f);
        worldPromptIcon = icon.AddComponent<SpriteRenderer>();
        worldPromptIcon.sortingOrder = WorldPromptSortingOrder + 1;
        worldPromptIcon.enabled = false;

        root.SetActive(false);
    }

    // Показывает пузырь над игроком: у двери — иконку двери, в комнате с
    // событием — иконку этого события. Через 0.25 сек пузырь «загорается»
    // (оранжевый highlighted) и мягко пульсирует.
    void UpdateWorldPrompt()
    {
        if (worldPrompt == null) return;

        Sprite idle = null;
        Sprite hot = null;
        Sprite iconSprite = null;
        string key = null;

        bool canPrompt = state != null && dayCycle != null && eventWindow != null
            && !dayCycle.IsOver && !eventWindow.IsOpen;

        if (canPrompt)
        {
            if (IsPlayerInDoorZone())
            {
                if (DoorTrigger.HasUsableDoorInRange)
                {
                    idle = promptDoorIdle;
                    hot = promptDoorHot;
                    key = "door";
                }
            }
            else
            {
                var de = dayCycle.GetPendingEventForRoom(GameSession.CurrentRoomId);
                if (de != null && de.ev != null)
                {
                    idle = promptBubbleIdle;
                    hot = promptBubbleHot;
                    iconSprite = GetEventIcon(de.ev);

                    string eventKey = string.IsNullOrEmpty(de.ev.eventId) ? de.ev.title : de.ev.eventId;
                    key = $"event:{GameSession.CurrentRoomId}:{eventKey}";
                }
            }
        }

        bool show = key != null;
        if (worldPrompt.gameObject.activeSelf != show)
            worldPrompt.gameObject.SetActive(show);

        if (!show)
        {
            worldPromptKey = null;
            return;
        }

        // Новое событие/дверь — снова показываем обычный пузырь,
        // затем он подсвечивается.
        if (worldPromptKey != key)
        {
            worldPromptKey = key;
            worldPromptShownAt = Time.time;
        }

        bool highlighted = Time.time - worldPromptShownAt >= 0.25f;
        worldPromptBubble.sprite = highlighted ? hot : idle;

        worldPromptIcon.sprite = iconSprite;
        worldPromptIcon.enabled = iconSprite != null;

        float time = Time.time;
        float bob = Mathf.Sin(time * 3f) * 0.04f;

        float pulse = highlighted ? 1f + Mathf.Sin(time * 6f) * 0.05f : 1f;
        worldPromptBubble.transform.localScale = Vector3.one * (worldPromptScale * pulse);

        // Ставим пузырь строго над верхом спрайта персонажа, чтобы он
        // не перекрывал самого коменданта.
        float playerTop = player.position.y + 1f;
        if (playerRenderer != null && playerRenderer.sprite != null)
            playerTop = playerRenderer.bounds.max.y;

        float bubbleHalfHeight = worldPromptBubble.sprite != null
            ? worldPromptBubble.sprite.bounds.extents.y * worldPromptScale * pulse
            : 0.41f;

        worldPrompt.position = new Vector3(
            player.position.x,
            playerTop + worldPromptGap + bubbleHalfHeight + bob,
            player.position.z);
    }

    Sprite GetEventIcon(GameEvent ev)
    {
        string iconName = IconNameForEvent(ev != null ? ev.eventId : null);
        if (iconName == null) return null;

        if (eventIconCache.TryGetValue(iconName, out var cached)) return cached;

        var sprite = Resources.Load<Sprite>("UI/EventIcons/" + iconName);
        eventIconCache[iconName] = sprite;
        return sprite;
    }

    // Сопоставляет eventId с иконкой из UI_EventIcons_16.
    // Если ничего не подошло, останется обычный пузырь с «!».
    static string IconNameForEvent(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return null;
        string id = eventId.ToLowerInvariant();

        if (id.Contains("fire")) return "event_fire";
        if (id.Contains("leak") || id.Contains("flood") || id.Contains("water") || id.Contains("outage"))
            return "event_leak";
        if (id.Contains("inspection") || id.Contains("sanitary")) return "event_inspection";
        if (id.Contains("party") || id.Contains("noise")) return "event_party_noise";
        if (id.Contains("conflict") || id.Contains("quarrel") || id.Contains("stranger")) return "event_conflict";
        if (id.Contains("repair") || id.Contains("broken") || id.Contains("dirty")) return "event_repair";
        if (id.Contains("resident") || id.Contains("room_change") || id.Contains("volunteer") || id.Contains("sick"))
            return "event_new_resident";
        if (id.Contains("blackout") || id.Contains("payment") || id.Contains("light"))
            return "event_blackout";
        return null;
    }

    // ------------------------------------------------------------------
    // Новая игра
    // ------------------------------------------------------------------
    void StartNewGame()
    {
        state = GameState.CreateNew();
        GameSession.SetCurrent(state);

        var rng = new System.Random();
        eventManager = new EventManager(rng);
        dayCycle = new DayCycleManager(state, eventManager, allEvents, rng);

        if (panel != null) panel.Bind(state.resources);

        EnsureFloorTransitions();

        // Начальная точка: воспитательская (office1), 1 этаж.
        TeleportPlayerToOffice();

        // Освещение: глобальный свет по времени суток и фонарик у игрока.
        LightingController.Ensure(player);

        Debug.Log("[GameBootstrap] Новая игра начата.");
        StartNextDay();
    }

    // ------------------------------------------------------------------
    // Загрузка сохранения
    // ------------------------------------------------------------------
    bool LoadGame()
    {
        SaveData data;
        if (!SaveManager.TryLoad(out data)) return false;

        state = data.ToState();
        GameSession.SetCurrent(state);

        var rng = new System.Random();
        eventManager = new EventManager(rng);
        dayCycle = new DayCycleManager(state, eventManager, allEvents, rng);

        // Восстанавливаем оставшиеся события этого дня.
        dayCycle.RestoreToday(data.BuildQueue(allEvents, state));

        if (panel != null) panel.Bind(state.resources);
        RestorePlayer(data);
        EnsureFloorTransitions();

        // Восстанавливаем игровое время из сохранения.
        GameClock.SetHour(data.hour);

        // Восстанавливаем комнату по сохранённой позиции (точнее, чем по этажу).
        var pos = new Vector2(data.playerX, data.playerY);
        var loadedRoom = ResolveRoomAtPosition(pos, data.floor);
        GameSession.SetCurrentRoom(loadedRoom != null
            ? loadedRoom.id
            : (state.rooms != null && state.rooms.Count > 0 ? state.rooms[0].id : null));

        LightingController.Ensure(player);

        // Окно не должно оставаться открытым при загрузке.
        if (eventWindow != null) eventWindow.Hide();

        Debug.Log($"[GameBootstrap] Игра загружена: день {state.day}, {GameClock.Hour:00}:00, комната {GameSession.CurrentRoomId}.");

        if (!dayCycle.HasPendingEvents)
            FinishDay();

        return true;
    }

    void RestorePlayer(SaveData data)
    {
        if (player == null || data == null || !data.hasPlayerPosition) return;

        var position = new Vector2(data.playerX, data.playerY);
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = position;
        }

        // Ставим и Transform: если Rigidbody2D ещё не обновил позицию,
        // игрок всё равно окажется в сохранённой точке.
        player.position = new Vector3(position.x, position.y, player.position.z);
    }

    // ------------------------------------------------------------------
    // Цикл по событиям (вариант B: по E в нужной комнате)
    // ------------------------------------------------------------------
    void StartNextDay()
    {
        dayCycle.StartDay();

        // Закрываем окно перед автосохранением начала дня.
        if (eventWindow != null) eventWindow.Hide();

        // Сохраняемся в начале дня: позиции, события и ресурсы соответствуют
        // началу нового дня.
        SaveNow(false);

        var pending = dayCycle.SnapshotToday();
        Debug.Log($"[GameBootstrap] День {state.day}: событий сегодня — {pending.Count}. Подойди в нужное место и нажми E, чтобы разобраться.");
        for (int i = 0; i < pending.Count; i++)
        {
            var e = pending[i];
            if (e == null || e.ev == null) continue;
            string place = e.room != null ? $"{e.room.name} ({e.room.id})" : "неизвестное место";
            Debug.Log($"[GameBootstrap] Событие {i + 1}: \"{e.ev.title}\" — {place}");
        }

        if (!dayCycle.HasPendingEvents)
            FinishDay();
    }

    // Единая точка входа для E.
    // Приоритет: дверь -> переход; иначе событие текущей комнаты; иначе ничего.
    void TryInteract()
    {
        if (state == null || dayCycle == null || eventWindow == null) return;
        if (eventWindow.IsOpen) return;
        if (dayCycle.IsOver) return;

        // 1) Дверь. Находясь в её зоне, E всегда означает переход,
        //    и событие комнаты в этот момент не открывается.
        if (IsPlayerInDoorZone())
        {
            DoorTrigger.TryUseNearestDoor();
            return;
        }

        // 2) Событие, привязанное к текущей комнате.
        var de = dayCycle.GetPendingEventForRoom(GameSession.CurrentRoomId);
        if (de == null || de.ev == null)
        {
            Debug.Log($"[GameBootstrap] E: в текущем месте ({GameSession.CurrentRoomId ?? "?"}) событий нет.");
            return;
        }

        ShowRoomEvent(de);
    }

    void ShowRoomEvent(DayEvent de)
    {
        string context = $"День {state.day} из {DayCycleManager.LastDay}";
        if (de.student != null) context += $"\nЖитель: {de.student.name}";
        if (de.room != null) context += $"\nМесто: {de.room.name}";

        eventWindow.Show(de.ev, state, index =>
        {
            eventManager.TryApplyChoice(state, de.ev, index, de.student, de.room);
            dayCycle.RemovePendingEvent(de);

            if (state.resources.IsDefeated() || dayCycle.IsOver)
                FinishDay();
            else if (!dayCycle.HasPendingEvents)
                FinishDay();
            // Иначе ждём следующего: игрок идёт к следующему месту с событием
        }, context);
    }

    Room ResolveRoomAtPosition(Vector2 position, int fallbackFloor)
    {
        if (state == null || state.rooms == null) return null;

        int floor = fallbackFloor;
        if (floor <= 0) floor = GameSession.FloorFromPosition(position);

        // Простая привязка по зоне 1 этажа; остальные позиции считаем коридором этажа.
        if (floor <= 1)
        {
            if (position.x >= -7f && position.x <= 4f && position.y >= -44f && position.y <= -35f)
                return state.GetRoom("office1");
            if (position.x >= -24f && position.x <= -12f && position.y >= -44f && position.y <= -34f)
                return state.GetRoom("guardpost1");
            if (position.x >= 9f && position.x <= 21f && position.y >= -44f && position.y <= -34f)
                return state.GetRoom("med1");
            if (position.x >= -12f && position.x <= 9f && position.y >= -44f && position.y <= -34f)
                return state.GetRoom("corridor1");
            return state.rooms.Find(r => r != null && r.floor == 1 && r.type == RoomType.Corridor)
                ?? state.GetRoom("corridor1");
        }

        return state.rooms.Find(r => r != null && r.floor == floor && r.type == RoomType.Corridor)
            ?? state.GetRoom("corridor2");
    }

    void FinishDay()
    {
        int dayNumber = state.day;
        string report = dayCycle.EndDay();

        if (dayCycle.IsOver)
        {
            float rating = dayCycle.CalculateRating();
            string ending = dayCycle.Defeated ? "Поражение" : DayCycleManager.GetEnding(rating);
            ShowInfo($"Окончание: {ending}",
                     report + $"\nИтоговый рейтинг: {rating:0} из 100",
                     "Начать заново", StartNewGame);
        }
        else
        {
            ShowInfo($"Конец дня {dayNumber}", report, "Следующий день", () =>
            {
                TeleportPlayerToOffice();
                StartNextDay();
            });
        }
    }

    // Показывает отчёт: заголовок, текст, кнопка действия
    void ShowInfo(string title, string text, string buttonText, System.Action onClose)
    {
        var info = ScriptableObject.CreateInstance<GameEvent>();
        info.title = title;
        info.description = text;
        info.choices.Add(new Choice { text = buttonText });
        eventWindow.Show(info, state, _ => onClose());
    }

    // ------------------------------------------------------------------
    // Сохранение
    // ------------------------------------------------------------------
    void SaveNow(bool manual)
    {
        if (state == null || dayCycle == null) return;

        // Точное сохранение, если открыто окно события или отчёта:
        // состояние могло бы сохраниться неполным.
        if (eventWindow != null && eventWindow.IsOpen)
        {
            if (manual) Debug.Log("[GameBootstrap] Точное сохранение в момент открытого окна запрещено.");
            return;
        }

        bool hasPlayer = player != null;
        Vector2 position = hasPlayer ? (Vector2)player.position : Vector2.zero;
        int floor = hasPlayer ? GameSession.FloorFromPosition(position) : 2;

        var data = SaveData.FromGame(state, dayCycle.SnapshotToday(), position, hasPlayer, floor, GameClock.Hour);
        bool saved = SaveManager.Save(data);

        if (manual)
            Debug.Log(saved ? "[GameBootstrap] Ручное сохранение (F5) выполнено." : "[GameBootstrap] Сохранение не удалось.");
    }
}
