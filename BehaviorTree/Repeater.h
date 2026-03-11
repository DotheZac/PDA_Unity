#pragma once
#include "Decorator.h"

class Repeater : public Decorator
{
public:
	Repeater(std::string name, int repeat) : Decorator(name), m_TimeToRepeat(repeat) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;

private:
	int m_TimeToRepeat;
	int m_TimesDone = 0;
};

