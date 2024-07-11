using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon;

public class PokemonSTPool<T>(BaseConfig settings) : List<T> where T : PKM, new()
{
    private readonly int ExpectedSize = new T().Data.Length;
    private readonly BaseConfig Settings = settings;
    private bool Randomized => Settings.Shuffled;

    public readonly Dictionary<string, SurpriseTradeRequest<T>> Files = [];
    private int Counter;
    private bool InitialStart = true;

    public T GetRandomPoke()
    {
        if (InitialStart && Randomized)
        {
            Shuffle(this, 0, Count, Util.Rand);
            InitialStart = false;
        }

        var choice = this[Counter];
        Counter = (Counter + 1) % Count;
        if (Counter == 0 && Randomized)
        {
            Shuffle(this, 0, Count, Util.Rand);
        }
        return choice;
    }

    public static void Shuffle(IList<T> items, int start, int end, Random rnd)
    {
        for (int i = start; i < end; i++)
        {
            int index = i + rnd.Next(end - i);
            (items[index], items[i]) = (items[i], items[index]);
        }
    }

    public T GetRandomSurprise()
    {
        while (true)
        {
            var rand = GetRandomPoke();
            if (DisallowRandomRecipientTrade(rand))
                continue;
            return rand;
        }
    }

    public bool Reload(string path, SearchOption opt = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(path))
            return false;
        Clear();
        Files.Clear();
        return LoadFolder(path, opt);
    }

    public bool LoadFolder(string path, SearchOption opt = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(path))
            return false;

        var loadedAny = false;
        var files = Directory.EnumerateFiles(path, "*", opt);
        var matchFiles = LoadUtil.GetFilesOfSize(files, ExpectedSize);
        int surpriseBlocked = 0;
        var pokemonsToAdd = new List<T>();
        var filesToAdd = new Dictionary<string, SurpriseTradeRequest<T>>();

        foreach (var file in matchFiles)
        {
            var data = File.ReadAllBytes(file);
            var prefer = EntityFileExtension.GetContextFromExtension(file, EntityContext.None);
            var pkm = EntityFormat.GetFromBytes(data, prefer);
            if (pkm is null)
                continue;
            if (pkm is not T)
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _);
            if (pkm is not T dest)
                continue;

            if (dest.Species == 0)
            {
                LogUtil.LogInfo("SKIPPED: Provided file is not valid: " + dest.FileName, nameof(PokemonSTPool<T>));
                continue;
            }

            (bool canBeTraded, string errorMessage) = dest.CanBeTraded();
            if (!canBeTraded)
            {
                LogUtil.LogInfo("SKIPPED: Provided file cannot be traded: " + dest.FileName + $" -- {errorMessage}", nameof(PokemonSTPool<T>));
                continue;
            }

            var la = new LegalityAnalysis(dest);
            if (!la.Valid)
            {
                var reason = la.Report();
                LogUtil.LogInfo($"SKIPPED: Provided file is not legal: {dest.FileName} -- {reason}", nameof(PokemonSTPool<T>));
                continue;
            }

            if (DisallowRandomRecipientTrade(dest, la.EncounterMatch))
            {
                LogUtil.LogInfo("SKIPPED: Provided file can't be Surprise traded:" + dest.FileName, nameof(PokemonSTPool<T>));
                surpriseBlocked++;
                continue;
            }

            var fn = Path.GetFileNameWithoutExtension(file);
            fn = StringsUtil.Sanitize(fn);

            // Since file names can be sanitized to the same string, only add one of them.
            if (!Files.ContainsKey(fn))
            {
                pokemonsToAdd.Add(dest);
                filesToAdd.Add(fn, new SurpriseTradeRequest<T>(dest, fn));
            }
            else
            {
                LogUtil.LogInfo("Provided file was not added due to duplicate name: " + dest.FileName, nameof(PokemonSTPool<T>));
            }
            loadedAny = true;
        }

        foreach (var pokemon in pokemonsToAdd)
        {
            Add(pokemon);
        }

        foreach (var kvp in filesToAdd)
        {
            Files.Add(kvp.Key, kvp.Value);
        }

        if (surpriseBlocked == Count)
            LogUtil.LogInfo("Surprise trading will fail; failed to load any compatible files.", nameof(PokemonSTPool<T>));

        return loadedAny;
    }

    private static bool DisallowRandomRecipientTrade(T pk, IEncounterTemplate enc)
    {
        // Anti-spam
        if (pk.IsNicknamed && StringsUtil.IsSpammyString(pk.Nickname))
            return true;
        if (StringsUtil.IsSpammyString(pk.OriginalTrainerName) && !AutoLegalityWrapper.IsFixedOT(enc, pk))
            return true;
        return DisallowRandomRecipientTrade(pk);
    }

    public static bool DisallowRandomRecipientTrade(T pk)
    {
        // Surprise Trade currently bans Mythicals and Legendaries, not Sub-Legendaries.
        if (SpeciesCategory.IsLegendary(pk.Species))
            return true;
        if (SpeciesCategory.IsMythical(pk.Species))
            return true;

        // Can't surprise trade fused stuff.
        if (FormInfo.IsFusedForm(pk.Species, pk.Form, pk.Format))
            return true;

        return false;
    }
}