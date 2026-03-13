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
        private bool _preserveWarningOnReset;
        private bool _attackAnimRequested;
        private bool _attackAnimStarted;
        protected bool FailRequested { get; private set; }

        protected SkillNode(string name, float warningDuration) : base(name)
        {
            _warningDuration = warningDuration;
        }

        public override NodeState Tick(BossBlackboard blackboard, float deltaTime)
        {
            return blackboard.ExecutionMode switch
            {
                PatternExecutionMode.WarningOnly => TickWarningOnly(blackboard, deltaTime),
                PatternExecutionMode.AttackOnly => TickAttackOnly(blackboard, deltaTime),
                _ => TickNormal(blackboard, deltaTime)
            };
        }

        public override void Reset()
        {
            var preserveWarnings = _preserveWarningOnReset;
            _started = false;
            _attacked = false;
            _elapsed = 0f;
            _preserveWarningOnReset = false;
            _attackAnimRequested = false;
            _attackAnimStarted = false;
            FailRequested = false;
            OnReset(preserveWarnings);
        }

        protected void RequestFailure() => FailRequested = true;

        protected abstract bool InitializeSkill(BossBlackboard blackboard, bool showWarning);
        protected abstract void EndWarningAndAttack(BossBlackboard blackboard);
        protected virtual bool IsAttackFinished(BossBlackboard blackboard, float deltaTime) => true;
        protected virtual void OnReset(bool preserveWarnings) { }

        private void TryStartAttackAnimation(BossBlackboard blackboard)
        {
            if (_attackAnimRequested)
            {
                return;
            }

            _attackAnimRequested = true;
            _attackAnimStarted = blackboard.TryPlayAttackAnimation(Name);
        }

        private bool IsAttackAnimationBlocking(BossBlackboard blackboard)
        {
            if (!_attackAnimRequested || !_attackAnimStarted)
            {
                return false;
            }

            return blackboard.IsAttackAnimationRunning(Name);
        }

        private NodeState TickNormal(BossBlackboard blackboard, float deltaTime)
        {
            if (!_started)
            {
                if (!InitializeSkill(blackboard, showWarning: true))
                {
                    RequestFailure();
                }

                Debug.Log($"[SkillNode] Skill triggered: {Name}", blackboard);
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
                TryStartAttackAnimation(blackboard);

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

            if (IsAttackAnimationBlocking(blackboard))
            {
                return NodeState.Running;
            }

            Reset();
            return NodeState.Success;
        }

        private NodeState TickWarningOnly(BossBlackboard blackboard, float deltaTime)
        {
            if (!_started)
            {
                if (!InitializeSkill(blackboard, showWarning: true))
                {
                    RequestFailure();
                }

                _started = true;
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

            // Warning-only phase should keep already visible warnings even if parent composite resets this node.
            _preserveWarningOnReset = true;
            return NodeState.Success;
        }

        private NodeState TickAttackOnly(BossBlackboard blackboard, float deltaTime)
        {
            if (!_started)
            {
                if (!InitializeSkill(blackboard, showWarning: false))
                {
                    RequestFailure();
                }

                _started = true;
            }

            if (FailRequested)
            {
                Reset();
                return NodeState.Failure;
            }

            if (!_attacked)
            {
                EndWarningAndAttack(blackboard);
                _attacked = true;
                TryStartAttackAnimation(blackboard);
            }

            if (!IsAttackFinished(blackboard, deltaTime))
            {
                return NodeState.Running;
            }

            if (IsAttackAnimationBlocking(blackboard))
            {
                return NodeState.Running;
            }

            Reset();
            return NodeState.Success;
        }
    }

    public sealed class ArmSmashSkillNode : SkillNode
    {
        private readonly string _rangeKey;
        private readonly float _moveDuration;
        private readonly List<TelegraphTile> _activeTiles = new();

        private TelegraphTile _movingTile;
        private Vector3 _moveStartPosition;
        private Vector3 _moveTargetPosition;
        private float _moveElapsed;
        private bool _isMoving;

        public ArmSmashSkillNode(string name, string rangeKey, float warningDuration = 1.5f, float attackDuration = 0.7f)
            : base(name, warningDuration)
        {
            _rangeKey = rangeKey;
            _moveDuration = attackDuration;
        }

        protected override bool InitializeSkill(BossBlackboard blackboard, bool showWarning)
        {
            _activeTiles.Clear();
            _movingTile = null;
            _moveElapsed = 0f;
            _isMoving = false;

            if (!blackboard.TryGetRange(_rangeKey, out var indices) || indices == null)
            {
                return false;
            }

            var minIndex = int.MaxValue;
            var maxIndex = int.MinValue;

            foreach (var index in indices)
            {
                if (index < 0 || index >= blackboard.Telegraphs.Length)
                {
                    continue;
                }

                var tile = blackboard.Telegraphs[index];
                if (tile == null)
                {
                    continue;
                }

                tile.SetWarningVisible(showWarning);
                tile.SetDamageActive(false);
                _activeTiles.Add(tile);

                minIndex = Mathf.Min(minIndex, index);
                maxIndex = Mathf.Max(maxIndex, index);
            }

            if (_activeTiles.Count == 0 || minIndex == int.MaxValue || maxIndex == int.MinValue)
            {
                return false;
            }

            _movingTile = blackboard.Telegraphs[maxIndex];
            var targetTile = blackboard.Telegraphs[minIndex];
            if (_movingTile == null || targetTile == null)
            {
                return false;
            }

            _moveStartPosition = _movingTile.transform.position;
            _moveTargetPosition = targetTile.transform.position;
            return true;
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            SkillNodeUtils.SetDamage(_activeTiles, false, Name);

            if (_movingTile == null)
            {
                RequestFailure();
                return;
            }

            _movingTile.transform.position = _moveStartPosition;
            _movingTile.SetDamageActive(true);
            _moveElapsed = 0f;
            _isMoving = true;
        }

        protected override bool IsAttackFinished(BossBlackboard blackboard, float deltaTime)
        {
            if (!_isMoving || _movingTile == null)
            {
                return true;
            }

            _moveElapsed += deltaTime;
            var t = _moveDuration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(_moveElapsed / _moveDuration);
            _movingTile.transform.position = Vector3.Lerp(_moveStartPosition, _moveTargetPosition, t);

            if (t < 1f)
            {
                return false;
            }

            _movingTile.SetDamageActive(false);
            _isMoving = false;
            return true;
        }

        protected override void OnReset(bool preserveWarnings)
        {
            if (!preserveWarnings)
            {
                SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            }

            SkillNodeUtils.SetDamage(_activeTiles, false, Name);

            if (_movingTile != null)
            {
                _movingTile.transform.position = _moveStartPosition;
            }

            _activeTiles.Clear();
            _movingTile = null;
            _moveElapsed = 0f;
            _isMoving = false;
        }
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

        protected override bool InitializeSkill(BossBlackboard blackboard, bool showWarning)
        {
            return SkillNodeUtils.PopulateTiles(blackboard, Name, _rangeKey, _activeTiles, showWarning);
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            SkillNodeUtils.SetDamage(_activeTiles, false, Name);
            _attackElapsed = 0f;
            _damageEnabled = false;
        }

        protected override bool IsAttackFinished(BossBlackboard blackboard, float deltaTime)
        {
            _attackElapsed += deltaTime;

            if (!_damageEnabled && _attackElapsed >= _damageStart)
            {
                SkillNodeUtils.SetDamage(_activeTiles, true, Name);
                _damageEnabled = true;
            }

            if (_damageEnabled && _attackElapsed >= _damageEnd)
            {
                SkillNodeUtils.SetDamage(_activeTiles, false, Name);
                _damageEnabled = false;
            }

            return _attackElapsed >= _attackDuration;
        }

        protected override void OnReset(bool preserveWarnings)
        {
            if (!preserveWarnings)
            {
                SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            }

            SkillNodeUtils.SetDamage(_activeTiles, false, Name);
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

        protected override bool InitializeSkill(BossBlackboard blackboard, bool showWarning)
        {
            if (!SkillNodeUtils.PopulateTiles(blackboard, Name, _rangeKey, _activeTiles, showWarning))
            {
                return false;
            }

            _activeIndices.Clear();
            if (!blackboard.TryGetRange(_rangeKey, out var indices) || indices == null)
            {
                return false;
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
                return false;
            }

            _minIndex = int.MaxValue;
            _maxIndex = int.MinValue;
            foreach (var index in _activeIndices)
            {
                _minIndex = Mathf.Min(_minIndex, index);
                _maxIndex = Mathf.Max(_maxIndex, index);
            }

            _centerIndex = (_minIndex + _maxIndex) / 2;
            return true;
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            SkillNodeUtils.SetDamage(_activeTiles, false, Name);
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
                Debug.Log($"[SkillNode] {Name} activated damage tile: {SkillNodeUtils.DescribeTile(blackboard.Telegraphs[left])}", blackboard);
            }

            if (right <= _maxIndex)
            {
                blackboard.Telegraphs[right].SetDamageActive(true);
                if (right != left)
                {
                    Debug.Log($"[SkillNode] {Name} activated damage tile: {SkillNodeUtils.DescribeTile(blackboard.Telegraphs[right])}", blackboard);
                }
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

        protected override void OnReset(bool preserveWarnings)
        {
            if (!preserveWarnings)
            {
                SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            }

            SkillNodeUtils.SetDamage(_activeTiles, false, Name);
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

        protected override bool InitializeSkill(BossBlackboard blackboard, bool showWarning)
        {
            return SkillNodeUtils.PopulateTiles(blackboard, Name, _rangeKey, _activeTiles, showWarning);
        }

        protected override void EndWarningAndAttack(BossBlackboard blackboard)
        {
            SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            SkillNodeUtils.SetDamage(_activeTiles, true, Name);
            _attackElapsed = 0f;
        }

        protected override bool IsAttackFinished(BossBlackboard blackboard, float deltaTime)
        {
            _attackElapsed += deltaTime;
            return _attackElapsed >= _attackDuration;
        }

        protected override void OnReset(bool preserveWarnings)
        {
            if (!preserveWarnings)
            {
                SkillNodeUtils.SetWarning(_activeTiles, false, Name);
            }

            SkillNodeUtils.SetDamage(_activeTiles, false, Name);
            _activeTiles.Clear();
            _attackElapsed = 0f;
        }
    }

    internal static class SkillNodeUtils
    {
        internal static bool PopulateTiles(BossBlackboard blackboard, string skillName, string rangeKey, List<TelegraphTile> buffer, bool showWarning)
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
                tile.SetWarningVisible(showWarning);
                tile.SetDamageActive(false);
                buffer.Add(tile);
            }

            if (showWarning && buffer.Count > 0)
            {
                var tileNames = string.Join(", ", buffer.ConvertAll(DescribeTile));
                Debug.Log($"[SkillNode] {skillName} warning tiles (range: {rangeKey}): {tileNames}", blackboard);
            }

            return buffer.Count > 0;
        }

        internal static void SetWarning(List<TelegraphTile> tiles, bool visible, string skillName)
        {
            foreach (var tile in tiles)
            {
                tile.SetWarningVisible(visible);
            }
        }

        internal static void SetDamage(List<TelegraphTile> tiles, bool active, string skillName)
        {
            foreach (var tile in tiles)
            {
                tile.SetDamageActive(active);
            }

            if (!active || tiles.Count == 0)
            {
                return;
            }

            var tileNames = string.Join(", ", tiles.ConvertAll(DescribeTile));
            Debug.Log($"[SkillNode] {skillName} activated damage tiles: {tileNames}");
        }

        internal static string DescribeTile(TelegraphTile tile)
        {
            if (tile == null)
            {
                return "<null>";
            }

            return $"{tile.name}#{tile.GetInstanceID()}";
        }
    }
}


