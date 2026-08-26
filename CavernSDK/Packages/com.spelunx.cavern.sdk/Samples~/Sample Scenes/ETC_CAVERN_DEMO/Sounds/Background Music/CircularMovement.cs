using UnityEngine;
using UnityEngine.Audio;

public class CircularMovement : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private Vector3 centerPosition = Vector3.zero;

    private float angle = 0f;

    private void Update()
    {
        MoveInCircularMotion();
    }

    private void MoveInCircularMotion()
    {
        angle += speed * Time.deltaTime;

        float x = centerPosition.x + radius * Mathf.Cos(angle);
        float z = centerPosition.z + radius * Mathf.Sin(angle);

        transform.position = new Vector3(x, transform.position.y, z);
    }
}
