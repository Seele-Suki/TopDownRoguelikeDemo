namespace TopDownRoguelike.Networking.Protocol
{
    public enum NetworkEntityType : byte
    {
        Invalid = 0,
        Player = 1,
        Enemy = 2,
        Boss = 3,
        ExperienceOrb = 4
    }
}