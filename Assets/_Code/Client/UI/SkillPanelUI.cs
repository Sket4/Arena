using System.Collections;
using Arena;
using Arena.Client.Abilities;
using TzarGames.GameCore.Abilities;
using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.UI
{
    public class SkillPanelUI : GameUIBase
    {
        [System.Serializable]
        class SkillInfoUI
        {
            public UsePlayerAbility UseSkill = default;
            public SkillButtonUI SkillButton = default;
            public byte SlotIndex = 0;
        }

        [SerializeField]
        SkillInfoUI[] skillButtons = default;

        Coroutine coroutine;
        bool initialized = false;

        private void OnEnable()
        {
            if(OwnerEntity == Entity.Null)
            {
                return;
            }

            if(initialized == false)
            {
                if(coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                coroutine = StartCoroutine(init());
            }
        }

        IEnumerator init()
        {
            while (HasData<AbilityElement>() == false)
            {
                yield return null;
            }

            var abilities = GetBuffer<AbilityElement>();

            if (abilities.Length == 0)
            {
                yield return null;
                abilities = GetBuffer<AbilityElement>();
            }

            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (abilities.Length <= i)
                {
                    Debug.LogError("Not enough abilities");
                    break;
                }
                
                var skillButton = skillButtons[i];

                Entity targetAbility = Entity.Null;

                foreach(var ability in abilities)
                {
                    var abilityEntity = ability.AbilityEntity;

                    var slot = GetData<Slot>(abilityEntity);

                    if(slot.Value == skillButton.SlotIndex)
                    {
                        targetAbility = abilityEntity;
                        break;
                    }
                }
                
                if (targetAbility == Entity.Null)
                {
                    Debug.LogError("No skill instance at index " + i);
                    continue;
                }

                skillButton.SkillButton.SetSkillInstance(targetAbility, EntityManager);
                skillButton.UseSkill.SetDefaultSkill(targetAbility, EntityManager);
            }
            coroutine = null;
            initialized = true;
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);

            if(enabled == false || gameObject.activeInHierarchy == false)
            {
                return;
            }

            if(coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            coroutine = StartCoroutine(init());
        }
    }
}
