#include "Inverter.h"

NodeState Inverter::Tick(BlackBoard& bb, float deltaTime)
{
    if (!m_Children)  // 자식 노드가 하나라고 가정
        return NodeState::Failure;

    NodeState result = m_Children->Tick(bb, deltaTime);

    switch (result)
    {
    case NodeState::Success: return NodeState::Failure;
    case NodeState::Failure: return NodeState::Success;
    case NodeState::Running: return NodeState::Running;
    }

    return NodeState::Failure;
}

