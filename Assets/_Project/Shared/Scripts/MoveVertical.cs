using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveVertical : MonoBehaviour
{
    public bool activateMouvement = false;
    private Vector3 initialPosition;
    public float verticalOffset = 0.3f;
    public float speedReduction = 15f;

    void Start()
    {
        initialPosition = transform.position;
    }

        void Update()
    {
        if (activateMouvement)
        {
            transform.position = new Vector3(initialPosition.x, initialPosition.y + Mathf.PingPong(Time.time / speedReduction, verticalOffset), initialPosition.z);
        }

    }
}
