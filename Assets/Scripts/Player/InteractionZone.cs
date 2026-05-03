using System.Collections.Generic;
using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    private readonly List<IInteractable> _inRange = new();
    public IInteractable CurrentInteractable { get; private set; }

    public void ForceRefresh() => RefreshCurrent();

    // Re-evaluate every frame so interactables that become available while the
    // player is already inside the zone are picked up without requiring the
    // player to leave and re-enter (e.g. the sliding penguin finishing its slide).
    private void Update() => RefreshCurrent();

    private void OnTriggerEnter(Collider other)
    {
        // Always track every interactable that enters range — CanInteract() may
        // return false right now (e.g. penguin is mid-slide) but become true later.
        // GetClosest() already filters by CanInteract(), so nothing is shown early.
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            _inRange.Add(interactable);
            RefreshCurrent();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            _inRange.Remove(interactable);
            RefreshCurrent();
        }
    }

    private void RefreshCurrent()
    {
        _inRange.RemoveAll(i => {
            var mb = i as MonoBehaviour;
            return mb == null || !mb.gameObject.activeInHierarchy;
        });

        var best = GetClosest();
        if (best == CurrentInteractable) return;

        CurrentInteractable?.OnStopHover();
        CurrentInteractable = best;
        CurrentInteractable?.OnHover();
    }

    private IInteractable GetClosest()
    {
        _inRange.RemoveAll(i => i == null || (i as MonoBehaviour) == null);

        IInteractable closest = null;
        float minDist = float.MaxValue;
        foreach (var i in _inRange)
        {
            if (!i.CanInteract()) continue;
            var mb = i as MonoBehaviour;
            float dist = Vector3.Distance(transform.position, mb.transform.position);
            if (dist < minDist) { minDist = dist; closest = i; }
        }
        return closest;
    }
}