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
        }

        [SerializeField] private SkillInfoUI attack;
        [SerializeField] private SkillInfoUI ability1;
        [SerializeField] private SkillInfoUI ability2;
        [SerializeField] private SkillInfoUI ability3;
        

        Coroutine coroutine;

        private void OnEnable()
        {
            if(OwnerEntity == Entity.Null)
            {
                return;
            }

            UpdateData();
        }

        IEnumerator init()
        {
            while (HasData<TzarGames.GameCore.Abilities.AbilityArray>() == false)
            {
                yield return null;
            }

            var playerAbilities = GetData<PlayerAbilities>();

            attack.SkillButton.SetSkillInstance(playerAbilities.AttackAbility.Ability, EntityManager);
            attack.UseSkill.SetDefaultSkill(playerAbilities.AttackAbility.Ability, EntityManager);
            
            ability1.SkillButton.SetSkillInstance(playerAbilities.Ability1.Ability, EntityManager);
            ability1.UseSkill.SetDefaultSkill(playerAbilities.Ability1.Ability, EntityManager);
            
            ability2.SkillButton.SetSkillInstance(playerAbilities.Ability2.Ability, EntityManager);
            ability2.UseSkill.SetDefaultSkill(playerAbilities.Ability2.Ability, EntityManager);
            
            ability3.SkillButton.SetSkillInstance(playerAbilities.Ability3.Ability, EntityManager);
            ability3.UseSkill.SetDefaultSkill(playerAbilities.Ability3.Ability, EntityManager);
            
            coroutine = null;
        }

        public void UpdateData()
        {
            if(coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            coroutine = StartCoroutine(init());
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);

            if(enabled == false || gameObject.activeInHierarchy == false)
            {
                return;
            }

            UpdateData();
        }
    }
}
