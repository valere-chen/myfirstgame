using System;
using System.Security;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
public class gameEnding : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //游戏结束时的淡出时间
    public float fadeDuration=1f;
    //游戏胜利图片显示时间
    public float displayDuration=1f;
    //游戏人物
    public GameObject Player;
    //画布背景
    public CanvasGroup ExitBK;
    //游戏胜利时的bool值
    bool IsExit;
    //游戏失败时的bool值
    bool IsFail;
    //游戏失败时的画布1
    public CanvasGroup FailBK;
    
    //定义计时器，用于图片渐变与完全显示
    public float timer = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject==Player.gameObject)
        {
           IsExit=true;
        }
    }
    //游戏失败时的函数
    public void Cought()
    {
        IsFail=true;
    }
      

    void Start()
{
    ExitBK.alpha = 0f;
    FailBK.alpha = 0f;
}
// Update is called once per frame
    void Update()
    {
        if (IsExit)
        {
           //显示游戏胜利图片
           EndLevel(ExitBK,false);
          
        }
        if (IsFail)
        {
             //显示游戏失败图片
            EndLevel(FailBK,true);
          
        }

    }

    //游戏胜利时的方法
    void EndLevel(CanvasGroup igCanvasGroup,bool doRestart)
    {
      //玩家碰到触发器后，计时器开始计时
      timer+= Time.deltaTime;
      //控制图片的alpha值
      igCanvasGroup.alpha=timer/fadeDuration;
      //胜利图片显示一秒，渐变一秒
      if(timer>fadeDuration+displayDuration )
      {
        //游戏失败，重启
       if(doRestart)
       {
         SceneManager.LoadScene("Main");
       }

       else if(!doRestart)
       {
       // 游戏胜利
       Application.Quit();
       
       
       }
      }
    }




    
}


