using System.Collections.Generic;
using Unity.Entities;
using System;

namespace Arena
{
    public class CustomBootstrap : ICustomBootstrap
    {
        public bool Initialize(string str)
        {
            var world = new World("Default world");
            World.DefaultGameObjectInjectionWorld = world;
            return true;
        }
    }
}
