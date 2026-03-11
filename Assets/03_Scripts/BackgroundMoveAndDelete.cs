using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMoveAndDelete : MonoBehaviour
{

    [SerializeField] float speed = 1f;       // 초당 이동 속도
    [SerializeField] float destroyX = -10f;  // 이 x좌표에 도달하면 삭제
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (transform.position.x <= destroyX)
        {
            Destroy(gameObject);
        }
    }
}
