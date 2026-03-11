#pragma once
#include <vector>
#include <memory>
#include <iostream>



class BlackBoard;


enum class NodeState
{
	Success,
	Failure,
	Running,
};

class BTNode
{
public :
	BTNode(std::string name) : m_Name(name) {}

	virtual ~BTNode() = default;
	virtual NodeState Tick(BlackBoard& bb, float deltaTime) = 0;

	virtual void Reset() {}
protected:
	std::string m_Name;

};

