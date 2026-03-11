#include "PhaseChecker_3.h"
#include "BlackBoard.h"
#include "SpriteRenderer.h"
#include "AnimationComponent.h"
#include "TransformComponent.h"
#include <algorithm>
#include "AnimationUtils.h"

#ifdef max
#undef max
#endif

#ifdef min
#undef min
#endif

NodeState PhaseChecker_3::Tick(BlackBoard& bb, float deltaTime)
{
    int currPhase = bb.GetValue<int>("CurrPhase").value();
    float hp = bb.GetValue<float>("BossCurrHP").value();
    bool isPhase3 = bb.GetValue<bool>("3Phase").value();

    if (m_PhaseChange)
    {
        m_Hole->GetComponent<SpriteRenderer>()->SetOpacity(1.0f);
        m_Hole_Back->GetComponent<SpriteRenderer>()->SetOpacity(1.0f);


        {
            auto sr = m_Back_0->GetComponent<SpriteRenderer>();
            float curOpacity = sr->GetOpacity();
            float newOpacity = std::max(0.f, curOpacity - m_fadeSpeed * deltaTime);
            sr->SetOpacity(newOpacity);
        }

        // 배경 1: 점차 선명해지기 (증가)
        {
            auto sr = m_Back_1->GetComponent<SpriteRenderer>();
            float curOpacity = sr->GetOpacity();
            float newOpacity = std::min(1.f, curOpacity + m_fadeSpeed * deltaTime);
            sr->SetOpacity(newOpacity);
        }

        // 배경 2: 선명해졌다가 희미해지는 효과 (반복)
        {
            auto sr = m_Back_2->GetComponent<SpriteRenderer>();
            float curOpacity = sr->GetOpacity();

            // m_Back2FadeDirection: +1이면 증가, -1이면 감소 (멤버 변수로 bool 또는 int 설정 필요)
            if (m_Back2FadeDirection > 0)
            {
                float newOpacity = curOpacity + m_fadeSpeed * deltaTime;
                if (newOpacity >= 1.f)
                {
                    newOpacity = 1.f;
                    m_Back2FadeDirection = -1; // 감소로 전환
                }
                sr->SetOpacity(newOpacity);
            }
            else
            {
                float newOpacity = curOpacity - m_fadeSpeed * deltaTime;
                if (newOpacity <= 0.f)
                {
                    newOpacity = 0.f;
                    m_Back2FadeDirection = 1; // 증가로 전환
                }
                sr->SetOpacity(newOpacity);
            }
        }
        m_elapsedTime += deltaTime;

        auto trans = m_Boss_Phase_2_Arm->GetComponent<TransformComponent>();
        auto pos = trans->GetPosition();

        pos.x -= m_moveSpeed * deltaTime; // m_moveSpeed = 총 이동 거리
        trans->SetPosition(pos);


        bool allFinished = true;
        const float fadeSpeed = 0.5f; // 초당 1.0씩 증가 (0~1 사이)

        auto fadeObj = [&](std::shared_ptr<GameObject> obj) {
            auto anim = obj->GetComponent<AnimationComponent>();
            anim->SetIsActive(true);
            auto sprite = obj->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(true);
            float current = sprite->GetOpacity();
            current += fadeSpeed * deltaTime;
            if (current >= 1.0f)
                current = 1.0f;
            sprite->SetOpacity(current);
            return current >= 1.0f; // true면 완전 불투명
            };

        if (!fadeObj(m_Boss_Main)) allFinished = false;
        if (!fadeObj(m_Boss_Arm_L)) allFinished = false;
        if (!fadeObj(m_Boss_Arm_R)) allFinished = false;

        // 세 개 다 완전히 나타나면 PhaseChange 종료

        if (allFinished && m_elapsedTime >= m_moveDuration)
        {
            m_PhaseChange = false;
            m_elapsedTime = 0.0f;
            return NodeState::Success;
        }

        return NodeState::Running;
    }

    if (currPhase == 3)
        return NodeState::Success;

    if (hp < 50.f && isPhase3)
    {
        bb.SetValue("CurrPhase", 3);
        m_PhaseChange = true;

        m_Boss_Main = GetAnim(bb, "Boss_3Phase_IDLE_ani");
        m_Boss_Arm_L = GetAnim(bb, "Boss_Anim_Arm_L");
        m_Boss_Arm_R = GetAnim(bb, "Boss_Anim_Arm_R");
        m_Boss_Phase_2_Arm = GetAnim(bb, "Boss_Anim_Phase2_Arm");

        auto map = bb.GetValue<std::unordered_map<std::string, std::shared_ptr<GameObject>>>("Backgrounds");
        auto& backgrounds = map.value();

        m_Hole = backgrounds["Phase_2_Big_Hole"];
        m_Hole_Back = backgrounds["Phase_3_Hole_Background"];
        m_Back_0 = backgrounds["Phase_1_Monitor"];
        m_Back_1 = backgrounds["Phase_2_Monitor_1"];
        m_Back_2 = backgrounds["Phase_2_Monitor_2"];
        m_Back_3 = backgrounds["Phase_2_Monitor_3"];
        m_Back_4 = backgrounds["Phase_2_Monitor_4"];

        return NodeState::Running;
    }
    return NodeState::Failure;
}