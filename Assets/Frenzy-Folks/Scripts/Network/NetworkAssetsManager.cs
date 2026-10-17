using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class NetworkAssetsManager : NetworkBehaviour
{
    public static NetworkAssetsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayForEveryone(AudioSource source)
    {
        PlayAssetForEveryone(source, false);
    }

    public void PlayForEveryone(ParticleSystem particles)
    {
        PlayAssetForEveryone(particles, true);
    }

    public void SetActiveForEveryone(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        NetworkObject sourceObject = target.GetComponentInParent<NetworkObject>();
        if (sourceObject == null || !sourceObject.IsSpawned)
        {
            target.SetActive(active);
            return;
        }

        NetworkObjectReference sourceReference = new NetworkObjectReference(sourceObject);
        string sourcePath = GetRelativePath(sourceObject.transform, target.transform);

        if (IsServer)
        {
            SetActiveForEveryoneClientRpc(sourceReference, sourcePath, active);
        }
        else
        {
            SetActiveForEveryoneServerRpc(sourceReference, sourcePath, active);
        }
    }

    private void PlayAssetForEveryone(Component asset, bool isParticleSystem)
    {
        if (asset == null)
        {
            return;
        }

        NetworkObject sourceObject = asset.GetComponentInParent<NetworkObject>();
        if (!IsSpawned || (!IsServer && !IsClient))
        {
            PlayAssetLocally(asset, isParticleSystem);
            return;
        }

        NetworkObjectReference sourceReference = default;
        string sourcePath;
        if (sourceObject != null && sourceObject.IsSpawned)
        {
            sourceReference = new NetworkObjectReference(sourceObject);
            sourcePath = GetRelativePath(sourceObject.transform, asset.transform);
        }
        else
        {
            sourcePath = GetHierarchyPath(asset.transform);
        }

        if (IsServer)
        {
            PlayForEveryoneClientRpc(sourceReference, sourcePath, isParticleSystem);
        }
        else
        {
            PlayForEveryoneServerRpc(sourceReference, sourcePath, isParticleSystem);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayForEveryoneServerRpc(
        NetworkObjectReference sourceReference,
        string sourcePath,
        bool isParticleSystem)
    {
        if (TryGetAsset(sourceReference, sourcePath, isParticleSystem, out _))
        {
            PlayForEveryoneClientRpc(sourceReference, sourcePath, isParticleSystem);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void PlayForEveryoneClientRpc(
        NetworkObjectReference sourceReference,
        string sourcePath,
        bool isParticleSystem)
    {
        if (TryGetAsset(sourceReference, sourcePath, isParticleSystem, out Component asset))
        {
            PlayAssetLocally(asset, isParticleSystem);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetActiveForEveryoneServerRpc(
        NetworkObjectReference sourceReference,
        string sourcePath,
        bool active)
    {
        if (TryGetTarget(sourceReference, sourcePath, out _))
        {
            SetActiveForEveryoneClientRpc(sourceReference, sourcePath, active);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void SetActiveForEveryoneClientRpc(
        NetworkObjectReference sourceReference,
        string sourcePath,
        bool active)
    {
        if (TryGetTarget(sourceReference, sourcePath, out GameObject target))
        {
            target.SetActive(active);
        }
    }

    private static bool TryGetAsset(
        NetworkObjectReference sourceReference,
        string sourcePath,
        bool isParticleSystem,
        out Component asset)
    {
        asset = null;

        if (sourceReference.TryGet(out NetworkObject sourceObject))
        {
            Transform sourceTransform = string.IsNullOrEmpty(sourcePath)
                ? sourceObject.transform
                : FindBySiblingPath(sourceObject.transform, sourcePath);

            return TryGetAsset(sourceTransform, isParticleSystem, out asset);
        }

        return TryGetAsset(
            FindSceneObjectBySiblingPath(sourcePath),
            isParticleSystem,
            out asset);
    }

    private static bool TryGetAsset(
        Transform target,
        bool isParticleSystem,
        out Component asset)
    {
        asset = null;
        if (target == null)
        {
            return false;
        }

        asset = isParticleSystem
            ? target.GetComponent<ParticleSystem>()
            : target.GetComponent<AudioSource>();
        return asset != null;
    }

    private static bool TryGetTarget(
        NetworkObjectReference sourceReference,
        string sourcePath,
        out GameObject target)
    {
        target = null;

        if (!sourceReference.TryGet(out NetworkObject sourceObject))
        {
            return false;
        }

        Transform targetTransform = string.IsNullOrEmpty(sourcePath)
            ? sourceObject.transform
            : FindBySiblingPath(sourceObject.transform, sourcePath);

        target = targetTransform != null ? targetTransform.gameObject : null;
        return target != null;
    }

    private static void PlayAssetLocally(Component asset, bool isParticleSystem)
    {
        if (isParticleSystem)
        {
            ((ParticleSystem)asset).Play(true);
        }
        else
        {
            ((AudioSource)asset).Play();
        }
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == target)
        {
            return string.Empty;
        }

        System.Collections.Generic.List<int> pathParts =
            new System.Collections.Generic.List<int>();
        Transform current = target;

        while (current != null && current != root)
        {
            pathParts.Add(current.GetSiblingIndex());
            current = current.parent;
        }

        if (current != root)
        {
            return string.Empty;
        }

        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    private static string GetHierarchyPath(Transform target)
    {
        System.Collections.Generic.List<int> pathParts =
            new System.Collections.Generic.List<int>();
        Transform current = target;

        while (current != null && current.parent != null)
        {
            pathParts.Add(current.GetSiblingIndex());
            current = current.parent;
        }

        pathParts.Reverse();
        return current == null
            ? string.Empty
            : current.name + "|" + string.Join("/", pathParts);
    }

    private static Transform FindBySiblingPath(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return root;
        }

        Transform current = root;
        string[] indexes = path.Split('/');
        foreach (string indexText in indexes)
        {
            if (!int.TryParse(indexText, out int index) ||
                index < 0 ||
                index >= current.childCount)
            {
                return null;
            }

            current = current.GetChild(index);
        }

        return current;
    }

    private static Transform FindSceneObjectBySiblingPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        int separatorIndex = path.IndexOf('|');
        if (separatorIndex < 0)
        {
            return null;
        }

        string rootName = path.Substring(0, separatorIndex);
        string childPath = path.Substring(separatorIndex + 1);
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;
        foreach (GameObject root in roots)
        {
            if (root.name == rootName)
            {
                current = root.transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(childPath))
        {
            return current;
        }

        string[] indexes = childPath.Split('/');
        foreach (string indexText in indexes)
        {
            if (!int.TryParse(indexText, out int childIndex) ||
                childIndex < 0 ||
                childIndex >= current.childCount)
            {
                return null;
            }

            current = current.GetChild(childIndex);
        }

        return current;
    }
}
