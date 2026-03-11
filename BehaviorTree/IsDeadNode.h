#pragma once
#include "SkillNode.h"

class IsDeadNode : public SkillNode
{
public:
	IsDeadNode(std::string name) : SkillNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;
private:
	std::shared_ptr<GameObject> m_Boss_Main;
	std::shared_ptr<GameObject> m_Boss_Arm_L;
	std::shared_ptr<GameObject> m_Boss_Arm_R;

	std::shared_ptr<GameObject> m_Boss_Main_Die;
	std::shared_ptr<GameObject> m_Boss_Arm_L_Die;
	std::shared_ptr<GameObject> m_Boss_Arm_R_Die;

};

