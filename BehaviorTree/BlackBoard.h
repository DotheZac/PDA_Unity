#pragma once
#include <string>
#include <unordered_map>
#include <optional>
#include <memory>
#include <variant>
#include "SoundManager.h"
#include "EventDispatcher.h"

class Telegraph;
class GameObject;
class GraffitiObject;

// 행동트리 노드들이 공유하는 데이터 저장소
// AI가 판단하거나 행동할 때 참고하는 모든 상태, 변수, 정보들을 담음
class BlackBoard
{
public:
    using BBValue = std::variant
        <
        int,
        float,
        bool,
        EventDispatcher,
        std::string, 
        std::vector<std::string>,
        std::vector<std::shared_ptr<Telegraph>>,
        std::vector<std::shared_ptr<GraffitiObject>>,
        std::vector<std::shared_ptr<GameObject>>,
        std::unordered_map<std::string, std::shared_ptr<GameObject>>,
        std::unordered_map<std::string, std::vector<int>>,
        std::vector<int>
        >;

public:

    BlackBoard(SoundManager& soundmanager) : m_SoundManager(soundmanager) {}
    virtual ~BlackBoard() = default;
	void SetValue(const std::string& key, const BBValue& value);

    // 데이터 확인
    template<typename T>
    std::optional<T> GetValue(const std::string& key) const {
        auto it = m_Data.find(key);
        if (it == m_Data.end()) return std::nullopt;

        if (auto val = std::get_if<T>(&(it->second))) {
            return *val;
        }
        return std::nullopt;
    }


    // 데이터 존재 여부
    bool HasKey(const std::string& key) const;
    SoundManager& GetSoundManager() { return m_SoundManager; }

protected:
	std::unordered_map<std::string, BBValue> m_Data;
    SoundManager& m_SoundManager;

};

