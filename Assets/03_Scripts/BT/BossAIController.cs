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
        if (blackboard == null || _root == null)
        {
            return;
        }

        blackboard.TickPhaseTimer(Time.deltaTime);
        blackboard.TickAnimationSync();
        blackboard.SetPatternExecutionMode(PatternExecutionMode.Normal);
        _root.Tick(blackboard, Time.deltaTime);
    }
}

