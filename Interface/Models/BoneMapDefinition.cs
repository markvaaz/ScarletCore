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

  public BoneTransferMode Mode { get; set; } = BoneTransferMode.Direct;

  /// <summary>The source bone's local rotation in its bind pose, as a quaternion. Composed down the
  /// chain to recover where an aimed joint points at rest, which is what calibrates
  /// <see cref="BoneTransferMode.Aim"/>. Identity is a safe default for <see cref="BoneTransferMode.Direct"/>,
  /// which does not read it.</summary>
  public float RestX { get; set; }
  public float RestY { get; set; }
  public float RestZ { get; set; }
  public float RestW { get; set; } = 1f;

  public BoneMapEntry() { }

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

  public List<BoneMapEntry> Entries { get; set; } = [];

  public BoneMapDefinition() { }

  public BoneMapDefinition(string sourceRig) {
    SourceRig = sourceRig;
  }

  public BoneMapDefinition Add(BoneMapEntry entry) {
    Entries.Add(entry);
    return this;
  }

  public BoneMapDefinition Add(string sourcePath, string targetBone, BoneTransferMode mode = BoneTransferMode.Direct) =>
    Add(new BoneMapEntry(sourcePath, targetBone, mode));
}
