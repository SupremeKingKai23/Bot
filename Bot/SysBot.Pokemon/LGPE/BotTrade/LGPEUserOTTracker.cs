namespace SysBot.Pokemon;

public class LGPEUserLog
{
    private const int Capacity = 1000;
    public readonly List<LGPEUser> Users = new(Capacity);
    private readonly object _sync = new();
    private int ReplaceIndex;

    public LGPEUser? TryRegister(ulong trainerID, string ot, uint tid, uint sid)
    {
        lock (_sync)
            return InsertReplace(trainerID, ot, tid, sid);
    }

    private LGPEUser? InsertReplace(ulong trainerID, string ot, uint tid, uint sid)
    {
        var index = Users.FindIndex(z => z.TrainerID == tid);
        if (index < 0)
        {
            Insert(trainerID, ot, tid, sid);
            return null;
        }

        var match = Users[index];
        Users[index] = new LGPEUser(trainerID, ot, tid, sid);
        return match;
    }

    private void Insert(ulong trainerID, string ot, uint tid, uint sid)
    {
        var user = new LGPEUser(trainerID, ot, tid, sid);
        if (Users.Count != Capacity)
        {
            Users.Add(user);
            return;
        }

        Users[ReplaceIndex] = user;
        ReplaceIndex = (ReplaceIndex + 1) % Capacity;
    }

    public LGPEUser? TryGetPreviousTrainerID(ulong trainerID)
    {
        lock (_sync)
            return Users.Find(z => z.TrainerID == trainerID);
    }

    public IEnumerable<string> Summarize()
    {
        lock (_sync)
            return Users.FindAll(z => z.TrainerID != 0).OrderBy(z => z.OT).Select(z => $"Discord ID: {z.TrainerID}, OT: {z.OT} TID: {z.TID} SID: {z.SID}").ToList();
    }
}

public sealed record LGPEUser
{
    public readonly ulong TrainerID;
    public readonly string OT;
    public readonly uint TID;
    public readonly uint SID;

    public LGPEUser(ulong trainerID, string ot, uint tid, uint sid)
    {
        TrainerID = trainerID;
        OT = ot;
        TID = tid;
        SID = sid;
    }
}