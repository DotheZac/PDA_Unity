#pragma once
#include "CompositeNode.h"

class Sequence : public CompositeNode
{
public:
	Sequence(std::string name) : CompositeNode(name) {}
	NodeState Tick(BlackBoard& bb, float deltaTime) override;

private: 
	int m_CurrIndex = 0;
};

