#include "Lazer.h"
#include "BlackBoard.h"
#include "AnimationComponent.h"
#include "SpriteRenderer.h"
#include "TransformComponent.h"

NodeState Lazer::Tick(BlackBoard& bb, float deltaTime)
{
#pragma region Initialize
    if (!m_Initialized)
    {
        m_Telegraphs = bb.GetValue<std::vector<std::shared_ptr<Telegraph>>>("BossTelegraph").value();
        m_Fires = bb.GetValue<std::vector<std::shared_ptr<GameObject>>>("LazerFire").value();
        m_AttackRange = bb.GetValue<std::vector<int>>(m_Name).value();
        m_minIndex = m_AttackRange.front();
        m_maxIndex = m_AttackRange.back();

        m_StartTelIndex = (m_minIndex + m_maxIndex) / 2;


        if (!m_Boss_Main)
        {
            if (bb.GetValue<int>("CurrPhase").value() == 1)
            {
                m_Boss_Main = GetAnim(bb, "Boss_Phase_1_Main");
                m_Lazer_CCTV = GetAvailableAnim(bb, "Boss_Lazer_CCTV");

            }
            else if (bb.GetValue<int>("CurrPhase").value() == 3)
            {
                m_Boss_Main = GetAnim(bb, "Boss_3Phase_IDLE_ani");
                m_Lazer_CCTV = GetAvailableAnim(bb, "Boss_3Phase_Laser_CCTV");

            }
        }




        m_Initialized = true;
    }

#pragma endregion

    if (!m_HasStarted)
    {
        bb.GetSoundManager().SFX_Shot(L"boss_laser_charge");
        bb.GetSoundManager().SFX_Shot(L"boss_warning_beep");

        StartWarning(bb);
        m_HasStarted = true;
    }

    m_ElapsedTime += deltaTime;

    //공격 장판 활성화
    if (m_IsActivating)
    {
        // 첫 프레임: 중앙 활성화
        if (m_ActivateStep == 0)
        {
            bb.GetSoundManager().SFX_Shot(L"boss_laser_explosion");
            m_Telegraphs[m_StartTelIndex]->SetColliderActive(true);
            ActiveAnimation(m_StartTelIndex, true);
            m_PrevLeft = m_StartTelIndex;
            m_PrevRight = m_StartTelIndex;
            m_ActivateStep = 1;
        }

        // 다음 프레임들: 좌우 확장
        m_ActivateTimer += deltaTime;
        if (m_ActivateTimer >= 0.3f)
        {
            int curLeft = m_StartTelIndex - m_ActivateStep;
            int curRight = m_StartTelIndex + m_ActivateStep;

            if (curLeft >= m_minIndex)
            {
                m_Telegraphs[curLeft]->SetColliderActive(true);
                ActiveAnimation(curLeft, true);
            }
            if (curRight <= m_maxIndex)
            {
                m_Telegraphs[curRight]->SetColliderActive(true);
                ActiveAnimation(curRight, true);

            }

            // 이전 거 끄기
            if (m_PrevLeft >= 0) 
            {
                m_Telegraphs[m_PrevLeft]->SetColliderActive(false);
                ActiveAnimation(m_PrevLeft, false);

            }
            if (m_PrevRight >= 0) 
            {
                m_Telegraphs[m_PrevRight]->SetColliderActive(false);
                ActiveAnimation(m_PrevRight, false);

            }

            m_PrevLeft = curLeft;
            m_PrevRight = curRight;

            m_ActivateStep++;
            m_ActivateTimer = 0.0f;

            // 다 했으면 종료
            // 하드코딩임 고쳐야 함
            if (curLeft < m_minIndex && curRight > m_maxIndex)
            {
                m_IsActivating = false;
            }
        }
    }



    if (m_AnimPlaying && m_CurrentAnimObj)
    {
        auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();
        if (animComp->IsAnimationFinished())
        {
            animComp->SetIsActive(false);
            auto sprite = m_CurrentAnimObj->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(false);
            sprite->SetOpacity(0.f);

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

    if (m_ElapsedTime >= m_WarningTime)
    {
        if (!m_AttackStarted)
        {
            bb.GetSoundManager().SFX_Shot(L"boss_laser_fire");

            EndWarning(bb);
            m_AttackStarted = true;
        }

        // 실행 중인 애니메이션이 하나라도 남아 있으면 계속 Running
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

void Lazer::StartWarning(BlackBoard& bb)
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
    // 경고 애니메이션 실행
    std::shared_ptr<GameObject> animObj;

    if (bb.GetValue<int>("CurrPhase").value() == 1)
    {
        animObj = GetAvailableAnim(bb, "Boss_Lazer"); 

    }

    else if (bb.GetValue<int>("CurrPhase").value() == 3)
    {
        animObj = GetAvailableAnim(bb, "Boss_Lazer_Phase_3");

    }

    if (animObj)
    {
        m_CurrentAnimObj = animObj;

        //if (!m_AttackRange.empty())
        //{
        //    // 위치 설정 (원하는 위치로 조정 가능)
        //    float posX = m_Telegraphs[m_StartTelIndex]->GetComponent<TransformComponent>()->GetPosition().x + 750.f;
        //    float posY = m_Telegraphs[m_StartTelIndex]->GetComponent<TransformComponent>()->GetPosition().y - 250.f;

        //    animObj->GetComponent<TransformComponent>()->SetPosition({ posX, posY });
        //}

        auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();
        animComp->SetIsActive(true);
        animComp->Play("charge");  // 애니메이션 클립 이름(경고용)
        animComp->SetLoop(true);    // 경고니까 루프 설정 가능

        auto sprite = m_CurrentAnimObj->GetComponent<SpriteRenderer>();
        sprite->SetIsActive(true);
        sprite->SetOpacity(1.f);

        m_AnimPlaying = true;
    }

    if (m_Boss_Main)
    {
        m_Boss_Main->GetComponent<SpriteRenderer>()->SetOpacity(0);
        auto anim = m_Boss_Main->GetComponent<AnimationComponent>();
        anim->SetIsActive(false);

    }


    if (m_Lazer_CCTV)
    {
        m_Lazer_CCTV->GetComponent<SpriteRenderer>()->SetOpacity(1);
        auto anim = m_Lazer_CCTV->GetComponent<AnimationComponent>();
        anim->SetIsActive(true);
        
    }
}

void Lazer::EndWarning(BlackBoard& bb)
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

    m_ActivateStep = 0;
    m_ActivateTimer = 0.0f;
    m_IsActivating = true;

    //if (!m_AttackRange.empty())
    //{
    //    float posX = m_Telegraphs[m_StartTelIndex]->GetComponent<TransformComponent>()->GetPosition().x + 750.f;
    //    float posY = m_Telegraphs[m_StartTelIndex]->GetComponent<TransformComponent>()->GetPosition().x - 1300.f;

    //    m_CurrentAnimObj->GetComponent<TransformComponent>()->SetPosition({ posX, posY });
    //}

    auto animComp = m_CurrentAnimObj->GetComponent<AnimationComponent>();

    animComp->Play("fire");
    animComp->SetLoop(false);

    animComp->SetIsActive(true);

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

void Lazer::Reset()
{
    __super::Reset();
    m_ActivateStep = 0;
    m_ActivateTimer = 0.0f;

    if (m_Boss_Main)
    {
        m_Boss_Main->GetComponent<SpriteRenderer>()->SetOpacity(1);
        auto anim = m_Boss_Main->GetComponent<AnimationComponent>();
        anim->SetIsActive(true);

    }


    if (m_Lazer_CCTV)
    {
        m_Lazer_CCTV->GetComponent<SpriteRenderer>()->SetOpacity(0);
        auto anim = m_Lazer_CCTV->GetComponent<AnimationComponent>();
        anim->SetIsActive(false);

    }
}

void Lazer::ActiveAnimation(int index, bool flag)
{
    auto anim = m_Fires[index]->GetComponent<AnimationComponent>();
    if (flag)
    {
        anim->SetIsActive(true);
        anim->Play();
        m_Fires[index]->GetComponent<SpriteRenderer>()->SetOpacity(1);

    }

    else
    {
        anim->SetIsActive(false);
        m_Fires[index]->GetComponent<SpriteRenderer>()->SetOpacity(0);

    }

}

