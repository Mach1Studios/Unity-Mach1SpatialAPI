using blumhouse.effect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMoveObject : MonoBehaviour
{
    public GameObject moveObject;
    public float Speed = 1.0f;
    public bool useRandomMovements = false;
    public float randomMovementPeriod = 1.0f;

    private float counter = 0;
    void Update()
    {
        if (useRandomMovements)
        {
            if (counter > randomMovementPeriod)
            {
                // do stuff
                moveObject.transform.position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f));
                moveObject.transform.rotation = Random.rotation;
                counter = 0;
            }
            counter += UnityEngine.Time.deltaTime;
        } else
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                moveObject.transform.Translate(Vector3.left * Time.deltaTime * Speed, Space.Self);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                moveObject.transform.Translate(Vector3.right * Time.deltaTime * Speed, Space.Self);
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                moveObject.transform.Translate(Vector3.forward * Time.deltaTime * Speed, Space.Self);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                moveObject.transform.Translate(Vector3.forward * Time.deltaTime * -Speed, Space.Self);
            }
        }
    }
}
