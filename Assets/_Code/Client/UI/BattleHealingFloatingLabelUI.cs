using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using TzarGames.GameCore;
using Unity.Transforms;

namespace Arena.Client.UI
{
    public class BattleHealingFloatingLabelUI : MonoBehaviour//, Arena.Skills.IBattleHealingEventHandler
    {
        [SerializeField]
        FloatingLabelScreenUI labelScreen = default;

        [SerializeField]
        Color color = Color.green;

        public void OnCharacterHealed(Entity character, float healValue)
        {
            if (character != labelScreen.OwnerEntity || enabled == false)
            {
                return;
            }
            
            var text = string.Format("+{0} HP", (int)healValue);
            var pos = labelScreen.GetData<LocalTransform>();
            labelScreen.AddCommonLabel(text, color, pos.Position + math.up());
        }

        private void Awake()
        {
            //Common.EventSystem.Event<Arena.Skills.IBattleHealingEventHandler>.AddHandler(this);
        }
        private void OnDestroy()
        {
            //Common.EventSystem.Event<Arena.Skills.IBattleHealingEventHandler>.RemoveHandler(this);
        }
    }
}

