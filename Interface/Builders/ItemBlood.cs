namespace ScarletCore.Interface.Builders;

/// <summary>
/// The blood stored in a potion (the game's <c>StoredBlood</c>): primary blood type + quality, plus
/// the infused secondary blood of a mixed potion. Manual form of <c>ItemViewer.Blood</c> — the
/// entity form reads it from the item itself, and only when the item's PUBLIC identity is a blood
/// potion (a custom item riding a potion entity as its carrier shows no blood).
/// </summary>
/// <param name="Type">Primary blood type prefab guid (BloodType_*).</param>
/// <param name="Quality">Primary blood quality, 0–100.</param>
/// <param name="SecondaryType">Infused blood type of a mixed potion, 0 = none.</param>
/// <param name="SecondaryQuality">Infused blood quality, 0–100.</param>
/// <param name="SecondaryBuffIndex">Which of the infused blood's tier buffs applies (StoredBlood.SecondaryBlood.BuffIndex).</param>
public readonly record struct ItemBlood(int Type, float Quality, int SecondaryType = 0,
    float SecondaryQuality = 0f, int SecondaryBuffIndex = 0);
