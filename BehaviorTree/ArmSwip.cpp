#include "ArmSwip.h"
#include "BlackBoard.h"
#include "TransformComponent.h"
#include "AnimationComponent.h"
#include "SpriteRenderer.h"

NodeState ArmSwip::Tick(BlackBoard& bb, float deltaTime)
{
#pragma region Initialize
    if (!m_Initialized)
    {
        m_Telegraphs = bb.GetValue<std::vector<std::shared_ptr<Telegraph>>>("BossTelegraph").value();
        m_AttackRange = bb.GetValue<std::vector<int>>(m_Name).value();


        if (m_Name == "Swip_L")
        {
            m_MoveStartIndex.push_back(0);
            m_MoveStartIndex.push_back(5);
            m_MoveStartIndex.push_back(10);
            m_Boss_Arm = GetAnim(bb, "Boss_Anim_Arm_L");
        }
        else if (m_Name == "Swip_R")
        {
            m_MoveStartIndex.push_back(4);
            m_MoveStartIndex.push_back(9);
            m_MoveStartIndex.push_back(14);
            m_Boss_Arm = GetAnim(bb, "Boss_Anim_Arm_R");

        }

        m_TargetIndex.push_back(2);
        m_TargetIndex.push_back(7);
        m_TargetIndex.push_back(12);

        for (int i = 0; i < m_MoveStartIndex.size(); i++)
        {
            m_MoveStartPos.push_back(m_Telegraphs[m_MoveStartIndex[i]]->GetInitPos());
            m_MoveTargetPos.push_back(m_Telegraphs[m_TargetIndex[i]]->GetInitPos());
        }




        m_Initialized = true;

    }

#pragma endregion

    if (!m_HasStarted)
    {
        bb.GetSoundManager().SFX_Shot(L"boss_warning_beep");

        StartWarning(bb);
        m_HasStarted = true;
    }

    m_ElapsedTime += deltaTime;

    if (m_AnimPlaying && m_CurrentAnimObj)
    {
        auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();
        if (animComp->IsAnimationFinished())
        {
            animComp->SetIsActive(false);
            auto sprite = m_CurrentAnimObj->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(false);
            sprite->SetOpacity(0);

            m_AnimPlaying = false;

            m_Boss_Arm->GetComponent<SpriteRenderer>()->SetOpacity(1);
            auto anim = m_Boss_Arm->GetComponent<AnimationComponent>();
            anim->SetIsActive(true);

            // 실행 종료한 애니메이션 이름 블랙보드에서 제거
            auto runningAnimsOpt = bb.GetValue<std::vector<std::string>>("RunningAnims");
            if (runningAnimsOpt.has_value())
            {
                auto runningAnims = runningAnimsOpt.value();
                auto it = std::find(runningAnims.begin(), runningAnims.end(), m_Name);
                if (it != runningAnims.end())
                {
                    runningAnims.erase(it);
                    bb.SetValue("RunningAnims", runningAnims);
                }
            }

            m_CurrentAnimObj = nullptr;
        }
    }

    // 이동은 공격 시작 여부와 무관하게 매 프레임 처리
    if (m_IsMoving)
    {
        m_MoveTimer += deltaTime;
        float t = m_MoveTimer / m_MoveDuration;
        if (t > 1.0f) t = 1.0f;

        for (size_t i = 0; i < m_MoveStartPos.size(); ++i)
        {
            auto trans = m_Telegraphs[m_MoveStartIndex[i]]->GetComponent<TransformComponent>();

            Math::Vector2F fromPos = m_Forward ? m_MoveStartPos[i] : m_MoveTargetPos[i];
            Math::Vector2F toPos = m_Forward ? m_MoveTargetPos[i] : m_MoveStartPos[i];

            Math::Vector2F currPos;
            currPos.x = fromPos.x + (toPos.x - fromPos.x) * t;
            currPos.y = fromPos.y + (toPos.y - fromPos.y) * t;

            trans->SetPosition(currPos);

            // 도착 지점에서 콜라이더 처리
            if (t >= 1.0f && !m_Forward) // 돌아오는 과정이 끝났을 때
            {
                m_Telegraphs[m_MoveStartIndex[i]]->SetColliderActive(false);
            }
        }

        if (t >= 1.0f)
        {
            if (m_Forward)
            {
                // 목표까지 도착 → 돌아오는 단계로 전환
                m_Forward = false;
                m_MoveTimer = 0.0f;
            }
            else
            {
                // 돌아오는 것도 끝 → 이동 완전 종료
                m_IsMoving = false;
            }
        }
    }


    if (m_ElapsedTime >= m_WarningTime)
    {
        if (!m_AttackStarted)
        {
            bb.GetSoundManager().SFX_Shot(L"boss_arm_sweep");

            EndWarning(bb);
            m_AttackStarted = true;
        }

        // 애니메이션 끝났는지 체크
        auto runningAnimsOpt = bb.GetValue<std::vector<std::string>>("RunningAnims");
        if (runningAnimsOpt.has_value() && !runningAnimsOpt.value().empty())
        {
            return NodeState::Running;
        }


        Reset();
        return NodeState::Success;
    }


    return NodeState::Running;
}

