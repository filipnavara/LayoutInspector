namespace Schaumamal.Models.Dumper;

public class NicknameProvider
{
    public string GetNext(string? current)
    {
        if (current == null) return Nicknames[0];
        var idx = Array.IndexOf(Nicknames, current);
        if (idx == -1) return Nicknames[0];
        return Nicknames[(idx + 1) % Nicknames.Length];
    }

    public static readonly string[] Nicknames =
    [
        "Frodo", "Sam", "Merry", "Pippin", "Aragorn", "Legolas", "Gimli", "Boromir",
        "Gandalf", "Gollum", "Sauron", "Elrond", "Galadriel", "Arwen", "Faramir",
        "Théoden", "Eomer", "Eowyn", "Isildur", "Radagast", "Saruman", "Thorin",
        "Smaug", "Beorn", "Glorfindel",
        "Zeus", "Hera", "Ares", "Apollo", "Athena", "Artemis", "Hermes", "Hades",
        "Perseus", "Achilles", "Hector", "Medusa", "Circe", "Pandora", "Theseus",
        "Minos", "Demeter", "Orpheus", "Icarus", "Eurydice",
        "Thor", "Odin", "Loki", "Freya", "Tyr", "Balder", "Fenrir", "Jörmungandr",
        "Heimdall", "Skadi", "Frigg", "Njord", "Aegir", "Valkyrie", "Hel", "Bragi",
        "Mimir", "Sif", "Eir",
        "Țepeș", "Viteazul", "Brâncoveanu", "Bălcescu", "Cantemir", "Eminescu",
        "Iorga", "Eliade", "Coandă", "Kogălniceanu", "Caragiale", "Creangă"
    ];
}
