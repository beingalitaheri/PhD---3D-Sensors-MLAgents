using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;
using MBaske.MLUtil;
using System.Collections.Generic;

namespace MBaske.Dogfight
{
    /// <summary>
    /// Agent that pilots a <see cref="Spaceship>"/> and has to 
    /// follow other agents while avoiding <see cref="Asteroid"/>s.
    /// </summary>
    public class PilotAgent2 : Agent
    {
    

        private Spaceship m_Ship;
        private StatsRecorder m_Stats;
        private BulletPool m_Bullets;

        [SerializeField]
        [Tooltip("Reference to sensor component for retrieving detected opponent gameobjects.")]
        private GridSensorComponent3D m_SensorComponent;
        [SerializeField]
        [Tooltip("Ship-to-ship forward axis angle below which agent is rewarded for following opponent.")]
        private float m_TargetFollowAngle = 30;
        [SerializeField]
        [Tooltip("Ship-to-ship distance below which agent is rewarded for following opponent.")]
        private float m_TargetFollowDistance = 50;
        [SerializeField]
        private float minimumTargetFollowDistance = 10;
        private float m_TargetFollowDistanceSqr;
        [SerializeField]
        [Tooltip("Ship-to-ship forward axis angle below which target is locked and auto-fire triggered.")]
        private float m_TargetLockAngle = 10;
        [SerializeField]
        [Tooltip("Ship-to-ship distance below which target is locked and auto-fire triggered.")]
        private float m_TargetLockDistance = 20;
        private float m_TargetLockDistanceSqr;
        [SerializeField]
        [Tooltip("Step interval for writing stats to Tensorboard.")]
        private int m_StatsInterval = 120;
        private int m_CollisionCount;
        private int m_HitScoreCount;

        private IList<GameObject> m_Targets;
        [SerializeField]
        private List<GameObject> targets;
        // Cache targets, so we don't need to repeatedly get PilotAgent component.
        private static IDictionary<GameObject, PilotAgent2> s_TargetCache;
        [SerializeField] private Transform targetTransform;
        private string m_TargetTag; // same for all.
        

        /// <inheritdoc/>
        public override void Initialize()
        {
            m_Bullets = FindObjectOfType<BulletPool>();
            m_Stats = Academy.Instance.StatsRecorder;

            m_Ship = GetComponentInChildren<Spaceship>();
            m_Ship.BulletHitEvent += OnBulletHitSuffered;
            m_Ship.WallHitEvent += OnWallHit;
            m_Ship.CollisionEvent += OnCollision;

            m_Ship.EnvironmentRadius = 100; // TBD
            AddDecisionRequester();


            s_TargetCache ??= CreateTargetCache();
            m_TargetTag ??= m_Ship.tag;
            m_Targets = new List<GameObject>(30);

            m_TargetFollowDistanceSqr = m_TargetFollowDistance * m_TargetFollowDistance;
            m_TargetLockDistanceSqr = m_TargetLockDistance * m_TargetLockDistance;
        }

        private void AddDecisionRequester()
        {
            var req = gameObject.AddComponent<DecisionRequester>();
            req.DecisionPeriod = 4;
            req.TakeActionsBetweenDecisions = true;
        }

        /// <inheritdoc/>
        public override void OnEpisodeBegin()
        {
            m_CollisionCount = 0;
            m_HitScoreCount = 0;
        }

        /// <inheritdoc/>
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_Ship.Throttle);
            sensor.AddObservation(m_Ship.Pitch);
            sensor.AddObservation(m_Ship.Roll);
            sensor.AddObservation(m_Ship.NormPosition);
            sensor.AddObservation(m_Ship.NormOrientation);
            sensor.AddObservation(Normalization.Sigmoid(m_Ship.LocalSpin));
            sensor.AddObservation(Normalization.Sigmoid(m_Ship.LocalVelocity));

            Vector3 pos = m_Ship.transform.position;
            Vector3 fwd = m_Ship.transform.forward;
            m_Targets.Clear();
            targets.Clear();

