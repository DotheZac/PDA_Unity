using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BT
{
    public static class BossBehaviorTreeFactory
    {
        public static BTNode<BossBlackboard> CreateTestTree()
        {
            //var phase2 = new SequenceNode<BossBlackboard>("Phase2")
            //    .AddChild(new ConditionNode<BossBlackboard>(
            //        "IsPhase2",
            //        bb =>
            //        {
            //            if (bb.IsPhase2)
            //                Debug.Log("PHASE 2");

            //            return bb.IsPhase2;
            //        }
            //    ))
            //    .AddChild(new WaitNode<BossBlackboard>("Wait", 1f));

            //var phase1 = new SequenceNode<BossBlackboard>("Phase1")
            //    .AddChild(new ConditionNode<BossBlackboard>(
            //        "IsPhase1",
            //        bb => !bb.IsPhase2
            //    ))
            //    .AddChild(new WaitNode<BossBlackboard>("Wait", 1f));

            //return new SelectorNode<BossBlackboard>("Root")
            //    .AddChild(phase2)
            //    .AddChild(phase1);

            // C++ 쪽 패턴 중 Parallel + Wait + Skill 구조를 그대로 축소해서 옮긴 시작 템플릿.
            var openWithRow1 = new SequenceNode<BossBlackboard>("Open_With_Row1")
                .AddChild(new ConditionNode<BossBlackboard>("Has_Row_1", bb => bb.TryGetRange("Row_1", out _)))
                .AddChild(new ArmSmashSkillNode("ArmSmash_Row_1", "Row_1", warningDuration: 1.2f));

            var delayedRow3 = new SequenceNode<BossBlackboard>("Delayed_Row3")
                .AddChild(new WaitNode<BossBlackboard>("Wait_0.5", 0.5f))
                .AddChild(new ArmSmashSkillNode("ArmSmash_Row_3", "Row_3", warningDuration: 1.2f));

            var patternA = new ParallelAllNode<BossBlackboard>("Pattern_A")
                .AddChild(openWithRow1)
                .AddChild(delayedRow3);

            // Selector로 다음 패턴 확장 가능.
            return new SelectorNode<BossBlackboard>("Root")
                .AddChild(patternA);
        }
    }
}
