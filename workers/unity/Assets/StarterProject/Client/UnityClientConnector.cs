using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using StarterProject.Shared;
using StarterProject.Shared.Time;
using UnityEngine;

namespace StarterProject.Client
{
    public class UnityClientConnector : WorkerConnector, ITickable
    {
        private Ticker _ticker;

        void Awake()
        {
            _ticker = new Ticker(TimeUtils.CurrentTimeInMs());
            _ticker.Add(this);
        }

        private async void Start()
        {
            var connParams = CreateConnectionParameters(WorkerTypes.UnityClient);

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionParameters(connParams);

            if (!Application.isEditor)
            {
                var initializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerTypes.UnityClient), initializer));
                        break;
                    case ConnectionService.Locator:
                        builder.SetConnectionFlow(new LocatorFlow(initializer));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerTypes.UnityClient)));
            }

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            PlayerLifecycleHelper.AddClientSystems(Worker.World);

            Worker.World.AddSystem(new TimeRequestSystem(_ticker));
            Debug.Log("request");
        }

        void Update()
        {
            _ticker.Update();
        }

        #region Debug
        private long _lastTick = 0;
        public void Tick(float deltaTime, long tick)
        {
            _lastTick = tick;
        }

        void OnGUI()
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(30, 10, 500, 20), "Client Tick = " + _lastTick);
            float x = _lastTick % 500;
            GUI.Label(new Rect(x, 10, 500, 20), "|");
        }

        #endregion
    }
}
