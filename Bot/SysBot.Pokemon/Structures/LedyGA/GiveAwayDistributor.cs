using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class GiveAwayDistributor<T> where T : PKM, new()
    {
        public readonly Dictionary<string, GiveAwayRequest<T>> UserRequests = new();
        public readonly Dictionary<string, GiveAwayRequest<T>> GiveAway;
        public readonly PokemonGAPool<T> Pool;

        private readonly List<GiveAwayUser> Previous = new();

        public GiveAwayDistributor(PokemonGAPool<T> GApool)
        {
            Pool = GApool;
            GiveAway = Pool.Files;
        }

        private const Species NoMatchSpecies = Species.None;

        public GiveAwayResponse<T>? GetGiveAwayTrade(T pk, ulong partnerId, Species speciesMatch = NoMatchSpecies)
        {
            if (speciesMatch != NoMatchSpecies && pk.Species != (int)speciesMatch)
                return null;

            var response = GetGiveAwayResponse(pk);
            if (response is null)
                return null;

            if (response.Type != GiveAwayResponseType.MatchRequest)
                return response;

            var previous = Previous.Find(z => z.Recipient == partnerId);
            if (previous is null)
            {
                AddRecipient(partnerId, response);
                return response;
            }

            if (!previous.CanReceive(response))
                return new GiveAwayResponse<T>(response.Receive, GiveAwayResponseType.AbuseDetected);

            UpdateRecipient(previous, response);
            return response;
        }

        private void UpdateRecipient(GiveAwayUser previous, GiveAwayResponse<T> response)
        {
            previous.Requests.Add(response);
        }

        private void AddRecipient(ulong partnerId, GiveAwayResponse<T> response)
        {
            Previous.Add(new GiveAwayUser(partnerId, response));
        }

        private GiveAwayResponse<T>? GetGiveAwayResponse(T pk)
        {
            // All the files should be loaded in as lowercase, regular-width text with no white spaces.
            var nick = StringsUtil.Sanitize(pk.Nickname);
            if (UserRequests.TryGetValue(nick, out var match))
                return new GiveAwayResponse<T>(match.RequestInfo, GiveAwayResponseType.MatchRequest);
            if (GiveAway.TryGetValue(nick, out match))
                return new GiveAwayResponse<T>(match.RequestInfo, GiveAwayResponseType.MatchPool);

            return null;
        }

        private sealed class GiveAwayUser
        {
            public readonly ulong Recipient;
            public readonly List<GiveAwayResponse<T>> Requests = new(1);

            public GiveAwayUser(ulong recipient, GiveAwayResponse<T> first)
            {
                Recipient = recipient;
                Requests.Add(first);
            }

            public bool CanReceive(GiveAwayResponse<T> response)
            {
                var poke = response.Receive;
                var prev = Requests.Find(z => ReferenceEquals(z.Receive, poke));
                if (prev is null)
                    return true;

                // Disallow receiving duplicate legends (prevents people farming the bot)
                if (SpeciesCategory.IsLegendary(poke.Species))
                    return false;
                if (SpeciesCategory.IsMythical(poke.Species))
                    return false;
                if (SpeciesCategory.IsSubLegendary(poke.Species))
                    return false;

                return true;
            }
        }
    }
}