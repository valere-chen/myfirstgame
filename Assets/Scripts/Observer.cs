using UnityEngine;



public class Observer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   
   //游戏角色
   public Transform Player;
   //gameending
   public gameEnding gameEnding;
    //游戏玩家是否进入到牛头雕像视线范围
   public bool IsInSight = false;
    
   private void OnTriggerEnter(Collider other )
    {
        //扫描是否触碰到玩家
        
        if(other.gameObject==Player.gameObject)
        {
            IsInSight = true;
        }
    }
  //扫描到玩家离开
    private void OnTriggerExit(Collider other )
    {
      if(other.gameObject==Player.gameObject)
      {
        IsInSight = false;
      }

    }
    
    
    
    
   
    // Update is called once per frame
    void Update()
    {
        
      if(IsInSight==true)
      {

        Vector3 dir=Player.position-transform.position+Vector3.up;
        Ray ray=new Ray(transform.position,dir);
        RaycastHit raycastHit;
        if(Physics.Raycast(ray,out raycastHit))
        {

          Debug.Log(raycastHit.collider.transform.name);
          
          if(raycastHit.collider.transform==Player)
          {
            //玩家被发现，调用gameEnding的Cought方法
            gameEnding.Cought();
         
          }
        }
      
      
      }
    
    
    
    
    
    
    
    
    
    }
}
