using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScarletCore.Interface.Models;

/// <summary>
/// Something to put in the character's hand while a bound animation plays — a pitchfork for raking,
/// a hammer for the anvil.
///
/// <para>The game's own interaction animations bring no prop: when a farmer rakes, the rake is the
/// farmer's weapon, animated by the clip like any weapon; a villager with nothing equipped plays the
/// same animation empty-handed. A player therefore holds whatever they equipped, which is wrong for
/// nearly every animation — so the prop is named here and the client dresses the hand for exactly as
/// long as the animation runs, hiding the equipped weapon (both hands) meanwhile.</para>
///
/// <para>Mesh and material are the game's own assets, by name, and the model that owns them (an NPC's
/// model or an item's) is streamed in on demand when named. The presets below are verified; anything
/// else comes from the client's prop catalogue — <c>{"animpropdump": ""}</c> in its debug-chat.json
/// writes <c>prop-catalog.txt</c>, one SPEC line per prop, which <see cref="FromSpec"/> reads.</para>
///
/// <para><b>Defaults.</b> The game's own interaction buffs carry their prop on the client with no
/// server involvement: <c>Buff_IdleInteraction_Raking</c> shows the pitchfork, the anvil and forge
/// buffs the hammer, whittling and knife-throwing the knife, on whoever carries them, bound or not. A
/// binding on one of those buffs keeps that default unless it names a Prop of its own, or
/// <see cref="None"/> to play empty-handed.</para>
///
/// <para><b>Any buff.</b> Not only animations: <see cref="ScarletCore.Interface.InterfaceManager.SetAnimationPropAll"/> gives
/// any buff a prop, and whoever carries the buff holds it, seen by every client. Call it again with
/// another prop and characters wearing the buff switch on the spot. A tool system is one buff per tool
/// and one call per tool — the axe buff shows an axe, hiding the equipped weapon in both hands:
/// <code>
/// var axe = AnimationProp.FromSpec("Axe_GEO|BoneAxe_1H_Standard|...|0,0,0;0,0,0;1|Weapon01_JNT");
/// InterfaceManager.SetAnimationPropAll(MyPlugin, AxeToolBuff, axe);
/// // two hands: a dagger in each
/// var daggers = AnimationProp.FromSpec(rightSpec).With(AnimationProp.FromSpec(leftSpec));
/// </code>
/// The specs are the SPEC lines of the client's prop catalogue (<c>prop-catalog.txt</c>). When a
/// character carries several buffs with a prop, one wins: an animation binding's own prop, then a prop
/// set here, then the game's default; between equals the one already shown stays.</para>
/// </summary>
public sealed class AnimationProp {
  /// <summary>Wire form of <see cref="None"/>.</summary>
  public const string NoneMesh = "-";

  /// <summary>Empty hands: no prop, not even the buff's default, and the equipped weapon hidden (both
  /// hands) — for a binding that plays something on a raking or anvil carrier that a pitchfork or
  /// hammer would be wrong for, or a buff that should just put the weapon away. Chain
  /// <see cref="KeepWeapon"/> for "no prop, weapon stays".</summary>
  public static AnimationProp None => new(NoneMesh);

  /// <summary>Same as <see cref="None"/>: hides the equipped weapon, shows nothing — named for the use.</summary>
  public static AnimationProp HiddenWeapon => None;

  /// <summary>True for <see cref="None"/>.</summary>
  public bool IsNone => Mesh == NoneMesh;
  /// <summary>Right-hand weapon joint, present on every player rig.</summary>
  public const string RightHand = "Weapon01_JNT";
  /// <summary>Left-hand weapon joint.</summary>
  public const string LeftHand = "Weapon02_JNT";

  /// <summary>Mesh name as the game holds it — <c>PitchFork_GEO</c>.</summary>
  public string Mesh { get; set; } = string.Empty;

  /// <summary>Material name — <c>NPCLittleGuy_PitchFork01</c>. Empty falls back to the material of
  /// the weapon the prop replaces, which renders but with the wrong texture.</summary>
  public string Material { get; set; } = string.Empty;

  /// <summary>Joint the prop hangs from; see <see cref="RightHand"/> and <see cref="LeftHand"/>.</summary>
  public string Bone { get; set; } = RightHand;

  /// <summary>Local offset from the joint, in world units. Zero puts the prop exactly where the NPC's
  /// own weapon sits, which is right for anything authored for the humanoid NPC skeleton.</summary>
  public float PositionX { get; set; }
  /// <inheritdoc cref="PositionX"/>
  public float PositionY { get; set; }
  /// <inheritdoc cref="PositionX"/>
  public float PositionZ { get; set; }

