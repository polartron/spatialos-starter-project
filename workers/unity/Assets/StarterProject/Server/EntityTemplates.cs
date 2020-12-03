using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.QueryBasedInterest;
using StarterProject.Shared;
using UnityEngine;

namespace StarterProject.Server
{
    public static class EntityTemplates
    {
        public static EntityTemplate CreatePlayerEntityTemplate(EntityId entityId, string workerId, byte[] serializedArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = WorkerTypes.UnityGameLogic;

            var position = new Vector3(0, 1f, 0);
            var coords = Coordinates.FromUnityVector(position);

            var template = new EntityTemplate();
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            template.AddComponent(new Position.Snapshot(coords), serverAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            template.AddComponent(new ServerUpdate.Snapshot(), serverAttribute);
            template.AddComponent(new ClientUpdate.Snapshot(), clientAttribute);

            var radius = 100;

            var clientSelfInterest = InterestQuery.Query(Constraint.EntityId(entityId)).FilterResults(new[]
            {
                ClientUpdate.ComponentId, ServerUpdate.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId, Position.ComponentId
            });

            var clientRangeInterest = InterestQuery.Query(Constraint.RelativeCylinder(radius)).FilterResults(new[]
            {
                Metadata.ComponentId, Position.ComponentId
            });

            var serverSelfInterest = InterestQuery.Query(Constraint.EntityId(entityId)).FilterResults(new[]
            {
                ServerUpdate.ComponentId, ClientUpdate.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId, Position.ComponentId
            });

            var serverRangeInterest = InterestQuery.Query(Constraint.RelativeCylinder(radius)).FilterResults(new[]
            {
                Metadata.ComponentId, Position.ComponentId
            });

            var interest = InterestTemplate.Create()
                .AddQueries<ClientUpdate.Component>(clientSelfInterest, clientRangeInterest)
                .AddQueries<ServerUpdate.Component>(serverSelfInterest, serverRangeInterest);

            template.AddComponent(interest.ToSnapshot(), serverAttribute);

            template.SetReadAccess(WorkerTypes.UnityClient, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;
        }
    }
}
