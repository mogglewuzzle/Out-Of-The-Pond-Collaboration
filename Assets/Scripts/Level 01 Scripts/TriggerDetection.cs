using UnityEngine;
using UnityEngine.Events;

public class TriggerDetection : MonoBehaviour
{

    public UnityEvent onPlayerEnter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    void Start()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            onPlayerEnter.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