  /// <summary>Local rotation from the joint, Euler degrees.</summary>
  public float RotationX { get; set; }
  /// <inheritdoc cref="RotationX"/>
  public float RotationY { get; set; }
  /// <inheritdoc cref="RotationX"/>
  public float RotationZ { get; set; }

  /// <summary>Uniform scale; 1 is the mesh's authored size.</summary>
  public float Scale { get; set; } = 1f;

  /// <summary>Hide the equipped weapon — both hands, so a dual or two-handed weapon goes away whole —
  /// while the prop is on. Default true: a pitchfork through an axe looks like exactly that.</summary>
  public bool HideWeapon { get; set; } = true;

  /// <summary>The hybrid model asset that owns the mesh, as the four ints of its
  /// <c>AssetGuid</c> — the farmer's model for his pitchfork. With it the client streams the model in
  /// on demand, the way an NPC in view would; without it the mesh resolves only while some unit that
  /// uses it happens to be loaded. Read it off a unit's <c>HybridModelGuid</c> on the client.</summary>
  public int AssetA { get; set; }
  /// <inheritdoc cref="AssetA"/>
  public int AssetB { get; set; }
  /// <inheritdoc cref="AssetA"/>
  public int AssetC { get; set; }
  /// <inheritdoc cref="AssetA"/>
  public int AssetD { get; set; }

  /// <summary>True when an owning model asset was named (see <see cref="AssetA"/>).</summary>
  public bool HasAsset => AssetA != 0 || AssetB != 0 || AssetC != 0 || AssetD != 0;

  /// <summary>Creates an empty prop (set the properties directly).</summary>
  public AnimationProp() { }

  /// <summary>Creates a prop from a mesh and material name.</summary>
  public AnimationProp(string mesh, string material = "", string bone = RightHand) {
    Mesh = mesh;
    Material = material ?? string.Empty;
    Bone = string.IsNullOrEmpty(bone) ? RightHand : bone;
  }

  /// <summary>Sets the local offset and returns this prop, for chaining.</summary>
  public AnimationProp At(float x, float y, float z) { PositionX = x; PositionY = y; PositionZ = z; return this; }
  /// <summary>Sets the local rotation (Euler degrees) and returns this prop, for chaining.</summary>
  public AnimationProp Rotated(float x, float y, float z) { RotationX = x; RotationY = y; RotationZ = z; return this; }
  /// <summary>Sets the uniform scale and returns this prop, for chaining.</summary>
  public AnimationProp Scaled(float scale) { Scale = scale > 0f ? scale : 1f; return this; }
  /// <summary>Hangs the prop from the left hand and returns this prop, for chaining.</summary>
  public AnimationProp InLeftHand() { Bone = LeftHand; return this; }
  /// <summary>Keeps the equipped weapon visible and returns this prop, for chaining.</summary>
  public AnimationProp KeepWeapon() { HideWeapon = false; return this; }
  /// <summary>Names the model asset that owns the mesh (see <see cref="AssetA"/>) and returns this
  /// prop, for chaining.</summary>
  public AnimationProp FromAsset(int a, int b, int c, int d) { AssetA = a; AssetB = b; AssetC = c; AssetD = d; return this; }

  /// <summary>More meshes, each with its own offset — for a prop the game builds from several (the
  /// alchemist's potion is a bottle and a cap), or one per hand (a dagger in each, an axe and a
  /// shield). A part hangs from the main mesh's joint and comes from its model asset unless it names
  /// its own (<see cref="AnimationPropPart.Bone"/>, <see cref="AnimationPropPart.AssetA"/>).</summary>
  public List<AnimationPropPart> Parts { get; set; } = [];

  /// <summary>Adds a part (see <see cref="Parts"/>) and returns this prop, for chaining.
  /// <paramref name="bone"/> empty = the main mesh's joint.</summary>
  public AnimationProp WithPart(string mesh, string material, float x = 0f, float y = 0f, float z = 0f,
      float rotationX = 0f, float rotationY = 0f, float rotationZ = 0f, float scale = 1f, string bone = "") {
    Parts.Add(new AnimationPropPart {
      Mesh = mesh, Material = material ?? string.Empty, Bone = bone ?? string.Empty,
      PositionX = x, PositionY = y, PositionZ = z,
      RotationX = rotationX, RotationY = rotationY, RotationZ = rotationZ,
      Scale = scale > 0f ? scale : 1f,
    });
    return this;
  }

  /// <summary>Adds a part (see <see cref="Parts"/>) and returns this prop, for chaining.</summary>
  public AnimationProp WithPart(AnimationPropPart part) {
    if (part != null && !string.IsNullOrWhiteSpace(part.Mesh)) Parts.Add(part);
    return this;
  }

