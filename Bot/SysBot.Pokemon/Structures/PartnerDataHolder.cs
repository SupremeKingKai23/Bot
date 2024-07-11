namespace SysBot.Pokemon;

public class PartnerDataHolder(ulong trainerNid, string trainerName, string trainerTid)
{
    public readonly ulong TrainerOnlineID = trainerNid;
    public readonly string TrainerName = trainerName;
    public readonly string TrainerTID = trainerTid;
}