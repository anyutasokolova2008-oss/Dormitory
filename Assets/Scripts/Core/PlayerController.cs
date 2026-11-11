using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float animFps = 8f;
    // 16 кадров: по 4 на каждое направление
    [SerializeField] private Sprite[] frames = new Sprite[16];

    // Номера направлений. Если влево и вправо окажутся перепутаны, поменяй местами 1 и 2.
    private const int Down = 0, Left = 1, Right = 2, Up = 3;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 input;
    private int direction = Down;
    private float animTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        input = ReadInput();

        if (input.x < 0) direction = Left;
        else if (input.x > 0) direction = Right;
        else if (input.y > 0) direction = Up;
        else if (input.y < 0) direction = Down;

        int frame = 0;
        if (input.sqrMagnitude > 0.01f)
        {
            animTimer += Time.deltaTime * animFps;
            frame = (int)animTimer % 4;
        }
        else
        {
            animTimer = 0f;
        }

        int index = direction * 4 + frame;
        if (frames != null && index < frames.Length && frames[index] != null)
            sr.sprite = frames[index];
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
    }

    Vector2 ReadInput()
    {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
        return new Vector2(x, y).normalized;
    }
}