using System;

namespace Core
{
    public static class ProductionExtension
    {
        public static string ToThingId(this ProductionType productionType)
        {
            var thingProduction = productionType switch
            {
                // Point
                ProductionType.Ap => "Point_Ap",
                ProductionType.Gold => "Point_Gold",
                ProductionType.TaleStone => "Coin_TaleStone",
                ProductionType.DungeonTicket => "Point_DungeonTicket",
                ProductionType.TotalWarTicket => "Point_TotalWar_Ticket",
                ProductionType.ArcanaCoin => "Point_ArcanaCoin",
                ProductionType.ArenaTicket => "Point_ArenaTicket",
                // Item
                ProductionType.MemoryCoin => "Item_Coin_MemoryCoin",
                ProductionType.ExpeditionCoin => "Item_Coin_ExpeditionCoin",
                ProductionType.TotalWar_01 => "Item_Coin_TotalWar_01",
                ProductionType.ArenaCoin => "Item_Coin_ArenaCoin",
                ProductionType.PracticeBookTicket => "Point_PracticeBookTicket",
                _ => nameof(ProductionType.None)
            };

            return thingProduction;
        }
    }
}