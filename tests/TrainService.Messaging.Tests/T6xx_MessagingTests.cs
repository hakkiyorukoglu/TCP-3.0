using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Xunit;
using TrainService.Messaging;
using TrainService.Core.Messaging;

namespace TrainService.Messaging.Tests;

public sealed class EmbeddedBrokerFixture : IAsyncLifetime
{
    public int Port { get; private set; }
    public EmbeddedBrokerService Broker { get; private set; } = default!;
    public async Task InitializeAsync()
    {
        Port = BosPortBul();                       
        Broker = new EmbeddedBrokerService(Port, keepAliveSec: 2);  
        await Broker.StartAsync();
    }
    public async Task DisposeAsync() => await Broker.StopAsync();
    
    public async Task<IMqttClient> ClientAsync(string id, (string topic,string payload)? lwt = null) 
    { 
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(id)
            .WithTcpServer("127.0.0.1", Port)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(2));
            
        if (lwt.HasValue)
        {
            builder.WithWillTopic(lwt.Value.topic)
                   .WithWillPayload(lwt.Value.payload)
                   .WithWillRetain(true);
        }
        await client.ConnectAsync(builder.Build());
        return client;
    }
    
    static int BosPortBul()
    { 
        var l = new TcpListener(IPAddress.Loopback, 0); 
        l.Start(); 
        var p = ((IPEndPoint)l.LocalEndpoint).Port; 
        l.Stop(); 
        return p; 
    }
}

public class T6xx_MessagingTests : IClassFixture<EmbeddedBrokerFixture>
{
    private readonly EmbeddedBrokerFixture _fx;

    public T6xx_MessagingTests(EmbeddedBrokerFixture fx)
    {
        _fx = fx;
    }

