using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BT;

public class BossAIController : MonoBehaviour
{
    [SerializeField] private BossBlackboard blackboard;

    private BTNode<BossBlackboard> _root;

    private void Awake()
    {
        _root = BossBehaviorTreeFactory.CreateTestTree();
    }

    private void Update()
    {
        _root.Tick(blackboard, Time.deltaTime);
    }
}