void ArmSwip::StartWarning(BlackBoard& bb)
{
    std::cout << m_Name << " 경고" << std::endl;

    for (int idx : m_AttackRange)
    {
        auto& telegraph = m_Telegraphs[idx];

        if (telegraph)
        {
            telegraph->SetActive();
        }
    }
}

void ArmSwip::EndWarning(BlackBoard& bb)
{
    std::cout << m_Name << " 공격" << std::endl;

    for (int idx : m_AttackRange)
    {
        auto& telegraph = m_Telegraphs[idx];

        if (telegraph)
        {
            telegraph->SetInactive();
        }
    }

    for (int i = 0; i < m_MoveStartIndex.size(); i++)
    {
        m_Telegraphs[m_MoveStartIndex[i]]->SetColliderActive(true);
    }
    
    m_IsMoving = true;
    
    std::shared_ptr<GameObject> animObj;
    if (m_Name == "Swip_L")
    {
        animObj = GetAvailableAnim(bb, "Boss_Anim_ArmSwip_L");

    }
    else if (m_Name == "Swip_R")
    {
        animObj = GetAvailableAnim(bb, "Boss_Anim_ArmSwip_R");

    }
    if (animObj)
    {
        m_CurrentAnimObj = animObj;

        m_Boss_Arm->GetComponent<SpriteRenderer>()->SetOpacity(0);

        auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();
        animComp->SetIsActive(true);
        animComp->Play();

        auto sprite = m_CurrentAnimObj->GetComponent<SpriteRenderer>();
        sprite->SetIsActive(true);
        sprite->SetOpacity(1);

        // 실행 시작 시 블랙보드에 애니메이션 이름 추가
        auto runningAnimsOpt = bb.GetValue<std::vector<std::string>>("RunningAnims");
        if (!runningAnimsOpt.has_value())
        {
            bb.SetValue("RunningAnims", std::vector<std::string>{ m_Name });
        }
        else
        {
            auto runningAnims = runningAnimsOpt.value();
            runningAnims.push_back(m_Name);
            bb.SetValue("RunningAnims", runningAnims);
        }



        m_AnimPlaying = true;
    }

}

void ArmSwip::Reset()
{
    __super::Reset();
    m_IsMoving = false;
    m_Forward = true;
    m_HasStarted = false;

    m_MoveTimer = 0.0f;

    //for (int i = 0; i < m_MoveStartIndex.size(); i++)
    //{
    //    m_Telegraphs[m_MoveStartIndex[i]]->GetComponent<TransformComponent>()->SetPosition(m_MoveStartPos[i]);
    //}

    m_MoveStartPos.clear();    // 좌표 버퍼 초기화
    m_MoveTargetPos.clear();

    m_Initialized = false;

    if (m_Boss_Arm)
    {
        m_Boss_Arm->GetComponent<SpriteRenderer>()->SetOpacity(1);
        auto anim = m_Boss_Arm->GetComponent<AnimationComponent>();
        anim->SetIsActive(true);
    }
}

