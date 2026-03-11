#pragma once
#include "BTnode.h"

class Decorator : public BTNode
{
public:
	Decorator(std::string name) : BTNode(name) {}
	void SetChild(std::shared_ptr<BTNode> child)
	{
		m_Children = child;
	}

protected:
	std::shared_ptr<BTNode> m_Children;
};

