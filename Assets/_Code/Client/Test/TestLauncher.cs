using Unity.CharacterController;
using TzarGames.AnimationFramework;
using TzarGames.GameCore;
using TzarGames.MatchFramework.Client;
using Unity.Entities;
using Unity.Physics.GraphicsIntegration;
using Hash128 = Unity.Entities.Hash128;

public class TestLauncher : SimpleLocalGameLauncher
{
    protected override GameLoopBase CreateGameLoop(Hash128[] additionalScenes)
    {
        var loop = base.CreateGameLoop(additionalScenes);
        
        loop.EnableSystemManaged<FixedStepSimulationSystemGroup>(false);
        loop.EnableSystemManaged<SimpleAnimationSystem>(false);
        loop.EnableSystemManaged<AnimationSystemGroup>(false);
        loop.EnableSystem<TzarGames.Rendering.SkinnedMeshDeformationSystem>(false);
        loop.EnableSystem<CharacterInterpolationSystem>(false);
        //loop.EnableSystemManaged<DestroyBrokenJointsSystem>(false);
        loop.EnableSystem<SmoothRigidBodiesGraphicalMotion>(false);
        loop.EnableSystemByName("CompanionGameObjectUpdateTransformSystem", false);
        loop.EnableSystemByName("CompanionGameObjectUpdateSystem", false);
        loop.EnableSystemByName("DebugSystem", false);
        loop.EnableSystemByName("DebugDisplaySystem", false);
        
        return loop;
    }
}
