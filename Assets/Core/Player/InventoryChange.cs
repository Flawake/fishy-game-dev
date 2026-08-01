// InventoryChange.cs
using System.Collections.Generic;
using ItemSystem;

/// <summary>
/// Everything one add did to an inventory: an entry per stack that changed, each carrying the amount
/// that stack gained rather than its new total.
///
/// The split between merged and created is what makes an optimistic add exactly reversible. A merged
/// stack existed before the add and has to keep whatever it already held, while a created stack only
/// exists because of the add and can be dropped whole.
/// </summary>
public class InventoryChange
{
    private readonly List<DeltaItem> all = new();
    private readonly List<DeltaItem> merged = new();
    private readonly List<DeltaItem> created = new();

    // Stacks that already existed and grew.
    public IReadOnlyList<DeltaItem> Merged => merged;

    // Stacks this change brought into existence.
    public IReadOnlyList<DeltaItem> Created => created;

    // Every changed stack, in the order the change touched them.
    public List<DeltaItem> All => all;

    public bool IsEmpty => all.Count == 0;

    /// <summary>
    /// Server only: the generations this change moved the inventory between. A client compares
    /// <see cref="GenerationBefore"/> against the last generation it was told about to notice that it
    /// is reconciling against a base state the server has since moved past.
    /// </summary>
    public uint GenerationBefore { get; set; }
    public uint GenerationAfter { get; set; }

    public void AddMerged(DeltaItem delta)
    {
        merged.Add(delta);
        all.Add(delta);
    }

    public void AddCreated(DeltaItem delta)
    {
        created.Add(delta);
        all.Add(delta);
    }

    /// <summary>
    /// Folds another change into this one, keeping each stack on the side it came in on. A created
    /// stack that came over as merged would survive a <see cref="PlayerInventory.Revert"/> with only
    /// its gained amount taken back off, leaving a stack behind that never existed before the change.
    /// </summary>
    public void MergeChanges(InventoryChange changed)
    {
        changed.merged.ForEach(AddMerged);
        changed.created.ForEach(AddCreated);
    }
}
