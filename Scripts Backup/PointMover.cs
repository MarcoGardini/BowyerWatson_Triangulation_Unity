using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointMover : MonoBehaviour
{
    public float   speed    = 5.0f;
    public Vector2 direction;
    public int changeDirection;

    public float maxDistanceFromSpawn;

    void Start()
    {
        direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        float scaleAlpha = Random.Range(0.0f, 0.5f);
        transform.localScale *= 0.5f + scaleAlpha;
        GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f, 0.6f - scaleAlpha) ;
    }
        
    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
        if (Random.Range(0, changeDirection) == 0)
            direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        if (transform.position.magnitude >= maxDistanceFromSpawn)
            direction = -direction;
    }
}
