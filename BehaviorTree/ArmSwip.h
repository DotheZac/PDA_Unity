#pragma once
#include "SkillNode.h"
#include "Telegraph.h"
#include "GameObject.h"


class ArmSwip : public SkillNode
{
public:
	ArmSwip(std::string name) : SkillNode(name) {}

	NodeState Tick(BlackBoard& bb, float deltaTime) override;

private:
	void StartWarning(BlackBoard& bb) override;
	void EndWarning(BlackBoard& bb) override;
	void Reset() override;

	//움직일 노드 인덱스와 도착할 인덱스
	std::vector<int> m_MoveStartIndex;
	std::vector<int> m_TargetIndex;

	//위치값 저장
	std::vector<Math::Vector2F> m_MoveStartPos;
	std::vector<Math::Vector2F> m_MoveTargetPos;

	bool m_IsMoving = false;
	bool m_Forward = true;       // 처음은 목표 방향으로 이동
	bool m_HasReturned = false;  // 아직 돌아오지 않음

	float m_MoveTimer = 0.f;
	float m_MoveDuration = 0.7f;

	std::shared_ptr<GameObject> m_CurrentAnimObj;
	std::shared_ptr<GameObject> m_Boss_Arm;

};

