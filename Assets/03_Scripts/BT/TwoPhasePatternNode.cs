namespace BT
{
    public sealed class TwoPhasePatternNode : BTNode<BossBlackboard>
    {
        private enum Phase
        {
            Warning,
            Attack,
            WaitForClear
        }

        private readonly BTNode<BossBlackboard> _pattern;
        private Phase _phase = Phase.Warning;

        public TwoPhasePatternNode(string name, BTNode<BossBlackboard> pattern) : base(name)
        {
            _pattern = pattern;
        }

        public override NodeState Tick(BossBlackboard blackboard, float deltaTime)
        {
            if (_phase == Phase.Warning)
            {
                blackboard.SetPatternExecutionMode(PatternExecutionMode.WarningOnly);
                var warningState = _pattern.Tick(blackboard, deltaTime);

                if (warningState == NodeState.Running)
                {
                    return NodeState.Running;
                }

                if (warningState == NodeState.Failure)
                {
                    Cleanup(blackboard);
                    return NodeState.Failure;
                }

                blackboard.ClearAllWarnings();
                _pattern.Reset();
                _phase = Phase.Attack;
                return NodeState.Running;
            }

            if (_phase == Phase.Attack)
            {
                blackboard.SetPatternExecutionMode(PatternExecutionMode.AttackOnly);
                var attackState = _pattern.Tick(blackboard, deltaTime);

                if (attackState == NodeState.Running)
                {
                    return NodeState.Running;
                }

                if (attackState == NodeState.Failure)
                {
                    Cleanup(blackboard);
                    return NodeState.Failure;
                }

                _phase = Phase.WaitForClear;
                return NodeState.Running;
            }

            blackboard.SetPatternExecutionMode(PatternExecutionMode.Normal);
            if (blackboard.HasAnyActiveTelegraphState() || blackboard.HasAnyRunningAttackAnimation())
            {
                return NodeState.Running;
            }

            Cleanup(blackboard);
            return NodeState.Success;
        }

        public override void Reset()
        {
            _phase = Phase.Warning;
            _pattern.Reset();
        }

        private void Cleanup(BossBlackboard blackboard)
        {
            _phase = Phase.Warning;
            _pattern.Reset();
            blackboard.SetPatternExecutionMode(PatternExecutionMode.Normal);
            blackboard.ClearAllWarnings();
            blackboard.ClearAllDamage();
            blackboard.ClearAllAttackAnimations();
        }
    }
}
