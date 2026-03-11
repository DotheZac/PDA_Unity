#include "Repeater.h"

NodeState Repeater::Tick(BlackBoard& bb, float deltaTime)
{
    if (m_TimesDone >= m_TimeToRepeat) {
        m_TimesDone = 0;
        return NodeState::Success;
    }

    NodeState result = m_Children->Tick(bb, deltaTime);

    if (result == NodeState::Success || result == NodeState::Failure) {
        m_TimesDone++;
    }

    return NodeState::Running;
}

