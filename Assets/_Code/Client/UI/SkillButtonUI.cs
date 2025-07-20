using Arena.Client;
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
        [SerializeField] private Image cooldownBg = default;
        [SerializeField] private Color defaultCooldownTint = Color.gray;
        [SerializeField] private Color defaultColor = Color.chocolate;
        
        private Entity skillInstance;
        private EntityManager manager;
        private bool update = false;
        private Color skillColor;

        public void SetSkillInstance(Entity newSkill, EntityManager manager)
        {
            if (newSkill == Entity.Null)
            {
                update = false;
                skillIconImage.sprite = defaultIcon;
                skillIconImage.color = defaultColor;
                return;
            }
            
            this.manager = manager;
            skillInstance = newSkill;

            if (this.manager.HasComponent<ColorData>(newSkill))
            {
                skillColor = manager.GetComponentData<ColorData>(newSkill).Color;
            }
            else
            {
                skillColor = Color.white;
            }
            skillIconImage.color = skillColor;

            if (cooldownBg)
            {
                var bgc = cooldownBg.color;
                cooldownBg.color = new Color(skillColor.r, skillColor.g, skillColor.b, bgc.a);
                cooldownBg.fillAmount = 0;    
            }

            if (manager.HasComponent<AbilityIcon>(newSkill))
            {
                skillIconImage.sprite = manager.GetSharedComponentManaged<AbilityIcon>(newSkill).Sprite;
            }
            else
            {
                Debug.LogError($"No icon set for ability {newSkill}");
            }
            update = true;
            
            if (cooldownImage)
            {
                cooldownImage.fillAmount = 0;
                
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

            if (cooldownImage && manager.HasComponent<AbilityCooldown>(skillInstance))
            {
                var cooldown = manager.GetComponentData<AbilityCooldown>(skillInstance);

                if (cooldown.IsRunning)
                {
                    var alpha = cooldown.Elapsed / cooldown.Time;
                    cooldownImage.fillAmount = 1.0f - alpha;
                    cooldownBg.fillAmount = cooldownImage.fillAmount;

                    const float threshold = 0.95f;
                    if (alpha < threshold)
                    {
                        skillIconImage.color = Color.Lerp(Color.black, Color.gray, alpha * (1.0f / threshold));    
                    }
                    else
                    {
                        skillIconImage.color = Color.Lerp(Color.gray, skillColor, (alpha - threshold) / (1.0f - threshold));    
                    }
                }
                else
                {
                    skillIconImage.color = skillColor;
                    cooldownImage.fillAmount = 0;
                    cooldownBg.fillAmount = 0;
                }
            }
        }
    }    
}

