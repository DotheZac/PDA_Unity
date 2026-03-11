#include "BossEventComponent.h"
#include "BlackBoard.h"
#include "BossBlackBoard.h"
#include "EventDispatcher.h"
#include "PlayerObject.h"


BossEventComponent::BossEventComponent(BlackBoard* bb)
{
    m_BlackBoard = bb;
}

BossEventComponent::~BossEventComponent()
{
    auto bbb = dynamic_cast<BossBlackBoard*>(m_BlackBoard);
    bbb->GetEventDispatcher().RemoveListener(EventType::OnBossAttacked, this);
}

void BossEventComponent::OnEvent(EventType type, const void* data)
{
    if (type == EventType::OnBossAttacked)
    {
        auto attackInfo = static_cast<const PlayerAttackInfo*>(data);

        float currentHP = m_BlackBoard->GetValue<float>("BossCurrHP").value();
        m_BlackBoard->SetValue("BossCurrHP", currentHP - attackInfo->damage);
    }
}

void BossEventComponent::Start()
{
    auto bbb = dynamic_cast<BossBlackBoard*>(m_BlackBoard);
    bbb->GetEventDispatcher().AddListener(EventType::OnBossAttacked, this);
}
