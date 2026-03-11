using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("좌우 이동")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    [Header("레일 설정 (아래, 중간, 위 순서)")]
    [SerializeField] private float[] railY = { -2f, 0f, 2f };
    [SerializeField] private float railChangeSpeed = 8f;

    private int currentRail = 1;
    private bool isChangingRail = false;
    private float targetY;

    void Start()
    {
        targetY = railY[currentRail];
        Vector3 pos = transform.position;
        pos.y = targetY;
        transform.position = pos;
    }

    void Update()
    {
        HandleRailInput();
        HandleHorizontalMove();
        MoveToRail();
    }

    void HandleRailInput()
    {
        if (isChangingRail) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentRail < railY.Length - 1)
            {
                currentRail++;
                targetY = railY[currentRail];
                isChangingRail = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentRail > 0)
            {
                currentRail--;
                targetY = railY[currentRail];
                isChangingRail = true;
            }
        }
    }

    void HandleHorizontalMove()
    {
        float h = Input.GetAxisRaw("Horizontal");

        Vector3 pos = transform.position;
        pos.x += h * moveSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        transform.position = new Vector3(pos.x, transform.position.y, transform.position.z);
    }

    void MoveToRail()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, targetY, railChangeSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Approximately(transform.position.y, targetY))
        {
            isChangingRail = false;
        }
    }
}
