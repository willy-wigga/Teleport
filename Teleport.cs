using Il2CppSystem.IO;
using SonsSdk;
using SUI;
using static SUI.SUI;

using UnityEngine;
using TheForest.Utils;
using TheForest;
using Sons.Multiplayer.Gui;
using Sons.Areas;
using Sons.Gui;
using Endnight.Utilities;

namespace Teleport;

public class Teleport : SonsMod
{
    private SContainerOptions container;
    private SLabelOptions[] text = new SLabelOptions[30];

    private List<PlayerNameUiLink> _players = new List<PlayerNameUiLink>();

    private bool _tp = false;
    private int _offset = 0;
    private float _timer = 0;
    private float _holdTimer = 0;

    private readonly int listCount = 5;

    private Teleport()
    { OnUpdateCallback = Update; }

    private void Update()
    {
        if (LocalPlayer.IsInWorld && !PauseMenu.IsActive && !TheForest.UI.Multiplayer.ChatBox.IsChatOpen && !DebugConsole.Instance._showConsole)
        {
            // close menu after 10 seconds
            if (_tp)
            {
                if (_timer >= 10)
                    EndTP();
                else _timer += Time.deltaTime;
            }

            // tp to mountain if KeyCode.Tab is held for 3+ seconds
            if (_holdTimer > 0 && Time.time - _holdTimer >= 3f)
                TP(-1);

            // check tp, or cancel tp
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _holdTimer = Time.time;

                if (!_tp)
                    CheckTP();
                else
                    EndTP();
            } else if (Input.GetKeyUp(KeyCode.Tab)) _holdTimer = 0;

            // initiate teleport if there is a target player
            if (_tp && Input.inputString.Length == 1 && Input.inputString.ToCharArray()[0] - '0' >= 1 && Input.inputString.ToCharArray()[0] - '0' <= listCount && Input.inputString.ToCharArray()[0] - '0' + _offset <= _players.Count())
                TP(Input.inputString.ToCharArray()[0] - '0' - 1);

            if (_tp && Input.mouseScrollDelta.y != 0)
                Scroll(Input.mouseScrollDelta.y > 0);
        } else if (_tp) EndTP();
    }

    private void Scroll(bool _up)
    {
        // Scroll up
        if (_up)
        {
            if (_offset > 0)
                _offset--;
            else return;
        }
        // Scroll down
        else if (_offset + listCount < _players.Count())
            _offset++;
        else return;

        // Update player list (cannot use 'CheckTP' because player list may be inconsistent; sometimes orders change or players leave)
        for (int i = listCount; i < 30; i++)
            if (text[i] != null)
            {
                if (i >= listCount + _offset && i < (listCount * 2) + _offset)
                {
                    text[i].VOffset(60f * (i - (listCount + _offset)), 0f);
                    text[i].Visible(true);
                }
                else text[i].Visible(false);
            }
    }

    private void CheckTP()
    {
        // Fill up player list
        int _count = listCount;
        foreach (BoltEntity _entity in BoltCore.entities.ToArray())
        {
            PlayerNameUiLink _playerUI = _entity.GetComponentInChildren<PlayerNameUiLink>();

            if (_playerUI != null)
            {
                text[_count] = SLabel.FontColor(_playerUI._playerColor).
                FontStyle(TMPro.FontStyles.Bold).Visible(false).Text(_playerUI._playerName).
                Alignment(TMPro.TextAlignmentOptions.Left).Anchor(AnchorType.MiddleLeft).
                HOffset(30f, 0f).VOffset(60f * (_count - listCount), 0f);

                text[_count].SetParent(container);

                _players.Add(_playerUI);

                // Make text visible if it's below the listcount max (5 in this case)
                if (_count < listCount * 2)
                    text[_count].Visible(true);

                _count++;
            }
        }

        // Make sure there's someone to teleport to
        if (_players.Count == 0)
            return;

        // Make number labels visible
        for (int i = 0; i < listCount; i++)
            text[i].Visible(true);

        _tp = true;
    }

    private void TP(int _target)
    {
        // TP to mountain
        if (_target == -1)
        {
            _holdTimer = 0;

            LocalPlayer.SetPosition(new Vector3(201f, 739f, 144f));
            CaveEntranceManager.SetLocalPlayerCurrentArea((AreaMask)0);
        }
        // TP to the target player if the player hasn't left
        else if (_players[_target + _offset] != null)
        {
            PlayerNameUiLink _targetPlayer = _players[_target + _offset];
            LocalPlayer.SetPosition(_targetPlayer._gpsLocator.Position());
            CaveEntranceManager.SetLocalPlayerCurrentArea((AreaMask)_targetPlayer.GetComponentInParent<PlayerLocation>().AreaMask);
        }

        EndTP();
    }

    private void EndTP()
    {
        // Deinitialization
        _tp = false;
        _offset = 0;
        _timer = 0;

        // Reset player data
        _players.Clear();
        for (int i = 0; i < 30; i++)
            if (text[i] != null)
                text[i].Visible(false);
    }

    protected override void OnSdkInitialized()
    {
        container = RegisterNewPanel("container");
        // #listCount of 'text' is reserved for labels
        for (int i = 0; i < listCount; i++)
        {
            text[i] = SLabel.FontColor(Color.white).Visible(false).
            Text(string.Format("{0}", i + 1)).Alignment(TMPro.TextAlignmentOptions.Left).
            Anchor(AnchorType.MiddleLeft).HOffset(5f, 0f).VOffset(60f * i, 0f);

            text[i].SetParent(container);
        }
    }
}