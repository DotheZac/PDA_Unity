#pragma once
#include "BTNode.h"
#include "Telegraph.h"
#include "GameObject.h"
#include "AnimationUtils.h"

//ÆÈ ³»·ÁÂï±â
class SkillNode : public BTNode
{
public:
	SkillNode(std::string name) : BTNode(name) {}

	virtual NodeState Tick(BlackBoard& bb, float deltaTime) override = 0;

protected:
	virtual void StartWarning(BlackBoard& bb) {}
	virtual void EndWarning(BlackBoard& bb) {}
	virtual void Reset() 
	{
		m_HasStarted = false;
		m_AnimPlaying = false;
		m_AttackStarted = false;
		m_ElapsedTime = 0.f;
		m_AnimTimer = 0.0f;

	}

	//°ø°Ý ¹üÀ§
	std::vector<int> m_AttackRange;

	std::vector<std::shared_ptr<Telegraph>> m_Telegraphs;
	std::vector<std::shared_ptr<GameObject>> m_Anims;

	bool m_HasStarted = false;
	bool m_Initialized = false;
	bool m_AnimPlaying = false;
	bool m_AttackStarted = false;

	float m_WarningTime = 3.f;
	float m_ElapsedTime = 0.f;
	float m_AnimTimer = 0.0f;
};

