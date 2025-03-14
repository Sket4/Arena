using System;
using TzarGames.GameCore;
using Unity.Entities;

namespace Arena
{
    [Serializable]
    public struct Gender : IComponentData
    {
        //[HideInAuthoring]
        public Genders Value;

        public Gender(Genders genders)
        {
            Value = genders;
        }
    }
    public class GenderComponent : ComponentDataBehaviour<Gender>
    {
    }
}
