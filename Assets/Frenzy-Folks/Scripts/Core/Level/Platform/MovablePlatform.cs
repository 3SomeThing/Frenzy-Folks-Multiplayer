using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class MovablePlatform : NetworkBehaviour
{
    [SerializeField] private float speed = 5F;
    [SerializeField] private float pauseDuration = 1F;

    public float Speed => speed;

    private List<Vector3> pathPoints = new List<Vector3>();

    private int _currentPathIndex;
    private bool isPaused;
    private IEnumerator pauseCoroutine;
    
    private Rigidbody _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    public void SetPathPoints(List<Vector3> points)
    {
        pathPoints.Clear();
        pathPoints.AddRange(points);
        _currentPathIndex = 0;
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (!IsServer) return;
    //     
    //     Debug.Log("VAR");
    //     NetworkPlayer player = other.gameObject.GetComponent<NetworkPlayer>();
    //
    //     if (!player) return;
    //
    //     Debug.Log("VAR2");
    //     Rigidbody playerBody = player.GetComponent<Rigidbody>();
    //     if (Math.Abs(playerBody.linearVelocity.y /*- _body.linearVelocity.y*/) < 0.1F) return;
    //
    //     player.NetworkObject.TrySetParent(gameObject);
    // }
    //
    // private void OnTriggerExit(Collider other)
    // {
    //     if (!IsServer) return;
    //     
    //     Debug.Log("lalala");
    //     NetworkPlayer player = other.gameObject.GetComponent<NetworkPlayer>();
    //
    //     if (!player) return;
    //     Debug.Log("lalala2");
    //
    //     Rigidbody playerBody = player.GetComponent<Rigidbody>();
    //     if (Math.Abs(playerBody.linearVelocity.y /*- _body.linearVelocity.y*/) < 0.1F) return;
    //
    //     player.NetworkObject.TryRemoveParent();
    // }

    // private void OnCollisionEnter(Collision collision)
    // {
    //     NetworkPlayer player = collision.gameObject.GetComponent<NetworkPlayer>();
    //
    //     if (!player) return;
    //     
    //     bool fromTop = collision.contacts.Any(contact => contact.normal.y < 0F);
    //     if (!fromTop) return;
    //     
    //     player.NetworkObject.TrySetParent(gameObject);
    // }
    //
    // private void OnCollisionExit(Collision collision)
    // {
    //     NetworkPlayer player = collision.gameObject.GetComponent<NetworkPlayer>();
    //
    //     if (!player) return;
    //
    //     Rigidbody playerBody = player.GetComponent<Rigidbody>();
    //     if (Math.Abs(playerBody.linearVelocity.y /*- _body.linearVelocity.y*/) < 0.1F) return;
    //
    //     player.NetworkObject.TryRemoveParent();
    // }

    private void FixedUpdate()
    {
        if (pathPoints.Count == 0 || isPaused)
            return;

        Vector3 target = pathPoints[_currentPathIndex];
        _body.MovePosition(Vector3.MoveTowards(
            _body.position, target, speed * Time.fixedDeltaTime));

        if (Vector3.Distance(_body.position, target) < 0.01F)
        {
            isPaused = true;
            _currentPathIndex = (_currentPathIndex + 1) % pathPoints.Count;
            
            StartCoroutine(pauseCoroutine = PausePlatform());
        }
    }

    private IEnumerator PausePlatform()
    {
        yield return new WaitForSeconds(pauseDuration);
        
        isPaused = false;
        StopCoroutine(pauseCoroutine);
    }
}