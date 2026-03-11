#pragma once
#include "CompositeNode.h"

class Selector : public CompositeNode
{
public:
	Selector(std::string name) : CompositeNode(name) {}
	NodeState Tick(BlackBoard& bb, float deltaTime) override;

private:
	int m_CurrIndex = 0;
};

