using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    public abstract class SkillNode : BTNode<BossBlackboard>
    {
        private readonly float _warningDuration;
        private bool _started;
        private bool _attacked;
        private float _elapsed;
        protected bool FailRequested { get; private set; }


        protected SkillNode(string name, float warningDuration) : base(name)
        {
            _warningDuration = warningDuration;
        }

        public override NodeState Tick(BossBlackboard blackboard, float deltaTime)
        {
            if (!_started)
            {
                StartWarning(blackboard);
                _started = true;

                if (FailRequested)
                {
                    Reset();
                    return NodeState.Failure;
                }
            }

            if (FailRequested)
            {
                Reset();
                return NodeState.Failure;
            }

            _elapsed += deltaTime;
            if (_elapsed < _warningDuration)
            {
                return NodeState.Running;
            }

            if (!_attacked)
            {
                EndWarningAndAttack(blackboard);
                _attacked = true;

                if (FailRequested)
                {
                    Reset();
                    return NodeState.Failure;
                }
            }

            if (!IsAttackFinished(blackboard, deltaTime))
            {
                return NodeState.Running;
            }

            Reset();
            return NodeState.Success;
        }

        public override void Reset()
        {
            _started = false;
            _attacked = false;
            _elapsed = 0f;
            FailRequested = false;
            OnReset();
        }

        protected void RequestFailure()
        {
            FailRequested = true;
        }

        protected abstract void StartWarning(BossBlackboard blackboard);
        protected abstract void EndWarningAndAttack(BossBlackboard blackboard);
        protected virtual bool IsAttackFinished(BossBlackboard blackboard, float deltaTime) => true;
        protected virtual void OnReset() { }
    }


    //Custom Skills
    public sealed class ArmSmashSkillNode : SkillNode
    {
        private readonly string _rangeKey;
        private readonly float _attackDuration;
        private readonly List<TelegraphTile> _activeTiles = new();
        private float _attackElapsed;

        public ArmSmashSkillNode(string name, string rangeKey, float warningDuration = 1.5f, float attackDuration = 0.35f)
            : base(name, warningDuration)
        {
            _rangeKey = rangeKey;
            _attackDuration = attackDuration;
        }

        protected override void StartWarning(BossBlackboard blackboard)
        {
            _activeTiles.Clear();
            Debug.Log($"[ArmSmashSkillNode:{Name}] ArmSmash 시도. rangeKey={_rangeKey}");

            if (!blackboard.TryGetRange(_rangeKey, out var indices))
            {
                Debug.LogWarning($"[ArmSmashSkillNode:{Name}] ArmSmash 실패. Range key not found: {_rangeKey}");
                RequestFailure();
                return;
            }

            foreach (var index in indices)
            {
                if (index < 0 || index >= blackboard.Telegraphs.Length)
                {
                    continue;
                }

                var tile = blackboard.Telegraphs[index];
                tile.SetWarningVisible(true);
                tile.SetDamageActive(false);
                _activeTiles.Add(tile);
            }

            if (_activeTiles.Count == 0)
            {
                Debug.LogWarning($"[ArmSmashSkillNode:{Name}] ArmSmash 실패. No valid telegraph tile index in range '{_rangeKey}'.");
                RequestFailure();
            }
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            _attackElapsed = 0f;
            Debug.Log("팔 휘두르기");
            foreach (var tile in _activeTiles)
            {
                tile.SetWarningVisible(false);
                tile.SetDamageActive(true);
            }
        }

        protected override bool IsAttackFinished(BossBlackboard blackboard, float deltaTime)
        {
            _attackElapsed += deltaTime;
            if (_attackElapsed < _attackDuration)
            {
                return false;
            }

            Debug.Log($"[ArmSmashSkillNode:{Name}] ArmSmash 성공.");
            return true;
        }

        protected override void OnReset()
        {
            foreach (var tile in _activeTiles)
            {
                tile.SetWarningVisible(false);
                tile.SetDamageActive(false);
            }

            _activeTiles.Clear();
            _attackElapsed = 0f;
        }
    }
}