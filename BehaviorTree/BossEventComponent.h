#pragma once
#include "Event.h"
#include "Component.h"
#include "IEventListener.h"

class BlackBoard;

class BossEventComponent : public Component, public IEventListener
{
public:
	BossEventComponent(BlackBoard* bb);
	virtual ~BossEventComponent();
	void OnEvent(EventType type, const void* data);
	void Update(float deltaTime) {}
	void Start();

	const char* GetTypeName() const { return "asdf"; }

	virtual void Serialize(nlohmann::json& j) const {}
	virtual void Deserialize(const nlohmann::json& j) {}
private:
	BlackBoard* m_BlackBoard = nullptr;
};