            // Find targets in vicinity.
            foreach (var target in m_SensorComponent.GetDetectedGameObjects(m_TargetTag))
            {
                Vector3 delta = target.transform.position - pos;
                if (Vector3.Angle(fwd, delta) < m_TargetFollowAngle && 
                    delta.sqrMagnitude < m_TargetFollowDistanceSqr)
                {
                    m_Targets.Add(target);
                    targets.Add(target);
                }
            }
        }

        /// <inheritdoc/>
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.DiscreteActions;
            float speed = m_Ship.ManagedUpdate(actions[0] - 1, actions[1] - 1, actions[2] - 1);
            CheckTargets();

            if (StepCount % m_StatsInterval == 0)
            {
                float n = m_StatsInterval;
                m_Stats.Add("Pilot/Speed", speed);
                m_Stats.Add("Pilot/Collision Ratio", m_CollisionCount / n);
                m_Stats.Add("Pilot/Hit Score Ratio", m_HitScoreCount / n);
                m_CollisionCount = 0;
                m_HitScoreCount = 0;
            }
        }

        /// <inheritdoc/>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.DiscreteActions;
            bool shift = Input.GetKey(KeyCode.LeftShift);
            int vert = Mathf.RoundToInt(Input.GetAxis("Vertical"));
            actions[0] = 1 + (shift ? 0 : vert); // throttle
            actions[1] = 1 + (shift ? vert : 0); // pitch
            actions[2] = 1 + Mathf.RoundToInt(Input.GetAxis("Horizontal")); // roll
        }

        private void CheckTargets()
        {
            Vector3 pos = m_Ship.transform.position;
            Vector3 fwd = m_Ship.transform.forward;
            Vector3 vlc = m_Ship.WorldVelocity;

            foreach (var target in m_Targets)
            {
                Vector3 delta = target.transform.position - pos;
                // Speed towards target.
                float speed = Vector3.Dot(delta.normalized, vlc);
                AddReward(speed * 0.01f);

                if (speed > 0)
                {
                    // Penalize opponent for being followed.
                    if(s_TargetCache.Count == 0){continue;}
                    if(s_TargetCache.ContainsKey(target) == false){continue;}
                    s_TargetCache[target].AddReward(speed * -0.005f);
                }

            }

            // Speed towards target.
            Vector3 deltaToTarget = targetTransform.transform.position - pos;
            float speedTowardsTarget = Vector3.Dot(deltaToTarget.normalized, vlc);
            AddReward(speedTowardsTarget * 0.01f);
            if (speedTowardsTarget <= 0)
            {
                AddReward(-0.01f);
            }

            var distanceToTarget = Vector3.Distance(pos, targetTransform.position);
            if (distanceToTarget > m_TargetFollowDistance)
            {
                AddReward( distanceToTarget * -0.01f);

            }
            if (distanceToTarget >= minimumTargetFollowDistance && distanceToTarget < m_TargetFollowDistance)
            {
                AddReward( distanceToTarget * 0.01f);

            }
        }

        private void OnCollision()
        {
            AddReward(-2);
            m_CollisionCount++;
        }

        /// <summary>
        /// Is invoked only if <see cref="Bullet"/> collider is trigger.
        /// Otherwise, <see cref="OnCollision"/> registers bullet hits.
        /// </summary>
        public void OnBulletHitSuffered()
        {
            AddReward(-1); 
        }

        public void OnWallHit()
        {
            OnCollision();
            s_TargetCache.Clear();
            transform.localPosition = Vector3.zero;
            m_Ship.ManagedUpdate(0, 0, 0);
            m_Ship.LocalPosition = Vector3.zero;
            EndEpisode();
        }

        /// <inheritdoc/>
        public void OnBulletHitScored()
        {
            AddReward(1);
            m_HitScoreCount++;
        }

        private void OnApplicationQuit()
        {
            m_Ship.BulletHitEvent -= OnBulletHitSuffered;
            m_Ship.CollisionEvent -= OnCollision;
        }

        private static IDictionary<GameObject, PilotAgent2> CreateTargetCache()
        {
            var cache = new Dictionary<GameObject, PilotAgent2>();
            var ships = FindObjectsOfType<Spaceship>();
            
            foreach (var ship in ships)
            {
                cache.Add(ship.gameObject, ship.GetComponentInParent<PilotAgent2>());
            }
            return cache;
        }
    }
}