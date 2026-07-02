using UnityEngine;

public class camerafollow1 : MonoBehaviour
{
    //跟随的人物
    public Transform Player;
    //相机与人物之间的距离
    Vector3 offset ;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        offset = transform.position - Player.position;

    }

    // Update is called once per frame
    void Update()
    {
        
     
        transform.position =  Player.position + offset;


    }
}
