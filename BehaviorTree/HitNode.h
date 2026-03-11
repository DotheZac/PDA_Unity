#pragma once
#include "SkillNode.h"

class HitNode : public SkillNode
{
public:
	HitNode(std::string name) : SkillNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;

private:
	void Reset();


	float m_prevHP = -1.f;
	float m_currHP;

	std::shared_ptr<GameObject> m_Boss_Main;
	std::shared_ptr<GameObject> m_Boss_Shot;

};

