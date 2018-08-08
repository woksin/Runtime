/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Dolittle. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using Dolittle.Collections;
using Dolittle.Execution;
using Dolittle.Logging;
using Dolittle.Serialization.Protobuf;
using Grpc.Core;

namespace Dolittle.Runtime.Events.Relativity
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventHorizon"/>
    /// </summary>
    [Singleton]
    public class EventHorizon : IEventHorizon, IDisposable
    {
        const int _port = 50051;
        readonly List<ISingularity> _singularities = new List<ISingularity>();

        readonly Server _server;
        readonly ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationManager"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public EventHorizon(IEventHorizonsConfigurationManager configurationManager, ISerializer serializer, ILogger logger)
        {
            _logger = logger;

            try
            {
                _server = new Server
                {
                    Services = { QuantumTunnelService.BindService(new QuantumTunnelServiceImplementation(this, serializer, logger)) },
                    Ports = {
                    new ServerPort("localhost", configurationManager.Current.Port, SslServerCredentials.Insecure)
                    //new ServerPort($"unix:{configurationManager.Current.UnixSocket}", 0, SslServerCredentials.Insecure)
                    }
                };

                _server
                    .Ports
                    .ForEach(_ =>
                        _logger.Information($"Starting gRPC server on {_.Host}" + (_.Port > 0 ? $" for port {_.Port}" : string.Empty)));

                _server.Start();


                _logger.Information("Server started");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't not establish an event horizon");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _server.ShutdownAsync().Wait();
        }

        /// <inheritdoc/>
        public void PassThrough(Dolittle.Runtime.Events.Store.CommittedEventStream committedEventStream)
        {
            lock(_singularities)
            {
                _logger.Information($"Passing committed events through {_singularities.Count} singularities");
                _singularities
                    .Where(_ => _.CanReceive(committedEventStream)).AsParallel()
                    .ForEach(_ =>
                    {
                        _.Tunnel.PassThrough(committedEventStream);
                    });
            }
        }

        /// <inheritdoc/>
        public void Collapse(ISingularity singularity)
        {
            lock(_singularities)
            {
                _logger.Information($"Quantum tunnel collapsed for singularity identified with bounded context '{singularity.BoundedContext}' in application '{singularity.Application}'");
                _singularities.Remove(singularity);
            }
        }

        /// <inheritdoc/>
        public void GravitateTowards(ISingularity singularity)
        {
            lock(_singularities)
            {
                _logger.Information($"Gravitate events in the event horizon towards singularity identified with bounded context '{singularity.BoundedContext}' in application '{singularity.Application}'");
                _singularities.Add(singularity);
            }
        }
    }
}