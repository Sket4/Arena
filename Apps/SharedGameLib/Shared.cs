namespace Arena
{
    public enum CharacterClass : short
    {
        Knight = 1,
        Mage = 2,
        Archer = 4,
    }

    public enum ItemMetaKeys
    {
        Activated,
        Color
    }

    public enum GameLocationType
    {
        Arena,
        SafeZone
    }

    public class Zone
    {
        public Zone(GameLocationType mainArea, params GameLocationType[] subAreas)
        {
            MainArea = mainArea;
            SubAreas = subAreas;
        }

        public GameLocationType MainArea { get; private set; }
        public GameLocationType[] SubAreas { get; private set; }

        public bool HasArea(GameLocationType area)
        {
            if(area == MainArea)
            {
                return true;
            }

            foreach(var sub in SubAreas)
            {
                if(sub == area)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static class ZoneLibrary
    {
        static Zone[] zones =
        {
            // малая игра
            new Zone(GameLocationType.Arena),
            
            // большая игра
            //new Zone(
            //    Area.Town_1, 
            //    Area.Town_1_Catacombs, 
            //    Area.Town_1_Cemetery, 
            //    Area.Town_1_Lighthouse,
            //    Area.Town_1_AncientTemple,
            //    Area.Town_1_Bridge,
            //    Area.Town_1_FishmanVillage,
            //    Area.Town_1_LakeVillage,
            //    Area.Town_1_Waterfall
            //    )
        };

        public static Zone GetZone(GameLocationType mainLocation)
        {
            foreach(var zone in zones)
            {
                if(zone.MainArea == mainLocation)
                {
                    return zone;
                }
            }
            return null;
        }

        public static Zone GetStartZone()
        {
            return zones[0];
        }

        public static bool SupportsMatchmaking(GameLocationType area)
        {
            foreach (var zone in zones)
            {
                if(zone.HasArea(area))
                {
                    return area != zone.MainArea;
                }
            }

            return false;
        }
    }
}
