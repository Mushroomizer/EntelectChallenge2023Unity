using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using UnityEngine;

public class RunnerHubService
{
    private readonly string _runnerIp;
    private readonly string _runnerPort;
    private string _botNickname;
    private bool _containerRestarted = false;
    private HubConnection _connection;

    public event EventHandler OnConnected;

    private Guid BotId { get; set; }
    public BotState BotState { get; private set; }

    private bool sendingCommand = false;

    public RunnerHubService(string runnerIp, string runnerPort, string botNickname)
    {
        _runnerIp = runnerIp;
        _runnerPort = runnerPort;
        _botNickname = botNickname;
    }

    private bool ShouldConnect()
    {
        return _connection is not {State: HubConnectionState.Connected} && _connection is not {State: HubConnectionState.Connecting} && _connection is not {State: HubConnectionState.Reconnecting};
    }

    public void Initialize()
    {
        Start();
    }

    private async Task Start()
    {
        // Uncomment this to restart the container on every run (it wont be created, so you need to run the run.cmd or run.sh script from the cyfi root dir first)
        // if (!_containerRestarted)
        // {
        //     await RestartContainer();
        // }
    
        if (!ShouldConnect())
        {
            return;
        }

        try
        {
            Debug.Log("Connecting to Runner");
            _connection = new HubConnectionBuilder()
                .WithUrl($"{_runnerIp}:{_runnerPort}/runnerhub")
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug); })
                .WithAutomaticReconnect()
                .Build();

            await _connection.StartAsync();
            Debug.Log("Connected to Runner");


            _connection.On<Guid>("Registered", (id) => BotId = id);

            _connection.On<String>(
                "Disconnect",
                async (reason) =>
                {
                    Debug.Log($"Server sent disconnect with reason: {reason}");
                    await _connection.StopAsync();
                }
            );

            _connection.On<BotState>(
                "ReceiveBotState",
                (newBotState) => { BotState = newBotState; }
            );

            _connection.Closed += (error) =>
            {
                Debug.Log($"Server closed with error: {error}");
                return Task.CompletedTask;
            };

            OnConnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            Debug.Log("Is the server running?");
        }
    }

    private async Task RestartContainer()
    {
        if (_containerRestarted)
        {
            return;
        }

        Debug.Log("Starting Docker container.");

        const string containerName = "cyfi";
        const string searchString = "Content root path: \"/app\"";

        using var client = new DockerClientConfiguration().CreateClient();


        await client.Containers.RestartContainerAsync(containerName, new ContainerRestartParameters());
        var logsStream = await client.Containers.GetContainerLogsAsync(containerName, new ContainerLogsParameters
        {
            Follow = true,
            ShowStdout = true,
            ShowStderr = true
        });

        using var reader = new StreamReader(logsStream);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase) <= 0)
            {
                continue;
            }

            Thread.Sleep(1000);
            Debug.Log("Docker container restarted.");
            _containerRestarted = true;
            break;
        }
    }

    // Public

    public async Task Register(string nickName)
    {
        await Start();
        _botNickname = nickName;
        await _connection.InvokeAsync("Register", _botNickname);
    }

    public async Task SendCommand(InputCommand command)
    {
        if (sendingCommand)
        {
            return;
        }

        sendingCommand = true;
        await Start();
        await _connection.InvokeAsync("SendPlayerCommand", new BotCommand() {BotId = BotId, Action = command});
        sendingCommand = false;
    }
}