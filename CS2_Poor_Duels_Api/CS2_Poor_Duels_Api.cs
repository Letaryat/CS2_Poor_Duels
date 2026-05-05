﻿using CounterStrikeSharp.API.Core;

namespace Poor_Duels_Api
{
    public interface IPoorDuelsApi 
    {
        public bool IsAFK(CCSPlayerController player);
        public void PerformAFKAction(CCSPlayerController player, bool afk);
        public void ForceTryStartDuel();
    }
}