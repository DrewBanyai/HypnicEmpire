namespace HypnicEmpire
{
    public enum GameUnlock
    {
        Unlocked_Resource_Food,
        Unlocked_Resource_Treasure,
        Unlocked_Resource_Herbs,
        Unlocked_Resource_Money,
        Unlocked_Resource_Wood,
        Unlocked_Resource_People,

        Unlocked_Game_Start,            //  Unlocked automatically at game start
        Unlocked_Empty_Belly,           //  Food at 0
        Unlocked_Action_Forage,         //  Development unlock
        Unlocked_Small_Hoard,           //  Treasure at maximum
        Unlocked_Foraging_Speed_Up,     //  Development unlock
        Unlocked_Action_Sell_Food,      //  Development unlock
        Unlocked_Foraging_Gain_Up_2_5,  //  Development unlock
        Unlocked_Action_Market,         //  Development unlock
        Unlocked_Action_Trade_Herbs     //  Development unlock
    }
}