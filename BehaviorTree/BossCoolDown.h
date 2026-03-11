#pragma once
#include "BTNode.h"

class BossCoolDown : public BTNode
{
public:
	BossCoolDown(std::string name) : BTNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;
};

