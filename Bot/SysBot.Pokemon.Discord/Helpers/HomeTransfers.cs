﻿using PKHeX.Core;
using System.Collections.Generic;
using static PKHeX.Core.Species;

namespace SysBot.Pokemon.Discord.Helpers
{
    internal class HomeTransfers
    {
        public static readonly HashSet<(Species, byte)> TransferMapSV =
        [
            (Pikachu, 1),
            (Pikachu, 2),
            (Pikachu, 3),
            (Pikachu, 4),
            (Pikachu, 5),
            (Pikachu, 6),
            (Pikachu, 7),
            (Pikachu, 8),
            (Pikachu, 9),
            (Raichu, 1),
            (Weezing , 1),
            (Articuno , 1),
            (Zapdos , 1),
            (Moltres , 1),
            (Zapdos , 1),
            (Moltres , 1),
            (Jirachi, 0),
            (Deoxys,0),
            (Deoxys,1),
            (Deoxys,2),
            (Deoxys,3),
            (Uxie, 0),
            (Mesprit, 0),
            (Azelf, 0),
            (Heatran, 0),
            (Regigigas, 0),
            (Giratina, 0),
            (Giratina, 1),
            (Cresselia, 0),
            (Manaphy, 0),
            (Shaymin, 0),
            (Shaymin , 1),
            (Arceus, 0),
            (Arceus, 1),
            (Arceus, 2),
            (Arceus, 3),
            (Arceus, 4),
            (Arceus, 5),
            (Arceus, 6),
            (Arceus, 7),
            (Arceus, 8),
            (Arceus, 9),
            (Arceus, 10),
            (Arceus, 11),
            (Arceus, 12),
            (Arceus, 13),
            (Arceus, 14),
            (Arceus, 15),
            (Arceus, 16),
            (Arceus, 17),
            (Lilligant , 1),
            (Braviary , 1),
            (Tornadus, 0),
            (Tornadus , 1),
            (Thundurus, 0),
            (Thundurus , 1),
            (Landorus, 0),
            (Landorus , 1),
            (Keldeo, 0),
            (Vivillon , 19),
            (Sliggoo , 1),
            (Goodra , 1),
            (Avalugg , 1),
            (Diancie, 0),
            (Hoopa, 0),
            (Hoopa, 1),
            (Volcanion, 0),
            (Magearna, 0),
            (Magearna , 1),
            (Zacian, 0),
            (Zamazenta, 0),
            (Eternatus, 0),
            (Zarude, 0),
            (Zarude, 1),
            (Regieleki, 0),
            (Regidrago, 0),
            (Calyrex, 0),
            (Wyrdeer, 0),
            (Ursaluna, 0),
            (Enamorus, 0),
            (Enamorus, 1),
        ];

        public static bool IsHomeTransferOnlySV(Species species, byte form)
        {
            return TransferMapSV.Contains((species, form));
        }
    }
}