    private async Task Eventually(Func<bool> condition, int timeoutSeconds)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < timeoutSeconds)
        {
            if (condition()) return;
            await Task.Delay(50);
        }
        throw new Exception("Timeout: Eventually condition failed.");
    }

    [Fact]
    public async Task T601_Broker_Baslat_Durdur_TekrarBaslat()
    {
        var b = new EmbeddedBrokerService(_fx.Port + 1, 2);
        await b.StartAsync();
        await b.StopAsync();
        var act = async () => { await b.StartAsync(); await b.StopAsync(); };  
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task T602_Loopback_PubSub()
    {
        var alindi = new TaskCompletionSource<string>();
        var sub = await _fx.ClientAsync("sub");
        await sub.SubscribeAsync("test/konu");
        sub.ApplicationMessageReceivedAsync += e => {
            alindi.TrySetResult((e.ApplicationMessage.Payload != null ? System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload) : string.Empty)); return Task.CompletedTask; };
        var pub = await _fx.ClientAsync("pub");
        await pub.PublishStringAsync("test/konu", "merhaba");
        (await alindi.Task.WaitAsync(TimeSpan.FromSeconds(3))).Should().Be("merhaba");
    }

    [Fact]
    public void T603_TopicContract_SabitlerFormatDogru()
    {
        TrainService.Core.Messaging.Topics.Commands.Should().Be("restaurant/commands");
        TrainService.Core.Messaging.Topics.RfidTelemetry("5").Should().Be("restaurant/telemetry/5/rfid");
        TrainService.Core.Messaging.Topics.Ack("5").Should().Be("restaurant/ack/5");
        TrainService.Core.Messaging.Topics.Status("dev1").Should().Be("restaurant/status/dev1");
    }

    [Fact]
    public void T604_Payload_JsonRoundTrip_BilinmeyenAlanYokSayilir()
    {
        var cmd = new CommandPayload(Guid.NewGuid(), "T1", "5", "GO");
        var json = JsonSerializer.Serialize(cmd);
        var geri = JsonSerializer.Deserialize<CommandPayload>(json);
        geri.Should().BeEquivalentTo(cmd);
        
        var act = () => JsonSerializer.Deserialize<CommandPayload>(json.Insert(1, "\"yeniAlan\":42,"));
        act.Should().NotThrow();
    }

    [Fact]
    public async Task T605_LWT_KabaKopus_OfflineAlgilanir()
    {
        var offline = new TaskCompletionSource<string>();
        var izleyici = await _fx.ClientAsync("izleyici");
        await izleyici.SubscribeAsync("restaurant/status/dev1");
        izleyici.ApplicationMessageReceivedAsync += e => {
            if ((e.ApplicationMessage.Payload != null ? System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload) : string.Empty) == "offline") offline.TrySetResult("offline");
            return Task.CompletedTask; };

        var dev = await _fx.ClientAsync("dev1", lwt: ("restaurant/status/dev1", "offline"));
        dev.Dispose();   

        (await offline.Task.WaitAsync(TimeSpan.FromSeconds(6))).Should().Be("offline",
            "kaba kopuşta LWT ile offline algılanmalı (keep-alive=2sn)");
    }

    [Fact]
    public async Task T606_Status_Retained_GecBaglananGorur()
    {
        var dev = await _fx.ClientAsync("dev2");
        await dev.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("restaurant/status/dev2").WithPayload("online").WithRetainFlag().Build());
        await Task.Yield();
        var gorulen = new TaskCompletionSource<string>();
        var gec = await _fx.ClientAsync("gec-izleyici");   
        gec.ApplicationMessageReceivedAsync += e => { gorulen.TrySetResult((e.ApplicationMessage.Payload != null ? System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload) : string.Empty)); return Task.CompletedTask; };
        await gec.SubscribeAsync("restaurant/status/dev2");
        (await gorulen.Task.WaitAsync(TimeSpan.FromSeconds(3))).Should().Be("online", "retained mesaj geç gelene ulaşmalı");
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task T607_610_Hub_QoS_Teslim(int qos)
    {
        var alindi = new TaskCompletionSource<bool>();
        var sub = await _fx.ClientAsync($"q{qos}s");
        await sub.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("q/test")
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos).Build());
        sub.ApplicationMessageReceivedAsync += e => { alindi.TrySetResult(true); return Task.CompletedTask; };
        var pub = await _fx.ClientAsync($"q{qos}p");
        await pub.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("q/test")
            .WithPayload("x").WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos).Build());
        (await alindi.Task.WaitAsync(TimeSpan.FromSeconds(3))).Should().BeTrue($"QoS{qos} teslim edilmeli");
    }

    [Fact]
    public async Task T611_DeviceRegistry_YeniCihazKaydeder()
    {
        var reg = new DeviceRegistry(_fx.Broker);   
        var dev = await _fx.ClientAsync("regdev");
        await dev.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("restaurant/status/regdev").WithPayload("online").WithRetainFlag().Build());
        await Eventually(() => reg.IsOnline("regdev"), 3);   
        reg.IsOnline("regdev").Should().BeTrue();
    }

    [Fact]
    public async Task T612_DeviceRegistry_LastSeenGuncellenir()
    {
        var reg = new DeviceRegistry(_fx.Broker);
        var dev = await _fx.ClientAsync("seen");
        await dev.PublishStringAsync("restaurant/status/seen", "online");
        await Eventually(() => reg.LastSeen("seen") != null, 3);
        var t1 = reg.LastSeen("seen");
        await dev.PublishStringAsync("restaurant/status/seen", "online");
        await Eventually(() => reg.LastSeen("seen") > t1, 3);
        reg.LastSeen("seen").Should().BeAfter(t1!.Value);
    }

    [Fact]
    public async Task T613_DeviceRegistry_LWT_OfflineIsaretler()
    {
        var reg = new DeviceRegistry(_fx.Broker);
        var dev = await _fx.ClientAsync("lwtdev", lwt: ("restaurant/status/lwtdev", "offline"));
        await dev.PublishStringAsync("restaurant/status/lwtdev", "online");
        await Eventually(() => reg.IsOnline("lwtdev"), 3);
        dev.Dispose();
        await Eventually(() => !reg.IsOnline("lwtdev"), 6);
        reg.IsOnline("lwtdev").Should().BeFalse();
    }

    [Fact]
    public async Task T614_DeviceRegistry_StatusChangedEvent_TekYayin()
    {
        var reg = new DeviceRegistry(_fx.Broker);
        int sayac = 0; reg.StatusChanged += (_, __) => Interlocked.Increment(ref sayac);
        var dev = await _fx.ClientAsync("evt");
        await dev.PublishStringAsync("restaurant/status/evt", "online");
        await Eventually(() => sayac >= 1, 3);
        await dev.PublishStringAsync("restaurant/status/evt", "online");  
        await Task.Delay(300);
        sayac.Should().Be(1, "aynı duruma ikinci 'online' event tekrar üretmemeli");
    }

    [Fact]
    public void T615_PingService_BilinenCihazlariPingler()
    {
        var ping = new PingService(new[] { "127.0.0.1" });
        var sonuc = ping.PingAll();   
        sonuc.Should().ContainKey("127.0.0.1");
    }

    [Fact]
    public async Task T616_PingService_Timeout_CokmezSarilir()
    {
        var ping = new PingService(new[] { "10.255.255.1" });  
        var act = async () => await ping.PingAllAsync(timeoutMs: 300);
        await act.Should().NotThrowAsync("timeout exception'a değil, 'ulaşılamaz' sonucuna dönüşmeli");
    }

    [Fact]
    public void T617_DeviceHealth_DortDurumBirlesimi()
    {
        DeviceHealth.Combine(ping:true,  mqtt:true ).Should().Be(HealthState.Green);
        DeviceHealth.Combine(ping:true,  mqtt:false).Should().Be(HealthState.Yellow);
        DeviceHealth.Combine(ping:false, mqtt:false).Should().Be(HealthState.Red);
        DeviceHealth.Combine(ping:false, mqtt:true ).Should().Be(HealthState.Yellow); 
    }
}