  /// <summary>
  /// Adds everything <paramref name="other"/> holds — its main mesh and its parts, each keeping its
  /// own joint, offset and model — as parts of this prop, and returns this prop, for chaining. How to
  /// fill both hands: <c>FromSpec(rightDagger).With(FromSpec(leftDagger))</c>, or an axe and a shield
  /// from two different models.
  /// </summary>
  public AnimationProp With(AnimationProp other) {
    if (other == null || other.IsNone || string.IsNullOrWhiteSpace(other.Mesh)) return this;
    Parts.Add(new AnimationPropPart {
      Mesh = other.Mesh, Material = other.Material, Bone = other.Bone,
      PositionX = other.PositionX, PositionY = other.PositionY, PositionZ = other.PositionZ,
      RotationX = other.RotationX, RotationY = other.RotationY, RotationZ = other.RotationZ, Scale = other.Scale,
      AssetA = other.AssetA, AssetB = other.AssetB, AssetC = other.AssetC, AssetD = other.AssetD,
    });
    foreach (AnimationPropPart part in other.Parts) {
      if (part == null) continue;
      Parts.Add(new AnimationPropPart {
        Mesh = part.Mesh, Material = part.Material,
        Bone = string.IsNullOrEmpty(part.Bone) ? other.Bone : part.Bone,
        PositionX = part.PositionX, PositionY = part.PositionY, PositionZ = part.PositionZ,
        RotationX = part.RotationX, RotationY = part.RotationY, RotationZ = part.RotationZ, Scale = part.Scale,
        AssetA = part.HasAsset ? part.AssetA : other.AssetA, AssetB = part.HasAsset ? part.AssetB : other.AssetB,
        AssetC = part.HasAsset ? part.AssetC : other.AssetC, AssetD = part.HasAsset ? part.AssetD : other.AssetD,
      });
    }
    return this;
  }

  /// <summary>
  /// A prop from a SPEC line of the client's prop catalogue (<c>prop-catalog.txt</c>):
  /// <c>mesh|material|a,b,c,d|px,py,pz;rx,ry,rz;scale|bone</c>. Every field after the mesh is optional
  /// (empty asset = resolve by name only, empty bone = right hand). Null when the text has no mesh.
  /// </summary>
  public static AnimationProp FromSpec(string spec) {
    if (string.IsNullOrWhiteSpace(spec)) return null;
    string[] f = spec.Trim().Trim('"').Split('|');
    if (f[0].Trim().Length == 0) return null;
    var prop = new AnimationProp(f[0].Trim(), f.Length > 1 ? f[1].Trim() : "", f.Length > 4 ? f[4].Trim() : RightHand);
    if (f.Length > 2) {
      string[] g = f[2].Split(',');
      if (g.Length == 4 && TryInt(g[0], out int a) && TryInt(g[1], out int b) && TryInt(g[2], out int c) && TryInt(g[3], out int d))
        prop.FromAsset(a, b, c, d);
    }
    if (f.Length > 3 && f[3].Trim().Length > 0) {
      string[] o = f[3].Split(';');
      if (o.Length > 0 && TryVector(o[0], out float px, out float py, out float pz)) prop.At(px, py, pz);
      if (o.Length > 1 && TryVector(o[1], out float rx, out float ry, out float rz)) prop.Rotated(rx, ry, rz);
      if (o.Length > 2 && TryFloat(o[2], out float s)) prop.Scaled(s);
    }
    return prop;
  }

  static bool TryInt(string s, out int v) => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
  static bool TryFloat(string s, out float v) => float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
  static bool TryVector(string s, out float x, out float y, out float z) {
    x = y = z = 0f;
    string[] c = s.Split(',');
    return c.Length == 3 && TryFloat(c[0], out x) && TryFloat(c[1], out y) && TryFloat(c[2], out z);
  }

  // ── Presets ──────────────────────────────────────────────────────────────────
  // Mesh, material, model and offset come from the client's prop catalogue ("animpropdump" in
  // debug-chat.json), i.e. exactly how the owning NPC holds them. The client applies the matching
  // preset by default for the game's own interaction buffs; these are for binding them yourself.

  /// <summary>The Dunley farmer's pitchfork: the tool the game's raking NPCs actually hold. Streams
  /// in the farmer's model (<c>CHAR_Farmlands_Farmer</c>) on demand.</summary>
  public static AnimationProp Pitchfork =>
    new AnimationProp("PitchFork_GEO", "NPCLittleGuy_PitchFork01").FromAsset(-1254399096, 1410466018, -1505441607, 1365596224);

