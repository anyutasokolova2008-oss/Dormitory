using UnityEngine;
using UnityEngine.Rendering.Universal;

// Управляет освещением всей сцены в зависимости от игровых часов.
// Компонент сам находит Global Light 2D и создаёт локальный свет
// вокруг коменданта — в сцене ничего вручную добавлять не нужно.
[DefaultExecutionOrder(50)]
public class LightingController : MonoBehaviour
{
    [Header("Глобальный свет")]
    [SerializeField] private Light2D globalLight;

    [Header("Плавность перехода")]
    [Tooltip("Сколько игровых часов освещение проходит за одну реальную секунду.")]
    [SerializeField] private float transitionHoursPerSecond = 3f;

    [Header("Свет вокруг коменданта")]
    [SerializeField] private bool createPlayerLight = true;
    [SerializeField] private Color playerLightColor = new Color(1f, 0.93f, 0.78f, 1f);
    [SerializeField] private float playerLightOuterRadius = 3.5f;
    [SerializeField] private float playerLightInnerRadius = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float playerLightFalloff = 0.6f;
    [SerializeField] private float playerLightMaxIntensity = 0.75f;

    private Light2D playerLight;
    private Transform player;
    private float shownHour = GameClock.DayStartHour;
    private bool initialized;

    // Ключевые кадры суток: время, цвет, яркость глобального света,
    // яркость локального света вокруг персонажа.
    private struct LightKey
    {
        public float hour;
        public Color color;
        public float intensity;
        public float playerIntensity;

        public LightKey(float hour, Color color, float intensity, float playerIntensity)
        {
            this.hour = hour;
            this.color = color;
            this.intensity = intensity;
            this.playerIntensity = playerIntensity;
        }
    }

    private static readonly LightKey[] Keys = new LightKey[]
    {
        // Ночь (самая тёмная точка)
        new LightKey(0f,  new Color(0.18f, 0.22f, 0.42f), 0.20f, 1.00f),
        new LightKey(5f,  new Color(0.24f, 0.28f, 0.48f), 0.24f, 1.00f),
        // Рассвет / утро
        new LightKey(7f,  new Color(0.88f, 0.80f, 0.70f), 0.72f, 0.20f),
        new LightKey(9f,  new Color(1.00f, 0.96f, 0.88f), 0.94f, 0.00f),
        // День
        new LightKey(12f, new Color(1.00f, 1.00f, 1.00f), 1.00f, 0.00f),
        new LightKey(16f, new Color(1.00f, 0.98f, 0.94f), 1.00f, 0.00f),
        // Вечер
        new LightKey(18f, new Color(1.00f, 0.78f, 0.56f), 0.80f, 0.45f),
        new LightKey(20f, new Color(0.95f, 0.63f, 0.44f), 0.62f, 0.75f),
        // Ночь
        new LightKey(22f, new Color(0.28f, 0.33f, 0.58f), 0.34f, 1.00f),
        new LightKey(24f, new Color(0.18f, 0.22f, 0.42f), 0.20f, 1.00f)
    };

    // Создаёт (или находит) контроллер освещения для текущей сцены.
    // Вызывается из GameBootstrap — в Hierarchy ничего добавлять не нужно.
    public static LightingController Ensure(Transform playerTransform)
    {
        var existing = Object.FindAnyObjectByType<LightingController>();
        if (existing != null)
        {
            existing.Initialize(playerTransform);
            return existing;
        }

        var global = FindGlobalLight();
        if (global == null)
        {
            Debug.LogWarning("[Lighting] В сцене нет Global Light 2D — освещение не настроено.");
            return null;
        }

        var controller = global.gameObject.AddComponent<LightingController>();
        controller.Initialize(playerTransform);
        return controller;
    }

    public static Light2D FindGlobalLight()
    {
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include);
        foreach (var light in lights)
        {
            if (light != null && light.lightType == Light2D.LightType.Global)
                return light;
        }
        return null;
    }

    public void Initialize(Transform playerTransform)
    {
        if (playerTransform != null) player = playerTransform;

        if (globalLight == null) globalLight = FindGlobalLight();
        if (globalLight == null)
        {
            Debug.LogWarning("[Lighting] Global Light 2D не найден.");
            enabled = false;
            return;
        }

        EnsurePlayerLight();

        // При загрузке/старте ставим свет сразу, без анимации.
        shownHour = GameClock.Hour;
        Apply(shownHour);

        initialized = true;
    }

    void Update()
    {
        if (!initialized || globalLight == null) return;

        // Плавно подтягиваем освещение к текущему игровому часу.
        float target = GameClock.Hour;
        if (Mathf.Approximately(shownHour, target))
        {
            shownHour = target;
        }
        else
        {
            shownHour = Mathf.MoveTowards(shownHour, target, transitionHoursPerSecond * Time.deltaTime);
        }

        Apply(shownHour);
    }

    void Apply(float hour)
    {
        LightKey a = Keys[0];
        LightKey b = Keys[Keys.Length - 1];

        for (int i = 0; i < Keys.Length - 1; i++)
        {
            if (hour >= Keys[i].hour && hour <= Keys[i + 1].hour)
            {
                a = Keys[i];
                b = Keys[i + 1];
                break;
            }
        }

        float t = b.hour > a.hour ? Mathf.InverseLerp(a.hour, b.hour, hour) : 0f;

        globalLight.color = Color.Lerp(a.color, b.color, t);
        globalLight.intensity = Mathf.Lerp(a.intensity, b.intensity, t);

        if (playerLight != null)
            playerLight.intensity = Mathf.Lerp(a.playerIntensity, b.playerIntensity, t) * playerLightMaxIntensity;
    }

    void EnsurePlayerLight()
    {
        if (!createPlayerLight || player == null) return;

        if (playerLight == null)
        {
            var existing = player.Find("KomendantLight");
            if (existing != null) playerLight = existing.GetComponent<Light2D>();
        }

        if (playerLight == null)
        {
            var go = new GameObject("KomendantLight");
            go.transform.SetParent(player, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            playerLight = go.AddComponent<Light2D>();
            playerLight.lightType = Light2D.LightType.Point;
            // Additive — локальный свет добавляет яркость, а не затемняет.
            playerLight.blendStyleIndex = 1;
            playerLight.shadowsEnabled = false;

            // Свет должен действовать на те же Sorting Layers, что и глобальный.
            var globalLayers = globalLight != null ? globalLight.targetSortingLayers : null;
            if (globalLayers != null && globalLayers.Length > 0)
                playerLight.targetSortingLayers = (int[])globalLayers.Clone();
        }

        playerLight.color = playerLightColor;
        playerLight.pointLightInnerRadius = playerLightInnerRadius;
        playerLight.pointLightOuterRadius = playerLightOuterRadius;
        playerLight.falloffIntensity = playerLightFalloff;
    }
}
