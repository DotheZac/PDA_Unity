#include "ArmStretch.h"
#include "BlackBoard.h"
#include "AnimationComponent.h"
#include "SpriteRenderer.h"
#include "TransformComponent.h"

NodeState ArmStretch::Tick(BlackBoard& bb, float deltaTime)
{
#pragma region Initialize
    if (!m_Initialized)
    {
        m_Telegraphs = bb.GetValue<std::vector<std::shared_ptr<Telegraph>>>("BossTelegraph").value();
        m_AttackRange = bb.GetValue<std::vector<int>>(m_Name).value();

        if (!m_CurrentAnimObj)
        {
            m_CurrentAnimObj = GetAnim(bb, "Boss_Anim_Phase2_Arm");
            m_CurrentAnimObj->GetComponent<AnimationComponent>()->SetLoop(true);
        }

        for (int idx : m_AttackRange)
        {
            if (m_minIndex > idx)
            {
                m_minIndex = idx;
            }
            m_maxIndex = m_minIndex + 2;
        }
        m_MoveStartPos = m_Telegraphs[m_minIndex]->GetCurrPos();
        m_MoveTargetPos = m_Telegraphs[m_maxIndex]->GetCurrPos();

        m_ScrollSpeed = bb.GetValue<float>("ScrollSpeed").value();

        m_Initialized = true;
    }
#pragma endregion

    m_Moved += m_ScrollSpeed * deltaTime;

    if (!m_HasStarted)
    {
        bb.GetSoundManager().SFX_Shot(L"boss_warning_beep");

        StartWarning(bb); // 여기서 m_CurrentAnimObj 이동 시작
        m_HasStarted = true;
    }

    m_ElapsedTime += deltaTime;



    if (m_IsMoving)
    {
        m_MoveTimer += deltaTime;

        float t = m_MoveTimer / m_MoveDuration;
        if (t > 1.0f) t = 1.0f;

        auto trans = m_Telegraphs[m_minIndex]->GetComponent<TransformComponent>();
        Math::Vector2F currPos;
        currPos.x = m_Moved + m_MoveStartPos.x + (m_MoveTargetPos.x - m_MoveStartPos.x) * t;
        currPos.y = m_MoveStartPos.y + (m_MoveTargetPos.y - m_MoveStartPos.y) * t;

        trans->SetPosition(currPos);

        if (t >= 1.0f)
        {
            m_IsMoving = false; // 이동 완료
            m_Telegraphs[m_minIndex]->SetColliderActive(false);
        }
    }


    // --- 경고 중 m_CurrentAnimObj를 m_MoveStartPos로 부드럽게 이동 ---
    if (m_IsMovingToStartPos)
    {
        m_MoveToStartPosTimer += deltaTime;
        float t = m_MoveToStartPosTimer / m_MoveToStartPosDuration;
        if (t > 1.0f) t = 1.0f;

        if (m_CurrentAnimObj)
        {
            auto trans = m_CurrentAnimObj->GetComponent<TransformComponent>();
            Math::Vector2F currPos;
            currPos.x = m_Moved + m_MoveFromPos.x + (m_MoveStartPos.x - m_MoveFromPos.x) * t;
            currPos.y = 40.f + m_MoveFromPos.y + (m_MoveStartPos.y - m_MoveFromPos.y) * t;

            trans->SetPosition(currPos);

            if (t >= 1.0f) m_IsMovingToStartPos = false;
        }
    }

    // 텔레그래프 이동


    // 애니메이션 종료 처리
    if (m_AnimPlaying && m_CurrentAnimObj)
    {
        auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();
        if (animComp->IsAnimationFinished())
        {
            m_AnimPlaying = false;
        }
    }





    if (m_ElapsedTime >= m_WarningTime)
    {
        if (!m_AttackStarted)
        {
            bb.GetSoundManager().SFX_Shot(L"boss_arm_extend");
            EndWarning(bb); // 경고 끝나고 공격 시작
            m_AttackStarted = true;
        }

        if (!m_CurrentAnimObj->GetComponent<AnimationComponent>()->IsAnimationFinished())
        {
            return NodeState::Running;
        }

        Reset();
        return NodeState::Success;
    }


    return NodeState::Running;
}

void ArmStretch::StartWarning(BlackBoard& bb)
{
    std::cout << m_Name << " 경고" << std::endl;

    // 사용 가능한 애니메이션 가져오기
    if (m_CurrentAnimObj)
    {
        auto trans = m_CurrentAnimObj->GetComponent<TransformComponent>();
        m_MoveFromPos = trans->GetPosition(); // 현재 위치 저장
        m_MoveToStartPosTimer = 0.0f;
        m_IsMovingToStartPos = true;

        // 애니메이션 표시
        auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();
        animComp->SetIsActive(true);
        animComp->Play("idle");
        animComp->SetLoop(true);

        auto sprite = m_CurrentAnimObj->GetComponent<SpriteRenderer>();
        sprite->SetIsActive(true);
        sprite->SetOpacity(1);


        m_AnimPlaying = true;
    }

    int i = 0;
    for (int idx : m_AttackRange)
    {
        if (i > 2)
        {
            break;
        }
        auto& telegraph = m_Telegraphs[idx];
        if (telegraph) telegraph->SetActive();
        i++;
    }
}

void ArmStretch::EndWarning(BlackBoard& bb)
{
    std::cout << m_Name << " 공격" << std::endl;


    int i = 0;
    for (int idx : m_AttackRange)
    {
        if (i > 2)
        {
            break;
        }
        auto& telegraph = m_Telegraphs[idx];
        if (telegraph)
        {
            telegraph->SetInactive();
        }
        i++;
    }

    m_Telegraphs[m_minIndex]->SetColliderActive(true);
    m_IsMoving = true;

    // 공격 애니메이션 시작 (위치는 이미 StartWarning에서 이동시킴)

    auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();
    animComp->Play("stretch");
    animComp->SetLoop(false);
    animComp->SetIsActive(true);

    auto sprite = m_CurrentAnimObj->GetComponent<SpriteRenderer>();
    sprite->SetIsActive(true);
    sprite->SetOpacity(1);

    m_AnimPlaying = true;
}

void ArmStretch::Reset()
{
    __super::Reset();
    m_MoveTimer = 0.0f;

    m_Telegraphs[m_minIndex]->GetComponent<TransformComponent>()->SetPosition({ m_MoveStartPos.x + m_Moved, m_MoveStartPos.y});

    if (m_CurrentAnimObj)
    {
        //m_CurrentAnimObj->GetComponent<TransformComponent>()->SetPosition({ m_MoveStartPos.x + m_ScrollSpeed, m_MoveStartPos.y + 40.f });
        m_CurrentAnimObj->GetComponent<AnimationComponent>()->Play("idle");
    }

    m_Moved = 0.f;
    m_Initialized = false;
}
