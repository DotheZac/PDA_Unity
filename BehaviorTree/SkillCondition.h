#pragma once
#include "BTNode.h"

class SkillCondition : public BTNode
{
public:
	SkillCondition(std::string name) : BTNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;
};

