#pragma once
#include <memory>

class BlackBoard;
class BTNode;

class BossBehaviorTree
{
public:
	BossBehaviorTree(BlackBoard& blackBoard) : m_BlackBoard(blackBoard) {}

	void Initialize();

	void Tick(float deltaTime);
private:
	BlackBoard& m_BlackBoard;
	std::shared_ptr<BTNode> m_BehaviorTree;
};

