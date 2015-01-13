using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Threading;
using Microsoft.AspNet.SignalR;

using Newtonsoft.Json;

namespace MoveShapeDemo
{
    public class Broadcaster
    {
        private readonly static Lazy<Broadcaster> _instance = new Lazy<Broadcaster>(() => new Broadcaster());
        // We're going to broadcast to all clients <<n>> times per second
        private readonly TimeSpan BroadcastInterval = TimeSpan.FromMilliseconds(20);
        private readonly IHubContext _hubContext;
        private Timer _broadcastLoop;
        private ShapeModel _model;
        private bool _modelUpdated;

        public Broadcaster()
        {
            // Save our hub context so we can easily use it 
            // to send to its connected clients
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<MoveShapeHub>();

            _model = new ShapeModel();
            _modelUpdated = false;

            // Start the broadcast loop
            _broadcastLoop = new Timer(
                BroadcastShape,
                null,
                BroadcastInterval,
                BroadcastInterval
                );
        }

        public void BroadcastShape(object state)
        {
            // No need to send anything if our model hasn't changed
            if (_modelUpdated)
            {
                // This is how we can access the Clients property 
                // in a static hub method or outside of the hub entirely
                _hubContext.Clients.AllExcept(_model.LastUpdatedBy).updateShape(_model);
                _modelUpdated = false;
            }
        }

        public void UpdateShape(ShapeModel clientModel)
        {
            _model = clientModel;
            _modelUpdated = true;
        }

        public void triggerShapeClick()
        {
            _hubContext.Clients.All.clientClickedShape();

            Console.WriteLine("triggerShapeClick Broadcaster");
        }

        public void triggerShapeDoubleClick()
        {
            _hubContext.Clients.All.clientDoubleClickedShape();

            Console.WriteLine("triggerShapeDoubleClick Broadcaster");
        }

        public static Broadcaster Instance
        {
            get
            {
                return _instance.Value;
            }
        }

    }

    public class MoveShapeHub : Hub
    {
        // Is set via the constructor on each creation
        private Broadcaster _broadcaster;

        public MoveShapeHub() : this(Broadcaster.Instance)
        {
        }

        public MoveShapeHub(Broadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }

        public void UpdateModel(ShapeModel clientModel)
        {
            clientModel.LastUpdatedBy = Context.ConnectionId;
            // Update the shape model within our broadcaster
            _broadcaster.UpdateShape(clientModel);
        }

        public void triggerShapeClick()
        {
            // Update the shape model within our broadcaster
            _broadcaster.triggerShapeClick();

            Console.WriteLine("triggerShapeClick");
        }

        public void triggerShapeDoubleClick()
        {
            // Update the shape model within our broadcaster
            _broadcaster.triggerShapeDoubleClick();

            Console.WriteLine("triggerShapeDoubleClick");
        }
    }
    public class ShapeModel
    {
        // We declare Left and Top as lowercase with 
        // JsonProperty to sync the client and server models
        [JsonProperty("left")]
        public double Left { get; set; }

        [JsonProperty("top")]
        public double Top { get; set; }

        // We don't want the client to get the "LastUpdatedBy" property
        [JsonIgnore]
        public string LastUpdatedBy { get; set; }
    }

}