  /// <summary>Same as <see cref="Pitchfork"/> — named for the animation it goes with
  /// (<c>Idle_FarmerRaking</c>).</summary>
  public static AnimationProp Rake => Pitchfork;

  /// <summary>Valyr's small smithing hammer (<c>CHAR_Blackfang_Valyr_VBlood</c>), for the anvil and
  /// forge animations. He holds it in the left hand; this is the same grip mirrored to the right.</summary>
  public static AnimationProp Hammer =>
    new AnimationProp("Hammer_Small01_GEO", "NPCVampireMale_BlackFang_ValyrHammer")
      .FromAsset(1143887108, -199784028, -192808118, -1312104014).At(0f, 0f, 0.246f);

  /// <summary>Grayson the Armourer's big two-handed hammer (<c>CHAR_Bandit_Stalker_VBlood</c>).</summary>
  public static AnimationProp Sledgehammer =>
    new AnimationProp("Hammer_GEO", "NPCLumberJack_BanditArmorer_Hammer")
      .FromAsset(-1521710931, -464344066, 1324641672, -1262644630).At(0f, 0f, 0.25f);

  /// <summary>The Blackfang wood carver's knife, for whittling (<c>Idle_Whittling</c>) and
  /// knife-throwing. Streams in the carver's model (<c>CHAR_Blackfang_WoodCarver</c>).</summary>
  public static AnimationProp Knife =>
    new AnimationProp("Knife_GEO", "Blackfang_Carver_Knife").FromAsset(-1491426943, -198716464, -357635509, 402398327);

  /// <summary>The shovel of the Dunley villager who fights with one
  /// (<c>CHAR_Farmlands_HostileVillager_Male_Shovel</c>), for the digging animations.</summary>
  public static AnimationProp Shovel =>
    new AnimationProp("Tools_MineShovel02", "WoodRustyMetal01")
      .FromAsset(-178430282, 1687685338, 738487337, -1791968684).At(0f, 0f, 0.417f).Rotated(0f, -180f, 0f);

  /// <summary>The Blackfang alchemist's potion (<c>CHAR_Blackfang_Alchemist</c>), bottle and cap, for
  /// the alchemy table animations.</summary>
  public static AnimationProp Potion =>
    new AnimationProp("WeaponBottle_GEO", "NPCDeadeye_BlackFang_Alchemist_Potion04")
      .FromAsset(940783332, -456279739, 558624729, 1664031140).At(0f, 0.017f, 0f).Rotated(0f, -71.796f, 0f).Scaled(1.141f)
      .WithPart("WeaponBottleCap_GEO", "NPCDeadeye_BlackFang_Alchemist01", 0f, 0.017f, 0f, 0f, -71.796f, 0f, 1.141f);
}

/// <summary>One extra mesh of an <see cref="AnimationProp"/>, with its own offset — and, when it
/// differs from the main mesh's, its own joint and model.</summary>
public sealed class AnimationPropPart {
  /// <summary>Mesh name as the game holds it.</summary>
  public string Mesh { get; set; } = string.Empty;
  /// <summary>Material name.</summary>
  public string Material { get; set; } = string.Empty;
  /// <summary>Joint it hangs from; empty = the main mesh's. <see cref="AnimationProp.LeftHand"/> puts
  /// the second weapon of a pair in the other hand.</summary>
  public string Bone { get; set; } = string.Empty;
  /// <summary>Model asset that owns the mesh; all zero = the main mesh's.</summary>
  public int AssetA { get; set; }
  /// <inheritdoc cref="AssetA"/>
  public int AssetB { get; set; }
  /// <inheritdoc cref="AssetA"/>
  public int AssetC { get; set; }
  /// <inheritdoc cref="AssetA"/>
  public int AssetD { get; set; }
  /// <summary>True when the part names a model of its own.</summary>
  public bool HasAsset => AssetA != 0 || AssetB != 0 || AssetC != 0 || AssetD != 0;
  /// <summary>Local offset from the joint.</summary>
  public float PositionX { get; set; }
  /// <inheritdoc cref="PositionX"/>
  public float PositionY { get; set; }
  /// <inheritdoc cref="PositionX"/>
  public float PositionZ { get; set; }
  /// <summary>Local rotation from the joint, Euler degrees.</summary>
  public float RotationX { get; set; }
  /// <inheritdoc cref="RotationX"/>
  public float RotationY { get; set; }
  /// <inheritdoc cref="RotationX"/>
  public float RotationZ { get; set; }
  /// <summary>Uniform scale.</summary>
  public float Scale { get; set; } = 1f;
}
