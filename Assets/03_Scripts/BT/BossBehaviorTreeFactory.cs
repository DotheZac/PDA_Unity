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

        private static ConditionNode<BossBlackboard> RequirePhase(string name, int phase)
                   => new(name, bb => bb.CurrentPhase == phase);

        private static ActionNode<BossBlackboard> CooldownNode(string name, int skillCount)
            => new(name, bb => bb.TickCooldownAndPreparePattern(Time.deltaTime, skillCount) ? NodeState.Success : NodeState.Running);

        private static ActionNode<BossBlackboard> HitWindowNode(string name)
            => new(name, bb =>
            {
                bb.ProcessPendingHit();
                return NodeState.Success;
            });

        private static ConditionNode<BossBlackboard> SkillChanceCondition(string name, int skillIndex, int skillCount)
            => new(name, bb => bb.TryConsumeSkillChance(skillIndex, skillCount));

        private static ActionNode<BossBlackboard> CommitSkillSelection(string name, int skillIndex, int skillCount)
            => new(name, bb =>
            {
                bb.OnSkillSelected(skillIndex, skillCount);
                return NodeState.Success;
            });

        private static BTNode<BossBlackboard> CreateSkillNode(string name, string key, float warning = 1.2f, float attack = 0.35f)
        {
            if (name.Contains("Pick"))
            {
                return new PickSkillNode(name, key, warningDuration: warning, attackDuration: attack <= 0f ? 0.95f : attack);
            }

            if (name.Contains("Lazer"))
            {
                return new LazerSkillNode(name, key, warningDuration: warning, stepInterval: attack <= 0f ? 0.3f : attack);
            }

            if (name.Contains("Swip"))
            {
                return new ArmSwipSkillNode(name, key, warningDuration: warning, attackDuration: attack);
            }

            if (name.Contains("Stretch"))
            {
                return new ArmStretchSkillNode(name, key, warningDuration: warning, attackDuration: attack);
            }

            return new ArmSmashSkillNode(name, key, warningDuration: warning, attackDuration: attack);
        }

        private static BTNode<BossBlackboard> WeightedSkill(string nodeName, int skillIndex, int skillCount, BTNode<BossBlackboard> pattern)
        {
            return new SequenceNode<BossBlackboard>($"{nodeName}_Weighted")
                .AddChild(SkillChanceCondition($"{nodeName}_Chance", skillIndex, skillCount))
                .AddChild(pattern)
                .AddChild(CommitSkillSelection($"{nodeName}_CommitWeight", skillIndex, skillCount));
        }

        public static BTNode<BossBlackboard> CreateTestTree()
        {
            var p1Skill1 = new SequenceNode<BossBlackboard>("P1_Skill_1")
                .AddChild(RequireAny("P1_Skill_1_Condition", "Pick_5", "Pick_4", "Pick_14", "Pick_0", "Pick_10"))
                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_1_Parallel")
                    .AddChild(CreateSkillNode("Pick_5", "Pick_5", warning: 0.9f, attack: 0.95f))
                    .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_1_2_Sequence")
                        .AddChild(new WaitNode<BossBlackboard>("P1_Skill_1_3_Wait", 0.67f))
                        .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_3_Parallel")
                            .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_4_Parallel")
                                .AddChild(CreateSkillNode("Pick_4", "Pick_4", warning: 0.9f, attack: 0.95f))
                                .AddChild(CreateSkillNode("Pick_14", "Pick_14", warning: 0.9f, attack: 0.95f)))
                            .AddChild(new SequenceNode<BossBlackboard>("P1_Skill_1_4_Sequence")
                                .AddChild(new WaitNode<BossBlackboard>("P1_Skill_1_5_Wait", 0.17f))
                                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_1_5_Parallel")
                                    .AddChild(CreateSkillNode("Pick_0", "Pick_0", warning: 0.9f, attack: 0.95f))
                                    .AddChild(CreateSkillNode("Pick_10", "Pick_10", warning: 0.9f, attack: 0.95f)))))));

            var p1Skill2 = new SequenceNode<BossBlackboard>("P1_Skill_2")
                .AddChild(RequireAny("P1_Skill_2_Condition", "Pick_5", "Row_3", "Row_1"))
                .AddChild(new ParallelAllNode<BossBlackboard>("P1_Skill_2_1_Parallel")
                    .AddChild(CreateSkillNode("Pick_5", "Pick_5", warning: 1.0f, attack: 0.95f))
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
                                .AddChild(CreateSkillNode("Pick_7", "Pick_7", warning: 0.9f, attack: 0.95f))))));

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
                                    .AddChild(CreateSkillNode("Pick_0", "Pick_0", warning: 0.9f, attack: 0.95f))
                                    .AddChild(CreateSkillNode("Pick_10", "Pick_10", warning: 0.9f, attack: 0.95f)))))));

            var phase1Skills = new SelectorNode<BossBlackboard>("Phase_1_Skills")
                .AddChild(WeightedSkill("P1_Skill_1", 1, 5, p1Skill1))
                .AddChild(WeightedSkill("P1_Skill_2", 2, 5, p1Skill2))
                .AddChild(WeightedSkill("P1_Skill_3", 3, 5, p1Skill3))
                .AddChild(WeightedSkill("P1_Skill_4", 4, 5, p1Skill4))
                .AddChild(WeightedSkill("P1_Skill_5", 5, 5, p1Skill5));

            var phase2Skills = new SelectorNode<BossBlackboard>("Phase_2_Skills")
                .AddChild(WeightedSkill("P2_Skill_1", 1, 3, new SequenceNode<BossBlackboard>("P2_Skill_1")
                    .AddChild(RequireAny("P2_Skill_1_Condition", "Row_1"))
                    .AddChild(CreateSkillNode("ArmStretch_Row_1", "Row_1", warning: 1.4f, attack: 0.7f))))
                .AddChild(WeightedSkill("P2_Skill_2", 2, 3, new SequenceNode<BossBlackboard>("P2_Skill_2")
                    .AddChild(RequireAny("P2_Skill_2_Condition", "Row_2"))
                    .AddChild(CreateSkillNode("ArmStretch_Row_2", "Row_2", warning: 1.4f, attack: 0.7f))))
                .AddChild(WeightedSkill("P2_Skill_3", 3, 3, new SequenceNode<BossBlackboard>("P2_Skill_3")
                    .AddChild(RequireAny("P2_Skill_3_Condition", "Row_3"))
                    .AddChild(CreateSkillNode("ArmStretch_Row_3", "Row_3", warning: 1.4f, attack: 0.7f))));

            var phase3Skills = new SelectorNode<BossBlackboard>("Phase_3_Skills")
                .AddChild(WeightedSkill("P3_Skill_1", 1, 5, new SequenceNode<BossBlackboard>("P3_Skill_1")
                    .AddChild(RequireAny("P3_Skill_1_Condition", "Row_1", "Pick_5", "Swip_L"))
                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_1_1_Parallel")
                        .AddChild(CreateSkillNode("ArmSmash_Row_1", "Row_1"))
                        .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_1_2_Sequence")
                            .AddChild(new WaitNode<BossBlackboard>("P3_Skill_1_3_Wait", 0.33f))
                            .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_1_3_Parallel")
                                .AddChild(CreateSkillNode("Pick_5", "Pick_5", warning: 0.9f, attack: 0.95f))
                                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_1_4_Sequence")
                                    .AddChild(new WaitNode<BossBlackboard>("P3_Skill_1_5_Wait", 0.33f))
                                    .AddChild(CreateSkillNode("ArmSwip_L", "Swip_L", warning: 0.8f, attack: 0.45f))))))))
                .AddChild(WeightedSkill("P3_Skill_2", 2, 5, new SequenceNode<BossBlackboard>("P3_Skill_2")
                    .AddChild(RequireAny("P3_Skill_2_Condition", "Swip_L", "Row_2", "Pick_4"))
                    .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_2_1_Parallel")
                        .AddChild(CreateSkillNode("ArmSwip_L", "Swip_L", warning: 0.8f, attack: 0.45f))
                        .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_2_2_Sequence")
                            .AddChild(new WaitNode<BossBlackboard>("P3_Skill_2_3_Wait", 0.33f))
                            .AddChild(new ParallelAllNode<BossBlackboard>("P3_Skill_2_3_Parallel")
                                .AddChild(CreateSkillNode("Lazer_Row_2", "Row_2", warning: 0.8f, attack: 0.45f))
                                .AddChild(new SequenceNode<BossBlackboard>("P3_Skill_2_4_Sequence")
                                    .AddChild(new WaitNode<BossBlackboard>("P3_Skill_2_5_Wait", 0.33f))
                                    .AddChild(CreateSkillNode("Pick_4", "Pick_4", warning: 0.9f, attack: 0.95f))))))))
                .AddChild(WeightedSkill("P3_Skill_3", 3, 5, new SequenceNode<BossBlackboard>("P3_Skill_3")
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
                                        .AddChild(CreateSkillNode("Pick_3", "Pick_3", warning: 0.9f, attack: 0.95f))
                                        .AddChild(CreateSkillNode("Pick_8", "Pick_8", warning: 0.9f, attack: 0.95f)))))))))
                .AddChild(WeightedSkill("P3_Skill_4", 4, 5, new SequenceNode<BossBlackboard>("P3_Skill_4")
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
                                        .AddChild(CreateSkillNode("Pick_1", "Pick_1", warning: 0.9f, attack: 0.95f))
                                        .AddChild(CreateSkillNode("Pick_6", "Pick_6", warning: 0.9f, attack: 0.95f)))))))))
                .AddChild(WeightedSkill("P3_Skill_5", 5, 5, new SequenceNode<BossBlackboard>("P3_Skill_5")
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
                                    .AddChild(CreateSkillNode("Pick_8", "Pick_8", warning: 0.9f, attack: 0.95f))))))));

            var phase1CooldownAndHit = new ParallelAllNode<BossBlackboard>("Phase_1_Parallel_CoolDown")
                .AddChild(HitWindowNode("Phase_1_HitNode"))
                .AddChild(CooldownNode("Phase_1_CoolDown", 5));

            var phase1 = new SequenceNode<BossBlackboard>("Phase_1")
                .AddChild(RequirePhase("Phase_1_Check", 1))
                .AddChild(phase1CooldownAndHit)
                .AddChild(phase1Skills);

            var phase2 = new SequenceNode<BossBlackboard>("Phase_2")
                .AddChild(RequirePhase("Phase_2_Check", 2))
                .AddChild(CooldownNode("Phase_2_CoolDown", 3))
                .AddChild(phase2Skills);

            var phase3CooldownAndHit = new ParallelAllNode<BossBlackboard>("Phase_3_Parallel_CoolDown")
                .AddChild(HitWindowNode("Phase_3_HitNode"))
                .AddChild(CooldownNode("Phase_3_CoolDown", 5));

            var phase3 = new SequenceNode<BossBlackboard>("Phase_3")
                .AddChild(RequirePhase("Phase_3_Check", 3))
                .AddChild(phase3CooldownAndHit)
                .AddChild(phase3Skills);

            return new SelectorNode<BossBlackboard>("Root")
                .AddChild(phase1)
                .AddChild(phase2)
                .AddChild(phase3);
        }
    }
}