using PKHeX.Core;
using SysBot.Pokemon.Helpers;

namespace SysBot.Pokemon;

public static class LanguageHelper
{
    public static string LAReport(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Reporte de Legalidad ",
            BotLanguage.Italiano => "Resoconto della Legalità",
            BotLanguage.Deutsch => $"Legalitätsbericht",
            BotLanguage.Français => "Rapport de Légalité",
            _ => "Legality Report"
        };
        return msg;
    }

    public static string Invalid(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Invalide",
            BotLanguage.Italiano => "Invalido",
            BotLanguage.Deutsch => $"Ungültig",
            BotLanguage.Français => "Invalide",
            _ => "Invalid"
        };
        return msg;

    }

    public static string Oops(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "¡Ups!",
            BotLanguage.Italiano => "Ops!",
            BotLanguage.Deutsch => $"Ups!",
            BotLanguage.Français => "Oups!",
            _ => "Oops!"
        };
        return msg;
    }

    public static string ParseSet(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "No se ha podido procesar el conjunto Showdown:",
            BotLanguage.Italiano => "Non sono riuscito ad analizzare il set di Showdown:",
            BotLanguage.Deutsch => $"Showdown-Set konnte nicht gelesen/analysiert werden:",
            BotLanguage.Français => "Impossible d'analyser le Set Showdown.",
            _ => "Unable to parse Showdown Set:"
        };
        return msg;
    }

    public static string BestAttempt(BotLanguage lang, Species spec)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"¡Aquí esta mi mejor intento para ese {spec}!",
            BotLanguage.Italiano => $"Ecco il mio migliore tentativo per quel {spec}!",
            BotLanguage.Deutsch => $"Hier ist mein bester versuch für {spec}!",
            BotLanguage.Français => $"Voici ma meilleure tentative {spec}!",
            _ => $"Here's my best attempt for that {spec}!"
        };
        return msg;

    }

    public static string BestDitto(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "¡Aquí esta mi mejor intento para ese Ditto!",
            BotLanguage.Italiano => "Ecco il migliore tentativo per quel Ditto!",
            BotLanguage.Deutsch => $"Hier ist mein bestes Ergebnis für Ditto!",
            BotLanguage.Français => "Voici ma meilleure tentative pour Métamorph!",
            _ => "Here's my best attempt for that Ditto!"
        };
        return msg;
    }

    public static string BadItem(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "el item que ingresaste no fue reconocido.",
            BotLanguage.Italiano => "Non ho riconosciuto lo strumento che hai inserito.",
            BotLanguage.Deutsch => $"Das von dir eingegebene Item wurde nicht erkannt",
            BotLanguage.Français => "L'object que vous avez saisi n'a pas été reconnu.",
            _ => "the item you entered wasn't recognized."
        };
        return msg;
    }

    public static string BadLanguage(BotLanguage lang, string language)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Couldn't recognize language: {language}.",
            BotLanguage.Italiano => $"Lingua non riconosciuta: {language}.",
            BotLanguage.Deutsch => $"Sprache konnte nicht erkannt werden {language}",
            BotLanguage.Français => $"Langue non reconnu: {language}.",
            _ => $"Couldn't recognize language: {language}."
        };
        return msg;
    }

    public static string NoBreed(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "no tiene la función de crianza!",
            BotLanguage.Italiano => "non permette l'accoppiarsi dei Pokémon!",
            BotLanguage.Deutsch => $"Kann nicht gezüchtet werden!",
            BotLanguage.Français => "Ne peut pas être élevé!",
            _ => "does not have breeding!"
        };
        return msg;
    }

    public static string NoEgg(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "¡El Pokémon enviado no puede ser un huevo!",
            BotLanguage.Italiano => "Il Pokémon richiesto non può essere in un uovo!",
            BotLanguage.Deutsch => $"Das gewünschte Pokémon kann nicht als Ei erstellt werden!",
            BotLanguage.Français => "Pokémon fourni ne peut être un oeuf!",
            _ => "Provided Pokémon cannot be an egg!"
        };
        return msg;
    }

    public static string Timeout(BotLanguage lang, string spec)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Ese {spec} set tardó mucho en generarse",
            BotLanguage.Italiano => $"Quella {spec} set ci ha messo troppo per generarsi.",
            BotLanguage.Deutsch => $"Das {spec} set hat zu lange gedauert um es zu erstellen.",
            BotLanguage.Français => $"Le set {spec} a pris trop de temps à générer.",
            _ => $"That {spec} set took too long to generate."
        };
        return msg;
    }

    public static string MemeSet(BotLanguage lang, string spec)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"No fui capaz de crear ese {spec}. ¡Aquí tienes un meme en su lugar!",
            BotLanguage.Italiano => $"Non sono riuscito a creare quel {spec}.\nEccoti un meme invece!",
            BotLanguage.Deutsch => $"Ich konnte das {spec} nicht erstellen. Hier dafür ein meme! ",
            BotLanguage.Français => $"Je n'ai pas plus créer ça {spec}.\nVoici un meme plutôt!",
            _ => $"I wasn't able to create that {spec}.\nHere's a meme instead!"
        };
        return msg;
    }

    public static string SimpleTimeout(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Ese set tardó mucho en generarse",
            BotLanguage.Italiano => "Quel set ci ha messo troppo a generarsi.",
            BotLanguage.Deutsch => $"Dieses set hat zu lange gedauert um es zu erstellen.",
            BotLanguage.Français => "Le set a pris trop de temps à générer.",
            _ => "That set took too long to generate."
        };
        return msg;
    }

    public static string Mismatch(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Solicitud rechazada: La versión de PKHeX y Auto-Legality Mod no coinciden",
            BotLanguage.Italiano => "Richiesta rifiutata: Versione di PKHeX e della Mod di Auto-Legalità non combaciano.",
            BotLanguage.Deutsch => $"Anfrage abgelehnt: PKHeX und Auto-Legality Mod Version nicht kompatibel.",
            BotLanguage.Français => "Demande refusée: La version PKHeX et Module Auto-Légalité non compatible.",
            _ => "Request refused: PKHeX and Auto-Legality Mod version mismatch."
        };
        return msg;
    }

    public static string Unable(BotLanguage lang, string spec)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"No he podido crear un {spec} con ese set.",
            BotLanguage.Italiano => $"Non sono riuscito a creare un {spec} da quel set.",
            BotLanguage.Deutsch => $"Ich konnte kein {spec} aus diesem set erstellen.",
            BotLanguage.Français => $"Je n'ai pas plus créer {spec} de ce set.",
            _ => $"I wasn't able to create a {spec} from that set."
        };
        return msg;
    }

    public static string SimpleUnable(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "No fui capaz de crear algo a partir de eso.",
            BotLanguage.Italiano => "Non sono riuscito a creare nulla da quello.",
            BotLanguage.Deutsch => $"Daraus konnte ich nichts erstellen.",
            BotLanguage.Français => "Je n'ai pas pu créer quelque chose à partir de ça.",
            _ => "I wasn't able to create something from that."
        };
        return msg;
    }

    public static string Unexpected(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "¡Ups! Ocurrió un problema inesperado con este Set de Showdown:",
            BotLanguage.Italiano => "Ops! È successo un problema inaspettato con questo set di Showdown:",
            BotLanguage.Deutsch => $"Ups! Mit dem Showdown set ist ein Problem aufgetretten:",
            BotLanguage.Français => "Oups! un problème inattendu est survenu avec ce Set Showdown:",
            _ => "Oops! An unexpected problem happened with this Showdown Set:"
        };
        return msg;
    }

    public static string SomethingHappened(BotLanguage lang, PokeTradeResult reason)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"¡Ups! Ocurrió algo. Cancelando el intercambio: {reason}",
            BotLanguage.Italiano => $"Ops! È successo qualcosa. Annullando lo scambio: {reason}.",
            BotLanguage.Deutsch => $"Ups! Etwas unerwartetes ist passiert. Tausch abgebrochen: {reason}",
            BotLanguage.Français => $"Oups! Quelque chose est survenu. Annulation de l'échange:{reason}.",
            _ => $"Oops! Something happened. Canceling the trade: {reason}."
        };
        return msg;
    }

    public static string QueueAdd(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Te he añadido a la cola. Te enviaré un mensaje aquí cuando comience tu intercambio.",
            BotLanguage.Italiano => "Ti ho aggiunto alla coda! Ti manderò un messaggio quando lo scambio sta per iniziare.",
            BotLanguage.Deutsch => $"Zur Warteliste hinzugefügt! Ich melde mich wenn du an der Reihe bist.",
            BotLanguage.Français => "Je t'ai ajouté à la file d'attente! Je t'enverrai un message ici lorsque ton échange commencera.",
            _ => "I've added you to the queue! I'll message you here when your trade is starting.",

        };
        return msg;
    }

    public static string AddedQueue(BotLanguage lang, PokeRoutineType type, string ticketID, int position, string receiving)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Añadido a la Cola de {type}{ticketID}. Posición Actual: {position}. {receiving} ",
            BotLanguage.Italiano => $"Aggiunto alla coda di {type}{ticketID}. Posizione attuale: {position}. {receiving}",
            BotLanguage.Deutsch => $"Zur Warteliste hinzugefügt {type}{ticketID}. Aktuelle position {position}. {receiving}",
            BotLanguage.Français => $"Ajouté à la {type} file {ticketID}. Position Actuelle: {position}. {receiving}",
            _ => $"Added to the {type} queue{ticketID}. Current Position: {position}. {receiving}"
        };
        return msg;
    }

    public static string AddedQueue2(BotLanguage lang, PokeRoutineType type, string ticketID)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Añadido a la Cola de {type} \n{ticketID}",
            BotLanguage.Italiano => $"Aggiunto alla coda di {type} \n{ticketID}",
            BotLanguage.Deutsch => $"Hinzugefügt {type} \n{ticketID}",
            BotLanguage.Français => $"Ajouté à la {type} file \n{ticketID}",
            _ => $"Added to the {type} queue \n{ticketID}"
        };
        return msg;
    }

    public static string AddedQueue3(BotLanguage lang, PokeRoutineType type, string ticketID, int position)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Añadido a la Cola de {type}{ticketID}. Posición Actual: {position}.",
            BotLanguage.Italiano => $"Aggiunto alla coda di {type}{ticketID}. Posizione attuale: {position}.",
            BotLanguage.Deutsch => $"Hinzugefügt {type}{ticketID}. Position {position}.",
            BotLanguage.Français => $"Ajouté à la {type} file{ticketID}. Position Actuelle: {position}.",
            _ => $"Added to the {type} queue{ticketID}. Current Position: {position}."
        };
        return msg;
    }

    public static string InQueue(BotLanguage lang, string user)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Lo siento {user}, ya tienes un intercambio en la cola",
            BotLanguage.Italiano => $"Scusa {user}, sei già in coda.",
            BotLanguage.Deutsch => $"Sry {user}, Du bist bereits in der Warteliste.",
            BotLanguage.Français => $"Désolé {user}, tu es déjà en file d'attente.",
            _ => $"Sorry {user}, you are already in the queue.",
        };
        return msg;
    }

    public static string FoundPartner(BotLanguage lang, string tradePartner)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Encontré a un jugador: {tradePartner}.Esperando por un Pokémon...",
            BotLanguage.Italiano => $"Trovato partner di Scambio: {tradePartner}. Offrimi un Pokémon...",
            BotLanguage.Deutsch => $"Tausch Partner gefunden: {tradePartner}. Warte auf ein Pokémon...",
            BotLanguage.Français => $"Lien vers partenaire d'échange trouvé: {tradePartner}. Attente d'un Pokémon...",
            _ => $"Found Link Trade partner: {tradePartner}. Waiting for a Pokémon..."
        };
        return msg;
    }

    public static string TradeCode(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Tu código de intercambio será:",
            BotLanguage.Italiano => "Il tuo codice di scambio sarà:",
            BotLanguage.Deutsch => $"Dein tausch code ist:",
            BotLanguage.Français => $"Votre code d'échange sera:",
            _ => "Your trade code will be:"
        };
        return msg;
    }

    public static string TradeSearch(BotLanguage lang, string trainer, string ign, int code, bool lgpe = false)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Te estoy esperando, {trainer}! Mi IGN es {ign}. El código es: {(lgpe ? "" : $"**{code:0000 0000}**.")}",
            BotLanguage.Italiano => $"Ti sto aspettando, {trainer}! Il mio IGN è **{ign}**. Il codice è: {(lgpe ? "" : $"**{code:0000 0000}**.")}",
            BotLanguage.Deutsch => $"Ich warte auf dich, {trainer}! Mein IGN ist **{ign}**. Dein code lautet: {(lgpe ? "" : $"**{code:0000 0000}**.")}",
            BotLanguage.Français => $"Je vous attends, {trainer}! Mon IGN est **{ign}**. Votre code est: {(lgpe ? "" : $"**{code:0000 0000}**.")}",

            _ => $"I'm waiting for you, {trainer}! My IGN is **{ign}**. Your code is: {(lgpe ? "" : $"**{code:0000 0000}**.")}"
        };
        return msg;
    }

    public static string TradeInit(BotLanguage lang, string receiving, int code, bool lgpe = false)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Empezando el intercambio{receiving}. Prepárate. Tu Código será: {(lgpe ? "" : $"**{code:0000 0000}**.")}",
            BotLanguage.Italiano => $"Sto preparando lo scambio{receiving}. Sii pronto per favore. Il codice sarà: {(lgpe ? "" : $"**{code:0000 0000}**.")}",
            BotLanguage.Deutsch => $"Ich bereite den tausch vor{receiving}. Halte dich bereit: Dein tausch code lautet: {(lgpe ? "" : $"**{code:0000 0000}**.")}",
            BotLanguage.Français => $"Début de l'échange{receiving}. S.V.P. soyez prêt. Votre code est: {(lgpe ? "" : $"**{code:0000 0000}**.")}",
            _ => $"Initializing trade{receiving}. Please be ready. Your code is: {(lgpe ? "" : $"**{code:0000 0000}**.")}"
        };
        return msg;
    }

    public static string TradeCancel(BotLanguage lang, PokeTradeResult reason)
    {
        var msg = lang switch
        {
            BotLanguage.Español => $"Intercambio cancelado: {reason}",
            BotLanguage.Italiano => $"Scambio cancellato: {reason} ",
            BotLanguage.Deutsch => $"Tausch abgebrochen: {reason}",
            BotLanguage.Français => $"Échange annulé: {reason}",
            _ => $"Trade canceled: {reason}"
        };
        return msg;
    }

    public static string TradeFinish(BotLanguage lang, ushort trade)
    {
        var msg = lang switch
        {
            BotLanguage.Español => trade != 0 ? "Intercambio finalizado. ¡Disfruta de tu Pokémon!" : "Intercambio finalizado.",
            BotLanguage.Italiano => trade != 0 ? "Scambio finito. Goditi il Pokémon!" : "Scambio concluso.",
            BotLanguage.Deutsch => trade != 0 ? "Tausch Erfolgreich. Viel spaß mit deinem neuen Pokémon!" : "Tauch Erfolgreich",
            BotLanguage.Français => trade != 0 ? "Échange terminé. Profitez de votre Pokémon!" : "Échange terminé",
            _ => trade != 0 ? "Trade finished. Enjoy your Pokémon!" : "Trade finished"
        };
        return msg;
    }

    public static string TradeReturn(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Aquí tienes lo que me intercambiaste:",
            BotLanguage.Italiano => "Ecco cosa mi hai scambiato:",
            BotLanguage.Deutsch => $"Hier ist was du mir zu geschickt hast",
            BotLanguage.Français => "Voici ce que vous m'avez échangé:",
            _ => "Here's What you traded me:"
        };
        return msg;
    }

    public static string QueueCP(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Parece que ya estas siendo procesado. No fuiste eliminado de la cola.",
            BotLanguage.Italiano => "Sembra che sia il tuo turno! Non ti ho rimosso dalle code.",
            BotLanguage.Deutsch => $"Sieht so aus als wärst du gerade an der Reihe! Ich konnte dich nicht aus den allen Wartelisten entfernen",
            BotLanguage.Français => "On dirait que vous êtes actuellement en cours de traitement! N'a pas été retiré de toutes les files d'attente.",
            _ => "Looks like you're currently being processed! Did not remove from all queues."
        };
        return msg;
    }

    public static string QueueCPRemoved(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Parece que ya estas siendo procesado. Eliminado de la cola.",
            BotLanguage.Italiano => "Sembra che sia il tuo turno! Rimosso dalla coda.",
            BotLanguage.Deutsch => $"Sieht so aus als wärst du gerade an der Reihe! Aus Warteliste entfernt.",
            BotLanguage.Français => "On dirait que vous êtes actuellement en cours de traitement! Retiré de la file d'attente.",
            _ => "Looks like you're currently being processed! Removed from queue."
        };
        return msg;
    }

    public static string QueueRemoved(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Te he eliminado de la cola.",
            BotLanguage.Italiano => "Ti ho rimosso dalla coda.",
            BotLanguage.Deutsch => $"Aus Warteliste entfernt.",
            BotLanguage.Français => $"Retiré de la file d'attente.",
            _ => "Removed you from the queue."
        };
        return msg;
    }

    public static string NotInQueue(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "Lo siento, actualmente no estas en la cola.",
            BotLanguage.Italiano => "Scusa, al momento non sei in coda.",
            BotLanguage.Deutsch => $"Entschuldige, du bist in keiner Warteliste.",
            BotLanguage.Français => $"Désolé, vous n'êtes actuellement pas dans la file d'attente.",
            _ => "Sorry, you are not currently in the queue."
        };
        return msg;
    }

    public static string RequeueAttempt(BotLanguage lang)
    {
        var msg = lang switch
        {
            BotLanguage.Español => "¡Ups! Ocurrió algo. Te volveré a poner en la cola para otro intento.",
            BotLanguage.Italiano => "Ops! È successo qualcosa. Ti rimetto in cosa per un altro tentativo.",
            BotLanguage.Deutsch => $"Ups! Etwas ist schief gelaufen. Ich füge dich erneut für einen weiteren versuch der aktuellen Warteliste hinzu.",
            BotLanguage.Français => "Oups! Quelque chose est arrivé. Je vous remets en file d'attente pour une autre tentative.",
            _ => "Oops! Something happened. I'll requeue you for another attempt."
        };
        return msg;
    }
}