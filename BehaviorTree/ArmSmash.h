#pragma once
#include "SkillNode.h"
#include "Telegraph.h"
#include "GameObject.h"

class ArmSmash : public SkillNode
{
public:
	ArmSmash(std::string name) : SkillNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;

private:
	void StartWarning(BlackBoard& bb) override;
	void EndWarning(BlackBoard& bb) override;
	void Reset() override;

	//텔레그래프 위치 관련
	int m_minIndex = 15;
	int m_maxIndex = 0;

	//이동 관련
	float m_MoveTimer = 0.0f;
	float m_MoveDuration = 0.7f;
	bool m_IsMoving = false;

	Math::Vector2F m_MoveStartPos = { 0,0 };
	Math::Vector2F m_MoveTargetPos = { 0,0 };

	std::shared_ptr<GameObject> m_CurrentAnimObj;

};

