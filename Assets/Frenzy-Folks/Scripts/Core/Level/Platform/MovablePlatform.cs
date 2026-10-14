using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovablePlatform : MonoBehaviour
{
    [SerializeField] private float speed = 2F;
    [SerializeField] private float pauseDuration = 1F;
    
    private List<Vector3> pathPoints = new List<Vector3>()
    {
        new Vector3(10, 0.8F, 10),
        new Vector3(10, 10F, -10),
        new Vector3(-10, 0.8F, -10),
        new Vector3(-10, 5F, 10),
    };

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