#include "Selector.h"

NodeState Selector::Tick(BlackBoard& bb, float deltaTime)
{
    while (m_CurrIndex < m_Children.size())
    {
        NodeState result = m_Children[m_CurrIndex]->Tick(bb, deltaTime);

        if (result == NodeState::Running) {
            return NodeState::Running;  // currentIndex 그대로 유지
        }
        else if (result == NodeState::Success) {
            m_CurrIndex = 0;  // 초기화
            return NodeState::Success;
        }
        else {      //Failure
            m_CurrIndex++;
        }
    }

    m_CurrIndex = 0;  // 모든 자식 실패 시 초기화
    return NodeState::Failure;
}
