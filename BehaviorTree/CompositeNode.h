#pragma once
#include "BTNode.h"

class CompositeNode : public BTNode
{
public:
	CompositeNode(std::string name) : BTNode(name) {}
	void AddChild(std::shared_ptr<BTNode> child);

    void Reset() override
    {
        for (auto& child : m_Children)
        {
            if (child)
                child->Reset();
        }
    }
protected:
	std::vector<std::shared_ptr<BTNode>> m_Children;
};

