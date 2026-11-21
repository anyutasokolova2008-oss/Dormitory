using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameBootstrap : MonoBehaviour
{
    [Header("Ссылки для сцены PlayTest")]
    [SerializeField] private ResourcePanel panel;
    [SerializeField] private EventWindow eventWindow;

    // Можно не указывать в Inspector, найдётся сам по тегу Player.
    [SerializeField] private Transform player;

    [Header("Пул событий сцены")]
    [SerializeField] private List<GameEvent> allEvents = new List<GameEvent>();

    private GameState state;
    private EventManager eventManager;
    private DayCycleManager dayCycle;

    // Активный bootstrap нужен, чтобы другие скрипты (например, двери)
    // могли узнать, есть ли в текущей комнате неразрешённое событие.
    private static GameBootstrap active;
    private static int doorZoneCount;
    private static int suppressRoomEventFrame = -1;

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

    public static void SetPlayerInDoorZone(bool inside)
    {
        doorZoneCount += inside ? 1 : -1;
        if (doorZoneCount < 0) doorZoneCount = 0;
    }

    public static bool IsPlayerInDoorZone()
    {
        return doorZoneCount > 0;
    }

    // Подавляет открытие события комнаты в текущем кадре (например, когда E
    // уже использовано для перехода через дверь).
    public static void SuppressRoomEventInteractionThisFrame()
    {
        suppressRoomEventFrame = Time.frameCount;
    }

    void Awake()
    {
        active = this;
        doorZoneCount = 0;
    }

    void OnDestroy()
    {
        if (active == this)
        {
            active = null;
            doorZoneCount = 0;
        }
    }

    void Start()
    {
        ResolvePlayer();

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

        // Вариант B: события привязаны к месту и открываются по E в нужной комнате.
        // Если игрок стоит в зоне двери, DoorTrigger не даёт здесь открыть событие.
        if (keyboard.eKey.wasPressedThisFrame)
            TryInteractWithRoomEvent();
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

        // Начальная комната: при старте игры персонаж находится в коридоре 2 этажа
        // (жилые комнаты и общие зоны распределены по 2 этажу).
        var startRoom = state.GetRoom("corridor2") ?? (state.rooms != null && state.rooms.Count > 0 ? state.rooms[0] : null);
        if (startRoom != null)
            GameSession.SetCurrentRoom(startRoom.id);

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

        // Восстанавливаем игровое время из сохранения.
        GameClock.SetHour(data.hour);

        // Начальная комната по сохранённому этажу (грубо).
        var loadedRoom = state.rooms != null ? state.rooms.Find(r => r != null && r.floor == data.floor) : null;
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

    void TryInteractWithRoomEvent()
    {
        if (state == null || dayCycle == null || eventWindow == null) return;
        if (eventWindow.IsOpen) return;
        if (dayCycle.IsOver) return;

        // Находясь в зоне двери, E относится к переходу, а не к событию в комнате.
        // (Событие не должно появляться при выходе/у двери.)
        if (IsPlayerInDoorZone()) return;
        if (Time.frameCount == suppressRoomEventFrame) return;

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
            ShowInfo($"Конец дня {dayNumber}", report, "Следующий день", StartNextDay);
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
