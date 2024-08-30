using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using MBaske.Sensors.Grid;

public class SmartAgent : Agent
{
    [SerializeField]
    private List<Transform> targets; // List to store multiple targets
    [SerializeField]
    private float optimalDistance = 30f; // The optimal distance from the targets
    [SerializeField]
    private float tooCloseDistance = 10f; // Too close to the targets
    [SerializeField]
    private float tooFarDistance = 50f; // Too far from the targets
    [SerializeField]
    private Transform centralTransform;
    [SerializeField]
    private float maxDistanceFromCenter = 45f;
    [SerializeField]
    private GridSensorComponent3D gridSensor; // Grid Sensor for detecting targets

    [SerializeField]
    private string targetTag;
    private Rigidbody rb;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Randomize the agent's position and velocity
        transform.position = RandomPosition();
        rb.velocity = Vector3.zero;

        // Randomize each target's position
        foreach (var target in targets)
        {
            target.position = RandomPosition();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Basic ship observations
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.forward);

        // Observations from GridSensorComponent3D
        foreach (var target in gridSensor.GetDetectedGameObjects(targetTag))
        {
            Vector3 relativePosition = target.transform.position - transform.position;
            float distance = relativePosition.magnitude;
            sensor.AddObservation(relativePosition.normalized);
            sensor.AddObservation(distance);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions for moving the agent in 3D
        Vector3 moveVector = new Vector3(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1], actionBuffers.ContinuousActions[2]);
        rb.AddForce(moveVector * 10f, ForceMode.VelocityChange);

        CheckDistanceFromCenter();
        // Calculate reward based on current state to each detected target
        EvaluateDistanceToTargets();
    }

    private void CheckDistanceFromCenter()
    {
        if (Vector3.Distance(transform.position, centralTransform.position) > maxDistanceFromCenter)
        {
            EndEpisode();
        }
    }
    private void EvaluateDistanceToTargets()
    {
        foreach (var target in gridSensor.GetDetectedGameObjects(targetTag))
        {
            float currentDistance = Vector3.Distance(transform.position, target.transform.position);
            if (currentDistance < tooCloseDistance)
            {
                AddReward(-0.05f * (tooCloseDistance - currentDistance));
            }
            else if (currentDistance > tooFarDistance)
            {
                AddReward(-0.05f * (currentDistance - tooFarDistance));
            }
            else if (currentDistance > tooCloseDistance && currentDistance < optimalDistance)
            {
                AddReward(0.1f * (1 - Mathf.Abs(currentDistance - optimalDistance) / (optimalDistance - tooCloseDistance)));
            }
            else if (currentDistance < tooFarDistance && currentDistance > optimalDistance)
            {
                AddReward(0.1f * (1 - Mathf.Abs(currentDistance - optimalDistance) / (tooFarDistance - optimalDistance)));
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        actionsOut.ContinuousActions.Array[0] = Input.GetAxis("Horizontal");
        actionsOut.ContinuousActions.Array[1] = Input.GetAxis("Vertical");
        actionsOut.ContinuousActions.Array[2] = Input.GetAxis("UpDown");
    }
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("wall") || collision.gameObject.CompareTag(targetTag) || collision.gameObject.CompareTag("Spaceship"))
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }
    private Vector3 RandomPosition()
    {
        return new Vector3(Random.Range(-10 , 10), Random.Range(-10, 10), Random.Range(-10, 10));
    }
}
