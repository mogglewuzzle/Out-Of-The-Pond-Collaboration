using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//put this on a gameobject which represents the location (using position) of what you want to spawn
public class Spawner : MonoBehaviour
{
	//Drag in the prefab from the assets folder to this variable in the inspector
    public GameObject prefabToSpawn;
	
	//Adjust this value to your taste (default is spawn an enemy every 1 second)
    public float intervalInSeconds = 1;
	
	//Adjust this value to your taste (default is to (effectively) never stop spawning enemies.
    public int numberToSpawn = int.MaxValue;

	//This function is called when the game starts, and just begins the spawning process
    void Start()
    {
        StartCoroutine(Spawn());    
    }

    //This function is called a "Coroutine". For our purposes, think of them as 
    //a chunk of code that can "wait" in the middle of it (for seconds, or until the next frame etc)
    //see: https://docs.unity3d.com/Manual/Coroutines.html
    IEnumerator Spawn()
    {
        //keep track of how many we have stored (local variable)
        int spawned = 0;

        //Usually coroutines have a loop in them
        //this one runs the coroutine until we have spawned enough objects
        while (spawned < numberToSpawn)
        {
            //this is how we "wait" -- note the confusing syntax of "yield return"
            yield return new WaitForSeconds(intervalInSeconds);

            //this is how we "spawn" objects at runtime
            //https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
            GameObject.Instantiate(prefabToSpawn, transform.position, transform.rotation);

            // count how many we've done so far
            spawned++;
        }
    }
}
