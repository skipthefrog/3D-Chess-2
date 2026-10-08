using UnityEngine;

/// <summary>
/// Player names are generated, never typed, so there is nothing offensive to moderate.
/// A name is an adjective + a noun + a number from 1 to 99, e.g. "NeonRook42".
/// Keep these lists in sync with server-cf/src/names.ts (the server rejects anything else).
/// </summary>
public static class PlayerNames
{
    public static readonly string[] Adjectives =
    {
        "Neon", "Turbo", "Glitchy", "Cosmic", "Pixel", "Laser", "Hyper", "Chrome",
        "Electric", "Lucky", "Sneaky", "Fuzzy", "Zippy", "Mega", "Retro", "Wobbly",
        "Sparkly", "Funky", "Jolly", "Brave", "Swift", "Quantum", "Rocket", "Disco",
        "Shiny", "Bouncy", "Mighty", "Clever", "Groovy", "Stellar", "Atomic", "Sunny",
    };

    public static readonly string[] Nouns =
    {
        "Rook", "Knight", "Bishop", "Pawn", "Queen", "King", "Castle", "Gambit",
        "Robot", "Comet", "Panda", "Otter", "Falcon", "Taco", "Waffle", "Pickle",
        "Noodle", "Nova", "Dragon", "Frog", "Llama", "Yeti", "Wizard", "Penguin",
        "Koala", "Rocket", "Meteor", "Cactus", "Donut", "Narwhal", "Phoenix", "Gecko",
    };

    private const string PrefKey = "PlayerGeneratedName";

    public static string Random()
    {
        string adjective = Adjectives[UnityEngine.Random.Range(0, Adjectives.Length)];
        string noun;
        do noun = Nouns[UnityEngine.Random.Range(0, Nouns.Length)]; while (noun == adjective); // no "RocketRocket"
        return $"{adjective}{noun}{UnityEngine.Random.Range(1, 100)}";
    }

    /// <summary>This device's current name, created the first time it's needed</summary>
    public static string Current
    {
        get
        {
            string name = PlayerPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(name)) Current = name = Random();
            return name;
        }
        set
        {
            PlayerPrefs.SetString(PrefKey, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Pick a new name, different from the current one</summary>
    public static string Reroll()
    {
        string old = Current, next;
        do next = Random(); while (next == old);
        Current = next;
        return next;
    }
}
