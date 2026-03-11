#pragma once
#include "SkillNode.h"
#include "Telegraph.h"
#include "GameObject.h"

class Pick : public SkillNode
{
public:
	Pick(std::string name) : SkillNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;

private:
	void StartWarning(BlackBoard& bb) override;
	void EndWarning(BlackBoard& bb) override;
	void Reset();

	//현재 재생하는 애니메이션 오브젝트
	std::shared_ptr<GameObject> m_CurrentAnimObj;

	float m_AttackElapsedTime = 0.f;
	bool m_ColliderOn = false;
};

