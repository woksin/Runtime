/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Applications;
using Dolittle.Artifacts;
using Dolittle.Collections;
using Dolittle.Logging;
using Google.Protobuf;
using Grpc.Core;

namespace Dolittle.Runtime.Events.Relativity
{
    /// <summary>
    /// Represents a concrete connection through a <see cref="IBarrier"/>
    /// </summary>
    public class Connection : IDisposable
    {
        readonly string _url;
        readonly IEnumerable<Artifact> _events;
        readonly ILogger _logger;
        readonly Application _application;
        readonly BoundedContext _boundedContext;
        readonly Channel _channel;
        readonly QuantumTunnelService.QuantumTunnelServiceClient _client;

        /// <summary>
        /// Initializes a new instance of <see cref="Connection"/>
        /// </summary>
        /// <param name="application">The current <see cref="Application"/></param>
        /// <param name="boundedContext">The current <see cref="BoundedContext"/></param>
        /// <param name="url">Url for the <see cref="IEventHorizon"/> we're connecting to</param>
        /// <param name="events"><see cref="IEnumerable{Artifact}">Events</see> to connect for</param>
        /// <param name="logger"><see cref="ILogger"/> for logging purposes</param>
        public Connection(Application application, BoundedContext boundedContext, string url, IEnumerable<Artifact> events, ILogger logger)
        {
            _url = url;
            _events = events;
            _logger = logger;
            _application = application;
            _boundedContext = boundedContext;
            _channel = new Channel(_url, ChannelCredentials.Insecure);
            _client = new QuantumTunnelService.QuantumTunnelServiceClient(_channel);

            Task.Run(() => Run());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _channel.ShutdownAsync();
        }


        void Run()
        {
            _logger.Information($"Establishing connection towards event horizon at '{_url}'");

            Task.Run(async() =>
            {
                for (;;)
                {
                    try
                    {
                        await OpenAndHandleStream();
                    }
                    catch( Exception ex )
                    {
                        _logger.Error(ex, "Error occurred during establishing quantum tunnel");
                    }
                    _logger.Warning("Connection broken - backing off for a second");
                    Thread.Sleep(1000);
                    _logger.Warning("Trying to reconnect");
                }
            }).Wait();

            _channel.ShutdownAsync();
        }



        async Task OpenAndHandleStream()
        {

            var openTunnelMessage = new OpenTunnelMessage
            {
                Application = ByteString.CopyFrom(_application.Value.ToByteArray()),
                BoundedContext = ByteString.CopyFrom(_boundedContext.Value.ToByteArray()),
            };

            _events.Select(_ => new EventArtifactMessage
            {
                Event = ByteString.CopyFrom(_.Id.Value.ToByteArray()),
                Generation = _.Generation
            }).ForEach(openTunnelMessage.Events.Add);

            var stream = _client.Open(openTunnelMessage);
            while (await stream.ResponseStream.MoveNext(CancellationToken.None))
            {
                _logger.Information("Event received");
            }

            _logger.Information("Done opening and handling the stream");
        }

    }
}