#include "PhaseChecker_1.h"
#include "BlackBoard.h"

NodeState PhaseChecker_1::Tick(BlackBoard& bb, float deltaTime)
{
    int currPhase = bb.GetValue<int>("CurrPhase").value();
    float hp = bb.GetValue<float>("BossCurrHP").value();

    if (currPhase == 1)
    {
        if (hp < 50.f)
        {
            // 1페이즈 유지 조건이 아님 → 실패 처리
            return NodeState::Failure;
        }
        return NodeState::Success;
    }

    if (hp >= 50.f)
    {
        bb.SetValue("CurrPhase", 1);
        return NodeState::Success;
    }

    return NodeState::Failure;

}