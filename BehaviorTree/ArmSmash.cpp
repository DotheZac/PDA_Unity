#include "ArmSmash.h"
#include "BlackBoard.h"
#include "AnimationComponent.h"
#include "SpriteRenderer.h"
#include "TransformComponent.h"


NodeState ArmSmash::Tick(BlackBoard& bb, float deltaTime)
{

#pragma region Initialize
    if (!m_Initialized)
    {
        m_Telegraphs = bb.GetValue<std::vector<std::shared_ptr<Telegraph>>>("BossTelegraph").value();

        m_AttackRange = bb.GetValue<std::vector<int>>(m_Name).value();

        for (int idx : m_AttackRange)
        {
            if (m_minIndex > idx)
            {
                m_minIndex = idx;
            }
            if (m_maxIndex < idx)
            {
                m_maxIndex = idx;
            }
        }
        m_MoveStartPos = m_Telegraphs[m_maxIndex]->GetInitPos();
        m_MoveTargetPos = m_Telegraphs[m_minIndex]->GetInitPos();

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

    // 애니메이션 비활성화 타이머 (이게 먼저 와야 Success 전에 실행됨)
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

        auto trans = m_Telegraphs[m_maxIndex]->GetComponent<TransformComponent>();
        Math::Vector2F currPos;
        currPos.x = m_MoveStartPos.x + (m_MoveTargetPos.x - m_MoveStartPos.x) * t;
        currPos.y = m_MoveStartPos.y + (m_MoveTargetPos.y - m_MoveStartPos.y) * t;

        trans->SetPosition(currPos);

        if (t >= 1.0f)
        {
            m_IsMoving = false; // 이동 완료
            m_Telegraphs[m_maxIndex]->SetColliderActive(false);
        }
    }


    if (m_ElapsedTime >= m_WarningTime)
    {
        if (!m_AttackStarted)
        {
            bb.GetSoundManager().SFX_Shot(L"boss_arm_slam_stab");
            EndWarning(bb);
            m_AttackStarted = true;
        }

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


void ArmSmash::StartWarning(BlackBoard& bb)
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

void ArmSmash::EndWarning(BlackBoard& bb)
{
    std::cout << m_Name << " 공격" << std::endl;

    for (int idx : m_AttackRange)
    {
        auto& telegraph = m_Telegraphs[idx];
        if (telegraph)
        {
            telegraph->SetInactive();
            //telegraph->SetColliderActive();
        }
    }

    m_Telegraphs[m_maxIndex]->SetColliderActive(true);

    m_IsMoving = true;

    std::shared_ptr<GameObject> animObj;
    if (bb.GetValue<int>("CurrPhase").value() == 1)
    {
        animObj = GetAvailableAnim(bb, "Boss_ArmSmash");

    }
    else if (bb.GetValue<int>("CurrPhase").value() == 3)
    {
        animObj = GetAvailableAnim(bb, "Boss_3Phase_Arm_Smash");

    }

    if (animObj)
    {
        m_CurrentAnimObj = animObj;

        if (!m_AttackRange.empty()) //위치 조정
        {
            float posX = m_MoveStartPos.x + 1100.f;
            float posY = m_MoveStartPos.y - 60.f;
            animObj->GetComponent<TransformComponent>()->SetPosition({posX, posY});

        }

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

void ArmSmash::Reset()
{
    __super::Reset();
    m_MoveTimer = 0.0f;

    m_Telegraphs[m_maxIndex]->GetComponent<TransformComponent>()->SetPosition(m_MoveStartPos);
}