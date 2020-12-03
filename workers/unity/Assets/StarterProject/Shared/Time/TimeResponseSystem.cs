using Improbable.Gdk.Core;
using Unity.Entities;

namespace StarterProject.Shared.Time
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [DisableAutoCreation]
    public class TimeResponseSystem : SystemBase
    {
        private ComponentUpdateSystem _componentUpdateSystem;
        protected override void OnCreate()
        {
            _componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref SpatialEntityId entityId, ref ServerUpdate.Component serverUpdate) =>
            {
                var timeRequests = _componentUpdateSystem.GetEventsReceived<ClientUpdate.TimeRequest.Event>(entityId.EntityId);

                for (int i = 0; i < timeRequests.Count; i++)
                {
                    var payload = timeRequests[i].Event.Payload;

                    var timeInMs = TimeUtils.CurrentTimeInMs();

                    var timeResponse = new ServerUpdate.TimeResponse.Event(new TimeResponse(payload.RequestId, timeInMs));
                    _componentUpdateSystem.SendEvent(timeResponse, entityId.EntityId);
                }

            }).WithoutBurst().Run();
        }
    }
}
