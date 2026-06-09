using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Add this alongside any NPC whose Yarn variables should survive scene changes.
///
/// How to set up:
///   1. Add this component to the NPC GameObject.
///   2. Assign the same DialogueRunner you use on the NPC component.
///   3. List every Yarn variable you want to persist, e.g. "$gave_snowballs".
///
/// How it works:
///   • Just before dialogue starts (called by NPC.Interact via LoadIntoYarn),
///     it reads each variable's value from WorldStateManager and pushes it into
///     Yarn's variable storage — so the dialogue sees the correct saved state.
///   • When dialogue finishes, it reads the variables back out of Yarn and
///     saves them to WorldStateManager as global flags.
///
/// No changes to your .yarn files are needed.
/// </summary>
public class YarnStatePersistence : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;

    [Tooltip("Yarn variable names to persist across scenes. Include the $ prefix, e.g. \"$gave_snowballs\".")]
    [SerializeField] private string[] variablesToPersist;

    private InMemoryVariableStorage _variableStorage;

    private void Start()
    {
        _variableStorage = FindFirstObjectByType<InMemoryVariableStorage>();

        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(SaveToWorldState);
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(SaveToWorldState);
    }

    /// <summary>
    /// Call this right before starting dialogue (NPC.Interact does this automatically).
    /// Pushes saved WorldStateManager values into Yarn's variable storage.
    /// </summary>
    public void LoadIntoYarn()
    {
        if (_variableStorage == null || WorldStateManager.Instance == null) return;

        foreach (string varName in variablesToPersist)
        {
            bool saved = WorldStateManager.Instance.GetGlobalFlag(FlagKey(varName));
            _variableStorage.SetValue(varName, saved);
        }
    }

    /// <summary>
    /// Called automatically when dialogue ends.
    /// Reads Yarn variables and saves their current values to WorldStateManager.
    /// </summary>
    private void SaveToWorldState()
    {
        if (_variableStorage == null || WorldStateManager.Instance == null) return;

        foreach (string varName in variablesToPersist)
        {
            if (_variableStorage.TryGetValue(varName, out bool value))
                WorldStateManager.Instance.SetGlobalFlag(FlagKey(varName), value);
        }
    }

    // Converts "$gave_snowballs" → "yarn_gave_snowballs" so it doesn't collide
    // with any other WorldStateManager flags you might have.
    private static string FlagKey(string yarnVar) => "yarn_" + yarnVar.TrimStart('$');
}
