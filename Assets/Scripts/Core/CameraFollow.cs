using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    void LateUpdate()
    {
        if (target == null) return;
        var p = target.position;
        transform.position = new Vector3(p.x, p.y + 0.5f, -10f);
    }
}