#include "CompositeNode.h"

void CompositeNode::AddChild(std::shared_ptr<BTNode> child)
{
	m_Children.push_back(child);
}
