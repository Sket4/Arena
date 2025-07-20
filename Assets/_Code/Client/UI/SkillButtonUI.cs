using TzarGames.GameCore.Abilities;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Arena
{
    public class SkillButtonUI : MonoBehaviour
    {
        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private Image skillIconImage = default;
        [SerializeField] private Image cooldownImage = default;
        [SerializeField] private Color defaultCooldownTint = Color.gray;
        
        private Entity skillInstance;
        private EntityManager manager;
        private bool update = false;

        public void SetSkillInstance(Entity newSkill, EntityManager manager)
        {
            if (newSkill == Entity.Null)
            {
                update = false;
                skillIconImage.sprite = defaultIcon;
                return;
            }
            
            this.manager = manager;
            skillInstance = newSkill;

            if (manager.HasComponent<AbilityIcon>(newSkill))
            {
                skillIconImage.sprite = manager.GetSharedComponentManaged<AbilityIcon>(newSkill).Sprite;
            }
            else
            {
                Debug.LogError($"No icon set for ability {newSkill}");
            }
            update = true;
            
            if (cooldownImage != null)
            {
                if (manager.HasComponent<AbilityDisabledIcon>(newSkill))
                {
                    cooldownImage.color = Color.white;
                    cooldownImage.sprite = manager.GetSharedComponentManaged<AbilityDisabledIcon>(newSkill).Sprite;    
                }
                else
                {
                    cooldownImage.color = defaultCooldownTint;
                    cooldownImage.sprite = skillIconImage.sprite;
                }
            }
        }
        
        private void Update()
        {
            if (update == false)
            {
                return;
            }

            // var activable = skillInstance as IActivableSkill;
            // if(activable != null)
            // {
            //     if(activable.IsActive)
            //     {
            //         skillIconImage.sprite = activable.ActivatedIcon;
            //         cooldownImage.enabled = false;
            //     }
            //     else
            //     {
            //         skillIconImage.sprite = skillInstance.Icon;
            //         cooldownImage.enabled = true;
            //     }
            // }

            if (cooldownImage != null && manager.HasComponent<AbilityCooldown>(skillInstance))
            {
                var cooldown = manager.GetComponentData<AbilityCooldown>(skillInstance);

                if (cooldown.IsRunning)
                {
                    cooldownImage.fillAmount = 1.0f - cooldown.Elapsed / cooldown.Time;    
                }
                else
                {
                    cooldownImage.fillAmount = 0;
                }
            }
        }
    }    
}

