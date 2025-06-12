# OnOtherDeath Event

The `OnOtherDeath` event in ScarletCore allows you to listen for, inspect, and react to unfiltered or miscellaneous death events that occur in the game. This is useful for handling deaths that do not fit into more specific categories (such as player, V Blood, or servant deaths), enabling custom logic, analytics, or special effects for these cases.

---

## Event Signature

```csharp
public static event EventHandler<DeathEventArgs> OnOtherDeath;
```

- **sender**: The `DeathEventListenerSystem` instance that detected and raised the event.
- **args**: A `DeathEventArgs` object containing all relevant information about the batch of deaths.

---

## DeathEventArgs Properties

- `List<DeathInfo> Deaths` — List of all deaths that occurred in the frame
- `int DeathCount` — Number of deaths in this batch

### DeathInfo Properties
- `Entity Died` — The entity that died
- `Entity Killer` — The entity that caused the death
- `Entity Source` — The source of the damage/death

---

## Basic Usage

### Listen for all 'other' death events
```csharp
EventManager.OnOtherDeath += (sender, args) => {
    foreach (var death in args.Deaths) {
        Log.Info($"[OtherDeath] {death.Died} was killed by {death.Killer} (source: {death.Source})");
    }
};
```

### Count total 'other' deaths in a frame
```csharp
EventManager.OnOtherDeath += (sender, args) => {
    Log.Info($"Other deaths this frame: {args.DeathCount}");
};
```

---

## Using a Method Handler

You can use a named method for your handler. The method must match the event signature:

```csharp
void OnOtherDeathHandler(object sender, DeathEventArgs args) {
    // sender is the DeathEventListenerSystem instance
    foreach (var death in args.Deaths) {
        // Custom logic here
    }
}

EventManager.OnOtherDeath += OnOtherDeathHandler;
EventManager.OnOtherDeath -= OnOtherDeathHandler; // Unsubscribe when done
```

> **Note:** When using a method handler, the first parameter must be of type `object` (the sender, which is the DeathEventListenerSystem instance), and the second parameter must be of type `DeathEventArgs`.

---

## Advanced: Accessing the Sender (DeathEventListenerSystem)

The `sender` parameter is the `DeathEventListenerSystem` instance that detected the death. You can cast it if you need to access system-specific methods or properties:

```csharp
EventManager.OnOtherDeath += (sender, args) => {
    var deathSystem = sender as DeathEventListenerSystem;
    // Use deathSystem if needed
};
```

---

## Multiple Handlers

You can register multiple handlers. All will be called in order.

---

## Unsubscribing

Always unsubscribe your handler when it is no longer needed to avoid memory leaks:

```csharp
EventManager.OnOtherDeath -= OnOtherDeathHandler;
```

---

## When to Use

- Custom logic for non-player/non-boss/non-servant deaths
- Analytics and logging for miscellaneous deaths
- Special effects or triggers for environmental or neutral entity deaths

> **Tip:** For more advanced death features, combine this event with other ScarletCore systems and services.
