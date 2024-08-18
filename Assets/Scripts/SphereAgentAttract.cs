using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class SphereAgentAttract : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Material loseMat;
    [SerializeField] private Material winMat;
    [SerializeField] private MeshRenderer WinLoseMeshRenderer;
    
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(targetTransform.position);
        
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        var moveX = actions.ContinuousActions[0];
        var moveY = actions.ContinuousActions[1];
        var moveZ = actions.ContinuousActions[2];


        transform.position += new Vector3(moveX, moveY, moveZ) * Time.deltaTime * moveSpeed;
    }

    public override void OnEpisodeBegin()
    {
        var targetRandomX = Random.Range(0f, -4f);
        var targetRandomY = Random.Range(1.5f, -2f);
        var targetRandomZ = Random.Range(-5.05f, -7.5f);
        targetTransform.localPosition = new Vector3(targetRandomX, targetRandomY, targetRandomZ);
        
        var agentRandomX = Random.Range(0f, -4.7f);
        var agentRandomY = Random.Range(2.37f, -2.43f);
        var agentRandomZ = Random.Range(1.88f, -0.5f);
        transform.localPosition = new Vector3(agentRandomX, agentRandomY, agentRandomZ);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
        continuousActions[2] = Input.GetAxisRaw("Mouse ScrollWheel");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Target>(out Target target))
        {
            SetReward(+1);
            WinLoseMeshRenderer.sharedMaterial = winMat;
            EndEpisode();
        }
        
        if (other.TryGetComponent<Obstacle>(out Obstacle obstacle))
        {
            SetReward(-1);
            WinLoseMeshRenderer.sharedMaterial = loseMat;
            EndEpisode();
        }

    }
}
