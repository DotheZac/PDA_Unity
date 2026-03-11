#include "HitNode.h"
#include "AnimationComponent.h"
#include "SpriteRenderer.h"

NodeState HitNode::Tick(BlackBoard& bb, float deltaTime)
{
    if (!m_Initialized)
    {
        if (!m_Boss_Main)
        {
            m_Boss_Main = GetAnim(bb, "Boss_3Phase_IDLE_ani");
        }

        if (!m_Boss_Shot)
        {
            m_Boss_Shot = GetAnim(bb, "Boss_3Phase_Shot_Ani");
        }

        m_Initialized = true;
    }
    m_currHP = bb.GetValue<float>("BossCurrHP").value();

    if (m_prevHP < 0) m_prevHP = m_currHP;

    if (m_currHP < m_prevHP)
    {
        if (m_Boss_Main)
        {
            m_Boss_Main->GetComponent<SpriteRenderer>()->SetOpacity(0);
            auto anim = m_Boss_Main->GetComponent<AnimationComponent>();
            anim->SetIsActive(false);
        }

        if (m_Boss_Shot)
        {
            auto anim = m_Boss_Shot->GetComponent<AnimationComponent>();
            anim->SetIsActive(true);
            anim->Play();

            auto sprite = m_Boss_Shot->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(true);
            sprite->SetOpacity(1.f);
        }

        m_prevHP = m_currHP;

        m_AnimPlaying = true;
    }

    if (m_AnimPlaying)
    {
        auto anim = m_Boss_Shot->GetComponent<AnimationComponent>();
        if (anim->IsAnimationFinished())
        {
            anim->SetIsActive(false);
            auto sprite = m_Boss_Shot->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(false);
            sprite->SetOpacity(0.f);

            if (m_Boss_Main)
            {
                auto mainAnim = m_Boss_Main->GetComponent<AnimationComponent>();
                mainAnim->SetIsActive(true);
                mainAnim->Play();

                auto mainSprite = m_Boss_Main->GetComponent<SpriteRenderer>();
                mainSprite->SetIsActive(true);
                mainSprite->SetOpacity(1.f);
            }

            m_AnimPlaying = false;
            m_prevHP = m_currHP;


            Reset();
            return NodeState::Success;
        }
        else
        {
            return NodeState::Running;
        }
    }
    else
    {
        // 피격 애니메이션이 없으면 정상 종료로 처리
        Reset();
        return NodeState::Success;
    }

}

void HitNode::Reset()
{
    __super::Reset();

    m_prevHP = -1;

    m_Initialized = false;
}
