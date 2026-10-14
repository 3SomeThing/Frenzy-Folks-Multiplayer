using System.Collections.Generic;
using SUPERCharacter;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private List<GameObject> localOnlyObjects = new();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        foreach (GameObject localOnlyObject in localOnlyObjects) localOnlyObject.SetActive(true);

        GetComponent<SUPERCharacterAIO>().enabled = true;
    }
}