using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string runnerIp;
    [SerializeField] private string runnerPort;
    [SerializeField] private string botNickname;
    
    [SerializeField] private GameObject world;

    [SerializeField] private GameObject airPrefab;
    [SerializeField] private GameObject collectiblePrefab;
    [SerializeField] private GameObject hazardPrefab;
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private GameObject ladderPrefab;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject solidPrefab;
    [SerializeField] private GameObject opponentPrefab;

    private List<GameObject> _tiles = new List<GameObject>();

    private RunnerHubService _runnerHubService;

    private BotState lastState = null;

    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        _runnerHubService = new RunnerHubService(runnerIp, runnerPort, botNickname);
        _runnerHubService.Initialize();
        _runnerHubService.OnConnected += (sender, args) => _runnerHubService.Register(botNickname);
    }

    // Update is called once per frame
    void Update()
    {
        var botState = _runnerHubService.BotState;
        if (botState == null)
        {
            return;
        }

        if (lastState == null || botState.X != lastState.X || botState.Y != lastState.Y)
        {
            UpdateTiles(botState);
        }

        processUserInput();
    }

    private void processUserInput()
    {
        var inputCommand = InputCommand.UP;

        var vertical = Input.GetAxis("Vertical");
        var horizontal = Input.GetAxis("Horizontal");

        if (horizontal == 0 && vertical == 0)
        {
            return;
        }


        inputCommand = horizontal switch
        {
            > 0 => InputCommand.RIGHT,
            < 0 => InputCommand.LEFT,
            _ => inputCommand
        };

        inputCommand = vertical switch
        {
            > 0 => InputCommand.UP,
            < 0 => InputCommand.DOWN,
            _ => inputCommand
        };

        inputCommand = inputCommand switch
        {
            // set Diagonal movements
            InputCommand.UP when horizontal > 0 => InputCommand.UPRIGHT,
            InputCommand.UP when horizontal < 0 => InputCommand.UPLEFT,
            InputCommand.DOWN when horizontal > 0 => InputCommand.DOWNRIGHT,
            InputCommand.DOWN when horizontal < 0 => InputCommand.DOWNLEFT,
            _ => inputCommand
        };

        _runnerHubService.SendCommand(inputCommand);
    }

    private void UpdateTiles(BotState botState)
    {
        foreach (var tile in _tiles)
        {
            Destroy(tile);
        }

        var heroWindow = botState.HeroWindow;
        var heroXTiles = new List<int>
        {
            (heroWindow.Length / 2),(heroWindow.Length / 2) -1, // x
        };
        var heroYTiles = new List<int>
        {
            (heroWindow[0].Length / 2), (heroWindow[0].Length / 2) - 1 // y 
        };

        for (var y = heroWindow[0].Length - 1; y >= 0; y--)
        {
            for (var x = 0; x < heroWindow.Length; x++)
            {
                var tile = heroWindow[x][y];
                var tilePrefab = (heroXTiles.Contains(x) && heroYTiles.Contains(y)) ? heroPrefab : GetPrefab(tile);
                var tilePosition = new Vector3(x, y, 0);
                // Just nests new gameobjects under a gameobject of choice , make sure to set world to position 0,0,0
                if (world != null)
                {
                    _tiles.Add(Instantiate(tilePrefab, tilePosition, Quaternion.identity,world.transform));
                }
                else
                {
                    _tiles.Add(Instantiate(tilePrefab, tilePosition, Quaternion.identity));
                }
               
            }
        }

        lastState = botState;
    }

    private GameObject GetPrefab(int tile)
    {
        return tile switch
        {
            0 => airPrefab,
            1 => solidPrefab,
            2 => collectiblePrefab,
            3 => hazardPrefab,
            4 => platformPrefab,
            5 => ladderPrefab,
            6 => opponentPrefab,
            _ => throw new Exception("Invalid tile type")
        };
    }
}