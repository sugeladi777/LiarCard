using System.Collections.Generic;
using UnityEngine;
using LiarCard.LocalGame.Core;
using LiarCard.LocalGame.Gameplay;

namespace LiarCard.App
{
    public class GameBootstrap : MonoBehaviour
    {
        private LocalGameService _localGameService;

        private void Start()
        {
            _localGameService = new LocalGameService();
            _localGameService.StartGame();
        }
    }
}