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

        protected void RequestFailure() => FailRequested = true;

        protected abstract void StartWarning(BossBlackboard blackboard);
        protected abstract void EndWarningAndAttack(BossBlackboard blackboard);
        protected virtual bool IsAttackFinished(BossBlackboard blackboard, float deltaTime) => true;
        protected virtual void OnReset() { }
    }

    public sealed class ArmSmashSkillNode : TimedRangeSkillNode
    {
        public ArmSmashSkillNode(string name, string rangeKey, float warningDuration = 1.5f, float attackDuration = 0.35f)
            : base(name, rangeKey, warningDuration, attackDuration) { }
    }

    public sealed class ArmSwipSkillNode : TimedRangeSkillNode
    {
        public ArmSwipSkillNode(string name, string rangeKey, float warningDuration = 0.8f, float attackDuration = 0.9f)
            : base(name, rangeKey, warningDuration, attackDuration) { }
    }

    public sealed class ArmStretchSkillNode : TimedRangeSkillNode
    {
        public ArmStretchSkillNode(string name, string rangeKey, float warningDuration = 1.4f, float attackDuration = 0.7f)
            : base(name, rangeKey, warningDuration, attackDuration) { }
    }

    public sealed class PickSkillNode : SkillNode
    {
        private readonly string _rangeKey;
        private readonly float _attackDuration;
        private readonly float _damageStart;
        private readonly float _damageEnd;
        private readonly List<TelegraphTile> _activeTiles = new();
        private float _attackElapsed;
        private bool _damageEnabled;

        public PickSkillNode(string name, string rangeKey, float warningDuration = 0.9f, float attackDuration = 0.95f, float damageStart = 0.5f, float damageEnd = 0.8f)
            : base(name, warningDuration)
        {
            _rangeKey = rangeKey;
            _attackDuration = attackDuration;
            _damageStart = damageStart;
            _damageEnd = damageEnd;
        }

        protected override void StartWarning(BossBlackboard blackboard)
        {
            if (!SkillNodeUtils.PopulateTiles(blackboard, _rangeKey, _activeTiles))
            {
                RequestFailure();
            }
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            SkillNodeUtils.SetWarning(_activeTiles, false);
            SkillNodeUtils.SetDamage(_activeTiles, false);
            _attackElapsed = 0f;
            _damageEnabled = false;
        }

        protected override bool IsAttackFinished(BossBlackboard blackboard, float deltaTime)
        {
            _attackElapsed += deltaTime;

            if (!_damageEnabled && _attackElapsed >= _damageStart)
            {
                SkillNodeUtils.SetDamage(_activeTiles, true);
                _damageEnabled = true;
            }

            if (_damageEnabled && _attackElapsed >= _damageEnd)
            {
                SkillNodeUtils.SetDamage(_activeTiles, false);
                _damageEnabled = false;
            }

            return _attackElapsed >= _attackDuration;
        }

        protected override void OnReset()
        {
            SkillNodeUtils.SetWarning(_activeTiles, false);
            SkillNodeUtils.SetDamage(_activeTiles, false);
            _activeTiles.Clear();
            _attackElapsed = 0f;
            _damageEnabled = false;
        }
    }

    public sealed class LazerSkillNode : SkillNode
    {
        private readonly string _rangeKey;
        private readonly float _stepInterval;
        private readonly List<TelegraphTile> _activeTiles = new();
        private readonly List<int> _activeIndices = new();

        private int _step;
        private int _centerIndex;
        private int _minIndex;
        private int _maxIndex;
        private int _prevLeft = -1;
        private int _prevRight = -1;
        private float _stepElapsed;

        public LazerSkillNode(string name, string rangeKey, float warningDuration = 0.8f, float stepInterval = 0.3f)
            : base(name, warningDuration)
        {
            _rangeKey = rangeKey;
            _stepInterval = stepInterval;
        }

        protected override void StartWarning(BossBlackboard blackboard)
        {
            if (!SkillNodeUtils.PopulateTiles(blackboard, _rangeKey, _activeTiles))
            {
                RequestFailure();
                return;
            }

            _activeIndices.Clear();
            if (!blackboard.TryGetRange(_rangeKey, out var indices) || indices == null)
            {
                RequestFailure();
                return;
            }

            foreach (var index in indices)
            {
                if (index >= 0 && index < blackboard.Telegraphs.Length)
                {
                    _activeIndices.Add(index);
                }
            }

            if (_activeIndices.Count == 0)
            {
                RequestFailure();
                return;
            }

            _minIndex = int.MaxValue;
            _maxIndex = int.MinValue;
            foreach (var index in _activeIndices)
            {
                _minIndex = Mathf.Min(_minIndex, index);
                _maxIndex = Mathf.Max(_maxIndex, index);
            }

            _centerIndex = (_minIndex + _maxIndex) / 2;
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            SkillNodeUtils.SetWarning(_activeTiles, false);
            SkillNodeUtils.SetDamage(_activeTiles, false);
            _step = 0;
            _prevLeft = -1;
            _prevRight = -1;
            _stepElapsed = _stepInterval;
        }

        protected override bool IsAttackFinished(BossBlackboard blackboard, float deltaTime)
        {
            _stepElapsed += deltaTime;
            if (_stepElapsed < _stepInterval)
            {
                return false;
            }

            _stepElapsed = 0f;
            var left = _centerIndex - _step;
            var right = _centerIndex + _step;

            if (left >= _minIndex)
            {
                blackboard.Telegraphs[left].SetDamageActive(true);
            }

            if (right <= _maxIndex)
            {
                blackboard.Telegraphs[right].SetDamageActive(true);
            }

            if (_prevLeft >= _minIndex && _prevLeft <= _maxIndex)
            {
                blackboard.Telegraphs[_prevLeft].SetDamageActive(false);
            }

            if (_prevRight >= _minIndex && _prevRight <= _maxIndex)
            {
                blackboard.Telegraphs[_prevRight].SetDamageActive(false);
            }

            _prevLeft = left;
            _prevRight = right;
            _step++;

            return left < _minIndex && right > _maxIndex;
        }

        protected override void OnReset()
        {
            SkillNodeUtils.SetWarning(_activeTiles, false);
            SkillNodeUtils.SetDamage(_activeTiles, false);
            _activeTiles.Clear();
            _activeIndices.Clear();
            _step = 0;
            _prevLeft = -1;
            _prevRight = -1;
            _stepElapsed = 0f;
        }
    }

    public abstract class TimedRangeSkillNode : SkillNode
    {
        private readonly string _rangeKey;
        private readonly float _attackDuration;
        private readonly List<TelegraphTile> _activeTiles = new();
        private float _attackElapsed;

        protected TimedRangeSkillNode(string name, string rangeKey, float warningDuration, float attackDuration)
            : base(name, warningDuration)
        {
            _rangeKey = rangeKey;
            _attackDuration = attackDuration;
        }

        protected override void StartWarning(BossBlackboard blackboard)
        {
            if (!SkillNodeUtils.PopulateTiles(blackboard, _rangeKey, _activeTiles))
            {
                RequestFailure();
            }
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            SkillNodeUtils.SetWarning(_activeTiles, false);
            SkillNodeUtils.SetDamage(_activeTiles, true);
            _attackElapsed = 0f;
        }

        protected override bool IsAttackFinished(BossBlackboard blackboard, float deltaTime)
        {
            _attackElapsed += deltaTime;
            return _attackElapsed >= _attackDuration;
        }

        protected override void OnReset()
        {
            SkillNodeUtils.SetWarning(_activeTiles, false);
            SkillNodeUtils.SetDamage(_activeTiles, false);
            _activeTiles.Clear();
            _attackElapsed = 0f;
        }
    }

    internal static class SkillNodeUtils
    {
        internal static bool PopulateTiles(BossBlackboard blackboard, string rangeKey, List<TelegraphTile> buffer)
        {
            buffer.Clear();

            if (!blackboard.TryGetRange(rangeKey, out var indices) || indices == null)
            {
                return false;
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
                buffer.Add(tile);
            }

            return buffer.Count > 0;
        }

        internal static void SetWarning(List<TelegraphTile> tiles, bool visible)
        {
            foreach (var tile in tiles)
            {
                tile.SetWarningVisible(visible);
            }
        }

        internal static void SetDamage(List<TelegraphTile> tiles, bool active)
        {
            foreach (var tile in tiles)
            {
                tile.SetDamageActive(active);
            }
        }
    }
}