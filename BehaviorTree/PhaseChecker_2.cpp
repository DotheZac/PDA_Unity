#include "PhaseChecker_2.h"
#include "BlackBoard.h"
#include "SpriteRenderer.h"
#include "TransformComponent.h"
#include <algorithm>

NodeState PhaseChecker_2::Tick(BlackBoard& bb, float deltaTime)
{
    int currPhase = bb.GetValue<int>("CurrPhase").value();
    float hp = bb.GetValue<float>("BossCurrHP").value();
    bool isPhase3 = bb.GetValue<bool>("3Phase").value();

    // ----------------- PhaseChange 이동 -----------------
    if (m_PhaseChange)
    {
        m_elapsedTime += deltaTime;

        auto trans = m_Lazer_CCTV->GetComponent<TransformComponent>();
        auto pos = trans->GetPosition();
        pos.x += m_moveSpeed * deltaTime;
        trans->SetPosition(pos);

        if (m_elapsedTime >= m_moveDuration)
        {
            m_PhaseChange = false;
            m_elapsedTime = 0.0f;
            return NodeState::Success;
        }
        return NodeState::Running;
    }

    // ----------------- Phase2 상태 처리 -----------------
    if (currPhase == 2)
    {
        if (hp < 45.f)
        {
            bb.SetValue("3Phase", true);
        }
        return NodeState::Success;
    }

    // ----------------- Phase2 시작 조건 -----------------
    if (hp < 50.f && !isPhase3)
    {
        // 배경 초기화
        auto map = bb.GetValue<std::unordered_map<std::string, std::shared_ptr<GameObject>>>("Backgrounds");
        auto& backgrounds = map.value();
        m_Back_0 = backgrounds["Phase_1_Monitor"];
        m_Back_1 = backgrounds["Phase_2_Monitor_1"];
        m_Back_2 = backgrounds["Phase_2_Monitor_2"];
        m_Back_3 = backgrounds["Phase_2_Monitor_3"];
        m_Back_4 = backgrounds["Phase_2_Monitor_4"];

        // 레이저 초기화
        m_Lazer_CCTV = GetAnim(bb, "Boss_Phase_1_Main");

        m_PhaseChange = true;
        bb.SetValue("CurrPhase", 2);
        return NodeState::Running;
    }

    return NodeState::Failure;
}
