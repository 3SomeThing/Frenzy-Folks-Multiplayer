using System;
using Unity.VisualScripting;
using UnityEngine;

public class TurretTrap : MonoBehaviour
{
    [SerializeField] private GameObject rotatingObject;
    [SerializeField] private Collider collider;

    private void Awake()
    {
        collider = GetComponent<Collider>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    
}
