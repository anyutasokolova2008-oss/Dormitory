using UnityEngine;

// Мостик между главным меню и игровой сценой.
// Главное меню не может передать GameState напрямую (сцена перезагружается),
// поэтому оно оставляет "заявку": новая игра или продолжение.
// После загрузки PlayTest заявку забирает GameBootstrap.
public static class GameSession
{
    // Текущее состояние игры. Пока используется загрузкой/сохранением,
    // позже к нему будут обращаться свет, события, взаимодействия.
    public static GameState Current { get; private set; }

    private static bool continueRequested;

    // При входе в Play mode статические переменные нужно обнулить,
    // иначе данные прошлого запуска могут попасть в новый.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayMode()
    {
        Current = null;
        continueRequested = false;
        CurrentRoomId = null;
    }

    public static void RequestNewGame()
    {
        continueRequested = false;
        Current = null;
        CurrentRoomId = null;
    }

    public static void RequestContinue()
    {
        continueRequested = true;
    }

    // Возвращает true, если меню попросило продолжить сохранённую игру,
    // и сразу сбрасывает заявку, чтобы она не сработала второй раз.
    public static bool ConsumeContinueRequest()
    {
        bool result = continueRequested;
        continueRequested = false;
        return result;
    }

    public static void SetCurrent(GameState state)
    {
        Current = state;
    }

    // Текущая комната, в которой находится комендант.
    // Ставится компонентами RoomView (вход в помещение) и DoorTrigger (телепорт).
    public static string CurrentRoomId { get; private set; }

    public static void SetCurrentRoom(string roomId)
    {
        CurrentRoomId = roomId;
    }

    public static void ClearCurrentRoom()
    {
        CurrentRoomId = null;
    }

    // Этажи сейчас находятся в одной сцене и разнесены по высоте:
    // 2 этаж около y = 0, 1 этаж около y = -36.
    public static int FloorFromPosition(Vector2 position)
    {
        return position.y >= -18f ? 2 : 1;
    }
}
