using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   //定义旋转速度
   public float turnSpeed = 20f;
   //定义游戏人物上的组件
   Animator m_Animator;
   Rigidbody m_Rigidbody;
   //定义游戏人物移动的矢量
   Vector3 m_Movement;
  //游戏人物旋转的角度
  Quaternion m_Quaternion = Quaternion.identity;
  //定义游戏人物移动的速度
  
    void Start()
    {
        //获取人物上的刚体，动画控制机组件
        m_Animator=GetComponent<Animator>();
        m_Rigidbody=GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
   private void FixedUpdate()
   {
     //获取是否有水平竖直方向的键值输入
     float horizontal=Input.GetAxis("Horizontal");
     float vertical=Input.GetAxis("Vertical");
     //设置游戏人物移动的方向
     m_Movement.Set(horizontal,0f,vertical);  
     m_Movement.Normalize();

     //定义游戏人物是否移动的bool值,对比水平方向是否有输入
     bool hasHorizontalInput = !Mathf.Approximately(horizontal, 0f);
        bool hasVerticalInput = !Mathf.Approximately(vertical, 0f);
     
     
     //将蓝色isWalking赋值给isWallking，将会导致人物状态条件“isWallking”变化
     bool isWalking=hasHorizontalInput||hasVerticalInput;

     m_Animator.SetBool("isWalking",isWalking);
     
     //旋转的过渡，通过三元数转到四元数获取游戏人物当前应有的角度
     Vector3 desiredForward=Vector3.RotateTowards(transform.forward,m_Movement,turnSpeed*Time.deltaTime,0f);
     m_Quaternion=Quaternion.LookRotation(desiredForward);
   
   }


 //游戏人物旋转和移动
 private void OnAnimatorMove()
 {
   m_Rigidbody.MovePosition(m_Rigidbody.position+m_Movement*m_Animator.deltaPosition.magnitude);
   m_Rigidbody.MoveRotation(m_Quaternion);

 }
}
