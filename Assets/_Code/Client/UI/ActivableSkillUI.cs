using TzarGames.GameFramework.UI;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.UI
{
    public class ActivableSkillUI : GameUIBase
    {
        Entity skillInstance = default;

        [SerializeField]
        GameObject[] activableObjects = default;

        protected override void Start()
        {
            base.Start();
            check();
        }

        void Update()
        {
            check();
        }

        void check()
        {
            // bool enable = true;

            // if(skillInstance == Entity.Null)
            // {
            //     if (OwnerEntity != null)
            //     {
            //         skillInstance = OwnerEntity.TemplateInstance.GetSkillInstance(skill);
            //         if(skillInstance == Entity.Null)
            //         {
            //             enable = false;
            //         }
            //     }
            //     else
            //     {
            //         enable = false;
            //     }
            // }

            // if(enable)
            // {
            //     var activable = skillInstance as Skills.IActivableSkill;
            //     if (activable != null)
            //     {
            //         enable = activable.IsActive;
            //     }
            // }

            // for (int i = 0; i < activableObjects.Length; i++)
            // {
            //     var obj = activableObjects[i];
            //     obj.SetActive(enable);
            // }
        }

        protected override void OnSetup(Entity ownerEntity, Entity uiEntity, EntityManager manager)
        {
            base.OnSetup(ownerEntity, uiEntity, manager);
            check();
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            check();
        }
    }
}
