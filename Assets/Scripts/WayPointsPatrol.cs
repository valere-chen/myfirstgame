using UnityEngine;
using UnityEngine.AI;

public class WayPointsPatrol : MonoBehaviour
{
    //导航组件
    private NavMeshAgent navMeshAgent;

    //导航点数组
    public Transform[] wayPoints;

    //当前巡逻目标点的索引
    int m_currentpointIndex;
    void Start()
    {
        //获取导航组件
        navMeshAgent = GetComponent<NavMeshAgent>();
        //从起点到达初始目标点
        navMeshAgent.SetDestination(wayPoints[0].position);
    }

   
    void Update()
    {

        //到达目标点，前往下一个目标点
        if(navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
        {
           m_currentpointIndex=(m_currentpointIndex + 1) % wayPoints.Length;


           navMeshAgent.SetDestination(wayPoints[m_currentpointIndex].position);
        }
        

    }
}
