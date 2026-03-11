#include "BossCoolDown.h"
#include "BlackBoard.h"
#include <random>
#include "GraffitiObject.h"
#include "SpriteRenderer.h"
#include "GraffitiComponent.h"

NodeState BossCoolDown::Tick(BlackBoard& bb, float deltaTime)
{
    float coolDown = bb.GetValue<float>("IdleCoolDown").value();
    float elapsedTime = bb.GetValue<float>("ElapsedIdleTime").value();

    elapsedTime += deltaTime;

    auto graffitis = bb.GetValue< std::vector<std::shared_ptr<GraffitiObject>>>("Graffiti").value();

    if (elapsedTime < coolDown)
    {
        bb.SetValue("CanBeHit", true);

        if (bb.GetValue<int>("CurrPhase").value() == 1)
        {
            auto graffiti = graffitis[0];
            graffiti->GetComponent<GraffitiComponent>()->SetIsActive(true);
            graffiti->GetComponent<SpriteRenderer>()->SetOpacity(1.0f);
        }

        if (bb.GetValue<int>("CurrPhase").value() == 3)
        {
            auto graffiti = graffitis[1];
            graffiti->GetComponent<GraffitiComponent>()->SetIsActive(true);
            graffiti->GetComponent<SpriteRenderer>()->SetOpacity(1.0f);
        }


        bb.SetValue("ElapsedIdleTime", elapsedTime);
        return NodeState::Running;
    }

    for (auto& graffiti : graffitis)
    {
        graffiti->GetComponent<SpriteRenderer>()->SetOpacity(0.f);
        graffiti->GetComponent<GraffitiComponent>()->SetIsActive(false);

    }
    bb.SetValue("CanBeHit", false);


    // 스킬 가중치 1~5 가져오기
    float skillWeight_1 = bb.GetValue<float>("SkillWeight_1").value();
    float skillWeight_2 = bb.GetValue<float>("SkillWeight_2").value();
    float skillWeight_3 = bb.GetValue<float>("SkillWeight_3").value();
    float skillWeight_4 = bb.GetValue<float>("SkillWeight_4").value();
    float skillWeight_5 = bb.GetValue<float>("SkillWeight_5").value();

    float sum = skillWeight_1 + skillWeight_2 + skillWeight_3 + skillWeight_4 + skillWeight_5;

    float chance1 = skillWeight_1 / sum;
    float chance2 = (skillWeight_1 + skillWeight_2) / sum;
    float chance3 = (skillWeight_1 + skillWeight_2 + skillWeight_3) / sum;
    float chance4 = (skillWeight_1 + skillWeight_2 + skillWeight_3 + skillWeight_4) / sum;
    float chance5 = (skillWeight_1 + skillWeight_2 + skillWeight_3 + skillWeight_4 + skillWeight_5) / sum;

    bb.SetValue("SkillChance_1", chance1);
    bb.SetValue("SkillChance_2", chance2);
    bb.SetValue("SkillChance_3", chance3);
    bb.SetValue("SkillChance_4", chance4);
    bb.SetValue("SkillChance_5", chance5);

    static std::random_device rd;
    static std::mt19937 gen(rd());
    static std::uniform_real_distribution<float> dist(0.0f, 1.0f);

    float randVal = dist(gen);
    std::cout << randVal << std::endl;

    bb.SetValue("RandomValue", randVal);

    bb.SetValue("ElapsedIdleTime", 0.f);

    return NodeState::Success;
}
