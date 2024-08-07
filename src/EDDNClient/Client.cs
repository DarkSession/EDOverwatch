﻿using ActiveMQ.Artemis.Client;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;
using System.Text;

namespace EDDNClient
{
    internal class Client : IDisposable
    {
        private bool Disposed { get; set; }
        private IConfiguration Configuration { get; }
        private ILogger Log { get; }
        public DateTimeOffset LastMessageReceived { get; private set; } = DateTimeOffset.UtcNow;
        private SubscriberSocket SubscriberSocket { get; } = new();
        private string EDDNAddress { get; }

        public Client(IConfiguration configuration, ILogger<Client> log)
        {
            Configuration = configuration;
            Log = log;
            EDDNAddress = Configuration.GetValue<string>("EDDN:Address") ?? throw new Exception("No EDDN address configured!");
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                try
                {
                    SubscriberSocket.Disconnect(EDDNAddress);
                    Log.LogDebug("EDDN: Properly disconnected");
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Exception while trying to disconnect.");
                }
                SubscriberSocket.Dispose();
                Disposed = true;
            }
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.LogInformation("Starting client");

                Endpoint activeMqEndpoint = Endpoint.Create(
                   Configuration.GetValue<string>("ActiveMQ:Host") ?? throw new Exception("No ActiveMQ host configured"),
                   Configuration.GetValue<int?>("ActiveMQ:Port") ?? throw new Exception("No ActiveMQ port configured"),
                   Configuration.GetValue<string>("ActiveMQ:Username") ?? string.Empty,
                   Configuration.GetValue<string>("ActiveMQ:Password") ?? string.Empty);

                ConnectionFactory connectionFactory = new();
                await using IConnection connection = await connectionFactory.CreateAsync(activeMqEndpoint, cancellationToken);
                await using IProducer producer = await connection.CreateProducerAsync("EDDN", RoutingType.Anycast, cancellationToken);

                Log.LogInformation("Starting NetMQ...");
                using NetMQRuntime runtime = new();
                runtime.Run(cancellationToken, ReceiveData(producer, cancellationToken));

                await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Process exception");
            }
        }

        private async Task ReceiveData(IProducer producer, CancellationToken cancellationToken = default)
        {
            SubscriberSocket.Connect(EDDNAddress);
            SubscriberSocket.SubscribeToAnyTopic();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    (byte[] bytes, _) = await SubscriberSocket.ReceiveFrameBytesAsync(cancellationToken);
                    LastMessageReceived = DateTimeOffset.UtcNow;
                    await using MemoryStream ms = new(bytes);
                    await using InflaterInputStream inputStream = new(ms);
                    await using MemoryStream outputStream = new();
                    await inputStream.CopyToAsync(outputStream, cancellationToken);
                    outputStream.Position = 0;
                    string jsonString = Encoding.UTF8.GetString(outputStream.ToArray());
                    JObject json = JObject.Parse(jsonString);
                    if (json.TryGetValue("$schemaRef", out JToken? schemaRef) &&
                        schemaRef.Value<string>() is string schema &&
                        !string.IsNullOrEmpty(schema) &&
                        (Configuration.GetSection("EDDN:Schemas").Get<List<string>>()?.Contains(schema) ?? false))
                    {
                        Log.LogDebug("Received and sent EDDN message to ActiveMQ.");
                        await producer.SendAsync(new Message(jsonString), cancellationToken);
                    }
                    else
                    {
                        Log.LogDebug("Received and dropped EDDN message.");
                    }
                }
                catch (Exception e)
                {
                    Log.LogError(e, "EDDN data receival/processing exception");
                }
            }
        }
    }
}
