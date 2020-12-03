using System;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Unity.Entities;
using UnityEngine;

namespace StarterProject.Shared.Time
{
    public struct TimeRequestData : IComponentData
    {
        public long ServerDateTimeTicks;
        public long UpdatedTime;
        public long RttMs;
    }

    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [DisableAutoCreation]
    public class TimeRequestSystem : SystemBase
    {
        private ComponentUpdateSystem _componentUpdateSystem = null;
        private Dictionary<int, long> _timeRequests = new Dictionary<int, long>();
        private int _timeRequestId = 0;
        private TimeSince _sendTimer;
        private Ticker _ticker;

        public TimeRequestSystem(Ticker ticker)
        {
            _ticker = ticker;
            _sendTimer = TimeConfig.TimeRequestIntervalSeconds;
        }

        protected override void OnCreate()
        {
            _componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            EntityManager.CreateEntity(ComponentType.ReadOnly<TimeRequestData>());
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref SpatialEntityId entityId, ref ClientUpdate.Component clientUpdate, in ClientUpdate.HasAuthority authority) =>
            {
                var timeResponses =
                    _componentUpdateSystem.GetEventsReceived<ServerUpdate.TimeResponse.Event>(entityId.EntityId);

                for (int i = 0; i < timeResponses.Count; i++)
                {
                    var timeResponse = timeResponses[i];
                    var payload = timeResponse.Event.Payload;

                    if (!_timeRequests.ContainsKey(payload.RequestId))
                    {
                        Debug.LogWarning("Received a time response with an invalid request id. Id was " + payload.RequestId);
                        continue;
                    }

                    long timeInMs = TimeUtils.CurrentTimeInMs();
                    long rtt = timeInMs - _timeRequests[payload.RequestId];
                    long rttHalf = rtt / 2;
                    long newTime = payload.Time + rttHalf + TimeConfig.CommandBufferLengthMs;
                    _ticker.SetTime(newTime);

                    var timeData = new TimeRequestData()
                    {
                        ServerDateTimeTicks = newTime, UpdatedTime = timeInMs, RttMs = rtt
                    };

                    SetSingleton(timeData);
                    _timeRequests.Remove(payload.RequestId);
                }

                var timeDilations =
                    _componentUpdateSystem.GetEventsReceived<ServerUpdate.TimeDilation.Event>(entityId.EntityId);

                for (int i = 0; i < timeDilations.Count; i++)
                {
                    _ticker.Dilate();
                }

            }).WithoutBurst().Run();
            float time = _sendTimer;
            if (time > TimeConfig.TimeRequestIntervalSeconds)
            {
                if (SendTimeRequest())
                {
                    _sendTimer = 0;
                }
            }
        }

        private bool SendTimeRequest()
        {
            bool sentRequest = false;

            Entities.ForEach((Entity entity, ref SpatialEntityId entityId, ref ClientUpdate.Component clientUpdate, in ClientUpdate.HasAuthority authority) =>
            {
                int id = _timeRequestId++;
                long timeAtRequest = TimeUtils.CurrentTimeInMs();

                var timeRequest = new ClientUpdate.TimeRequest.Event(new TimeRequest(id));
                _componentUpdateSystem.SendEvent(timeRequest, entityId.EntityId);
                _timeRequests[id] = timeAtRequest;

                sentRequest = true;

            }).WithoutBurst().Run();

            return sentRequest;
        }
    }
}
