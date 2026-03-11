#include "BossBlackBoard.h"

BossBlackBoard::BossBlackBoard(
	float scrollspeed,
	std::vector<std::shared_ptr<GraffitiObject>>& graffitis,
	EventDispatcher& eventdispatcher,

	std::vector<std::shared_ptr<Telegraph>>& telegraphs, 
	std::vector<std::shared_ptr<GameObject>>& anims, 
	std::vector<std::shared_ptr<GameObject>>& fires,
	std::unordered_map<std::string, std::shared_ptr<GameObject>>& backgrounds,
	std::unordered_map<std::string, std::vector<int>>& animIndexMap,
	SoundManager& soundmanager)
	: BlackBoard(soundmanager), m_EventDispatcher(eventdispatcher)
{

	SetValue("EventDispatcher", eventdispatcher);

	SetValue("Graffiti", graffitis);

	SetValue("ScrollSpeed", scrollspeed);

	SetValue("CanBeHit", false);

	SetValue("IsDead", false);
	SetValue("BossMaxHP", 100.0f);
	SetValue("BossCurrHP", GetValue<float>("BossMaxHP").value());

	//보스 쿨다운
	SetValue("IdleCoolDown", 1.0f);

	//보스 공격 장판 전체
	SetValue("BossTelegraph", telegraphs);

	//보스 애니메이션 저장
	SetValue("LazerFire", fires);

	SetValue("BossAnims", anims);
	SetValue("BossAnimIndexMap", animIndexMap);

	//배경 저장
	SetValue("Backgrounds", backgrounds);

	//보스 패턴 활성화 지정?
	// 나중에 패턴 이름있으면 여기에 Skill_1 대신 입력
	SetValue("Row_1", std::vector<int>{0, 1, 2, 3, 4});
	SetValue("Row_2", std::vector<int>{5, 6, 7, 8, 9});
	SetValue("Row_3", std::vector<int>{10, 11, 12, 13, 14});

	SetValue("Pick_0", std::vector<int>{0});
	SetValue("Pick_1", std::vector<int>{1});
	SetValue("Pick_3", std::vector<int>{3});
	SetValue("Pick_4", std::vector<int>{4});
	SetValue("Pick_5", std::vector<int>{5});
	SetValue("Pick_6", std::vector<int>{6});
	SetValue("Pick_7", std::vector<int>{7});
	SetValue("Pick_8", std::vector<int>{8});
	SetValue("Pick_9", std::vector<int>{9});
	SetValue("Pick_10", std::vector<int>{10});
	SetValue("Pick_14", std::vector<int>{14});

	SetValue("Swip_L", std::vector<int>{0, 1, 2, 5, 6, 7, 10, 11, 12});
	SetValue("Swip_R", std::vector<int>{2, 3, 4, 7, 8, 9, 12, 13, 14});


	//쿨다운 경과 시간
	SetValue("ElapsedIdleTime", 0.0f);
	SetValue("WarningTime", 3.0f);


	SetValue("3Phase", false);
	SetValue("RandomValue", 0.0f);

	SetValue("SkillWeight_1", 1.0f);
	SetValue("SkillWeight_2", 1.0f);
	SetValue("SkillWeight_3", 1.0f);
	SetValue("SkillWeight_4", 5001.0f);
	SetValue("SkillWeight_5", 1.0f);

	SetValue("SkillChance_1", 0.f);
	SetValue("SkillChance_2", 0.f);
	SetValue("SkillChance_3", 0.f);
	SetValue("SkillChance_4", 0.f);
	SetValue("SkillChance_5", 0.f);

	SetValue("CurrPhase", 0);
}



