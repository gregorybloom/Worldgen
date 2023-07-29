
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControlManager : MonoBehaviour
{
    public GameControlRelay GameControlRelay;

    // Start is called before the first frame update
    void Start()
    {
        if(GameControlRelay == null) GameControlRelay = GetComponent<GameControlRelay>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
