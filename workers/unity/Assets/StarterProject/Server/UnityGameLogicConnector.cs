using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using StarterProject.Shared;
using StarterProject.Shared.Time;
using UnityEngine;

namespace StarterProject.Server
{
    public class UnityGameLogicConnector : WorkerConnector, ITickable
    {
        private Ticker _ticker;

        void Awake()
        {
            _ticker = new Ticker(TimeUtils.CurrentTimeInMs());
            _ticker.Add(this);
        }

        private async void Start()
        {
            PlayerLifecycleConfig.CreatePlayerEntityTemplate = EntityTemplates.CreatePlayerEntityTemplate;

            IConnectionFlow flow;
            ConnectionParameters connectionParameters;

            if (Application.isEditor)
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerTypes.UnityGameLogic));
                connectionParameters = CreateConnectionParameters(WorkerTypes.UnityGameLogic);
            }
            else
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerTypes.UnityGameLogic),
                    new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerTypes.UnityGameLogic,
                    new CommandLineConnectionParameterInitializer());
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(flow)
                .SetConnectionParameters(connectionParameters);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worker.World.GetOrCreateSystem<MetricSendSystem>();
            PlayerLifecycleHelper.AddServerSystems(Worker.World);

            Worker.World.AddSystem(new TimeResponseSystem());
            Debug.Log("Response");
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
            GUI.color = Color.red;
            GUI.Label(new Rect(30, 25, 500, 20), "Server Tick = " + _lastTick);
            float x = _lastTick % 500;
            GUI.Label(new Rect(x, 25, 500, 20), "|");
        }

        #endregion
    }
}
