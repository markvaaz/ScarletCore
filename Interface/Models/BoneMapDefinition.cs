using System.Collections.Generic;

namespace ScarletCore.Interface.Models;

/// <summary>How a source skeleton's bone drives the bone that stands for it on the player's rig.</summary>
public enum BoneTransferMode {
  /// <summary>Copy the bone's local rotation. Correct wherever the two rigs share that bone's local
  /// bind axes, which V Rising's <c>_JNT</c> convention holds for everywhere but the neck.</summary>
  Direct = 0,
  /// <summary>Point the target bone where the source bone points, by the shortest arc. For joints
  /// whose local axes differ between the rigs — the neck and head, and every limb of a rig that
  /// names bones anatomically (a quadruped's <c>Scapula/Humerus/RadiusUlna</c>).</summary>
  Aim = 1,
  /// <summary>No target: the bone exists only so the animation's binding path resolves and the chain
  /// below it composes. Ancestors of a mapped bone that the player's rig has no counterpart for.</summary>
  Carry = 2,
}

/// <summary>
/// One bone of a source skeleton, and the bone it drives on the player's rig.
/// </summary>
public sealed class BoneMapEntry {
  /// <summary>The path the animation binds to, from the root joint —
  /// <c>Root_JNT/Pelvis_JNT/Spine_JNT/Gut_JNT/Chest_JNT/Left_Collar_JNT</c>. This is the source rig's
  /// own hierarchy, not the player's; the client reproduces it as inert transforms so the animation's
  /// curves have something to bind to.</summary>
  public string SourcePath { get; set; } = string.Empty;

  /// <summary>Name of the bone on the player's rig that follows it, e.g. <c>Left_Collar_JNT</c>.
  /// Empty for <see cref="BoneTransferMode.Carry"/>.</summary>
  public string TargetBone { get; set; } = string.Empty;

  /// <summary>How the source bone drives its target on the player's rig.</summary>
  public BoneTransferMode Mode { get; set; } = BoneTransferMode.Direct;

  /// <summary>X of the source bone's local bind-pose rotation quaternion. Composed down the
  /// chain to recover where an aimed joint points at rest, which is what calibrates
  /// <see cref="BoneTransferMode.Aim"/>. Identity is a safe default for <see cref="BoneTransferMode.Direct"/>,
  /// which does not read it.</summary>
  public float RestX { get; set; }
  /// <summary>Y of the bind-pose rotation quaternion (see <see cref="RestX"/>).</summary>
  public float RestY { get; set; }
  /// <summary>Z of the bind-pose rotation quaternion (see <see cref="RestX"/>).</summary>
  public float RestZ { get; set; }
  /// <summary>W of the bind-pose rotation quaternion (see <see cref="RestX"/>); defaults to identity.</summary>
  public float RestW { get; set; } = 1f;

  /// <summary>Creates an empty entry (set the properties directly).</summary>
  public BoneMapEntry() { }

  /// <summary>Creates an entry mapping <paramref name="sourcePath"/> to <paramref name="targetBone"/>.</summary>
  /// <param name="sourcePath">Bind path on the source rig (see <see cref="SourcePath"/>).</param>
  /// <param name="targetBone">Bone on the player's rig that follows it (see <see cref="TargetBone"/>).</param>
  /// <param name="mode">How the target follows the source (see <see cref="Mode"/>).</param>
  public BoneMapEntry(string sourcePath, string targetBone, BoneTransferMode mode = BoneTransferMode.Direct) {
    SourcePath = sourcePath;
    TargetBone = targetBone;
    Mode = mode;
  }

  /// <summary>Sets the bind-pose rotation. Only <see cref="BoneTransferMode.Aim"/> reads it.</summary>
  public BoneMapEntry WithRest(float x, float y, float z, float w) {
    RestX = x; RestY = y; RestZ = z; RestW = w;
    return this;
  }
}

/// <summary>
/// Tells the client how to retarget animations authored for one skeleton onto the player's rig.
///
/// <para>Most animations need no map at all: the client derives one from the player's own rig, which
/// covers every animation authored for the humanoid NPC skeleton. Send a map only for a source
/// skeleton that one does not fit — a creature, a boss, anything whose bones are named or arranged
/// differently — identified by <see cref="SourceRig"/>.</para>
///
/// <para>Entries are per bone. A bone whose path is identical on both rigs needs no entry: the
/// animation reaches it directly. Bones the animation never writes cost nothing, so a map may be
/// generous.</para>
/// </summary>
public sealed class BoneMapDefinition {
  /// <summary>The rig this map is for, as the game names it —
  /// <c>CreatureStoneGolem_Standard_LOD00_rig</c>. Matched against the rig an animation was authored
  /// for; a map replaces any previously sent for the same rig.</summary>
  public string SourceRig { get; set; } = string.Empty;

  /// <summary>The per-bone retargeting entries that make up this map.</summary>
  public List<BoneMapEntry> Entries { get; set; } = [];

  /// <summary>Creates an empty definition (set <see cref="SourceRig"/> and add entries).</summary>
  public BoneMapDefinition() { }

  /// <summary>Creates a definition for the given source rig.</summary>
  /// <param name="sourceRig">The rig this map is for (see <see cref="SourceRig"/>).</param>
  public BoneMapDefinition(string sourceRig) {
    SourceRig = sourceRig;
  }

  /// <summary>Adds an entry and returns this definition, for chaining.</summary>
  /// <param name="entry">The entry to add.</param>
  public BoneMapDefinition Add(BoneMapEntry entry) {
    Entries.Add(entry);
    return this;
  }

  /// <summary>Adds an entry built from its parts and returns this definition, for chaining.</summary>
  /// <param name="sourcePath">Bind path on the source rig (see <see cref="BoneMapEntry.SourcePath"/>).</param>
  /// <param name="targetBone">Bone on the player's rig that follows it (see <see cref="BoneMapEntry.TargetBone"/>).</param>
  /// <param name="mode">How the target follows the source (see <see cref="BoneMapEntry.Mode"/>).</param>
  public BoneMapDefinition Add(string sourcePath, string targetBone, BoneTransferMode mode = BoneTransferMode.Direct) =>
    Add(new BoneMapEntry(sourcePath, targetBone, mode));
}
