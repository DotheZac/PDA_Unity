#pragma once
#include "BTNode.h"

class PhaseChecker_1 : public BTNode
{
public:
	PhaseChecker_1(std::string name) : BTNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;
};

