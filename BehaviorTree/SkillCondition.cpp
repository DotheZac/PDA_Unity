#include "SkillCondition.h"
#include "BlackBoard.h"

NodeState SkillCondition::Tick(BlackBoard& bb, float deltaTime)
{
    float skillchance = 0.f;
    std::string skillName;
    int skillIndex = 0;

    if (m_Name == "Skill_1_Con")
    {
        skillchance = bb.GetValue<float>("SkillChance_1").value();
        skillName = "SkillWeight_1";
        skillIndex = 1;
    }
    else if (m_Name == "Skill_2_Con")
    {
        skillchance = bb.GetValue<float>("SkillChance_2").value();
        skillName = "SkillWeight_2";
        skillIndex = 2;
    }
    else if (m_Name == "Skill_3_Con")
    {
        skillchance = bb.GetValue<float>("SkillChance_3").value();
        skillName = "SkillWeight_3";
        skillIndex = 3;
    }
    else if (m_Name == "Skill_4_Con")
    {
        skillchance = bb.GetValue<float>("SkillChance_4").value();
        skillName = "SkillWeight_4";
        skillIndex = 4;
    }
    else if (m_Name == "Skill_5_Con")
    {
        skillchance = bb.GetValue<float>("SkillChance_5").value();
        skillName = "SkillWeight_5";
        skillIndex = 5;
    }

    float randomValue = bb.GetValue<float>("RandomValue").value();
    
    if (randomValue < skillchance)
    {
        // 실행된 스킬: 가중치 감소
        float weight = bb.GetValue<float>(skillName).value();
        bb.SetValue(skillName, 1.0f); // 1로 초기화

        int n = 0;
        // 실행되지 않은 스킬들: 가중치 증가
        if (bb.GetValue<int>("CurrPhase").value() == 2)
        {
            n = 3;
            bb.SetValue("SkillChance_4", 0.f);
            bb.SetValue("SkillChance_5", 0.f);

        }
        else
        {
            n = 5;
        }

        //가중치 코드
        //std::cout << "[SkillCondition] 현재 스킬 가중치 상태:" << std::endl;
        //for (int i = 1; i <= n; ++i)
        //{
        //    std::string skillKey = "SkillWeight_" + std::to_string(i);
        //    auto valOpt = bb.GetValue<float>(skillKey);
        //    if (valOpt.has_value())
        //    {
        //        std::cout << "  " << skillKey << ": " << valOpt.value() << std::endl;
        //    }
        //    else
        //    {
        //        std::cout << "  " << skillKey << ": (값 없음)" << std::endl;
        //    }
        //}

        for (int i = 1; i <= n; ++i)
        {
            if (i == skillIndex) continue; // 실행된 스킬은 건너뜀

            std::string otherSkill = "SkillWeight_" + std::to_string(i);
            float otherWeight = bb.GetValue<float>(otherSkill).value();
            bb.SetValue(otherSkill, otherWeight + 1.0f);
        }

        return NodeState::Success;
    }

    return NodeState::Failure;
}

