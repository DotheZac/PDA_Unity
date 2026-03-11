#include "IsDeadNode.h"
#include "AnimationComponent.h"
#include "SpriteRenderer.h"


NodeState IsDeadNode::Tick(BlackBoard& bb, float deltaTime)
{
    if (m_Initialized)
    {
        return NodeState::Failure;
    }

    if (bb.GetValue<float>("BossCurrHP").value() <= 0)
    {
        m_Boss_Main = GetAnim(bb, "Boss_3Phase_IDLE_ani");
        m_Boss_Arm_L = GetAnim(bb, "Boss_Anim_Arm_L");
        m_Boss_Arm_R = GetAnim(bb, "Boss_Anim_Arm_R");

        m_Boss_Main_Die = GetAnim(bb, "Boss_3Phase_DEAD_Ani");
        m_Boss_Arm_L_Die = GetAnim(bb, "Boss_3Phase_Arms_Dead_L");
        m_Boss_Arm_R_Die = GetAnim(bb, "Boss_3Phase_Arms_Dead_R");

        bb.GetSoundManager().SFX_Shot(L"boss_destroy_explosion");



        if (m_Boss_Main)
        {
            auto anim = m_Boss_Main->GetComponent<AnimationComponent>();
            anim->SetIsActive(false);
            auto sprite = m_Boss_Main->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(false);
            sprite->SetOpacity(0.f);
        }
        if (m_Boss_Arm_L)
        {
            auto anim = m_Boss_Arm_L->GetComponent<AnimationComponent>();
            anim->SetIsActive(false);
            auto sprite = m_Boss_Arm_L->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(false);
            sprite->SetOpacity(0.f);
        }

        if (m_Boss_Arm_R)
        {
            auto anim = m_Boss_Arm_R->GetComponent<AnimationComponent>();
            anim->SetIsActive(false);
            auto sprite = m_Boss_Arm_R->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(false);
            sprite->SetOpacity(0.f);
        }

        if (m_Boss_Main_Die)
        {
            auto anim = m_Boss_Main_Die->GetComponent<AnimationComponent>();
            anim->SetIsActive(true);
            anim->Play();

            auto sprite = m_Boss_Main_Die->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(true);
            sprite->SetOpacity(1.f);
        }

        if (m_Boss_Arm_L_Die)
        {
            auto anim = m_Boss_Arm_L_Die->GetComponent<AnimationComponent>();
            anim->SetIsActive(true);
            anim->Play();

            auto sprite = m_Boss_Arm_L_Die->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(true);
            sprite->SetOpacity(1.f);
        }

        if (m_Boss_Arm_R_Die)
        {
            auto anim = m_Boss_Arm_R_Die->GetComponent<AnimationComponent>();
            anim->SetIsActive(true);
            anim->Play();

            auto sprite = m_Boss_Arm_R_Die->GetComponent<SpriteRenderer>();
            sprite->SetIsActive(true);
            sprite->SetOpacity(1.f);
        }

        m_Initialized = true;
    }

    if (m_Boss_Main_Die)
    {
        auto anim = m_Boss_Main_Die->GetComponent<AnimationComponent>();
        if (!anim->IsAnimationFinished())
        {
            return NodeState::Running;
        }
        if (anim->IsAnimationFinished())
        {
            bb.SetValue("IsDead", true);
            return NodeState::Failure;  // 죽음 애니메이션 완료
        }
    }

    return NodeState::Success;
}
