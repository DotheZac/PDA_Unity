#pragma once
#include "BlackBoard.h"

class Telegraph;
class GameObject;
class GraffitiObject;
class EventDispatcher;

class BossBlackBoard : public BlackBoard
{
public:
	BossBlackBoard() = delete;
	BossBlackBoard(
		float scrollspeed,
		std::vector<std::shared_ptr<GraffitiObject>>& graffitis,
		EventDispatcher& eventdispatcher,
		std::vector<std::shared_ptr<Telegraph>>& telegraphs,
		std::vector<std::shared_ptr<GameObject>>& anims, 
		std::vector<std::shared_ptr<GameObject>>& fires,
		std::unordered_map<std::string, std::shared_ptr<GameObject>>& backgrounds, 
		std::unordered_map<std::string, std::vector<int>>& animIndexMap,
		SoundManager& soundmanager);
	virtual ~BossBlackBoard() = default;

	EventDispatcher& GetEventDispatcher() { return m_EventDispatcher; }
private:
	EventDispatcher& m_EventDispatcher;
};

