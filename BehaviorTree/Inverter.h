#pragma once
#include "Decorator.h"

class Inverter : public Decorator
{
public:
	Inverter(std::string name) : Decorator(name) {}
	NodeState Tick(BlackBoard& bb, float deltaTime) override;
};

