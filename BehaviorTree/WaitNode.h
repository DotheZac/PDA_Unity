#pragma once
#include "SkillNode.h"

class WaitNode : public SkillNode
{
public:
    WaitNode(std::string name, float waitTime) : SkillNode(name), m_WaitTime(waitTime) {}

    NodeState Tick(BlackBoard& bb, float deltaTime) override
    {
        m_ElapsedTime += deltaTime;


        if (m_ElapsedTime >= m_WaitTime)
        {

            Reset();
            return NodeState::Success;
        }
        return NodeState::Running;
    }

    void Reset()
    {
        m_ElapsedTime = 0.0f;
    }

private:
    float m_WaitTime;
    float m_ElapsedTime = 0.0f;
};