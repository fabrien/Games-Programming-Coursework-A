using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

public class SphereScript : MonoBehaviour
{
    public float detectionRadius;
    public float playerInfluence;
    public float spheresInfluence;
    public float randomInfluence;
    public float centerMovement;
    public float requiredDistance;
    public float matchingStrength;
    
    private const float timeBeforeDeath = 2.0f;
    private float timeLeft = timeBeforeDeath;

    public float speed;

    private Rigidbody _rigidbody;
    private int _sphereMask;

    private PathFinding _pathFinding;

    public void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        LayerMask sphereMask = LayerMask.NameToLayer("Sphere");
        _sphereMask = 1 << sphereMask.value;
        _pathFinding = GetComponent<PathFinding>();
    }

    public void Update()
    {
        if (Math.Abs(_rigidbody.velocity.y) < 0.8 && transform.position.y <= 0.5)
            timeLeft -= Time.deltaTime;
        else {
            timeLeft = timeBeforeDeath;
        }
        if (timeLeft <= 0)
            Destroy(gameObject);
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        var totalInfluence = playerInfluence + spheresInfluence + randomInfluence;

        // var spheresDirection = FindSpheresDirection() * spheresInfluence;
        var spheresDirection = BoidDirection() * spheresInfluence;
        var randomDirection = FindRandomDirection() * randomInfluence;
        var playerDirection = FindPlayerDirection() * playerInfluence;

        var finalDirection = (spheresDirection + randomDirection + playerDirection) / totalInfluence;

        // transform.position += finalDirection * speed;
        var velocity = _rigidbody.velocity;
        var y = velocity.y;
        velocity = Vector3.Lerp(velocity, finalDirection * speed, 0.3f);
        _rigidbody.velocity = new Vector3(velocity.x, y, velocity.z);
    }

    private static Vector3 FindRandomDirection()
    {
        return new Vector3(Random.Range(-100, 100), 0, Random.Range(-100, 100)).normalized;
    }

    private Vector3 FindPlayerDirection()
    {
        /*var x = Input.GetAxis("Horizontal");
        var z = Input.GetAxis("Vertical");
        return new Vector3(x, 0, z).normalized;*/
        return _pathFinding.direction;
    }
    
    private Vector3 FindSpheresDirection()
    {
        LayerMask sphereMask = 1 << 10;
        var spherePosAverage = Vector3.zero;
        var position = transform.position;
        var spheresColliders = Physics.OverlapSphere(position, detectionRadius, sphereMask);
        spherePosAverage = spheresColliders.Aggregate(
            spherePosAverage,
            (current, sColl) => current + sColl.gameObject.transform.position
            );

        spherePosAverage /= spheresColliders.Length;
        var direction = spherePosAverage - position;
        direction.y = 0;
        return direction.normalized;
    }

    private Vector3 BoidDirection()
    {
        var position = transform.position;
        var spheresColliders = Physics.OverlapSphere(position, detectionRadius, _sphereMask);
        if (spheresColliders.Length == 0)
            return Vector3.zero;
        
        var spheresPosition = spheresColliders.Select(sColl => sColl.transform.position).ToArray();
        var spheresVelocity = spheresColliders.Select(sColl => sColl.GetComponent<Rigidbody>().velocity).ToArray();
        
        var ruleOne = BoidRuleOneCenter(spheresPosition);
        var ruleTwo = BoidRuleTwoAway(spheresPosition);
        var ruleThree = BoidRuleThreeMatch(spheresVelocity);
        return ruleOne + ruleTwo + ruleThree;
    }

    private Vector3 BoidRuleOneCenter(IReadOnlyCollection<Vector3> spheresPosition)
    {
        var spheresPosAverage = spheresPosition.Aggregate(
            Vector3.zero,
            (current, sPos) => current + sPos
        );
        spheresPosAverage /= spheresPosition.Count;
        return (spheresPosAverage - transform.position) * centerMovement;
    }

    private Vector3 BoidRuleTwoAway(IReadOnlyCollection<Vector3> spheresPosition)
    {
        var position = transform.position;
        
        return spheresPosition.Aggregate(
            Vector3.zero,
            (current, sPos) =>
            {
                var distance = sPos - position;
                if (distance.magnitude <= requiredDistance)
                    return current - distance;
                return current;
            });
    }

    private Vector3 BoidRuleThreeMatch(IReadOnlyCollection<Vector3> spheresVelocity)
    {
        var averageVelo = spheresVelocity.Aggregate(
            Vector3.zero,
            (current, sVel) => current + sVel);
        averageVelo /= spheresVelocity.Count;
        return averageVelo * matchingStrength;
    }
}
