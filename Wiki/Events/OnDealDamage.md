# OnDealDamage Event

The `OnDealDamage` event in ScarletCore allows you to listen for, inspect, and react to all damage events that occur in the game. This is useful for custom damage logic, analytics, damage-based triggers, or implementing special effects and mechanics in your mod.

---

## Event Signature

```csharp
public static event EventHandler<DamageEventArgs> OnDealDamage;
```

- **sender**: The `StatChangeSystem` instance that detected and raised the event.
- **args**: A `DamageEventArgs` object containing all relevant information about the batch of damage events.

---

## DamageEventArgs Properties

- `List<DamageInfo> DamageInstances` — List of all damage instances that occurred in the frame
- `int DamageCount` — Number of damage instances in this batch

### DamageInfo Properties
- `Entity Attacker` — The entity that dealt the damage
- `Entity Target` — The entity that received the damage

---

## Basic Usage

### Listen for all damage events
```csharp
EventManager.OnDealDamage += (sender, args) => {
    foreach (var dmg in args.DamageInstances) {
        Log.Info($"{dmg.Attacker} dealt damage to {dmg.Target}");
    }
};
```

### Count total damage events in a frame
```csharp
EventManager.OnDealDamage += (sender, args) => {
    Log.Info($"Damage events this frame: {args.DamageCount}");
};
```

---

## Using a Method Handler

You can use a named method for your handler. The method must match the event signature:

```csharp
void OnDealDamageHandler(object sender, DamageEventArgs args) {
    // sender is the StatChangeSystem instance
    foreach (var dmg in args.DamageInstances) {
        // Custom logic here
    }
}

EventManager.OnDealDamage += OnDealDamageHandler;
EventManager.OnDealDamage -= OnDealDamageHandler; // Unsubscribe when done
```

> **Note:** When using a method handler, the first parameter must be of type `object` (the sender, which is the StatChangeSystem instance), and the second parameter must be of type `DamageEventArgs`.

---

## Advanced: Accessing the Sender (StatChangeSystem)

The `sender` parameter is the `StatChangeSystem` instance that detected the damage. You can cast it if you need to access system-specific methods or properties:

```csharp
EventManager.OnDealDamage += (sender, args) => {
    var statSystem = sender as StatChangeSystem;
    // Use statSystem if needed
};
```

---

## Multiple Handlers

You can register multiple handlers. All will be called in order.

---

## Unsubscribing

Always unsubscribe your handler when it is no longer needed to avoid memory leaks:

```csharp
EventManager.OnDealDamage -= OnDealDamageHandler;
```

---

## When to Use

- Custom damage logic or effects
- Analytics and logging
- Damage-based triggers or achievements
- Modifying or blocking damage (if supported by future versions)

> **Tip:** For more advanced damage features, combine this event with other ScarletCore systems and services.
