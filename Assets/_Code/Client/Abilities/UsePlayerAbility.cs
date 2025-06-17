using System.Collections;
using TzarGames.Common.UI;
using TzarGames.GameCore.Abilities;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Abilities
{
    [DefaultExecutionOrder(-50)]
    public class UsePlayerAbility : CircleTouchButtonBase
    {
        Entity defaultSkill = Entity.Null;

        private EntityManager manager;
        private World world;
        private Coroutine updateCoroutine;
        private AbilityID id;

        private void OnDisable()
        {
            cancel();
        }

        void cancel()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
            if(world != null &&
               world.IsCreated
                && manager.Exists(defaultSkill))
            {
                var owner = manager.GetComponentData<AbilityOwner>(defaultSkill);
                var input = manager.GetComponentData<PlayerInput>(owner.Value);
                input.PendingAbilityID = AbilityID.Null;
                manager.SetComponentData(owner.Value,input);
            }
        }

        public void SetDefaultSkill(Entity skill, EntityManager manager)
        {
            defaultSkill = skill;
            
            if (defaultSkill == Entity.Null)
            {
                return;    
            }
            
            this.manager = manager;
            world = manager.World;
            id = manager.GetComponentData<AbilityID>(defaultSkill);
        }

        public void UseDefaultSkill()
        {
            if (defaultSkill == Entity.Null)
            {
                return;
            }

            cancel();
            updateCoroutine = StartCoroutine(update());
        }

        IEnumerator update()
        {
            while (true)
            {
                var owner = manager.GetComponentData<AbilityOwner>(defaultSkill);
                var input = manager.GetComponentData<PlayerInput>(owner.Value);
                input.PendingAbilityID = id;
                manager.SetComponentData(owner.Value, input);
                yield return null;
            }
        }

        public void StopUsingSkill()
        {
            cancel();
        }

        protected override void OnPointerDown(Vector2 position)
        {
            UseDefaultSkill();
        }

        protected override void OnDrag(Vector2 position, Vector2 delta)
        {
        }

        protected override void OnPointerUp(Vector2 position)
        {
            StopUsingSkill();
        }
    }    
}
