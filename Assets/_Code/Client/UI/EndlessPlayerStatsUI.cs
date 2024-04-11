using Arena;
using TzarGames.Common;
using TzarGames.Common.UI;
using UnityEngine;

namespace Arena.Client.UI
{
    public class EndlessPlayerStatsUI : TzarGames.GameFramework.UI.CharacterStatsUI
    {
        [SerializeField]
        TextUI blockChance = default;

        [SerializeField]
        GameObject blockChanceContainer = default;

        float lastBlockChance = float.MaxValue;

        protected override void UpdateCharacterStats()
        {
            base.UpdateCharacterStats();

            var characterClass = GetData<CharacterClassData>();
            
            if (characterClass.Value == CharacterClass.Knight)
            {
                if(blockChanceContainer != null && blockChanceContainer.activeSelf == false)
                {
                    blockChanceContainer.SetActive(true);
                }
            
                if (blockChance != null && blockChance.gameObject.activeInHierarchy)
                {
                    //Debug.LogError("Not implemented");
                    var currentBlockChance = 0;//characterTemplate.BlockChance;
                    if (Mathf.Abs(currentBlockChance - lastBlockChance) > FMath.KINDA_SMALL_NUMBER)
                    {
                        lastBlockChance = currentBlockChance;
                        blockChance.text = string.Format("{0}%", currentBlockChance);
                    }
                }
            }
            else
            {
                if(blockChanceContainer != null && blockChanceContainer.activeSelf)
                {
                    blockChanceContainer.SetActive(false);
                }
            }
        }
    }
}
