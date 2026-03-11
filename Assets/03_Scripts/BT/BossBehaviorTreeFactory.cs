using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BT
{
    public static class BossBehaviorTreeFactory
    {
        private static readonly string[] RequiredRangeKeys =
        {
            "Row_1", "Row_2", "Row_3",
            "Swip_L", "Swip_R",
            "Pick_0", "Pick_1", "Pick_3", "Pick_4", "Pick_5", "Pick_6", "Pick_7", "Pick_8", "Pick_10", "Pick_14"
        };

        public static IReadOnlyList<string> GetRequiredRangeKeys() => RequiredRangeKeys;

        private static ConditionNode<BossBlackboard> RequireAny(string name, params string[] keys)
        {
            return new ConditionNode<BossBlackboard>(name, bb =>
            {
                foreach (var key in keys)
                {
                    if (bb.TryGetRange(key, out _))
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        private static BTNode<BossBlackboard> CreateSkillNode(string name, string key, float warning = 1.2f, float attack = 0.35f)
                    => new ArmSmashSkillNode(name, key, warningDuration: warning, attackDuration: attack);

        public static BTNode<BossBlackboard> CreateTestTree()
        {
            var p1Skill1 = new SequenceNode<BossBlackboard>("P1_Skill_1")
                .AddChild(RequireAny("P1_Skill_1_Condition", "Pick_5", "Pick_4", "Pick_14", "Pick_0", "Pick_10"))
                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_1_Parallel")
                    .AddChild(CreateSkillNode("Pick_5", "Pick_5", warning: 0.9f))
                    .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_1_2_Sequence")
                        .AddChild(new WaitNode<BossBlackboard>("P1_Skill_1_3_Wait", 0.67f))
                        .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_3_Parallel")
                            .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_4_Parallel")
                                .AddChild(CreateSkillNode("Pick_4", "Pick_4", warning: 0.9f))
                                .AddChild(CreateSkillNode("Pick_14", "Pick_14", warning: 0.9f)))
                            .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_1_4_Sequence")
                                .AddChild(new WaitNode<BossBlackboard>("P1_Skill_1_5_Wait", 0.17f))
                                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_5_Parallel")
                                    .AddChild(CreateSkillNode("Pick_0", "Pick_0", warning: 0.9f))
                                    .AddChild(CreateSkillNode("Pick_10", "Pick_10", warning: 0.9f)))))));

            var p1Skill2 = new SequenceNode<BossBlackboard>("P1_Skill_2")
                .AddChild(RequireAny("P1_Skill_2_Condition", "Pick_5", "Row_3", "Row_1"))
                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_2_1_Parallel")
                    .AddChild(CreateSkillNode("Pick_5", "Pick_5", warning: 1.0f))
                    .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_2_2_Sequence")
                        .AddChild(new WaitNode<BossBlackboard>("P1_Skill_2_3_Wait", 0.5f))
                        .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_2_3_Parallel")
                            .AddChild(CreateSkillNode("ArmSmash_Row_3", "Row_3"))
                            .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_2_4_Sequence")
                                .AddChild(new WaitNode<BossBlackboard>("P1_Skill_2_5_Wait", 0.5f))
                                .AddChild(CreateSkillNode("Lazer_Row_1", "Row_1", warning: 0.8f, attack: 0.45f))))));

            var p1Skill3 = new SequenceNode<BossBlackboard>("P1_Skill_3")
                .AddChild(RequireAny("P1_Skill_3_Condition", "Row_1", "Row_3", "Pick_7"))
                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_3_1_Parallel")
                    .AddChild(CreateSkillNode("Lazer_Row_1", "Row_1", warning: 0.8f, attack: 0.45f))
                    .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_3_2_Sequence")
                        .AddChild(new WaitNode<BossBlackboard>("P1_Skill_3_3_Wait", 0.5f))
                        .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_3_3_Parallel")
                            .AddChild(CreateSkillNode("ArmSmash_Row_3", "Row_3"))
                            .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_3_4_Sequence")
                                .AddChild(new WaitNode<BossBlackboard>("P1_Skill_3_5_Wait", 0.5f))
                                .AddChild(CreateSkillNode("Pick_7", "Pick_7", warning: 0.9f))))));

            var p1Skill4 = new SequenceNode<BossBlackboard>("P1_Skill_4")
                .AddChild(RequireAny("P1_Skill_4_Condition", "Row_1", "Row_2", "Row_3"))
                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_4_1_Parallel")
                    .AddChild(CreateSkillNode("ArmSmash_Row_1", "Row_1"))
                    .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_4_2_Sequence")
                        .AddChild(new WaitNode<BossBlackboard>("P1_Skill_4_3_Wait", 1.25f))
                        .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_4_3_Parallel")
                            .AddChild(CreateSkillNode("ArmSmash_Row_2", "Row_2"))
                            .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_4_4_Sequence")
                                .AddChild(new WaitNode<BossBlackboard>("P1_Skill_4_5_Wait", 1.25f))
                                .AddChild(CreateSkillNode("ArmSmash_Row_3", "Row_3"))))));

            var p1Skill5 = new SequenceNode<BossBlackboard>("P1_Skill_5")
                .AddChild(RequireAny("P1_Skill_5_Condition", "Row_1", "Row_2", "Row_3", "Pick_0", "Pick_10"))
                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_5_1_Parallel")
                    .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_5_2_ParallelNode")
                        .AddChild(CreateSkillNode("ArmSmash_Row_2", "Row_2"))
                        .AddChild(CreateSkillNode("ArmSmash_Row_3", "Row_3")))
                    .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_5_2_Sequence")
                        .AddChild(new WaitNode<BossBlackboard>("P1_Skill_5_3_Wait", 1.3f))
                        .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_5_3_Parallel")
                            .AddChild(CreateSkillNode("Lazer_Row_1", "Row_1", warning: 0.8f, attack: 0.45f))
                            .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_5_4_Sequence")
                                .AddChild(new WaitNode<BossBlackboard>("P1_Skill_5_5_Wait", 0.3f))
                                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_5_5_ParallelNode")
                                    .AddChild(CreateSkillNode("Pick_0", "Pick_0", warning: 0.9f))
                                    .AddChild(CreateSkillNode("Pick_10", "Pick_10", warning: 0.9f)))))));

            var phase1Skills = new SelectorNode<BossBlackboard>("Phase_1_Skills")
                .AddChild(p1Skill1)
                .AddChild(p1Skill2)
                .AddChild(p1Skill3)
                .AddChild(p1Skill4)
                .AddChild(p1Skill5);

            var phase2Skills = new SelectorNode<BossBlackboard>("Phase_2_Skills")
                .AddChild(new SequenceNode<BossBlackboard>("P2_Skill_1")
                    .AddChild(RequireAny("P2_Skill_1_Condition", "Row_1"))
                    .AddChild(CreateSkillNode("ArmStretch_Row_1", "Row_1", warning: 1.4f, attack: 0.5f)))
                .AddChild(new SequenceNode<BossBlackboard>("P2_Skill_2")
                    .AddChild(RequireAny("P2_Skill_2_Condition", "Row_2"))
                    .AddChild(CreateSkillNode("ArmStretch_Row_2", "Row_2", warning: 1.4f, attack: 0.5f)))
                .AddChild(new SequenceNode<BossBlackboard>("P2_Skill_3")
                    .AddChild(RequireAny("P2_Skill_3_Condition", "Row_3"))
                    .AddChild(CreateSkillNode("ArmStretch_Row_3", "Row_3", warning: 1.4f, attack: 0.5f)));

            var phase3Skills = new SelectorNode<BossBlackboard>("Phase_3_Skills")
                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_1")
                    .AddChild(RequireAny("P3_Skill_1_Condition", "Row_1", "Pick_5", "Swip_L"))
                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_1_1_Parallel")
                        .AddChild(CreateSkillNode("ArmSmash_Row_1", "Row_1"))
                        .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_1_2_Sequence")
                            .AddChild(new WaitNode<BossBlackboard>("P3_Skill_1_3_Wait", 0.33f))
                            .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_1_3_Parallel")
                                .AddChild(CreateSkillNode("Pick_5", "Pick_5", warning: 0.9f))
                                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_1_4_Sequence")
                                    .AddChild(new WaitNode<BossBlackboard>("P3_Skill_1_5_Wait", 0.33f))
                                    .AddChild(CreateSkillNode("ArmSwip_L", "Swip_L", warning: 0.8f, attack: 0.45f)))))))
                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_2")
                    .AddChild(RequireAny("P3_Skill_2_Condition", "Swip_L", "Row_2", "Pick_4"))
                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_2_1_Parallel")
                        .AddChild(CreateSkillNode("ArmSwip_L", "Swip_L", warning: 0.8f, attack: 0.45f))
                        .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_2_2_Sequence")
                            .AddChild(new WaitNode<BossBlackboard>("P3_Skill_2_3_Wait", 0.33f))
                            .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_2_3_Parallel")
                                .AddChild(CreateSkillNode("Lazer_Row_2", "Row_2", warning: 0.8f, attack: 0.45f))
                                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_2_4_Sequence")
                                    .AddChild(new WaitNode<BossBlackboard>("P3_Skill_2_5_Wait", 0.33f))
                                    .AddChild(CreateSkillNode("Pick_4", "Pick_4", warning: 0.9f)))))))
                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_3")
                    .AddChild(RequireAny("P3_Skill_3_Condition", "Swip_L", "Row_3", "Pick_3", "Pick_8"))
                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_3_1_Parallel")
                        .AddChild(CreateSkillNode("ArmSwip_L", "Swip_L", warning: 0.8f, attack: 0.45f))
                        .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_3_2_Sequence")
                            .AddChild(new WaitNode<BossBlackboard>("P3_Skill_3_3_Wait", 0.3f))
                            .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_3_3_Parallel")
                                .AddChild(CreateSkillNode("ArmSmash_Row_3", "Row_3"))
                                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_3_4_Sequence")
                                    .AddChild(new WaitNode<BossBlackboard>("P3_Skill_3_5_Wait", 0.2f))
                                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_3_5_Parallel")
                                        .AddChild(CreateSkillNode("Pick_3", "Pick_3", warning: 0.9f))
                                        .AddChild(CreateSkillNode("Pick_8", "Pick_8", warning: 0.9f))))))))
                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_4")
                    .AddChild(RequireAny("P3_Skill_4_Condition", "Swip_R", "Row_1", "Pick_1", "Pick_6"))
                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_4_1_Parallel")
                        .AddChild(CreateSkillNode("ArmSwip_R", "Swip_R", warning: 0.8f, attack: 0.45f))
                        .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_4_2_Sequence")
                            .AddChild(new WaitNode<BossBlackboard>("P3_Skill_4_3_Wait", 0.3f))
                            .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_4_3_Parallel")
                                .AddChild(CreateSkillNode("ArmSmash_Row_1", "Row_1"))
                                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_4_4_Sequence")
                                    .AddChild(new WaitNode<BossBlackboard>("P3_Skill_4_5_Wait", 0.2f))
                                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_4_5_Parallel")
                                        .AddChild(CreateSkillNode("Pick_1", "Pick_1", warning: 0.9f))
                                        .AddChild(CreateSkillNode("Pick_6", "Pick_6", warning: 0.9f))))))))
                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_5")
                    .AddChild(RequireAny("P3_Skill_5_Condition", "Row_1", "Row_3", "Swip_L", "Pick_8"))
                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_5_1_Parallel")
                        .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_5_2_ParallelNode")
                            .AddChild(CreateSkillNode("ArmSmash_Row_1", "Row_1"))
                            .AddChild(CreateSkillNode("ArmSmash_Row_3", "Row_3")))
                        .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_5_2_Sequence")
                            .AddChild(new WaitNode<BossBlackboard>("P3_Skill_5_3_Wait", 1.3f))
                            .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_5_3_Parallel")
                                .AddChild(CreateSkillNode("ArmSwip_L", "Swip_L", warning: 0.8f, attack: 0.45f))
                                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_5_4_Sequence")
                                    .AddChild(new WaitNode<BossBlackboard>("P3_Skill_5_5_Wait", 0.3f))
                                    .AddChild(CreateSkillNode("Pick_8", "Pick_8", warning: 0.9f)))))));

            return new SelectorNode<BossBlackboard>("Root")
                .AddChild(phase1Skills)
                .AddChild(phase2Skills)
                .AddChild(phase3Skills);
        }
    }
}